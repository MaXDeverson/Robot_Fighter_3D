namespace Game.Events
{
    /// <summary>
    /// A passive tap on the event stream.
    /// </summary>
    /// <remarks>
    /// Observers run before any handler, always in registration order, and are not affected by handler
    /// priorities. They are the seam the debug logger and the event history hang off, which keeps those two out
    /// of the subscription list the debugger shows. An observer always receives the event boxed, so leaving none
    /// registered is what keeps <see cref="IEventPublisher.Publish{T}"/> allocation free in release builds.
    /// </remarks>
    public interface IEventObserver
    {
        /// <summary>Called once for every published event, including events delivered by a queue flush.</summary>
        /// <param name="path">The path of the event type.</param>
        /// <param name="gameEvent">The event, boxed.</param>
        void OnPublished(in EventPath path, IGameEvent gameEvent);
    }
}
