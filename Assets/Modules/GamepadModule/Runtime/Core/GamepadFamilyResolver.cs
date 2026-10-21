using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.XInput;

namespace Game.GamepadSupport
{
    /// <summary>
    /// Tells which <see cref="GamepadFamily"/> a pad belongs to. Desktop and iOS drivers give PlayStation and Xbox pads
    /// layouts of their own; Android mostly does not, and there only the name the pad reports gives it away.
    /// </summary>
    internal static class GamepadFamilyResolver
    {
        // Only registered on desktop platforms, so it is matched by name: the type is not compiled into a phone build.
        private const string SwitchProLayout = "SwitchProControllerHID";

        private static readonly string[] PlayStationNames = { "sony", "playstation", "dualshock", "dualsense", "wireless controller" };
        private static readonly string[] SwitchNames = { "nintendo", "pro controller", "joy-con" };

        public static GamepadFamily Resolve(InputDevice pad)
        {
            if (pad is DualShockGamepad)
                return GamepadFamily.PlayStation;

            if (pad is XInputController)
                return GamepadFamily.Xbox;

            if (!string.IsNullOrEmpty(pad.layout) && InputSystem.IsFirstLayoutBasedOnSecond(pad.layout, SwitchProLayout))
                return GamepadFamily.Switch;

            // "Wireless Controller" is the name a DualShock 4 reports over Bluetooth on Android.
            string name = $"{pad.description.manufacturer} {pad.description.product} {pad.displayName}".ToLowerInvariant();

            if (ContainsAny(name, PlayStationNames))
                return GamepadFamily.PlayStation;

            if (ContainsAny(name, SwitchNames))
                return GamepadFamily.Switch;

            return GamepadFamily.Xbox;
        }

        private static bool ContainsAny(string text, string[] parts)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                if (text.Contains(parts[i]))
                    return true;
            }

            return false;
        }
    }
}
