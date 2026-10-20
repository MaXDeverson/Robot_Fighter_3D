using System;
using System.Collections.Generic;

namespace Game.Events.Tests
{
    /// <summary>
    /// Captures what the service reports instead of writing to the Unity console, so a test can assert on failures
    /// without the Test Runner treating a logged error as a test failure.
    /// </summary>
    internal sealed class RecordingGameEventsLogger : IGameEventsLogger
    {
        internal List<string> Warnings { get; } = new List<string>();

        internal List<string> Errors { get; } = new List<string>();

        internal List<Exception> Exceptions { get; } = new List<Exception>();

        public void LogEvent(in EventPath path, Type eventType, int handlerCount, bool fromQueue)
        {
        }

        public void LogSubscription(in EventPath path, bool subscribed, bool isBranch, string target)
        {
        }

        public void LogWarning(string message)
        {
            Warnings.Add(message);
        }

        public void LogError(string message, Exception exception)
        {
            Errors.Add(message);
            if (exception != null)
            {
                Exceptions.Add(exception);
            }
        }
    }
}
