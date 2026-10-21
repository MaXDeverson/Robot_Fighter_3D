using System;
using Game.Ads;
using Game.Privacy;

/// <summary>
/// Tells the ads module whether the player's privacy choices allow ads — the consent platform has answered and, where the
/// law requires it, the player has answered its form — so the ad network starts only after that.
/// </summary>
public class PrivacyAdsConsentSource : IAdsConsentSource
{
    private readonly IPrivacyService _privacy;

    public PrivacyAdsConsentSource(IPrivacyService privacy)
    {
        _privacy = privacy;
    }

    public bool CanRequestAds => _privacy.CanRequestAds;

    public event Action Changed
    {
        add => _privacy.ConsentChanged += value;
        remove => _privacy.ConsentChanged -= value;
    }
}
