using System;
using System.Collections.Generic;
using Zenject;

namespace Game.Events.DI
{
    /// <summary>
    /// Attaches every <see cref="IEventObserver"/> bound in the container to the service, and detaches them when
    /// the context is destroyed.
    /// </summary>
    /// <remarks>
    /// Observers are wired here rather than through the service constructor so that an observer is free to depend on
    /// the service itself without creating a cycle, and so that a context with no observers at all still resolves.
    /// Bind your own tap with <c>Container.BindInterfacesTo&lt;MyObserver&gt;().AsSingle()</c> and it is picked
    /// up automatically.
    /// </remarks>
    internal sealed class GameEventsObserverBinder : IInitializable, IDisposable
    {
        private readonly IEventObserverRegistry _registry;
        private readonly List<IEventObserver> _observers;
        private readonly List<IDisposable> _tokens = new List<IDisposable>();

        public GameEventsObserverBinder(
            IEventObserverRegistry registry,
            [InjectOptional] List<IEventObserver> observers)
        {
            _registry = registry;
            _observers = observers;
        }

        public void Initialize()
        {
            if (_observers == null)
            {
                return;
            }

            for (int i = 0; i < _observers.Count; i++)
            {
                _tokens.Add(_registry.AddObserver(_observers[i]));
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < _tokens.Count; i++)
            {
                _tokens[i].Dispose();
            }

            _tokens.Clear();
        }
    }
}
