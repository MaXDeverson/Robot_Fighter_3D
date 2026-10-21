using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Game.GamepadSupport
{
    /// <summary>
    /// Keeps track of the pad the prompts are drawn for. Plugging and unplugging is reported by the Input System; which
    /// pad was used last is not, so it is looked at once a frame as well.
    /// </summary>
    internal sealed class GamepadService : IGamepadService, IInitializable, ITickable, IDisposable
    {
        private readonly GamepadConfig _config;

        private Gamepad _current;
        private GamepadFamily _family;

        public GamepadService(GamepadConfig config)
        {
            _config = config;
        }

        public bool IsConnected => _current != null;

        public Gamepad Current => _current;

        public GamepadFamily Family => _family;

        public event Action Changed;

        public void Initialize()
        {
            InputSystem.onDeviceChange += HandleDeviceChange;
            Refresh();
        }

        public void Dispose()
        {
            InputSystem.onDeviceChange -= HandleDeviceChange;
        }

        public void Tick() => Refresh();

        public Sprite GetGlyph(InputAction action)
        {
            if (_current == null || action == null)
                return null;

            string controlPath = GamepadControlFinder.FindControlPath(action, _current);
            if (controlPath == null)
                return null;

            GamepadGlyphSet glyphs = _config.GetGlyphSet(_family);
            return glyphs != null ? glyphs.Find(controlPath) : null;
        }

        private void HandleDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device is Gamepad)
                Refresh();
        }

        /// <summary>Called on every device change and every frame; raises <see cref="Changed"/> only when the pad differs.</summary>
        internal void Refresh()
        {
            // Gamepad.current is cleared when that pad is unplugged, and stays empty until another one is used — while
            // that other pad is plugged in all along.
            Gamepad pad = Gamepad.current;
            if (pad == null || !pad.added)
                pad = Gamepad.all.Count > 0 ? Gamepad.all[0] : null;

            if (pad == _current)
                return;

            _current = pad;
            _family = pad != null ? GamepadFamilyResolver.Resolve(pad) : default;
            Changed?.Invoke();
        }
    }
}
