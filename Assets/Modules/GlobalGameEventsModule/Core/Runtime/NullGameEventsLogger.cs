using System;

namespace Game.Events
{
    /// <summary>
    /// Discards everything. Used when a service is built without a logger, mainly in core-only tests.
    /// </summary>
    /// <remarks>
    /// In a Unity project the installer binds a logger that writes to the console instead, because handler
    /// failures must stay visible even in a shipping build.
    /// </remarks>
    public sealed class NullGameEventsLogger : IGameEventsLogger
    {
        /// <inheritdoc />
        public void LogEvent(in EventPath path, Type eventType, int handlerCount, bool fromQueue)
        {
        }

        /// <inheritdoc />
        public void LogSubscription(in EventPath path, bool subscribed, bool isBranch, string target)
        {
        }

        /// <inheritdoc />
        public void LogWarning(string message)
        {
        }

        /// <inheritdoc />
        public void LogError(string message, Exception exception)
        {
        }
    }
}
