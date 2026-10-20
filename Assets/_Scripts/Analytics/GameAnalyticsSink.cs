using System.Collections.Generic;
using GameAnalyticsSDK;
using UnityEngine;

namespace Game.Analytics.Backends
{
    /// <summary>
    /// Sends every event of the analytics service to GameAnalytics.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The structured half of <see cref="AnalyticsEvent"/> maps one to one onto GameAnalytics' calls: a design event
    /// becomes <c>NewDesignEvent</c> with the hierarchy path as its id, a progression <c>NewProgressionEvent</c> with
    /// up to three segments and the score, an ad <c>NewAdEvent</c>. The parameters travel as custom fields.
    /// </para>
    /// <para>
    /// The keys are read from the GameAnalytics settings asset (<b>Window &gt; GameAnalytics &gt; Select Settings</b>).
    /// The SDK comes up synchronously in <see cref="Initialize"/>, so nothing is queued here.
    /// </para>
    /// </remarks>
    public sealed class GameAnalyticsSink : IAnalyticsSink
    {
        /// <inheritdoc />
        public string Name
        {
            get { return "GameAnalytics"; }
        }

        /// <inheritdoc />
        public void Initialize()
        {
            // The component ends the session on quit and forwards Unity logs; GameAnalytics expects exactly one.
            if (Object.FindFirstObjectByType<GameAnalytics>() == null)
            {
                new GameObject("GameAnalytics").AddComponent<GameAnalytics>();
            }

            if (!GameAnalytics.Initialized)
            {
                GameAnalytics.Initialize();
            }
        }

        /// <inheritdoc />
        public void Send(AnalyticsEvent analyticsEvent)
        {
            Dictionary<string, object> fields = BuildCustomFields(analyticsEvent);

            switch (analyticsEvent.Category)
            {
                case AnalyticsCategory.Design:
                    SendDesign(analyticsEvent, fields);
                    break;
                case AnalyticsCategory.Progression:
                    SendProgression(analyticsEvent, fields);
                    break;
                case AnalyticsCategory.Ad:
                    SendAd(analyticsEvent, fields);
                    break;
            }
        }

        /// <inheritdoc />
        public void SetCollectionEnabled(bool enabled)
        {
            GameAnalytics.SetEnabledEventSubmission(enabled);
        }

        /// <inheritdoc />
        public void Flush()
        {
            // GameAnalytics submits on its own timer and persists what is pending; there is nothing to push.
        }

        /// <inheritdoc />
        public void Shutdown()
        {
            // The GameAnalytics component ends the session when the application quits.
        }

        private static void SendDesign(AnalyticsEvent analyticsEvent, Dictionary<string, object> fields)
        {
            if (analyticsEvent.Value.HasValue)
            {
                GameAnalytics.NewDesignEvent(analyticsEvent.HierarchyPath, (float)analyticsEvent.Value.Value, fields);
            }
            else
            {
                GameAnalytics.NewDesignEvent(analyticsEvent.HierarchyPath, fields);
            }
        }

        private static void SendProgression(AnalyticsEvent analyticsEvent, Dictionary<string, object> fields)
        {
            GAProgressionStatus status = ToGameAnalytics(analyticsEvent.ProgressionStatus);
            IReadOnlyList<string> hierarchy = analyticsEvent.Hierarchy;
            string progression01 = hierarchy.Count > 0 ? hierarchy[0] : string.Empty;
            string progression02 = hierarchy.Count > 1 ? hierarchy[1] : null;
            string progression03 = hierarchy.Count > 2 ? hierarchy[2] : null;

            if (analyticsEvent.Value.HasValue)
            {
                int score = (int)analyticsEvent.Value.Value;
                GameAnalytics.NewProgressionEvent(status, progression01, progression02, progression03, score, fields);
            }
            else
            {
                GameAnalytics.NewProgressionEvent(status, progression01, progression02, progression03, fields);
            }
        }

        private static void SendAd(AnalyticsEvent analyticsEvent, Dictionary<string, object> fields)
        {
            AdDetails ad = analyticsEvent.AdDetails;
            if (ad == null)
            {
                return;
            }

            GAAdAction action = ToGameAnalytics(ad.Action);
            GAAdType type = ToGameAnalytics(ad.Type);

            if (ad.Action == AdAction.FailedShow)
            {
                GameAnalytics.NewAdEvent(
                    action, type, ad.Network, ad.Placement, ToGameAnalytics(ad.FailureReason), fields);
            }
            else
            {
                GameAnalytics.NewAdEvent(action, type, ad.Network, ad.Placement, fields);
            }
        }

        private static Dictionary<string, object> BuildCustomFields(AnalyticsEvent analyticsEvent)
        {
            IReadOnlyList<AnalyticsParameter> parameters = analyticsEvent.Parameters;
            if (parameters.Count == 0)
            {
                return null;
            }

            var fields = new Dictionary<string, object>(parameters.Count);
            for (int i = 0; i < parameters.Count; i++)
            {
                AnalyticsValue value = parameters[i].Value;

                // Custom fields only take text and numbers, so a flag travels as 0 or 1.
                fields[parameters[i].Key] = value.Type == AnalyticsValueType.Boolean
                    ? value.AsInteger
                    : value.ToObject();
            }

            return fields;
        }

        private static GAProgressionStatus ToGameAnalytics(ProgressionStatus status)
        {
            switch (status)
            {
                case ProgressionStatus.Start:
                    return GAProgressionStatus.Start;
                case ProgressionStatus.Complete:
                    return GAProgressionStatus.Complete;
                case ProgressionStatus.Fail:
                    return GAProgressionStatus.Fail;
                default:
                    return GAProgressionStatus.Undefined;
            }
        }

        private static GAAdAction ToGameAnalytics(AdAction action)
        {
            switch (action)
            {
                case AdAction.Show:
                    return GAAdAction.Show;
                case AdAction.RewardReceived:
                    return GAAdAction.RewardReceived;
                case AdAction.FailedShow:
                    return GAAdAction.FailedShow;
                default:
                    return GAAdAction.Undefined;
            }
        }

        private static GAAdType ToGameAnalytics(AdType type)
        {
            switch (type)
            {
                case AdType.Interstitial:
                    return GAAdType.Interstitial;
                case AdType.Rewarded:
                    return GAAdType.RewardedVideo;
                default:
                    return GAAdType.Undefined;
            }
        }

        private static GAAdError ToGameAnalytics(AdFailureReason reason)
        {
            switch (reason)
            {
                case AdFailureReason.NoFill:
                    return GAAdError.NoFill;
                case AdFailureReason.Unknown:
                    return GAAdError.Unknown;
                default:
                    return GAAdError.Undefined;
            }
        }
    }
}
