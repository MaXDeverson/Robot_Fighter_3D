using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEngine;

namespace Game.Ads.AdmobImplementation
{
    /// <summary>
    /// AdMob (the Google Mobile Ads SDK) as the ads manager's network. Interstitials only: a banner or a rewarded ad is
    /// never available.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lives on the root of <c>Prefabs/AdmobAdsProvider</c>, where its ad unit IDs are set in the Inspector. The
    /// <c>AdsManagerInstaller</c> creates it under the <c>ProjectContext</c> as the manager's <see cref="IAdsProvider"/>
    /// when its <b>Provider</b> is <c>Network</c> and this prefab is its <b>Network Provider Prefab</b>.
    /// </para>
    /// <para>
    /// One interstitial is kept loaded. The next one is loaded as soon as the previous one has closed or failed to show;
    /// a load that fails is tried again later, after a wait that doubles with every failure in a row. The SDK answers
    /// from its own thread, so every answer is brought back to the main thread before it is acted on.
    /// </para>
    /// </remarks>
    internal sealed class AdmobAdsProvider : MonoBehaviour, IAdsProvider
    {
        // Google's sample ad units, which always serve test ads: https://developers.google.com/admob/unity/test-ads
        private const string TestInterstitialAndroid = "ca-app-pub-3940256099942544/1033173712";
        private const string TestInterstitialIos = "ca-app-pub-3940256099942544/4411468910";

        // AdMob lets a loaded interstitial expire after an hour; one kept a little shorter is thrown away and reloaded.
        private static readonly TimeSpan AdLifetime = TimeSpan.FromMinutes(55);

        [Header("Interstitial ad units")]
        [SerializeField]
        [Tooltip("The interstitial ad unit ID for Android, from the AdMob console: ca-app-pub-XXXXXXXXXXXXXXXX/NNNNNNNNNN.")]
        private string _androidInterstitialUnitId;

        [SerializeField]
        [Tooltip("The interstitial ad unit ID for iOS, from the AdMob console.")]
        private string _iosInterstitialUnitId;

        [Header("Testing")]
        [SerializeField]
        [Tooltip("In the editor and in development builds, load Google's test ad unit instead of the IDs above. Clicks " +
                 "on your own live ads can get the AdMob account suspended.")]
        private bool _testAdsInDevelopmentBuilds = true;

        [Header("Loading")]
        [SerializeField]
        [Min(1f)]
        [Tooltip("Seconds before the next attempt after an interstitial failed to load. Doubles with every failure in " +
                 "a row.")]
        private float _retryDelay = 5f;

        [SerializeField]
        [Min(1f)]
        [Tooltip("The longest wait, in seconds, between two attempts to load.")]
        private float _maxRetryDelay = 120f;

        private string _unitId;
        private bool _sdkReady;
        private bool _loading;
        private int _failedLoads;
        private bool _retryPending;
        private double _retryAt;

        // Loaded and not shown yet; null while there is none.
        private InterstitialAd _loaded;
        private DateTime _loadedAtUtc;

        // On screen, with the manager's callback; both null otherwise.
        private InterstitialAd _showing;
        private Action<AdStatus> _onClosed;

        public string Network
        {
            get { return "admob"; }
        }

        public bool IsAdAvailable(AdType type)
        {
            // Banners and rewarded ads do not come from AdMob.
            return type == AdType.Interstitial && HasInterstitial();
        }

        public void Initialize()
        {
            _unitId = ChooseUnitId();
            if (string.IsNullOrEmpty(_unitId))
            {
                Debug.LogError(
                    "[Ads] AdMob has no interstitial ad unit ID for this platform, so no interstitial will be shown. " +
                    "Set it on '" + name + "'.", this);
                return;
            }

            if (Debug.isDebugBuild)
            {
                Debug.Log("[Ads] AdMob starting, interstitial ad unit " + _unitId + ".", this);
            }

            MobileAds.Initialize(status => OnMainThread(() => OnSdkInitialized(status)));
        }

        public void ShowBanner()
        {
            // Banners do not come from AdMob.
        }

        public void HideBanner()
        {
            // Banners do not come from AdMob.
        }

