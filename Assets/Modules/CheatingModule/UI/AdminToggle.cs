using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Game.Cheating.UI
{
    /// <summary>
    /// The admin switch: put it on a <see cref="Toggle"/>. The toggle turns the admin mode on and off and follows it when
    /// it changes from elsewhere. On a device where the admin mode is not available the whole row is hidden for good —
    /// the device does not change while the game runs.
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public sealed class AdminToggle : MonoBehaviour
    {
        [Tooltip("Hidden where the admin mode is not available: the row with the label. Empty: the toggle itself.")]
        [SerializeField] private GameObject _row;

        private ICheatingService _service;
        private Toggle _toggle;

        [Inject]
        private void Construct(ICheatingService service)
        {
            Unsubscribe();

            _service = service;
            _service.AdminEnabledChanged += OnAdminEnabledChanged;

            Toggle.onValueChanged.RemoveListener(OnToggleChanged);
            Toggle.onValueChanged.AddListener(OnToggleChanged);
            Toggle.SetIsOnWithoutNotify(_service.IsAdminEnabled);

            GameObject row = _row != null ? _row : gameObject;
            row.SetActive(_service.IsAdminAvailable);
        }

        private Toggle Toggle => _toggle != null ? _toggle : _toggle = GetComponent<Toggle>();

        private void Start()
        {
            if (_service == null)
                Debug.LogError($"[Cheating] {name} was not injected, so it switches nothing. " +
                               "Instantiate it through the container.", this);
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void OnToggleChanged(bool isOn)
        {
            _service.SetAdminEnabled(isOn);

            // Refused (not available): put the toggle back.
            if (_service.IsAdminEnabled != isOn)
                Toggle.SetIsOnWithoutNotify(_service.IsAdminEnabled);
        }

        private void OnAdminEnabledChanged(bool isAdminEnabled)
        {
            if (this == null)
            {
                Unsubscribe();
                return;
            }

            Toggle.SetIsOnWithoutNotify(isAdminEnabled);
        }

        private void Unsubscribe()
        {
            if (_service != null)
                _service.AdminEnabledChanged -= OnAdminEnabledChanged;
        }
    }
}
