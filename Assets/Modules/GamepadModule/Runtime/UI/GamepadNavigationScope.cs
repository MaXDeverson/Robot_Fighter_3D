using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Game.GamepadSupport
{
    /// <summary>
    /// Marks a menu that a pad can walk through. Goes on the object that is switched on for as long as the menu is on
    /// screen — a window's container — and takes the focus from whatever menu had it while it is on. See
    /// <see cref="GamepadNavigation"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GamepadNavigationScope : MonoBehaviour
    {
        [Tooltip("Selected when the menu gets the focus with a pad connected. Empty: the first button in the menu that can be pressed.")]
        [SerializeField] private Selectable _firstSelected;

        private GamepadNavigation _navigation;

        /// <summary>
        /// Can be set from code for a menu whose first button changes — the level selection starts on the last level
        /// unlocked. Read when the menu gets the focus.
        /// </summary>
        public Selectable FirstSelected
        {
            get => _firstSelected;
            set => _firstSelected = value;
        }

        /// <summary>
        /// The button selected here last, given back when a window opened over this menu closes. Forgotten when the menu
        /// itself closes: opened again, it starts from its first button rather than from where it was left.
        /// </summary>
        internal GameObject LastSelected { get; set; }

        [Inject]
        public void Construct(GamepadNavigation navigation)
        {
            _navigation = navigation;
            if (isActiveAndEnabled)
                _navigation.Add(this);
        }

        private void OnEnable()
        {
            _navigation?.Add(this);
        }

        private void OnDisable()
        {
            LastSelected = null;
            _navigation?.Remove(this);
        }

        /// <summary>Test hook: sets the first button without going through the serializer.</summary>
        internal void Configure(Selectable firstSelected) => _firstSelected = firstSelected;
    }
}
