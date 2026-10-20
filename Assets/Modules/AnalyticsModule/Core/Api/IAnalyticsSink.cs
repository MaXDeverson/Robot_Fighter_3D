using System;

namespace Game.Analytics
{
    /// <summary>
    /// A destination for analytics events: the console, GameAnalytics, Firebase, your own server.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the seam a backend is swapped at. Writing a sink and binding it is the whole integration: nothing in
    /// gameplay or in the trackers knows which sinks exist. The service owns the lifecycle —
    /// <see cref="Initialize"/> when the sink is added, <see cref="Shutdown"/> when it is removed or the service goes
    /// away — and calls every method on the main thread.
    /// </para>
    /// <para>
    /// Every event that reaches <see cref="Send"/> has already passed <see cref="AnalyticsEventValidator"/>, so it
    /// fits the limits of the backends the validator was written for; a sink only translates. An exception thrown from
    /// any method is caught, logged and counted against this sink alone, and the other sinks still get the event.
    /// </para>
    /// <para>
    /// A backend whose SDK comes up asynchronously queues inside its sink until it is ready: the service never waits.
    /// </para>
    /// </remarks>
    public interface IAnalyticsSink
    {
        /// <summary>Short name for logs and the debugger, for example <c>"Console"</c>.</summary>
        string Name { get; }

        /// <summary>Called once, when the sink is added to the service. Bring the backend up here.</summary>
        void Initialize();

        /// <summary>Delivers one event.</summary>
        void Send(AnalyticsEvent analyticsEvent);

        /// <summary>
        /// Whether the player allows collection. Called right after <see cref="Initialize"/> and again on every change.
        /// Pass it on to a backend that collects on its own — sessions, device data — besides what <see cref="Send"/>
        /// hands it; the service already holds back the events themselves.
        /// </summary>
        void SetCollectionEnabled(bool enabled);

        /// <summary>Pushes out anything buffered. Called when the application is paused or quits.</summary>
        void Flush();

        /// <summary>Called once, when the sink is removed or the service is disposed. The sink is not used again.</summary>
        void Shutdown();
    }

    /// <summary>Attaches sinks to the service. The Zenject integration attaches every bound <see cref="IAnalyticsSink"/>.</summary>
    public interface IAnalyticsSinkRegistry
    {
        /// <summary>Adds a sink and initializes it.</summary>
        /// <returns>A token. Dispose it to flush, shut down and remove the sink.</returns>
        IDisposable AddSink(IAnalyticsSink sink);
    }
}
