using System;
using System.Collections.Generic;
using System.Threading;
using GoogleMobileAds.Ump.Api;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace Game.Privacy.UmpImplementation
{
    /// <summary>
    /// Google's User Messaging Platform (UMP, part of the Google Mobile Ads SDK) as the game's consent platform. It shows
    /// the messages set up in the AdMob console (Privacy &amp; messaging): the GDPR form in the EEA, the UK and
    /// Switzerland; nothing anywhere else.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lives on the root of <c>Prefabs/UmpPrivacyConsentProvider</c> in the privacy module; the <c>PrivacyInstaller</c>
    /// creates it when its <b>Provider</b> is <c>Network</c> and this prefab is its <b>Network Provider Prefab</b>.
    /// </para>
    /// <para>
    /// Gathering is <c>ConsentInformation.Update</c>, then — only when the region needs consent and the player has not
    /// answered — <c>ConsentForm.Load</c> and <c>Show</c>. Loading and showing apart, rather than
    /// <c>LoadAndShowConsentFormIfRequired</c>, tells exactly when the form is on the screen: the service's boot timeout
    /// stands still from then on (<see cref="IsShowingForm"/>).
    /// </para>
    /// <para>
    /// The ads need nothing from here but <see cref="CanRequestAds"/>: UMP saves the player's choices on the device as
    /// an IAB TCF string, and the Google Mobile Ads SDK reads it before every request — personalized ads after a
    /// consent, non-personalized or limited ones after a refusal.
    /// </para>
    /// <para>
    /// The analytics are not in the form, so <see cref="AnalyticsAllowed"/> comes from the TCF purposes the player
    /// consented to: allowed where the GDPR does not apply, and under it only with purpose 1 (store and access
    /// information on a device — GameAnalytics keeps its ids there and reads the advertising ID) and purpose 8 (measure
    /// content performance — what the analytics is for). The TCF keys live in the app's default SharedPreferences on
    /// Android (not in PlayerPrefs, which use a file of their own) and in the standard user defaults on iOS, where
    /// PlayerPrefs read them. The editor has neither: there an answered form allows the analytics.
    /// </para>
    /// <para>
    /// The SDK may answer from a thread of its own; every answer is brought back to the main thread before it is acted
    /// on.
    /// </para>
    /// </remarks>
    internal sealed class UmpPrivacyConsentProvider : PrivacyConsentProvider
    {
        // IAB TCF v2.2 keys UMP writes on the device.
        private const string GdprAppliesKey = "IABTCF_gdprApplies";
        private const string PurposeConsentsKey = "IABTCF_PurposeConsents";

        // Purpose 1 "Store and/or access information on a device", purpose 8 "Measure content performance".
        private static readonly int[] AnalyticsPurposes = { 1, 8 };

        private bool _initialized;
        private SynchronizationContext _mainThread;
        private int _mainThreadId;

        private PrivacyConsentStatus _status;
        private bool _canRequestAds;
        private bool _analyticsAllowed;
        private bool _optionsRequired;
        private bool _showingForm;
        private string _loggedTcf;

        public override string Platform
        {
            get { return "ump"; }
        }

        public override PrivacyConsentStatus ConsentStatus
        {
            get
            {
                EnsureInitialized();
                return _status;
            }
        }

        public override bool CanRequestAds
        {
            get
            {
                EnsureInitialized();
                return _canRequestAds;
            }
        }

        public override bool AnalyticsAllowed
        {
            get
            {
                EnsureInitialized();
                return _analyticsAllowed;
            }
        }

        public override bool IsPrivacyOptionsRequired
        {
            get
            {
                EnsureInitialized();
                return _optionsRequired;
            }
        }

        public override bool IsShowingForm
        {
            get { return _showingForm; }
        }

        public override void GatherConsent(PrivacyConsentRequest request, Action<string> onCompleted)
        {
            EnsureInitialized();
            ConsentInformation.Update(ToParameters(request), updateError => OnMainThread(onCompleted, () =>
            {
                Refresh();
                if (updateError != null)
                {
                    onCompleted(Describe("update the consent information", updateError));
                    return;
                }

                if (ConsentInformation.ConsentStatus != GoogleMobileAds.Ump.Api.ConsentStatus.Required)
                {
                    // Answered on an earlier launch, or a region with no form.
                    onCompleted(null);
                    return;
                }

                if (!ConsentInformation.IsConsentFormAvailable())
                {
                    onCompleted("the region needs consent, but there is no consent form for this app — check that the " +
                                "GDPR message is published in AdMob, Privacy & messaging");
                    return;
                }

                ConsentForm.Load((form, loadError) => OnMainThread(onCompleted, () =>
                {
                    if (loadError != null || form == null)
                    {
                        onCompleted(Describe("load the consent form", loadError));
                        return;
                    }

                    ShowForm(onCompleted, form.Show, "show the consent form");
                }));
            }));
        }

        public override void ShowPrivacyOptions(Action<string> onClosed)
        {
            EnsureInitialized();
            ShowForm(onClosed, ConsentForm.ShowPrivacyOptionsForm, "show the privacy options form");
        }

        public override void ResetConsent()
        {
            EnsureInitialized();
            ConsentInformation.Reset();
            Refresh();
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            // Always reached on the main thread first: from Awake, or from the service reading a property.
            _initialized = true;
            _mainThread = SynchronizationContext.Current;
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            Refresh();
        }

        private void ShowForm(Action<string> onClosed, Action<Action<FormError>> show, string action)
        {
            _showingForm = true;
            try
            {
                show(showError => OnMainThread(onClosed, () =>
                {
                    _showingForm = false;
                    Refresh();
                    onClosed(showError == null ? null : Describe(action, showError));
                }));
            }
            catch
            {
                // While the flag is up the boot timeout stands still: a form that never came up must not keep it there.
                _showingForm = false;
                throw;
            }
        }

        /// <summary>Reads everything the service asks for once, after every answer of the SDK.</summary>
        private void Refresh()
        {
            _status = ToStatus(ConsentInformation.ConsentStatus);
            _canRequestAds = ConsentInformation.CanRequestAds();
            _optionsRequired = ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;
            _analyticsAllowed = (_status == PrivacyConsentStatus.NotRequired || _status == PrivacyConsentStatus.Obtained)
                                && TcfAllowsAnalytics();
        }

        private bool TcfAllowsAnalytics()
        {
            int gdprApplies;
            string purposeConsents;
            if (!TryReadTcf(out gdprApplies, out purposeConsents))
            {
                return false;
            }

            bool allowed = gdprApplies != 1 || HasConsents(purposeConsents, AnalyticsPurposes);

            if (Debug.isDebugBuild)
            {
                string tcf = "gdprApplies " + gdprApplies + ", purpose consents '" + purposeConsents + "'";
                if (tcf != _loggedTcf)
                {
                    _loggedTcf = tcf;
                    Debug.Log("[Privacy] UMP saved " + tcf + ": analytics " + (allowed ? "allowed." : "not allowed."), this);
                }
            }

            return allowed;
        }

        /// <summary>The TCF values UMP saved; -1 and an empty string where it saved none (no form was ever answered).</summary>
        private bool TryReadTcf(out int gdprApplies, out string purposeConsents)
        {
            gdprApplies = -1;
            purposeConsents = string.Empty;
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                using (AndroidJavaClass preferenceManager = new AndroidJavaClass("android.preference.PreferenceManager"))
                using (AndroidJavaObject preferences = preferenceManager.CallStatic<AndroidJavaObject>(
                           "getDefaultSharedPreferences", AndroidApplication.currentContext))
                {
                    gdprApplies = preferences.Call<int>("getInt", GdprAppliesKey, -1);
                    purposeConsents = preferences.Call<string>("getString", PurposeConsentsKey, string.Empty) ?? string.Empty;
                }
#elif UNITY_IOS && !UNITY_EDITOR
                gdprApplies = PlayerPrefs.GetInt(GdprAppliesKey, -1);
                purposeConsents = PlayerPrefs.GetString(PurposeConsentsKey, string.Empty);
#endif
                return true;
            }
            catch (Exception exception)
            {
                // Not knowing what the player chose, the analytics stay off.
                Debug.LogError("[Privacy] The consent UMP saved on the device could not be read, so the analytics " +
                               "stay off: " + exception.Message, this);
                return false;
            }
        }

        private static bool HasConsents(string purposeConsents, int[] purposes)
        {
            // One character per purpose, '1' for a consent; purpose N is at index N - 1.
            foreach (int purpose in purposes)
            {
                if (purposeConsents.Length < purpose || purposeConsents[purpose - 1] != '1')
                {
                    return false;
                }
            }

            return true;
        }

        private void OnMainThread(Action<string> onAnswer, Action action)
        {
            Action guarded = () =>
            {
                try
                {
                    action();
                }
                catch (Exception exception)
                {
                    // The service waits for an answer; a throw would leave it waiting for the timeout or for ever.
                    Debug.LogException(exception, this);
                    _showingForm = false;
                    onAnswer(exception.Message);
                }
            };

            if (Thread.CurrentThread.ManagedThreadId == _mainThreadId || _mainThread == null)
            {
                guarded();
                return;
            }

            _mainThread.Post(_ =>
            {
                // The SDK may answer after this object has gone with the ProjectContext, as the game quits.
                if (this != null)
                {
                    guarded();
                }
            }, null);
        }

        private static ConsentRequestParameters ToParameters(PrivacyConsentRequest request)
        {
            var parameters = new ConsentRequestParameters
            {
                TagForUnderAgeOfConsent = request.TagForUnderAgeOfConsent,
            };

            // Both are dropped from a release build by the config already.
            if (request.DebugGeography != PrivacyDebugGeography.Disabled || request.TestDeviceHashedIds.Count > 0)
            {
                parameters.ConsentDebugSettings = new ConsentDebugSettings
                {
                    DebugGeography = ToDebugGeography(request.DebugGeography),
                    TestDeviceHashedIds = new List<string>(request.TestDeviceHashedIds),
                };
            }

            return parameters;
        }

        private static DebugGeography ToDebugGeography(PrivacyDebugGeography geography)
        {
            switch (geography)
            {
                case PrivacyDebugGeography.Eea:
                    return DebugGeography.EEA;
                case PrivacyDebugGeography.RegulatedUsState:
                    return DebugGeography.RegulatedUSState;
                case PrivacyDebugGeography.Other:
                    return DebugGeography.Other;
                default:
                    return DebugGeography.Disabled;
            }
        }

        private static PrivacyConsentStatus ToStatus(GoogleMobileAds.Ump.Api.ConsentStatus status)
        {
            switch (status)
            {
                case GoogleMobileAds.Ump.Api.ConsentStatus.NotRequired:
                    return PrivacyConsentStatus.NotRequired;
                case GoogleMobileAds.Ump.Api.ConsentStatus.Required:
                    return PrivacyConsentStatus.Required;
                case GoogleMobileAds.Ump.Api.ConsentStatus.Obtained:
                    return PrivacyConsentStatus.Obtained;
                default:
                    return PrivacyConsentStatus.Unknown;
            }
        }

        private static string Describe(string action, FormError error)
        {
            return error != null
                ? "UMP could not " + action + " (code " + error.ErrorCode + ": " + error.Message + ")"
                : "UMP could not " + action + " and gave no reason";
        }
    }
}
