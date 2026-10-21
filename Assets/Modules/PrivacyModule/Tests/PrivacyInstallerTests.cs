using System.Collections.Generic;
using System.Text.RegularExpressions;
using Game.Privacy.DI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zenject;

namespace Game.Privacy.Tests
{
    /// <summary>The module the way the game gets it: through the installer and a real <see cref="DiContainer"/>.</summary>
    [TestFixture]
    internal sealed class PrivacyInstallerTests : ZenjectUnitTestFixture
    {
        private const string PolicyUrl = "https://example.github.io/portal-quest-privacy/";

        private readonly List<Object> _spawned = new List<Object>();

        [TearDown]
        public void TearDownSpawned()
        {
            foreach (Object spawned in _spawned)
            {
                if (spawned != null)
                    Object.DestroyImmediate(spawned);
            }

            _spawned.Clear();
        }

        [Test]
        public void TheServiceRunsOnTheStubCreatedUnderTheInstaller()
        {
            PrivacyInstaller installer = Install(Config(), PrivacyInstaller.ProviderKind.Base, StubPrefab());

            IPrivacyService service = Container.Resolve<IPrivacyService>();
            var provider = (Component)Container.Resolve<IPrivacyConsentProvider>();

            Assert.That(service.PrivacyPolicyUrl, Is.EqualTo(PolicyUrl));
            Assert.That(provider, Is.TypeOf<BasePrivacyConsentProvider>());
            Assert.That(provider.transform.parent, Is.EqualTo(installer.transform));
            Assert.That(Container.ResolveAll<ITickable>(), Has.Some.TypeOf<PrivacyService>());
            _spawned.Add(provider.gameObject);
        }

        [Test]
        public void NetworkRunsOnTheNetworkPrefab()
        {
            BasePrivacyConsentProvider network = StubPrefab();
            network.name = "UmpPrivacyConsentProvider";
            Install(Config(), PrivacyInstaller.ProviderKind.Network, StubPrefab(), network);

            var provider = (Component)Container.Resolve<IPrivacyConsentProvider>();

            Assert.That(provider.name, Is.EqualTo("UmpPrivacyConsentProvider"));
            _spawned.Add(provider.gameObject);
        }

        [Test]
        public void NetworkWithoutAPrefabFallsBackToTheStub()
        {
            LogAssert.Expect(LogType.Error, new Regex("no Network Provider Prefab is assigned"));
            Install(Config(), PrivacyInstaller.ProviderKind.Network, StubPrefab());

            var provider = (Component)Container.Resolve<IPrivacyConsentProvider>();

            Assert.That(provider, Is.TypeOf<BasePrivacyConsentProvider>());
            _spawned.Add(provider.gameObject);
        }

        [Test]
        public void WithoutAnyPrefabAdsAndAnalyticsStayOff()
        {
            LogAssert.Expect(LogType.Error, new Regex("No provider prefab assigned"));
            Install(Config(), PrivacyInstaller.ProviderKind.Base, null);

            IPrivacyService service = Container.Resolve<IPrivacyService>();

            Assert.That(service.CanRequestAds, Is.False);
            Assert.That(service.AnalyticsAllowed, Is.False);
        }

        [Test]
        public void WithoutAConfigTheGameStillBootsButHasNoPolicy()
        {
            LogAssert.Expect(LogType.Error, new Regex("No config assigned"));
            Install(null, PrivacyInstaller.ProviderKind.Base, StubPrefab());

            IPrivacyService service = Container.Resolve<IPrivacyService>();

            Assert.That(service.HasPrivacyPolicy, Is.False);
            _spawned.Add(((Component)Container.Resolve<IPrivacyConsentProvider>()).gameObject);
        }

        private PrivacyInstaller Install(PrivacyConfig config, PrivacyInstaller.ProviderKind kind,
            BasePrivacyConsentProvider basePrefab, PrivacyConsentProvider networkPrefab = null)
        {
            var host = new GameObject("PrivacyService");
            _spawned.Add(host);

            PrivacyInstaller installer = host.AddComponent<PrivacyInstaller>();
            installer.Setup(config, kind, basePrefab, networkPrefab);
            Container.Inject(installer);
            installer.InstallBindings();
            return installer;
        }

        private PrivacyConfig Config()
        {
            var config = ScriptableObject.CreateInstance<PrivacyConfig>();
            config.Configure(PolicyUrl);
            _spawned.Add(config);
            return config;
        }

        private BasePrivacyConsentProvider StubPrefab()
        {
            var prefab = new GameObject("StubPrefab");
            prefab.SetActive(false);
            _spawned.Add(prefab);
            return prefab.AddComponent<BasePrivacyConsentProvider>();
        }
    }
}
