using Game.Ads;
using UnityEngine;
using Zenject;

/// <summary>
/// Keeps a part of the gameplay UI where nothing covers it: inside the device's safe area — clear of notches, rounded
/// corners and the home indicator — and clear of the ad banner while one is on screen.
/// </summary>
/// <remarks>
/// <para>
/// The anchors set in the editor are read as fractions of that area instead of the whole screen: the HUD, stretched
/// over the screen, is stretched over the area; the pause button, anchored to the top left corner, keeps to the area's
/// top left corner. So the parent has to cover the whole screen, as a child of a Screen Space Overlay canvas does.
/// </para>
/// <para>
/// The edge the banner is on stops <c>Banner Gap</c> short of it. Looked at every frame: the banner comes late, once its
/// ad has loaded, and goes with an ad block, and the safe area changes when the device turns — none of it is announced.
/// The anchors are written only when the area has changed, and put back as they were when this is switched off.
/// </para>
/// </remarks>
[RequireComponent(typeof(RectTransform))]
public sealed class SafeAreaFitter : MonoBehaviour
{
    [Tooltip("Keep inside the device's safe area as well: away from notches, rounded corners and the home indicator. " +
             "Off: only the banner is kept clear of.")]
    [SerializeField] private bool _deviceSafeArea = true;

    [Tooltip("Canvas units between the banner and the edge of the area, so that nothing touches the banner.")]
    [SerializeField, Min(0f)] private float _bannerGap = 10f;

    [InjectOptional] private IAdsManagerService _ads;

    private RectTransform _rect;
    private Canvas _canvas;

    // The anchors as set in the editor, read as fractions of the area.
    private Vector2 _anchorMin;
    private Vector2 _anchorMax;

    // The area the anchors were last written for, as fractions of the screen; min above max until the first write.
    private Vector2 _fittedMin = Vector2.one;
    private Vector2 _fittedMax = Vector2.zero;

    private void Awake()
    {
        _rect = (RectTransform)transform;
        _anchorMin = _rect.anchorMin;
        _anchorMax = _rect.anchorMax;

        Canvas canvas = GetComponentInParent<Canvas>(true);
        _canvas = canvas != null ? canvas.rootCanvas : null;
    }

    private void OnEnable()
    {
        // Before the first frame is drawn, not a frame late.
        Fit();
    }

    private void LateUpdate()
    {
        Fit();
    }

    private void OnDisable()
    {
        _rect.anchorMin = _anchorMin;
        _rect.anchorMax = _anchorMax;
        _fittedMin = Vector2.one;
        _fittedMax = Vector2.zero;
    }

    private void Fit()
    {
        if (Screen.width <= 0 || Screen.height <= 0)
            return;

        Rect area = FreeArea();
        Vector2 min = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
        Vector2 max = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
        if (min == _fittedMin && max == _fittedMax)
            return;

        _fittedMin = min;
        _fittedMax = max;
        _rect.anchorMin = Remap(_anchorMin, min, max);
        _rect.anchorMax = Remap(_anchorMax, min, max);
    }

    /// <summary>The part of the screen nothing covers, in pixels from the bottom left corner.</summary>
    private Rect FreeArea()
    {
        Rect area = _deviceSafeArea ? Screen.safeArea : new Rect(0f, 0f, Screen.width, Screen.height);

        // Only its height counts: the edge it is on is kept clear across, however wide it is said to be.
        Rect banner = _ads != null ? _ads.BannerScreenArea : Rect.zero;
        if (banner.height <= 0f)
            return area;

        // Canvas units to pixels. Not known before the canvas scaler's first update; the next frame puts it right.
        float gap = _bannerGap * (_canvas != null ? _canvas.scaleFactor : 1f);

        if (banner.center.y >= Screen.height / 2f)
            area.yMax = Mathf.Max(area.yMin, Mathf.Min(area.yMax, banner.yMin - gap));
        else
            area.yMin = Mathf.Min(area.yMax, Mathf.Max(area.yMin, banner.yMax + gap));

        return area;
    }

    private static Vector2 Remap(Vector2 anchor, Vector2 min, Vector2 max)
    {
        return new Vector2(Mathf.LerpUnclamped(min.x, max.x, anchor.x), Mathf.LerpUnclamped(min.y, max.y, anchor.y));
    }
}
