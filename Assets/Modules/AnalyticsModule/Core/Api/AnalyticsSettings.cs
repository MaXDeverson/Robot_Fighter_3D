namespace Game.Analytics
{
    /// <summary>
    /// Tuning of one analytics service. Immutable; build it once and bind the instance into the container.
    /// </summary>
    public sealed class AnalyticsSettings
    {
        /// <summary>Default number of events that may wait for consent.</summary>
        public const int DefaultConsentQueueCapacity = 256;

        /// <summary>Default number of events the debug history keeps.</summary>
        public const int DefaultHistoryCapacity = 256;

        /// <summary>Creates settings with the documented defaults: enabled, no consent required, history recorded.</summary>
        public AnalyticsSettings()
            : this(true, false, DefaultConsentQueueCapacity, true, DefaultHistoryCapacity)
        {
        }

        /// <summary>Creates settings.</summary>
        /// <param name="enabled">Master switch. Off, every tracked event is dropped before it reaches a sink.</param>
        /// <param name="requireConsent">Hold events until the player grants consent.</param>
        /// <param name="consentQueueCapacity">How many events may wait for consent. Values below one are raised to one.</param>
        /// <param name="recordHistory">Keep the ring buffer the debugger window reads.</param>
        /// <param name="historyCapacity">How many records the history keeps. Values below one are raised to one.</param>
        public AnalyticsSettings(
            bool enabled,
            bool requireConsent,
            int consentQueueCapacity,
            bool recordHistory,
            int historyCapacity)
        {
            Enabled = enabled;
            RequireConsent = requireConsent;
            ConsentQueueCapacity = consentQueueCapacity < 1 ? 1 : consentQueueCapacity;
            RecordHistory = recordHistory;
            HistoryCapacity = historyCapacity < 1 ? 1 : historyCapacity;
        }

        /// <summary>Master switch. Off, every tracked event is dropped before it reaches a sink.</summary>
        public bool Enabled { get; }

        /// <summary>Whether events wait for the player's explicit consent.</summary>
        public bool RequireConsent { get; }

        /// <summary>How many events may wait for consent before the oldest are discarded.</summary>
        public int ConsentQueueCapacity { get; }

        /// <summary>Whether the service keeps the history the debugger window reads.</summary>
        public bool RecordHistory { get; }

        /// <summary>How many records the history keeps.</summary>
        public int HistoryCapacity { get; }
    }
}
