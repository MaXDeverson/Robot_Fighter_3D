using UnityEngine;
using Zenject;

namespace Game.Analytics.DI
{
    /// <summary>
    /// Installs the analytics service into the context this component belongs to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Meant for the <c>ProjectContext</c>, next to the <c>GlobalGameEventsInstaller</c>: the service, its consent
    /// queue and the counters of the trackers have to outlive scene loads, and every death reloads the gameplay scene.
    /// </para>
    /// <para>
    /// It binds the service under each of its roles, the consent, the storage, the clock and the logger, the console
    /// sink when the config asks for it, the binder that attaches every <see cref="IAnalyticsSink"/> of the container
    /// to the service, and the hooks that flush on pause and quit. A backend is added by binding its sink in any
    /// installer of this context — nothing here changes.
    /// </para>
    /// <para>
    /// Storage, clock, logger and consent are bound with <c>IfNotBound</c>: bind your own before this installer runs
    /// (earlier in the <b>Mono Installers</b> list) and it is used instead.
    /// </para>
    /// </remarks>
    public sealed class AnalyticsInstaller : MonoInstaller
    {
        [SerializeField]
        [Tooltip("Collection, consent, console output and debugging. Without one the defaults are used.")]
        private AnalyticsConfig _config;

        /// <inheritdoc />
        public override void InstallBindings()
        {
            if (Container.HasBindingId(typeof(IAnalytics), null, InjectSources.Local))
            {
                Debug.LogWarning(
                    "[Analytics] This context already installs an analytics service, so the installer on '" + name +
                    "' was skipped. Keep exactly one AnalyticsInstaller per context.", this);
                return;
            }

            if (Container.HasBindingId(typeof(IAnalytics), null, InjectSources.AnyParent))
            {
                // A second service would receive the same gameplay events and report every one of them twice.
                Debug.LogWarning(
                    "[Analytics] A parent context already provides analytics, so the installer on '" + name + "' was " +
                    "skipped and everything here uses the parent's service.", this);
                return;
            }

            if (_config == null)
            {
                Debug.LogWarning(
                    "[Analytics] No config assigned on '" + name + "', so the defaults are used: collection on, no " +
                    "consent required, console output in the editor and development builds.", this);
            }

            AnalyticsSettings settings = _config != null
                ? _config.BuildSettings(Debug.isDebugBuild)
                : new AnalyticsSettings(
                    true,
                    false,
                    AnalyticsSettings.DefaultConsentQueueCapacity,
                    Debug.isDebugBuild,
                    AnalyticsSettings.DefaultHistoryCapacity);

            Container.BindInstance(settings).AsSingle();
            Container.Bind<IAnalyticsStorage>().To<PlayerPrefsAnalyticsStorage>().AsSingle().IfNotBound();
            Container.Bind<IAnalyticsClock>().To<UnityAnalyticsClock>().AsSingle().IfNotBound();
            Container.Bind<IAnalyticsLogger>().To<UnityAnalyticsLogger>().AsSingle().IfNotBound();
            Container.Bind<IAnalyticsConsent>().To<AnalyticsConsent>().AsSingle().IfNotBound();

            // One instance under every role it plays: IAnalytics, IAnalyticsSinkRegistry, IAnalyticsDiagnostics and
            // IDisposable. The last one puts it in the container's DisposableManager, which shuts the sinks down.
            Container.BindInterfacesAndSelfTo<AnalyticsService>().AsSingle();

            if (ShouldLogToConsole())
            {
                Container.Bind<IAnalyticsSink>().To<ConsoleAnalyticsSink>().AsSingle();
            }

            // Attached before anything else initializes, so no tracker can report into a service without its sinks.
            Container.BindInterfacesTo<AnalyticsSinkBinder>().AsSingle();
            Container.BindInitializableExecutionOrder<AnalyticsSinkBinder>(-100);

            Container.Bind<AnalyticsApplicationHooks>()
                .FromNewComponentOnNewGameObject()
                .WithGameObjectName("AnalyticsApplicationHooks")
                .AsSingle()
                .NonLazy();
        }

        private bool ShouldLogToConsole()
        {
            AnalyticsConsoleOutput output = _config != null
                ? _config.ConsoleOutput
                : AnalyticsConsoleOutput.EditorAndDevelopmentBuilds;

            switch (output)
            {
                case AnalyticsConsoleOutput.Always:
                    return true;
                case AnalyticsConsoleOutput.EditorAndDevelopmentBuilds:
                    return Debug.isDebugBuild;
                default:
                    return false;
            }
        }
    }
}
