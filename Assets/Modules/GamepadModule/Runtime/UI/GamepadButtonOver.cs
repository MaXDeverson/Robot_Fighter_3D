using UnityEngine;
using UnityEngine.UI;

namespace Game.GamepadSupport
{
    /// <summary>
    /// A button's own look for being selected from a pad, in place of the common frame <see cref="GamepadUIOverlay"/>
    /// draws: while one of its buttons is selected, the overlay switches this visual on and hides its frame.
    /// <para>
    /// The visual is part of the button, so it moves, scales and is clipped with it. Shown only under the same rules as
    /// the frame: a pad connected and the button's menu in focus.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GamepadButtonOver : MonoBehaviour
    {
        [Tooltip("Switched on while one of the buttons below is selected from a pad. Keep it off in the prefab.")]
        [SerializeField] private GameObject _visual;
        [Tooltip("The buttons this look stands for — this object or its children. Empty: the button on this object.")]
        [SerializeField] private Selectable[] _buttons;

        internal bool Covers(GameObject selected)
        {
            if (_buttons == null || _buttons.Length == 0)
                return selected == gameObject;

            for (int i = 0; i < _buttons.Length; i++)
            {
                if (_buttons[i] != null && _buttons[i].gameObject == selected)
                    return true;
            }

            return false;
        }

        internal void SetShown(bool shown)
        {
            if (_visual != null && _visual.activeSelf != shown)
                _visual.SetActive(shown);
        }

        private void Awake()
        {
            SetShown(false);
        }

        private void OnDisable()
        {
            SetShown(false);
        }
    }
}
