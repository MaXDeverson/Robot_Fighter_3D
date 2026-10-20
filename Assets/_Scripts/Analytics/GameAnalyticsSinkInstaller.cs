using UnityEngine;
using Zenject;

namespace Game.Analytics.Backends
{
    /// <summary>
    /// Adds the <see cref="GameAnalyticsSink"/> to the analytics service of this context.
    /// </summary>
    /// <remarks>
    /// Goes on the <c>ProjectContext</c> next to the <c>AnalyticsInstaller</c>. The sink binder of the analytics module
    /// attaches every bound <see cref="IAnalyticsSink"/>, so the order of the two installers does not matter.
    /// </remarks>
    public sealed class GameAnalyticsSinkInstaller : MonoInstaller
    {
        [SerializeField]
        [Tooltip("Off, events are not sent to GameAnalytics. The other sinks still get them.")]
        private bool _enabled = true;

        /// <inheritdoc />
        public override void InstallBindings()
        {
            if (!_enabled)
            {
                return;
            }

            Container.Bind<IAnalyticsSink>().To<GameAnalyticsSink>().AsSingle();
        }
    }
}
