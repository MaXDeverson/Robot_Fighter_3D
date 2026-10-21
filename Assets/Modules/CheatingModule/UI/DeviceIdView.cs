using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Game.Cheating.UI
{
    /// <summary>
    /// Shows the device id with a Copy button — how a new phone gets into <see cref="CheatingConfig"/>: open the panel on
    /// that phone, copy, send it to yourself, paste into the config. It has to be reachable without the admin mode (the
    /// phone is not allowed yet), so hide it behind something players will not stumble on, e.g. a
    /// <see cref="MultiTapReveal"/>.
    /// </summary>
    public sealed class DeviceIdView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _idText;

        [Tooltip("Optional. The device model and whether the admin mode is available here.")]
        [SerializeField] private TMP_Text _detailsText;

        [SerializeField] private Button _copyButton;

        [Tooltip("Optional. Shown for a moment after copying.")]
        [SerializeField] private GameObject _copiedFeedback;

        [SerializeField, Min(0f)] private float _copiedFeedbackSeconds = 1.5f;

        private IDeviceInfo _device;
        private ICheatingService _service;
        private Coroutine _feedback;

        [Inject]
        private void Construct(IDeviceInfo device, ICheatingService service)
        {
            _device = device;
            _service = service;
        }

        private void Awake()
        {
            if (_copyButton != null)
                _copyButton.onClick.AddListener(Copy);
        }

        private void OnEnable()
        {
            if (_copiedFeedback != null)
                _copiedFeedback.SetActive(false);

            if (_device == null)
                return;

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
            if (_service == null)
                return "unknown";

            return _service.Access switch
            {
                AdminAccess.Denied => "not in the list",
                AdminAccess.AllowedDevice => "allowed device",
                AdminAccess.DevelopmentBuild => "development build",
                _ => _service.Access.ToString().ToLowerInvariant(),
            };
        }
    }
}
