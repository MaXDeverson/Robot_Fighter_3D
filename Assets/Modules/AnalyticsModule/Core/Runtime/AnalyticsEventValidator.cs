using System;
using System.Collections.Generic;
using System.Globalization;

namespace Game.Analytics
{
    /// <summary>
    /// The contract every event meets before any sink sees it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The limits are the strictest of the backends this game may report to, so an event that passes here fits all of
    /// them, and switching backends never turns a working event into one the new backend silently throws away.
    /// </para>
    /// <para>
    /// Names and parameter keys follow Firebase: at most 40 characters of lower case letters, digits and <c>_</c>,
    /// starting with a letter; at most 25 parameters; text values of at most 100 characters. Hierarchy segments follow
    /// GameAnalytics: 1 to 64 characters of letters, digits, space and <c>_-.()!?</c>; up to three segments for a
    /// progression and five for a design event; whole number progression scores. Numbers must be finite everywhere.
    /// </para>
    /// <para>
    /// An ad event needs an action and a type, a placement that is a valid segment, and a network of at most 32 lower
    /// case letters, digits and <c>_</c> — GameAnalytics' limit for an ad SDK name. Only a failed show has a reason, and
    /// no ad event has a value.
    /// </para>
    /// </remarks>
    public sealed class AnalyticsEventValidator
    {
        /// <summary>Longest event name or parameter key.</summary>
        public const int MaxNameLength = 40;

        /// <summary>Most parameters an event may carry, global parameters included.</summary>
        public const int MaxParameterCount = 25;

        /// <summary>Longest text value.</summary>
        public const int MaxTextLength = 100;

        /// <summary>Longest hierarchy segment.</summary>
        public const int MaxSegmentLength = 64;

        /// <summary>Most hierarchy segments of a progression event.</summary>
        public const int MaxProgressionSegments = 3;

        /// <summary>Most hierarchy segments of a design event.</summary>
        public const int MaxDesignSegments = 5;

        /// <summary>Hierarchy segments of an ad event: its type and its placement.</summary>
        public const int AdSegments = 2;

        /// <summary>Longest ad network name.</summary>
        public const int MaxAdNetworkLength = 32;

        private const string SegmentReplacement = "unknown";

        /// <summary>Whether a character may appear in a hierarchy segment.</summary>
        public static bool IsSegmentCharacter(char character)
        {
            return (character >= 'a' && character <= 'z') ||
                   (character >= 'A' && character <= 'Z') ||
                   (character >= '0' && character <= '9') ||
                   character == ' ' || character == '_' || character == '-' || character == '.' ||
                   character == '(' || character == ')' || character == '!' || character == '?';
        }

        /// <summary>
        /// Makes arbitrary text usable as a hierarchy segment: characters a backend would refuse become <c>_</c> and the
        /// result is cut to <see cref="MaxSegmentLength"/>. Ids typed by designers go through this, so one stray
        /// character cannot get a whole event rejected.
        /// </summary>
        public static string SanitizeSegment(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return SegmentReplacement;
            }

            int length = Math.Min(text.Length, MaxSegmentLength);
            char[] characters = new char[length];
            for (int i = 0; i < length; i++)
            {
                char character = text[i];
                characters[i] = IsSegmentCharacter(character) ? character : '_';
            }

            return new string(characters);
        }

        /// <summary>Checks an event against the contract.</summary>
        /// <param name="analyticsEvent">The event, normally stamped and with the global parameters merged in.</param>
        /// <param name="problem">What is wrong, readable enough to fix it from; <c>null</c> when the event is valid.</param>
        /// <returns>True when the event may be sent.</returns>
        public bool TryValidate(AnalyticsEvent analyticsEvent, out string problem)
        {
            if (analyticsEvent == null)
            {
                problem = "The event is null.";
                return false;
            }

            if (!IsIdentifier(analyticsEvent.Name))
            {
                problem = "The name '" + analyticsEvent.Name + "' must be 1-" + MaxNameLength + " characters of lower " +
                          "case letters, digits and '_', starting with a letter.";
                return false;
            }

            return TryValidateStructure(analyticsEvent, out problem) &&
                   TryValidateParameters(analyticsEvent.Parameters, out problem);
        }

