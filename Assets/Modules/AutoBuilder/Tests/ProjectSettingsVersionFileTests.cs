using System;
using NUnit.Framework;

namespace Game.AutoBuilder.Tests
{
    public class ProjectSettingsVersionFileTests
    {
        private const string Yaml =
            "PlayerSettings:\n" +
            "  productName: Portal Quest\n" +
            "  bundleVersion: 0.0.1\n" +
            "  preloadedAssets: []\n" +
            "  AndroidBundleVersionCode: 1\n" +
            "  AndroidMinSdkVersion: 25\n";

        [Test]
        public void Apply_ReplacesOnlyTheTwoValues()
        {
            string result = ProjectSettingsVersionFile.Apply(Yaml, "0.0.2", 2);

            Assert.AreEqual(Yaml.Replace("bundleVersion: 0.0.1", "bundleVersion: 0.0.2")
                .Replace("AndroidBundleVersionCode: 1", "AndroidBundleVersionCode: 2"), result);
        }

        [Test]
        public void Apply_KeepsWindowsLineEndings()
        {
            string crlf = Yaml.Replace("\n", "\r\n");

            string result = ProjectSettingsVersionFile.Apply(crlf, "1.0.0", 10);

            StringAssert.Contains("  bundleVersion: 1.0.0\r\n", result);
            StringAssert.Contains("  AndroidBundleVersionCode: 10\r\n", result);
            Assert.AreEqual(crlf.Replace("bundleVersion: 0.0.1", "bundleVersion: 1.0.0")
                .Replace("AndroidBundleVersionCode: 1\r", "AndroidBundleVersionCode: 10\r"), result);
        }

        [Test]
        public void Apply_IgnoresKeysThatOnlyEndTheSame()
        {
            string yaml = Yaml + "  someOtherbundleVersion: 9\n  iOSBundleVersionCode: 7\n";

            string result = ProjectSettingsVersionFile.Apply(yaml, "0.0.2", 2);

            StringAssert.Contains("someOtherbundleVersion: 9", result);
            StringAssert.Contains("iOSBundleVersionCode: 7", result);
        }

        [Test]
        public void Apply_Throws_WhenALineIsMissing()
        {
            string yaml = Yaml.Replace("  AndroidBundleVersionCode: 1\n", string.Empty);

            Assert.Throws<InvalidOperationException>(() => ProjectSettingsVersionFile.Apply(yaml, "0.0.2", 2));
        }

        [TestCase("")]
        [TestCase("1.0\n  hacked: 1")]
        [TestCase("a: b")]
        public void Apply_RejectsValuesThatWouldBreakTheYaml(string version)
        {
            Assert.Throws<ArgumentException>(() => ProjectSettingsVersionFile.Apply(Yaml, version, 2));
        }
    }
}
