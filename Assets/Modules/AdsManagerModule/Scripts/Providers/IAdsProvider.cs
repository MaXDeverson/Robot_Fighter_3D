using System;

namespace Game.Ads
{
    /// <summary>
    /// An ad network as the ads manager drives it: it shows what it is told to and reports how it went. The rules — the
    /// ad block, the levels, the cooldown, one full screen ad at a time — are the manager's, never the provider's.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Internal, like every provider: nothing outside the module reaches one; the game goes through
    /// <see cref="IAdsManagerService"/>. A real network is one more implementation of this — see the module README.
    /// </para>
    /// <para>
    /// The callbacks of <see cref="ShowInterstitial"/> and <see cref="ShowRewarded"/> are to be invoked exactly once and
    /// on the main thread — SDKs that answer from their own thread have to hop back first. Answering before the show
    /// call returns is fine.
    /// </para>
    /// </remarks>
    internal interface IAdsProvider
    {
        /// <summary>
        /// The network's name for analytics, as <see cref="AdResult.Network"/> carries it: <c>levelplay</c>,
        /// <c>admob</c>. Lower case letters, digits and <c>_</c>, at most 32 characters — what GameAnalytics takes as an
        /// ad SDK name. Read once, when the manager is created.
        /// </summary>
        string Network { get; }

        /// <summary>
        /// Whether the network has an ad of this type loaded and can show it now. Only what the network has — the rules
        /// are the manager's. The manager asks this for full screen ads only; a banner is always requested.
        /// </summary>
        bool IsAdAvailable(AdType type);

        /// <summary>Brings the network up and starts loading ads. Called once, before anything else.</summary>
        void Initialize();

        /// <summary>Puts the banner on screen.</summary>
        void ShowBanner();

        /// <summary>Takes the banner off screen.</summary>
        void HideBanner();

        /// <summary>
        /// The part of the screen the banner covers, in pixels from the bottom left corner like
        /// <see cref="UnityEngine.Screen.safeArea"/>: while it is on screen with an ad in it, empty otherwise. Asked every
        /// frame by the UI that keeps clear of it, so keep it cheap — no call into the SDK.
        /// </summary>
        UnityEngine.Rect BannerScreenArea { get; }

        /// <summary>Shows an interstitial.</summary>
        /// <param name="onClosed">
        /// <see cref="AdStatus.Completed"/> once it was on screen and has closed, however it was closed;
        /// <see cref="AdStatus.NotReady"/> when there was nothing to show; <see cref="AdStatus.Failed"/> when it could
        /// not be shown.
        /// </param>
        void ShowInterstitial(Action<AdStatus> onClosed);

        /// <summary>Shows a rewarded ad.</summary>
        /// <param name="onClosed">
        /// <see cref="AdStatus.Completed"/> when it was watched to the end and the reward is earned;
        /// <see cref="AdStatus.Cancelled"/> when it was closed before; <see cref="AdStatus.NotReady"/> or
        /// <see cref="AdStatus.Failed"/> when it was not shown.
        /// </param>
        void ShowRewarded(Action<AdStatus> onClosed);
    }
}
