using System;

namespace Game.Privacy
{
    /// <summary>
    /// The platform when the installer has no prefab at all: it knows nothing and every request fails, so ads and
    /// analytics stay off rather than run without consent.
    /// </summary>
    internal sealed class NullPrivacyConsentProvider : IPrivacyConsentProvider
    {
        private const string NoPlatform = "no consent platform is installed";

        public string Platform
        {
            get { return "none"; }
        }

        public PrivacyConsentStatus ConsentStatus
        {
            get { return PrivacyConsentStatus.Unknown; }
        }

        public bool CanRequestAds
        {
            get { return false; }
        }

        public bool AnalyticsAllowed
        {
            get { return false; }
        }

        public bool IsPrivacyOptionsRequired
        {
            get { return false; }
        }

        public bool IsShowingForm
        {
            get { return false; }
        }

        public void GatherConsent(PrivacyConsentRequest request, Action<string> onCompleted)
        {
            onCompleted(NoPlatform);
        }

        public void ShowPrivacyOptions(Action<string> onClosed)
        {
            onClosed(NoPlatform);
        }

        public void ResetConsent()
        {
            // Nothing is kept.
        }
    }
}
