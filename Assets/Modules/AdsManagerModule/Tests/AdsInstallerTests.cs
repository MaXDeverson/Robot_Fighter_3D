using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Zenject;

namespace Game.Ads.Tests
{
    /// <summary>
    /// The module the way the game gets it: the installer, the config asset and the provider prefabs in a real
    /// <see cref="DiContainer"/>.
    /// </summary>
    /// <remarks>
    /// Storage, clock, pause and level source are bound before the installer runs, so its <c>IfNotBound</c> defaults
    /// never reach PlayerPrefs, <c>Time.timeScale</c> or the audio listener.
    /// </remarks>
    [TestFixture]
    internal sealed class AdsInstallerTests : ZenjectUnitTestFixture
    {
        private const string ConfigPath = "Assets/Modules/AdsManagerModule/Configs/AdsConfig.asset";
        private const string ProviderPrefabPath = "Assets/Modules/AdsManagerModule/Prefabs/BaseAdsProvider.prefab";

        private readonly List<Object> _spawned = new List<Object>();

        [SetUp]
        public void BindTheSeams()
        {
            Container.Bind<IAdsStorage>().FromInstance(new MemoryAdsStorage());
            Container.Bind<IAdsClock>().FromInstance(new FakeAdsClock());
            Container.Bind<IAdsGamePause>().FromInstance(new FakeGamePause());
            Container.Bind<IAdsLevelSource>().FromInstance(new FakeLevelSource());
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
        }

        [Test]
        public void TheStubIsCreatedFromThePrefabUnderTheInstaller()
        {
            GameObject host = Install(Container, true);

            Assert.That(Container.Resolve<IAdsManagerService>(), Is.TypeOf<AdsManagerService>());

            BaseAdsProvider stub = Container.Resolve<IAdsProvider>() as BaseAdsProvider;
            Assert.That(stub, Is.Not.Null);
            Assert.That(stub.gameObject.name, Is.EqualTo("BaseAdsProvider"));
            Assert.That(stub.transform.parent, Is.SameAs(host.transform), "a GameObject of its own under the installer");
        }

