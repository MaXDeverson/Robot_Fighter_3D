using System;
using System.Collections.Generic;

namespace Game.Analytics
{
    /// <summary>
    /// Collects the parts of an <see cref="AnalyticsEvent"/>. Start one with <see cref="AnalyticsEvent.Design"/>,
    /// <see cref="AnalyticsEvent.Progression"/> or <see cref="AnalyticsEvent.Ad"/>.
    /// </summary>
    /// <remarks>
    /// Nothing is checked here. A malformed event is reported by the service when it is tracked, where the debugger
    /// shows it next to the events that did go out, instead of throwing at the call site in the middle of gameplay.
    /// </remarks>
    public sealed class AnalyticsEventBuilder
    {
        private readonly List<AnalyticsParameter> _parameters = new List<AnalyticsParameter>();
        private readonly string _name;
        private readonly AnalyticsCategory _category;
        private readonly ProgressionStatus _status;
        private readonly string[] _hierarchy;
        private readonly AdAction _adAction;
        private readonly AdType _adType;
        private readonly string _adPlacement;
        private readonly string _adNetwork;

        private AdFailureReason _adFailureReason;
        private double? _value;
        private string _source;

        internal AnalyticsEventBuilder(string name, AnalyticsCategory category, ProgressionStatus status, string[] hierarchy)
        {
            _name = name;
            _category = category;
            _status = status;

            // Copied, so changing the array afterwards cannot reach into an event that was already built.
            _hierarchy = hierarchy == null ? Array.Empty<string>() : (string[])hierarchy.Clone();
        }

        internal AnalyticsEventBuilder(string name, AdAction action, AdType type, string placement, string network)
            : this(name, AnalyticsCategory.Ad, ProgressionStatus.None, new[] { type.ToString(), placement })
        {
            _adAction = action;
            _adType = type;
            _adPlacement = placement;
            _adNetwork = network;
        }

        /// <summary>Sets the value of a design event, or the score of a progression (a whole number there).</summary>
        public AnalyticsEventBuilder WithValue(double value)
        {
            _value = value;
            return this;
        }

        /// <summary>Sets the score of a progression.</summary>
        public AnalyticsEventBuilder WithScore(int score)
        {
            _value = score;
            return this;
        }

        /// <summary>Sets why an ad could not be shown. Only a <see cref="AdAction.FailedShow"/> carries one.</summary>
        public AnalyticsEventBuilder WithFailureReason(AdFailureReason reason)
        {
            _adFailureReason = reason;
            return this;
        }

        /// <summary>Adds a parameter, or replaces the value of one already added under the same key.</summary>
        /// <param name="key">Lower snake case, for example <c>level_deaths</c>.</param>
        /// <param name="value">Text, a whole number, a fraction or a flag — the primitives convert implicitly.</param>
        public AnalyticsEventBuilder With(string key, AnalyticsValue value)
        {
            for (int i = 0; i < _parameters.Count; i++)
            {
                if (string.Equals(_parameters[i].Key, key, StringComparison.Ordinal))
                {
                    _parameters[i] = new AnalyticsParameter(key, value);
                    return this;
                }
            }

            _parameters.Add(new AnalyticsParameter(key, value));
            return this;
        }

        /// <summary>Records what produced the event, for the debugger. Trackers set it for you.</summary>
        public AnalyticsEventBuilder From(string source)
        {
            _source = source;
            return this;
        }

        /// <summary>Builds the event. The builder can go on being used; later changes do not reach this event.</summary>
        public AnalyticsEvent Build()
        {
            AdDetails adDetails = _category == AnalyticsCategory.Ad
                ? new AdDetails(_adAction, _adType, _adPlacement, _adNetwork, _adFailureReason)
                : null;

            return new AnalyticsEvent(
                _name,
                _category,
                _status,
                adDetails,
                _hierarchy,
                _value,
                _parameters.ToArray(),
                _source,
                0,
                0d,
                default(DateTime));
        }
    }
}
