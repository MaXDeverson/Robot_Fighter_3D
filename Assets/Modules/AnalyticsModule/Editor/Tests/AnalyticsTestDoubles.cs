using System;
using System.Collections.Generic;

namespace Game.Analytics.Tests
{
    /// <summary>Keeps every event it is sent and counts its lifecycle calls; can be told to throw.</summary>
    internal sealed class RecordingAnalyticsSink : IAnalyticsSink
    {
        internal RecordingAnalyticsSink(string name = "Recording")
        {
            Name = name;
        }

        public string Name { get; }

        internal List<AnalyticsEvent> Events { get; } = new List<AnalyticsEvent>();

        internal List<bool> CollectionStates { get; } = new List<bool>();

        internal int InitializeCalls { get; private set; }

        internal int FlushCalls { get; private set; }

        internal int ShutdownCalls { get; private set; }

        internal bool ThrowOnInitialize { get; set; }

        internal bool ThrowOnSend { get; set; }

        internal AnalyticsEvent Last
        {
            get { return Events[Events.Count - 1]; }
        }

        public void Initialize()
        {
            InitializeCalls++;
            if (ThrowOnInitialize)
            {
                throw new InvalidOperationException("The backend is down.");
            }
        }

        public void Send(AnalyticsEvent analyticsEvent)
        {
            if (ThrowOnSend)
            {
                throw new InvalidOperationException("The backend refused the event.");
            }

            Events.Add(analyticsEvent);
        }

        public void SetCollectionEnabled(bool enabled)
        {
            CollectionStates.Add(enabled);
        }

        public void Flush()
        {
            FlushCalls++;
        }

        public void Shutdown()
        {
            ShutdownCalls++;
        }

        internal List<string> Names()
        {
            List<string> names = new List<string>(Events.Count);
            for (int i = 0; i < Events.Count; i++)
            {
                names.Add(Events[i].Name);
            }

            return names;
        }
    }

    /// <summary>A clock the test moves by hand.</summary>
    internal sealed class ManualAnalyticsClock : IAnalyticsClock
    {
        public double Now { get; set; }

        public DateTime UtcNow { get; set; } = new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc);
    }

    /// <summary>
    /// Captures what the service reports instead of writing to the console, so a test can assert on a rejected event
    /// without the Test Runner failing it for a logged error.
    /// </summary>
    internal sealed class RecordingAnalyticsLogger : IAnalyticsLogger
    {
        internal List<string> Warnings { get; } = new List<string>();

        internal List<string> Errors { get; } = new List<string>();

        public void LogWarning(string message)
        {
            Warnings.Add(message);
        }

        public void LogError(string message, Exception exception)
        {
            Errors.Add(message);
        }
    }
}