        [Test]
        public void AProviderBoundBeforeTheInstallerReplacesTheStub()
        {
            FakeAdsProvider network = new FakeAdsProvider();
            Container.Bind<IAdsProvider>().FromInstance(network);
            Install(Container, true);

            IAdsManagerService ads = Container.Resolve<IAdsManagerService>();
            List<AdResult> results = new List<AdResult>();
            ads.ShowRewarded(results.Add);
            network.Close(AdStatus.Completed);

            Assert.That(Container.Resolve<IAdsProvider>(), Is.SameAs(network));
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].IsSuccess, Is.True);
        }

        [Test]
        public void WithoutAPrefabOrAProviderNoAdIsEverReady()
        {
            LogAssert.Expect(LogType.Error, new Regex("No provider prefab assigned"));
            Install(Container, false);

            List<AdResult> results = new List<AdResult>();
            Container.Resolve<IAdsManagerService>().ShowRewarded(results.Add);

            Assert.That(Container.Resolve<IAdsProvider>(), Is.TypeOf<NullAdsProvider>());
            Assert.That(results[0].Status, Is.EqualTo(AdStatus.NotReady));
        }

        [Test]
        public void NetworkCreatesTheProviderFromTheNetworkPrefab()
        {
            // Any prefab with a provider on its root will do; the stub's stands in for AdMob's.
            GameObject network = AssetDatabase.LoadAssetAtPath<GameObject>(ProviderPrefabPath);
            GameObject host = Install(Container, false, AdsManagerInstaller.ProviderChoice.Network, network);

            BaseAdsProvider provider = Container.Resolve<IAdsProvider>() as BaseAdsProvider;
            Assert.That(provider, Is.Not.Null);
            Assert.That(provider.gameObject.name, Is.EqualTo(network.name));
            Assert.That(provider.transform.parent, Is.SameAs(host.transform), "a GameObject of its own under the installer");
        }

        [Test]
        public void NetworkWithoutAPrefabDoesNotFallBackToTheStub()
        {
            LogAssert.Expect(LogType.Error, new Regex("No provider prefab assigned .* for Network"));
            Install(Container, true, AdsManagerInstaller.ProviderChoice.Network);

            Assert.That(Container.Resolve<IAdsProvider>(), Is.TypeOf<NullAdsProvider>());
        }

        [Test]
        public void NetworkWithAPrefabThatIsNoProviderShowsNoAd()
        {
            GameObject notAProvider = new GameObject("NotAProvider");
            _spawned.Add(notAProvider);

            LogAssert.Expect(LogType.Error, new Regex("'NotAProvider'.* has no ads provider"));
            Install(Container, true, AdsManagerInstaller.ProviderChoice.Network, notAProvider);

            Assert.That(Container.Resolve<IAdsProvider>(), Is.TypeOf<NullAdsProvider>());
        }

        [Test]
        public void ASecondManagerInAChildContextIsSkipped()
        {
            Container.Bind<IAdsProvider>().FromInstance(new FakeAdsProvider());
            Install(Container, false);

            DiContainer child = Container.CreateSubContainer();
            LogAssert.Expect(LogType.Warning, new Regex("parent context already provides the ads manager"));
            Install(child, false);

            Assert.That(child.Resolve<IAdsManagerService>(), Is.SameAs(Container.Resolve<IAdsManagerService>()));
        }

        [Test]
        public void TheConfigHoldsTheAgreedRules()
        {
            AdsConfig config = AssetDatabase.LoadAssetAtPath<AdsConfig>(ConfigPath);
            AdsSettings settings = config.BuildSettings(true);

            Assert.That(settings.BannerFromLevel, Is.EqualTo(10));
            Assert.That(settings.InterstitialFromLevel, Is.EqualTo(6));
            Assert.That(settings.InterstitialCooldownSeconds, Is.EqualTo(180));
            Assert.That(settings.RewardedFromLevel, Is.EqualTo(1));
            Assert.That(config.BuildSettings(false).LogRequests, Is.False, "a release build never logs requests");
        }

        [Test]
        public void TheStubPrefabIsWired()
        {
            BaseAdsProvider prefab = AssetDatabase.LoadAssetAtPath<BaseAdsProvider>(ProviderPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            SerializedObject serialized = new SerializedObject(prefab);
            string[] views = { "_banner", "_fullscreen", "_title", "_timer" };
            for (int i = 0; i < views.Length; i++)
            {
                Assert.That(serialized.FindProperty(views[i]).objectReferenceValue, Is.Not.Null, views[i]);
            }

            Assert.That(serialized.FindProperty("_interstitialDuration").floatValue, Is.EqualTo(2f));
            Assert.That(serialized.FindProperty("_rewardedDuration").floatValue, Is.EqualTo(2f));

            Canvas canvas = prefab.GetComponent<Canvas>();
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(canvas.sortingOrder, Is.GreaterThanOrEqualTo(30000), "over every screen of the game");
        }

        private GameObject Install(
            DiContainer container,
            bool withBasePrefab,
            AdsManagerInstaller.ProviderChoice provider = AdsManagerInstaller.ProviderChoice.Base,
            GameObject networkPrefab = null)
        {
            GameObject host = new GameObject("Ads");
            _spawned.Add(host);
            AdsManagerInstaller installer = host.AddComponent<AdsManagerInstaller>();

            SerializedObject serialized = new SerializedObject(installer);
            serialized.FindProperty("_config").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AdsConfig>(ConfigPath);
            serialized.FindProperty("_provider").enumValueIndex = (int)provider;
            serialized.FindProperty("_baseProviderPrefab").objectReferenceValue =
                withBasePrefab ? AssetDatabase.LoadAssetAtPath<BaseAdsProvider>(ProviderPrefabPath) : null;
            serialized.FindProperty("_networkProviderPrefab").objectReferenceValue = networkPrefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            container.Inject(installer);
            installer.InstallBindings();
            container.FlushBindings();
            return host;
        }
    }
}
