using UnityEngine;

namespace Game.Cheating
{
    /// <summary>The device the game really runs on.</summary>
    public sealed class UnityDeviceInfo : IDeviceInfo
    {
        public string DeviceId => SystemInfo.deviceUniqueIdentifier;

        public string DeviceModel => SystemInfo.deviceModel;

        public RuntimePlatform Platform => Application.platform;

        public bool IsEditor => Application.isEditor;

        // Debug.isDebugBuild is always true in the editor, which has its own switch in the config.
        public bool IsDevelopmentBuild => !Application.isEditor && Debug.isDebugBuild;
    }
}
