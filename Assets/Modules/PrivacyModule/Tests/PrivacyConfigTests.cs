using NUnit.Framework;
using UnityEngine;

namespace Game.Privacy.Tests
{
    [TestFixture]
    internal sealed class PrivacyConfigTests
    {
        private PrivacyConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<PrivacyConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void TheTestingSettingsReachADevelopmentBuild()
        {
            _config.Configure(" https://example.github.io/privacy/ ", " privacy@example.com ", 7f, true,
                PrivacyDebugGeography.Eea, " ABC ", "", "DEF");

            PrivacySettings settings = _config.BuildSettings(isDebugBuild: true);

            Assert.That(settings.PrivacyPolicyUrl, Is.EqualTo("https://example.github.io/privacy/"));
            Assert.That(settings.ContactEmail, Is.EqualTo("privacy@example.com"));
            Assert.That(settings.GatherTimeoutSeconds, Is.EqualTo(7f));
            Assert.That(settings.ConsentRequest.TagForUnderAgeOfConsent, Is.True);
            Assert.That(settings.ConsentRequest.DebugGeography, Is.EqualTo(PrivacyDebugGeography.Eea));
            Assert.That(settings.ConsentRequest.TestDeviceHashedIds, Is.EqualTo(new[] { "ABC", "DEF" }));
        }

        [Test]
        public void AReleaseBuildAlwaysUsesTheRealLocation()
        {
            _config.Configure("https://example.github.io/privacy/", debugGeography: PrivacyDebugGeography.Eea,
                testDeviceHashedIds: new[] { "ABC" });

            PrivacySettings settings = _config.BuildSettings(isDebugBuild: false);

            Assert.That(settings.ConsentRequest.DebugGeography, Is.EqualTo(PrivacyDebugGeography.Disabled));
            Assert.That(settings.ConsentRequest.TestDeviceHashedIds, Is.Empty);
            Assert.That(settings.LogRequests, Is.False);
        }

        [Test]
        public void TheTimeoutIsAtLeastASecond()
        {
            Assert.That(new PrivacySettings(gatherTimeoutSeconds: 0f).GatherTimeoutSeconds, Is.EqualTo(1f));
        }

        [TestCase("https://maxdeverson.github.io/portal-quest-privacy/", true)]
        [TestCase("  https://example.com/privacy  ", true)]
        [TestCase("http://example.com/privacy", false)]
        [TestCase("example.com/privacy", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void OnlyAnHttpsAddressIsAPrivacyPolicy(string url, bool expected)
        {
            Assert.That(PrivacySettings.IsHttpsUrl(url), Is.EqualTo(expected));
        }
    }
}
