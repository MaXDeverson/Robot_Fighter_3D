using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Ads.Tests
{
    /// <summary>
    /// A manager over test doubles: a network the test closes by hand, clocks it moves on, storage in memory. Every
    /// <see cref="Create"/> shares the storage, so a second one stands for the game started again.
    /// </summary>
    internal abstract class AdsServiceFixture
    {
        protected FakeAdsProvider Provider { get; private set; }

        protected FakeAdsClock Clock { get; private set; }

        protected MemoryAdsStorage Storage { get; private set; }

        protected FakeLevelSource Levels { get; private set; }

        protected FakeGamePause Pause { get; private set; }

        protected List<AdResult> Results { get; private set; }

        protected AdResult Last
        {
            get { return Results[Results.Count - 1]; }
        }

        [SetUp]
        public void CreateDoubles()
        {
            Provider = new FakeAdsProvider();
            Clock = new FakeAdsClock();
            Storage = new MemoryAdsStorage();
            Levels = new FakeLevelSource();
            Pause = new FakeGamePause();
            Results = new List<AdResult>();
        }

        /// <summary>A manager on the default rules, quiet in the console, already initialized.</summary>
        /// <param name="consent">The player's privacy choices. <c>null</c>: none bound, ads always allowed.</param>
        protected AdsManagerService Create(AdsSettings settings = null, bool withLevels = true,
            IAdsConsentSource consent = null)
        {
            AdsManagerService service = new AdsManagerService(
                Provider,
                settings ?? new AdsSettings(logRequests: false),
                Storage,
                Clock,
                Pause,
                withLevels ? Levels : null,
                consent);
            service.Initialize();
            return service;
        }

        protected void Record(AdResult result)
        {
            Results.Add(result);
        }

        /// <summary>Lets time pass, then gives the manager the frame its once-a-second refresh runs on.</summary>
        protected void TickAfter(AdsManagerService service, double seconds)
        {
            Clock.AdvanceSeconds(seconds);
            service.Tick();
        }
    }
}
