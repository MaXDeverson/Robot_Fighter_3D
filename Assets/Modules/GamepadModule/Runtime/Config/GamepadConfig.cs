using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.GamepadSupport
{
    /// <summary>
    /// Everything the module is set up with: the glyphs of each pad family, and the two UI actions the menu prompts
    /// stand for — confirm and back. Those two have to be the very actions the event system's input module runs on, or
    /// the prompt would name a button that does something else.
    /// </summary>
    [CreateAssetMenu(fileName = "GamepadConfig", menuName = "Game/Gamepad/Config")]
    public sealed class GamepadConfig : ScriptableObject
    {
        [Tooltip("One set per family. A family with no set of its own is drawn with the first set in the list.")]
        [SerializeField] private List<GamepadGlyphSet> _glyphSets = new List<GamepadGlyphSet>();

        [Tooltip("The UI Submit action of the input module. Its glyph is the \"Select\" prompt under a menu.")]
        [SerializeField] private InputActionReference _submitAction;

        [Tooltip("The UI Cancel action of the input module. Its glyph is the \"Back\" prompt, and a menu's back button is " +
                 "the GamepadButtonShortcut that listens to it.")]
        [SerializeField] private InputActionReference _cancelAction;

        public InputAction SubmitAction => _submitAction != null ? _submitAction.action : null;

        public InputAction CancelAction => _cancelAction != null ? _cancelAction.action : null;

        /// <summary>The set for <paramref name="family"/>, or the first set when it has none. Null only for an empty list.</summary>
        public GamepadGlyphSet GetGlyphSet(GamepadFamily family)
        {
            for (int i = 0; i < _glyphSets.Count; i++)
            {
                if (_glyphSets[i] != null && _glyphSets[i].Family == family)
                    return _glyphSets[i];
            }

            for (int i = 0; i < _glyphSets.Count; i++)
            {
                if (_glyphSets[i] != null)
                    return _glyphSets[i];
            }

            return null;
        }

        /// <summary>Test hook: fills the asset without going through the serializer.</summary>
        internal void Configure(IEnumerable<GamepadGlyphSet> glyphSets, InputActionReference submit = null,
            InputActionReference cancel = null)
        {
            _glyphSets = new List<GamepadGlyphSet>(glyphSets);
            _submitAction = submit;
            _cancelAction = cancel;
        }
    }
}
