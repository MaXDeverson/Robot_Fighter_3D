using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.GamepadSupport
{
    /// <summary>
    /// Whether a pad is plugged in, which one, and what its buttons look like. Everything that shows a gamepad prompt
    /// asks here, so the prompts come and go together with the pad.
    /// </summary>
    public interface IGamepadService
    {
        /// <summary>True while at least one pad is plugged in.</summary>
        bool IsConnected { get; }

        /// <summary>
        /// The pad the prompts are drawn for: the one used last, or, before any has been used, one that is plugged
        /// in. Null while there is none.
        /// </summary>
        Gamepad Current { get; }

        /// <summary>The family of <see cref="Current"/>. Meaningless while nothing is connected.</summary>
        GamepadFamily Family { get; }

        /// <summary>Raised when a pad is plugged in or out, and when another pad takes over as <see cref="Current"/>.</summary>
        event Action Changed;

        /// <summary>
        /// The glyph of the button on <see cref="Current"/> that <paramref name="action"/> is bound to — the first of its
        /// bindings this pad has a control for. Null while no pad is connected, or when the action has nothing on a pad.
        /// </summary>
        Sprite GetGlyph(InputAction action);
    }
}
