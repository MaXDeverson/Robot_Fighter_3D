using UnityEngine;
using Zenject;

namespace Game.Events.DI
{
    /// <summary>
    /// Registers a complete event service into the context this component belongs to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Drop it on a child of <c>ProjectContext</c> for one service that outlives scene loads, or on a child of a
    /// <c>SceneContext</c> for a service scoped to that scene. Both are supported; what is never wanted is one of
    /// each by accident, because the scene's systems would then talk to a different service than the project's.
    /// <see cref="NestedGameEventsPolicy"/> decides what happens when a parent context already has a service.
    /// </para>
    /// <para>
    /// Everything the module needs is bound here: the service itself under each of its role interfaces, the path
    /// registry, the console logger, the clock, the per-context subscription scope, the lifetime service for
    /// MonoBehaviours, the late-tick queue pump, the observer binder and, when enabled, the debug history.
    /// </para>
    /// </remarks>
    public sealed class GlobalGameEventsInstaller : MonoInstaller
    {
        /// <summary>What to do when a parent context already provides an <see cref="IGlobalGameEvents"/>.</summary>
        public enum NestedGameEventsPolicy
        {
            /// <summary>Skip installing and let everything resolve the parent's service. The safe default.</summary>
            ReuseParent = 0,

            /// <summary>Install a second, scene-local service on purpose. Events do not cross between the two.</summary>
            InstallOwn = 1,

            /// <summary>Treat it as a setup error and stop the context from building.</summary>
            Fail = 2
        }

        [Header("Configuration")]
        [SerializeField]
        [Tooltip("Optional shared config asset. When empty, the inline settings below are used.")]
        private GameEventsConfig _config;

        [Header("Inline settings (used when no config asset is assigned)")]
        [SerializeField]
        [Min(1)]
        [Tooltip("How deep publishes may nest before the chain is treated as runaway recursion and throws.")]
        private int _maxDispatchDepth = GameEventsSettings.DefaultMaxDispatchDepth;

        [SerializeField]
        [Min(1)]
        [Tooltip("Upper bound on how many queued events a single late-tick flush delivers.")]
        private int _maxEventsPerFlush = GameEventsSettings.DefaultMaxEventsPerFlush;

        [SerializeField]
        [Min(1)]
        [Tooltip("How many events the debugger window can show.")]
        private int _historyCapacity = GameEventsSettings.DefaultHistoryCapacity;

        [SerializeField]
        [Tooltip("Record the event history. Costs one boxing allocation per published event. Editor and development builds only.")]
        private bool _recordHistory = true;

        [SerializeField]
        [Tooltip("Write every dispatch and subscription change to the console. Editor and development builds only.")]
        private bool _logEventFlow;

        [SerializeField]
        [Tooltip("Throw when the service is used off the thread that created it. Editor and development builds only.")]
        private bool _verifyMainThread = true;

        [Header("Safety")]
        [SerializeField]
        [Tooltip("What to do when a parent context already provides an event service.")]
        private NestedGameEventsPolicy _nestedEventsPolicy = NestedGameEventsPolicy.ReuseParent;

        [SerializeField]
        [Tooltip("Resolve every event type's path while installing, so a malformed or duplicated [EventPath] " +
                 "is reported once at startup instead of the first time that event is published. " +
                 "Editor and development builds only; it also warms the path cache.")]
        private bool _validateEventPaths = true;

        /// <inheritdoc />
        public override void InstallBindings()
        {
            if (Container.HasBindingId(typeof(IGlobalGameEvents), null, InjectSources.Local))
            {
                // Two GlobalGameEventsInstaller components on one context. This is never deliberate: the second set of
                // bindings would shadow the first and the two halves of the game would stop hearing each other.
                Debug.LogWarning(
                    "[GameEvents] This context already installs an event service, so the installer on '" +
                    name + "' was skipped. Keep exactly one GlobalGameEventsInstaller per context.", this);
                return;
            }

            if (Container.HasBindingId(typeof(IGlobalGameEvents), null, InjectSources.AnyParent))
            {
                switch (_nestedEventsPolicy)
                {
                    case NestedGameEventsPolicy.ReuseParent:
                        Debug.LogWarning(
                            "[GameEvents] A parent context already provides an IGlobalGameEvents, so the installer on '" +
                            name + "' was skipped and everything here will resolve the parent's service. Set the " +
                            "nested service policy to InstallOwn if a separate scene-local service is what you want.",
                            this);
                        return;

                    case NestedGameEventsPolicy.Fail:
                        throw new GameEventsException(
                            "A parent context already provides an IGlobalGameEvents and the installer on '" + name +
                            "' is set to fail on that. Remove one of the two installers.");
                }
            }

            GameEventsSettings settings = _config != null ? _config.BuildSettings() : BuildInlineSettings();

            // Built here rather than left to the container so the validation pass below can warm the very cache
            // the service will use, instead of throwing the work away.
            EventPathRegistry registry = new EventPathRegistry();

            Container.BindInstance(settings).AsSingle();
            Container.Bind<IEventPathRegistry>().FromInstance(registry).AsSingle();
            Container.Bind<IGameEventsLogger>().To<UnityGameEventsLogger>().AsSingle();
            Container.Bind<IEventClock>().To<UnityEventClock>().AsSingle();

            // One instance under every role it plays: IGlobalGameEvents, IEventPublisher, IEventQueue, IEventSubscriber,
            // IEventObserverRegistry, IGameEventsDiagnostics and IDisposable. The last one is what puts the service in
            // the container's DisposableManager, so tearing this context down releases every subscription.
            Container.BindInterfacesAndSelfTo<GlobalGameEvents>().AsSingle();

            Container.Bind<ISubscriptionLifetime>().To<SubscriptionLifetimeService>().AsSingle();

            // The context-lifetime bag for plain C# systems. Also disposed by the DisposableManager.
            Container.BindInterfacesAndSelfTo<SubscriptionBag>().AsSingle();

            // Only the editor's debugger window reads the history, and recording it describes every published event
            // through reflection — measurable on a phone, so a release build never pays for it.
            if (settings.RecordHistory && Debug.isDebugBuild)
            {
                Container.BindInterfacesAndSelfTo<EventHistoryService>().AsSingle();
            }

            Container.BindInterfacesTo<EventQueuePump>().AsSingle().NonLazy();
            Container.BindInterfacesTo<GameEventsObserverBinder>().AsSingle().NonLazy();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_validateEventPaths)
            {
                ValidateEventPaths(registry);
            }
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Resolves every declared event path once, so a missing attribute, a malformed path or two types
        /// claiming the same path all surface here rather than at the first publish.
        /// </summary>
        /// <param name="registry">The registry the service will use, so the work doubles as a cache warm-up.</param>
        public void ValidateEventPaths(IEventPathRegistry registry)
        {
            EventTypeScanner scanner = new EventTypeScanner();
            System.Collections.Generic.List<string> problems = new System.Collections.Generic.List<string>();

            if (registry.TryValidate(scanner.FindEventTypes(), problems))
            {
                return;
            }

            for (int i = 0; i < problems.Count; i++)
            {
                Debug.LogError("[GameEvents] " + problems[i], this);
            }
        }
#endif

        private GameEventsSettings BuildInlineSettings()
        {
            return new GameEventsSettings(
                _maxDispatchDepth,
                _historyCapacity,
                _maxEventsPerFlush,
                _logEventFlow,
                _recordHistory,
                _verifyMainThread);
        }
    }
}
