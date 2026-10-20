using System.Diagnostics;

namespace Game.Events
{
    /// <summary>
    /// A clock backed by a stopwatch owned by this instance, so the core can timestamp history without an
    /// engine reference. The Zenject integration replaces it with one reading Unity's clock.
    /// </summary>
    public sealed class SystemEventClock : IEventClock
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();

        /// <summary>Starts the clock.</summary>
        public SystemEventClock()
        {
            _stopwatch.Start();
        }

        /// <inheritdoc />
        public double Now
        {
            get { return _stopwatch.Elapsed.TotalSeconds; }
        }
    }
}
