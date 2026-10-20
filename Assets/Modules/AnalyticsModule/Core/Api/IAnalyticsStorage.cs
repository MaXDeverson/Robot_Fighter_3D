namespace Game.Analytics
{
    /// <summary>
    /// Small persistent store for what analytics keeps across launches: the consent answer and the counters trackers
    /// maintain, such as deaths on a level since it was last completed.
    /// </summary>
    /// <remarks>
    /// The Zenject integration binds a PlayerPrefs backed store unless one is bound before it, so the game can route
    /// this through its own save system without touching the module.
    /// </remarks>
    public interface IAnalyticsStorage
    {
        /// <summary>Reads a number, or <paramref name="fallback"/> when the key was never written.</summary>
        int GetInt(string key, int fallback = 0);

        /// <summary>Writes a number.</summary>
        void SetInt(string key, int value);

        /// <summary>Forgets a key.</summary>
        void Delete(string key);

        /// <summary>Makes pending writes durable. Called on pause and quit, and right after a consent answer.</summary>
        void Save();
    }
}
