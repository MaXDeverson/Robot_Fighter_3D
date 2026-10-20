namespace Game.Ads
{
    /// <summary>Where the module keeps what has to outlive the session: the ad block.</summary>
    internal interface IAdsStorage
    {
        /// <summary>The stored number, or <paramref name="fallback"/> when there is none.</summary>
        int GetInt(string key, int fallback = 0);

        /// <summary>Stores a number.</summary>
        void SetInt(string key, int value);

        /// <summary>The stored text, or <paramref name="fallback"/> when there is none.</summary>
        string GetString(string key, string fallback = null);

        /// <summary>Stores a text.</summary>
        void SetString(string key, string value);

        /// <summary>Forgets a key.</summary>
        void Delete(string key);

        /// <summary>Writes what is pending to disk.</summary>
        void Save();
    }
}
