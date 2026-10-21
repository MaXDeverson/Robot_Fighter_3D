namespace Game.Privacy
{
    /// <summary>
    /// Where the consent platform should believe the device is, to test the forms from anywhere. Honoured only in the
    /// editor and development builds, and by Google's UMP only on its test devices.
    /// </summary>
    public enum PrivacyDebugGeography
    {
        /// <summary>The real location.</summary>
        Disabled = 0,

        /// <summary>The EEA, the UK or Switzerland: the GDPR form.</summary>
        Eea = 1,

        /// <summary>A US state with a privacy law: the US state regulations message.</summary>
        RegulatedUsState = 2,

        /// <summary>Anywhere no form is needed.</summary>
        Other = 3,
    }
}
