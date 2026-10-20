using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;

namespace Game.Events.Tests
{
    [TestFixture]
    internal sealed class GameEventsResilienceTests : GameEventsTestFixture
    {
        [Test]
        public void UnsubscribingFromInsideAHandlerSkipsThatHandler()
        {
            GlobalGameEvents events = CreateEvents();
            List<string> order = new List<string>();
            IDisposable second = null;

            events.Subscribe<SuiteLevelEntered>(e =>
            {
                order.Add("first");
                second.Dispose();
            }, 0);

            second = events.Subscribe<SuiteLevelEntered>(e => order.Add("second"), 1);
            events.Subscribe<SuiteLevelEntered>(e => order.Add("third"), 2);

            events.Publish(new SuiteLevelEntered());

            Assert.That(order, Is.EqualTo(new[] { "first", "third" }),
                "a handler removed mid-dispatch must not run, and the ones after it must not be skipped");
        }

        [Test]
        public void UnsubscribingSelfFromInsideAHandlerIsSafe()
        {
            GlobalGameEvents events = CreateEvents();
            int calls = 0;
            IDisposable token = null;
            token = events.Subscribe<SuiteLevelEntered>(e =>
            {
                calls++;
                token.Dispose();
            });

            events.Publish(new SuiteLevelEntered());
            events.Publish(new SuiteLevelEntered());

            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void SubscribingFromInsideAHandlerStartsWithTheNextDispatch()
        {
            GlobalGameEvents events = CreateEvents();
            List<string> order = new List<string>();

            events.Subscribe<SuiteLevelEntered>(e =>
            {
                order.Add("original");
                events.Subscribe<SuiteLevelEntered>(_ => order.Add("added"));
            });

            events.Publish(new SuiteLevelEntered());
            Assert.That(order, Is.EqualTo(new[] { "original" }),
                "a handler added mid-dispatch must not be invoked for the event in flight");

            order.Clear();
            events.Publish(new SuiteLevelEntered());
            Assert.That(order, Does.Contain("added"));
        }

        [Test]
        public void UnsubscribingAndSubscribingBranchesMidDispatchIsSafe()
        {
            GlobalGameEvents events = CreateEvents();
            List<string> order = new List<string>();
            IDisposable second = null;

            events.SubscribeBranch("Suite", e =>
            {
                order.Add("first");
                second.Dispose();
                events.SubscribeBranch("Suite", _ => order.Add("added"), 5);
            }, 0);

            second = events.SubscribeBranch("Suite", e => order.Add("second"), 1);
            events.SubscribeBranch("Suite", e => order.Add("third"), 2);

            Assert.DoesNotThrow(() => events.Publish(new SuiteLevelEntered()));
            Assert.That(order, Is.EqualTo(new[] { "first", "third" }));
        }

        [Test]
        public void AHandlerThatThrowsDoesNotStopTheOthers()
        {
            GlobalGameEvents events = CreateEvents();
            List<string> order = new List<string>();

            events.Subscribe<SuiteLevelEntered>(e => order.Add("before"), 0);
            events.Subscribe<SuiteLevelEntered>(e => throw new InvalidOperationException("boom"), 1);
            events.Subscribe<SuiteLevelEntered>(e => order.Add("after"), 2);

            Assert.DoesNotThrow(() => events.Publish(new SuiteLevelEntered()));

            Assert.That(order, Is.EqualTo(new[] { "before", "after" }));
            Assert.That(Logger.Errors.Count, Is.EqualTo(1));
            Assert.That(Logger.Errors[0], Does.Contain("Suite.Level.Entered"));
            Assert.That(Logger.Exceptions[0], Is.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void AnObserverThatThrowsDoesNotStopTheHandlers()
        {
            GlobalGameEvents events = CreateEvents();
            int calls = 0;
            events.AddObserver(new ThrowingObserver());
            events.Subscribe<SuiteLevelEntered>(e => calls++);

            Assert.DoesNotThrow(() => events.Publish(new SuiteLevelEntered()));

            Assert.That(calls, Is.EqualTo(1));
            Assert.That(Logger.Errors.Count, Is.EqualTo(1));
        }

        [Test]
        public void RunawayRecursionStopsWithAChainInTheMessage()
        {
            GlobalGameEvents events = CreateEvents(SettingsWithDepth(4));
            int hops = 0;

            events.Subscribe<SuiteEcho>(e =>
            {
                hops++;
                events.Publish(new SuiteEcho { Hop = e.Hop + 1 });
            });

            GameEventsRecursionException exception = Assert.Throws<GameEventsRecursionException>(
                () => events.Publish(new SuiteEcho()));

            Assert.That(hops, Is.EqualTo(4), "exactly MaxDispatchDepth dispatches should have been allowed");
            Assert.That(exception.Message, Does.Contain("Suite.Echo -> Suite.Echo"));
            Assert.That(exception.Message, Does.Contain("MaxDispatchDepth"));
        }

        [Test]
        public void RecursionUnwindsCleanlyAndTheServiceStaysUsable()
        {
            GlobalGameEvents events = CreateEvents(SettingsWithDepth(3));
            events.Subscribe<SuiteEcho>(e => events.Publish(new SuiteEcho()));

            Assert.Throws<GameEventsRecursionException>(() => events.Publish(new SuiteEcho()));
            Assert.That(events.DispatchDepth, Is.Zero, "the depth counter must unwind even when a dispatch throws");

            int calls = 0;
            events.Subscribe<SuiteLevelEntered>(e => calls++);
            Assert.DoesNotThrow(() => events.Publish(new SuiteLevelEntered()));
            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void RecursionInsideAFlushDoesNotAbortTheRestOfTheQueue()
        {
            GlobalGameEvents events = CreateEvents(SettingsWithDepth(2));
            int survivors = 0;

            events.Subscribe<SuiteEcho>(e => events.Publish(new SuiteEcho()));
            events.Subscribe<SuiteLevelEntered>(e => survivors++);

            events.Enqueue(new SuiteEcho());
            events.Enqueue(new SuiteLevelEntered());

            Assert.DoesNotThrow(() => events.Flush());

            Assert.That(survivors, Is.EqualTo(1));
            Assert.That(Logger.Errors.Count, Is.EqualTo(1));
        }

        [Test]
        public void NestedPublishingBelowTheLimitIsAllowed()
        {
            GlobalGameEvents events = CreateEvents(SettingsWithDepth(4));
            bool innerRan = false;

            events.Subscribe<SuiteLevelEntered>(e => events.Publish(new SuiteLevelCompleted()));
            events.Subscribe<SuiteLevelCompleted>(e => innerRan = true);

            Assert.DoesNotThrow(() => events.Publish(new SuiteLevelEntered()));
            Assert.That(innerRan, Is.True);
        }

        [Test]
        public void TwoServicesDoNotSeeEachOthersEvents()
        {
            GlobalGameEvents first = CreateEvents();
            GlobalGameEvents second = CreateEvents();

            int onFirst = 0;
            int onSecond = 0;
            first.Subscribe<SuiteLevelEntered>(e => onFirst++);
            second.Subscribe<SuiteLevelEntered>(e => onSecond++);
            first.SubscribeBranch("*", e => onFirst++);

            first.Publish(new SuiteLevelEntered());

            Assert.That(onFirst, Is.EqualTo(2));
            Assert.That(onSecond, Is.Zero, "no shared state means no leakage between services");

            second.Publish(new SuiteLevelEntered());

            Assert.That(onFirst, Is.EqualTo(2));
            Assert.That(onSecond, Is.EqualTo(1));
        }

        [Test]
        public void UsingTheServiceFromAnotherThreadThrows()
        {
            GlobalGameEvents events = CreateEvents();
            events.Subscribe<SuiteLevelEntered>(e => { });

            Exception captured = null;
            Thread worker = new Thread(() =>
            {
                try
                {
                    events.Publish(new SuiteLevelEntered());
                }
                catch (Exception exception)
                {
                    captured = exception;
                }
            });

            worker.Start();
            worker.Join();

            Assert.That(captured, Is.TypeOf<GameEventsThreadException>());
        }

        [Test]
        public void TypedPublishAllocatesNothing()
        {
            long controlBefore = GC.GetAllocatedBytesForCurrentThread();
            object control = new object();
            long controlAfter = GC.GetAllocatedBytesForCurrentThread();
            GC.KeepAlive(control);

            if (controlAfter <= controlBefore)
            {
                Assert.Ignore("GC.GetAllocatedBytesForCurrentThread is not instrumented on this runtime.");
            }

            GlobalGameEvents events = CreateEvents();
            int calls = 0;
            events.Subscribe<SuiteLevelEntered>(e => calls++);
            events.Subscribe<SuiteLevelEntered>(e => calls++, 5);

            SuiteLevelEntered sample = new SuiteLevelEntered { LevelId = "forest" };

            // Warm-up: the first publish resolves the path, grows the dispatch chain list and forces the
            // generic code to be prepared. Steady state is what the assertion is about.
            for (int i = 0; i < 128; i++)
            {
                events.Publish(sample);
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 512; i++)
            {
                events.Publish(sample);
            }

            long after = GC.GetAllocatedBytesForCurrentThread();

            Assert.That(calls, Is.EqualTo((128 + 512) * 2));
            Assert.That(after - before, Is.Zero,
                "a typed publish with no observers and no matching branch handler must not allocate");
        }

        private sealed class ThrowingObserver : IEventObserver
        {
            public void OnPublished(in EventPath path, IGameEvent gameEvent)
            {
                throw new InvalidOperationException("observer boom");
            }
        }
    }
}
