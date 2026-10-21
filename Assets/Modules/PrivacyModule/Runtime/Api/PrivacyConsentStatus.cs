namespace Game.Privacy
{
    /// <summary>
    /// Where the player stands with the consent the law of their region asks for — GDPR in the EEA, the UK and
    /// Switzerland, the privacy laws of some US states. The same four states as Google's UMP.
    /// </summary>
    public enum PrivacyConsentStatus
    {
        /// <summary>Not known yet: consent has never been gathered on this device, or the last attempt failed.</summary>
        Unknown = 0,

        /// <summary>The player's region needs no consent form.</summary>
        NotRequired = 1,

        /// <summary>The region needs consent and the player has not answered the form yet.</summary>
        Required = 2,

        /// <summary>The player has answered the form. Whatever they chose, the answer is in.</summary>
        Obtained = 3,
    }
}
