using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Events.Tests
{
    [TestFixture]
    internal sealed class GameEventsQueueTests : GameEventsTestFixture
    {
        [Test]
        public void EnqueueDoesNothingUntilFlush()
        {
            GlobalGameEvents events = CreateEvents();
            int calls = 0;
            events.Subscribe<SuiteLevelEntered>(e => calls++);

            events.Enqueue(new SuiteLevelEntered());
            Assert.That(calls, Is.Zero, "a queued event must not run before the flush");
            Assert.That(events.PendingCount, Is.EqualTo(1));

            int dispatched = events.Flush();

            Assert.That(calls, Is.EqualTo(1));
            Assert.That(dispatched, Is.EqualTo(1));
            Assert.That(events.PendingCount, Is.Zero);
        }

        [Test]
        public void FlushKeepsTheOrderEventsWereQueuedInAcrossTypes()
        {
            GlobalGameEvents events = CreateEvents();
            List<string> order = new List<string>();
            events.SubscribeBranch("*", e => order.Add(e.GetType().Name));

            events.Enqueue(new SuiteLevelEntered());
            events.Enqueue(new SuitePlayerDied());
            events.Enqueue(new SuiteLevelCompleted());
            events.Enqueue(new SuiteLevelEntered());
            events.Flush();

            Assert.That(order, Is.EqualTo(new[]
            {
                nameof(SuiteLevelEntered),
                nameof(SuitePlayerDied),
                nameof(SuiteLevelCompleted),
                nameof(SuiteLevelEntered)
            }));
        }

        [Test]
        public void QueuedEventsCarryTheirValues()
        {
            GlobalGameEvents events = CreateEvents();
            List<int> scores = new List<int>();
            events.Subscribe<SuiteLevelCompleted>(e => scores.Add(e.Score));

            events.Enqueue(new SuiteLevelCompleted { Score = 1 });
            events.Enqueue(new SuiteLevelCompleted { Score = 2 });
            events.Flush();

            Assert.That(scores, Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void AnEventQueuedFromAHandlerWaitsForTheNextFlush()
        {
            GlobalGameEvents events = CreateEvents();
            int entered = 0;
            int died = 0;

            events.Subscribe<SuiteLevelEntered>(e =>
            {
                entered++;
                events.Enqueue(new SuitePlayerDied());
            });
            events.Subscribe<SuitePlayerDied>(e => died++);

            events.Enqueue(new SuiteLevelEntered());
            int firstFlush = events.Flush();

            Assert.That(entered, Is.EqualTo(1));
            Assert.That(died, Is.Zero, "the re-queued event must roll over rather than extend this flush");
            Assert.That(firstFlush, Is.EqualTo(1));
            Assert.That(events.PendingCount, Is.EqualTo(1));

            events.Flush();
            Assert.That(died, Is.EqualTo(1));
        }

        [Test]
        public void FlushOnAnEmptyQueueIsFree()
        {
            GlobalGameEvents events = CreateEvents();

            Assert.That(events.Flush(), Is.Zero);
        }

        [Test]
        public void EnqueueRejectsAnEventWithoutAPathAtTheCallSite()
        {
            GlobalGameEvents events = CreateEvents();

            Assert.Throws<EventPathFormatException>(() => events.Enqueue(new SuiteUnpathed()));
            Assert.That(events.PendingCount, Is.Zero);
        }

        [Test]
        public void FlushRespectsTheBudgetAndWarns()
        {
            GameEventsSettings settings = new GameEventsSettings(
                GameEventsSettings.DefaultMaxDispatchDepth,
                GameEventsSettings.DefaultHistoryCapacity,
                2,
                false,
                false,
                true);

            GlobalGameEvents events = CreateEvents(settings);
            int calls = 0;
            events.Subscribe<SuiteLevelEntered>(e => calls++);

            for (int i = 0; i < 5; i++)
            {
                events.Enqueue(new SuiteLevelEntered());
            }

            int dispatched = events.Flush();

            Assert.That(dispatched, Is.EqualTo(2));
            Assert.That(calls, Is.EqualTo(2));
            Assert.That(events.PendingCount, Is.EqualTo(3));
            Assert.That(Logger.Warnings.Count, Is.EqualTo(1));
            Assert.That(Logger.Warnings[0], Does.Contain("MaxEventsPerFlush"));
        }

        [Test]
        public void ObserversSeeQueuedEventsToo()
        {
            GlobalGameEvents events = CreateEvents();
            CountingObserver observer = new CountingObserver();
            events.AddObserver(observer);

            events.Enqueue(new SuiteLevelEntered());
            events.Publish(new SuitePlayerDied());

            Assert.That(observer.Paths, Is.EqualTo(new[] { "Suite.Player.Died" }));

            events.Flush();

            Assert.That(observer.Paths, Is.EqualTo(new[] { "Suite.Player.Died", "Suite.Level.Entered" }));
        }

        private sealed class CountingObserver : IEventObserver
        {
            internal List<string> Paths { get; } = new List<string>();

            public void OnPublished(in EventPath path, IGameEvent gameEvent)
            {
                Paths.Add(path.Full);
            }
        }
    }
}
