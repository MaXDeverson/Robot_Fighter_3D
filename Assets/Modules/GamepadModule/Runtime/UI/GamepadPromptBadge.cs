using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zenject;

namespace Game.GamepadSupport
{
    /// <summary>
    /// Shows, on a HUD button, the pad button that does the same thing. The glyph is worked out from the action's own
    /// bindings on the pad that is connected, so it follows the bindings in the actions asset and the kind of pad —
    /// the same action is a cross on a PlayStation pad and A on an Xbox one. Hidden while no pad is connected, or when
    /// the action has nothing on the pad — and then the touch hint it replaces, if it has one, is shown instead.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GamepadPromptBadge : MonoBehaviour
    {
        [Tooltip("What the button under the badge does, as an action of the game's actions asset.")]
        [SerializeField] private InputActionReference _action;

        [Tooltip("Switched on and off with the prompt: the background and the icon. Not this object — it keeps listening while hidden.")]
        [SerializeField] private GameObject _content;

        [SerializeField] private Image _icon;

        [Tooltip("Optional: what the prompt stands in for, such as a \"double tap to skip\" hint. Switched off while the " +
                 "prompt is shown and back on while it is hidden.")]
        [SerializeField] private GameObject _replaces;

        private IGamepadService _gamepad;
        private bool _isSubscribed;

        [Inject]
        public void Construct(IGamepadService gamepad)
        {
            _gamepad = gamepad;
            if (isActiveAndEnabled)
                Subscribe();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            if (_isSubscribed)
                _gamepad.Changed -= Refresh;
            _isSubscribed = false;
        }

        private void Subscribe()
        {
            if (_gamepad == null)
            {
                // Not injected (yet): stay hidden rather than show a stale glyph.
                SetVisible(null);
                return;
            }

            if (!_isSubscribed)
                _gamepad.Changed += Refresh;
            _isSubscribed = true;
            Refresh();
        }

        private void Refresh()
        {
            InputAction action = _action != null ? _action.action : null;
            SetVisible(_gamepad != null ? _gamepad.GetGlyph(action) : null);
        }

        private void SetVisible(Sprite glyph)
        {
            if (_icon != null && glyph != null)
                _icon.sprite = glyph;

            if (_content != null)
                _content.SetActive(glyph != null);

            if (_replaces != null)
                _replaces.SetActive(glyph == null);
        }

        /// <summary>Test hook: sets the fields without going through the serializer. Call before <see cref="Construct"/>.</summary>
        internal void Configure(InputActionReference action, GameObject content, Image icon, GameObject replaces = null)
        {
            _action = action;
            _content = content;
            _icon = icon;
            _replaces = replaces;
        }
    }
}
