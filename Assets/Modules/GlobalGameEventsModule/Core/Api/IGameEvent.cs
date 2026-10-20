namespace Game.Events
{
    /// <summary>
    /// Marker implemented by every event that can travel through an <see cref="IGlobalGameEvents"/>.
    /// </summary>
    /// <remarks>
    /// Events are declared as <c>struct</c> so a typed publish never boxes and never allocates. Every event type
    /// also carries an <see cref="EventPathAttribute"/> placing it in the hierarchical path tree that
    /// <see cref="IEventSubscriber.SubscribeBranch"/> filters on.
    /// <code>
    /// [EventPath("Level.Entered")]
    /// public struct LevelEntered : IGameEvent
    /// {
    ///     public string LevelId;
    /// }
    /// </code>
    /// </remarks>
    public interface IGameEvent
    {
    }
}
