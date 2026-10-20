using System;
using System.Collections.Generic;
using Zenject;

namespace Game.Analytics.DI
{
    /// <summary>
    /// Attaches every <see cref="IAnalyticsSink"/> bound in the container to the service, and detaches them when the
    /// context is destroyed.
    /// </summary>
    /// <remarks>
    /// This is what makes a backend a one-line integration: bind
    /// <c>Container.Bind&lt;IAnalyticsSink&gt;().To&lt;MySink&gt;().AsSingle()</c> in any installer of the context and it
    /// is picked up here. Sinks are wired here rather than through the service constructor so the core never sees
    /// Zenject, and so a context without any sink still resolves.
    /// </remarks>
    internal sealed class AnalyticsSinkBinder : IInitializable, IDisposable
    {
        private readonly IAnalyticsSinkRegistry _registry;
        private readonly List<IAnalyticsSink> _sinks;
        private readonly List<IDisposable> _tokens = new List<IDisposable>();

        public AnalyticsSinkBinder(IAnalyticsSinkRegistry registry, List<IAnalyticsSink> sinks)
        {
            _registry = registry;
            _sinks = sinks;
        }

        public void Initialize()
        {
            for (int i = 0; i < _sinks.Count; i++)
            {
                _tokens.Add(_registry.AddSink(_sinks[i]));
            }
        }

        public void Dispose()
        {
            for (int i = _tokens.Count - 1; i >= 0; i--)
            {
                _tokens[i].Dispose();
            }

            _tokens.Clear();
        }
    }
}
