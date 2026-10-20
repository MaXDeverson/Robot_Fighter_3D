using System;
using System.Collections.Generic;

namespace Game.Analytics
{
    /// <summary>
    /// One analytics event: what happened, what it happened to, and the numbers around it. Immutable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Built with <see cref="Design"/>, <see cref="Progression"/> or <see cref="Ad"/> and tracked through
    /// <see cref="IAnalytics"/>:
    /// <code>
    /// analytics.Track(AnalyticsEvent
    ///     .Progression("level_complete", ProgressionStatus.Complete, "Level_003")
    ///     .WithScore(3)
    ///     .With("duration", 42.5));
    /// </code>
    /// </para>
    /// <para>
    /// The same event has to make sense to very different backends, so it describes itself at two levels of
    /// structure. <see cref="Name"/> is the flat, snake case name a Firebase-style backend sends as is and the debugger
    /// filters on. <see cref="Category"/>, <see cref="ProgressionStatus"/>, <see cref="AdDetails"/>,
    /// <see cref="Hierarchy"/> and <see cref="Value"/> are the structured form a GameAnalytics-style backend is built
    /// on. A sink reads whichever half its backend understands; the parameters go to both.
    /// </para>
    /// <para>
    /// Tracking an event produces a new instance, stamped with a sequence number and a time and carrying the global
    /// parameters; the draft that was passed in is left untouched.
    /// </para>
    /// </remarks>
    public sealed class AnalyticsEvent
    {
        /// <summary>Separator between hierarchy segments in <see cref="HierarchyPath"/>.</summary>
        public const string HierarchySeparator = ":";

        private readonly string[] _hierarchy;
        private readonly AnalyticsParameter[] _parameters;

        internal AnalyticsEvent(
            string name,
            AnalyticsCategory category,
            ProgressionStatus progressionStatus,
            AdDetails adDetails,
            string[] hierarchy,
            double? value,
            AnalyticsParameter[] parameters,
            string source,
            long sequence,
            double time,
            DateTime utcTime)
        {
            Name = name ?? string.Empty;
            Category = category;
            ProgressionStatus = progressionStatus;
            AdDetails = adDetails;
            _hierarchy = hierarchy ?? Array.Empty<string>();
            HierarchyPath = string.Join(HierarchySeparator, _hierarchy);
            Value = value;
            _parameters = parameters ?? Array.Empty<AnalyticsParameter>();
            Source = source;
            Sequence = sequence;
            Time = time;
            UtcTime = utcTime;
        }

        /// <summary>Flat lower snake case name, for example <c>level_complete</c>.</summary>
        public string Name { get; }

        /// <summary>What kind of moment this is.</summary>
        public AnalyticsCategory Category { get; }

        /// <summary>Start, complete or fail for a progression; <see cref="Analytics.ProgressionStatus.None"/> otherwise.</summary>
        public ProgressionStatus ProgressionStatus { get; }

        /// <summary>
        /// The action, type, placement and network of an ad event; <c>null</c> for every other kind.
        /// </summary>
        public AdDetails AdDetails { get; }

        /// <summary>
        /// What the event is about, from the general to the specific: <c>Level_003</c>, or
        /// <c>Death</c> / <c>Level_003</c> / <c>Acid</c>. A structured backend reports and drills down by it.
        /// </summary>
        public IReadOnlyList<string> Hierarchy
        {
            get { return _hierarchy; }
        }

        /// <summary>The hierarchy joined with <see cref="HierarchySeparator"/>: <c>Death:Level_003:Acid</c>.</summary>
        public string HierarchyPath { get; }

        /// <summary>The score of a progression or the value of a design event; <c>null</c> when there is none.</summary>
        public double? Value { get; }

        /// <summary>The custom fields, in the order they were added; global parameters come last.</summary>
        public IReadOnlyList<AnalyticsParameter> Parameters
        {
            get { return _parameters; }
        }

        /// <summary>What produced the event — the path of the gameplay event a tracker turned into it — or <c>null</c>.</summary>
        public string Source { get; }

        /// <summary>Position in the order events were tracked, starting at 1. Zero on a draft that was not tracked yet.</summary>
        public long Sequence { get; }

        /// <summary>When it was tracked, in seconds of the analytics clock (unscaled, since start-up).</summary>
        public double Time { get; }

        /// <summary>When it was tracked, as wall-clock time.</summary>
        public DateTime UtcTime { get; }

        /// <summary>Whether this is a tracked event rather than a draft.</summary>
        public bool IsTracked
        {
            get { return Sequence > 0; }
        }

        /// <summary>Starts a design event.</summary>
        /// <param name="name">Lower snake case name, for example <c>player_death</c>.</param>
        /// <param name="hierarchy">One to five segments, general to specific.</param>
        public static AnalyticsEventBuilder Design(string name, params string[] hierarchy)
        {
            return new AnalyticsEventBuilder(name, AnalyticsCategory.Design, ProgressionStatus.None, hierarchy);
        }

        /// <summary>Starts a progression event.</summary>
        /// <param name="name">Lower snake case name, for example <c>level_start</c>.</param>
        /// <param name="status">Start, complete or fail.</param>
        /// <param name="hierarchy">One to three segments, general to specific.</param>
        public static AnalyticsEventBuilder Progression(string name, ProgressionStatus status, params string[] hierarchy)
        {
            return new AnalyticsEventBuilder(name, AnalyticsCategory.Progression, status, hierarchy);
        }

        /// <summary>
        /// Starts an ad event. Its hierarchy is the type and the placement, <c>Interstitial:level_fail</c>; a failed show
        /// adds its reason with <see cref="AnalyticsEventBuilder.WithFailureReason"/>.
        /// </summary>
        /// <param name="name">Lower snake case name, for example <c>ad_show</c>.</param>
        /// <param name="action">What happened to the ad.</param>
        /// <param name="type">Which kind of ad it is.</param>
        /// <param name="placement">Where in the game it was shown, for example <c>level_fail</c>.</param>
        /// <param name="network">The ad network that served it, lower case, for example <c>admob</c>.</param>
        public static AnalyticsEventBuilder Ad(string name, AdAction action, AdType type, string placement, string network)
        {
            return new AnalyticsEventBuilder(name, action, type, placement, network);
        }

        /// <summary>Builds the event a builder describes, so a builder can be passed wherever an event is expected.</summary>
        public static implicit operator AnalyticsEvent(AnalyticsEventBuilder builder)
        {
            return builder == null ? null : builder.Build();
        }

        /// <summary>Looks a parameter up by key.</summary>
        public bool TryGetParameter(string key, out AnalyticsValue value)
        {
            for (int i = 0; i < _parameters.Length; i++)
            {
                if (string.Equals(_parameters[i].Key, key, StringComparison.Ordinal))
                {
                    value = _parameters[i].Value;
                    return true;
                }
            }

            value = default(AnalyticsValue);
            return false;
        }

        /// <summary>Name and hierarchy, for logs.</summary>
        public override string ToString()
        {
            return HierarchyPath.Length == 0 ? Name : Name + " " + HierarchyPath;
        }

        /// <summary>The parameters as the array they are kept in, so the service can reuse it without a copy.</summary>
        internal AnalyticsParameter[] ParameterArray
        {
            get { return _parameters; }
        }

        /// <summary>The tracked copy of this draft.</summary>
        internal AnalyticsEvent Stamp(long sequence, double time, DateTime utcTime, AnalyticsParameter[] parameters)
        {
            return new AnalyticsEvent(
                Name, Category, ProgressionStatus, AdDetails, _hierarchy, Value, parameters, Source, sequence, time,
                utcTime);
        }
    }
}
