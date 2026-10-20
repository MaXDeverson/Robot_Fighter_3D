using System;
using System.Diagnostics;

namespace Game.Analytics
{
    /// <summary>Fallback clock of a service built without one: a stopwatch started with the service.</summary>
    internal sealed class SystemAnalyticsClock : IAnalyticsClock
    {
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        public double Now
        {
            get { return _stopwatch.Elapsed.TotalSeconds; }
        }

        public DateTime UtcNow
        {
            get { return DateTime.UtcNow; }
        }
    }
}
