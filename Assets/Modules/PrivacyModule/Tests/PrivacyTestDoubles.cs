using System;
using System.Collections.Generic;

namespace Game.Privacy.Tests
{
    /// <summary>A consent platform the test answers by hand: every request waits until <see cref="AnswerGather"/>.</summary>
    internal sealed class FakeConsentProvider : IPrivacyConsentProvider
    {
        public readonly List<PrivacyConsentRequest> GatherRequests = new List<PrivacyConsentRequest>();
        public int OptionsRequests;
        public int Resets;
        public bool ThrowOnGather;

        private Action<string> _pendingGather;
        private Action<string> _pendingOptions;

        public string Platform
        {
            get { return "fake"; }
        }

        public PrivacyConsentStatus ConsentStatus { get; set; }

        public bool CanRequestAds { get; set; }

        public bool AnalyticsAllowed { get; set; }

        public bool IsPrivacyOptionsRequired { get; set; }

        public bool IsShowingForm { get; set; }

        public void GatherConsent(PrivacyConsentRequest request, Action<string> onCompleted)
        {
            GatherRequests.Add(request);
            if (ThrowOnGather)
                throw new InvalidOperationException("platform blew up");

            _pendingGather = onCompleted;
        }

        public void ShowPrivacyOptions(Action<string> onClosed)
        {
            OptionsRequests++;
            _pendingOptions = onClosed;
        }

        /// <summary>Forgets the answer, as UMP does: nothing known, nothing allowed.</summary>
        public void ResetConsent()
        {
            Resets++;
            ConsentStatus = PrivacyConsentStatus.Unknown;
            CanRequestAds = false;
            AnalyticsAllowed = false;
            IsPrivacyOptionsRequired = false;
        }

        /// <summary>The player answered the form: consent in, ads and analytics allowed, options required.</summary>
        public void SetObtained(bool analyticsAllowed)
        {
            ConsentStatus = PrivacyConsentStatus.Obtained;
            CanRequestAds = true;
            AnalyticsAllowed = analyticsAllowed;
            IsPrivacyOptionsRequired = true;
        }

        public void AnswerGather(string error = null)
        {
            _pendingGather(error);
        }

        public void AnswerOptions(string error = null)
        {
            _pendingOptions(error);
        }
    }

    internal sealed class FakePrivacyClock : IPrivacyClock
    {
        public double RealtimeSinceStartup { get; set; }
    }

    internal sealed class FakeUrlOpener : IUrlOpener
    {
        public readonly List<string> Opened = new List<string>();

        public void Open(string url)
        {
            Opened.Add(url);
        }
    }
}
