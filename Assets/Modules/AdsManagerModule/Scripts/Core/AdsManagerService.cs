using System;
using System.Globalization;
using UnityEngine;
using Zenject;

namespace Game.Ads
{
    /// <summary>
    /// The ads manager behind <see cref="IAdsManagerService"/>: decides whether an ad may be shown, has the provider
    /// show it and reports how it went.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The rules, checked in this order on every request: one full screen ad at a time; the ad block (banners and
    /// interstitials only); the level the type of ad starts at; the interstitial cooldown; the provider having an ad
    /// ready. The first one that stops the ad is the status the caller gets.
    /// </para>
    /// <para>
    /// With an <see cref="IAdsConsentSource"/> bound, the provider is not started before the player's privacy choices
    /// allow ads, and until then every full screen ad is <see cref="AdStatus.NotReady"/> and the banner stays hidden. The
    /// provider starts, and a requested banner comes up, the moment the source reports that they do.
    /// </para>
    /// <para>
    /// A callback is invoked exactly once — straight away when the ad is not shown, otherwise when it closes — so a flow
    /// waiting on one always goes on. A second answer from the provider is ignored, and a provider that throws counts as
    /// <see cref="AdStatus.Failed"/>. Once the service is disposed, as the game quits, a pending callback is dropped.
    /// <see cref="FullscreenAdFinished"/> is raised with the same result just before each callback.
    /// </para>
    /// <para>
    /// Lives in the <c>ProjectContext</c>: the cooldown and the banner request have to outlive the scene reload that
    /// every death causes. Main thread only.
    /// </para>
    /// </remarks>
    internal sealed class AdsManagerService : IAdsManagerService, IInitializable, ITickable, IDisposable
    {
        /// <summary>How often, in seconds of real time, the ad block and the banner are looked at again.</summary>
        internal const double RefreshInterval = 1;

        // Far enough for any promo, short of what DateTime and TimeSpan can hold.
        private const int MaxAdBlockDays = 36500;

        private readonly IAdsProvider _provider;
        private readonly AdsSettings _settings;
        private readonly IAdsClock _clock;
        private readonly IAdsGamePause _pause;
        private readonly IAdsLevelSource _levels;
        private readonly IAdsConsentSource _consent;
        private readonly AdBlock _adBlock;
        private readonly string _network;

        private FullscreenRequest _fullscreen;
        private double? _cooldownStartedAt;
        private double _nextRefreshAt;
        private bool _providerInitialized;
        private bool _bannerRequested;
        private bool _bannerVisible;
        private bool _levelSourceFailureReported;
        private bool _disposed;

        /// <summary>Creates the manager.</summary>
        /// <param name="provider">The ad network.</param>
        /// <param name="settings">The rules.</param>
        /// <param name="storage">Where the ad block is kept between sessions.</param>
        /// <param name="clock">Real time for the cooldown, the wall clock for the ad block.</param>
        /// <param name="pause">What holds the game still while a full screen ad is up.</param>
        /// <param name="levels">How far the player has got. <c>null</c>: no level limits.</param>
        /// <param name="consent">Whether the player's privacy choices allow ads. <c>null</c>: always.</param>
        public AdsManagerService(
            IAdsProvider provider,
            AdsSettings settings,
            IAdsStorage storage,
            IAdsClock clock,
            IAdsGamePause pause,
            [InjectOptional] IAdsLevelSource levels,
            [InjectOptional] IAdsConsentSource consent = null)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (storage == null)
            {
                throw new ArgumentNullException(nameof(storage));
            }

            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }

            if (pause == null)
            {
                throw new ArgumentNullException(nameof(pause));
            }

