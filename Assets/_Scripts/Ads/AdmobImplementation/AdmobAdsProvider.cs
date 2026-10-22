using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEngine;

namespace Game.Ads.AdmobImplementation
{
    /// <summary>
    /// AdMob (the Google Mobile Ads SDK) as the ads manager's network: banners, interstitials and rewarded ads.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lives on the root of <c>Prefabs/AdmobAdsProvider</c>, where its ad unit IDs are set in the Inspector. The
    /// <c>AdsManagerInstaller</c> creates it under the <c>ProjectContext</c> as the manager's <see cref="IAdsProvider"/>
    /// when its <b>Provider</b> is <c>Network</c> and this prefab is its <b>Network Provider Prefab</b>. A type of ad
    /// with no ad unit ID for the platform is never loaded.
    /// </para>
    /// <para>
    /// One interstitial and one rewarded ad are kept loaded. The next one of a type is loaded as soon as the previous one
    /// has closed or failed to show; a load that fails is tried again later, after a wait that doubles with every failure
    /// in a row. The SDK answers from its own thread, so every answer is brought back to the main thread before it is
    /// acted on.
    /// </para>
    /// <para>
    /// The banner is an anchored adaptive one. Its view is created and loaded the first time the manager shows it, and
    /// is only hidden, not destroyed, when the manager takes it away. Once it has an ad, AdMob refreshes it at the rate
    /// set for its ad unit in the AdMob console. The SDK does not tell where on screen it is, so
    /// <see cref="BannerScreenArea"/> works it out the way the SDK places it: its size in pixels against the edge of
    /// <see cref="Screen.safeArea"/> that <b>Banner Position</b> names.
    /// </para>
    /// </remarks>
    internal sealed class AdmobAdsProvider : MonoBehaviour, IAdsProvider
    {
        // Google's sample ad units, which always serve test ads: https://developers.google.com/admob/unity/test-ads
        private const string TestBannerAndroid = "ca-app-pub-3940256099942544/9214589741";
        private const string TestBannerIos = "ca-app-pub-3940256099942544/2435281174";
        private const string TestInterstitialAndroid = "ca-app-pub-3940256099942544/1033173712";
        private const string TestInterstitialIos = "ca-app-pub-3940256099942544/4411468910";
        private const string TestRewardedAndroid = "ca-app-pub-3940256099942544/5224354917";
        private const string TestRewardedIos = "ca-app-pub-3940256099942544/1712485313";

        // AdMob lets a loaded full screen ad expire after an hour; one kept a little shorter is thrown away and reloaded.
        private static readonly TimeSpan AdLifetime = TimeSpan.FromMinutes(55);

        [Header("Banner ad units")]
        [SerializeField]
        [Tooltip("The banner ad unit ID for Android, from the AdMob console: ca-app-pub-XXXXXXXXXXXXXXXX/NNNNNNNNNN.")]
        private string _androidBannerUnitId;

        [SerializeField]
        [Tooltip("The banner ad unit ID for iOS, from the AdMob console.")]
        private string _iosBannerUnitId;

        [Header("Interstitial ad units")]
        [SerializeField]
        [Tooltip("The interstitial ad unit ID for Android, from the AdMob console: ca-app-pub-XXXXXXXXXXXXXXXX/NNNNNNNNNN.")]
        private string _androidInterstitialUnitId;

        [SerializeField]
        [Tooltip("The interstitial ad unit ID for iOS, from the AdMob console.")]
        private string _iosInterstitialUnitId;

        [Header("Rewarded ad units")]
        [SerializeField]
        [Tooltip("The rewarded ad unit ID for Android, from the AdMob console: ca-app-pub-XXXXXXXXXXXXXXXX/NNNNNNNNNN.")]
        private string _androidRewardedUnitId;

        [SerializeField]
        [Tooltip("The rewarded ad unit ID for iOS, from the AdMob console.")]
        private string _iosRewardedUnitId;

