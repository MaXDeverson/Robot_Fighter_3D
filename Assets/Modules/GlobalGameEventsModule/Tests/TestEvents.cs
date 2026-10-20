namespace Game.Events.Tests
{
    // The suite owns a path root of its own. Sharing "Level.Entered" with the samples would make both types
    // claim one path, and the installer's start-up validation would rightly report that as a conflict.

    [EventPath("Suite.Level.Entered")]
    public struct SuiteLevelEntered : IGameEvent
    {
        public string LevelId;
    }

    [EventPath("Suite.Level.Completed")]
    public struct SuiteLevelCompleted : IGameEvent
    {
        public int Score;
    }

    [EventPath("Suite.Level.Entered.Boss")]
    public struct SuiteBossEntered : IGameEvent
    {
        public string BossId;
    }

    /// <summary>Shares a string prefix with the Level branch but not a segment. A branch subscription to
    /// "Suite.Level" must never receive it.</summary>
    [EventPath("Suite.LevelUp.Gained")]
    public struct SuiteLevelUpGained : IGameEvent
    {
        public int Amount;
    }

    [EventPath("Suite.Player.Died")]
    public struct SuitePlayerDied : IGameEvent
    {
        public string Cause;
    }

    /// <summary>Used by the recursion tests: a handler republishes it.</summary>
    [EventPath("Suite.Echo")]
    public struct SuiteEcho : IGameEvent
    {
        public int Hop;
    }

    /// <summary>Declared without an [EventPath] on purpose.</summary>
    public struct SuiteUnpathed : IGameEvent
    {
        public int Value;
    }

    [EventPath("Suite.Duplicate")]
    public struct SuiteDuplicateA : IGameEvent
    {
        public int Value;
    }

    [EventPath("Suite.Duplicate")]
    public struct SuiteDuplicateB : IGameEvent
    {
        public int Value;
    }

    [EventPath("Suite..Broken")]
    public struct SuiteMalformed : IGameEvent
    {
        public int Value;
    }
}
