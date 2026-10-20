using System;
using System.Collections.Generic;
using System.Linq;
using Game.Events.DI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zenject;

namespace Game.Events.Tests
{
    /// <summary>
    /// Integration side of the suite: the module is exercised the way a game gets it, through the installer and
    /// a real <see cref="DiContainer"/>.
    /// </summary>
    [TestFixture]
    internal sealed class GlobalGameEventsInstallerTests : ZenjectUnitTestFixture
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDownSpawnedObjects()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_spawned[i]);
                }
            }

            _spawned.Clear();
        }

        [Test]
        public void TheServiceIsBoundUnderEveryRoleAsOneInstance()
        {
            Install(Container);

            IGlobalGameEvents events = Container.Resolve<IGlobalGameEvents>();

            Assert.That(events, Is.Not.Null);
            Assert.That(Container.Resolve<IEventPublisher>(), Is.SameAs(events));
            Assert.That(Container.Resolve<IEventSubscriber>(), Is.SameAs(events));
            Assert.That(Container.Resolve<IEventQueue>(), Is.SameAs(events));
            Assert.That(Container.Resolve<IEventObserverRegistry>(), Is.SameAs(events));
            Assert.That(Container.Resolve<IGameEventsDiagnostics>(), Is.SameAs(events));
            Assert.That(Container.Resolve<GlobalGameEvents>(), Is.SameAs(events));
        }

        [Test]
        public void TheSupportServicesAreBound()
        {
            Install(Container);

            Assert.That(Container.Resolve<GameEventsSettings>(), Is.Not.Null);
            Assert.That(Container.Resolve<IEventPathRegistry>(), Is.Not.Null);
            Assert.That(Container.Resolve<IGameEventsLogger>(), Is.Not.Null);
            Assert.That(Container.Resolve<IEventClock>(), Is.Not.Null);
            Assert.That(Container.Resolve<ISubscriptionLifetime>(), Is.Not.Null);
            Assert.That(Container.Resolve<IEventSubscriptionScope>(), Is.Not.Null);
            Assert.That(Container.Resolve<IEventHistory>(), Is.Not.Null);
        }

        [Test]
        public void TheQueueIsFlushedByTheLateTick()
        {
            Install(Container);
            Container.Bind<TickableManager>().AsSingle();

            System.Collections.Generic.List<ILateTickable> lateTickables = Container.ResolveAll<ILateTickable>();
            Assert.That(lateTickables.Count, Is.EqualTo(1),
                "one late tickable expected, got: " +
                string.Join(", ", lateTickables.Select(t => t.GetType().Name + "#" + t.GetHashCode())));

            IGlobalGameEvents events = Container.Resolve<IGlobalGameEvents>();
            int calls = 0;
            events.Subscribe<SuiteLevelEntered>(e => calls++);
            events.Enqueue(new SuiteLevelEntered());

            // Zenject marks TickableManager.Initialize with [Inject], so resolving it already registers every
            // ILateTickable. Calling Initialize again here would register the pump twice.
            TickableManager tickables = Container.Resolve<TickableManager>();

            Assert.That(calls, Is.Zero, "nothing should run before the tick");

            tickables.LateUpdate();

            Assert.That(calls, Is.EqualTo(1));
            Assert.That(events.PendingCount, Is.Zero);
        }

        [Test]
        public void ObserversBoundInTheContainerAreAttachedOnInitialize()
        {
            Install(Container);
            Container.BindInterfacesAndSelfTo<RecordingObserver>().AsSingle();
            Container.Bind<InitializableManager>().AsSingle();

            IGlobalGameEvents events = Container.Resolve<IGlobalGameEvents>();
            RecordingObserver observer = Container.Resolve<RecordingObserver>();

            events.Publish(new SuiteLevelEntered());
            Assert.That(observer.Paths.Count, Is.Zero, "observers are attached when the context initializes");

            Container.Resolve<InitializableManager>().Initialize();
            events.Publish(new SuiteLevelEntered());

            Assert.That(observer.Paths, Is.EqualTo(new[] { "Suite.Level.Entered" }));
        }

        [Test]
        public void TheHistoryServiceRecordsThroughTheContainer()
        {
            Install(Container);
            Container.Bind<InitializableManager>().AsSingle();
            Container.Resolve<InitializableManager>().Initialize();

            Container.Resolve<IGlobalGameEvents>().Publish(new SuiteLevelEntered { LevelId = "forest" });

            IEventHistory history = Container.Resolve<IEventHistory>();
            Assert.That(history.Count, Is.EqualTo(1));
            Assert.That(history[0].Path, Is.EqualTo("Suite.Level.Entered"));
            Assert.That(history[0].Payload, Does.Contain("forest"));
        }

        [Test]
        public void TearingTheContextDownReleasesEverySubscription()
        {
            Install(Container);
            Container.Bind<DisposableManager>().AsSingle();

            IGlobalGameEvents events = Container.Resolve<IGlobalGameEvents>();
            IEventSubscriptionScope scope = Container.Resolve<IEventSubscriptionScope>();

            int calls = 0;
            scope.Add(events.Subscribe<SuiteLevelEntered>(e => calls++));
            events.Publish(new SuiteLevelEntered());
            Assert.That(calls, Is.EqualTo(1));

            Container.Resolve<DisposableManager>().Dispose();

            Assert.That(Container.Resolve<IGameEventsDiagnostics>().SubscriptionCount, Is.Zero);
            Assert.Throws<ObjectDisposedException>(() => events.Publish(new SuiteLevelEntered()));
            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void ASecondInstallerOnTheSameContextIsSkipped()
        {
            Install(Container);
            GlobalGameEvents first = Container.Resolve<GlobalGameEvents>();

            Install(Container);

            Assert.That(Container.Resolve<GlobalGameEvents>(), Is.SameAs(first),
                "a duplicate installer must not shadow the existing service");
        }

        [Test]
        public void ASceneInstallerReusesTheParentServiceByDefault()
        {
            Install(Container);

            DiContainer child = Container.CreateSubContainer();
            Install(child);

            Assert.That(child.Resolve<IGlobalGameEvents>(), Is.SameAs(Container.Resolve<IGlobalGameEvents>()));
        }

        [Test]
        public void ASceneInstallerCanOptIntoItsOwnService()
        {
            Install(Container);

            DiContainer child = Container.CreateSubContainer();
            Install(child, GlobalGameEventsInstaller.NestedGameEventsPolicy.InstallOwn);

            Assert.That(child.Resolve<IGlobalGameEvents>(), Is.Not.SameAs(Container.Resolve<IGlobalGameEvents>()));
        }

        [Test]
        public void TwoContainersGetTwoIndependentServices()
        {
            Install(Container);
            DiContainer other = new DiContainer();
            Install(other);

            IGlobalGameEvents first = Container.Resolve<IGlobalGameEvents>();
            IGlobalGameEvents second = other.Resolve<IGlobalGameEvents>();

            int onFirst = 0;
            int onSecond = 0;
            first.Subscribe<SuiteLevelEntered>(e => onFirst++);
            second.Subscribe<SuiteLevelEntered>(e => onSecond++);

            first.Publish(new SuiteLevelEntered());

            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(onFirst, Is.EqualTo(1));
            Assert.That(onSecond, Is.Zero);
        }

        [Test]
        public void SubscriptionLifetimeReleasesTheTokenWhenTheOwnerIsDestroyed()
        {
            Install(Container);

            IGlobalGameEvents events = Container.Resolve<IGlobalGameEvents>();
            ISubscriptionLifetime lifetime = Container.Resolve<ISubscriptionLifetime>();

            GameObject owner = new GameObject("Listener");
            _spawned.Add(owner);

            int calls = 0;
            lifetime.Bind(events.Subscribe<SuiteLevelEntered>(e => calls++), owner);

            events.Publish(new SuiteLevelEntered());
            Assert.That(calls, Is.EqualTo(1));

            UnityEngine.Object.DestroyImmediate(owner);
            events.Publish(new SuiteLevelEntered());

            Assert.That(calls, Is.EqualTo(1), "OnDestroy must dispose the token");
            Assert.That(Container.Resolve<IGameEventsDiagnostics>().SubscriptionCount, Is.Zero);
        }

        [Test]
        public void SubscriptionLifetimeDisposesATokenBoundToNothing()
        {
            Install(Container);

            IGlobalGameEvents events = Container.Resolve<IGlobalGameEvents>();
            ISubscriptionLifetime lifetime = Container.Resolve<ISubscriptionLifetime>();

            int calls = 0;
            lifetime.Bind(events.Subscribe<SuiteLevelEntered>(e => calls++), (GameObject)null);

            events.Publish(new SuiteLevelEntered());

            Assert.That(calls, Is.Zero);
            Assert.That(Container.Resolve<IGameEventsDiagnostics>().SubscriptionCount, Is.Zero);
        }

        private void Install(DiContainer container)
        {
            Install(container, GlobalGameEventsInstaller.NestedGameEventsPolicy.ReuseParent);
        }

        private void Install(DiContainer container, GlobalGameEventsInstaller.NestedGameEventsPolicy policy)
        {
            GameObject host = new GameObject("GlobalGameEventsInstaller");
            _spawned.Add(host);

            GlobalGameEventsInstaller installer = host.AddComponent<GlobalGameEventsInstaller>();

            // The start-up validation scans every loaded assembly, including this one, which deliberately
            // declares malformed and duplicated paths. Turn it off so the suite stays hermetic.
            SerializedObject serialized = new SerializedObject(installer);
            serialized.FindProperty("_validateEventPaths").boolValue = false;
            serialized.FindProperty("_nestedEventsPolicy").enumValueIndex = (int)policy;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            container.Inject(installer);
            installer.InstallBindings();
            container.FlushBindings();
        }

        private sealed class RecordingObserver : IEventObserver
        {
            internal List<string> Paths { get; } = new List<string>();

            public void OnPublished(in EventPath path, IGameEvent gameEvent)
            {
                Paths.Add(path.Full);
            }
        }
    }
}
