using System;
using System.Reflection;
using Game.Events;
using Zenject;

namespace Game.Analytics.Gameplay
{
    /// <summary>
    /// Base of the classes that turn gameplay events into analytics events — one tracker per area of the tracking
    /// plan.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A tracker subscribes in <see cref="Subscribe"/> through <see cref="Listen{T}"/> — typed, so the event bus never
    /// boxes — and reports through <see cref="Track"/>, which stamps every analytics event with the path of the
    /// gameplay event it came from: the "source" the debugger window shows.
    /// </para>
    /// <para>
    /// Its subscriptions are its own and end with it: <see cref="Dispose"/> runs from the <c>DisposableManager</c> of
    /// whichever context binds the tracker. Parking them in the project-wide <c>IEventSubscriptionScope</c> instead
    /// would leak a copy per scene load the day a tracker is bound in a scene context.
    /// </para>
    /// </remarks>
    public abstract class AnalyticsTracker : IInitializable, IDisposable
    {
        private readonly IEventSubscriber _events;
        private readonly SubscriptionBag _subscriptions = new SubscriptionBag();

        // The path of the gameplay event being handled right now, so Track can stamp it without every handler passing
        // it along. Saved and restored around each handler, which keeps it right when handling one event publishes
        // another that this tracker also listens to.
        private string _source;

        /// <summary>Creates the tracker.</summary>
        /// <param name="events">The gameplay event bus.</param>
        /// <param name="analytics">Where the analytics events go.</param>
        protected AnalyticsTracker(IEventSubscriber events, IAnalytics analytics)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            Analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
        }

        /// <summary>The analytics service, for global parameters and anything <see cref="Track"/> does not cover.</summary>
        protected IAnalytics Analytics { get; }

        /// <summary>Subscribes. Zenject calls it once, from <c>Start</c>.</summary>
        public void Initialize()
        {
            Subscribe();
        }

        /// <summary>Unsubscribes. Zenject calls it when the context the tracker lives in is destroyed.</summary>
        public void Dispose()
        {
            _subscriptions.Dispose();
            Release();
        }

        /// <summary>Subscribes to the gameplay events this tracker reports on, with <see cref="Listen{T}"/>.</summary>
        protected abstract void Subscribe();

        /// <summary>Releases anything the tracker holds besides its event subscriptions. Nothing, by default.</summary>
        protected virtual void Release()
        {
        }

        /// <summary>Subscribes a handler to one gameplay event type, for as long as the tracker lives.</summary>
        protected void Listen<T>(Action<T> handler) where T : struct, IGameEvent
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            string source = DescribeSource(typeof(T));
            _subscriptions.Add(_events.Subscribe<T>(gameEvent =>
            {
                string outer = _source;
                _source = source;
                try
                {
                    handler(gameEvent);
                }
                finally
                {
                    _source = outer;
                }
            }));
        }

        /// <summary>Tracks an event, stamped with the gameplay event being handled as its source.</summary>
        protected void Track(AnalyticsEventBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (_source != null)
            {
                builder.From(_source);
            }

            Analytics.Track(builder.Build());
        }

        /// <summary>Rounds to hundredths: durations and positions, without float noise in the data.</summary>
        protected static double Round(double value)
        {
            return Math.Round(value, 2);
        }

        private static string DescribeSource(Type eventType)
        {
            EventPathAttribute attribute = eventType.GetCustomAttribute<EventPathAttribute>(false);
            return attribute != null ? attribute.Path : eventType.Name;
        }
    }
}