        public void ShowInterstitial(Action<AdStatus> onClosed)
        {
            if (_onClosed != null)
            {
                onClosed(AdStatus.Failed);
                return;
            }

            if (!HasInterstitial())
            {
                onClosed(AdStatus.NotReady);
                return;
            }

            InterstitialAd ad = _loaded;
            _loaded = null;
            _showing = ad;
            _onClosed = onClosed;

            try
            {
                ad.Show();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                EndShow(ad, AdStatus.Failed);
            }
        }

        public void ShowRewarded(Action<AdStatus> onClosed)
        {
            // The manager never asks, IsAdAvailable says there is none; answered anyway, so no caller waits for ever.
            onClosed(AdStatus.NotReady);
        }

        private void Update()
        {
            // Real time: the manager holds Time.timeScale at zero while an ad is up, and a pause menu may too.
            if (_retryPending && Time.realtimeSinceStartupAsDouble >= _retryAt)
            {
                _retryPending = false;
                LoadInterstitial();
            }
        }

        private void OnDestroy()
        {
            _sdkReady = false;
            _retryPending = false;

            if (_loaded != null)
            {
                _loaded.Destroy();
                _loaded = null;
            }

            if (_showing != null)
            {
                // The manager is still waiting for this ad, and the SDK's answer will no longer reach this object.
                EndShow(_showing, AdStatus.Failed);
            }
        }

        private void OnSdkInitialized(InitializationStatus status)
        {
            if (status == null)
            {
                Debug.LogError("[Ads] AdMob failed to initialize; no interstitial will be shown this session.", this);
                return;
            }

            _sdkReady = true;
            LoadInterstitial();
        }

        private void LoadInterstitial()
        {
            if (!_sdkReady || _loading || _retryPending || _loaded != null || _showing != null)
            {
                return;
            }

            _loading = true;
            InterstitialAd.Load(_unitId, new AdRequest(), (ad, error) => OnMainThread(() => OnLoaded(ad, error)));
        }

        private void OnLoaded(InterstitialAd ad, LoadAdError error)
        {
            _loading = false;

            if (error != null || ad == null)
            {
                _failedLoads++;
                float delay = Mathf.Min(_retryDelay * Mathf.Pow(2f, _failedLoads - 1), Mathf.Max(_retryDelay, _maxRetryDelay));
                Debug.LogWarning(
                    "[Ads] AdMob could not load an interstitial (" + Describe(error) + "); next attempt in " + delay +
                    " s.", this);

                _retryAt = Time.realtimeSinceStartupAsDouble + delay;
                _retryPending = true;
                return;
            }

            _failedLoads = 0;
            ad.OnAdFullScreenContentClosed += () => OnMainThread(() => EndShow(ad, AdStatus.Completed));
            ad.OnAdFullScreenContentFailed += showError => OnMainThread(() => OnShowFailed(ad, showError));
            _loaded = ad;
            _loadedAtUtc = DateTime.UtcNow;
        }

        private void OnShowFailed(InterstitialAd ad, AdError error)
        {
            Debug.LogWarning("[Ads] AdMob could not show the interstitial (" + Describe(error) + ").", this);
            EndShow(ad, AdStatus.Failed);
        }

        private void EndShow(InterstitialAd ad, AdStatus status)
        {
            // One answer per ad: a second one, or one from an ad that is no longer on screen, is ignored.
            if (ad != _showing)
            {
                return;
            }

            Action<AdStatus> onClosed = _onClosed;
            _showing = null;
            _onClosed = null;

            // An interstitial is good for one show only.
            ad.Destroy();
            LoadInterstitial();
            onClosed(status);
        }

        private bool HasInterstitial()
        {
            if (_loaded == null)
            {
                return false;
            }

            if (_loaded.CanShowAd() && DateTime.UtcNow - _loadedAtUtc < AdLifetime)
            {
                return true;
            }

            // Expired, or no longer showable: a fresh one is loaded in its place.
            _loaded.Destroy();
            _loaded = null;
            LoadInterstitial();
            return false;
        }

        private string ChooseUnitId()
        {
#if UNITY_IOS
            string unitId = _iosInterstitialUnitId;
            string testUnitId = TestInterstitialIos;
#else
            // Android, and the editor whatever its build target.
            string unitId = _androidInterstitialUnitId;
            string testUnitId = TestInterstitialAndroid;
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
    }
}
