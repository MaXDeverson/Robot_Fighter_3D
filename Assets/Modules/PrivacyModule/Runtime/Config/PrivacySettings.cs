using System;

namespace Game.Privacy
{
    /// <summary>The settings the service runs on, built once from <see cref="PrivacyConfig"/>.</summary>
    internal sealed class PrivacySettings
    {
        /// <summary>Seconds boot waits for the consent platform by default.</summary>
        public const float DefaultGatherTimeoutSeconds = 10f;

        /// <summary>Creates the settings.</summary>
        /// <param name="privacyPolicyUrl">The privacy policy page.</param>
        /// <param name="contactEmail">Where players write about their data.</param>
        /// <param name="gatherTimeoutSeconds">Seconds boot waits for the platform. Values below one are raised to one.</param>
        /// <param name="consentRequest">What the platform is told.</param>
        /// <param name="logRequests">Log every request and what came of it.</param>
        public PrivacySettings(
            string privacyPolicyUrl = "",
            string contactEmail = "",
            float gatherTimeoutSeconds = DefaultGatherTimeoutSeconds,
            PrivacyConsentRequest consentRequest = null,
            bool logRequests = false)
        {
            PrivacyPolicyUrl = privacyPolicyUrl?.Trim() ?? string.Empty;
            ContactEmail = contactEmail?.Trim() ?? string.Empty;
            GatherTimeoutSeconds = gatherTimeoutSeconds < 1f ? 1f : gatherTimeoutSeconds;
            ConsentRequest = consentRequest ?? new PrivacyConsentRequest(false, PrivacyDebugGeography.Disabled, null);
            LogRequests = logRequests;
        }

        public string PrivacyPolicyUrl { get; }

        public string ContactEmail { get; }

        public float GatherTimeoutSeconds { get; }

        public PrivacyConsentRequest ConsentRequest { get; }

        public bool LogRequests { get; }

        /// <summary>Whether <paramref name="url"/> is an absolute https address.</summary>
        public static bool IsHttpsUrl(string url)
        {
            return Uri.TryCreate(url?.Trim(), UriKind.Absolute, out Uri uri)
                   && uri.Scheme == Uri.UriSchemeHttps
                   && !string.IsNullOrEmpty(uri.Host);
        }
    }
}
