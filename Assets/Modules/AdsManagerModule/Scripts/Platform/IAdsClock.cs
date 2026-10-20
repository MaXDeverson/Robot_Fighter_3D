using System;

namespace Game.Ads
{
    /// <summary>The two clocks the ads manager measures with.</summary>
    internal interface IAdsClock
    {
        /// <summary>
        /// Seconds of real time since the game started: not slowed by <c>Time.timeScale</c>, not reset by a scene load.
        /// Measures the interstitial cooldown.
        /// </summary>
        double RealtimeSinceStartup { get; }

        /// <summary>The device's wall clock, in UTC. Measures the ad block, which has to run while the game is closed.</summary>
        DateTime UtcNow { get; }
    }
}
