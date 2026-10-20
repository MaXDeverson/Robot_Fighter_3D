namespace Game.Events
{
    /// <summary>
    /// Supplies timestamps for the event history. The Zenject integration binds an implementation backed by
    /// Unity's clock; the core falls back to a stopwatch so it can be used and tested without an engine.
    /// </summary>
    public interface IEventClock
    {
        /// <summary>Seconds since the clock started.</summary>
        double Now { get; }
    }
}
