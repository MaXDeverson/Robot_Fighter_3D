using System;
using System.Linq;
using NUnit.Framework;

namespace Game.Ads.Tests
{
    /// <summary>
    /// The rule of the module, checked: the game reaches it through <see cref="IAdsManagerService"/> alone.
    /// </summary>
    [TestFixture]
    internal sealed class AdsModuleBoundaryTests
    {
        /// <summary>
        /// Everything a script outside the module can name is the manager's interface, the types it speaks in and the
        /// ports the game fills in. A provider, the storage or the installer turning public fails this — on purpose.
        /// </summary>
        [Test]
        public void OnlyTheManagerAndTheTypesItSpeaksInArePublic()
        {
            string[] exported = typeof(IAdsManagerService).Assembly.GetExportedTypes()
                .Select(type => type.FullName)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.That(exported, Is.EqualTo(new[]
            {
                "Game.Ads.AdResult",
                "Game.Ads.AdStatus",
                "Game.Ads.AdType",
                "Game.Ads.IAdsConsentSource",
                "Game.Ads.IAdsLevelSource",
                "Game.Ads.IAdsManagerService",
            }));
        }
    }
}
