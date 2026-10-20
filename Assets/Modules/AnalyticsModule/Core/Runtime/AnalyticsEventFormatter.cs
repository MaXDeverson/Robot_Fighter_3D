using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Game.Analytics
{
    /// <summary>
    /// Renders events as single lines of text, for the console sink and the debugger window.
    /// </summary>
    /// <remarks>
    /// Reuses one builder, so an instance belongs to one caller on the main thread. Numbers are invariant: a device
    /// set to a comma locale still logs <c>42.5</c>.
    /// </remarks>
    public sealed class AnalyticsEventFormatter
    {
        private readonly StringBuilder _builder = new StringBuilder(256);

        /// <summary>
        /// The whole event: <c>#12 level_complete  Progression:Complete  Level_003  score=3  {duration=42.5 stars=3}</c>.
        /// </summary>
        public string FormatLine(AnalyticsEvent analyticsEvent)
        {
            _builder.Clear();
            if (analyticsEvent.IsTracked)
            {
                _builder.Append('#').Append(analyticsEvent.Sequence.ToString(CultureInfo.InvariantCulture)).Append(' ');
            }

            _builder.Append(analyticsEvent.Name).Append("  ");
            AppendHeadline(analyticsEvent);

            if (analyticsEvent.Parameters.Count > 0)
            {
                _builder.Append("  {");
                AppendParameters(analyticsEvent.Parameters);
                _builder.Append('}');
            }

            return _builder.ToString();
        }

        /// <summary>
        /// Kind, hierarchy and value: <c>Progression:Complete  Level_003  score=3</c>; for an ad, its network and the
        /// reason it failed: <c>Ad:FailedShow  Rewarded:double_coins  network=admob reason=NoFill</c>.
        /// </summary>
        public string FormatHeadline(AnalyticsEvent analyticsEvent)
        {
            _builder.Clear();
            AppendHeadline(analyticsEvent);
            return _builder.ToString();
        }

        /// <summary>The parameters as space separated <c>key=value</c> pairs; text is quoted.</summary>
        public string FormatParameters(IReadOnlyList<AnalyticsParameter> parameters)
        {
            _builder.Clear();
            AppendParameters(parameters);
            return _builder.ToString();
        }

        private void AppendHeadline(AnalyticsEvent analyticsEvent)
        {
            AdDetails ad = analyticsEvent.AdDetails;

            _builder.Append(analyticsEvent.Category.ToString());
            if (analyticsEvent.Category == AnalyticsCategory.Progression)
            {
                _builder.Append(':').Append(analyticsEvent.ProgressionStatus.ToString());
            }
            else if (ad != null)
            {
                _builder.Append(':').Append(ad.Action.ToString());
            }

            if (analyticsEvent.HierarchyPath.Length > 0)
            {
                _builder.Append("  ").Append(analyticsEvent.HierarchyPath);
            }

            if (analyticsEvent.Value.HasValue)
            {
                _builder.Append("  ")
                    .Append(analyticsEvent.Category == AnalyticsCategory.Progression ? "score=" : "value=")
                    .Append(analyticsEvent.Value.Value.ToString("R", CultureInfo.InvariantCulture));
            }

            if (ad != null)
            {
                _builder.Append("  network=").Append(ad.Network);
                if (ad.FailureReason != AdFailureReason.None)
                {
                    _builder.Append(" reason=").Append(ad.FailureReason.ToString());
                }
            }
        }

        private void AppendParameters(IReadOnlyList<AnalyticsParameter> parameters)
        {
            for (int i = 0; i < parameters.Count; i++)
            {
                if (i > 0)
                {
                    _builder.Append(' ');
                }

                AnalyticsParameter parameter = parameters[i];
                _builder.Append(parameter.Key).Append('=');
                if (parameter.Value.Type == AnalyticsValueType.String)
                {
                    _builder.Append('"').Append(parameter.Value.AsString).Append('"');
                }
                else
                {
                    _builder.Append(parameter.Value.ToString());
                }
            }
        }
    }
}
