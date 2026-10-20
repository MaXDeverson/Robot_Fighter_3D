namespace Game.Analytics
{
    /// <summary>
    /// The ad an <see cref="AnalyticsCategory.Ad"/> event is about: what happened to it, which kind of ad it was, where
    /// in the game it was shown and which network served it. Immutable; built by <see cref="AnalyticsEvent.Ad"/>.
    /// </summary>
    /// <remarks>
    /// The fields are the ones ad reporting is built on — GameAnalytics' ad event takes exactly these — so a sink maps
    /// them one to one. A flat backend reads them the same way, for example into Firebase's <c>ad_impression</c>.
    /// </remarks>
    public sealed class AdDetails
    {
        internal AdDetails(AdAction action, AdType type, string placement, string network, AdFailureReason failureReason)
        {
            Action = action;
            Type = type;
            Placement = placement;
            Network = network;
            FailureReason = failureReason;
        }

        /// <summary>What happened to the ad.</summary>
        public AdAction Action { get; }

        /// <summary>Which kind of ad it was.</summary>
        public AdType Type { get; }

        /// <summary>Where in the game it was shown, for example <c>level_fail</c>.</summary>
        public string Placement { get; }

        /// <summary>The ad network that served it, lower case, for example <c>admob</c>.</summary>
        public string Network { get; }

        /// <summary>Why it could not be shown; <see cref="AdFailureReason.None"/> but for a <see cref="AdAction.FailedShow"/>.</summary>
        public AdFailureReason FailureReason { get; }
    }

    /// <summary>What happened to an ad. The names are GameAnalytics' ad actions.</summary>
    public enum AdAction
    {
        /// <summary>The event is not an ad event.</summary>
        None = 0,

        /// <summary>The ad was on screen.</summary>
        Show = 1,

        /// <summary>A rewarded ad was watched to the end and its reward earned.</summary>
        RewardReceived = 2,

        /// <summary>The ad was asked for and the network could not show it.</summary>
        FailedShow = 3,
    }

    /// <summary>
    /// Which kind of ad an ad event is about. Only the kinds this game reports; a banner or an app open ad is a new
    /// member here.
    /// </summary>
    public enum AdType
    {
        /// <summary>The event is not an ad event.</summary>
        None = 0,

        /// <summary>A full screen ad shown at a natural break in the game.</summary>
        Interstitial = 1,

        /// <summary>A full screen ad the player chooses to watch for a reward.</summary>
        Rewarded = 2,
    }

    /// <summary>Why an ad could not be shown. Only on a <see cref="AdAction.FailedShow"/>.</summary>
    public enum AdFailureReason
    {
        /// <summary>No reason: the ad was not a failed show.</summary>
        None = 0,

        /// <summary>The network had no ad to show.</summary>
        NoFill = 1,

        /// <summary>The network failed to show one and did not say why.</summary>
        Unknown = 2,
    }
}
