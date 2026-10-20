using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Analytics.Tests
{
    /// <summary>
    /// The pipeline on its own, built with its constructor: no container, no engine. That is the point — the core does
    /// not depend on either.
    /// </summary>
    [TestFixture]
    internal sealed class AnalyticsServiceTests
    {
        private RecordingAnalyticsLogger _logger;
        private ManualAnalyticsClock _clock;
        private MemoryAnalyticsStorage _storage;
        private AnalyticsConsent _consent;

        [SetUp]
        public void SetUp()
        {
            _logger = new RecordingAnalyticsLogger();
            _clock = new ManualAnalyticsClock();
            _storage = new MemoryAnalyticsStorage();
        }

        [Test]
        public void ATrackedEventReachesEverySink()
        {
            AnalyticsService service = CreateService();
            RecordingAnalyticsSink first = new RecordingAnalyticsSink("First");
            RecordingAnalyticsSink second = new RecordingAnalyticsSink("Second");
            service.AddSink(first);
            service.AddSink(second);

            service.Track(Death());

            Assert.That(first.Names(), Is.EqualTo(new[] { "player_death" }));
            Assert.That(second.Names(), Is.EqualTo(new[] { "player_death" }));
            Assert.That(service.SentCount, Is.EqualTo(1));
            Assert.That(service.History[0].Status, Is.EqualTo(AnalyticsDeliveryStatus.Sent));
        }

        [Test]
        public void TrackingStampsTheEventAndLeavesTheDraftAlone()
        {
            AnalyticsService service = CreateService();
            RecordingAnalyticsSink sink = new RecordingAnalyticsSink();
            service.AddSink(sink);
            _clock.Now = 12.5;

            AnalyticsEvent draft = Death().Build();
            service.Track(draft);
            service.Track(draft);

            Assert.That(sink.Events[0].Sequence, Is.EqualTo(1));
            Assert.That(sink.Events[1].Sequence, Is.EqualTo(2));
            Assert.That(sink.Events[0].Time, Is.EqualTo(12.5));
            Assert.That(sink.Events[0].UtcTime, Is.EqualTo(_clock.UtcNow));
            Assert.That(draft.IsTracked, Is.False);
        }

        [Test]
        public void GlobalParametersJoinEveryEventButTheEventsOwnParametersWin()
        {
            AnalyticsService service = CreateService();
            RecordingAnalyticsSink sink = new RecordingAnalyticsSink();
            service.AddSink(sink);
            service.SetGlobalParameter("level", 3);
            service.SetGlobalParameter("level_id", "Level_3");

            service.Track(Death().With("level", 7));

            AnalyticsEvent tracked = sink.Last;
            Assert.That(tracked.TryGetParameter("level", out AnalyticsValue level), Is.True);
            Assert.That(level.AsInteger, Is.EqualTo(7));
            Assert.That(tracked.TryGetParameter("level_id", out AnalyticsValue levelId), Is.True);
            Assert.That(levelId.AsString, Is.EqualTo("Level_3"));
            Assert.That(tracked.Parameters.Count, Is.EqualTo(2));
        }

        [Test]
        public void ARemovedGlobalParameterIsNoLongerAdded()
        {
            AnalyticsService service = CreateService();
            RecordingAnalyticsSink sink = new RecordingAnalyticsSink();
            service.AddSink(sink);
            service.SetGlobalParameter("level", 3);

            Assert.That(service.RemoveGlobalParameter("level"), Is.True);
            Assert.That(service.RemoveGlobalParameter("level"), Is.False);
            service.Track(Death());

            Assert.That(sink.Last.TryGetParameter("level", out AnalyticsValue _), Is.False);
        }

        [Test]
        public void ASinkThatThrowsDoesNotKeepTheEventFromTheOthers()
        {
            AnalyticsService service = CreateService();
            RecordingAnalyticsSink broken = new RecordingAnalyticsSink("Broken") { ThrowOnSend = true };
            RecordingAnalyticsSink healthy = new RecordingAnalyticsSink("Healthy");
            service.AddSink(broken);
            service.AddSink(healthy);

            service.Track(Death());

            Assert.That(healthy.Events.Count, Is.EqualTo(1));
            Assert.That(_logger.Errors.Count, Is.EqualTo(1));
            Assert.That(service.History[0].Note, Does.Contain("Broken"));

            List<AnalyticsSinkInfo> sinks = new List<AnalyticsSinkInfo>();
            service.CopySinks(sinks);
            Assert.That(sinks[0].FailedCount, Is.EqualTo(1));
            Assert.That(sinks[0].LastError, Does.Contain("refused"));
            Assert.That(sinks[1].SentCount, Is.EqualTo(1));
        }

        [Test]
        public void ASinkThatFailsToInitializeIsSkippedFromThenOn()
        {
            AnalyticsService service = CreateService();
            RecordingAnalyticsSink broken = new RecordingAnalyticsSink("Broken") { ThrowOnInitialize = true };
            service.AddSink(broken);

            service.Track(Death());

            Assert.That(broken.Events, Is.Empty);
            Assert.That(broken.CollectionStates, Is.Empty);
            List<AnalyticsSinkInfo> sinks = new List<AnalyticsSinkInfo>();
            service.CopySinks(sinks);
            Assert.That(sinks[0].IsActive, Is.False);
        }

        [Test]
        public void EverySinkIsToldWhetherCollectionIsAllowed()
        {
            AnalyticsService service = CreateService(requireConsent: true);
            RecordingAnalyticsSink sink = new RecordingAnalyticsSink();
            service.AddSink(sink);

            _consent.Grant();
            _consent.Deny();

            Assert.That(sink.CollectionStates, Is.EqualTo(new[] { false, true, false }));
        }

        [TestCaseSource(nameof(InvalidEvents))]
        public void AnInvalidEventIsRejectedAndNeverSent(AnalyticsEventBuilder builder)
        {
            AnalyticsService service = CreateService();
            RecordingAnalyticsSink sink = new RecordingAnalyticsSink();
            service.AddSink(sink);

            service.Track(builder);

            Assert.That(sink.Events, Is.Empty);
            Assert.That(service.RejectedCount, Is.EqualTo(1));
            Assert.That(service.History[0].Status, Is.EqualTo(AnalyticsDeliveryStatus.Rejected));
            Assert.That(service.History[0].Note, Is.Not.Empty);
            Assert.That(_logger.Errors.Count, Is.EqualTo(1));
        }

        [Test]
        public void TooManyGlobalParametersGetAnOtherwiseValidEventRejected()
        {
            AnalyticsService service = CreateService();
            for (int i = 0; i < AnalyticsEventValidator.MaxParameterCount; i++)
            {
                service.SetGlobalParameter("global_" + i, i);
            }

            service.Track(Death().With("own", 1));

            Assert.That(service.RejectedCount, Is.EqualTo(1));
        }

        [Test]
        public void WithConsentRequiredEventsWaitAndGoOutInOrderOnceItIsGranted()
        {
            AnalyticsService service = CreateService(requireConsent: true);
            RecordingAnalyticsSink sink = new RecordingAnalyticsSink();
            service.AddSink(sink);

            service.Track(Death().With("n", 1));
            service.Track(Death().With("n", 2));

            Assert.That(sink.Events, Is.Empty);
            Assert.That(service.QueuedCount, Is.EqualTo(2));
            Assert.That(service.History[0].Status, Is.EqualTo(AnalyticsDeliveryStatus.Queued));

            _consent.Grant();

            Assert.That(sink.Events.Count, Is.EqualTo(2));
            Assert.That(ValueOf(sink.Events[0], "n"), Is.EqualTo(1));
            Assert.That(ValueOf(sink.Events[1], "n"), Is.EqualTo(2));
            Assert.That(service.QueuedCount, Is.Zero);
            Assert.That(service.History[0].Status, Is.EqualTo(AnalyticsDeliveryStatus.Sent));
        }

        [Test]
        public void DenyingConsentDropsTheQueueAndEverythingAfterIt()
        {
            AnalyticsService service = CreateService(requireConsent: true);
            RecordingAnalyticsSink sink = new RecordingAnalyticsSink();
            service.AddSink(sink);
            service.Track(Death());

            _consent.Deny();
            service.Track(Death());

            Assert.That(sink.Events, Is.Empty);
            Assert.That(service.QueuedCount, Is.Zero);
            Assert.That(service.DroppedCount, Is.EqualTo(2));
            Assert.That(service.History[0].Status, Is.EqualTo(AnalyticsDeliveryStatus.Dropped));
            Assert.That(service.History[1].Status, Is.EqualTo(AnalyticsDeliveryStatus.Dropped));
        }

        [Test]
        public void AFullConsentQueueDropsItsOldestEvent()
        {
            AnalyticsService service = CreateService(requireConsent: true, queueCapacity: 2);

            service.Track(Death().With("n", 1));
            service.Track(Death().With("n", 2));
            service.Track(Death().With("n", 3));
            service.Track(Death().With("n", 4));

            Assert.That(service.QueuedCount, Is.EqualTo(2));
            Assert.That(service.History[0].Status, Is.EqualTo(AnalyticsDeliveryStatus.Dropped));
            Assert.That(service.History[1].Status, Is.EqualTo(AnalyticsDeliveryStatus.Dropped));
            Assert.That(service.History[3].Status, Is.EqualTo(AnalyticsDeliveryStatus.Queued));
            Assert.That(_logger.Warnings.Count, Is.EqualTo(1), "the overflow is reported once, not per event");
        }

        [Test]
        public void WithoutRequiredConsentAnOptOutStopsCollection()
        {
            AnalyticsService service = CreateService();
            RecordingAnalyticsSink sink = new RecordingAnalyticsSink();
            service.AddSink(sink);
            service.Track(Death());

            _consent.Deny();
            service.Track(Death());

            Assert.That(sink.Events.Count, Is.EqualTo(1));
            Assert.That(service.History[1].Status, Is.EqualTo(AnalyticsDeliveryStatus.Dropped));
        }

        [Test]
        public void TheConsentAnswerSurvivesARestart()
        {
            AnalyticsSettings settings = new AnalyticsSettings(true, true, 16, false, 16);
            new AnalyticsConsent(settings, _storage).Grant();

            AnalyticsConsent afterRestart = new AnalyticsConsent(settings, _storage);
            Assert.That(afterRestart.State, Is.EqualTo(AnalyticsConsentState.Granted));
            Assert.That(afterRestart.CollectionAllowed, Is.True);

            afterRestart.Reset();
            Assert.That(new AnalyticsConsent(settings, _storage).State, Is.EqualTo(AnalyticsConsentState.Unknown));
        }

        [Test]
        public void DisabledAnalyticsDropsEverything()
        {
            AnalyticsService service = CreateService(enabled: false);
            RecordingAnalyticsSink sink = new RecordingAnalyticsSink();
            service.AddSink(sink);

            service.Track(Death());

            Assert.That(sink.Events, Is.Empty);
            Assert.That(service.History[0].Status, Is.EqualTo(AnalyticsDeliveryStatus.Dropped));
        }

        [Test]
        public void TheHistoryKeepsTheNewestRecords()
        {
            AnalyticsService service = CreateService(historyCapacity: 3);
            for (int i = 0; i < 5; i++)
            {
                service.Track(Death());
            }

            Assert.That(service.History.Count, Is.EqualTo(3));
            Assert.That(service.History[0].Event.Sequence, Is.EqualTo(3));
            Assert.That(service.History[2].Event.Sequence, Is.EqualTo(5));
            Assert.That(service.History[2].Headline, Is.EqualTo("Design  Death:Level_001:Acid"));
        }

        [Test]
        public void DisposingASinkTokenFlushesAndShutsThatSinkDown()
        {
            AnalyticsService service = CreateService();
            RecordingAnalyticsSink sink = new RecordingAnalyticsSink();
            IDisposable token = service.AddSink(sink);

            token.Dispose();
            token.Dispose();
            service.Track(Death());

            Assert.That(sink.FlushCalls, Is.EqualTo(1));
            Assert.That(sink.ShutdownCalls, Is.EqualTo(1));
            Assert.That(sink.Events, Is.Empty);
        }

        [Test]
        public void DisposingTheServiceShutsEverySinkDownAndLaterEventsAreIgnored()
        {
            AnalyticsService service = CreateService();
            RecordingAnalyticsSink sink = new RecordingAnalyticsSink();
            IDisposable token = service.AddSink(sink);

            service.Dispose();

            Assert.That(sink.ShutdownCalls, Is.EqualTo(1));
            Assert.DoesNotThrow(() => service.Track(Death()));
            Assert.DoesNotThrow(() => token.Dispose());
            Assert.That(sink.ShutdownCalls, Is.EqualTo(1));
            Assert.That(_logger.Warnings.Count, Is.EqualTo(1));
        }

        [Test]
        public void TheSameSinkCannotBeAddedTwice()
        {
            AnalyticsService service = CreateService();
            RecordingAnalyticsSink sink = new RecordingAnalyticsSink();
            service.AddSink(sink);

            Assert.Throws<InvalidOperationException>(() => service.AddSink(sink));
        }

        private static IEnumerable<TestCaseData> InvalidEvents()
        {
            yield return new TestCaseData(AnalyticsEvent.Design("Player Death", "Death"))
                .SetName("NameWithCapitalsAndSpace");
            yield return new TestCaseData(AnalyticsEvent.Design("player_death"))
                .SetName("NoHierarchy");
            yield return new TestCaseData(AnalyticsEvent.Design("player_death", "a", "b", "c", "d", "e", "f"))
                .SetName("SixDesignSegments");
            yield return new TestCaseData(AnalyticsEvent.Progression("level_start", ProgressionStatus.Start, "a", "b", "c", "d"))
                .SetName("FourProgressionSegments");
            yield return new TestCaseData(AnalyticsEvent.Progression("level_start", ProgressionStatus.None, "Level_001"))
                .SetName("ProgressionWithoutStatus");
            yield return new TestCaseData(AnalyticsEvent.Progression("level_complete", ProgressionStatus.Complete, "Level_001").WithValue(2.5))
                .SetName("FractionalScore");
            yield return new TestCaseData(AnalyticsEvent.Design("player_death", "Death:Acid"))
                .SetName("SeparatorInsideASegment");
            yield return new TestCaseData(AnalyticsEvent.Design("player_death", "Death").WithValue(double.NaN))
                .SetName("NotANumberValue");
            yield return new TestCaseData(AnalyticsEvent.Design("player_death", "Death").With("Bad Key", 1))
                .SetName("BadParameterKey");
            yield return new TestCaseData(AnalyticsEvent.Design("player_death", "Death").With("note", new string('x', 101)))
                .SetName("TextTooLong");
            yield return new TestCaseData(AnalyticsEvent.Design("player_death", "Death").With("speed", double.PositiveInfinity))
                .SetName("InfiniteParameter");
            yield return new TestCaseData(AnalyticsEvent.Ad("ad_show", AdAction.None, AdType.Interstitial, "level_fail", "admob"))
                .SetName("AdWithoutAction");
            yield return new TestCaseData(AnalyticsEvent.Ad("ad_show", AdAction.Show, AdType.None, "level_fail", "admob"))
                .SetName("AdWithoutType");
            yield return new TestCaseData(AnalyticsEvent.Ad("ad_show", AdAction.Show, AdType.Interstitial, null, "admob"))
                .SetName("AdWithoutPlacement");
            yield return new TestCaseData(AnalyticsEvent.Ad("ad_show", AdAction.Show, AdType.Interstitial, "level:fail", "admob"))
                .SetName("SeparatorInsideAPlacement");
            yield return new TestCaseData(AnalyticsEvent.Ad("ad_show", AdAction.Show, AdType.Interstitial, "level_fail", "AdMob"))
                .SetName("NetworkWithCapitals");
            yield return new TestCaseData(AnalyticsEvent.Ad("ad_show", AdAction.Show, AdType.Interstitial, "level_fail", new string('a', 33)))
                .SetName("NetworkTooLong");
            yield return new TestCaseData(AnalyticsEvent.Ad("ad_show", AdAction.Show, AdType.Interstitial, "level_fail", "admob").WithFailureReason(AdFailureReason.NoFill))
                .SetName("FailureReasonOnAShow");
            yield return new TestCaseData(AnalyticsEvent.Ad("ad_show", AdAction.Show, AdType.Interstitial, "level_fail", "admob").WithValue(1))
                .SetName("AdWithAValue");
        }

        private static AnalyticsEventBuilder Death()
        {
            return AnalyticsEvent.Design("player_death", "Death", "Level_001", "Acid");
        }

        private static long ValueOf(AnalyticsEvent analyticsEvent, string key)
        {
            Assert.That(analyticsEvent.TryGetParameter(key, out AnalyticsValue value), Is.True, "missing parameter " + key);
            return value.AsInteger;
        }

        private AnalyticsService CreateService(
            bool requireConsent = false,
            int queueCapacity = 16,
            int historyCapacity = 16,
            bool enabled = true)
        {
            AnalyticsSettings settings = new AnalyticsSettings(enabled, requireConsent, queueCapacity, true, historyCapacity);
            _consent = new AnalyticsConsent(settings, _storage);
            return new AnalyticsService(settings, _consent, _clock, _logger);
        }
    }
}
