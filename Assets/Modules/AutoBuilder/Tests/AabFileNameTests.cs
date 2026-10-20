using System;
using NUnit.Framework;

namespace Game.AutoBuilder.Tests
{
    public class AabFileNameTests
    {
        private static readonly DateTime Time = new DateTime(2026, 10, 3, 15, 30, 45);

        [Test]
        public void ReleaseName_CarriesVersionCodeAndTime()
        {
            Assert.AreEqual("PortalQuest_v0.0.2_c3_2026-10-03_1530.aab",
                AutoBuilderPaths.AabFileName("Portal Quest", "0.0.2", 3, AabBuildKind.Release, Time));
        }

        [Test]
        public void DevelopmentName_IsMarked()
        {
            Assert.AreEqual("PortalQuest_v1.0.0_c12_dev_2026-10-03_1530.aab",
                AutoBuilderPaths.AabFileName("Portal Quest", "1.0.0", 12, AabBuildKind.Development, Time));
        }

        [Test]
        public void CharactersWindowsCannotTake_AreDropped()
        {
            Assert.AreEqual("PortalQuest_v1.0_c1_2026-10-03_1530.aab",
                AutoBuilderPaths.AabFileName("Portal: Quest?", "1/.0", 1, AabBuildKind.Release, Time));
        }

        [Test]
        public void EmptyProductName_FallsBackToGame()
        {
            StringAssert.StartsWith("Game_v", AutoBuilderPaths.AabFileName("", "1.0.0", 1, AabBuildKind.Release, Time));
        }
    }
}
