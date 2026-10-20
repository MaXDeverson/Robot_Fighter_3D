using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Events.DI
{
    /// <summary>
    /// Bookkeeping component added by <see cref="ISubscriptionLifetime"/>. It holds the tokens bound to its
    /// GameObject and disposes them in <c>OnDestroy</c>.
    /// </summary>
    /// <remarks>
    /// Never add this by hand; ask <see cref="ISubscriptionLifetime"/> to bind a token instead. Disposing an
    /// already released token is a no-op, so it does not matter whether the object or the container goes first.
    /// </remarks>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    internal sealed class SubscriptionOwner : MonoBehaviour
    {
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        internal int Count
        {
            get { return _subscriptions.Count; }
        }

        internal void Add(IDisposable subscription)
        {
            _subscriptions.Add(subscription);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _subscriptions.Count; i++)
            {
                try
                {
                    _subscriptions[i].Dispose();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                }
            }

            _subscriptions.Clear();
        }
    }
}
