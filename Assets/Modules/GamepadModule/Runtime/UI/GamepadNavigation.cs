using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zenject;

namespace Game.GamepadSupport
{
    /// <summary>
    /// Drives the menus from a pad: which menu has the focus, which button in it is selected, and the buttons that
    /// answer a pad button of their own (back, pause).
    /// <para>
    /// The focus belongs to the <see cref="GamepadNavigationScope"/> switched on last — a window opened over another
    /// takes it, and closing it hands it back, together with the button that was selected there. While a pad is
    /// connected the focused menu always has a button selected, and the selection never leaves it: Unity's automatic
    /// navigation would otherwise walk off a popup onto the buttons of the window behind it. With no menu focused the
    /// selection is cleared, so the confirm button — the jump button too — cannot press a HUD button tapped earlier.
    /// </para>
    /// <para>
    /// With no pad connected the selection is left alone: a finger or a mouse owns it then.
    /// </para>
    /// </summary>
    public sealed class GamepadNavigation : ITickable
    {
        private const float ScrollPadding = 24f;

        private readonly IGamepadService _gamepad;
        private readonly GamepadConfig _config;

        private readonly List<GamepadNavigationScope> _scopes = new List<GamepadNavigationScope>();
        private readonly List<GamepadButtonShortcut> _shortcuts = new List<GamepadButtonShortcut>();
        private readonly Dictionary<InputAction, int> _actionUsers = new Dictionary<InputAction, int>();
        private readonly HashSet<InputAction> _enabledHere = new HashSet<InputAction>();
        private readonly List<Selectable> _selectables = new List<Selectable>();

        private GamepadNavigationScope _focused;
        private GameObject _lastSelected;

        public GamepadNavigation(IGamepadService gamepad, GamepadConfig config)
        {
            _gamepad = gamepad;
            _config = config;
        }

        /// <summary>
        /// The menu that has the focus, or null while none is open. Gameplay input is off while this is set, so a
        /// button pressed in a menu does not reach the character as well.
        /// </summary>
        public GamepadNavigationScope Focused => _focused;

        /// <summary>True when the focused menu has a back button that would answer the cancel button right now.</summary>
        public bool CanGoBack { get; private set; }

        internal void Add(GamepadNavigationScope scope)
        {
            _scopes.Remove(scope);
            _scopes.Add(scope);
        }

        internal void Remove(GamepadNavigationScope scope)
        {
            _scopes.Remove(scope);
        }

        internal void Add(GamepadButtonShortcut shortcut)
        {
            if (_shortcuts.Contains(shortcut))
                return;

            _shortcuts.Add(shortcut);
            foreach (InputAction action in shortcut.Actions)
                Retain(action);
        }

        internal void Remove(GamepadButtonShortcut shortcut)
        {
            if (!_shortcuts.Remove(shortcut))
                return;

            foreach (InputAction action in shortcut.Actions)
                Release(action);
        }

        public void Tick()
        {
            GamepadNavigationScope top = FindTop();
            EventSystem eventSystem = EventSystem.current;

            // A dropdown builds its list under the root canvas, outside the menu it belongs to. While the list is open,
            // walking it is left to the dropdown, and the cancel button closes the list rather than the whole menu.
            bool isInDropdownList = eventSystem != null && IsInDropdownList(eventSystem.currentSelectedGameObject);

            if (!isInDropdownList)
                RunShortcuts(top);
            CanGoBack = top != null && !isInDropdownList && HasBackButton(top);

            bool focusChanged = top != _focused;
            _focused = top;

            if (!_gamepad.IsConnected || eventSystem == null || isInDropdownList)
                return;

            if (top == null)
            {
                if (eventSystem.currentSelectedGameObject != null)
                    eventSystem.SetSelectedGameObject(null);
                _lastSelected = null;
                return;
            }

            GameObject selected = eventSystem.currentSelectedGameObject;
            if (focusChanged || !IsSelectableIn(selected, top))
            {
                Selectable target = IsSelectableIn(top.LastSelected, top) ? top.LastSelected.GetComponent<Selectable>() : FindFirst(top);
                selected = target != null ? target.gameObject : null;
                if (selected != eventSystem.currentSelectedGameObject)
                    eventSystem.SetSelectedGameObject(selected);
            }

            if (selected == null)
                return;

            top.LastSelected = selected;

            if (selected != _lastSelected)
            {
                _lastSelected = selected;
                ScrollIntoView(selected.transform as RectTransform);
            }
        }

        private GamepadNavigationScope FindTop()
        {
            for (int i = _scopes.Count - 1; i >= 0; i--)
            {
                if (_scopes[i] != null && _scopes[i].isActiveAndEnabled)
                    return _scopes[i];

                _scopes.RemoveAt(i);
            }

            return null;
        }

