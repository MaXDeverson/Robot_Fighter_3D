using System;
using UnityEngine;

namespace Game.Analytics.DI
{
    /// <summary>Writes the service's own problems to the Unity console.</summary>
    internal sealed class UnityAnalyticsLogger : IAnalyticsLogger
    {
        private const string Prefix = "[Analytics] ";

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
