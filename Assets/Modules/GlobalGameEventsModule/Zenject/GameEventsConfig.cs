using UnityEngine;

namespace Game.Events.DI
{
    /// <summary>
    /// Serialized tuning for one event service, shared by every installer that points at it.
    /// </summary>
    /// <remarks>
    /// Assign one of these to an <see cref="GlobalGameEventsInstaller"/> when several contexts should agree on the same
    /// limits, or leave the field empty and edit the installer's own fields for a one-off setup.
    /// </remarks>
    [CreateAssetMenu(fileName = "GameEventsConfig", menuName = "Game/Events/Game Events Config", order = 0)]
    public sealed class GameEventsConfig : ScriptableObject
    {
        [Header("Limits")]
        [SerializeField]
        [Min(1)]
        [Tooltip("How deep publishes may nest before the chain is treated as runaway recursion and throws.")]
        private int _maxDispatchDepth = GameEventsSettings.DefaultMaxDispatchDepth;

        [SerializeField]
        [Min(1)]
        [Tooltip("Upper bound on how many queued events a single late-tick flush delivers.")]
        private int _maxEventsPerFlush = GameEventsSettings.DefaultMaxEventsPerFlush;

        [Header("Debugging")]
        [SerializeField]
        [Min(1)]
        [Tooltip("How many events the debugger window can show.")]
        private int _historyCapacity = GameEventsSettings.DefaultHistoryCapacity;

        [SerializeField]
        [Tooltip("Record the event history. Costs one boxing allocation per published event. Editor and development builds only.")]
        private bool _recordHistory = true;

        [SerializeField]
        [Tooltip("Write every dispatch and subscription change to the console. Editor and development builds only.")]
        private bool _logEventFlow;

        [SerializeField]
        [Tooltip("Throw when the service is used off the thread that created it. Editor and development builds only.")]
        private bool _verifyMainThread = true;

        /// <summary>Builds the immutable settings the service is constructed with.</summary>
        public GameEventsSettings BuildSettings()
        {
            return new GameEventsSettings(
                _maxDispatchDepth,
                _historyCapacity,
                _maxEventsPerFlush,
                _logEventFlow,
                _recordHistory,
                _verifyMainThread);
        }
    }
}
