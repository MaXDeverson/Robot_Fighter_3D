using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.XInput;
using UnityEngine.UI;

namespace Game.GamepadSupport.Tests
{
    /// <summary>Which pad is in hand, and what its buttons look like.</summary>
    [TestFixture]
    internal sealed class GamepadGlyphTests : GamepadTestFixture
    {
        [Test]
        public void PadsAreToldApartByTheirLayout()
        {
            Assert.That(GamepadFamilyResolver.Resolve(InputSystem.AddDevice<DualShock4GamepadHID>()), Is.EqualTo(GamepadFamily.PlayStation));
            Assert.That(GamepadFamilyResolver.Resolve(InputSystem.AddDevice<XInputController>()), Is.EqualTo(GamepadFamily.Xbox));
            Assert.That(GamepadFamilyResolver.Resolve(InputSystem.AddDevice("SwitchProControllerHID")), Is.EqualTo(GamepadFamily.Switch));
        }

        [Test]
        public void APadWithACommonLayoutIsToldApartByItsName()
        {
            // What Android does with most pads: one common layout, whoever made the pad.
            InputSystem.RegisterLayout(@"{ ""name"" : ""TestCommonPad"", ""extend"" : ""Gamepad"" }");
            InputSystem.RegisterLayoutMatcher("TestCommonPad", new InputDeviceMatcher().WithInterface("TestPad"));
            InputDevice sony = InputSystem.AddDevice(new InputDeviceDescription
            {
                interfaceName = "TestPad", product = "Wireless Controller", manufacturer = "Sony Interactive Entertainment",
            });
            InputDevice unknown = InputSystem.AddDevice(new InputDeviceDescription { interfaceName = "TestPad", product = "Pad" });

            Assert.That(GamepadFamilyResolver.Resolve(sony), Is.EqualTo(GamepadFamily.PlayStation));
            Assert.That(GamepadFamilyResolver.Resolve(unknown), Is.EqualTo(GamepadFamily.Xbox), "anything else is drawn as Xbox");
        }

        [Test]
        public void AnActionIsFoundOnThePadByItsFirstBindingThatFits()
        {
            InputActionAsset actions = Actions();
            Gamepad pad = InputSystem.AddDevice<Gamepad>();

            Assert.That(GamepadControlFinder.FindControlPath(actions.FindAction("Player/Jump"), pad), Is.EqualTo("buttonSouth"));
            Assert.That(GamepadControlFinder.FindControlPath(actions.FindAction("Player/MoveLeft"), pad), Is.EqualTo("leftStick/left"));
            Assert.That(GamepadControlFinder.FindControlPath(actions.FindAction("Player/Fly"), pad), Is.Null, "keyboard only");
        }

        [Test]
        public void ConfirmIsWhereverThePadPutsIt()
        {
            InputActionAsset actions = Actions();
            InputAction submit = actions.FindAction("UI/Submit");

            Assert.That(GamepadControlFinder.FindControlPath(submit, InputSystem.AddDevice<XInputController>()), Is.EqualTo("buttonSouth"));
            Assert.That(GamepadControlFinder.FindControlPath(submit, InputSystem.AddDevice("SwitchProControllerHID")), Is.EqualTo("buttonEast"),
                "Nintendo confirms with the right-hand button");
        }

        [Test]
        public void AControlWithNoGlyphOfItsOwnIsDrawnAsTheOneAboveIt()
        {
            Sprite stick = Glyph("stick");
            Sprite left = Glyph("stick left");
            GamepadGlyphSet set = GlyphSet(GamepadFamily.Xbox, ("leftStick", stick), ("rightStick/left", left));

            Assert.That(set.Find("leftStick/left"), Is.SameAs(stick));
            Assert.That(set.Find("rightStick/left"), Is.SameAs(left));
            Assert.That(set.Find("buttonSouth"), Is.Null);
        }

        [Test]
        public void TheServiceDrawsTheGlyphOfThePadInHand()
        {
            InputActionAsset actions = Actions();
            Sprite a = Glyph("A");
            Sprite cross = Glyph("cross");
            GamepadConfig config = Config(null, null,
                GlyphSet(GamepadFamily.Xbox, ("buttonSouth", a)),
                GlyphSet(GamepadFamily.PlayStation, ("buttonSouth", cross)));
            var service = new GamepadService(config);
            service.Initialize();
            int changes = 0;
            service.Changed += () => changes++;

            try
            {
                Assert.That(service.IsConnected, Is.False);
                Assert.That(service.GetGlyph(actions.FindAction("Player/Jump")), Is.Null, "nothing without a pad");

                DualShock4GamepadHID dualShock = InputSystem.AddDevice<DualShock4GamepadHID>();
                Assert.That(service.IsConnected, Is.True);
                Assert.That(service.Family, Is.EqualTo(GamepadFamily.PlayStation));
                Assert.That(service.GetGlyph(actions.FindAction("Player/Jump")), Is.SameAs(cross));
                Assert.That(service.GetGlyph(actions.FindAction("Player/Fly")), Is.Null, "an action with nothing on a pad");

                InputSystem.RemoveDevice(dualShock);
                Assert.That(service.IsConnected, Is.False);
                Assert.That(changes, Is.EqualTo(2), "plugged in, then out");
            }
            finally
            {
                service.Dispose();
            }
        }

        [Test]
        public void ABadgeStandsInForTheTouchHintWhileAPadIsConnected()
        {
            var gamepad = new FakeGamepadService { IsConnected = false, Glyph = Glyph("buttonNorth") };
            GameObject hint = Spawn("Hint");
            GameObject touch = Spawn("Touch", hint.transform);
            GameObject prompt = Spawn("Prompt", hint.transform);
            Image icon = Spawn("Icon", prompt.transform).AddComponent<Image>();

            var badge = hint.AddComponent<GamepadPromptBadge>();
            badge.Configure(Reference(Actions(), "Player/Jump"), prompt, icon, touch);
            badge.Construct(gamepad);

            Assert.That(touch.activeSelf, Is.True, "no pad: the touch hint");
            Assert.That(prompt.activeSelf, Is.False);

            gamepad.IsConnected = true;
            gamepad.RaiseChanged();
            Assert.That(prompt.activeSelf, Is.True, "a pad: its prompt");
            Assert.That(icon.sprite, Is.SameAs(gamepad.Glyph));
            Assert.That(touch.activeSelf, Is.False);

            gamepad.IsConnected = false;
            gamepad.RaiseChanged();
            Assert.That(touch.activeSelf, Is.True, "unplugged: the touch hint is back");
            Assert.That(prompt.activeSelf, Is.False);
        }

        [Test]
        public void AnotherPadTakesOverWhenTheOneInHandIsUnplugged()
        {
            var service = new GamepadService(Config(null, null));
            service.Initialize();

            try
            {
                XInputController xbox = InputSystem.AddDevice<XInputController>();
                DualShock4GamepadHID dualShock = InputSystem.AddDevice<DualShock4GamepadHID>();
                Press(dualShock.buttonSouth);
                service.Tick();
                Assert.That(service.Current, Is.SameAs(dualShock), "the pad used last");

                InputSystem.RemoveDevice(dualShock);
                Assert.That(service.Current, Is.SameAs(xbox));
                Assert.That(service.Family, Is.EqualTo(GamepadFamily.Xbox));
            }
            finally
            {
                service.Dispose();
            }
        }
    }
}
