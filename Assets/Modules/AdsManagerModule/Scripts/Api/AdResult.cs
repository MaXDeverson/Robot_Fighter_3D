namespace Game.Ads
{
    /// <summary>
    /// What a request for a full screen ad came to. Handed to the callback of
    /// <see cref="IAdsManagerService.ShowInterstitial"/> and <see cref="IAdsManagerService.ShowRewarded"/>, and to
    /// <see cref="IAdsManagerService.FullscreenAdFinished"/>.
    /// </summary>
    public readonly struct AdResult
    {
        /// <summary>Creates a result. The ads manager makes them; a test double of the manager can too.</summary>
        public AdResult(AdType type, AdStatus status, string placement = null, string network = null)
        {
            Type = type;
            Status = status;
            Placement = placement;
            Network = network;
        }

        /// <summary>Which ad was asked for.</summary>
        public AdType Type { get; }

        /// <summary>How it ended, and why when it was not shown.</summary>
        public AdStatus Status { get; }

        /// <summary>
        /// Where in the game the ad was asked for, as the call named it — <c>level_fail</c> — or the type in lower case,
        /// <c>interstitial</c> / <c>rewarded</c>, for a call that named none. For analytics.
        /// </summary>
        public string Placement { get; }

        /// <summary>
        /// The ad network the manager shows ads from, lower case — <c>stub</c> for the stand-in. Named on every result,
        /// also one the rules held back before the network was asked. For analytics.
        /// </summary>
        public string Network { get; }

        /// <summary>
        /// The ad was shown: an interstitial reached the player, a rewarded ad was watched to the end. Give a reward
        /// only when this is true.
        /// </summary>
        public bool IsSuccess
        {
            get { return Status == AdStatus.Completed; }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Type + ": " + Status;
        }
    }
}
