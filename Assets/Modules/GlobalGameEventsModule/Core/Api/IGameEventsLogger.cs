using System;

namespace Game.Events
{
    /// <summary>
    /// Where the service writes. Supplied through injection so the core stays free of any engine reference.
    /// </summary>
    /// <remarks>
    /// The two flow methods are only called from code that the compiler strips outside the editor and
    /// development builds. The two failure methods are always live: a handler that throws must be visible in a
    /// shipping build too.
    /// </remarks>
    public interface IGameEventsLogger
    {
        /// <summary>Flow logging: an event was dispatched. Editor and development builds only.</summary>
        /// <param name="path">Path of the event.</param>
        /// <param name="eventType">The event type. Resolve its name only if you actually write the line.</param>
        /// <param name="handlerCount">How many handlers the dispatch walked.</param>
        /// <param name="fromQueue">True when the event came out of the deferred queue.</param>
        void LogEvent(in EventPath path, Type eventType, int handlerCount, bool fromQueue);

        /// <summary>Flow logging: a subscription was added or removed. Editor and development builds only.</summary>
        /// <param name="path">Path or branch the subscription is for.</param>
        /// <param name="subscribed">True on subscribe, false on unsubscribe.</param>
        /// <param name="isBranch">True for branch subscriptions.</param>
        /// <param name="target">Description of the object owning the handler.</param>
        void LogSubscription(in EventPath path, bool subscribed, bool isBranch, string target);

        /// <summary>Always live: something the caller should know about but that did not stop the dispatch.</summary>
        /// <param name="message">Human readable description.</param>
        void LogWarning(string message);

        /// <summary>Always live: a handler threw, or a queued dispatch failed.</summary>
        /// <param name="message">Human readable description including the event path.</param>
        /// <param name="exception">The original exception, or <c>null</c>.</param>
        void LogError(string message, Exception exception);
    }
}
