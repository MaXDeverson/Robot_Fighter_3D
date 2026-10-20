namespace Game.Events
{
    /// <summary>
    /// Tuning knobs for one service. Immutable; build it once and bind the instance into the container.
    /// </summary>
    public sealed class GameEventsSettings
    {
        /// <summary>Default nesting limit before a publish chain is treated as runaway recursion.</summary>
        public const int DefaultMaxDispatchDepth = 8;

        /// <summary>Default number of events the debug history keeps.</summary>
        public const int DefaultHistoryCapacity = 256;

        /// <summary>Default upper bound on how many events one flush delivers.</summary>
        public const int DefaultMaxEventsPerFlush = 1024;

        /// <summary>Creates settings with the documented defaults.</summary>
        public GameEventsSettings()
            : this(DefaultMaxDispatchDepth, DefaultHistoryCapacity, DefaultMaxEventsPerFlush, false, true, true)
        {
        }

        /// <summary>Creates settings.</summary>
        /// <param name="maxDispatchDepth">How deep publishes may nest. Values below one are raised to one.</param>
        /// <param name="historyCapacity">How many records the debug history keeps.</param>
        /// <param name="maxEventsPerFlush">Upper bound on one flush, a guard against a runaway producer.</param>
        /// <param name="logEventFlow">Write every dispatch to the logger. Editor and development builds only.</param>
        /// <param name="recordHistory">Keep the ring buffer the debugger window reads.</param>
        /// <param name="verifyMainThread">Throw when the service is touched off the thread that created it.</param>
        public GameEventsSettings(
            int maxDispatchDepth,
            int historyCapacity,
            int maxEventsPerFlush,
            bool logEventFlow,
            bool recordHistory,
            bool verifyMainThread)
        {
            MaxDispatchDepth = maxDispatchDepth < 1 ? 1 : maxDispatchDepth;
            HistoryCapacity = historyCapacity < 1 ? 1 : historyCapacity;
            MaxEventsPerFlush = maxEventsPerFlush < 1 ? 1 : maxEventsPerFlush;
            LogEventFlow = logEventFlow;
            RecordHistory = recordHistory;
            VerifyMainThread = verifyMainThread;
        }

        /// <summary>How deep publishes may nest before <see cref="GameEventsRecursionException"/> is raised.</summary>
        public int MaxDispatchDepth { get; }

        /// <summary>How many records the debug history keeps.</summary>
        public int HistoryCapacity { get; }

        /// <summary>Upper bound on how many events a single flush delivers.</summary>
        public int MaxEventsPerFlush { get; }

        /// <summary>Whether the service writes every dispatch to the logger.</summary>
        public bool LogEventFlow { get; }

        /// <summary>Whether the history service is installed.</summary>
        public bool RecordHistory { get; }

        /// <summary>Whether the service verifies the calling thread outside release builds.</summary>
        public bool VerifyMainThread { get; }
    }
}
