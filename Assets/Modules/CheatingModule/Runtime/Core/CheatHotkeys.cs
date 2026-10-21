using System;
using UnityEngine.InputSystem;
using Zenject;

namespace Game.Cheating
{
    /// <summary>
    /// Runs a registered cheat when its <see cref="CheatDefinition.Hotkey"/> is pressed — keyboard playtests in the editor
    /// and desktop builds. Only while the admin mode is on, and at most one cheat a frame.
    /// <para>
    /// The keys are read with no regard for focus, so a letter also fires while typing into an input field of a scene
    /// that handles that cheat.
    /// </para>
    /// </summary>
    internal sealed class CheatHotkeys : ITickable
    {
        private readonly ICheatingService _service;
        private readonly Func<Key, bool> _isKeyDown;

        [Inject]
        public CheatHotkeys(ICheatingService service) : this(service, IsKeyDown)
        {
        }

        internal CheatHotkeys(ICheatingService service, Func<Key, bool> isKeyDown)
        {
            _service = service;
            _isKeyDown = isKeyDown;
        }

        public void Tick()
        {
            if (!_service.IsAdminEnabled)
                return;

            // Found first and run after the loop: the cheat may load a scene, which changes the registry.
            CheatDefinition pressed = null;

            foreach (CheatDefinition cheat in _service.RegisteredCheats)
            {
                if (cheat != null && cheat.Hotkey != Key.None && _isKeyDown(cheat.Hotkey))
                {
                    pressed = cheat;
                    break;
                }
            }

            if (pressed != null)
                _service.Execute(pressed);
        }

        // No keyboard at all on most phones.
        private static bool IsKeyDown(Key key)
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard[key].wasPressedThisFrame;
        }
    }
}
