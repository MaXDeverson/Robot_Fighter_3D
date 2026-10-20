using Game.Events;
using Game.Gameplay.Events;

namespace Game.Analytics.Gameplay
{
    /// <summary>
    /// The game's own lifecycle: its launch, as a design event on <c>Game_Launched</c>.
    /// </summary>
    public sealed class GameAnalyticsTracker : AnalyticsTracker
    {
        /// <summary>Creates the tracker.</summary>
        public GameAnalyticsTracker(IEventSubscriber events, IAnalytics analytics)
            : base(events, analytics)
        {
        }

        /// <inheritdoc />
        protected override void Subscribe()
        {
            Listen<GameLaunched>(OnGameLaunched);
        }

        private void OnGameLaunched(GameLaunched gameEvent)
        {
            Track(AnalyticsEvent.Design(TrackingPlan.Names.GameLaunched, TrackingPlan.GameLaunchedSegment));
        }
    }
}
