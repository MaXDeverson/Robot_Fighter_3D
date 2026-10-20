using System.Collections.Generic;

namespace Game.Analytics
{
    /// <summary>
    /// <see cref="IAnalyticsStorage"/> that forgets everything with the process. Used by tests, and as the fallback of
    /// a service or consent built without storage.
    /// </summary>
    public sealed class MemoryAnalyticsStorage : IAnalyticsStorage
    {
        private readonly Dictionary<string, int> _values = new Dictionary<string, int>();

        /// <inheritdoc />
        public int GetInt(string key, int fallback = 0)
        {
            return _values.TryGetValue(key, out int value) ? value : fallback;
        }

        /// <inheritdoc />
        public void SetInt(string key, int value)
        {
            _values[key] = value;
        }

        /// <inheritdoc />
        public void Delete(string key)
        {
            _values.Remove(key);
        }

        /// <inheritdoc />
        public void Save()
        {
        }
    }
}
