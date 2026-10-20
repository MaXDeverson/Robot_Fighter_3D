using UnityEngine;

namespace Game.Ads
{
    /// <summary>
    /// Default <see cref="IAdsStorage"/>: PlayerPrefs, every key prefixed so it cannot collide with the game's own.
    /// </summary>
    /// <remarks>
    /// Kept apart from the game's save files on purpose: resetting the progress from the settings must not take away an
    /// ad block the player paid for.
    /// </remarks>
    internal sealed class PlayerPrefsAdsStorage : IAdsStorage
    {
        private const string Prefix = "Ads.";

        public int GetInt(string key, int fallback = 0)
        {
            return PlayerPrefs.GetInt(Prefix + key, fallback);
        }

        public void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(Prefix + key, value);
        }

        public string GetString(string key, string fallback = null)
        {
            return PlayerPrefs.HasKey(Prefix + key) ? PlayerPrefs.GetString(Prefix + key) : fallback;
        }

        public void SetString(string key, string value)
        {
            PlayerPrefs.SetString(Prefix + key, value);
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
