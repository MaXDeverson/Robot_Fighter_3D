using System;

namespace Game.Ads
{
    /// <summary>
    /// The rules one <see cref="AdsManagerService"/> runs on. Immutable; built from an <see cref="AdsConfig"/>.
    /// </summary>
    internal sealed class AdsSettings
    {
        /// <summary>The level banners start at unless the config says otherwise.</summary>
        public const int DefaultBannerFromLevel = 10;

        /// <summary>The level interstitials start at unless the config says otherwise.</summary>
        public const int DefaultInterstitialFromLevel = 6;

        /// <summary>The level rewarded ads start at unless the config says otherwise: the first.</summary>
        public const int DefaultRewardedFromLevel = 1;

        /// <summary>Seconds between the close of one interstitial and the next, unless the config says otherwise.</summary>
        public const double DefaultInterstitialCooldownSeconds = 180;

        /// <summary>Creates settings. Every argument left out takes the documented default.</summary>
        /// <param name="bannerFromLevel">The level the player has to reach before banners. Below one is raised to one.</param>
        /// <param name="interstitialFromLevel">The same for interstitials.</param>
        /// <param name="rewardedFromLevel">The same for rewarded ads.</param>
        /// <param name="interstitialCooldownSeconds">
        /// Seconds after an interstitial closes before the next one may be shown. Negative is read as none.
        /// </param>
        /// <param name="rewardedRestartsInterstitialCooldown">A rewarded ad restarts the interstitial cooldown.</param>
        /// <param name="pauseGame">Freeze game time and mute the sound while a full screen ad is up.</param>
        /// <param name="logRequests">Log every request and what came of it.</param>
        public AdsSettings(
            int bannerFromLevel = DefaultBannerFromLevel,
            int interstitialFromLevel = DefaultInterstitialFromLevel,
            int rewardedFromLevel = DefaultRewardedFromLevel,
            double interstitialCooldownSeconds = DefaultInterstitialCooldownSeconds,
            bool rewardedRestartsInterstitialCooldown = true,
            bool pauseGame = true,
            bool logRequests = true)
        {
            BannerFromLevel = Math.Max(1, bannerFromLevel);
            InterstitialFromLevel = Math.Max(1, interstitialFromLevel);
            RewardedFromLevel = Math.Max(1, rewardedFromLevel);
            InterstitialCooldownSeconds = interstitialCooldownSeconds > 0 ? interstitialCooldownSeconds : 0;
            RewardedRestartsInterstitialCooldown = rewardedRestartsInterstitialCooldown;
            PauseGame = pauseGame;
            LogRequests = logRequests;
        }

        /// <summary>The level the player has to reach before banners are shown.</summary>
        public int BannerFromLevel { get; }

        /// <summary>The level the player has to reach before interstitials are shown.</summary>
        public int InterstitialFromLevel { get; }

        /// <summary>The level the player has to reach before rewarded ads are shown.</summary>
        public int RewardedFromLevel { get; }

        /// <summary>Seconds after an interstitial closes before the next one may be shown.</summary>
        public double InterstitialCooldownSeconds { get; }

        /// <summary>Whether a rewarded ad restarts the interstitial cooldown.</summary>
        public bool RewardedRestartsInterstitialCooldown { get; }

        /// <summary>Whether game time is frozen and the sound muted while a full screen ad is up.</summary>
        public bool PauseGame { get; }

        /// <summary>Whether every request and what came of it is logged.</summary>
        public bool LogRequests { get; }

        /// <summary>The level the player has to reach before ads of this type are shown.</summary>
        public int FromLevel(AdType type)
        {
            switch (type)
            {
                case AdType.Banner:
                    return BannerFromLevel;
                case AdType.Interstitial:
                    return InterstitialFromLevel;
                default:
                    return RewardedFromLevel;
            }
        }
    }
}
