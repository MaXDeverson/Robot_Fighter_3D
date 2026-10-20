using NUnit.Framework;

namespace Game.Events.Tests
{
    /// <summary>
    /// Shared setup for the core suite. The service is built with its own constructor; no container is involved,
    /// which is the point: the core has no dependency on Zenject.
    /// </summary>
    internal abstract class GameEventsTestFixture
    {
        private RecordingGameEventsLogger _logger;

        protected RecordingGameEventsLogger Logger
        {
            get { return _logger; }
        }

        [SetUp]
        public void SetUpFixture()
        {
            _logger = new RecordingGameEventsLogger();
        }

        /// <summary>Builds a service with the default settings and a logger the test can inspect.</summary>
        protected GlobalGameEvents CreateEvents()
        {
            return CreateEvents(new GameEventsSettings());
        }

        /// <summary>Builds a service with explicit settings.</summary>
        protected GlobalGameEvents CreateEvents(GameEventsSettings settings)
        {
            return new GlobalGameEvents(settings, new EventPathRegistry(), _logger);
        }

        /// <summary>Builds settings that differ from the defaults only in the nesting limit.</summary>
        protected GameEventsSettings SettingsWithDepth(int maxDispatchDepth)
        {
            return new GameEventsSettings(
                maxDispatchDepth,
                GameEventsSettings.DefaultHistoryCapacity,
                GameEventsSettings.DefaultMaxEventsPerFlush,
                false,
                false,
                true);
        }
    }
}
