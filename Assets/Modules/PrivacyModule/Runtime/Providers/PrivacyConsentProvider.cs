using System;
using UnityEngine;

namespace Game.Privacy
{
    /// <summary>
    /// A consent platform: it keeps the player's answer, decides whether the region needs a form and shows it. The
    /// module has a stub, <see cref="BasePrivacyConsentProvider"/>; a real platform — Google's UMP — derives from this
    /// class in an assembly of its own and is put on a prefab that the <see cref="DI.PrivacyInstaller"/> creates.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Public only so that an implementation can live outside the module. Game code never talks to a provider: it goes
    /// through <see cref="IPrivacyService"/>, which adds the timeout, the shared requests and the change events.
    /// </para>
    /// <para>
    /// The properties are read at any time, also before <see cref="GatherConsent"/>: they report the answer saved on
    /// the device from the previous launch, or <see cref="PrivacyConsentStatus.Unknown"/> when there is none.
    /// </para>
    /// <para>
    /// The callbacks are to be invoked exactly once and on the main thread — SDKs that answer from their own thread have
    /// to hop back first. Answering before the call returns is fine.
    /// </para>
    /// </remarks>
    public abstract class PrivacyConsentProvider : MonoBehaviour, IPrivacyConsentProvider
    {
        /// <summary>The platform's name for logs: <c>stub</c>, <c>ump</c>.</summary>
        public abstract string Platform { get; }

        /// <summary>The player's consent as the platform knows it.</summary>
        public abstract PrivacyConsentStatus ConsentStatus { get; }

        /// <summary>Whether ads may be requested: the consent is in, or the region needs none.</summary>
        public abstract bool CanRequestAds { get; }

        /// <summary>Whether analytics may be collected.</summary>
        public abstract bool AnalyticsAllowed { get; }

        /// <summary>Whether the region requires a way back to the form.</summary>
        public abstract bool IsPrivacyOptionsRequired { get; }

        /// <summary>
        /// Whether the platform's form is on the screen right now. While it is, the service does not time boot out: the
        /// player is reading it, however long that takes.
        /// </summary>
        public abstract bool IsShowingForm { get; }

        /// <summary>
        /// Brings the consent up to date and shows the form when the region needs it and the player has not answered.
        /// The service never calls it again while a call is out.
        /// </summary>
        /// <param name="request">What the config says about the request.</param>
        /// <param name="onCompleted">Null when it went through, or what went wrong.</param>
        public abstract void GatherConsent(PrivacyConsentRequest request, Action<string> onCompleted);

        /// <summary>Shows the form again so the player can change their answer.</summary>
        /// <param name="onClosed">Null when the form was shown and closed, or what went wrong.</param>
        public abstract void ShowPrivacyOptions(Action<string> onClosed);

        /// <summary>
        /// For testing: forgets the answer saved on the device, back to <see cref="PrivacyConsentStatus.Unknown"/>. The
        /// service never calls it while a call is out.
        /// </summary>
        public abstract void ResetConsent();
    }
}
