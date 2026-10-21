using NUnit.Framework;
using UnityEngine;

namespace Game.Cheating.Tests
{
    [TestFixture]
    internal sealed class AdminAccessPolicyTests : CheatingTestFixture
    {
        [Test]
        public void ThePhoneOfAPlayerIsDenied()
        {
            Assert.That(AdminAccessPolicy.Resolve(Config(), FakeDeviceInfo.Phone()), Is.EqualTo(AdminAccess.Denied));
        }

        [Test]
        public void TheEditorIsAllowedUnlessTheConfigSaysOtherwise()
        {
            Assert.That(AdminAccessPolicy.Resolve(Config(editor: true), FakeDeviceInfo.Editor()), Is.EqualTo(AdminAccess.Editor));
            Assert.That(AdminAccessPolicy.Resolve(Config(editor: false), FakeDeviceInfo.Editor()), Is.EqualTo(AdminAccess.Denied));
        }

        [TestCase(RuntimePlatform.WindowsPlayer)]
        [TestCase(RuntimePlatform.OSXPlayer)]
        [TestCase(RuntimePlatform.LinuxPlayer)]
        public void DesktopBuildsAreAllowedUnlessTheConfigSaysOtherwise(RuntimePlatform platform)
        {
            var desktop = new FakeDeviceInfo { Platform = platform };

            Assert.That(AdminAccessPolicy.Resolve(Config(desktop: true), desktop), Is.EqualTo(AdminAccess.Desktop));
            Assert.That(AdminAccessPolicy.Resolve(Config(desktop: false), desktop), Is.EqualTo(AdminAccess.Denied));
        }

        [Test]
        public void APhoneInTheListIsAllowed()
        {
            CheatingConfig config = Config(devices: new CheatingConfig.AllowedDevice("Max — Pixel", "a1b2c3"));

            Assert.That(AdminAccessPolicy.Resolve(config, FakeDeviceInfo.Phone("a1b2c3")), Is.EqualTo(AdminAccess.AllowedDevice));
            Assert.That(AdminAccessPolicy.Resolve(config, FakeDeviceInfo.Phone("a1b2c4")), Is.EqualTo(AdminAccess.Denied));
        }

        [Test]
        public void APastedIdMatchesWhateverItsCaseAndTheSpacesAroundIt()
        {
            CheatingConfig config = Config(devices: new CheatingConfig.AllowedDevice("iPhone", "  6F1A-B2C3 \n"));

            Assert.That(AdminAccessPolicy.Resolve(config, FakeDeviceInfo.Phone("6f1a-b2c3")), Is.EqualTo(AdminAccess.AllowedDevice));
        }

        [Test]
        public void AnEmptyIdMatchesNothingNotEvenAnEmptyEntry()
        {
            CheatingConfig config = Config(devices: new CheatingConfig.AllowedDevice("Unfinished", ""));

            Assert.That(AdminAccessPolicy.Resolve(config, FakeDeviceInfo.Phone("")), Is.EqualTo(AdminAccess.Denied));
            Assert.That(AdminAccessPolicy.Resolve(config, FakeDeviceInfo.Phone(null)), Is.EqualTo(AdminAccess.Denied));
        }

        [Test]
        public void ADevelopmentBuildIsAllowedOnlyWhenTheConfigSaysSo()
        {
            var developmentPhone = new FakeDeviceInfo { IsDevelopmentBuild = true };

            Assert.That(AdminAccessPolicy.Resolve(Config(developmentBuild: false), developmentPhone), Is.EqualTo(AdminAccess.Denied));
            Assert.That(AdminAccessPolicy.Resolve(Config(developmentBuild: true), developmentPhone), Is.EqualTo(AdminAccess.DevelopmentBuild));
        }

        [Test]
        public void NoConfigOrNoDeviceIsDenied()
        {
            Assert.That(AdminAccessPolicy.Resolve(null, FakeDeviceInfo.Editor()), Is.EqualTo(AdminAccess.Denied));
            Assert.That(AdminAccessPolicy.Resolve(Config(), null), Is.EqualTo(AdminAccess.Denied));
        }
    }
}
