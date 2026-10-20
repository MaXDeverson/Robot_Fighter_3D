using System;
using UnityEngine;

namespace Game.Events.DI
{
    /// <summary>
    /// Ties subscription tokens to the lifetime of a scene object.
    /// </summary>
    /// <remarks>
    /// This exists because the obvious spelling, an extension method such as <c>token.AddTo(gameObject)</c>,
    /// would need a class that this module is not allowed to declare. Inject this service instead; the container
    /// hands every MonoBehaviour the same instance.
    /// <code>
    /// [Inject]
    /// private void Construct(IEventSubscriber events, ISubscriptionLifetime lifetime)
    /// {
    ///     lifetime.Bind(events.Subscribe&lt;LevelEntered&gt;(OnLevelEntered), this);
    /// }
    /// </code>
    /// </remarks>
    public interface ISubscriptionLifetime
    {
        /// <summary>
        /// Disposes <paramref name="subscription"/> when <paramref name="owner"/> is destroyed.
        /// </summary>
        /// <param name="subscription">A token returned by a subscribe call.</param>
        /// <param name="owner">
        /// The object whose destruction ends the subscription. A destroyed or <c>null</c> owner disposes the
        /// token straight away rather than leaking it.
        /// </param>
        /// <returns>The same token, so the call can be chained or stored.</returns>
        IDisposable Bind(IDisposable subscription, GameObject owner);

        /// <summary>
        /// Disposes <paramref name="subscription"/> when the component's object is destroyed.
        /// </summary>
        /// <param name="subscription">A token returned by a subscribe call.</param>
        /// <param name="owner">Usually <c>this</c> inside a MonoBehaviour.</param>
        /// <returns>The same token, so the call can be chained or stored.</returns>
        IDisposable Bind(IDisposable subscription, Component owner);
    }
}
