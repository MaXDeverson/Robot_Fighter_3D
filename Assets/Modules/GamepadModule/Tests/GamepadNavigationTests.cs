using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.GamepadSupport.Tests
{
    /// <summary>
    /// Which menu has the focus and which buttons a pad button presses. Edit Mode runs no OnEnable, so the components
    /// are wired through their inject methods, the way the container wires them.
    /// </summary>
    [TestFixture]
    internal sealed class GamepadNavigationTests : GamepadTestFixture
    {
        private InputActionAsset _actions;
        private FakeGamepadService _pad;
        private GamepadNavigation _navigation;
        private Gamepad _gamepad;

        public override void Setup()
        {
            base.Setup();
            _actions = Actions();
            _actions.FindActionMap("UI").Enable();
            _pad = new FakeGamepadService();
            _navigation = new GamepadNavigation(_pad, Config(Reference(_actions, "UI/Submit"), Reference(_actions, "UI/Cancel")));
            _gamepad = InputSystem.AddDevice<Gamepad>();
        }

        [Test]
        public void TheMenuOpenedLastHasTheFocus()
        {
            GamepadNavigationScope menu = Menu("Menu");
            GamepadNavigationScope settings = Menu("Settings");
            _navigation.Tick();
            Assert.That(_navigation.Focused, Is.SameAs(settings));

            settings.gameObject.SetActive(false);
            _navigation.Tick();
            Assert.That(_navigation.Focused, Is.SameAs(menu), "closing it hands the focus back");

            menu.gameObject.SetActive(false);
            _navigation.Tick();
            Assert.That(_navigation.Focused, Is.Null);
        }

        [Test]
        public void CancelPressesTheBackButtonOfTheFocusedMenuOnly()
        {
            GamepadNavigationScope menu = Menu("Menu");
            int menuBacks = 0;
            Shortcut("MenuBack", menu.transform, () => menuBacks++, "UI/Cancel");
            GamepadNavigationScope settings = Menu("Settings");
            int settingsBacks = 0;
            Shortcut("SettingsBack", settings.transform, () => settingsBacks++, "UI/Cancel");

            PressAndTick(_gamepad.buttonEast);

            Assert.That(settingsBacks, Is.EqualTo(1));
            Assert.That(menuBacks, Is.Zero);
            Assert.That(_navigation.CanGoBack, Is.True);
        }

        [Test]
        public void AButtonOutsideEveryMenuAnswersOnlyWhileNoMenuIsOpen()
        {
            int pauses = 0;
            Shortcut("HudPause", null, () => pauses++, "Player/Pause");

            PressAndTick(_gamepad.startButton);
            Assert.That(pauses, Is.EqualTo(1));

            GamepadNavigationScope pauseWindow = Menu("PauseWindow");
            int continues = 0;
            Shortcut("Continue", pauseWindow.transform, () => continues++, "UI/Cancel", "Player/Pause");

            PressAndTick(_gamepad.startButton);
            Assert.That(continues, Is.EqualTo(1), "Start closes the pause window");
            Assert.That(pauses, Is.EqualTo(1), "and does not open it again from under it");
        }

        [Test]
        public void OnePadButtonPressesOneButton()
        {
            GamepadNavigationScope window = Menu("Window");
            int clicks = 0;
            Shortcut("Continue", window.transform, () => clicks++, "UI/Cancel", "Player/Pause");
            Shortcut("Close", window.transform, () => clicks++, "UI/Cancel");

            PressAndTick(_gamepad.buttonEast);

            Assert.That(clicks, Is.EqualTo(1));
        }

        [Test]
        public void ALockedButtonIsNotPressed()
        {
            GamepadNavigationScope window = Menu("Window");
            int clicks = 0;
            GamepadButtonShortcut back = Shortcut("Back", window.transform, () => clicks++, "UI/Cancel");
            CanvasGroup group = back.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;

            PressAndTick(_gamepad.buttonEast);

            Assert.That(clicks, Is.Zero);
            Assert.That(_navigation.CanGoBack, Is.False, "no Back prompt either");
        }

        [Test]
        public void AMenuWithoutABackButtonOffersNoBack()
        {
            Menu("WinWindow");
            _navigation.Tick();

            Assert.That(_navigation.CanGoBack, Is.False);
        }

        [Test]
        public void UIReachFollowsTheGroupsUpToTheOneThatIgnoresItsParents()
        {
            GameObject locked = Spawn("LockedHud");
            locked.AddComponent<CanvasGroup>().blocksRaycasts = false;
            GameObject unlocked = Spawn("UnlockedContainer", locked.transform);
            unlocked.AddComponent<CanvasGroup>().ignoreParentGroups = true;
            GameObject letThrough = Spawn("Jump", unlocked.transform);
            GameObject stillLocked = Spawn("PickUp", locked.transform);

            Assert.That(UIReach.IsReachable(letThrough.transform), Is.True);
            Assert.That(UIReach.IsReachable(stillLocked.transform), Is.False);
        }

        private GamepadNavigationScope Menu(string name)
        {
            GamepadNavigationScope scope = Spawn(name).AddComponent<GamepadNavigationScope>();
            scope.Construct(_navigation);
            return scope;
        }

        private GamepadButtonShortcut Shortcut(string name, Transform parent, System.Action onClick, params string[] actions)
        {
            GameObject go = Spawn(name, parent);
            Button button = go.AddComponent<Button>();
            button.onClick.AddListener(() => onClick());
            GamepadButtonShortcut shortcut = go.AddComponent<GamepadButtonShortcut>();
            var references = new InputActionReference[actions.Length];
            for (int i = 0; i < actions.Length; i++)
                references[i] = Reference(_actions, actions[i]);
            shortcut.Configure(references);
            shortcut.Construct(_navigation);
            return shortcut;
        }

        private void PressAndTick(UnityEngine.InputSystem.Controls.ButtonControl control)
        {
            Press(control);
            _navigation.Tick();
            Release(control);
            _navigation.Tick();
        }
    }
}
