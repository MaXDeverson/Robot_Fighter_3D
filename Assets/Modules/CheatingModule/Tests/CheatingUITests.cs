using Game.Cheating.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Game.Cheating.Tests
{
    /// <summary>
    /// The components, injected through a real container. Edit Mode runs no Awake, Start or OnEnable, and these
    /// components follow the admin mode from injection on, which is what is checked here.
    /// </summary>
    [TestFixture]
    internal sealed class CheatingUITests : CheatingTestFixture
    {
        [Test]
        public void TheActivatorShowsItsObjectsOnlyWhileTheAdminModeIsOn()
        {
            CheatingService service = AllowedService();
            GameObject panel = Spawn("Panel");
            GameObject button = Spawn("WinButton", panel.transform);
            CheatingContentActivator activator = panel.AddComponent<CheatingContentActivator>();
            SetTargets(activator, button);

            Inject(service, activator);
            Assert.That(button.activeSelf, Is.False, "hidden at once while the mode is off");

            service.SetAdminEnabled(true);
            Assert.That(button.activeSelf, Is.True);

            service.SetAdminEnabled(false);
            Assert.That(button.activeSelf, Is.False);
        }

        [Test]
        public void TheActivatorHidesItsOtherObjectsWhileTheAdminModeIsOn()
        {
            CheatingService service = AllowedService();
            GameObject panel = Spawn("Panel");
            GameObject cheatButton = Spawn("WinButton", panel.transform);
            GameObject playerButton = Spawn("ShopButton", panel.transform);
            CheatingContentActivator activator = panel.AddComponent<CheatingContentActivator>();
            SetTargets(activator, cheatButton);
            SetHidden(activator, playerButton);

            Inject(service, activator);
            Assert.That(playerButton.activeSelf, Is.True, "shown while the mode is off");
            Assert.That(cheatButton.activeSelf, Is.False);

            service.SetAdminEnabled(true);
            Assert.That(playerButton.activeSelf, Is.False);
            Assert.That(cheatButton.activeSelf, Is.True);

            service.SetAdminEnabled(false);
            Assert.That(playerButton.activeSelf, Is.True);
            Assert.That(cheatButton.activeSelf, Is.False);
        }

        [Test]
        public void TheActivatorMayHideItsOwnObject()
        {
            CheatingService service = AllowedService();
            GameObject banner = Spawn("Banner");
            CheatingContentActivator activator = banner.AddComponent<CheatingContentActivator>();
            SetHidden(activator, banner);

            Inject(service, activator);
            service.SetAdminEnabled(true);
            Assert.That(banner.activeSelf, Is.False);

            service.SetAdminEnabled(false);
            Assert.That(banner.activeSelf, Is.True, "it still listens while switched off");
        }

        [Test]
        public void TheActivatorMayListItsOwnObject()
        {
            CheatingService service = AllowedService();
            GameObject button = Spawn("WinButton");
            CheatingContentActivator activator = button.AddComponent<CheatingContentActivator>();
            SetTargets(activator, button);

            Inject(service, activator);
            Assert.That(button.activeSelf, Is.False);

            service.SetAdminEnabled(true);
            Assert.That(button.activeSelf, Is.True, "it still listens while switched off");
        }

        [Test]
        public void AnActivatorDestroyedWithItsSceneStopsListening()
        {
            CheatingService service = AllowedService();
            GameObject panel = Spawn("Panel");
            GameObject button = Spawn("WinButton", panel.transform);
            CheatingContentActivator activator = panel.AddComponent<CheatingContentActivator>();
            SetTargets(activator, button);
            Inject(service, activator);

            Object.DestroyImmediate(panel);

            Assert.DoesNotThrow(() => service.SetAdminEnabled(true));
            Assert.DoesNotThrow(() => service.SetAdminEnabled(false));
        }

        [Test]
        public void TheAdminToggleSwitchesTheModeAndFollowsIt()
        {
            CheatingService service = AllowedService();
            GameObject row = Spawn("AdminRow");
            Toggle toggle = Spawn("Toggle", row.transform).AddComponent<Toggle>();
            AdminToggle adminToggle = toggle.gameObject.AddComponent<AdminToggle>();
            SetRow(adminToggle, row);

            Inject(service, adminToggle);
            Assert.That(row.activeSelf, Is.True);
            Assert.That(toggle.isOn, Is.False);

            toggle.isOn = true;
            Assert.That(service.IsAdminEnabled, Is.True);

            service.SetAdminEnabled(false);
            Assert.That(toggle.isOn, Is.False);
        }

        [Test]
        public void TheAdminToggleRowIsHiddenOnAPlayersPhone()
        {
            GameObject row = Spawn("AdminRow");
            Toggle toggle = Spawn("Toggle", row.transform).AddComponent<Toggle>();
            AdminToggle adminToggle = toggle.gameObject.AddComponent<AdminToggle>();
            SetRow(adminToggle, row);

            Inject(DeniedService(), adminToggle);

            Assert.That(row.activeSelf, Is.False);
        }

        [Test]
        public void TheRevealOpensItsTargetOnlyAfterEnoughQuickTaps()
        {
            GameObject panel = Spawn("DeviceIdPanel");
            panel.SetActive(false);
            MultiTapReveal reveal = Spawn("Version").AddComponent<MultiTapReveal>();
            var serialized = new SerializedObject(reveal);
            serialized.FindProperty("_target").objectReferenceValue = panel;
            serialized.FindProperty("_requiredTaps").intValue = 3;
            serialized.FindProperty("_maxInterval").floatValue = 0.5f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(reveal.RegisterTap(0f), Is.False);
            Assert.That(reveal.RegisterTap(0.4f), Is.False);
            Assert.That(reveal.RegisterTap(1.5f), Is.False, "too slow: the count starts over");
            Assert.That(reveal.RegisterTap(1.9f), Is.False);
            Assert.That(panel.activeSelf, Is.False);

            Assert.That(reveal.RegisterTap(2.2f), Is.True);
            Assert.That(panel.activeSelf, Is.True);
        }

        private static void Inject(ICheatingService service, MonoBehaviour component)
        {
            var container = new DiContainer();
            container.Bind<ICheatingService>().FromInstance(service);
            container.Inject(component);
        }

        private static void SetTargets(CheatingContentActivator activator, params GameObject[] targets)
        {
            SetList(activator, "_shownInAdmin", targets);
        }

        private static void SetHidden(CheatingContentActivator activator, params GameObject[] targets)
        {
            SetList(activator, "_hiddenInAdmin", targets);
        }

        private static void SetList(CheatingContentActivator activator, string field, GameObject[] targets)
        {
            var serialized = new SerializedObject(activator);
            SerializedProperty array = serialized.FindProperty(field);
            array.arraySize = targets.Length;
            for (int i = 0; i < targets.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = targets[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRow(AdminToggle adminToggle, GameObject row)
        {
            var serialized = new SerializedObject(adminToggle);
            serialized.FindProperty("_row").objectReferenceValue = row;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