        /// <summary>Clicks at most one button a frame: the first shortcut whose pad button went down and that is in reach.</summary>
        private void RunShortcuts(GamepadNavigationScope top)
        {
            for (int i = _shortcuts.Count - 1; i >= 0; i--)
            {
                GamepadButtonShortcut shortcut = _shortcuts[i];
                if (shortcut == null || !shortcut.WasTriggered() || !IsInReach(shortcut, top))
                    continue;

                shortcut.Click();
                return;
            }
        }

        private bool HasBackButton(GamepadNavigationScope top)
        {
            InputAction cancel = _config.CancelAction;
            if (cancel == null)
                return false;

            for (int i = 0; i < _shortcuts.Count; i++)
            {
                GamepadButtonShortcut shortcut = _shortcuts[i];
                if (shortcut != null && shortcut.Listens(cancel) && IsInReach(shortcut, top))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// A shortcut answers only for the focused menu. One outside every menu — the pause button on the HUD — answers
        /// only while no menu is open, so the same button cannot open the pause window again from under it.
        /// </summary>
        private static bool IsInReach(GamepadButtonShortcut shortcut, GamepadNavigationScope top)
        {
            if (shortcut.Scope != top)
                return false;

            Button button = shortcut.Button;
            return button != null && button.IsInteractable() && UIReach.IsReachable(button);
        }

        // Both UGUI's Dropdown and TextMeshPro's name the list they build so.
        private const string DropdownListName = "Dropdown List";

        private static bool IsInDropdownList(GameObject candidate)
        {
            if (candidate == null || !candidate.activeInHierarchy)
                return false;

            for (Transform t = candidate.transform; t != null; t = t.parent)
            {
                if (t.name == DropdownListName)
                    return true;
            }

            return false;
        }

        private static bool IsSelectableIn(GameObject candidate, GamepadNavigationScope scope)
        {
            if (candidate == null || !candidate.activeInHierarchy || !candidate.transform.IsChildOf(scope.transform))
                return false;

            Selectable selectable = candidate.GetComponent<Selectable>();
            return IsNavigable(selectable);
        }

        private static bool IsNavigable(Selectable selectable)
        {
            return selectable != null
                && selectable.isActiveAndEnabled
                && selectable.IsInteractable()
                && selectable.navigation.mode != Navigation.Mode.None
                && UIReach.IsReachable(selectable);
        }

        private Selectable FindFirst(GamepadNavigationScope scope)
        {
            if (IsNavigable(scope.FirstSelected))
                return scope.FirstSelected;

            scope.GetComponentsInChildren(false, _selectables);
            Selectable first = null;
            for (int i = 0; i < _selectables.Count; i++)
            {
                if (IsNavigable(_selectables[i]))
                {
                    first = _selectables[i];
                    break;
                }
            }

            _selectables.Clear();
            return first;
        }

        /// <summary>A scroll view does not follow the selection by itself: a button walked onto off screen is scrolled in.</summary>
        private static void ScrollIntoView(RectTransform target)
        {
            if (target == null)
                return;

            ScrollRect scrollRect = target.GetComponentInParent<ScrollRect>();
            if (scrollRect == null || scrollRect.content == null || !target.IsChildOf(scrollRect.content))
                return;

            RectTransform viewport = scrollRect.viewport != null ? scrollRect.viewport : (RectTransform)scrollRect.transform;
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, target);
            Rect view = viewport.rect;
            Vector2 shift = Vector2.zero;

            if (scrollRect.vertical)
            {
                if (bounds.max.y + ScrollPadding > view.yMax)
                    shift.y = bounds.max.y + ScrollPadding - view.yMax;
                else if (bounds.min.y - ScrollPadding < view.yMin)
                    shift.y = bounds.min.y - ScrollPadding - view.yMin;
            }

            if (scrollRect.horizontal)
            {
                if (bounds.max.x + ScrollPadding > view.xMax)
                    shift.x = bounds.max.x + ScrollPadding - view.xMax;
                else if (bounds.min.x - ScrollPadding < view.xMin)
                    shift.x = bounds.min.x - ScrollPadding - view.xMin;
            }

            if (shift == Vector2.zero)
                return;

            scrollRect.StopMovement();
            scrollRect.content.anchoredPosition -= shift;
        }

        /// <summary>
        /// Shortcut actions are switched on for as long as some shortcut listens to them, in case nothing else has
        /// switched them on. Only an action switched on here is switched off here: the game keeps its own actions
        /// (<c>GameInput</c>) on in every scene and reads them itself, and the input module runs the UI ones.
        /// </summary>
        private void Retain(InputAction action)
        {
            if (action == null)
                return;

            _actionUsers.TryGetValue(action, out int users);
            _actionUsers[action] = users + 1;

            if (users == 0 && !action.enabled)
            {
                action.Enable();
                _enabledHere.Add(action);
            }
        }

        private void Release(InputAction action)
        {
            if (action == null || !_actionUsers.TryGetValue(action, out int users))
                return;

            if (users > 1)
            {
                _actionUsers[action] = users - 1;
                return;
            }

            _actionUsers.Remove(action);

            if (_enabledHere.Remove(action))
                action.Disable();
        }
    }
}
