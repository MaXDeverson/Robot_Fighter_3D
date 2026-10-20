using System;

namespace Game.Analytics
{
    /// <summary>Discards everything. The fallback of a service built without a logger.</summary>
    internal sealed class NullAnalyticsLogger : IAnalyticsLogger
    {
        public void LogWarning(string message)
        {
        }

        public void LogError(string message, Exception exception)
        {
        }
    }
}
