using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Ads.Tests
{
    /// <summary>
    /// An ad network the test drives by hand: a full screen ad stays up until <see cref="Close"/>, unless it is told to
    /// answer inside the show call. Counts every call.
    /// </summary>
    internal sealed class FakeAdsProvider : IAdsProvider
    {
        public string Network { get; set; } = "fake";

        internal bool InterstitialReady { get; set; } = true;

        internal bool RewardedReady { get; set; } = true;

        /// <summary>When set, every full screen ad answers with this before the show call returns.</summary>
        internal AdStatus? AnswerAtOnce { get; set; }

        internal bool ThrowOnShow { get; set; }

        internal int InitializeCalls { get; private set; }

        internal int InterstitialShows { get; private set; }

        internal int RewardedShows { get; private set; }

        internal int BannerShows { get; private set; }

        internal bool BannerOnScreen { get; private set; }

        /// <summary>
        /// Where the banner is; reported whether it is on screen or not, so the tests see the manager's own check.
        /// </summary>
        internal Rect BannerArea { get; set; }

        internal bool ThrowOnBannerArea { get; set; }

        public Rect BannerScreenArea
        {
            get
            {
                if (ThrowOnBannerArea)
                {
                    throw new InvalidOperationException("The network fell over.");
                }

                return BannerArea;
            }
        }

        /// <summary>The callback of the ad on screen, kept after <see cref="Close"/> to answer twice.</summary>
        internal Action<AdStatus> LastCallback { get; private set; }

        public bool IsAdAvailable(AdType type)
        {
            switch (type)
            {
                case AdType.Interstitial:
                    return InterstitialReady;
                case AdType.Rewarded:
                    return RewardedReady;
                default:
                    return true;
            }
        }

        public void Initialize()
        {
            InitializeCalls++;
        }

        public void ShowBanner()
        {
            BannerShows++;
            BannerOnScreen = true;
        }

        public void HideBanner()
        {
            BannerOnScreen = false;
        }

        public void ShowInterstitial(Action<AdStatus> onClosed)
        {
            InterstitialShows++;
            Show(onClosed);
        }

        public void ShowRewarded(Action<AdStatus> onClosed)
        {
            RewardedShows++;
            Show(onClosed);
        }

        /// <summary>Closes the ad on screen the way the network reports it.</summary>
        internal void Close(AdStatus status)
        {
            LastCallback(status);
        }

        private void Show(Action<AdStatus> onClosed)
        {
            if (ThrowOnShow)
            {
                throw new InvalidOperationException("The network fell over.");
            }

            LastCallback = onClosed;
            if (AnswerAtOnce.HasValue)
            {
                onClosed(AnswerAtOnce.Value);
            }
        }
    }

    /// <summary>Both clocks, moved on by the test.</summary>
    internal sealed class FakeAdsClock : IAdsClock
    {
        public double RealtimeSinceStartup { get; set; } = 100;

        public DateTime UtcNow { get; set; } = new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc);

        /// <summary>Time passes on both clocks, as it does while the game runs.</summary>
        internal void Advance(TimeSpan time)
        {
            RealtimeSinceStartup += time.TotalSeconds;
            UtcNow += time;
        }

        internal void AdvanceSeconds(double seconds)
        {
            Advance(TimeSpan.FromSeconds(seconds));
        }
    }

    /// <summary>Storage in a dictionary, shared between the services of one test to stand for a restart.</summary>
    internal sealed class MemoryAdsStorage : IAdsStorage
    {
        private readonly Dictionary<string, int> _ints = new Dictionary<string, int>();
        private readonly Dictionary<string, string> _strings = new Dictionary<string, string>();

        internal int SaveCalls { get; private set; }

        internal bool Has(string key)
        {
            return _ints.ContainsKey(key) || _strings.ContainsKey(key);
        }

        public int GetInt(string key, int fallback = 0)
        {
            int value;
            return _ints.TryGetValue(key, out value) ? value : fallback;
        }

        public void SetInt(string key, int value)
        {
            _ints[key] = value;
        }

        public string GetString(string key, string fallback = null)
        {
            string value;
            return _strings.TryGetValue(key, out value) ? value : fallback;
        }

        public void SetString(string key, string value)
        {
            _strings[key] = value;
        }

        public void Delete(string key)
        {
            _ints.Remove(key);
            _strings.Remove(key);
        }

        public void Save()
        {
            SaveCalls++;
        }
    }

    internal sealed class FakeLevelSource : IAdsLevelSource
    {
        internal int Level { get; set; } = 100;

        internal bool Throw { get; set; }

        public int ReachedLevel
        {
            get
            {
                if (Throw)
                {
                    throw new InvalidOperationException("The progress is not loaded.");
                }

                return Level;
            }
        }
    }

    /// <summary>The player's privacy choices, changed by the test with <see cref="Set"/>.</summary>
    internal sealed class FakeConsentSource : IAdsConsentSource
    {
        public bool CanRequestAds { get; private set; }

        public event Action Changed;

        internal int Subscribers
        {
            get { return Changed == null ? 0 : Changed.GetInvocationList().Length; }
        }

        internal void Set(bool canRequestAds)
        {
            CanRequestAds = canRequestAds;
            Changed?.Invoke();
        }
    }

    internal sealed class FakeGamePause : IAdsGamePause
    {
        internal bool IsPaused { get; private set; }

        internal int PauseCalls { get; private set; }

        public void Pause()
        {
            PauseCalls++;
            IsPaused = true;
        }

        public void Resume()
        {
            IsPaused = false;
        }
    }
}
