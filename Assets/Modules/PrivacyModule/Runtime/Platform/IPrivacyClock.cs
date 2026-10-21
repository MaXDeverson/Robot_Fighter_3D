namespace Game.Privacy
{
    /// <summary>Real time for the gather timeout, so the tests can move it.</summary>
    internal interface IPrivacyClock
    {
        /// <summary>Seconds since the start, not scaled and not paused.</summary>
        double RealtimeSinceStartup { get; }
    }
}
