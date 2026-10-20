using UnityEngine;
using Zenject;

namespace Game.Analytics.Gameplay
{
    /// <summary>
    /// Installs this game's tracking plan: the game and level trackers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Goes on the same object as the <c>AnalyticsInstaller</c>, in the <c>ProjectContext</c>. The trackers resolve
    /// <see cref="IAnalytics"/> and the event bus from that context, so installer order does not matter.
    /// </para>
    /// <para>
    /// A tracking plan is installed once per game. A second copy in a child context would report every event twice, so
    /// it is skipped with a warning.
    /// </para>
    /// </remarks>
    public sealed class GameplayAnalyticsInstaller : MonoInstaller
    {
        /// <inheritdoc />
        public override void InstallBindings()
        {
            if (Container.HasBindingId(typeof(LevelAnalyticsTracker), null, InjectSources.Any))
            {
                Debug.LogWarning(
                    "[Analytics] The gameplay tracking plan is already installed in this context or a parent, so the " +
                    "installer on '" + name + "' was skipped. A second copy would report every event twice.", this);
                return;
            }

            // Registers as IInitializable, because it subscribes in Start, and as IDisposable, so the context's
            // DisposableManager unsubscribes it.
            Container.BindInterfacesAndSelfTo<GameAnalyticsTracker>().AsSingle();
            Container.BindInterfacesAndSelfTo<LevelAnalyticsTracker>().AsSingle();
        }
    }
}
