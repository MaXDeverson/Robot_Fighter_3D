using System;
using Game.Ads;
using Game.Events;
using Game.Gameplay.Events;
using UnityEngine;
using Zenject;

/// <summary>
/// Connects the ads manager to what the game already has: the <c>ADSUNLOCK</c> purchase flag and the <c>Ads.*</c>
/// analytics events.
/// </summary>
/// <remarks>
/// Bound in <c>BootInstaller</c> on the <c>ProjectContext</c>, so it lives as long as the ads manager.
/// </remarks>
public sealed class AdsGameBridge : IInitializable, IDisposable
{
    public const string AdsUnlockKey = "ADSUNLOCK";

    private readonly IAdsManagerService _ads;
    private readonly IEventPublisher _events;

    public AdsGameBridge(IAdsManagerService ads, IEventPublisher events)
    {
        _ads = ads;
        _events = events;
    }

    public void Initialize()
    {
        // The "remove ads" purchase was only ever written to PlayerPrefs, so a player who bought it before the ads
        // manager existed gets the permanent ad block here.
        if (PlayerPrefs.GetInt(AdsUnlockKey) != 0 && !_ads.IsAdBlockPermanent)
        {
            _ads.AddPermanentAdBlock();
        }

        _ads.FullscreenAdFinished += OnFullscreenAdFinished;
    }

    public void Dispose()
    {
        _ads.FullscreenAdFinished -= OnFullscreenAdFinished;
    }

    private void OnFullscreenAdFinished(AdResult result)
    {
        bool interstitial = result.Type == AdType.Interstitial;

        switch (result.Status)
        {
            case AdStatus.Completed:
            case AdStatus.Cancelled:
                if (interstitial) _events.Publish(new InterstitialClosed());
                else _events.Publish(new RewardedClosed());
                break;
            case AdStatus.NotReady:
                if (interstitial) _events.Publish(new InterstitialLoadFailed());
                else _events.Publish(new RewardedLoadFailed());
                break;
            case AdStatus.Failed:
                if (interstitial) _events.Publish(new InterstitialContentFailed());
                else _events.Publish(new RewardedContentFailed());
                break;
        }
    }
}
