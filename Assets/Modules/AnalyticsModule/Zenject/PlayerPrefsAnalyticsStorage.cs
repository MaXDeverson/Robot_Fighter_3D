using UnityEngine;

namespace Game.Analytics.DI
{
    /// <summary>
    /// Default <see cref="IAnalyticsStorage"/>: PlayerPrefs, every key prefixed so it cannot collide with the game's own.
    /// </summary>
    internal sealed class PlayerPrefsAnalyticsStorage : IAnalyticsStorage
    {
        private const string Prefix = "Analytics.";

        public int GetInt(string key, int fallback = 0)
        {
            return PlayerPrefs.GetInt(Prefix + key, fallback);
        }

        public void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(Prefix + key, value);
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(Prefix + key);
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }
    }
}
