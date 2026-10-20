using Game.Events;

namespace Game.Gameplay.Events
{
    // Analytics events of the game. Each one keeps the meaning of the event name it replaced
    // (the old name is noted next to it).

    /// <summary>Published once by <c>GameBoot</c>, when the event service is ready and the first scene is about to load.</summary>
    [EventPath("Game.Booted")]
    public struct GameBooted : IGameEvent
    {
    }

    /// <summary>The player picked a mission (was <c>level_N</c> and <c>level_up</c>).</summary>
    [EventPath("Level.Selected")]
    public struct LevelSelected : IGameEvent
    {
        public int LevelIndex;
    }

    /// <summary>A level was loaded and the fight begins. <c>LevelIndex</c> is the level the player is on.</summary>
    [EventPath("Level.Started")]
    public struct LevelStarted : IGameEvent
    {
        public int LevelIndex;
    }

    /// <summary>The last wave of a level was beaten and the level complete screen is about to show.</summary>
    [EventPath("Level.Completed")]
    public struct LevelCompleted : IGameEvent
    {
        public int LevelIndex;
    }

    /// <summary>The player picked a weapon from the rewarded choice (was <c>weapon_N</c>).</summary>
    [EventPath("Weapon.Taken")]
    public struct WeaponTaken : IGameEvent
    {
        public int WeaponIndex;
    }

    /// <summary>The player watched a rewarded ad to double the cash (was <c>double_cash</c>).</summary>
    [EventPath("Cash.Doubled")]
    public struct CashDoubled : IGameEvent
    {
    }

    /// <summary>An ATM was broken (was <c>atm_crush</c>).</summary>
    [EventPath("Environment.AtmCrushed")]
    public struct AtmCrushed : IGameEvent
    {
    }

    /// <summary>The interstitial ad failed to load (was <c>interstitial_load_isError</c>).</summary>
    [EventPath("Ads.Interstitial.LoadFailed")]
    public struct InterstitialLoadFailed : IGameEvent
    {
    }

    /// <summary>The interstitial ad was closed (was <c>interstitial_closed</c>).</summary>
    [EventPath("Ads.Interstitial.Closed")]
    public struct InterstitialClosed : IGameEvent
    {
    }

    /// <summary>The interstitial ad failed to show (was <c>interstitial_content_failed</c>).</summary>
    [EventPath("Ads.Interstitial.ContentFailed")]
    public struct InterstitialContentFailed : IGameEvent
    {
    }

    /// <summary>The rewarded ad failed to load (was <c>rewarded_load_isError</c>).</summary>
    [EventPath("Ads.Rewarded.LoadFailed")]
    public struct RewardedLoadFailed : IGameEvent
    {
    }

    /// <summary>The rewarded ad was closed (was <c>rewarded_closed</c>).</summary>
    [EventPath("Ads.Rewarded.Closed")]
    public struct RewardedClosed : IGameEvent
    {
    }

    /// <summary>The rewarded ad failed to show (was <c>rewarded_content_failed</c>).</summary>
    [EventPath("Ads.Rewarded.ContentFailed")]
    public struct RewardedContentFailed : IGameEvent
    {
    }
}
