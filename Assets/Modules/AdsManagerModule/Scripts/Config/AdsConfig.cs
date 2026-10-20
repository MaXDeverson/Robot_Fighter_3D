using UnityEngine;

namespace Game.Ads
{
    /// <summary>
    /// The rules of the ads manager: the level each type of ad starts at, the interstitial cooldown, pausing, logging.
    /// </summary>
    /// <remarks>
    /// The ad block is not here: it is something the player gets — a purchase, a promo code — and the game grants it at
    /// run time through <see cref="IAdsManagerService"/>.
    /// </remarks>
    [CreateAssetMenu(fileName = "AdsConfig", menuName = "Game/Ads/Ads Config", order = 0)]
    internal sealed class AdsConfig : ScriptableObject
    {
        [Header("Banner")]
        [SerializeField]
        [Min(1)]
        [Tooltip("The level the player has to reach before banners are shown.")]
        private int _bannerFromLevel = AdsSettings.DefaultBannerFromLevel;

        [Header("Interstitial")]
        [SerializeField]
        [Min(1)]
        [Tooltip("The level the player has to reach before interstitials are shown.")]
        private int _interstitialFromLevel = AdsSettings.DefaultInterstitialFromLevel;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Seconds that have to pass after an interstitial closes before the next one is shown. Real time: " +
                 "pauses and slow motion do not stretch it. The game may ask after every loss; this is what keeps it " +
                 "from showing one every time.")]
        private float _interstitialCooldown = (float)AdsSettings.DefaultInterstitialCooldownSeconds;

        [Header("Rewarded")]
        [SerializeField]
        [Min(1)]
        [Tooltip("The level the player has to reach before rewarded ads are shown. 1 means from the start.")]
        private int _rewardedFromLevel = AdsSettings.DefaultRewardedFromLevel;

        [SerializeField]
        [Tooltip("A rewarded ad restarts the interstitial cooldown, so a player who has just watched one by choice is " +
                 "not shown another one right after it.")]
        private bool _rewardedRestartsInterstitialCooldown = true;

        [Header("While a full screen ad is up")]
        [SerializeField]
        [Tooltip("Time.timeScale at 0 and the sound muted until the ad closes, then both put back as they were. iOS " +
                 "keeps the game running under a network's full screen ad.")]
        private bool _pauseGame = true;

        [Header("Debugging")]
        [SerializeField]
        [Tooltip("Log every request and what came of it — shown, or why not. Only in the editor and development " +
                 "builds.")]
        private bool _logRequests = true;

        /// <summary>Builds the immutable settings the service runs on.</summary>
        /// <param name="isDebugBuild">Whether this is the editor or a development build. Only those ever log requests.</param>
        public AdsSettings BuildSettings(bool isDebugBuild)
        {
            return new AdsSettings(
                _bannerFromLevel,
                _interstitialFromLevel,
                _rewardedFromLevel,
                _interstitialCooldown,
                _rewardedRestartsInterstitialCooldown,
                _pauseGame,
                _logRequests && isDebugBuild);
        }
    }
}
