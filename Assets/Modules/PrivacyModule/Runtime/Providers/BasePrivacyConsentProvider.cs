using System;
using System.Collections;
using UnityEngine;

namespace Game.Privacy
{
    /// <summary>
    /// A stand-in consent platform for development: no form, no network. It answers the way the Inspector says, so the
    /// game's flows — boot, the "Privacy options" button, ads and analytics following the answer — can be walked through
    /// in the editor.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lives on <c>Prefabs/BasePrivacyConsentProvider</c>, which the <see cref="DI.PrivacyInstaller"/> creates under its
    /// own GameObject. Nothing is kept between launches: every launch starts from
    /// <see cref="PrivacyConsentStatus.Unknown"/>.
    /// </para>
    /// <para>
    /// To try a player changing their mind: switch <b>Simulation</b> in Play Mode, then press "Privacy options" — the
    /// stub "answers" the form again with the new choice.
    /// </para>
    /// </remarks>
    internal sealed class BasePrivacyConsentProvider : PrivacyConsentProvider
    {
        /// <summary>What the stub pretends the region and the player are.</summary>
        internal enum Simulation
        {
            /// <summary>A region with no form: ads and analytics allowed, no "Privacy options" button.</summary>
            NotRequired = 0,

            /// <summary>A regulated region; the player consents to everything.</summary>
            ConsentGiven = 1,

            /// <summary>A regulated region; the player refuses: ads still come (limited), analytics do not.</summary>
            ConsentRefused = 2,

            /// <summary>No connection: every request fails and nothing changes.</summary>
            Offline = 3,
        }

        [SerializeField]
        [Tooltip("Not Required: a region with no form. Consent Given / Consent Refused: a regulated region (EEA, UK, a " +
                 "US state with a privacy law) and the player's answer. Offline: every request fails.")]
        private Simulation _simulation = Simulation.NotRequired;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Seconds before the stub answers, real time — the network and the player reading the form. 0 answers " +
                 "at once.")]
        private float _answerDelay = 0.5f;

        private PrivacyConsentStatus _status = PrivacyConsentStatus.Unknown;
        private bool _consented;

        public override string Platform
        {
            get { return "stub"; }
        }

        public override PrivacyConsentStatus ConsentStatus
        {
            get { return _status; }
        }

        public override bool CanRequestAds
        {
            // As UMP: once the form is answered ads may be requested whatever the answer; a refusal only limits them.
            get { return _status == PrivacyConsentStatus.NotRequired || _status == PrivacyConsentStatus.Obtained; }
        }

        public override bool AnalyticsAllowed
        {
            get
            {
                return _status == PrivacyConsentStatus.NotRequired
                       || (_status == PrivacyConsentStatus.Obtained && _consented);
            }
        }

        public override bool IsPrivacyOptionsRequired
        {
            get { return _status == PrivacyConsentStatus.Obtained; }
        }

        public override bool IsShowingForm
        {
            // The stub has no form: the answer delay stands for the network as much as for the player, so the
            // service's timeout keeps running through it.
            get { return false; }
        }

        public override void ResetConsent()
        {
            _status = PrivacyConsentStatus.Unknown;
            _consented = false;
        }

        public override void GatherConsent(PrivacyConsentRequest request, Action<string> onCompleted)
        {
            AnswerLater(onCompleted, () =>
            {
                if (_simulation == Simulation.Offline)
                    return "no connection (simulated by the stub)";

                if (_simulation == Simulation.NotRequired)
                {
                    _status = PrivacyConsentStatus.NotRequired;
                    _consented = false;
                    return null;
                }

                AnswerForm();
                return null;
            });
        }

        public override void ShowPrivacyOptions(Action<string> onClosed)
        {
            AnswerLater(onClosed, () =>
            {
                if (_simulation == Simulation.Offline)
                    return "no connection (simulated by the stub)";

                if (_simulation == Simulation.NotRequired)
                    return "the stub simulates a region with no form";

                AnswerForm();
                return null;
            });
        }

        /// <summary>Test hook: sets what the stub simulates, and the delay.</summary>
        internal void Simulate(Simulation simulation, float answerDelay = 0f)
        {
            _simulation = simulation;
            _answerDelay = answerDelay;
        }

        private void AnswerForm()
        {
            _status = PrivacyConsentStatus.Obtained;
            _consented = _simulation == Simulation.ConsentGiven;
            Debug.Log("[Privacy] The stub stands in for the consent form: the player " +
                      (_consented ? "consents." : "refuses."), this);
        }

        private void AnswerLater(Action<string> callback, Func<string> answer)
        {
            if (_answerDelay <= 0f || !isActiveAndEnabled)
            {
                callback(answer());
                return;
            }

            StartCoroutine(AnswerAfterDelay(callback, answer));
        }

        private IEnumerator AnswerAfterDelay(Action<string> callback, Func<string> answer)
        {
            yield return new WaitForSecondsRealtime(_answerDelay);
            callback(answer());
        }
    }
}
