using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Privacy.Tests
{
    [TestFixture]
    internal sealed class PrivacyServiceTests
    {
        private const string PolicyUrl = "https://example.github.io/portal-quest-privacy/";

        private FakeConsentProvider _provider;
        private FakePrivacyClock _clock;
        private FakeUrlOpener _urls;
        private PrivacyService _service;
        private int _changes;

        [SetUp]
        public void SetUp()
        {
            _provider = new FakeConsentProvider();
            _clock = new FakePrivacyClock { RealtimeSinceStartup = 100 };
            _urls = new FakeUrlOpener();
            _changes = 0;
            _service = Create(new PrivacySettings(PolicyUrl, "privacy@example.com", 10f));
        }

        [Test]
        public void ThePlatformsAnswerSavedOnTheDeviceCountsBeforeTheRequest()
        {
            _provider.SetObtained(analyticsAllowed: true);

            PrivacyService nextLaunch = Create(new PrivacySettings(PolicyUrl));

            Assert.That(nextLaunch.CanRequestAds, Is.True);
            Assert.That(nextLaunch.AnalyticsAllowed, Is.True);
        }

        [Test]
        public void TheCallerIsAnsweredOnceWhenThePlatformIsDone()
        {
            int calls = 0;
            _service.GatherConsent(() => calls++);

            Assert.That(calls, Is.Zero, "the platform has not answered yet");

            _provider.SetObtained(analyticsAllowed: true);
            _provider.AnswerGather();

            Assert.That(calls, Is.EqualTo(1));
            Assert.That(_service.CanRequestAds, Is.True);
            Assert.That(_service.AnalyticsAllowed, Is.True);
            Assert.That(_changes, Is.EqualTo(1));
        }

        [Test]
        public void CallsWhileARequestIsOutShareIt()
        {
            int calls = 0;
            _service.GatherConsent(() => calls++);
            _service.GatherConsent(() => calls++);

            _provider.AnswerGather();

            Assert.That(_provider.GatherRequests, Has.Count.EqualTo(1));
            Assert.That(calls, Is.EqualTo(2));
        }

        [Test]
        public void AfterASuccessACallIsAnsweredAtOnceWithoutANewRequest()
        {
            _service.GatherConsent(() => { });
            _provider.AnswerGather();

            bool answered = false;
            _service.GatherConsent(() => answered = true);

            Assert.That(answered, Is.True);
            Assert.That(_provider.GatherRequests, Has.Count.EqualTo(1));
        }

        [Test]
        public void AfterAFailureTheNextCallTriesAgain()
        {
            bool answered = false;
            _service.GatherConsent(() => answered = true);
            LogAssert.Expect(LogType.Warning, new Regex("Consent could not be gathered: offline"));
            _provider.AnswerGather("offline");

            Assert.That(answered, Is.True, "a failure answers the caller too");

            _service.GatherConsent(() => { });

            Assert.That(_provider.GatherRequests, Has.Count.EqualTo(2));
        }

        [Test]
        public void TheTimeoutAnswersTheCallerAndALateAnswerStillCounts()
        {
            bool answered = false;
            _service.GatherConsent(() => answered = true);

            _clock.RealtimeSinceStartup += 9.9;
            _service.Tick();
            Assert.That(answered, Is.False, "not yet");

            LogAssert.Expect(LogType.Warning, new Regex("has not answered in 10 s"));
            _clock.RealtimeSinceStartup += 0.2;
            _service.Tick();
            Assert.That(answered, Is.True);
            Assert.That(_service.CanRequestAds, Is.False);

            _provider.SetObtained(analyticsAllowed: false);
            _provider.AnswerGather();

            Assert.That(_changes, Is.EqualTo(1), "the late answer is reported");
            Assert.That(_service.CanRequestAds, Is.True);
            Assert.That(_service.AnalyticsAllowed, Is.False);

            bool again = false;
            _service.GatherConsent(() => again = true);
            Assert.That(again, Is.True, "the late answer was a success");
            Assert.That(_provider.GatherRequests, Has.Count.EqualTo(1));
        }

        [Test]
        public void TheTimeoutStandsStillWhileThePlayerReadsTheForm()
        {
            bool answered = false;
            _service.GatherConsent(() => answered = true);

            _clock.RealtimeSinceStartup += 5;
            _service.Tick();
            _provider.IsShowingForm = true;
            _clock.RealtimeSinceStartup += 60;
            _service.Tick();
            Assert.That(answered, Is.False, "the form is up");

            _provider.IsShowingForm = false;
            _clock.RealtimeSinceStartup += 9.9;
            _service.Tick();
            Assert.That(answered, Is.False, "the clock started again when the form closed");

            LogAssert.Expect(LogType.Warning, new Regex("has not answered in 10 s"));
            _clock.RealtimeSinceStartup += 0.2;
            _service.Tick();
            Assert.That(answered, Is.True);
        }

        [Test]
        public void ACallAfterTheTimeoutWaitsForTheRequestStillOut()
        {
            _service.GatherConsent(() => { });
            LogAssert.Expect(LogType.Warning, new Regex("has not answered"));
            _clock.RealtimeSinceStartup += 11;
            _service.Tick();

            bool answered = false;
            _service.GatherConsent(() => answered = true);

            Assert.That(_provider.GatherRequests, Has.Count.EqualTo(1), "no second request while one is out");
            Assert.That(answered, Is.False);

            _provider.AnswerGather();
            Assert.That(answered, Is.True);
        }

        [Test]
        public void ASecondAnswerFromThePlatformIsIgnored()
        {
            int calls = 0;
            _service.GatherConsent(() => calls++);
            _provider.AnswerGather();

            LogAssert.Expect(LogType.Warning, new Regex("answered twice"));
            _provider.AnswerGather();

            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void APlatformThatThrowsCountsAsAFailure()
        {
            _provider.ThrowOnGather = true;
            bool answered = false;

            LogAssert.Expect(LogType.Exception, new Regex("platform blew up"));
            LogAssert.Expect(LogType.Warning, new Regex("Consent could not be gathered: platform blew up"));
            _service.GatherConsent(() => answered = true);

            Assert.That(answered, Is.True);
        }

        [Test]
        public void ACallbackThatThrowsDoesNotStopTheOthers()
        {
            bool second = false;
            _service.GatherConsent(() => throw new System.InvalidOperationException("caller blew up"));
            _service.GatherConsent(() => second = true);

            LogAssert.Expect(LogType.Exception, new Regex("caller blew up"));
            _provider.AnswerGather();

            Assert.That(second, Is.True);
        }

        [Test]
        public void TheRequestCarriesTheSettings()
        {
            var request = new PrivacyConsentRequest(true, PrivacyDebugGeography.Eea, new[] { "ABC" });
            PrivacyService service = Create(new PrivacySettings(PolicyUrl, consentRequest: request));

            service.GatherConsent(() => { });

            Assert.That(_provider.GatherRequests[0], Is.SameAs(request));
        }

        [Test]
        public void PrivacyOptionsAreNotShownWhereTheRegionNeedsNone()
        {
            bool closed = false;
            _service.ShowPrivacyOptions(() => closed = true);

            Assert.That(closed, Is.True);
            Assert.That(_provider.OptionsRequests, Is.Zero);
        }

        [Test]
        public void ChangingTheAnswerInPrivacyOptionsIsReported()
        {
            _provider.SetObtained(analyticsAllowed: true);
            _service.GatherConsent(() => { });
            _provider.AnswerGather();
            int changesBefore = _changes;

            bool closed = false;
            _service.ShowPrivacyOptions(() => closed = true);
            _service.ShowPrivacyOptions(() => { });
            Assert.That(_provider.OptionsRequests, Is.EqualTo(1), "the form is already up");

            _provider.AnalyticsAllowed = false;
            _provider.AnswerOptions();

            Assert.That(closed, Is.True);
            Assert.That(_changes, Is.EqualTo(changesBefore + 1));
            Assert.That(_service.AnalyticsAllowed, Is.False);
        }

        [Test]
        public void AResetForgetsTheAnswerAndTheNextCallAsksAgain()
        {
            _provider.SetObtained(analyticsAllowed: true);
            _service.GatherConsent(() => { });
            _provider.AnswerGather();
            int changesBefore = _changes;

            _service.ResetConsent();

            Assert.That(_provider.Resets, Is.EqualTo(1));
            Assert.That(_service.ConsentStatus, Is.EqualTo(PrivacyConsentStatus.Unknown));
            Assert.That(_service.CanRequestAds, Is.False);
            Assert.That(_service.AnalyticsAllowed, Is.False);
            Assert.That(_changes, Is.EqualTo(changesBefore + 1));

            bool answered = false;
            _service.GatherConsent(() => answered = true);

            Assert.That(_provider.GatherRequests, Has.Count.EqualTo(2), "asked again, not answered from the old success");
            Assert.That(answered, Is.False);
        }

        [Test]
        public void AResetIsRefusedWhileARequestIsOut()
        {
            _service.GatherConsent(() => { });

            LogAssert.Expect(LogType.Warning, new Regex("The consent was not reset"));
            _service.ResetConsent();

            Assert.That(_provider.Resets, Is.Zero);
        }

        [Test]
        public void ThePrivacyPolicyOpensInTheBrowser()
        {
            Assert.That(_service.HasPrivacyPolicy, Is.True);

            _service.OpenPrivacyPolicy();

            Assert.That(_urls.Opened, Is.EqualTo(new[] { PolicyUrl }));
        }

        [TestCase("")]
        [TestCase("http://example.com/privacy")]
        [TestCase("privacy.html")]
        public void WithoutAnHttpsPageNothingOpens(string url)
        {
            PrivacyService service = Create(new PrivacySettings(url));
            Assert.That(service.HasPrivacyPolicy, Is.False);

            LogAssert.Expect(LogType.Error, new Regex("There is no privacy policy to open"));
            service.OpenPrivacyPolicy();

            Assert.That(_urls.Opened, Is.Empty);
        }

        private PrivacyService Create(PrivacySettings settings)
        {
            var service = new PrivacyService(settings, _provider, _urls, _clock);
            service.ConsentChanged += () => _changes++;
            return service;
        }
    }
}
