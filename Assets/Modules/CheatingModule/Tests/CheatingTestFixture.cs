using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Cheating.Tests
{
    /// <summary>Any device the test needs.</summary>
    internal sealed class FakeDeviceInfo : IDeviceInfo
    {
        public string DeviceId { get; set; } = "phone-id";

        public string DeviceModel { get; set; } = "Test Phone";

        public RuntimePlatform Platform { get; set; } = RuntimePlatform.Android;

        public bool IsEditor { get; set; }

        public bool IsDevelopmentBuild { get; set; }

        public static FakeDeviceInfo Phone(string id = "phone-id") => new FakeDeviceInfo { DeviceId = id };

        public static FakeDeviceInfo Editor() => new FakeDeviceInfo
        {
            DeviceId = "editor-id", Platform = RuntimePlatform.WindowsEditor, IsEditor = true,
        };
    }

    /// <summary>Shared setup: assets and objects created for the test and destroyed after it, and a memory storage.</summary>
    internal abstract class CheatingTestFixture
    {
        private readonly List<Object> _spawned = new List<Object>();

        protected MemoryAdminStorage Storage { get; private set; }

        [SetUp]
        public void SetUpFixture()
        {
            Storage = new MemoryAdminStorage();
        }

        [TearDown]
        public void TearDownFixture()
        {
            foreach (Object spawned in _spawned)
            {
                if (spawned != null)
                    Object.DestroyImmediate(spawned);
            }

            _spawned.Clear();
        }

        protected CheatingConfig Config(bool editor = true, bool desktop = true, bool developmentBuild = false,
            params CheatingConfig.AllowedDevice[] devices)
        {
            var config = ScriptableObject.CreateInstance<CheatingConfig>();
            config.Configure(editor, desktop, developmentBuild, devices);
            _spawned.Add(config);
            return config;
        }

        protected CheatDefinition Cheat(string title, Key hotkey = Key.None)
        {
            var cheat = ScriptableObject.CreateInstance<CheatDefinition>();
            cheat.name = title;
            cheat.Configure(title, hotkey);
            _spawned.Add(cheat);
            return cheat;
        }

        protected GameObject Spawn(string name, Transform parent = null)
        {
            var gameObject = new GameObject(name);
            if (parent != null)
                gameObject.transform.SetParent(parent);
            else
                _spawned.Add(gameObject);

            return gameObject;
        }

        /// <summary>A service on a device that may use the admin mode, with the mode off.</summary>
        protected CheatingService AllowedService()
        {
            return new CheatingService(Config(), FakeDeviceInfo.Editor(), Storage);
        }

        /// <summary>A service on a device that may use the admin mode, with the mode on.</summary>
        protected CheatingService EnabledService()
        {
            CheatingService service = AllowedService();
            service.SetAdminEnabled(true);
            return service;
        }

        /// <summary>A service on a player's phone.</summary>
        protected CheatingService DeniedService()
        {
            return new CheatingService(Config(), FakeDeviceInfo.Phone(), Storage);
        }
    }
}