        private static bool TryValidateStructure(AnalyticsEvent analyticsEvent, out string problem)
        {
            int maxSegments;
            switch (analyticsEvent.Category)
            {
                case AnalyticsCategory.Progression:
                    if (analyticsEvent.ProgressionStatus == ProgressionStatus.None)
                    {
                        problem = "A progression event needs a status: Start, Complete or Fail.";
                        return false;
                    }

                    maxSegments = MaxProgressionSegments;
                    break;

                case AnalyticsCategory.Design:
                    maxSegments = MaxDesignSegments;
                    break;

                case AnalyticsCategory.Ad:
                    if (!TryValidateAd(analyticsEvent, out problem))
                    {
                        return false;
                    }

                    maxSegments = AdSegments;
                    break;

                default:
                    problem = "The category " + analyticsEvent.Category + " is not one this validator knows.";
                    return false;
            }

            IReadOnlyList<string> hierarchy = analyticsEvent.Hierarchy;
            if (hierarchy.Count == 0 || hierarchy.Count > maxSegments)
            {
                problem = "A " + analyticsEvent.Category + " event needs 1-" + maxSegments + " hierarchy segments, got " +
                          hierarchy.Count + ".";
                return false;
            }

            for (int i = 0; i < hierarchy.Count; i++)
            {
                if (!IsSegment(hierarchy[i]))
                {
                    problem = "The hierarchy segment '" + hierarchy[i] + "' must be 1-" + MaxSegmentLength +
                              " characters of letters, digits, space and _-.()!? — sanitize ids typed by hand with " +
                              "AnalyticsEventValidator.SanitizeSegment.";
                    return false;
                }
            }

            if (analyticsEvent.Value.HasValue)
            {
                double value = analyticsEvent.Value.Value;
                if (!IsFinite(value))
                {
                    problem = "The value must be a finite number.";
                    return false;
                }

                if (analyticsEvent.Category == AnalyticsCategory.Progression && value != Math.Floor(value))
                {
                    problem = "A progression score must be a whole number, got " +
                              value.ToString("R", CultureInfo.InvariantCulture) + ".";
                    return false;
                }
            }

            problem = null;
            return true;
        }

        private static bool TryValidateAd(AnalyticsEvent analyticsEvent, out string problem)
        {
            AdDetails ad = analyticsEvent.AdDetails;
            if (ad == null)
            {
                problem = "An ad event needs its ad — build it with AnalyticsEvent.Ad.";
                return false;
            }

            if (ad.Action == AdAction.None)
            {
                problem = "An ad event needs an action: Show, RewardReceived or FailedShow.";
                return false;
            }

            if (ad.Type == AdType.None)
            {
                problem = "An ad event needs the type of the ad: Interstitial or Rewarded.";
                return false;
            }

            if (!IsSegment(ad.Placement))
            {
                problem = "The ad placement '" + ad.Placement + "' must be 1-" + MaxSegmentLength + " characters of " +
                          "letters, digits, space and _-.()!?.";
                return false;
            }

            if (!IsIdentifier(ad.Network, MaxAdNetworkLength))
            {
                problem = "The ad network '" + ad.Network + "' must be 1-" + MaxAdNetworkLength + " characters of lower " +
                          "case letters, digits and '_', starting with a letter.";
                return false;
            }

            if (ad.FailureReason != AdFailureReason.None && ad.Action != AdAction.FailedShow)
            {
                problem = "Only a failed show has a failure reason; this is a " + ad.Action + ".";
                return false;
            }

            if (analyticsEvent.Value.HasValue)
            {
                problem = "An ad event carries no value.";
                return false;
            }

            problem = null;
            return true;
        }

        private static bool TryValidateParameters(IReadOnlyList<AnalyticsParameter> parameters, out string problem)
        {
            if (parameters.Count > MaxParameterCount)
            {
                problem = "At most " + MaxParameterCount + " parameters are allowed, global ones included; got " +
                          parameters.Count + ".";
                return false;
            }

            for (int i = 0; i < parameters.Count; i++)
            {
                AnalyticsParameter parameter = parameters[i];
                if (!IsIdentifier(parameter.Key))
                {
                    problem = "The parameter key '" + parameter.Key + "' must be 1-" + MaxNameLength + " characters of " +
                              "lower case letters, digits and '_', starting with a letter.";
                    return false;
                }

                for (int j = 0; j < i; j++)
                {
                    if (string.Equals(parameters[j].Key, parameter.Key, StringComparison.Ordinal))
                    {
                        problem = "The parameter '" + parameter.Key + "' appears twice.";
                        return false;
                    }
                }

                AnalyticsValue value = parameter.Value;
                if (value.Type == AnalyticsValueType.String && value.AsString.Length > MaxTextLength)
                {
                    problem = "The parameter '" + parameter.Key + "' is longer than " + MaxTextLength + " characters.";
                    return false;
                }

                if (value.Type == AnalyticsValueType.Float && !IsFinite(value.AsFloat))
                {
                    problem = "The parameter '" + parameter.Key + "' must be a finite number.";
                    return false;
                }
            }

            problem = null;
            return true;
        }

        private static bool IsIdentifier(string text)
        {
            return IsIdentifier(text, MaxNameLength);
        }

        private static bool IsIdentifier(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length > maxLength)
            {
                return false;
            }

            if (text[0] < 'a' || text[0] > 'z')
            {
                return false;
            }

            for (int i = 1; i < text.Length; i++)
            {
                char character = text[i];
                bool allowed = (character >= 'a' && character <= 'z') ||
                               (character >= '0' && character <= '9') ||
                               character == '_';
                if (!allowed)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsSegment(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length > MaxSegmentLength)
            {
                return false;
            }

            for (int i = 0; i < text.Length; i++)
            {
                if (!IsSegmentCharacter(text[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
