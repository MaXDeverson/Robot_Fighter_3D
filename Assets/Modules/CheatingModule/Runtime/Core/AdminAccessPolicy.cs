using UnityEngine;

namespace Game.Cheating
{
    /// <summary>Decides whether a device may switch the admin mode on. No state: the same answer for the same input.</summary>
    public static class AdminAccessPolicy
    {
        public static AdminAccess Resolve(CheatingConfig config, IDeviceInfo device)
        {
            if (config == null || device == null)
                return AdminAccess.Denied;

            if (device.IsEditor && config.AllowInEditor)
                return AdminAccess.Editor;

            if (IsDesktopPlayer(device.Platform) && config.AllowOnDesktop)
                return AdminAccess.Desktop;

            if (config.TryFindDevice(device.DeviceId, out _))
                return AdminAccess.AllowedDevice;

            if (device.IsDevelopmentBuild && config.AllowInDevelopmentBuild)
                return AdminAccess.DevelopmentBuild;

            return AdminAccess.Denied;
        }

        private static bool IsDesktopPlayer(RuntimePlatform platform)
        {
            return platform == RuntimePlatform.WindowsPlayer
                   || platform == RuntimePlatform.OSXPlayer
                   || platform == RuntimePlatform.LinuxPlayer;
        }
    }
}
