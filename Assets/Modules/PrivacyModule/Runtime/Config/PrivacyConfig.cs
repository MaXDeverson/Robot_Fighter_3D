using System.Collections.Generic;
using UnityEngine;

namespace Game.Privacy
{
    /// <summary>
    /// The privacy settings of the game: where the privacy policy lives, who players write to, and how consent is
    /// gathered and tested.
    /// </summary>
    /// <remarks>
    /// What the consent forms say — the GDPR message, the US state regulations message, the ad partners — is set in the
    /// AdMob console (Privacy &amp; messaging), not here; see the module README.
    /// </remarks>
    [CreateAssetMenu(fileName = "PrivacyConfig", menuName = "Game/Privacy/Privacy Config")]
    public sealed class PrivacyConfig : ScriptableObject
    {
        [Header("Privacy policy")]
        [SerializeField]
        [Tooltip("The privacy policy page: a public https address, not a PDF. The same address goes into Play Console " +
                 "(App content → Privacy policy) and into the AdMob consent messages.")]
        private string _privacyPolicyUrl;

        [SerializeField]
        [Tooltip("Where players write about their data — questions, deletion requests. The address the policy page " +
                 "names.")]
        private string _contactEmail;

        [Header("Consent")]
        [SerializeField]
        [Min(1f)]
        [Tooltip("Seconds boot waits for the consent platform before it goes on without an answer. The platform keeps " +
                 "going and the service reports its answer when it comes.")]
        private float _gatherTimeout = PrivacySettings.DefaultGatherTimeoutSeconds;

        [SerializeField]
        [Tooltip("Treat every player as under the age of consent: no form, limited ads. Leave it off while the target " +
                 "audience in Play Console is 13+.")]
        private bool _tagForUnderAgeOfConsent;

        [Header("Testing — editor and development builds only")]
        [SerializeField]
        [Tooltip("Where the consent platform should believe the device is, to see the GDPR or the US state form from " +
                 "anywhere. A release build always uses the real location.")]
        private PrivacyDebugGeography _debugGeography = PrivacyDebugGeography.Disabled;

        [SerializeField]
        [Tooltip("The hashed ids of the test devices the debug geography applies to. UMP prints a device's id to the " +
                 "log (adb logcat) on its first request.")]
        private List<string> _testDeviceHashedIds = new List<string>();

        [SerializeField]
        [Tooltip("Log every request and what came of it. Only in the editor and development builds.")]
        private bool _logRequests = true;

        /// <summary>The privacy policy page, trimmed; empty when none is set.</summary>
        public string PrivacyPolicyUrl => Trim(_privacyPolicyUrl);

        /// <summary>The contact address, trimmed; empty when none is set.</summary>
        public string ContactEmail => Trim(_contactEmail);

        /// <summary>Builds the immutable settings the service runs on.</summary>
        /// <param name="isDebugBuild">
        /// Whether this is the editor or a development build. Only those keep the debug geography, the test devices and
        /// the logging.
        /// </param>
        internal PrivacySettings BuildSettings(bool isDebugBuild)
        {
            var testDevices = new List<string>();
            if (isDebugBuild && _testDeviceHashedIds != null)
            {
                foreach (string id in _testDeviceHashedIds)
                {
                    string trimmed = Trim(id);
                    if (trimmed.Length > 0)
                        testDevices.Add(trimmed);
                }
            }

            var request = new PrivacyConsentRequest(
                _tagForUnderAgeOfConsent,
                isDebugBuild ? _debugGeography : PrivacyDebugGeography.Disabled,
                testDevices);

            return new PrivacySettings(PrivacyPolicyUrl, ContactEmail, _gatherTimeout, request,
                _logRequests && isDebugBuild);
        }

        /// <summary>Test hook: fills the asset without going through the serializer.</summary>
        internal void Configure(
            string privacyPolicyUrl,
            string contactEmail = null,
            float gatherTimeout = PrivacySettings.DefaultGatherTimeoutSeconds,
            bool tagForUnderAgeOfConsent = false,
            PrivacyDebugGeography debugGeography = PrivacyDebugGeography.Disabled,
            params string[] testDeviceHashedIds)
        {
            _privacyPolicyUrl = privacyPolicyUrl;
            _contactEmail = contactEmail;
            _gatherTimeout = gatherTimeout;
            _tagForUnderAgeOfConsent = tagForUnderAgeOfConsent;
            _debugGeography = debugGeography;
            _testDeviceHashedIds = new List<string>(testDeviceHashedIds);
            _logRequests = false;
        }

        private void OnValidate()
        {
            string url = PrivacyPolicyUrl;
            if (url.Length > 0 && !PrivacySettings.IsHttpsUrl(url))
            {
                Debug.LogWarning(
                    "[Privacy] The privacy policy URL on '" + name + "' is not an https address. Google Play takes only " +
                    "a public https page.", this);
            }

            string email = ContactEmail;
            if (email.Length > 0 && (email.IndexOf('@') <= 0 || email.IndexOf(' ') >= 0))
            {
                Debug.LogWarning("[Privacy] The contact email on '" + name + "' does not look like an address.", this);
            }
        }

        private static string Trim(string value)
        {
            return value?.Trim() ?? string.Empty;
        }
    }
}
