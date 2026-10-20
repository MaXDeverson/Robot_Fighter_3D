using UnityEngine;

namespace Game.Analytics.DI
{
    /// <summary>When the <see cref="ConsoleAnalyticsSink"/> is installed.</summary>
    public enum AnalyticsConsoleOutput
    {
        /// <summary>Never. Events still reach every other sink and the debugger window.</summary>
        Off = 0,

        /// <summary>In the editor and in development builds.</summary>
        EditorAndDevelopmentBuilds = 1,

        /// <summary>Everywhere, release builds included.</summary>
        Always = 2,
    }

    /// <summary>
    /// What an <see cref="AnalyticsInstaller"/> sets up: collection, consent, console output and debugging.
    /// </summary>
    [CreateAssetMenu(fileName = "AnalyticsConfig", menuName = "Game/Analytics/Analytics Config", order = 0)]
    public sealed class AnalyticsConfig : ScriptableObject
    {
        [Header("Collection")]
        [SerializeField]
        [Tooltip("Master switch. Off, every tracked event is dropped before it reaches any sink.")]
        private bool _enabled = true;

        [SerializeField]
        [Tooltip("Hold every event until the player grants consent (GDPR / ATT). Off, events flow until the player " +
                 "opts out.")]
        private bool _requireConsent;

        [SerializeField]
        [Min(1)]
        [Tooltip("How many events may wait for consent before the oldest are discarded.")]
        private int _consentQueueCapacity = AnalyticsSettings.DefaultConsentQueueCapacity;

        [Header("Console sink")]
        [SerializeField]
        [Tooltip("When every event is also written to the Unity console.")]
        private AnalyticsConsoleOutput _consoleOutput = AnalyticsConsoleOutput.EditorAndDevelopmentBuilds;

        [Header("Debugging")]
        [SerializeField]
        [Tooltip("Keep the last events for Window > Analytics Debugger. Only ever recorded in the editor and in " +
                 "development builds.")]
        private bool _recordHistory = true;

        [SerializeField]
        [Min(1)]
        [Tooltip("How many events the debugger window can show.")]
        private int _historyCapacity = AnalyticsSettings.DefaultHistoryCapacity;

        /// <summary>When the console sink is installed.</summary>
        public AnalyticsConsoleOutput ConsoleOutput
        {
            get { return _consoleOutput; }
        }

        /// <summary>Builds the immutable settings the service is constructed with.</summary>
        /// <param name="isDebugBuild">
        /// Whether this is the editor or a development build. The history is never recorded outside of those.
        /// </param>
        public AnalyticsSettings BuildSettings(bool isDebugBuild)
        {
            return new AnalyticsSettings(
                _enabled,
                _requireConsent,
                _consentQueueCapacity,
                _recordHistory && isDebugBuild,
                _historyCapacity);
        }
    }
}
