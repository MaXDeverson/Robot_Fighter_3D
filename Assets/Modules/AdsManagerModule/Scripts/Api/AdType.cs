namespace Game.Ads
{
    /// <summary>The kinds of ad the module shows.</summary>
    public enum AdType
    {
        /// <summary>A strip at the edge of the screen that stays up while the game goes on.</summary>
        Banner = 0,

        /// <summary>A full screen ad the game shows at a natural break — after a loss, between levels.</summary>
        Interstitial = 1,

        /// <summary>A full screen ad the player chooses to watch for a reward.</summary>
        Rewarded = 2,
    }
}
