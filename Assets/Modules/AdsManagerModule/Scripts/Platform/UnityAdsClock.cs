using System;
using UnityEngine;

namespace Game.Ads
{
    /// <summary>Unity's unscaled real time and the system clock.</summary>
    internal sealed class UnityAdsClock : IAdsClock
    {
        public double RealtimeSinceStartup
        {
            get { return Time.realtimeSinceStartupAsDouble; }
        }

        public DateTime UtcNow
        {
            get { return DateTime.UtcNow; }
        }
    }
}
