using System.Collections.Generic;

namespace Game.Privacy
{
    /// <summary>
    /// What the consent platform is told when consent is gathered — the part of <see cref="PrivacyConfig"/> it needs,
    /// with the testing settings already dropped in a release build.
    /// </summary>
    public sealed class PrivacyConsentRequest
    {
        /// <summary>Creates a request.</summary>
        /// <param name="tagForUnderAgeOfConsent">Treat the player as under the age of consent.</param>
        /// <param name="debugGeography">Where to pretend the device is.</param>
        /// <param name="testDeviceHashedIds">The devices the debug geography applies to.</param>
        public PrivacyConsentRequest(
            bool tagForUnderAgeOfConsent,
            PrivacyDebugGeography debugGeography,
            IReadOnlyList<string> testDeviceHashedIds)
        {
            TagForUnderAgeOfConsent = tagForUnderAgeOfConsent;
            DebugGeography = debugGeography;
            TestDeviceHashedIds = testDeviceHashedIds ?? new string[0];
        }

        /// <summary>
        /// Treat the player as under the age of consent: no form is shown and the ads become limited. Off for a game
        /// whose target audience in Play Console is 13+.
        /// </summary>
        public bool TagForUnderAgeOfConsent { get; }

        /// <summary>Where the platform should believe the device is; <see cref="PrivacyDebugGeography.Disabled"/> in a release build.</summary>
        public PrivacyDebugGeography DebugGeography { get; }

        /// <summary>The hashed ids of the test devices — UMP logs a device's id on its first request. Empty in a release build.</summary>
        public IReadOnlyList<string> TestDeviceHashedIds { get; }
    }
}
