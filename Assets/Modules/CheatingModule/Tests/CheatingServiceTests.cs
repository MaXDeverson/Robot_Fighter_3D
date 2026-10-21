using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Cheating.Tests
{
    [TestFixture]
    internal sealed class CheatingServiceTests : CheatingTestFixture
    {
        [Test]
        public void TheAdminModeStartsOffAndIsKeptOnceTurnedOn()
        {
            CheatingService service = AllowedService();
            Assert.That(service.IsAdminAvailable, Is.True);
            Assert.That(service.IsAdminEnabled, Is.False);

            service.SetAdminEnabled(true);

            Assert.That(service.IsAdminEnabled, Is.True);
            Assert.That(Storage.IsAdminEnabled, Is.True);
            Assert.That(AllowedService().IsAdminEnabled, Is.True, "the next launch starts with the mode on");
        }

        [Test]
        public void TurningTheAdminModeOffIsKeptToo()
        {
            CheatingService service = EnabledService();

            service.SetAdminEnabled(false);

            Assert.That(service.IsAdminEnabled, Is.False);
            Assert.That(Storage.IsAdminEnabled, Is.False);
        }

        [Test]
        public void APlayersPhoneCanNotTurnTheAdminModeOn()
        {
            CheatingService service = DeniedService();

            LogAssert.Expect(LogType.Warning, new Regex("not available"));
            service.SetAdminEnabled(true);

            Assert.That(service.IsAdminAvailable, Is.False);
            Assert.That(service.IsAdminEnabled, Is.False);
            Assert.That(Storage.IsAdminEnabled, Is.False);
        }

        [Test]
        public void ASavedSwitchMeansNothingOnAPlayersPhone()
        {
            Storage.IsAdminEnabled = true;

            Assert.That(DeniedService().IsAdminEnabled, Is.False);
        }

        [Test]
        public void ListenersHearOnlyRealChanges()
        {
            CheatingService service = AllowedService();
            var heard = new List<bool>();
            service.AdminEnabledChanged += heard.Add;

            service.SetAdminEnabled(true);
            service.SetAdminEnabled(true);
            service.SetAdminEnabled(false);

            Assert.That(heard, Is.EqualTo(new[] { true, false }));
        }

        [Test]
        public void AListenerThatThrowsDoesNotSilenceTheOthers()
        {
            CheatingService service = AllowedService();
            bool heard = false;
            service.AdminEnabledChanged += _ => throw new InvalidOperationException("listener failed");
            service.AdminEnabledChanged += _ => heard = true;

            LogAssert.Expect(LogType.Exception, new Regex("listener failed"));
            service.SetAdminEnabled(true);

            Assert.That(heard, Is.True);
        }

        [Test]
        public void ACheatRunsOnlyWhileTheAdminModeIsOn()
        {
            CheatingService service = AllowedService();
            CheatDefinition win = Cheat("Win level");
            int runs = 0;
            service.Register(win, () => runs++);

            LogAssert.Expect(LogType.Warning, new Regex("admin mode is off"));
            Assert.That(service.CanExecute(win), Is.False);
            Assert.That(service.Execute(win), Is.False);

            service.SetAdminEnabled(true);

            Assert.That(service.CanExecute(win), Is.True);
            Assert.That(service.Execute(win), Is.True);
            Assert.That(runs, Is.EqualTo(1));
        }

        [Test]
        public void ACheatNobodyHandlesIsRefused()
        {
            CheatingService service = EnabledService();
            CheatDefinition win = Cheat("Win level");

            LogAssert.Expect(LogType.Warning, new Regex("Nothing in the loaded scenes handles 'Win level'"));

            Assert.That(service.CanExecute(win), Is.False);
            Assert.That(service.Execute(win), Is.False);
        }

        [Test]
        public void TheLastHandlerRunsAndTheEarlierOneComesBackWhenItGoes()
        {
            CheatingService service = EnabledService();
            CheatDefinition win = Cheat("Win level");
            var runs = new List<string>();
            service.Register(win, () => runs.Add("first"));
            IDisposable second = service.Register(win, () => runs.Add("second"));

            service.Execute(win);
            second.Dispose();
            service.Execute(win);

            Assert.That(runs, Is.EqualTo(new[] { "second", "first" }));
        }

        [Test]
        public void ADisposedHandlerLeavesTheRegistry()
        {
            CheatingService service = EnabledService();
            CheatDefinition win = Cheat("Win level");
            IDisposable registration = service.Register(win, () => { });
            Assert.That(service.RegisteredCheats, Is.EquivalentTo(new[] { win }));

            registration.Dispose();
            registration.Dispose();

            Assert.That(service.RegisteredCheats, Is.Empty);
            Assert.That(service.CanExecute(win), Is.False);
        }

        [Test]
        public void AHandlerThatThrowsIsLoggedAndReportedAsNotRun()
        {
            CheatingService service = EnabledService();
            CheatDefinition win = Cheat("Win level");
            service.Register(win, () => throw new InvalidOperationException("cheat failed"));

            LogAssert.Expect(LogType.Exception, new Regex("cheat failed"));

            Assert.That(service.Execute(win), Is.False);
        }

        [Test]
        public void RegisteringNeedsACheatAndAnAction()
        {
            CheatingService service = EnabledService();

            Assert.Throws<ArgumentNullException>(() => service.Register(null, () => { }));
            Assert.Throws<ArgumentNullException>(() => service.Register(Cheat("Win level"), null));
        }
    }
}
