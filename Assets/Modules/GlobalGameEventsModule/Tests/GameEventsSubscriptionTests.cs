using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Events.Tests
{
    [TestFixture]
    internal sealed class GameEventsSubscriptionTests : GameEventsTestFixture
    {
        [Test]
        public void TypedHandlerReceivesTheEventValue()
        {
            GlobalGameEvents events = CreateEvents();
            string received = null;
            events.Subscribe<SuiteLevelEntered>(e => received = e.LevelId);

            events.Publish(new SuiteLevelEntered { LevelId = "forest" });

            Assert.That(received, Is.EqualTo("forest"));
        }

        [Test]
        public void TypedHandlerIgnoresOtherEventTypes()
        {
            GlobalGameEvents events = CreateEvents();
            int calls = 0;
            events.Subscribe<SuiteLevelEntered>(e => calls++);

            events.Publish(new SuiteLevelCompleted { Score = 1 });
            events.Publish(new SuitePlayerDied { Cause = "fall" });

            Assert.That(calls, Is.Zero);
        }

        [Test]
        public void DisposingTheTokenUnsubscribes()
        {
            GlobalGameEvents events = CreateEvents();
            int calls = 0;
            IDisposable token = events.Subscribe<SuiteLevelEntered>(e => calls++);

            events.Publish(new SuiteLevelEntered());
            token.Dispose();
            events.Publish(new SuiteLevelEntered());

            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void DisposingTheTokenTwiceIsHarmless()
        {
            GlobalGameEvents events = CreateEvents();
            int calls = 0;
            IDisposable token = events.Subscribe<SuiteLevelEntered>(e => calls++);

            token.Dispose();
            token.Dispose();
            events.Publish(new SuiteLevelEntered());

            Assert.That(calls, Is.Zero);
            Assert.That(events.SubscriptionCount, Is.Zero);
        }

        [Test]
        public void BranchHandlerReceivesEveryEventUnderIt()
        {
            GlobalGameEvents events = CreateEvents();
            List<string> seen = new List<string>();
            events.SubscribeBranch("Suite.Level", e => seen.Add(e.GetType().Name));

            events.Publish(new SuiteLevelEntered());
            events.Publish(new SuiteLevelCompleted());
            events.Publish(new SuiteBossEntered());

            Assert.That(seen, Is.EqualTo(new[]
            {
                nameof(SuiteLevelEntered),
                nameof(SuiteLevelCompleted),
                nameof(SuiteBossEntered)
            }));
        }

        [Test]
        public void BranchHandlerDoesNotReceiveASiblingWithASharedStringPrefix()
        {
            GlobalGameEvents events = CreateEvents();
            int calls = 0;
            events.SubscribeBranch("Suite.Level", e => calls++);

            events.Publish(new SuiteLevelUpGained { Amount = 3 });

            Assert.That(calls, Is.Zero, "'Suite.Level' must not match 'Suite.LevelUp.Gained'.");
        }

        [Test]
        public void BranchHandlerDoesNotReceiveAnUnrelatedBranch()
        {
            GlobalGameEvents events = CreateEvents();
            int calls = 0;
            events.SubscribeBranch("Suite.Level", e => calls++);

            events.Publish(new SuitePlayerDied());

            Assert.That(calls, Is.Zero);
        }

        [TestCase("*")]
        [TestCase("")]
        [TestCase((string)null)]
        public void RootBranchReceivesEverything(string root)
        {
            GlobalGameEvents events = CreateEvents();
            int calls = 0;
            events.SubscribeBranch(root, e => calls++);

            events.Publish(new SuiteLevelEntered());
            events.Publish(new SuiteLevelUpGained());
            events.Publish(new SuitePlayerDied());

            Assert.That(calls, Is.EqualTo(3));
        }

        [Test]
        public void BranchMatchCacheSurvivesANewSubscription()
        {
            GlobalGameEvents events = CreateEvents();
            int first = 0;
            int second = 0;

            events.SubscribeBranch("Suite.Level", e => first++);
            events.Publish(new SuiteLevelEntered());

            events.SubscribeBranch("Suite.Level", e => second++);
            events.Publish(new SuiteLevelEntered());

            Assert.That(first, Is.EqualTo(2));
            Assert.That(second, Is.EqualTo(1), "the cached match list must be rebuilt after a new branch subscription");
        }

        [Test]
        public void HandlersRunInPriorityOrderLowestFirst()
        {
            GlobalGameEvents events = CreateEvents();
            List<string> order = new List<string>();

            events.Subscribe<SuiteLevelEntered>(e => order.Add("late"), 10);
            events.Subscribe<SuiteLevelEntered>(e => order.Add("early"), -10);
            events.Subscribe<SuiteLevelEntered>(e => order.Add("default"));

            events.Publish(new SuiteLevelEntered());

            Assert.That(order, Is.EqualTo(new[] { "early", "default", "late" }));
        }

        [Test]
        public void EqualPrioritiesKeepSubscriptionOrder()
        {
            GlobalGameEvents events = CreateEvents();
            List<string> order = new List<string>();

            events.Subscribe<SuiteLevelEntered>(e => order.Add("first"));
            events.Subscribe<SuiteLevelEntered>(e => order.Add("second"));
            events.Subscribe<SuiteLevelEntered>(e => order.Add("third"));

            events.Publish(new SuiteLevelEntered());

            Assert.That(order, Is.EqualTo(new[] { "first", "second", "third" }));
        }

        [Test]
        public void TypedAndBranchHandlersShareOnePriorityOrdering()
        {
            GlobalGameEvents events = CreateEvents();
            List<string> order = new List<string>();

            events.Subscribe<SuiteLevelEntered>(e => order.Add("typed-0"));
            events.SubscribeBranch("Suite.Level", e => order.Add("branch-minus5"), -5);
            events.SubscribeBranch("*", e => order.Add("root-plus5"), 5);
            events.Subscribe<SuiteLevelEntered>(e => order.Add("typed-plus10"), 10);

            events.Publish(new SuiteLevelEntered());

            Assert.That(order, Is.EqualTo(new[] { "branch-minus5", "typed-0", "root-plus5", "typed-plus10" }));
        }

        [Test]
        public void SubscribeRejectsANullHandler()
        {
            GlobalGameEvents events = CreateEvents();

            Assert.Throws<ArgumentNullException>(() => events.Subscribe<SuiteLevelEntered>(null));
            Assert.Throws<ArgumentNullException>(() => events.SubscribeBranch("Suite.Level", null));
        }

        [Test]
        public void SubscribingToAnEventWithoutAPathFailsAtSubscribeTime()
        {
            GlobalGameEvents events = CreateEvents();

            Assert.Throws<EventPathFormatException>(() => events.Subscribe<SuiteUnpathed>(e => { }));
        }

        [Test]
        public void DiagnosticsListEveryLiveSubscription()
        {
            GlobalGameEvents events = CreateEvents();
            List<SubscriptionInfo> buffer = new List<SubscriptionInfo>();

            events.Subscribe<SuiteLevelEntered>(e => { }, 3);
            IDisposable branch = events.SubscribeBranch("Suite.Level", e => { });

            events.CopySubscriptions(buffer);
            Assert.That(buffer.Count, Is.EqualTo(2));
            Assert.That(events.SubscriptionCount, Is.EqualTo(2));

            branch.Dispose();
            events.CopySubscriptions(buffer);
            Assert.That(buffer.Count, Is.EqualTo(1));
            Assert.That(buffer[0].IsBranch, Is.False);
            Assert.That(buffer[0].Path, Is.EqualTo("Suite.Level.Entered"));
            Assert.That(buffer[0].Priority, Is.EqualTo(3));
        }

        [Test]
        public void DisposingTheServiceReleasesEverySubscription()
        {
            GlobalGameEvents events = CreateEvents();
            int calls = 0;
            IDisposable typed = events.Subscribe<SuiteLevelEntered>(e => calls++);
            IDisposable branch = events.SubscribeBranch("*", e => calls++);

            events.Dispose();

            Assert.That(events.SubscriptionCount, Is.Zero);
            Assert.Throws<ObjectDisposedException>(() => events.Publish(new SuiteLevelEntered()));

            // Tokens outliving the service must stay safe to dispose; a MonoBehaviour's OnDestroy often runs after
            // the container has already gone.
            Assert.DoesNotThrow(typed.Dispose);
            Assert.DoesNotThrow(branch.Dispose);
            Assert.That(calls, Is.Zero);
        }
    }
}
