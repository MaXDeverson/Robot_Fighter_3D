using System;

namespace Game.Events
{
    /// <summary>
    /// Collects subscription tokens and releases them together.
    /// </summary>
    /// <remarks>
    /// This is the lifetime handle for plain C# systems. The Zenject integration binds one instance per context
    /// through <c>BindInterfacesAndSelfTo</c>, so the container's <c>DisposableManager</c> empties it when the
    /// context is torn down and no system has to remember to unsubscribe. MonoBehaviours use
    /// <c>ISubscriptionLifetime</c> instead, which ties tokens to <c>OnDestroy</c>.
    /// </remarks>
    public interface IEventSubscriptionScope
    {
        /// <summary>
        /// Takes ownership of a token.
        /// </summary>
        /// <param name="subscription">The token returned by a subscribe call.</param>
        void Add(IDisposable subscription);
    }
}