        [Header("Banner")]
        [SerializeField]
        [Tooltip("The edge of the screen the banner is anchored to. It is as wide as the screen's safe area; AdMob picks " +
                 "its height for the device.")]
        private AdPosition _bannerPosition = AdPosition.Bottom;

        [Header("Testing")]
        [SerializeField]
        [Tooltip("In the editor and in development builds, load Google's test ad units instead of the IDs above. Clicks " +
                 "on your own live ads can get the AdMob account suspended.")]
        private bool _testAdsInDevelopmentBuilds = true;

        [Header("Loading")]
        [SerializeField]
        [Min(1f)]
        [Tooltip("Seconds before the next attempt after an ad failed to load. Doubles with every failure in a row.")]
        private float _retryDelay = 5f;

        [SerializeField]
        [Min(1f)]
        [Tooltip("The longest wait, in seconds, between two attempts to load.")]
        private float _maxRetryDelay = 120f;

        private bool _sdkReady;

        private InterstitialSlot _interstitial;
        private RewardedSlot _rewarded;

        private string _bannerUnitId;
        private LoadRetry _bannerRetry;
        private bool _bannerWanted;
        private bool _bannerLoading;

        // Created the first time the banner is shown, kept until this object goes; it has an ad once _bannerLoaded is set.
        private BannerView _bannerView;
        private bool _bannerLoaded;

        // Read from the view on every load, so that BannerScreenArea does not call into the SDK every frame.
        private Vector2 _bannerSizeInPixels;

        public string Network
        {
            get { return "admob"; }
        }

        public bool IsAdAvailable(AdType type)
        {
            switch (type)
            {
                case AdType.Banner:
                    return _bannerLoaded;
                case AdType.Interstitial:
                    return _interstitial.HasAd();
                case AdType.Rewarded:
                    return _rewarded.HasAd();
                default:
                    return false;
            }
        }

        public void Initialize()
        {
            bool hasBanner = HasUnitId(_bannerUnitId, "banners");
            bool hasInterstitial = HasUnitId(_interstitial.UnitId, "interstitials");
            bool hasRewarded = HasUnitId(_rewarded.UnitId, "rewarded ads");
            if (!hasBanner && !hasInterstitial && !hasRewarded)
            {
                return;
            }

            if (Debug.isDebugBuild)
            {
                Debug.Log(
                    "[Ads] AdMob starting; ad units: banner " + OrNone(_bannerUnitId) + ", interstitial " +
                    OrNone(_interstitial.UnitId) + ", rewarded " + OrNone(_rewarded.UnitId) + ".", this);
            }

            MobileAds.Initialize(status => OnMainThread(() => OnSdkInitialized(status)));
        }

        public void ShowBanner()
        {
            _bannerWanted = true;
            if (_bannerView != null)
            {
                _bannerView.Show();
            }

            // The first time, or while no ad has come yet: the view is created and loaded once the SDK is up.
            LoadBanner();
        }

        public void HideBanner()
        {
            _bannerWanted = false;
            if (_bannerView != null)
            {
                // Hidden, not destroyed: it comes back at once, and AdMob does not refresh a banner that is off screen.
                _bannerView.Hide();
            }
        }

        public Rect BannerScreenArea
        {
            get
            {
                if (_bannerView == null || !_bannerWanted || !_bannerLoaded)
                {
                    return Rect.zero;
                }

                // Where the SDK puts it: against an edge of the safe area — under the display cutout on Android, inside
                // the safe area layout guide on iOS — centred along that edge unless the position names a corner.
                Rect safe = Screen.safeArea;
                float width = Mathf.Min(_bannerSizeInPixels.x, safe.width);
                float height = Mathf.Min(_bannerSizeInPixels.y, safe.height);

                float x;
                float y;
                switch (_bannerPosition)
                {
                    case AdPosition.Top:
                        x = safe.center.x - width / 2f;
                        y = safe.yMax - height;
                        break;
                    case AdPosition.TopLeft:
                        x = safe.xMin;
                        y = safe.yMax - height;
                        break;
                    case AdPosition.TopRight:
                        x = safe.xMax - width;
                        y = safe.yMax - height;
                        break;
                    case AdPosition.BottomLeft:
                        x = safe.xMin;
                        y = safe.yMin;
                        break;
                    case AdPosition.BottomRight:
                        x = safe.xMax - width;
                        y = safe.yMin;
                        break;
                    case AdPosition.Center:
                        x = safe.center.x - width / 2f;
                        y = safe.center.y - height / 2f;
                        break;
                    case AdPosition.Bottom:
                    default:
                        x = safe.center.x - width / 2f;
                        y = safe.yMin;
                        break;
                }

                return new Rect(x, y, width, height);
            }
        }

