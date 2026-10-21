using UnityEngine;

namespace Game.Privacy
{
    /// <summary><see cref="Time.realtimeSinceStartupAsDouble"/>.</summary>
    internal sealed class UnityPrivacyClock : IPrivacyClock
    {
        public double RealtimeSinceStartup
        {
            get { return Time.realtimeSinceStartupAsDouble; }
        }
    }
}
