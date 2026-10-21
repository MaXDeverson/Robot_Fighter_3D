using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace Game.GamepadSupport
{
    /// <summary>
    /// What a pad player sees on top of every menu: a frame around the selected button and, in the corner, the prompts
    /// "Select" and "Back" with the glyphs of the pad in hand. The game's buttons are drawn in every style there is, and
    /// many of them look the same selected as not, so the selection is shown here, once, rather than by each button.
    /// A button that wants a look of its own says so with a <see cref="GamepadButtonOver"/>: the frame then stays hidden
    /// while it is selected.
    /// <para>
    /// Lives on its own overlay canvas next to the module's installer in the project context, so it is there in every
    /// scene. Shown only while a pad is connected and a menu has the focus; "Back" only while that menu has a back button.
    /// </para>
    /// </summary>
    public sealed class GamepadUIOverlay : MonoBehaviour
    {
        [Tooltip("The overlay canvas's own rect, the frame and the prompts are placed in.")]
        [SerializeField] private RectTransform _area;

        [Header("Selection")]
        [SerializeField] private RectTransform _selectionFrame;
        [SerializeField] private CanvasGroup _selectionGroup;
        [Tooltip("How far, in canvas units, the frame stands out from the edges of the selected button.")]
        [SerializeField] private float _selectionPadding = 12f;
        [Tooltip("How quickly the frame slides to the next button. 0 jumps.")]
        [SerializeField] private float _followSpeed = 18f;
        [SerializeField] private float _pulseSpeed = 4f;
        [SerializeField, Range(0f, 1f)] private float _pulseMinAlpha = 0.55f;

        [Header("Prompts")]
        [SerializeField] private GameObject _prompts;
        [SerializeField] private GameObject _submitPrompt;
        [SerializeField] private Image _submitIcon;
        [SerializeField] private GameObject _cancelPrompt;
        [SerializeField] private Image _cancelIcon;

        private readonly Vector3[] _corners = new Vector3[4];

        private IGamepadService _gamepad;
        private GamepadNavigation _navigation;
        private GamepadConfig _config;
        private bool _isFrameShown;

        private GameObject _overResolvedFor;
        private GamepadButtonOver _overForSelected;
        private GamepadButtonOver _shownOver;

        [Inject]
        public void Construct(IGamepadService gamepad, GamepadNavigation navigation, GamepadConfig config)
        {
            _gamepad = gamepad;
            _navigation = navigation;
            _config = config;
        }

        private void Awake()
        {
            HideAll();
        }

        private void LateUpdate()
        {
            if (_gamepad == null || !_gamepad.IsConnected || _navigation.Focused == null)
            {
                HideAll();
                return;
            }

            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            bool hasSelection = selected != null && selected.activeInHierarchy;

            // A button with a look of its own shows that instead of the frame.
            GamepadButtonOver over = hasSelection ? FindOver(selected) : null;
            SetOverShown(over);

            if (over != null)
                SetFrameShown(false);
            else
            {
                hasSelection = hasSelection && UpdateFrame(selected.transform as RectTransform);
                if (!hasSelection)
                    SetFrameShown(false);
            }

            UpdatePrompts(hasSelection);
        }

        /// <summary>The nearest <see cref="GamepadButtonOver"/> up from the selection that stands for it, if any.</summary>
        private GamepadButtonOver FindOver(GameObject selected)
        {
            if (selected != _overResolvedFor)
            {
                _overResolvedFor = selected;
                _overForSelected = null;

                for (Transform t = selected.transform; t != null; t = t.parent)
                {
                    if (t.TryGetComponent(out GamepadButtonOver candidate) && candidate.Covers(selected))
                    {
                        _overForSelected = candidate;
                        break;
                    }
                }
            }

            return _overForSelected != null && _overForSelected.isActiveAndEnabled ? _overForSelected : null;
        }

        private void SetOverShown(GamepadButtonOver over)
        {
            if (_shownOver != null && _shownOver != over)
                _shownOver.SetShown(false);

            _shownOver = over;
            if (over != null)
                over.SetShown(true);
        }

        private bool UpdateFrame(RectTransform target)
        {
            if (target == null || _selectionFrame == null || !TryGetRect(target, out Rect rect))
                return false;

            Vector2 size = rect.size + Vector2.one * (_selectionPadding * 2f);

            if (!_isFrameShown || _followSpeed <= 0f)
            {
                _selectionFrame.anchoredPosition = rect.center;
                _selectionFrame.sizeDelta = size;
            }
            else
            {
                // Menus run with the game paused, so the frame moves in real time.
                float t = 1f - Mathf.Exp(-_followSpeed * Time.unscaledDeltaTime);
                _selectionFrame.anchoredPosition = Vector2.Lerp(_selectionFrame.anchoredPosition, rect.center, t);
                _selectionFrame.sizeDelta = Vector2.Lerp(_selectionFrame.sizeDelta, size, t);
            }

            SetFrameShown(true);

            if (_selectionGroup != null)
            {
                float wave = (Mathf.Sin(Time.unscaledTime * _pulseSpeed) + 1f) * 0.5f;
                _selectionGroup.alpha = Mathf.Lerp(_pulseMinAlpha, 1f, wave);
            }

            return true;
        }

        /// <summary>The target's rect in the overlay's space, whatever canvas the target is drawn on.</summary>
        private bool TryGetRect(RectTransform target, out Rect rect)
        {
            rect = default;

            Canvas canvas = target.GetComponentInParent<Canvas>();
            if (canvas == null)
                return false;

            canvas = canvas.rootCanvas;
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera != null ? canvas.worldCamera : Camera.main;

            target.GetWorldCorners(_corners);
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            for (int i = 0; i < 4; i++)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, _corners[i]);
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, screen, null, out Vector2 local))
                    return false;

                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }

            // The frame is anchored to the middle of the overlay, so the position is measured from there.
            Vector2 pivotOffset = (_area.pivot - new Vector2(0.5f, 0.5f)) * _area.rect.size;
            rect = Rect.MinMaxRect(min.x + pivotOffset.x, min.y + pivotOffset.y, max.x + pivotOffset.x, max.y + pivotOffset.y);
            return true;
        }

        private void UpdatePrompts(bool hasSelection)
        {
            if (_prompts == null)
                return;

            Sprite submit = hasSelection ? _gamepad.GetGlyph(_config.SubmitAction) : null;
            Sprite cancel = _navigation.CanGoBack ? _gamepad.GetGlyph(_config.CancelAction) : null;

            SetPrompt(_submitPrompt, _submitIcon, submit);
            SetPrompt(_cancelPrompt, _cancelIcon, cancel);
            _prompts.SetActive(submit != null || cancel != null);
        }

        private static void SetPrompt(GameObject prompt, Image icon, Sprite glyph)
        {
            if (prompt != null && prompt.activeSelf != (glyph != null))
                prompt.SetActive(glyph != null);

            if (icon != null && glyph != null && icon.sprite != glyph)
                icon.sprite = glyph;
        }

        private void SetFrameShown(bool shown)
        {
            _isFrameShown = shown;
            if (_selectionFrame != null && _selectionFrame.gameObject.activeSelf != shown)
                _selectionFrame.gameObject.SetActive(shown);
        }

        private void HideAll()
        {
            SetFrameShown(false);
            SetOverShown(null);
            if (_prompts != null && _prompts.activeSelf)
                _prompts.SetActive(false);
        }
    }
}
