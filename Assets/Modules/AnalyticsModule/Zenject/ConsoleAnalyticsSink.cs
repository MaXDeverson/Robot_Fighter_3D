using UnityEngine;

namespace Game.Analytics.DI
{
    /// <summary>
    /// Writes every event to the Unity console. The stand-in backend until a real one is integrated, and still useful
    /// next to one afterwards.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A line looks like
    /// <c>[Analytics] #12 level_complete  Progression:Complete  Level_003  score=3  {duration=42.5 stars=3}</c>.
    /// </para>
    /// <para>
    /// In the editor the line carries its stack trace, so it opens like any other log: a double-click lands here, and
    /// the frames under the message lead to the tracker that built the event and, for a gameplay event that was
    /// published rather than queued, on to the code that raised it. On a device the trace would only bloat the log, so
    /// it is left off there.
    /// </para>
    /// </remarks>
    public sealed class ConsoleAnalyticsSink : IAnalyticsSink
    {
        private const string Prefix = "[Analytics] ";

        private readonly AnalyticsEventFormatter _formatter = new AnalyticsEventFormatter();
        private bool? _collectionEnabled;

        /// <inheritdoc />
        public string Name
        {
            get { return "Console"; }
        }

        /// <inheritdoc />
        public void Initialize()
        {
        }

        /// <inheritdoc />
        public void Send(AnalyticsEvent analyticsEvent)
        {
            Write(Prefix + _formatter.FormatLine(analyticsEvent));
        }

        /// <inheritdoc />
        public void SetCollectionEnabled(bool enabled)
        {
            // The first call only reports the starting state, which is not news; a change later on is.
            bool changed = _collectionEnabled.HasValue && _collectionEnabled.Value != enabled;
            _collectionEnabled = enabled;
            if (changed)
            {
                Write(Prefix + (enabled ? "collection enabled" : "collection disabled"));
            }
        }

        /// <inheritdoc />
        public void Flush()
        {
        }

        /// <inheritdoc />
        public void Shutdown()
        {
        }

        private static void Write(string line)
        {
#if UNITY_EDITOR
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "{0}", line);
#endif
        }
    }
}
