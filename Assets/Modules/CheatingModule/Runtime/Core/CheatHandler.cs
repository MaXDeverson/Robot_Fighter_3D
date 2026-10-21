using System;
using UnityEngine;
using Zenject;

namespace Game.Cheating
{
    /// <summary>
    /// Says how a cheat is done in this scene: derive, implement <see cref="Execute"/>, put the component in the scene
    /// and pick the cheat. It registers itself for as long as it lives, so the cheat works exactly where it has a handler.
    /// <para>
    /// Must be injected — a scene object, or instantiated through the container.
    /// </para>
    /// </summary>
    public abstract class CheatHandler : MonoBehaviour
    {
        [Tooltip("The cheat this component carries out.")]
        [SerializeField] private CheatDefinition _cheat;

        private ICheatingService _service;
        private IDisposable _registration;

        public CheatDefinition Cheat => _cheat;

        [Inject]
        private void Construct(ICheatingService service)
        {
            _service = service;
        }

        protected virtual void Start()
        {
            if (_cheat == null)
            {
                Debug.LogError($"[Cheating] {name} has no cheat to handle.", this);
                return;
            }

            if (_service == null)
            {
                Debug.LogError($"[Cheating] {name} was not injected, so '{_cheat.Title}' does nothing here. " +
                               "Instantiate it through the container.", this);
                return;
            }

            _registration = _service.Register(_cheat, Execute);
        }

        protected virtual void OnDestroy()
        {
            _registration?.Dispose();
            _registration = null;
        }

        /// <summary>Does the cheat. Runs only while the admin mode is on.</summary>
        protected abstract void Execute();
    }
}
