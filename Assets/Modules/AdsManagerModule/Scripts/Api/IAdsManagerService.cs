using System;

namespace Game.Ads
{
    /// <summary>
    /// The ads manager — the one way into the ads module. Shows banners, interstitials and rewarded ads, and decides
    /// whether they may be shown at all: the ad block, the level each type of ad starts at, the interstitial cooldown.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Inject this and nothing else. The ad network behind it — today the <c>BaseAdsProvider</c> stub, later an SDK —
    /// is internal to the module and can be replaced without any game code noticing.
    /// </para>
    /// <para>
    /// Main thread only; the callbacks come on the main thread as well.
    /// </para>
    /// </remarks>
    public interface IAdsManagerService
    {
        /// <summary>Raised when an ad block is added, removed or runs out.</summary>
        event Action AdBlockChanged;

        /// <summary>
        /// Raised once for every request for a full screen ad, when it is answered: with the result its callback gets,
        /// just before that callback runs — whether the ad was shown, or the rules or the network kept it back. For
        /// analytics and anything else that reports on the ads without being the one asking for them.
        /// </summary>
        /// <remarks>
        /// A listener that throws is logged; the other listeners and the callback still run.
        /// </remarks>
        event Action<AdResult> FullscreenAdFinished;

        /// <summary>Whether a banner is on screen now.</summary>
        bool IsBannerVisible { get; }

        /// <summary>Whether an interstitial or a rewarded ad is on screen now.</summary>
        bool IsShowingFullscreenAd { get; }

        /// <summary>
        /// Whether the player has an ad block now, permanent or temporary. While it lasts banners and interstitials are
        /// not shown; rewarded ads still are.
        /// </summary>
        bool IsAdBlockActive { get; }

        /// <summary>Whether the ad block is permanent.</summary>
        bool IsAdBlockPermanent { get; }

        /// <summary>When the temporary ad block ends, in UTC. <c>null</c> when there is none or it is permanent.</summary>
        DateTime? AdBlockEndsAtUtc { get; }

        /// <summary>
        /// Asks for the banner. It is on screen whenever the rules allow and stays asked for until
        /// <see cref="HideBanner"/>: an ad block takes it away and it comes back when the block ends; below its level it
        /// waits until the player gets there.
        /// </summary>
        void ShowBanner();

        /// <summary>Withdraws the request for the banner and takes it off screen.</summary>
        void HideBanner();

        /// <summary>
        /// Whether an ad of this type would be shown if asked for now: the ad block, the level, the cooldown, another
        /// full screen ad on screen and the network having one ready are all checked. For a rewarded ad this is the
        /// check before offering a "watch an ad" button at all; for an interstitial, the choice between the ad and a
        /// flow without one.
        /// </summary>
        /// <remarks>
        /// A yes is not a promise: the network can still fail to show the ad, so keep handling a failed
        /// <see cref="AdResult"/>.
        /// </remarks>
        bool IsAdAvailable(AdType type);

        /// <summary>Shows an interstitial, unless the ad block, the level, the cooldown or the network stop it.</summary>
        /// <param name="onFinished">
        /// Called exactly once: when the ad has closed, or straight away when it is not shown — so a flow waiting for it
        /// always goes on. <see cref="AdResult.IsSuccess"/> says whether it was shown.
        /// </param>
        /// <param name="placement">
        /// Where in the game the ad is shown, for analytics: <c>level_fail</c>. Lower case letters, digits and <c>_</c>.
        /// Left out, it is <c>interstitial</c>.
        /// </param>
        void ShowInterstitial(Action<AdResult> onFinished = null, string placement = null);

        /// <summary>Shows a rewarded ad, unless the level or the network stop it. An ad block does not.</summary>
        /// <param name="onFinished">
        /// Called exactly once: when the ad has closed, or straight away when it is not shown. Give the reward only when
        /// <see cref="AdResult.IsSuccess"/> is true — the player watched to the end.
        /// </param>
        /// <param name="placement">
        /// What the reward is, for analytics: <c>double_coins</c>. Lower case letters, digits and <c>_</c>. Left out, it
        /// is <c>rewarded</c>.
        /// </param>
        void ShowRewarded(Action<AdResult> onFinished, string placement = null);

        /// <summary>
        /// Adds a temporary ad block of so many days. The days add up: seven more on top of three left make ten.
        /// </summary>
        void AddAdBlock(int days);

        /// <summary>Adds a temporary ad block of any length — hours for a promo, minutes for a test.</summary>
        void AddAdBlock(TimeSpan duration);

        /// <summary>Adds a permanent ad block: the "remove ads" purchase.</summary>
        void AddPermanentAdBlock();

        /// <summary>Removes every ad block, permanent and temporary: a refund, a cheat, a test.</summary>
        void RemoveAdBlock();
    }
}
