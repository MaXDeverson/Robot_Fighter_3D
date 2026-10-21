using System.Collections;
using Game.Cheating;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

/// <summary>
/// Shows the device id with a Copy button — how a phone gets into the admin list (<see cref="CheatingConfig"/> →
/// Allowed Devices). Opened by 5 quick taps on the game version in the settings, with no admin mode needed: the phone
/// is not in the list yet.
/// </summary>
/// <remarks>
/// The module's <c>DeviceIdView</c> does the same with TextMeshPro; the menu is built on <see cref="Text"/>.
/// </remarks>
public sealed class DeviceIdPanel : MonoBehaviour
{
    [SerializeField] private Text _idText;

    [Tooltip("Optional. The device model and whether the admin mode is available here.")]
    [SerializeField] private Text _detailsText;

    [SerializeField] private Button _copyButton;

    [Tooltip("Optional. Hides the panel.")]
    [SerializeField] private Button _closeButton;

    [Tooltip("Optional. Shown for a moment after copying.")]
    [SerializeField] private GameObject _copiedFeedback;

    [SerializeField, Min(0f)] private float _copiedFeedbackSeconds = 1.5f;

    [Inject] private IDeviceInfo _device;
    [Inject] private ICheatingService _cheating;

    private Coroutine _feedback;

    private void Awake()
    {
        if (_copyButton != null)
            _copyButton.onClick.AddListener(Copy);

        if (_closeButton != null)
            _closeButton.onClick.AddListener(() => gameObject.SetActive(false));
    }

    private void OnEnable()
    {
        if (_copiedFeedback != null)
            _copiedFeedback.SetActive(false);

        if (_device == null)
        {
            Debug.LogError($"[Cheating] {name} was not injected, so it has no device id to show.", this);
            return;
        }

        if (_idText != null)
            _idText.text = _device.DeviceId;

        if (_detailsText != null)
            _detailsText.text = $"{_device.DeviceModel}\nAdmin: {DescribeAccess()}";
    }

    private void OnDisable()
    {
        _feedback = null;
    }

    private void Copy()
    {
        if (_device == null)
            return;

        GUIUtility.systemCopyBuffer = _device.DeviceId;
        Debug.Log($"[Cheating] Device id copied: {_device.DeviceId}");

        if (_copiedFeedback == null)
            return;

        if (_feedback != null)
            StopCoroutine(_feedback);

        _feedback = StartCoroutine(ShowCopiedFeedback());
    }

    private IEnumerator ShowCopiedFeedback()
    {
        _copiedFeedback.SetActive(true);
        yield return new WaitForSecondsRealtime(_copiedFeedbackSeconds);
        _copiedFeedback.SetActive(false);
        _feedback = null;
    }

    private string DescribeAccess()
    {
        if (_cheating == null)
            return "unknown";

        return _cheating.Access switch
        {
            AdminAccess.Denied => "not in the list",
            AdminAccess.AllowedDevice => "allowed device",
            AdminAccess.DevelopmentBuild => "development build",
            _ => _cheating.Access.ToString().ToLowerInvariant(),
        };
    }
}
