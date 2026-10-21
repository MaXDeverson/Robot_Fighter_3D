using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Privacy.Tests
{
    [TestFixture]
    internal sealed class BasePrivacyConsentProviderTests
    {
        private GameObject _host;
        private BasePrivacyConsentProvider _stub;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("StubHost");
            _stub = _host.AddComponent<BasePrivacyConsentProvider>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void NothingIsKnownBeforeTheFirstRequest()
        {
            Assert.That(_stub.ConsentStatus, Is.EqualTo(PrivacyConsentStatus.Unknown));
            Assert.That(_stub.CanRequestAds, Is.False);
            Assert.That(_stub.AnalyticsAllowed, Is.False);
        }

        [Test]
        public void ARegionWithNoFormAllowsEverythingAndNeedsNoOptions()
        {
            _stub.Simulate(BasePrivacyConsentProvider.Simulation.NotRequired);

            string error = Gather();

            Assert.That(error, Is.Null);
            Assert.That(_stub.ConsentStatus, Is.EqualTo(PrivacyConsentStatus.NotRequired));
            Assert.That(_stub.CanRequestAds, Is.True);
            Assert.That(_stub.AnalyticsAllowed, Is.True);
            Assert.That(_stub.IsPrivacyOptionsRequired, Is.False);
        }

        [Test]
        public void ARefusalStillAllowsLimitedAdsButNoAnalytics()
        {
            _stub.Simulate(BasePrivacyConsentProvider.Simulation.ConsentRefused);

            LogAssert.Expect(LogType.Log, new Regex("the player refuses"));
            Gather();

            Assert.That(_stub.ConsentStatus, Is.EqualTo(PrivacyConsentStatus.Obtained));
            Assert.That(_stub.CanRequestAds, Is.True);
            Assert.That(_stub.AnalyticsAllowed, Is.False);
            Assert.That(_stub.IsPrivacyOptionsRequired, Is.True);
        }

        [Test]
        public void ThePlayerCanChangeTheirMindInPrivacyOptions()
        {
            _stub.Simulate(BasePrivacyConsentProvider.Simulation.ConsentRefused);
            LogAssert.Expect(LogType.Log, new Regex("the player refuses"));
            Gather();

            _stub.Simulate(BasePrivacyConsentProvider.Simulation.ConsentGiven);
            string error = "not called";
            LogAssert.Expect(LogType.Log, new Regex("the player consents"));
            _stub.ShowPrivacyOptions(e => error = e);

            Assert.That(error, Is.Null);
            Assert.That(_stub.AnalyticsAllowed, Is.True);
        }

        [Test]
        public void AResetForgetsTheAnswer()
        {
            _stub.Simulate(BasePrivacyConsentProvider.Simulation.ConsentGiven);
            LogAssert.Expect(LogType.Log, new Regex("the player consents"));
            Gather();

            _stub.ResetConsent();

            Assert.That(_stub.ConsentStatus, Is.EqualTo(PrivacyConsentStatus.Unknown));
            Assert.That(_stub.CanRequestAds, Is.False);
            Assert.That(_stub.AnalyticsAllowed, Is.False);
            Assert.That(_stub.IsPrivacyOptionsRequired, Is.False);
        }

        [Test]
        public void OfflineEveryRequestFailsAndNothingChanges()
        {
            _stub.Simulate(BasePrivacyConsentProvider.Simulation.Offline);

            string error = Gather();

            Assert.That(error, Does.Contain("no connection"));
            Assert.That(_stub.ConsentStatus, Is.EqualTo(PrivacyConsentStatus.Unknown));
        }

        private string Gather()
        {
            string error = "not called";
            _stub.GatherConsent(new PrivacyConsentRequest(false, PrivacyDebugGeography.Disabled, null), e => error = e);
            return error;
        }
    }
}
