using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

namespace Game.Cheating.UI
{
    /// <summary>
    /// Switches objects by the admin mode: shows one list — cheat buttons, debug toggles, whole panels — only while the
    /// mode is on, and the other — what a player sees in their place — only while it is off. No code per element: put it
    /// on the element or a parent and list what it switches.
    /// <para>
    /// It follows the admin mode from injection on, also while its own object is switched off, so it may list itself and
    /// the listed objects are right whenever their window opens. Must be injected — a scene object, or instantiated
    /// through the container.
    /// </para>
    /// </summary>
    public sealed class CheatingContentActivator : MonoBehaviour
    {
        [Tooltip("Active only while the admin mode is on. May include this object. Keep them inactive in the prefab.")]
        [FormerlySerializedAs("_targets")]
        [SerializeField] private GameObject[] _shownInAdmin = new GameObject[0];

        [Tooltip("Active only while the admin mode is off. May include this object. Keep them active in the prefab.")]
        [SerializeField] private GameObject[] _hiddenInAdmin = new GameObject[0];

        private ICheatingService _service;

        [Inject]
        private void Construct(ICheatingService service)
        {
            Unsubscribe();

            _service = service;
            _service.AdminEnabledChanged += OnAdminEnabledChanged;
            Apply(_service.IsAdminEnabled);
        }

        private void Start()
        {
            if (_service == null)
                Debug.LogError($"[Cheating] {name} was not injected, so its objects stay as they are. " +
                               "Instantiate it through the container.", this);
        }

        // Not called for an object that was never active; OnAdminEnabledChanged unsubscribes by itself then.
        private void OnDestroy()
        {
            Unsubscribe();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_shownInAdmin == null || _hiddenInAdmin == null)
                return;

            foreach (GameObject shown in _shownInAdmin)
            {
                if (shown != null && System.Array.IndexOf(_hiddenInAdmin, shown) >= 0)
                    Debug.LogWarning($"[Cheating] {name} lists '{shown.name}' both to show and to hide in the admin " +
                                     "mode; it ends up hidden while the mode is on.", this);
            }
        }
#endif

        /// <summary>Switches both lists to the state of the admin mode.</summary>
        internal void Apply(bool isAdminEnabled)
        {
            SetActive(_shownInAdmin, isAdminEnabled);
            SetActive(_hiddenInAdmin, !isAdminEnabled);
        }

        private static void SetActive(GameObject[] targets, bool active)
        {
            if (targets == null)
                return;

            foreach (GameObject target in targets)
            {
                if (target != null && target.activeSelf != active)
                    target.SetActive(active);
            }
        }

        private void OnAdminEnabledChanged(bool isAdminEnabled)
        {
            // Destroyed with its scene while the service lives on in the project context.
            if (this == null)
            {
                Unsubscribe();
                return;
            }

            Apply(isAdminEnabled);
        }

        private void Unsubscribe()
        {
            if (_service != null)
                _service.AdminEnabledChanged -= OnAdminEnabledChanged;
        }
    }
}
