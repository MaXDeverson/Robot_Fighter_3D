namespace Game.Cheating
{
    /// <summary>
    /// Why the admin mode can or can not be switched on on this device. Worked out once at start: the device does not
    /// change while the game runs.
    /// </summary>
    public enum AdminAccess
    {
        /// <summary>A player's device: no admin switch, no cheats.</summary>
        Denied = 0,

        /// <summary>The Unity editor.</summary>
        Editor = 1,

        /// <summary>A Windows, macOS or Linux build.</summary>
        Desktop = 2,

        /// <summary>The device's id is in <see cref="CheatingConfig"/>.</summary>
        AllowedDevice = 3,

        /// <summary>A development build, on any device, when the config allows it.</summary>
        DevelopmentBuild = 4,
    }
}
