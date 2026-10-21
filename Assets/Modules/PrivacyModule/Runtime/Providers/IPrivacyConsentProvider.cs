using System;

namespace Game.Privacy
{
    /// <summary>
    /// A consent platform as the service drives it. Implemented through <see cref="PrivacyConsentProvider"/>; the
    /// interface is what the service depends on, so the tests can hand it a plain fake.
    /// </summary>
    internal interface IPrivacyConsentProvider
    {
        string Platform { get; }

        PrivacyConsentStatus ConsentStatus { get; }

        bool CanRequestAds { get; }

        bool AnalyticsAllowed { get; }

        bool IsPrivacyOptionsRequired { get; }

        bool IsShowingForm { get; }

        void GatherConsent(PrivacyConsentRequest request, Action<string> onCompleted);

        void ShowPrivacyOptions(Action<string> onClosed);

        void ResetConsent();
    }
}
