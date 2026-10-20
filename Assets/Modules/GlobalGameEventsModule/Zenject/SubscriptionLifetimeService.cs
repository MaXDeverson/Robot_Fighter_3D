using System;
using UnityEngine;

namespace Game.Events.DI
{
    /// <summary>
    /// Default <see cref="ISubscriptionLifetime"/>. Attaches a <see cref="SubscriptionOwner"/> to the target
    /// object the first time a token is bound to it, and reuses that component afterwards.
    /// </summary>
    internal sealed class SubscriptionLifetimeService : ISubscriptionLifetime
    {
        private readonly IGameEventsLogger _logger;

        public SubscriptionLifetimeService(IGameEventsLogger logger)
        {
            _logger = logger;
        }

        public IDisposable Bind(IDisposable subscription, GameObject owner)
        {
            if (subscription == null)
            {
                throw new ArgumentNullException(nameof(subscription));
            }

            if (owner == null)
            {
                // Binding to nothing would silently leak the handler for the lifetime of the service, which is the
                // single most common way an event service keeps dead objects alive. Fail loudly instead.
                subscription.Dispose();
                _logger.LogWarning(
                    "A subscription was bound to a null or already destroyed owner and has been disposed " +
                    "immediately. Bind to the component that made the subscription, usually 'this'.");
                return subscription;
            }

            SubscriptionOwner holder = owner.GetComponent<SubscriptionOwner>();
            if (holder == null)
            {
                holder = owner.AddComponent<SubscriptionOwner>();
            }

            holder.Add(subscription);
            return subscription;
        }

        public IDisposable Bind(IDisposable subscription, Component owner)
        {
            return Bind(subscription, owner == null ? null : owner.gameObject);
        }
    }
}
