using System;

namespace Game.Ads
{
    /// <summary>
    /// A network with nothing to show: every full screen ad is <see cref="AdStatus.NotReady"/> and the banner never
    /// appears. Stands in when the installer has no provider to create, so the game still runs.
    /// </summary>
    internal sealed class NullAdsProvider : IAdsProvider
    {
        public string Network
        {
            get { return "none"; }
        }

        public bool IsAdAvailable(AdType type)
        {
            return false;
        }

        public void Initialize()
        {
        }

        public void ShowBanner()
        {
        }

        public void HideBanner()
        {
        }

        public void ShowInterstitial(Action<AdStatus> onClosed)
        {
            onClosed(AdStatus.NotReady);
        }

        public void ShowRewarded(Action<AdStatus> onClosed)
        {
            onClosed(AdStatus.NotReady);
        }
    }
}
