using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Game.Privacy
{
    /// <summary>
    /// <see cref="IPrivacyService"/> over a consent platform: the platform keeps the answer and shows the form, the
    /// service adds the timeout, shares one request among the callers, guards the callbacks and reports changes.
    /// </summary>
    /// <remarks>
    /// Ticked by the context for the timeout, which stands still while the platform's form is on the screen. At most one
    /// gather request is out at a time: a call after a timeout, while the platform is still busy, waits for that same
    /// request with a new deadline instead of starting another.
    /// </remarks>
    internal sealed class PrivacyService : IPrivacyService, ITickable
    {
        private readonly PrivacySettings _settings;
        private readonly IPrivacyConsentProvider _provider;
        private readonly IUrlOpener _urls;
        private readonly IPrivacyClock _clock;

        // Callers of GatherConsent that have not been answered yet, and when they stop waiting.
        private readonly List<Action> _waiting = new List<Action>();
        private double _deadline;

        private bool _requestOut;
        private bool _gathered;
        private bool _optionsShowing;
        private ConsentSnapshot _last;

        public PrivacyService(
            PrivacySettings settings,
            IPrivacyConsentProvider provider,
            IUrlOpener urls,
            IPrivacyClock clock)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _urls = urls ?? throw new ArgumentNullException(nameof(urls));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _last = Snapshot();
        }

        public event Action ConsentChanged;

        public string PrivacyPolicyUrl
        {
            get { return _settings.PrivacyPolicyUrl; }
        }

        public bool HasPrivacyPolicy
        {
            get { return PrivacySettings.IsHttpsUrl(_settings.PrivacyPolicyUrl); }
        }

        public string ContactEmail
        {
            get { return _settings.ContactEmail; }
        }

        public PrivacyConsentStatus ConsentStatus
        {
            get { return _provider.ConsentStatus; }
        }

        public bool CanRequestAds
        {
            get { return _provider.CanRequestAds; }
        }

        public bool AnalyticsAllowed
        {
            get { return _provider.AnalyticsAllowed; }
        }

        public bool IsPrivacyOptionsRequired
        {
            get { return _provider.IsPrivacyOptionsRequired; }
        }

        public void OpenPrivacyPolicy()
        {
            if (!HasPrivacyPolicy)
            {
                Debug.LogError(
                    "[Privacy] There is no privacy policy to open: the config has no https address " +
                    (PrivacyPolicyUrl.Length == 0 ? "at all" : "('" + PrivacyPolicyUrl + "')") + ".");
                return;
            }

            Log("Opening the privacy policy: " + PrivacyPolicyUrl);
            _urls.Open(PrivacyPolicyUrl);
        }

        public void GatherConsent(Action onCompleted)
        {
            if (onCompleted == null)
                throw new ArgumentNullException(nameof(onCompleted));

            if (_gathered)
            {
                Invoke(onCompleted);
                return;
            }

            if (_waiting.Count == 0)
                _deadline = _clock.RealtimeSinceStartup + _settings.GatherTimeoutSeconds;

            _waiting.Add(onCompleted);

            if (_requestOut)
                return;

            _requestOut = true;
            Log("Gathering consent through '" + _provider.Platform + "'.");
            CallProvider(
                answer => _provider.GatherConsent(_settings.ConsentRequest, answer),
                OnGathered);
        }

        public void ShowPrivacyOptions(Action onClosed)
        {
            if (onClosed == null)
                throw new ArgumentNullException(nameof(onClosed));

            if (_optionsShowing)
            {
                Invoke(onClosed);
                return;
            }

            if (!_provider.IsPrivacyOptionsRequired)
            {
                Log("Privacy options were asked for, but the player's region needs none.");
                Invoke(onClosed);
                return;
            }

            _optionsShowing = true;
            CallProvider(
                answer => _provider.ShowPrivacyOptions(answer),
                error =>
                {
                    _optionsShowing = false;
                    if (error != null)
                        Debug.LogWarning("[Privacy] The privacy options form could not be shown: " + error);

                    RaiseIfChanged();
                    Invoke(onClosed);
                });
        }

        public void ResetConsent()
        {
            if (_requestOut || _optionsShowing)
            {
                Debug.LogWarning("[Privacy] The consent was not reset: '" + _provider.Platform + "' is still busy with " +
                                 "a request or a form. Try again when it is done.");
                return;
            }

            _provider.ResetConsent();
            _gathered = false;
            Log("Consent reset: the next GatherConsent asks as on a first launch.");

            RaiseIfChanged();
        }

        public void Tick()
        {
            if (_waiting.Count == 0)
                return;

            if (_provider.IsShowingForm)
            {
                // The player is reading the form; the clock starts again once it has closed.
                _deadline = _clock.RealtimeSinceStartup + _settings.GatherTimeoutSeconds;
                return;
            }

            if (_clock.RealtimeSinceStartup < _deadline)
                return;

            Debug.LogWarning(
                "[Privacy] '" + _provider.Platform + "' has not answered in " + _settings.GatherTimeoutSeconds +
                " s, so the game goes on without the answer. Ads and analytics follow the answer saved on the " +
                "device, if any, until the platform reports back.");
            AnswerWaiting();
        }

        private void OnGathered(string error)
        {
            _requestOut = false;

            if (error == null)
            {
                _gathered = true;
                Log("Consent gathered: " + Describe(Snapshot()) + ".");
            }
            else
            {
                Debug.LogWarning(
                    "[Privacy] Consent could not be gathered: " + error + ". Ads and analytics follow the answer " +
                    "saved on the device, if any; the next GatherConsent tries again.");
            }

            RaiseIfChanged();
            AnswerWaiting();
        }

        /// <summary>
        /// Calls the platform with a callback that counts only once, and turns a throw into a failure, so the caller is
        /// answered whatever the platform does.
        /// </summary>
        private void CallProvider(Action<Action<string>> call, Action<string> onAnswer)
        {
            bool answered = false;
            Action<string> once = error =>
            {
                if (answered)
                {
                    Debug.LogWarning("[Privacy] '" + _provider.Platform + "' answered twice; the second answer is " +
                                     "ignored.");
                    return;
                }

                answered = true;
                onAnswer(error);
            };

            try
            {
                call(once);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                once(exception.Message);
            }
        }

        private void AnswerWaiting()
        {
            if (_waiting.Count == 0)
                return;

            Action[] waiting = _waiting.ToArray();
            _waiting.Clear();
            foreach (Action callback in waiting)
                Invoke(callback);
        }

        private void RaiseIfChanged()
        {
            ConsentSnapshot now = Snapshot();
            if (now.Equals(_last))
                return;

            _last = now;
            Log("Consent changed: " + Describe(now) + ".");

            try
            {
                ConsentChanged?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private ConsentSnapshot Snapshot()
        {
            return new ConsentSnapshot(
                _provider.ConsentStatus,
                CanRequestAds,
                AnalyticsAllowed,
                _provider.IsPrivacyOptionsRequired);
        }

        private static string Describe(ConsentSnapshot snapshot)
        {
            return "status " + snapshot.Status + ", ads " + (snapshot.CanRequestAds ? "allowed" : "not allowed") +
                   ", analytics " + (snapshot.AnalyticsAllowed ? "allowed" : "not allowed") +
                   ", privacy options " + (snapshot.PrivacyOptionsRequired ? "required" : "not required");
        }

        private static void Invoke(Action callback)
        {
            try
            {
                callback();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void Log(string message)
        {
            if (_settings.LogRequests)
                Debug.Log("[Privacy] " + message);
        }

        private readonly struct ConsentSnapshot : IEquatable<ConsentSnapshot>
        {
            public readonly PrivacyConsentStatus Status;
            public readonly bool CanRequestAds;
            public readonly bool AnalyticsAllowed;
            public readonly bool PrivacyOptionsRequired;

            public ConsentSnapshot(PrivacyConsentStatus status, bool canRequestAds, bool analyticsAllowed,
                bool privacyOptionsRequired)
            {
                Status = status;
                CanRequestAds = canRequestAds;
                AnalyticsAllowed = analyticsAllowed;
                PrivacyOptionsRequired = privacyOptionsRequired;
            }

            public bool Equals(ConsentSnapshot other)
            {
                return Status == other.Status
                       && CanRequestAds == other.CanRequestAds
                       && AnalyticsAllowed == other.AnalyticsAllowed
                       && PrivacyOptionsRequired == other.PrivacyOptionsRequired;
            }
        }
    }
}
