using System;

namespace Game.Events
{
    /// <summary>
    /// Sends events. Inject this into systems that only ever produce events, so the type signature says so.
    /// </summary>
    public interface IEventPublisher
    {
        /// <summary>
        /// Delivers an event to every matching handler before returning.
        /// </summary>
        /// <typeparam name="T">The event type. Must carry an <see cref="EventPathAttribute"/>.</typeparam>
        /// <param name="gameEvent">The event value. It is passed by value and never boxed for typed handlers.</param>
        /// <exception cref="GameEventsRecursionException">
        /// Publishing nested deeper than <see cref="GameEventsSettings.MaxDispatchDepth"/>.
        /// </exception>
        void Publish<T>(T gameEvent) where T : struct, IGameEvent;
    }

    /// <summary>
    /// Defers events to the end of the frame. The Zenject integration flushes this from an
    /// <c>ILateTickable</c>, exactly once per frame.
    /// </summary>
    public interface IEventQueue
    {
        /// <summary>Number of events waiting for the next flush.</summary>
        int PendingCount { get; }

        /// <summary>
        /// Queues an event instead of delivering it now. Nothing runs until <see cref="Flush"/>.
        /// </summary>
        /// <typeparam name="T">The event type. Must carry an <see cref="EventPathAttribute"/>.</typeparam>
        /// <param name="gameEvent">The event value, copied into a per-type queue without boxing.</param>
        void Enqueue<T>(T gameEvent) where T : struct, IGameEvent;

        /// <summary>
        /// Delivers everything that was queued when the call started.
        /// </summary>
        /// <remarks>
        /// Events queued from inside a handler land in the next flush rather than extending this one, so a
        /// handler that re-queues its own event cannot stall the frame.
        /// </remarks>
        /// <returns>How many events were delivered.</returns>
        int Flush();
    }

    /// <summary>
    /// Listens for events. Inject this into systems that only ever consume events, such as analytics.
    /// </summary>
    /// <remarks>
    /// Every subscribe call returns a token; disposing the token unsubscribes. Handlers with a lower
    /// <c>priority</c> run first, matching Unity's script execution order convention. Handlers added or removed
    /// from inside a handler behave predictably: a removed handler is never called again, and a newly added one
    /// starts with the next dispatch rather than the one in flight.
    /// </remarks>
    public interface IEventSubscriber
    {
        /// <summary>
        /// Subscribes to one concrete event type. This is the allocation free path on publish.
        /// </summary>
        /// <typeparam name="T">The event type. Must carry an <see cref="EventPathAttribute"/>.</typeparam>
        /// <param name="handler">Called with the event value. Never <c>null</c>.</param>
        /// <param name="priority">Lower runs first. Handlers with equal priority run in subscription order.</param>
        /// <returns>A token. Dispose it to unsubscribe.</returns>
        IDisposable Subscribe<T>(Action<T> handler, int priority = 0) where T : struct, IGameEvent;

        /// <summary>
        /// Subscribes to a whole branch of the path tree.
        /// </summary>
        /// <param name="branchPath">
        /// A branch such as <c>"Level"</c>, which receives <c>Level.Entered</c>, <c>Level.Completed</c> and
        /// <c>Level.Entered.Boss</c> but never <c>LevelUp.Something</c>. Pass <c>null</c>, <c>""</c> or
        /// <c>"*"</c> to receive every event, which is what analytics and debug tooling want.
        /// </param>
        /// <param name="handler">Called with the event boxed as <see cref="IGameEvent"/>.</param>
        /// <param name="priority">Lower runs first, interleaved with the typed handlers of the same event.</param>
        /// <returns>A token. Dispose it to unsubscribe.</returns>
        IDisposable SubscribeBranch(string branchPath, Action<IGameEvent> handler, int priority = 0);
    }

    /// <summary>
    /// Attaches passive taps that see every event before the handlers run. Used by the debug logger and the
    /// event history; not intended for gameplay.
    /// </summary>
    public interface IEventObserverRegistry
    {
        /// <summary>
        /// Registers an observer.
        /// </summary>
        /// <param name="observer">Called for every published event, in registration order.</param>
        /// <returns>A token. Dispose it to detach the observer.</returns>
        IDisposable AddObserver(IEventObserver observer);
    }

    /// <summary>
    /// The whole service: publish, queue, subscribe and observe.
    /// </summary>
    /// <remarks>
    /// The service is built for the main thread. In the editor and in development builds it verifies the calling
    /// thread and throws <see cref="GameEventsThreadException"/> otherwise; in release builds that check is
    /// compiled out, so calling it from a worker thread is undefined behaviour.
    /// </remarks>
    public interface IGlobalGameEvents : IEventPublisher, IEventQueue, IEventSubscriber, IEventObserverRegistry
    {
    }
}
