using System;

namespace Game.Analytics
{
    /// <summary>
    /// Where the service reports its own problems: rejected events, failing sinks. Injected so the core keeps no
    /// engine reference. Always live, in release builds too — a sink that throws has to be visible everywhere.
    /// </summary>
    public interface IAnalyticsLogger
    {
        /// <summary>Something worth knowing that did not stop anything.</summary>
        void LogWarning(string message);

        /// <summary>An event was rejected or a sink failed.</summary>
        /// <param name="message">Human readable description naming the event or the sink.</param>
        /// <param name="exception">The original exception, or <c>null</c>.</param>
        void LogError(string message, Exception exception);
    }
}
