namespace Game.Ads
{
    /// <summary>
    /// How far the player has got, for the levels in the ads config ("no banner before level 10").
    /// </summary>
    /// <remarks>
    /// The one thing the module asks of the game rather than the other way round. It is implemented outside the module —
    /// the progress lives in the game's own assembly, which no module can reference — and bound in the
    /// <c>ProjectContext</c>. Without a binding the module has no level limits at all, and says so on start.
    /// </remarks>
    public interface IAdsLevelSource
    {
        /// <summary>
        /// The number, as the player sees it (1 based), of the furthest level the player has reached: one past the
        /// highest level ever completed, 1 before any. Replaying an earlier level does not lower it.
        /// </summary>
        int ReachedLevel { get; }
    }
}
