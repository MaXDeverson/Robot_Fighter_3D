using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GamepadSupport
{
    /// <summary>
    /// The glyphs of one <see cref="GamepadFamily"/>, keyed by the control's path on the pad as the Input System names it:
    /// <c>buttonSouth</c>, <c>rightShoulder</c>, <c>leftStick/left</c>, <c>dpad/right</c>, <c>start</c>. The names are the
    /// position of a button rather than its label, so the same key is the cross on a PlayStation pad, A on an Xbox one
    /// and B on a Switch Pro controller — the set is what turns the position into what is printed on that pad.
    /// </summary>
    [CreateAssetMenu(fileName = "GamepadGlyphs", menuName = "Game/Gamepad/Glyph Set")]
    public sealed class GamepadGlyphSet : ScriptableObject
    {
        [Serializable]
        public struct Glyph
        {
            [Tooltip("Path of the control on the pad, relative to the pad: buttonSouth, leftStick/left, dpad/up, start.")]
            public string Control;

            public Sprite Sprite;

            public Glyph(string control, Sprite sprite)
            {
                Control = control;
                Sprite = sprite;
            }
        }

        [SerializeField] private GamepadFamily _family;

        [Tooltip("A path with no glyph of its own falls back to its parent: leftStick/left to leftStick.")]
        [SerializeField] private List<Glyph> _glyphs = new List<Glyph>();

        public GamepadFamily Family => _family;

        public IReadOnlyList<Glyph> Glyphs => _glyphs;

        /// <summary>
        /// The glyph for <paramref name="controlPath"/>, or for the nearest control above it that has one — a direction of
        /// a stick is drawn as the stick when the set has no arrow for it. Null when nothing on the way has a glyph.
        /// </summary>
        public Sprite Find(string controlPath)
        {
            string path = controlPath;
            while (!string.IsNullOrEmpty(path))
            {
                for (int i = 0; i < _glyphs.Count; i++)
                {
                    if (_glyphs[i].Sprite != null && string.Equals(_glyphs[i].Control, path, StringComparison.OrdinalIgnoreCase))
                        return _glyphs[i].Sprite;
                }

                int slash = path.LastIndexOf('/');
                path = slash > 0 ? path.Substring(0, slash) : null;
            }

            return null;
        }

        /// <summary>Test hook: fills the asset without going through the serializer.</summary>
        internal void Configure(GamepadFamily family, IEnumerable<Glyph> glyphs)
        {
            _family = family;
            _glyphs = new List<Glyph>(glyphs);
        }
    }
}
