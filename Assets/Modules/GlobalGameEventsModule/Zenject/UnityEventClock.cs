using UnityEngine;

namespace Game.Events.DI
{
    /// <summary>
    /// Timestamps the event history with Unity's unscaled real time, so the debugger stays readable while the
    /// game is paused or running in slow motion.
    /// </summary>
    internal sealed class UnityEventClock : IEventClock
    {
        public double Now
        {
            get { return Time.realtimeSinceStartupAsDouble; }
        }
    }
}
