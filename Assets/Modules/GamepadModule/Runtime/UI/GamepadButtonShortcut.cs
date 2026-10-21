using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Zenject;

namespace Game.GamepadSupport
{
    /// <summary>
    /// Presses a <see cref="Button"/> when a pad button goes down, wherever the selection is: a window's back button
    /// on cancel, the HUD pause button on Start. It is pressed the way the event system presses a button on confirm,
    /// so it flashes its pressed colour and raises <see cref="Button.onClick"/> like a tap would.
    /// <para>
    /// It answers only while the menu it sits in has the focus, and a button outside every menu only while no menu is
    /// open — <see cref="GamepadNavigation"/> decides, and presses at most one button a frame. A button that is locked
    /// for a finger (not interactable, or under a group that blocks raycasts) is locked for the pad too.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class GamepadButtonShortcut : MonoBehaviour
    {
        [Tooltip("Pad buttons that press this one. Several can be given: the pause window's Continue answers both Back and Start.")]
        [SerializeField] private List<InputActionReference> _actions = new List<InputActionReference>();

        private readonly List<InputAction> _resolved = new List<InputAction>();

        private GamepadNavigation _navigation;
        private Button _button;
        private uint _enabledInUpdate;
        private bool _isRegistered;

        public Button Button => _button != null ? _button : _button = GetComponent<Button>();

        /// <summary>The menu this button belongs to, or null for one outside every menu. Found when switched on.</summary>
        internal GamepadNavigationScope Scope { get; private set; }

        internal IReadOnlyList<InputAction> Actions => _resolved;

        [Inject]
        public void Construct(GamepadNavigation navigation)
        {
            _navigation = navigation;
            if (isActiveAndEnabled)
                Register();
        }

        private void OnEnable()
        {
            // The press that opened a window is not let through to the buttons of that window on the same frame.
            _enabledInUpdate = InputState.updateCount;
            Register();
        }

        private void OnDisable()
        {
            if (_isRegistered)
                _navigation.Remove(this);
            _isRegistered = false;
        }

        internal bool Listens(InputAction action) => _resolved.Contains(action);

        internal bool WasTriggered()
        {
            if (InputState.updateCount == _enabledInUpdate)
                return false;

            for (int i = 0; i < _resolved.Count; i++)
            {
                if (_resolved[i] != null && _resolved[i].WasPressedThisFrame())
                    return true;
            }

            return false;
        }

        internal void Click()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null)
                ExecuteEvents.Execute(Button.gameObject, new BaseEventData(eventSystem), ExecuteEvents.submitHandler);
            else
                Button.onClick.Invoke();
        }

        private void Register()
        {
            if (_navigation == null || _isRegistered)
                return;

            Scope = GetComponentInParent<GamepadNavigationScope>(true);

            _resolved.Clear();
            for (int i = 0; i < _actions.Count; i++)
            {
                if (_actions[i] != null && _actions[i].action != null)
                    _resolved.Add(_actions[i].action);
            }

            _navigation.Add(this);
            _isRegistered = true;
        }

        /// <summary>Test hook: sets the actions without going through the serializer. Call before the object is switched on.</summary>
        internal void Configure(params InputActionReference[] actions)
        {
            _actions = new List<InputActionReference>(actions);
        }
    }
}