            _provider = provider;
            _settings = settings;
            _clock = clock;
            _pause = pause;
            _levels = levels;
            _consent = consent;
            _adBlock = new AdBlock(storage, clock);
            _network = ReadNetwork(provider);
        }

        /// <inheritdoc />
        public event Action AdBlockChanged;

        /// <inheritdoc />
        public event Action<AdResult> FullscreenAdFinished;

        /// <inheritdoc />
        public bool IsBannerVisible
        {
            get { return _bannerVisible; }
        }

        /// <inheritdoc />
        public bool IsShowingFullscreenAd
        {
            get { return _fullscreen != null; }
        }

        /// <inheritdoc />
        public bool IsAdBlockActive
        {
            get { return _adBlock.IsActive; }
        }

        /// <inheritdoc />
        public bool IsAdBlockPermanent
        {
            get { return _adBlock.IsPermanent; }
        }

        /// <inheritdoc />
        public DateTime? AdBlockEndsAtUtc
        {
            get { return _adBlock.EndsAtUtc; }
        }

        /// <summary>
        /// Brings the provider up — or, while the player's privacy choices do not allow ads yet, waits for them. Called by
        /// the context once everything is bound.
        /// </summary>
        public void Initialize()
        {
            if (_levels == null)
            {
                Debug.LogWarning(
                    "[Ads] No IAdsLevelSource is bound, so every ad is shown from the first level whatever the config " +
                    "says. Bind one in the ProjectContext.");
            }

            if (_consent != null)
            {
                _consent.Changed += OnConsentChanged;
                if (!_consent.CanRequestAds)
                {
                    Log("The ad network waits for the player's privacy choices.");
                }
            }

            EnsureProviderInitialized();
            _nextRefreshAt = _clock.RealtimeSinceStartup + RefreshInterval;
        }

        /// <summary>
        /// Once a second: forgets an ad block that has run out and brings the banner in line with the rules — it comes
        /// back when the block ends and appears when the player reaches its level.
        /// </summary>
        public void Tick()
        {
            if (_disposed)
            {
                return;
            }

            double now = _clock.RealtimeSinceStartup;
            if (now < _nextRefreshAt)
            {
                return;
            }

            _nextRefreshAt = now + RefreshInterval;

            if (_adBlock.ForgetExpired())
            {
                Log("The ad block has run out.");
                RaiseAdBlockChanged();
            }

            UpdateBanner();
        }

        /// <summary>Takes the game out of an ad that is still up, so time and sound are not left at zero.</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_consent != null)
            {
                _consent.Changed -= OnConsentChanged;
            }

            if (_fullscreen != null)
            {
                _fullscreen.Closed = true;
                _fullscreen = null;
                ResumeGame();
            }
        }

        /// <inheritdoc />
        public void ShowBanner()
        {
            if (_disposed)
            {
                return;
            }

            if (!_bannerRequested)
            {
                _bannerRequested = true;
                AdStatus? blocked = CheckRules(AdType.Banner);
                if (blocked.HasValue)
                {
                    LogHeldBack("Banner asked for, held back", AdType.Banner, blocked.Value);
                }
            }

            UpdateBanner();
        }

        /// <inheritdoc />
        public void HideBanner()
        {
            if (_disposed)
            {
                return;
            }

            _bannerRequested = false;
            UpdateBanner();
        }

        /// <inheritdoc />
        public bool IsAdAvailable(AdType type)
        {
            if (_disposed)
            {
                return false;
            }

            return type == AdType.Banner
                ? !CheckRules(type).HasValue && ConsentAllowsAds()
                : !CheckFullscreen(type).HasValue;
        }

        /// <inheritdoc />
        public void ShowInterstitial(Action<AdResult> onFinished = null, string placement = null)
        {
            ShowFullscreen(AdType.Interstitial, placement, onFinished);
        }

        /// <inheritdoc />
        public void ShowRewarded(Action<AdResult> onFinished, string placement = null)
        {
            ShowFullscreen(AdType.Rewarded, placement, onFinished);
        }

        /// <inheritdoc />
        public void AddAdBlock(int days)
        {
            if (days <= 0)
            {
                Debug.LogWarning("[Ads] An ad block of " + days + " days was ignored: the days have to be positive.");
                return;
            }

            AddAdBlock(TimeSpan.FromDays(Math.Min(days, MaxAdBlockDays)));
        }

        /// <inheritdoc />
        public void AddAdBlock(TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero)
            {
                Debug.LogWarning("[Ads] An ad block of " + duration + " was ignored: it has to be longer than zero.");
                return;
            }

            _adBlock.Add(duration);
            Log("Ad block added: " + duration + ", it now ends " + FormatUtc(_adBlock.EndsAtUtc) + ".");
            OnAdBlockChanged();
        }

        /// <inheritdoc />
        public void AddPermanentAdBlock()
        {
            _adBlock.AddPermanent();
            Log("Permanent ad block added.");
            OnAdBlockChanged();
        }

        /// <inheritdoc />
        public void RemoveAdBlock()
        {
            _adBlock.Remove();
            Log("Ad block removed.");
            OnAdBlockChanged();
        }

        private void ShowFullscreen(AdType type, string placement, Action<AdResult> onFinished)
        {
            if (_disposed)
            {
                // Teardown order is not ours to pick; something may still ask while the game quits. Nothing can be shown
                // any more, and calling back into a game that is going away would do more harm than good.
                Debug.LogWarning("[Ads] A " + type + " was asked for after the ads manager was disposed; ignored.");
                return;
            }

            if (string.IsNullOrEmpty(placement))
            {
                placement = type.ToString().ToLowerInvariant();
            }

            AdStatus? blocked = CheckFullscreen(type);
            if (blocked.HasValue)
            {
                LogHeldBack(Label(type, placement) + " not shown", type, blocked.Value);
                Deliver(new AdResult(type, blocked.Value, placement, _network), onFinished);
                return;
            }

            // Everything is in place before the provider is called: it may answer before the call returns.
            FullscreenRequest request = new FullscreenRequest(type, placement, onFinished);
            _fullscreen = request;
            if (_settings.PauseGame)
            {
                _pause.Pause();
            }

            Log("Showing " + Label(type, placement) + ".");

            try
            {
                if (type == AdType.Interstitial)
                {
                    _provider.ShowInterstitial(status => OnClosed(request, status));
                }
                else
                {
                    _provider.ShowRewarded(status => OnClosed(request, status));
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                OnClosed(request, AdStatus.Failed);
            }
        }

        private void OnClosed(FullscreenRequest request, AdStatus reported)
        {
            if (request.Closed)
            {
                if (!_disposed)
                {
                    Debug.LogWarning("[Ads] The provider reported the " + request.Type + " closed a second time; " +
                                     "ignored.");
                }

                return;
            }

            request.Closed = true;
            if (_fullscreen == request)
            {
                _fullscreen = null;
            }

            // Before the callback: whatever the game does next — restart the level, load a scene — happens in a game
            // that is running again.
            ResumeGame();

            AdStatus status = Normalize(request.Type, reported);
            if (StartsCooldown(request.Type, status))
            {
                _cooldownStartedAt = _clock.RealtimeSinceStartup;
            }

            Log(Label(request.Type, request.Placement) + " closed: " + status + ".");
            Deliver(new AdResult(request.Type, status, request.Placement, _network), request.Callback);
        }

        private AdStatus? CheckFullscreen(AdType type)
        {
            if (_fullscreen != null)
            {
                return AdStatus.Busy;
            }

            AdStatus? blocked = CheckRules(type);
            if (blocked.HasValue)
            {
                return blocked;
            }

            return IsProviderReady(type) ? (AdStatus?)null : AdStatus.NotReady;
        }

        private AdStatus? CheckRules(AdType type)
        {
            if (type != AdType.Rewarded && _adBlock.IsActive)
            {
                return AdStatus.AdBlock;
            }

            if (ReadLevel() < _settings.FromLevel(type))
            {
                return AdStatus.LevelLocked;
            }

            if (type == AdType.Interstitial && CooldownLeft() > 0)
            {
                return AdStatus.Cooldown;
            }

            return null;
        }

        private bool IsProviderReady(AdType type)
        {
            if (!ConsentAllowsAds())
            {
                return false;
            }

            EnsureProviderInitialized();

            try
            {
                return _provider.IsAdAvailable(type);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }
        }

        private void UpdateBanner()
        {
            AdStatus? blocked = _bannerRequested ? CheckRules(AdType.Banner) : null;
            bool show = _bannerRequested && !blocked.HasValue && ConsentAllowsAds();
            if (show == _bannerVisible)
            {
                return;
            }

            EnsureProviderInitialized();
            _bannerVisible = show;

            try
            {
                if (show)
                {
                    _provider.ShowBanner();
                }
                else
                {
                    _provider.HideBanner();
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            if (show)
            {
                Log("Banner shown.");
            }
            else if (blocked.HasValue)
            {
                LogHeldBack("Banner hidden", AdType.Banner, blocked.Value);
            }
            else
            {
                Log("Banner hidden.");
            }
        }

        private void OnAdBlockChanged()
        {
            // An ad block takes the banner away at once rather than on the next refresh.
            UpdateBanner();
            RaiseAdBlockChanged();
        }

        private void RaiseAdBlockChanged()
        {
            Action handlers = AdBlockChanged;
            if (handlers == null)
            {
                return;
            }

            // One listener that throws must not keep the news from the others.
            Delegate[] list = handlers.GetInvocationList();
            for (int i = 0; i < list.Length; i++)
            {
                try
                {
                    ((Action)list[i])();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        /// <summary>
        /// Announces the result, then hands it to the caller. Announced first: the caller often moves on at once —
        /// reloads the level, leaves for the menu — and what listens should hear of the ad in the game it was shown in.
        /// </summary>
        private void Deliver(AdResult result, Action<AdResult> callback)
        {
            RaiseFullscreenAdFinished(result);
            if (callback == null)
            {
                return;
            }

            // The game's code runs here, often inside the provider's own callback; an exception must not break either.
            try
            {
                callback(result);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void RaiseFullscreenAdFinished(AdResult result)
        {
            Action<AdResult> handlers = FullscreenAdFinished;
            if (handlers == null)
            {
                return;
            }

            // Like the ad block news: one listener that throws must not keep the result from the others or the caller.
            Delegate[] list = handlers.GetInvocationList();
            for (int i = 0; i < list.Length; i++)
            {
                try
                {
                    ((Action<AdResult>)list[i])(result);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        private int ReadLevel()
        {
            if (_levels == null)
            {
                return int.MaxValue;
            }

            try
            {
                return _levels.ReachedLevel;
            }
            catch (Exception exception)
            {
                if (!_levelSourceFailureReported)
                {
                    _levelSourceFailureReported = true;
                    Debug.LogException(exception);
                }

                // Not knowing how far the player has got, hold every level limited ad back rather than show one early.
                return 0;
            }
        }

        private double CooldownLeft()
        {
            if (!_cooldownStartedAt.HasValue)
            {
                return 0;
            }

            double elapsed = _clock.RealtimeSinceStartup - _cooldownStartedAt.Value;
            return Math.Max(0, _settings.InterstitialCooldownSeconds - elapsed);
        }

        private bool StartsCooldown(AdType type, AdStatus status)
        {
            bool wasOnScreen = status == AdStatus.Completed || status == AdStatus.Cancelled;
            if (!wasOnScreen)
            {
                return false;
            }

            return type == AdType.Interstitial || _settings.RewardedRestartsInterstitialCooldown;
        }

        private void EnsureProviderInitialized()
        {
            if (_providerInitialized || !ConsentAllowsAds())
            {
                return;
            }

            _providerInitialized = true;
            try
            {
                _provider.Initialize();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private bool ConsentAllowsAds()
        {
            return _consent == null || _consent.CanRequestAds;
        }

        private void OnConsentChanged()
        {
            if (_disposed)
            {
                return;
            }

            if (!_providerInitialized && ConsentAllowsAds())
            {
                Log("The player's privacy choices allow ads: starting the ad network.");
            }

            EnsureProviderInitialized();
            UpdateBanner();
        }

        private void ResumeGame()
        {
            if (_settings.PauseGame)
            {
                _pause.Resume();
            }
        }

        private void Log(string message)
        {
            if (_settings.LogRequests)
            {
                Debug.Log("[Ads] " + message);
            }
        }

        private void LogHeldBack(string what, AdType type, AdStatus status)
        {
            // Checked first: the description asks the level source again, which is not worth it for a log nobody sees.
            if (_settings.LogRequests)
            {
                Debug.Log("[Ads] " + what + ": " + Describe(type, status) + ".");
            }
        }

        private string Describe(AdType type, AdStatus status)
        {
            switch (status)
            {
                case AdStatus.AdBlock:
                    return _adBlock.IsPermanent
                        ? "permanent ad block"
                        : "ad block until " + FormatUtc(_adBlock.EndsAtUtc);
                case AdStatus.LevelLocked:
                    return "the player has reached level " + ReadLevel() + ", " + type + " starts at level " +
                           _settings.FromLevel(type);
                case AdStatus.Cooldown:
                    return "cooldown, " + Math.Ceiling(CooldownLeft()).ToString(CultureInfo.InvariantCulture) +
                           " s left";
                case AdStatus.NotReady:
                    return "the network has no ad ready";
                case AdStatus.Busy:
                    return "another full screen ad is on screen";
                default:
                    return status.ToString();
            }
        }

        /// <summary>
        /// What the provider said, in the terms the game gets. An interstitial closed early has still been seen; the
        /// statuses that belong to the rules are the manager's to give, so a provider using one is taken to have failed.
        /// </summary>
        private static AdStatus Normalize(AdType type, AdStatus reported)
        {
            switch (reported)
            {
                case AdStatus.Completed:
                case AdStatus.NotReady:
                case AdStatus.Failed:
                    return reported;
                case AdStatus.Cancelled:
                    return type == AdType.Interstitial ? AdStatus.Completed : AdStatus.Cancelled;
                default:
                    return AdStatus.Failed;
            }
        }

        /// <summary>The ad as the log names it: <c>Interstitial (level_fail)</c>.</summary>
        private static string Label(AdType type, string placement)
        {
            return type + " (" + placement + ")";
        }

        /// <summary>
        /// The provider's name for analytics, read once. A provider that fails to name itself is still a network that
        /// shows ads, so it is reported as <c>unknown</c> rather than breaking the manager.
        /// </summary>
        private static string ReadNetwork(IAdsProvider provider)
        {
            string network = null;
            try
            {
                network = provider.Network;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            return string.IsNullOrEmpty(network) ? "unknown" : network;
        }

        private static string FormatUtc(DateTime? moment)
        {
            return moment.HasValue
                ? moment.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC"
                : "never";
        }

        /// <summary>One full screen ad from the request until it closes.</summary>
        private sealed class FullscreenRequest
        {
            public FullscreenRequest(AdType type, string placement, Action<AdResult> callback)
            {
                Type = type;
                Placement = placement;
                Callback = callback;
            }

            public AdType Type { get; }

            public string Placement { get; }

            public Action<AdResult> Callback { get; }

            public bool Closed { get; set; }
        }
    }
}
