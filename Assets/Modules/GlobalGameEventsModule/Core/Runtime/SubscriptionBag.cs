using System;
using System.Collections.Generic;

namespace Game.Events
{
    /// <summary>
    /// Holds subscription tokens and disposes them together.
    /// </summary>
    /// <remarks>
    /// The Zenject installer binds one of these per context through <c>BindInterfacesAndSelfTo</c>. Because it
    /// implements <see cref="IDisposable"/>, the container's <c>DisposableManager</c> empties it when the
    /// context is destroyed, which is how a plain C# system unsubscribes without owning any lifetime code.
    /// A system that wants a tighter scope can also construct its own bag and dispose it whenever it likes.
    /// </remarks>
    public sealed class SubscriptionBag : IEventSubscriptionScope, IDisposable
    {
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private bool _disposed;

        /// <summary>How many tokens are currently held.</summary>
        public int Count
        {
            get { return _subscriptions.Count; }
        }

        /// <summary>
        /// Takes ownership of a token. Adding to an already disposed bag disposes the token immediately, so a
        /// late subscription cannot outlive the scope it was meant to belong to.
        /// </summary>
        /// <param name="subscription">The token returned by a subscribe call.</param>
        public void Add(IDisposable subscription)
        {
            if (subscription == null)
            {
                throw new ArgumentNullException(nameof(subscription));
            }

            if (_disposed)
            {
                subscription.Dispose();
                return;
            }

            _subscriptions.Add(subscription);
        }

        /// <summary>Disposes every held token and empties the bag.</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            for (int i = 0; i < _subscriptions.Count; i++)
            {
                _subscriptions[i].Dispose();
            }

            _subscriptions.Clear();
        }
    }
}
