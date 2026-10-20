namespace Game.Analytics
{
    /// <summary>What became of a tracked event.</summary>
    public enum AnalyticsDeliveryStatus
    {
        /// <summary>Handed to every sink.</summary>
        Sent = 0,

        /// <summary>Waiting in the consent queue.</summary>
        Queued = 1,

        /// <summary>Discarded: analytics is disabled, consent was refused, or the consent queue overflowed.</summary>
        Dropped = 2,

        /// <summary>Failed validation and was never sent.</summary>
        Rejected = 3,
    }

    /// <summary>One tracked event and what became of it, as the debugger window shows it.</summary>
    public sealed class AnalyticsRecord
    {
        internal AnalyticsRecord(AnalyticsEvent analyticsEvent)
        {
            Event = analyticsEvent;
        }

        /// <summary>The event as tracked: stamped, with the global parameters merged in.</summary>
        public AnalyticsEvent Event { get; }

        /// <summary>What became of it so far. A queued event moves on once the player answers.</summary>
        public AnalyticsDeliveryStatus Status { get; internal set; }

        /// <summary>Why it was rejected or dropped, or which sinks failed on it; <c>null</c> when there is nothing to say.</summary>
        public string Note { get; internal set; }

        /// <summary>Kind, hierarchy and value on one line: <c>Progression:Complete  Level_003  score=3</c>.</summary>
        public string Headline { get; internal set; }

        /// <summary>The parameters rendered as <c>key=value</c> pairs, text quoted.</summary>
        public string ParametersText { get; internal set; }
    }

    /// <summary>
    /// Ring buffer of the last tracked events, for the editor window. The Zenject integration only records it in the
    /// editor and in development builds.
    /// </summary>
    public interface IAnalyticsHistory
    {
        /// <summary>How many records the buffer holds before it starts overwriting the oldest.</summary>
        int Capacity { get; }

        /// <summary>How many records are stored.</summary>
        int Count { get; }

        /// <summary>Records in chronological order, index zero being the oldest still kept.</summary>
        /// <param name="index">Zero based index below <see cref="Count"/>.</param>
        AnalyticsRecord this[int index] { get; }

        /// <summary>Drops every record.</summary>
        void Clear();
    }
}
