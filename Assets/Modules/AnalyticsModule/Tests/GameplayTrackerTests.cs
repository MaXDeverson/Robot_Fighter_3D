using System.Collections.Generic;
using Game.Analytics.Gameplay;
using Game.Events;
using Game.Gameplay.Events;
using NUnit.Framework;

namespace Game.Analytics.Tests
{
    /// <summary>
    /// The tracking plan end to end: real gameplay events on a real event bus, the real trackers, and a recording sink
    /// at the far end of a real analytics service.
    /// </summary>
    [TestFixture]
    internal sealed class GameplayTrackerTests
    {
        private readonly List<AnalyticsTracker> _trackers = new List<AnalyticsTracker>();

        private GlobalGameEvents _events;
        private AnalyticsService _analytics;
        private RecordingAnalyticsSink _sink;

        [SetUp]
        public void SetUp()
        {
            _events = new GlobalGameEvents(new GameEventsSettings(), new EventPathRegistry(), null);

            AnalyticsSettings settings = new AnalyticsSettings();
            _analytics = new AnalyticsService(
                settings,
                new AnalyticsConsent(settings, new MemoryAnalyticsStorage()),
                new ManualAnalyticsClock(),
                new RecordingAnalyticsLogger());
            _sink = new RecordingAnalyticsSink();
            _analytics.AddSink(_sink);

            _trackers.Add(new LevelAnalyticsTracker(_events, _analytics));
            for (int i = 0; i < _trackers.Count; i++)
            {
                _trackers[i].Initialize();
            }
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _trackers.Count; i++)
            {
                _trackers[i].Dispose();
            }

            _trackers.Clear();
            _analytics.Dispose();
            _events.Dispose();
        }

        [Test]
        public void ALevelStartIsAProgressionStartOnItsIndex()
        {
            _events.Publish(new LevelStarted { LevelIndex = 2 });

            AnalyticsEvent started = _sink.Last;
            Assert.That(started.Name, Is.EqualTo(TrackingPlan.Names.LevelStart));
            Assert.That(started.Category, Is.EqualTo(AnalyticsCategory.Progression));
            Assert.That(started.ProgressionStatus, Is.EqualTo(ProgressionStatus.Start));
            Assert.That(started.HierarchyPath, Is.EqualTo("Level_Start2"));
            Assert.That(started.Source, Is.EqualTo("Level.Started"));
            Assert.That(Integer(started, TrackingPlan.Keys.Level), Is.EqualTo(2));
        }

        [Test]
        public void ALevelCompletionIsAProgressionCompleteOnItsIndex()
        {
            _events.Publish(new LevelCompleted { LevelIndex = 2 });

            AnalyticsEvent completed = _sink.Last;
            Assert.That(completed.Name, Is.EqualTo(TrackingPlan.Names.LevelComplete));
            Assert.That(completed.Category, Is.EqualTo(AnalyticsCategory.Progression));
            Assert.That(completed.ProgressionStatus, Is.EqualTo(ProgressionStatus.Complete));
            Assert.That(completed.HierarchyPath, Is.EqualTo("Level_Complete2"));
            Assert.That(completed.Source, Is.EqualTo("Level.Completed"));
            Assert.That(Integer(completed, TrackingPlan.Keys.Level), Is.EqualTo(2));
            Assert.That(_analytics.RejectedCount, Is.Zero);
        }

        [Test]
        public void StartAndCompletionAreReportedInPlayOrder()
        {
            _events.Publish(new LevelStarted { LevelIndex = 0 });
            _events.Publish(new LevelCompleted { LevelIndex = 0 });

            Assert.That(_sink.Names(), Is.EqualTo(new[] { TrackingPlan.Names.LevelStart, TrackingPlan.Names.LevelComplete }));
        }

        [Test]
        public void ADisposedTrackerStopsReporting()
        {
            for (int i = 0; i < _trackers.Count; i++)
            {
                _trackers[i].Dispose();
            }

            _events.Publish(new LevelStarted { LevelIndex = 0 });

            Assert.That(_sink.Events, Is.Empty);
        }

        private static long Integer(AnalyticsEvent analyticsEvent, string key)
        {
            Assert.That(
                analyticsEvent.TryGetParameter(key, out AnalyticsValue value),
                Is.True,
                "'" + analyticsEvent.Name + "' has no parameter '" + key + "'.");
            return value.AsInteger;
        }
    }
}
