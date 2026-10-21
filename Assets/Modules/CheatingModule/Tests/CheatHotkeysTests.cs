using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Cheating.Tests
{
    [TestFixture]
    internal sealed class CheatHotkeysTests : CheatingTestFixture
    {
        private readonly HashSet<Key> _pressed = new HashSet<Key>();

        [SetUp]
        public void SetUp()
        {
            _pressed.Clear();
        }

        [Test]
        public void TheHotkeyOfARegisteredCheatRunsIt()
        {
            CheatingService service = EnabledService();
            int runs = 0;
            service.Register(Cheat("Win level", Key.W), () => runs++);
            CheatHotkeys hotkeys = Hotkeys(service);

            hotkeys.Tick();
            _pressed.Add(Key.W);
            hotkeys.Tick();

            Assert.That(runs, Is.EqualTo(1));
        }

        [Test]
        public void HotkeysAreDeadWhileTheAdminModeIsOff()
        {
            CheatingService service = AllowedService();
            int runs = 0;
            service.Register(Cheat("Win level", Key.W), () => runs++);
            _pressed.Add(Key.W);

            Hotkeys(service).Tick();

            Assert.That(runs, Is.Zero);
        }

        [Test]
        public void ACheatWithoutAHotkeyIsNotRunByAnyKey()
        {
            CheatingService service = EnabledService();
            int runs = 0;
            service.Register(Cheat("Add coins"), () => runs++);
            _pressed.Add(Key.None);

            Hotkeys(service).Tick();

            Assert.That(runs, Is.Zero);
        }

        [Test]
        public void ACheatThatChangesTheRegistryDoesNotBreakTheFrame()
        {
            CheatingService service = EnabledService();
            CheatDefinition other = Cheat("Other");
            service.Register(Cheat("Win level", Key.W), () => service.Register(other, () => { }));
            _pressed.Add(Key.W);

            Assert.DoesNotThrow(() => Hotkeys(service).Tick());
            Assert.That(service.RegisteredCheats, Does.Contain(other));
        }

        private CheatHotkeys Hotkeys(ICheatingService service)
        {
            return new CheatHotkeys(service, key => _pressed.Contains(key));
        }
    }
}