        public void ShowInterstitial(Action<AdStatus> onClosed)
        {
            ShowFullscreen(_interstitial, onClosed);
        }

        public void ShowRewarded(Action<AdStatus> onClosed)
        {
            ShowFullscreen(_rewarded, onClosed);
        }

        private void Awake()
        {
            _bannerUnitId = ChooseUnitId(_androidBannerUnitId, _iosBannerUnitId, TestBannerAndroid, TestBannerIos);
            _bannerRetry = new LoadRetry(this);
            _interstitial = new InterstitialSlot(
                this,
                ChooseUnitId(_androidInterstitialUnitId, _iosInterstitialUnitId, TestInterstitialAndroid,
                    TestInterstitialIos));
            _rewarded = new RewardedSlot(
                this,
                ChooseUnitId(_androidRewardedUnitId, _iosRewardedUnitId, TestRewardedAndroid, TestRewardedIos));
        }

        private void Update()
        {
            if (_bannerRetry.ComesDue())
            {
                LoadBanner();
            }

            _interstitial.RetryIfDue();
            _rewarded.RetryIfDue();
        }

        private void OnDestroy()
        {
            _sdkReady = false;
            _bannerRetry.Stop();

            if (_bannerView != null)
            {
                _bannerView.Destroy();
                _bannerView = null;
            }

            _interstitial.Destroy();
            _rewarded.Destroy();
        }

        private void OnSdkInitialized(InitializationStatus status)
        {
            if (status == null)
            {
                Debug.LogError("[Ads] AdMob failed to initialize; no ad will be shown this session.", this);
                return;
            }

            _sdkReady = true;
            _interstitial.Load();
            _rewarded.Load();

            // Only if the manager has already asked for it.
            LoadBanner();
        }

        private void ShowFullscreen<TAd>(FullscreenSlot<TAd> slot, Action<AdStatus> onClosed) where TAd : class
        {
            // One full screen ad at a time, whatever its type.
            if (_interstitial.IsShowing || _rewarded.IsShowing)
            {
                onClosed(AdStatus.Failed);
                return;
            }

            slot.Show(onClosed);
        }

        private void LoadBanner()
        {
            if (!_sdkReady || !_bannerWanted || string.IsNullOrEmpty(_bannerUnitId) || _bannerLoaded || _bannerLoading ||
                _bannerRetry.Pending)
            {
                return;
            }

            if (_bannerView == null)
            {
                CreateBannerView();
            }

            // After a failed load the same view is asked again, as Google advises, rather than a new one made.
            _bannerLoading = true;
            _bannerView.LoadAd(new AdRequest());
        }

        private void CreateBannerView()
        {
            // Anchored adaptive (https://developers.google.com/admob/unity/banner/anchored-adaptive): as wide as the
            // screen's safe area, in density-independent pixels, and as tall as AdMob picks for that width.
            int width = MobileAds.Utils.GetDeviceSafeWidth();
            AdSize size = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(width);

            _bannerView = new BannerView(_bannerUnitId, size, _bannerPosition);
            _bannerView.OnBannerAdLoaded += () => OnMainThread(OnBannerLoaded);
            _bannerView.OnBannerAdLoadFailed += error => OnMainThread(() => OnBannerLoadFailed(error));
        }

