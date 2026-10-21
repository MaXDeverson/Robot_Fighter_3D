using System;
using Game.Analytics;
using Game.Privacy;
using Zenject;

/// <summary>
/// Hands the player's privacy choices to the analytics consent, now and whenever they change, so nothing is collected
/// before the consent platform has answered and nothing at all where the player has refused.
/// </summary>
/// <remarks>
/// Needs <c>Require Consent</c> on in the <c>AnalyticsConfig</c>: then events wait while the answer is unknown.
/// <list type="bullet">
/// <item>Allowed by the privacy service → <see cref="IAnalyticsConsent.Grant"/>: the waiting events go out.</item>
/// <item>The consent platform has not answered (no connection on the first launch), or the player has not answered
/// its form yet → <see cref="IAnalyticsConsent.Reset"/>: events keep waiting for an answer.</item>
/// <item>The platform has answered and analytics are not allowed → <see cref="IAnalyticsConsent.Deny"/>: events are
/// dropped.</item>
/// </list>
/// </remarks>
public class PrivacyAnalyticsConsent : IInitializable, IDisposable
{
    private readonly IPrivacyService _privacy;
    private readonly IAnalyticsConsent _consent;

    public PrivacyAnalyticsConsent(IPrivacyService privacy, IAnalyticsConsent consent)
    {
        _privacy = privacy;
        _consent = consent;
    }

    public void Initialize()
    {
        _privacy.ConsentChanged += Apply;
        Apply();
    }

    public void Dispose()
    {
        _privacy.ConsentChanged -= Apply;
    }

    private void Apply()
    {
        if (_privacy.AnalyticsAllowed)
            _consent.Grant();
        else if (_privacy.ConsentStatus == PrivacyConsentStatus.Unknown
                 || _privacy.ConsentStatus == PrivacyConsentStatus.Required)
            _consent.Reset();
        else
            _consent.Deny();
    }
}
