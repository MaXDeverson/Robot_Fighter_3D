using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Cheating
{
    /// <summary>
    /// Who may switch the admin mode on. The ids ship inside the build: they are not secrets, only a list of the team's
    /// phones, and faking one takes a rooted device — enough for a puzzle game.
    /// </summary>
    [CreateAssetMenu(fileName = "CheatingConfig", menuName = "Game/Cheating/Cheating Config")]
    public sealed class CheatingConfig : ScriptableObject
    {
        [Serializable]
        public sealed class AllowedDevice
        {
            [Tooltip("Whose device it is, for people: \"Max — Pixel 7\".")]
            public string Label;

            [Tooltip("The id the device shows in the Device ID panel (5 taps on the game version in the settings). " +
                     "On Android it differs between debug, locally signed and Google Play builds — take it from the " +
                     "build the device really runs.")]
            public string Id;

            public AllowedDevice()
            {
            }

            public AllowedDevice(string label, string id)
            {
                Label = label;
                Id = id;
            }
        }

        [Tooltip("The editor always may.")]
        [SerializeField] private bool _allowInEditor = true;

        [Tooltip("Windows, macOS and Linux builds always may.")]
        [SerializeField] private bool _allowOnDesktop = true;

        [Tooltip("Any device running a Development Build may. Handy while the device's id for the release key is unknown; " +
                 "leave it off if testers get development builds.")]
        [SerializeField] private bool _allowInDevelopmentBuild;

        [Tooltip("The phones and tablets that may, by their device id.")]
        [SerializeField] private List<AllowedDevice> _allowedDevices = new List<AllowedDevice>();

        [Tooltip("Writes the device id and the admin access to the log at start (IngameDebugConsole, adb logcat).")]
        [SerializeField] private bool _logDeviceIdOnStart = true;

        public bool AllowInEditor => _allowInEditor;

        public bool AllowOnDesktop => _allowOnDesktop;

        public bool AllowInDevelopmentBuild => _allowInDevelopmentBuild;

        public IReadOnlyList<AllowedDevice> AllowedDevices => _allowedDevices;

        public bool LogDeviceIdOnStart => _logDeviceIdOnStart;

        /// <summary>
        /// Finds the entry for <paramref name="deviceId"/>. Spaces around a pasted id and the letter case do not matter
        /// (Android gives lower case hex, iOS an upper case UUID); an empty id matches nothing.
        /// </summary>
        public bool TryFindDevice(string deviceId, out AllowedDevice device)
        {
            device = null;

            string id = Normalize(deviceId);
            if (id.Length == 0 || _allowedDevices == null)
                return false;

            foreach (AllowedDevice candidate in _allowedDevices)
            {
                if (candidate != null && string.Equals(Normalize(candidate.Id), id, StringComparison.OrdinalIgnoreCase))
                {
                    device = candidate;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Test hook: fills the asset without going through the serializer.</summary>
        internal void Configure(bool allowInEditor, bool allowOnDesktop, bool allowInDevelopmentBuild,
            params AllowedDevice[] devices)
        {
            _allowInEditor = allowInEditor;
            _allowOnDesktop = allowOnDesktop;
            _allowInDevelopmentBuild = allowInDevelopmentBuild;
            _allowedDevices = new List<AllowedDevice>(devices);
            _logDeviceIdOnStart = false;
        }

        private static string Normalize(string id)
        {
            return id?.Trim() ?? string.Empty;
        }
    }
}
