using System.Collections.Generic;

namespace Game.Analytics
{
    /// <summary>A sink as the debugger window sees it.</summary>
    public readonly struct AnalyticsSinkInfo
    {
        /// <summary>Creates the record.</summary>
        public AnalyticsSinkInfo(string name, bool isActive, int sentCount, int failedCount, string lastError)
        {
            Name = name;
            IsActive = isActive;
            SentCount = sentCount;
            FailedCount = failedCount;
            LastError = lastError;
        }

        /// <summary>The sink's <see cref="IAnalyticsSink.Name"/>.</summary>
        public string Name { get; }

        /// <summary>False when the sink failed to initialize and is being skipped.</summary>
        public bool IsActive { get; }

        /// <summary>Events it accepted.</summary>
        public int SentCount { get; }

        /// <summary>Calls that threw.</summary>
        public int FailedCount { get; }

        /// <summary>The last exception it threw, as text, or <c>null</c>.</summary>
        public string LastError { get; }
    }

    /// <summary>
    /// Read-only view of the service for tooling. Bound alongside <see cref="IAnalytics"/>, so the editor window can
    /// resolve it from a context's container.
    /// </summary>
    public interface IAnalyticsDiagnostics
    {
        /// <summary>The master switch from the settings.</summary>
        bool IsEnabled { get; }

        /// <summary>Every <see cref="IAnalytics.Track"/> call so far.</summary>
        int TrackedCount { get; }

        /// <summary>Events handed to the sinks.</summary>
        int SentCount { get; }

        /// <summary>Events waiting for consent right now.</summary>
        int QueuedCount { get; }

        /// <summary>Events discarded without being sent.</summary>
        int DroppedCount { get; }

        /// <summary>Events that failed validation.</summary>
        int RejectedCount { get; }

        /// <summary>The recorded events, or <c>null</c> when recording is off.</summary>
        IAnalyticsHistory History { get; }

        /// <summary>Fills <paramref name="buffer"/> with one entry per attached sink. Cleared first.</summary>
        void CopySinks(List<AnalyticsSinkInfo> buffer);

        /// <summary>Fills <paramref name="buffer"/> with the global parameters. Cleared first.</summary>
        void CopyGlobalParameters(List<AnalyticsParameter> buffer);
    }
}
