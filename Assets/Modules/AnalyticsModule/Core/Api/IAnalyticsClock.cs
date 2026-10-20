using System;

namespace Game.Analytics
{
    /// <summary>
    /// Time source for event stamps and the durations trackers measure. The Zenject integration binds Unity's unscaled
    /// clock; the core falls back to a stopwatch so it can be used and tested without an engine.
    /// </summary>
    public interface IAnalyticsClock
    {
        /// <summary>Seconds since a fixed start, monotonic and unaffected by time scale or pauses of the game.</summary>
        double Now { get; }

        /// <summary>Wall-clock time, for backends that want the moment an event happened rather than when it was sent.</summary>
        DateTime UtcNow { get; }
    }
}
