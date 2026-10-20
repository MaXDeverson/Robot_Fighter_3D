using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Lets a gamepad drive the uGUI menus. While the gamepad is the active input it keeps a button selected,
/// so InputSystemUIInputModule can move the selection with the stick / d-pad and click it with A.
/// When a screen or popup opens, one of its buttons gets selected; ScrollRects follow the selection.
/// Selectables with Navigation mode None (gameplay HUD, touch controls) are never selected automatically,
/// see <see cref="ExcludeFromNavigation"/>.
/// </summary>
public class GamepadMenuNavigator : MonoBehaviour
{
    // buttons whose name starts with one of these are preferred as the first selection of a screen
    [SerializeField] private string[] _preferredNames = { "Play", "Resume", "Next", "OK", "Restart", "Agree", "Level" };

    private readonly HashSet<Selectable> _known = new HashSet<Selectable>();
    private readonly List<Selectable> _current = new List<Selectable>();
    private readonly List<Selectable> _appeared = new List<Selectable>();
    private Selectable[] _buffer = new Selectable[128];
    private GameObject _lastSelected;

    private static readonly List<int> _pathA = new List<int>();
    private static readonly List<int> _pathB = new List<int>();

    // removes all selectables under root from gamepad navigation (they still work with touch and mouse)
    public static void ExcludeFromNavigation(GameObject root)
    {
        if (root == null) return;
        foreach (Selectable selectable in root.GetComponentsInChildren<Selectable>(true))
        {
            Navigation navigation = selectable.navigation;
            navigation.mode = Navigation.Mode.None;
            selectable.navigation = navigation;
        }
    }

    private void OnEnable()
    {
        GameInput.onInputTypeChanged += OnInputTypeChanged;
    }

    private void OnDisable()
    {
        GameInput.onInputTypeChanged -= OnInputTypeChanged;
    }

    private void OnInputTypeChanged(INPUTTYPE inputType)
    {
        // touch players should not see a highlighted button
        if (inputType == INPUTTYPE.TOUCHSCREEN && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void LateUpdate()
    {
        CollectSelectables();

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null || GameInput.CurrentInputType != INPUTTYPE.JOYPAD) return;

        GameObject selected = eventSystem.currentSelectedGameObject;
        bool selectionValid = selected != null && selected.activeInHierarchy && selected.TryGetComponent(out Selectable selectedSelectable) && selectedSelectable.IsInteractable();

        // a screen or popup was opened: move the selection into it, unless something there is already selected
        Selectable target = null;
        if (_appeared.Count > 0 && !(selectionValid && _appeared.Exists(s => s.gameObject == selected))) target = PickBest(_appeared);
        else if (!selectionValid) target = PickBest(_current);

        if (target != null) eventSystem.SetSelectedGameObject(target.gameObject);
        else if (!selectionValid && selected != null) eventSystem.SetSelectedGameObject(null);

        selected = eventSystem.currentSelectedGameObject;
        if (selected != _lastSelected)
        {
            _lastSelected = selected;
            if (selected != null) ScrollIntoView(selected);
        }
    }

    // gathers the selectables a gamepad may select and the ones that were not there in the previous frame
    private void CollectSelectables()
    {
        if (_buffer.Length < Selectable.allSelectableCount) _buffer = new Selectable[Selectable.allSelectableCount * 2];
        int count = Selectable.AllSelectablesNoAlloc(_buffer);

        _current.Clear();
        _appeared.Clear();
        for (int i = 0; i < count; i++)
        {
            Selectable selectable = _buffer[i];
            if (selectable == null || !selectable.IsInteractable() || selectable.navigation.mode == Navigation.Mode.None || selectable is Scrollbar) continue;
            _current.Add(selectable);
            if (!_known.Contains(selectable)) _appeared.Add(selectable);
        }

        _known.Clear();
        foreach (Selectable selectable in _current) _known.Add(selectable);
        System.Array.Clear(_buffer, 0, count);
    }

    // picks a button of the screen that is drawn on top: a preferred name first, otherwise the first one in the hierarchy
    private Selectable PickBest(List<Selectable> candidates)
    {
        if (candidates.Count == 0) return null;

        Selectable top = candidates[0];
        foreach (Selectable candidate in candidates)
        {
            if (CompareDrawOrder(candidate, top) > 0) top = candidate;
        }

        Transform topScreen = ScreenOf(top);
        List<Selectable> screen = candidates.FindAll(s => ScreenOf(s) == topScreen);
        screen.Sort(CompareDrawOrder);

        foreach (string preferredName in _preferredNames)
        {
            Selectable preferred = screen.Find(s => s.name.StartsWith(preferredName, System.StringComparison.OrdinalIgnoreCase));
            if (preferred != null) return preferred;
        }
        return screen[0];
    }

    // the screen (panel) of a selectable is its ancestor that sits directly under the root canvas
    private static Transform ScreenOf(Selectable selectable)
    {
        Canvas canvas = selectable.GetComponentInParent<Canvas>();
        Transform root = canvas != null ? canvas.rootCanvas.transform : null;
        Transform screen = selectable.transform;
        while (screen.parent != null && screen.parent != root) screen = screen.parent;
        return screen;
    }

    private static int CompareDrawOrder(Selectable a, Selectable b)
    {
        int sortingA = SortingOrderOf(a);
        int sortingB = SortingOrderOf(b);
        if (sortingA != sortingB) return sortingA.CompareTo(sortingB);

        FillHierarchyPath(a.transform, _pathA);
        FillHierarchyPath(b.transform, _pathB);
        for (int i = 0; i < _pathA.Count && i < _pathB.Count; i++)
        {
            if (_pathA[i] != _pathB[i]) return _pathA[i].CompareTo(_pathB[i]);
        }
        return _pathA.Count.CompareTo(_pathB.Count);
    }

    private static int SortingOrderOf(Selectable selectable)
    {
        Canvas canvas = selectable.GetComponentInParent<Canvas>();
        return canvas != null ? canvas.rootCanvas.sortingOrder : 0;
    }

    private static void FillHierarchyPath(Transform transform, List<int> path)
    {
        path.Clear();
        for (Transform t = transform; t != null; t = t.parent) path.Add(t.GetSiblingIndex());
        path.Reverse();
    }

    // scrolls the ScrollRect that contains the selection so the selected button is visible
    private static void ScrollIntoView(GameObject selected)
    {
        ScrollRect scrollRect = selected.GetComponentInParent<ScrollRect>();
        if (scrollRect == null || scrollRect.content == null || !selected.transform.IsChildOf(scrollRect.content)) return;

        RectTransform viewport = scrollRect.viewport != null ? scrollRect.viewport : (RectTransform)scrollRect.transform;
        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, selected.transform);
        Rect view = viewport.rect;

        Vector2 offset = Vector2.zero;
        if (scrollRect.horizontal)
        {
            if (bounds.max.x > view.xMax) offset.x = bounds.max.x - view.xMax;
            else if (bounds.min.x < view.xMin) offset.x = bounds.min.x - view.xMin;
        }
        if (scrollRect.vertical)
        {
            if (bounds.max.y > view.yMax) offset.y = bounds.max.y - view.yMax;
            else if (bounds.min.y < view.yMin) offset.y = bounds.min.y - view.yMin;
        }
        if (offset == Vector2.zero) return;

        // move the content the other way, converted from viewport space to the content's parent space
        Vector3 worldOffset = viewport.TransformVector(offset);
        Vector3 localOffset = scrollRect.content.parent.InverseTransformVector(worldOffset);
        scrollRect.content.localPosition -= localOffset;
        scrollRect.velocity = Vector2.zero;
    }
}