        private void OnBannerLoaded()
        {
            _bannerLoading = false;
            _bannerLoaded = true;
            _bannerRetry.Succeeded();
            _bannerSizeInPixels = new Vector2(_bannerView.GetWidthInPixels(), _bannerView.GetHeightInPixels());

            // Raised on every refresh too. The manager may have taken the banner away while it was loading.
            if (_bannerWanted)
            {
                _bannerView.Show();
            }
            else
            {
                _bannerView.Hide();
            }
        }

        private void OnBannerLoadFailed(LoadAdError error)
        {
            if (!_bannerLoading)
            {
                // A refresh AdMob started itself: the banner keeps the ad it has, and the next refresh tries again.
                Debug.LogWarning("[Ads] AdMob could not refresh the banner (" + Describe(error) + ").", this);
                return;
            }

            _bannerLoading = false;
            float delay = _bannerRetry.Schedule();
            Debug.LogWarning(
                "[Ads] AdMob could not load the banner (" + Describe(error) + "); next attempt in " + delay + " s.",
                this);
        }

        private bool HasUnitId(string unitId, string ads)
        {
            if (!string.IsNullOrEmpty(unitId))
            {
                return true;
            }

            Debug.LogError(
                "[Ads] AdMob has no ad unit ID for " + ads + " on this platform, so none will be shown. Set it on '" +
                name + "'.", this);
            return false;
        }

        private string ChooseUnitId(string androidUnitId, string iosUnitId, string testAndroid, string testIos)
        {
#if UNITY_IOS
            string unitId = iosUnitId;
            string testUnitId = testIos;
#else
            // Android, and the editor whatever its build target.
            string unitId = androidUnitId;
            string testUnitId = testAndroid;
#endif
            if (_testAdsInDevelopmentBuilds && Debug.isDebugBuild)
            {
                return testUnitId;
            }

            return unitId == null ? null : unitId.Trim();
        }

