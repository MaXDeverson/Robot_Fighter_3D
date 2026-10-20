using System;
using UnityEngine;

namespace Game.Analytics.DI
{
    /// <summary>
    /// Unity's unscaled real time, so stamps and durations stay true while the game is paused or in slow motion.
    /// </summary>
    internal sealed class UnityAnalyticsClock : IAnalyticsClock
    {
        public double Now
        {
            get { return Time.realtimeSinceStartupAsDouble; }
        }

        public DateTime UtcNow
        {
            get { return DateTime.UtcNow; }
        }
    }
}
