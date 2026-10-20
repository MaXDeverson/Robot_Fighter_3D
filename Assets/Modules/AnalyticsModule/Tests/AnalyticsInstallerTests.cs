using System.Collections.Generic;
using System.Text.RegularExpressions;
using Game.Analytics.DI;
using Game.Analytics.Gameplay;
using Game.Events;
using Game.Gameplay.Events;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zenject;

namespace Game.Analytics.Tests
{
    /// <summary>
    /// The module the way the game gets it: through its installers and a real <see cref="DiContainer"/>.
    /// </summary>
    /// <remarks>
    /// Storage is bound before the installers run, so their <c>IfNotBound</c> default never reaches PlayerPrefs and
    /// the suite leaves the player's saved data alone.
    /// </remarks>
    [TestFixture]
    internal sealed class AnalyticsInstallerTests
    {
        private readonly List<Object> _spawned = new List<Object>();

        private DiContainer Container;
        private MemoryAnalyticsStorage _storage;
        private GlobalGameEvents _events;

        [SetUp]
        public void BindTheGameSide()
        {
            Container = new DiContainer(StaticContext.Container);

            _storage = new MemoryAnalyticsStorage();
            Container.Bind<IAnalyticsStorage>().FromInstance(_storage);

            _events = new GlobalGameEvents(new GameEventsSettings(), new EventPathRegistry(), null);
            Container.BindInterfacesAndSelfTo<GlobalGameEvents>().FromInstance(_events);
        }

        [TearDown]
        public void DestroySpawnedObjects()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                {
                    Object.DestroyImmediate(_spawned[i]);
                }
            }

            _spawned.Clear();

            // Only created by a context resolving its roots, which this suite never does — but never leave one behind
            // in the open scene if that ever changes.
            GameObject hooks = GameObject.Find("AnalyticsApplicationHooks");
            if (hooks != null)
            {
                Object.DestroyImmediate(hooks);
            }

            _events.Dispose();
            StaticContext.Clear();
        }

        [Test]
        public void TheServiceIsBoundUnderEveryRoleAsOneInstance()
        {
            Install(Container, AnalyticsConsoleOutput.Off);

            IAnalytics analytics = Container.Resolve<IAnalytics>();
            Assert.That(analytics, Is.TypeOf<AnalyticsService>());
            Assert.That(Container.Resolve<IAnalyticsDiagnostics>(), Is.SameAs(analytics));
            Assert.That(Container.Resolve<IAnalyticsSinkRegistry>(), Is.SameAs(analytics));
            Assert.That(Container.Resolve<AnalyticsService>(), Is.SameAs(analytics));
            Assert.That(Container.Resolve<IAnalyticsConsent>(), Is.Not.Null);
            Assert.That(Container.Resolve<IAnalyticsClock>(), Is.Not.Null);
            Assert.That(Container.Resolve<IAnalyticsStorage>(), Is.SameAs(_storage), "a storage bound first is kept");
        }

        [Test]
        public void TheConsoleSinkFollowsTheConfig()
        {
            Install(Container, AnalyticsConsoleOutput.Always);
            Assert.That(Container.ResolveAll<IAnalyticsSink>(), Has.Some.TypeOf<ConsoleAnalyticsSink>());

            DiContainer quiet = new DiContainer(StaticContext.Container);
            quiet.Bind<IAnalyticsStorage>().FromInstance(new MemoryAnalyticsStorage());
            Install(quiet, AnalyticsConsoleOutput.Off);
            Assert.That(quiet.ResolveAll<IAnalyticsSink>(), Is.Empty);
        }

        [Test]
        public void BoundSinksAreAttachedAndReceiveTheTrackingPlan()
        {
            RecordingAnalyticsSink sink = new RecordingAnalyticsSink();
            Container.Bind<IAnalyticsSink>().FromInstance(sink);
            Install(Container, AnalyticsConsoleOutput.Off);
            InstallGameplay(Container);

            Container.Bind<InitializableManager>().AsSingle();
            Container.Resolve<InitializableManager>().Initialize();

            _events.Publish(new LevelStarted { LevelIndex = 0 });

            Assert.That(sink.InitializeCalls, Is.EqualTo(1));
            Assert.That(sink.Names(), Is.EqualTo(new[] { TrackingPlan.Names.LevelStart }));
        }

        [Test]
        public void ASecondTrackingPlanInAChildContextIsSkipped()
        {
            Install(Container, AnalyticsConsoleOutput.Off);
            InstallGameplay(Container);

            DiContainer child = Container.CreateSubContainer();
            ExpectWarning("tracking plan is already installed", () => InstallGameplay(child));

            Assert.That(child.HasBindingId(typeof(LevelAnalyticsTracker), null, InjectSources.Local), Is.False);
        }

        [Test]
        public void ASecondServiceInAChildContextIsSkipped()
        {
            Install(Container, AnalyticsConsoleOutput.Off);

            DiContainer child = Container.CreateSubContainer();
            ExpectWarning("parent context already provides analytics", () => Install(child, AnalyticsConsoleOutput.Off));

            Assert.That(child.Resolve<IAnalytics>(), Is.SameAs(Container.Resolve<IAnalytics>()));
        }

        /// <summary>Runs an action and asserts it logged a warning matching the pattern.</summary>
        private static void ExpectWarning(string pattern, System.Action action)
        {
            bool logged = false;
            Application.LogCallback capture = (message, stackTrace, type) =>
            {
                logged |= type == LogType.Warning && Regex.IsMatch(message, pattern);
            };

            Application.logMessageReceived += capture;
            try
            {
                action();
            }
            finally
            {
                Application.logMessageReceived -= capture;
            }

            Assert.That(logged, Is.True, "expected a warning matching '" + pattern + "'");
        }

        private void Install(DiContainer container, AnalyticsConsoleOutput console)
        {
            AnalyticsConfig config = ScriptableObject.CreateInstance<AnalyticsConfig>();
            _spawned.Add(config);
            SerializedObject serializedConfig = new SerializedObject(config);
            serializedConfig.FindProperty("_consoleOutput").enumValueIndex = (int)console;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();

            GameObject host = new GameObject("AnalyticsInstaller");
            _spawned.Add(host);
            AnalyticsInstaller installer = host.AddComponent<AnalyticsInstaller>();
            SerializedObject serializedInstaller = new SerializedObject(installer);
            serializedInstaller.FindProperty("_config").objectReferenceValue = config;
            serializedInstaller.ApplyModifiedPropertiesWithoutUndo();

            container.Inject(installer);
            installer.InstallBindings();
            container.FlushBindings();
        }

        private void InstallGameplay(DiContainer container)
        {
            GameObject host = new GameObject("GameplayAnalyticsInstaller");
            _spawned.Add(host);
            GameplayAnalyticsInstaller installer = host.AddComponent<GameplayAnalyticsInstaller>();

            container.Inject(installer);
            installer.InstallBindings();
            container.FlushBindings();
        }
    }
}
