using System.Globalization;

namespace Game.Analytics.Gameplay
{
    /// <summary>
    /// The vocabulary this game reports in: event names, parameter keys and hierarchy segments, each spelled once.
    /// </summary>
    /// <remarks>
    /// Dashboards are built on these strings. Renaming a constant renames it in the data, and reports made before the
    /// rename stop matching the data after it — change them on purpose, not in passing.
    /// </remarks>
    public static class TrackingPlan
    {
        /// <summary>Event names.</summary>
        public static class Names
        {
            /// <summary>A level was loaded and is being played.</summary>
            public const string LevelStart = "level_start";

            /// <summary>A level was completed.</summary>
            public const string LevelComplete = "level_complete";
        }

        /// <summary>Parameter keys.</summary>
        public static class Keys
        {
            /// <summary>Index of the level, as the game counts them: 0 for the first.</summary>
            public const string Level = "level";
        }

        /// <summary>The hierarchy segment of a started level: <c>Level_Start3</c> for level index 3.</summary>
        public static string LevelStartSegment(int levelIndex)
        {
            return "Level_Start" + levelIndex.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>The hierarchy segment of a completed level: <c>Level_Complete3</c> for level index 3.</summary>
        public static string LevelCompleteSegment(int levelIndex)
        {
            return "Level_Complete" + levelIndex.ToString(CultureInfo.InvariantCulture);
        }
    }
}
