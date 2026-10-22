using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace Game.Ads
{
    /// <summary>
    /// A stand-in ad network for development: a black screen saying "Interstitial Ad" or "Rewarded Ad" that closes
    /// itself when its timer runs out, and a black strip for the banner. Nothing is loaded from anywhere.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lives on a GameObject of its own: the <see cref="AdsManagerInstaller"/> creates it from
    /// <c>Prefabs/BaseAdsProvider</c> under the <c>ProjectContext</c>, so it outlives scene loads the way a real
    /// network's overlay does.
    /// </para>
    /// <para>
    /// Internal to the module like every provider: game code shows ads through <see cref="IAdsManagerService"/> and
    /// never sees this class. The timer runs on unscaled time, so it counts down while the manager holds the game
    /// paused.
    /// </para>
    /// </remarks>
    internal sealed class BaseAdsProvider : MonoBehaviour, IAdsProvider
    {
        /// <summary>What the stub pretends the network does, to take the game's callbacks through every branch.</summary>
        internal enum Simulation
        {
            /// <summary>Every ad is shown for its full time; a rewarded ad earns its reward.</summary>
            Success = 0,

            /// <summary>Ads close halfway: an interstitial still counts, a rewarded ad earns nothing.</summary>
            ClosedEarly = 1,

            /// <summary>No ad is ever ready.</summary>
            NotReady = 2,

            /// <summary>Ads look ready but fail to show.</summary>
            Failed = 3,
        }

        private const string InterstitialTitle = "Interstitial Ad";
        private const string RewardedTitle = "Rewarded Ad";

        [Header("Timer")]
        [SerializeField]
        [Min(0f)]
        [Tooltip("Seconds an interstitial stays on screen.")]
        private float _interstitialDuration = 2f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Seconds a rewarded ad stays on screen before its reward is earned.")]
        private float _rewardedDuration = 2f;

        [Header("Simulation")]
        [SerializeField]
        [Tooltip("What the stub does when asked for a full screen ad. Success: shown for the full time. Closed Early: " +
                 "closes halfway — an interstitial still counts, a rewarded ad earns nothing. Not Ready: nothing to " +
                 "show. Failed: fails to show.")]
        private Simulation _simulation = Simulation.Success;

        [Header("View")]
        [SerializeField]
        [Tooltip("The banner strip. Off until the manager shows it.")]
        private GameObject _banner;

        [SerializeField]
        [Tooltip("The black full screen panel. Off until an ad is shown.")]
        private GameObject _fullscreen;

        [SerializeField]
        [Tooltip("Says which ad is on screen.")]
        private TMP_Text _title;

        [SerializeField]
        [Tooltip("Seconds left.")]
        private TMP_Text _timer;

        // The manager's callback while a full screen ad is up; null otherwise.
        private Action<AdStatus> _onClosed;

        public string Network
        {
            // Nothing comes from a network; analytics still has to tell these apart from real impressions.
            get { return "stub"; }
        }

        public bool IsAdAvailable(AdType type)
        {
            // The strip is always there to show; the simulation is about full screen ads only.
            return type == AdType.Banner || _simulation != Simulation.NotReady;
        }

        public void Initialize()
        {
            if (Debug.isDebugBuild)
            {
                Debug.Log("[Ads] BaseAdsProvider is a stub: its ads are black screens with a timer, nothing is " +
                          "loaded from a network.", this);
            }
        }

        public void ShowBanner()
        {
            SetActive(_banner, true);
        }

        public void HideBanner()
        {
            SetActive(_banner, false);
        }

        public void ShowInterstitial(Action<AdStatus> onClosed)
        {
            Show(InterstitialTitle, _interstitialDuration, false, onClosed);
        }

        public void ShowRewarded(Action<AdStatus> onClosed)
        {
            Show(RewardedTitle, _rewardedDuration, true, onClosed);
        }

        private void Awake()
        {
            // The prefab may have been saved with the strip left on while it was being edited.
            SetActive(_banner, false);
        }

        private void OnEnable()
        {
            // Nothing full screen is up until the manager asks — also after this object was switched off mid-ad.
            SetActive(_fullscreen, false);
        }

        private void OnDisable()
        {
            if (_onClosed == null)
            {
                return;
            }

            // The countdown stops with the GameObject, and the manager is still waiting for this ad to close. The panel
            // is left alone: switching a child while the hierarchy is being switched off is an error, OnEnable hides it.
            Answer(AdStatus.Failed);
        }

        private void Show(string title, float duration, bool rewarded, Action<AdStatus> onClosed)
        {
            if (_simulation == Simulation.NotReady)
            {
                onClosed(AdStatus.NotReady);
                return;
            }

            if (_simulation == Simulation.Failed || _onClosed != null || !isActiveAndEnabled)
            {
                onClosed(AdStatus.Failed);
                return;
            }

            _onClosed = onClosed;
            SetText(_title, title);
            SetActive(_fullscreen, true);
            StartCoroutine(CountDown(duration, rewarded, _simulation == Simulation.ClosedEarly));
        }

        private IEnumerator CountDown(float duration, bool rewarded, bool closesEarly)
        {
            float shownFor = closesEarly ? duration * 0.5f : duration;

            // Unscaled: the manager holds Time.timeScale at zero while the ad is up.
            for (float elapsed = 0f; elapsed < shownFor; elapsed += Time.unscaledDeltaTime)
            {
                SetText(_timer, Mathf.CeilToInt(duration - elapsed).ToString(CultureInfo.InvariantCulture));
                yield return null;
            }

            SetActive(_fullscreen, false);
            Answer(closesEarly && rewarded ? AdStatus.Cancelled : AdStatus.Completed);
        }

        private void Answer(AdStatus status)
        {
            Action<AdStatus> onClosed = _onClosed;
            _onClosed = null;
            onClosed(status);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }

        private static void SetText(TMP_Text label, string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }
    }
}
