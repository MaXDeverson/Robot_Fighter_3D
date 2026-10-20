namespace Game.Ads
{
    /// <summary>How a request for a full screen ad ended.</summary>
    public enum AdStatus
    {
        /// <summary>Shown. For a rewarded ad: watched to the end, so the reward is earned.</summary>
        Completed = 0,

        /// <summary>A rewarded ad the player closed before the end. No reward.</summary>
        Cancelled = 1,

        /// <summary>Not shown: the player has an ad block. Never the answer for a rewarded ad.</summary>
        AdBlock = 2,

        /// <summary>Not shown: the player has not reached the level this type of ad starts at.</summary>
        LevelLocked = 3,

        /// <summary>Not shown: the previous interstitial was too recent.</summary>
        Cooldown = 4,

        /// <summary>Not shown: the ad network has no ad ready.</summary>
        NotReady = 5,

        /// <summary>Not shown: another full screen ad is on screen.</summary>
        Busy = 6,

        /// <summary>The ad network failed to show it.</summary>
        Failed = 7,
    }
}
