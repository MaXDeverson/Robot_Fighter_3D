using NUnit.Framework;

namespace Game.AutoBuilder.Tests
{
    public class AppVersionTests
    {
        [TestCase("0.0.1", 0, 0, 1)]
        [TestCase("1.2.3", 1, 2, 3)]
        [TestCase(" 10.20.30 ", 10, 20, 30)]
        [TestCase("2", 2, 0, 0)]
        [TestCase("2.5", 2, 5, 0)]
        public void TryParse_ReadsUpToThreeParts(string text, int major, int minor, int patch)
        {
            Assert.IsTrue(AppVersion.TryParse(text, out AppVersion version));
            Assert.AreEqual(new AppVersion(major, minor, patch), version);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("1.2.3.4")]
        [TestCase("1..2")]
        [TestCase("1.2.x")]
        [TestCase("-1.0.0")]
        [TestCase("v1.0.0")]
        [TestCase("1.0.0-beta")]
        public void TryParse_RejectsAnythingElse(string text)
        {
            Assert.IsFalse(AppVersion.TryParse(text, out _));
        }

        // The part goes in by name: VersionPart is internal, and a public test method cannot take it.
        [TestCase("Patch", "1.2.4")]
        [TestCase("Minor", "1.3.0")]
        [TestCase("Major", "2.0.0")]
        public void Bump_RaisesThePartAndResetsTheSmallerOnes(string part, string expected)
        {
            var versionPart = (VersionPart)System.Enum.Parse(typeof(VersionPart), part);

            Assert.AreEqual(expected, new AppVersion(1, 2, 3).Bump(versionPart).ToString());
        }

        [Test]
        public void Bump_WritesAllThreeParts_WhenTheVersionHadFewer()
        {
            AppVersion.TryParse("1.4", out AppVersion version);

            Assert.AreEqual("1.4.1", version.Bump(VersionPart.Patch).ToString());
        }
    }
}
