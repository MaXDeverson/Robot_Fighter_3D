namespace Game.Analytics
{
    /// <summary>
    /// Reports analytics events. The only analytics type gameplay code and trackers ever need.
    /// </summary>
    /// <remarks>
    /// It never says where events go. That is the sinks' business (<see cref="IAnalyticsSink"/>): the console today, a
    /// real backend later, several at once if need be — all without a line changing on this side.
    /// </remarks>
    public interface IAnalytics
    {
        /// <summary>
        /// Reports an event: stamps it, merges the global parameters in, validates it and delivers it to every sink,
        /// or holds it until the player's consent allows delivery.
        /// </summary>
        /// <param name="analyticsEvent">The event. A builder converts implicitly, so <c>Build()</c> can be left off.</param>
        void Track(AnalyticsEvent analyticsEvent);

        /// <summary>
        /// Adds a parameter to every event tracked from now on — the level being played, an A/B group — until it is
        /// removed. A parameter set on the event itself wins over a global one with the same key.
        /// </summary>
        void SetGlobalParameter(string key, AnalyticsValue value);

        /// <summary>Stops adding a global parameter.</summary>
        /// <returns>False when no global parameter had that key.</returns>
        bool RemoveGlobalParameter(string key);
    }
}
