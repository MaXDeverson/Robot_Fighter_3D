using UnityEngine;

namespace Game.Cheating
{
    /// <summary>What the admin access is decided on. Behind an interface so the tests can be any device.</summary>
    public interface IDeviceInfo
    {
        /// <summary>
        /// <see cref="SystemInfo.deviceUniqueIdentifier"/>: on Android the MD5 of ANDROID_ID, on iOS
        /// identifierForVendor. Not the IMEI — no store app may read that.
        /// </summary>
        string DeviceId { get; }

        /// <summary>The model, for people: it helps to label the id in the config.</summary>
        string DeviceModel { get; }

        RuntimePlatform Platform { get; }

        bool IsEditor { get; }

        /// <summary>A build with "Development Build" ticked. Never true in the editor.</summary>
        bool IsDevelopmentBuild { get; }
    }
}
