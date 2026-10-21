using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Game.Cheating.UI
{
    /// <summary>
    /// Runs a cheat on click: put it on a <see cref="Button"/> and pick the cheat. Whether the button is seen at all is up
    /// to a <see cref="CheatingContentActivator"/>; a click while the admin mode is off is refused anyway.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class CheatButton : MonoBehaviour
    {
        [Tooltip("The cheat the button runs.")]
        [SerializeField] private CheatDefinition _cheat;

        [Tooltip("Optional. Gets the cheat's title, so the button says what it does without being renamed by hand.")]
        [SerializeField] private TMP_Text _label;

        private ICheatingService _service;

        public CheatDefinition Cheat => _cheat;

        [Inject]
        private void Construct(ICheatingService service)
        {
            _service = service;
        }

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);

            if (_label != null && _cheat != null)
                _label.text = _cheat.Title;
        }

        private void OnClick()
        {
            if (_cheat == null)
            {
                Debug.LogError($"[Cheating] {name} has no cheat to run.", this);
                return;
            }

            if (_service == null)
            {
                Debug.LogError($"[Cheating] {name} was not injected, so '{_cheat.Title}' can not run. " +
                               "Instantiate it through the container.", this);
                return;
            }

            _service.Execute(_cheat);
        }
    }
}
