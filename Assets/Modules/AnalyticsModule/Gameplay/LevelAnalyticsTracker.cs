using Game.Events;
using Game.Gameplay.Events;

namespace Game.Analytics.Gameplay
{
    /// <summary>
    /// Levels started and completed, as progression events on <c>Level_Start&lt;index&gt;</c> and
    /// <c>Level_Complete&lt;index&gt;</c>.
    /// </summary>
    public sealed class LevelAnalyticsTracker : AnalyticsTracker
    {
        /// <summary>Creates the tracker.</summary>
        public LevelAnalyticsTracker(IEventSubscriber events, IAnalytics analytics)
            : base(events, analytics)
        {
        }

        /// <inheritdoc />
        protected override void Subscribe()
        {
            Listen<LevelStarted>(OnLevelStarted);
            Listen<LevelCompleted>(OnLevelCompleted);
        }

        private void OnLevelStarted(LevelStarted gameEvent)
        {
            Track(AnalyticsEvent
                .Progression(
                    TrackingPlan.Names.LevelStart,
                    ProgressionStatus.Start,
                    TrackingPlan.LevelStartSegment(gameEvent.LevelIndex))
                .With(TrackingPlan.Keys.Level, gameEvent.LevelIndex));
        }

        private void OnLevelCompleted(LevelCompleted gameEvent)
        {
            Track(AnalyticsEvent
                .Progression(
                    TrackingPlan.Names.LevelComplete,
                    ProgressionStatus.Complete,
                    TrackingPlan.LevelCompleteSegment(gameEvent.LevelIndex))
                .With(TrackingPlan.Keys.Level, gameEvent.LevelIndex));
        }
    }
}
