using System;

namespace Game.Privacy
{
    /// <summary>
    /// The game's privacy: the privacy policy page and the player's consent. The one way for game code into this module.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The consent platform — the stub in this module, Google's UMP in the build with the SDKs — keeps the player's
    /// answer and shows its own form where the law requires one: <see cref="GatherConsent"/> at boot, before the ads and
    /// the analytics start. Everywhere else it shows nothing and the game starts at once. <see cref="ShowPrivacyOptions"/>
    /// brings the form back from a button in the settings. The game reads <see cref="CanRequestAds"/> and
    /// <see cref="AnalyticsAllowed"/> and listens to <see cref="ConsentChanged"/>.
    /// </para>
    /// <para>
    /// Every callback is invoked exactly once, on the main thread; one that throws is logged and does not break the
    /// service.
    /// </para>
    /// </remarks>
    public interface IPrivacyService
    {
        /// <summary>The privacy policy page from the config; empty when none is set.</summary>
        string PrivacyPolicyUrl { get; }

        /// <summary>Whether <see cref="PrivacyPolicyUrl"/> is an https address — the only kind Google Play accepts.</summary>
        bool HasPrivacyPolicy { get; }

        /// <summary>Where players write about their data, from the config; empty when none is set.</summary>
        string ContactEmail { get; }

        /// <summary>The player's consent as the platform knows it.</summary>
        PrivacyConsentStatus ConsentStatus { get; }

        /// <summary>
        /// Whether ads may be requested: the consent is in — whatever the player chose — or the region needs none. Read
        /// from the answer saved on the device, so it can be true before <see cref="GatherConsent"/> has finished on
        /// this launch. Whether the ads are personalized is up to the ad network, which reads the answer itself.
        /// </summary>
        bool CanRequestAds { get; }

        /// <summary>
        /// Whether analytics may be collected: the region needs no consent, or the player has consented to what the
        /// analytics does. False while the platform has not answered.
        /// </summary>
        bool AnalyticsAllowed { get; }

        /// <summary>
        /// Whether the settings have to offer a "Privacy options" button: the law of the player's region requires a way
        /// back to the form.
        /// </summary>
        bool IsPrivacyOptionsRequired { get; }

        /// <summary>
        /// Raised when any of <see cref="ConsentStatus"/>, <see cref="CanRequestAds"/>, <see cref="AnalyticsAllowed"/>
        /// or <see cref="IsPrivacyOptionsRequired"/> has changed — also when the platform answers after the timeout.
        /// </summary>
        event Action ConsentChanged;

        /// <summary>Opens the privacy policy in the browser. Logs an error and opens nothing when there is none.</summary>
        void OpenPrivacyPolicy();

        /// <summary>
        /// Brings the consent up to date and shows the form when the region needs it and the player has not answered.
        /// Meant for boot, before the ads and the analytics start.
        /// </summary>
        /// <param name="onCompleted">
        /// Invoked when the platform is done, when it has failed (no connection) or when the config's timeout passes
        /// first, so boot never hangs. The timeout stands still while the form is on the screen: the player is reading
        /// it. After a success later calls are answered at once; after a failure the next call tries again. Calls made
        /// while a request is out share it.
        /// </param>
        void GatherConsent(Action onCompleted);

        /// <summary>
        /// Shows the platform's form again so the player can change their answer. Does nothing but call back when
        /// <see cref="IsPrivacyOptionsRequired"/> is false or the form is already up.
        /// </summary>
        /// <param name="onClosed">Invoked when the form has closed or could not be shown.</param>
        void ShowPrivacyOptions(Action onClosed);

        /// <summary>
        /// For testing: forgets the player's answer on this device, so the next <see cref="GatherConsent"/> asks again
        /// as on a first launch. Refused while a request is out or a form is up.
        /// </summary>
        void ResetConsent();
    }
}
