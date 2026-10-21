using System.Collections.Generic;
using System.IO;
using Game.Cheating.DI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zenject;

namespace Game.Cheating.Tests
{
    /// <summary>The module the way the game gets it: through the installer and a real <see cref="DiContainer"/>.</summary>
    [TestFixture]
    internal sealed class CheatingInstallerTests : ZenjectUnitTestFixture
    {
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
        public void TheServiceAndTheHotkeysAreBound()
        {
            Container.Bind<IDeviceInfo>().FromInstance(FakeDeviceInfo.Editor());
            Container.Bind<IAdminStorage>().To<MemoryAdminStorage>().AsSingle();
            Install(Config());

            Assert.That(Container.Resolve<ICheatingService>().Access, Is.EqualTo(AdminAccess.Editor));
            Assert.That(Container.ResolveAll<ITickable>(), Has.Some.TypeOf<CheatHotkeys>());
        }

        [Test]
        public void ADeviceAndAStorageBoundBeforeTheInstallerAreUsed()
        {
            var storage = new MemoryAdminStorage { IsAdminEnabled = true };
            Container.Bind<IDeviceInfo>().FromInstance(FakeDeviceInfo.Phone("a1b2c3"));
            Container.Bind<IAdminStorage>().FromInstance(storage);
            var config = Config();
            config.Configure(false, false, false, new CheatingConfig.AllowedDevice("Max", "a1b2c3"));
            Install(config);

            ICheatingService service = Container.Resolve<ICheatingService>();

            Assert.That(service.Access, Is.EqualTo(AdminAccess.AllowedDevice));
            Assert.That(service.IsAdminEnabled, Is.True);
        }

        [Test]
        public void WithoutAConfigNobodyIsAllowedButTheGameStillBoots()
        {
            Container.Bind<IDeviceInfo>().FromInstance(FakeDeviceInfo.Editor());
            Container.Bind<IAdminStorage>().To<MemoryAdminStorage>().AsSingle();
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, "[Cheating] CheatingInstaller has no config.");
            Install(null);

            Assert.That(Container.Resolve<ICheatingService>().IsAdminAvailable, Is.False);
        }

        [Test]
        public void TheFileStorageKeepsTheSwitchInAFolderOfItsOwn()
        {
            string folder = Path.Combine(Path.GetTempPath(), "CheatingTests_" + System.Guid.NewGuid().ToString("N"));
            string path = Path.Combine(folder, "Cheating", "admin_enabled");

            try
            {
                Assert.That(new FileAdminStorage(path).IsAdminEnabled, Is.False, "nothing saved yet");

                new FileAdminStorage(path).IsAdminEnabled = true;
                Assert.That(new FileAdminStorage(path).IsAdminEnabled, Is.True);
                Assert.That(Directory.GetFiles(folder), Is.Empty, "the top of the folder is what Reset game wipes");

                new FileAdminStorage(path).IsAdminEnabled = false;
                Assert.That(new FileAdminStorage(path).IsAdminEnabled, Is.False);
            }
            finally
            {
                if (Directory.Exists(folder))
                    Directory.Delete(folder, true);
            }
        }

        private CheatingConfig Config()
        {
            var config = ScriptableObject.CreateInstance<CheatingConfig>();
            config.Configure(true, true, false);
            _spawned.Add(config);
            return config;
        }

        private void Install(CheatingConfig config)
        {
            var gameObject = new GameObject("Cheating");
            _spawned.Add(gameObject);
            CheatingInstaller installer = gameObject.AddComponent<CheatingInstaller>();

            var serialized = new SerializedObject(installer);
            serialized.FindProperty("_config").objectReferenceValue = config;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Container.Inject(installer);
            installer.InstallBindings();
            Container.FlushBindings();
        }
    }
}
