using System;
using UnityEngine;

namespace Game.Events.DI
{
    /// <summary>
    /// Writes the service output to the Unity console.
    /// </summary>
    /// <remarks>
    /// The two flow methods are only reached from code the compiler strips outside the editor and development
    /// builds, and even there only when the settings turn flow logging on. The failure methods are always live:
    /// a handler that throws has to be visible in a shipping build too.
    /// </remarks>
    internal sealed class UnityGameEventsLogger : IGameEventsLogger
    {
        private const string Prefix = "[GameEvents] ";

        public void LogEvent(in EventPath path, Type eventType, int handlerCount, bool fromQueue)
        {
            Debug.Log(Prefix + (fromQueue ? "flush " : "publish ") + path.Full +
                      " (" + eventType.Name + ") -> " + handlerCount + " handler(s)");
        }

        public void LogSubscription(in EventPath path, bool subscribed, bool isBranch, string target)
        {
            Debug.Log(Prefix + (subscribed ? "+ " : "- ") + (isBranch ? "branch " : "typed ") +
                      path.ToString() + " : " + target);
        }

        public void LogWarning(string message)
        {
            Debug.LogWarning(Prefix + message);
        }

        public void LogError(string message, Exception exception)
        {
            Debug.LogError(Prefix + message);
            if (exception != null)
            {
                Debug.LogException(exception);
            }
        }
    }
}