        private void OnMainThread(Action action)
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                // The SDK may answer after this object has gone with the ProjectContext, as the game quits.
                if (this != null)
                {
                    action();
                }
            });
        }

        private static string Describe(AdError error)
        {
            return error != null
                ? "code " + error.GetCode() + ": " + error.GetMessage()
                : "no ad and no error";
        }

        private static string OrNone(string unitId)
        {
            return string.IsNullOrEmpty(unitId) ? "none" : unitId;
        }

        /// <summary>
        /// One type of full screen ad: the ad kept loaded, the one on screen with the manager's callback, and the retries
        /// after a failed load. What the SDK does differently for each type is left to the subclass.
        /// </summary>
        /// <typeparam name="TAd">The SDK's class for the type: <see cref="InterstitialAd"/>, <see cref="RewardedAd"/>.</typeparam>
        private abstract class FullscreenSlot<TAd> where TAd : class
        {
            private readonly string _format;
            private readonly LoadRetry _retry;
            private bool _loading;

            // Loaded and not shown yet; null while there is none.
            private TAd _loaded;
            private DateTime _loadedAtUtc;

            // On screen, with the manager's callback; both null otherwise.
            private TAd _showing;
            private Action<AdStatus> _onClosed;

            /// <param name="owner">The provider, which brings the SDK's answers to the main thread.</param>
            /// <param name="format">The type as the log names it: <c>interstitial</c>, <c>rewarded ad</c>.</param>
            /// <param name="unitId">The ad unit to load from; null or empty when the platform has none.</param>
            protected FullscreenSlot(AdmobAdsProvider owner, string format, string unitId)
            {
                Owner = owner;
                UnitId = unitId;
                _format = format;
                _retry = new LoadRetry(owner);
            }

            public string UnitId { get; }

            public bool IsShowing
            {
                get { return _showing != null; }
            }

            protected AdmobAdsProvider Owner { get; }

            /// <summary>Whether an ad is loaded and can be shown now. One that has expired is reloaded.</summary>
            public bool HasAd()
            {
                if (_loaded == null)
                {
                    return false;
                }

                if (CanShowAd(_loaded) && DateTime.UtcNow - _loadedAtUtc < AdLifetime)
                {
                    return true;
                }

                // Expired, or no longer showable: a fresh one is loaded in its place.
                DestroyAd(_loaded);
                _loaded = null;
                Load();
                return false;
            }

            /// <summary>Loads the next ad, unless one is loaded, loading, on screen or waiting to be tried again.</summary>
            public void Load()
            {
                if (!Owner._sdkReady || string.IsNullOrEmpty(UnitId) || _loading || _retry.Pending || _loaded != null ||
                    _showing != null)
                {
                    return;
                }

                _loading = true;
                LoadAd(UnitId, (ad, error) => Owner.OnMainThread(() => OnLoaded(ad, error)));
            }

            public void Show(Action<AdStatus> onClosed)
            {
                if (!HasAd())
                {
                    onClosed(AdStatus.NotReady);
                    return;
                }

                TAd ad = _loaded;
                _loaded = null;
                _showing = ad;
                _onClosed = onClosed;

                try
                {
                    ShowAd(ad);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, Owner);
                    EndShow(ad, AdStatus.Failed);
                }
            }

            public void RetryIfDue()
            {
                if (_retry.ComesDue())
                {
                    Load();
                }
            }

            /// <summary>Lets go of every ad, as the provider is destroyed.</summary>
            public void Destroy()
            {
                _retry.Stop();

                if (_loaded != null)
                {
                    DestroyAd(_loaded);
                    _loaded = null;
                }

                if (_showing != null)
                {
                    // The manager is still waiting for this ad, and the SDK's answer will no longer reach the provider.
                    EndShow(_showing, AdStatus.Failed);
                }
            }

            protected abstract void LoadAd(string unitId, Action<TAd, LoadAdError> onLoaded);

            protected abstract void ListenTo(TAd ad, Action onClosed, Action<AdError> onShowFailed);

            protected abstract bool CanShowAd(TAd ad);

            protected abstract void ShowAd(TAd ad);

            protected abstract void DestroyAd(TAd ad);

            /// <summary>What the manager is told when the ad on screen has closed.</summary>
            protected virtual AdStatus StatusOnClose()
            {
                return AdStatus.Completed;
            }

            private void OnLoaded(TAd ad, LoadAdError error)
            {
                _loading = false;

                if (error != null || ad == null)
                {
                    float delay = _retry.Schedule();
                    Debug.LogWarning(
                        "[Ads] AdMob could not load the " + _format + " (" + Describe(error) + "); next attempt in " +
                        delay + " s.", Owner);
                    return;
                }

                _retry.Succeeded();
                ListenTo(
                    ad,
                    () => Owner.OnMainThread(() => EndShow(ad, StatusOnClose())),
                    showError => Owner.OnMainThread(() => OnShowFailed(ad, showError)));
                _loaded = ad;
                _loadedAtUtc = DateTime.UtcNow;
            }

            private void OnShowFailed(TAd ad, AdError error)
            {
                Debug.LogWarning("[Ads] AdMob could not show the " + _format + " (" + Describe(error) + ").", Owner);
                EndShow(ad, AdStatus.Failed);
            }

            private void EndShow(TAd ad, AdStatus status)
            {
                // One answer per ad: a second one, or one from an ad that is no longer on screen, is ignored.
                if (ad != _showing)
                {
                    return;
                }

                Action<AdStatus> onClosed = _onClosed;
                _showing = null;
                _onClosed = null;

                // A full screen ad is good for one show only.
                DestroyAd(ad);
                Load();
                onClosed(status);
            }
        }

        /// <summary>Interstitials: <see cref="AdStatus.Completed"/> however the ad was closed.</summary>
        private sealed class InterstitialSlot : FullscreenSlot<InterstitialAd>
        {
            public InterstitialSlot(AdmobAdsProvider owner, string unitId)
                : base(owner, "interstitial", unitId)
            {
            }

            protected override void LoadAd(string unitId, Action<InterstitialAd, LoadAdError> onLoaded)
            {
                InterstitialAd.Load(unitId, new AdRequest(), onLoaded);
            }

            protected override void ListenTo(InterstitialAd ad, Action onClosed, Action<AdError> onShowFailed)
            {
                ad.OnAdFullScreenContentClosed += onClosed;
                ad.OnAdFullScreenContentFailed += onShowFailed;
            }

            protected override bool CanShowAd(InterstitialAd ad)
            {
                return ad.CanShowAd();
            }

            protected override void ShowAd(InterstitialAd ad)
            {
                ad.Show();
            }

            protected override void DestroyAd(InterstitialAd ad)
            {
                ad.Destroy();
            }
        }

        /// <summary>
        /// Rewarded ads: <see cref="AdStatus.Completed"/> once the SDK has granted the reward, <see cref="AdStatus.Cancelled"/>
        /// when the ad was closed before.
        /// </summary>
        private sealed class RewardedSlot : FullscreenSlot<RewardedAd>
        {
            // Whether the ad on screen has granted its reward.
            private bool _rewardEarned;

            public RewardedSlot(AdmobAdsProvider owner, string unitId)
                : base(owner, "rewarded ad", unitId)
            {
            }

            protected override void LoadAd(string unitId, Action<RewardedAd, LoadAdError> onLoaded)
            {
                RewardedAd.Load(unitId, new AdRequest(), onLoaded);
            }

            protected override void ListenTo(RewardedAd ad, Action onClosed, Action<AdError> onShowFailed)
            {
                ad.OnAdFullScreenContentClosed += onClosed;
                ad.OnAdFullScreenContentFailed += onShowFailed;
            }

            protected override bool CanShowAd(RewardedAd ad)
            {
                return ad.CanShowAd();
            }

            protected override void ShowAd(RewardedAd ad)
            {
                _rewardEarned = false;

                // The reward is granted while the ad is still up, before its close is reported. Both come to the main
                // thread through the same queue, so the reward is counted by the time the close is answered.
                ad.Show(reward => Owner.OnMainThread(() => _rewardEarned = true));
            }

            protected override void DestroyAd(RewardedAd ad)
            {
                ad.Destroy();
            }

            protected override AdStatus StatusOnClose()
            {
                // Closed before the end: nothing earned.
                return _rewardEarned ? AdStatus.Completed : AdStatus.Cancelled;
            }
        }

        /// <summary>
        /// When a load that failed is tried again: after <c>Retry Delay</c>, doubled with every failure in a row, up to
        /// <c>Max Retry Delay</c>.
        /// </summary>
        private sealed class LoadRetry
        {
            private readonly AdmobAdsProvider _owner;
            private int _failures;
            private double _at;

            public LoadRetry(AdmobAdsProvider owner)
            {
                _owner = owner;
            }

            /// <summary>Whether a wait is running; nothing is loaded until it is over.</summary>
            public bool Pending { get; private set; }

            /// <summary>Counts a failure and starts the wait. Returns its length in seconds.</summary>
            public float Schedule()
            {
                _failures++;
                float delay = Mathf.Min(
                    _owner._retryDelay * Mathf.Pow(2f, _failures - 1),
                    Mathf.Max(_owner._retryDelay, _owner._maxRetryDelay));

                // Real time: the manager holds Time.timeScale at zero while an ad is up, and a pause menu may too.
                _at = Time.realtimeSinceStartupAsDouble + delay;
                Pending = true;
                return delay;
            }

            /// <summary>True once, when the wait is over: the caller loads again then.</summary>
            public bool ComesDue()
            {
                if (!Pending || Time.realtimeSinceStartupAsDouble < _at)
                {
                    return false;
                }

                Pending = false;
                return true;
            }

            /// <summary>A load went through: the next failure waits the shortest time again.</summary>
            public void Succeeded()
            {
                _failures = 0;
            }

            /// <summary>Drops the wait without loading.</summary>
            public void Stop()
            {
                Pending = false;
            }
        }
    }
}
