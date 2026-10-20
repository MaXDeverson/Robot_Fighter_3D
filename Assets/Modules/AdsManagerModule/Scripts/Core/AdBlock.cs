using System;
using System.Globalization;

namespace Game.Ads
{
    /// <summary>
    /// The player's ad block: permanent, temporary until a moment in UTC, or both at once. Kept in the
    /// <see cref="IAdsStorage"/>, so it survives restarts.
    /// </summary>
    /// <remarks>
    /// A temporary block is measured on the device clock, because it has to run out while the game is closed too. A
    /// player who winds the clock back gets that time back as well; only a server clock would prevent it.
    /// </remarks>
    internal sealed class AdBlock
    {
        /// <summary>Storage key of the permanent block: 1 when there is one.</summary>
        internal const string PermanentKey = "AdBlock.Permanent";

        /// <summary>Storage key of the end of the temporary block: UTC ticks, written invariantly.</summary>
        internal const string EndsAtKey = "AdBlock.EndsAtUtcTicks";

        private readonly IAdsStorage _storage;
        private readonly IAdsClock _clock;

        private bool _permanent;
        private DateTime? _endsAtUtc;

        public AdBlock(IAdsStorage storage, IAdsClock clock)
        {
            _storage = storage;
            _clock = clock;
            Load();
        }

        /// <summary>Whether there is a permanent block.</summary>
        public bool IsPermanent
        {
            get { return _permanent; }
        }

        /// <summary>Whether ads are blocked now.</summary>
        public bool IsActive
        {
            get { return _permanent || IsTemporaryRunning; }
        }

        /// <summary>When the temporary block ends. <c>null</c> when none is running or the block is permanent.</summary>
        public DateTime? EndsAtUtc
        {
            get { return !_permanent && IsTemporaryRunning ? _endsAtUtc : null; }
        }

        private bool IsTemporaryRunning
        {
            get { return _endsAtUtc.HasValue && _clock.UtcNow < _endsAtUtc.Value; }
        }

        /// <summary>
        /// Adds time to the temporary block: from its current end while it is running, from now when it is not.
        /// </summary>
        public void Add(TimeSpan duration)
        {
            DateTime from = IsTemporaryRunning ? _endsAtUtc.Value : _clock.UtcNow;
            _endsAtUtc = duration >= DateTime.MaxValue - from ? DateTime.MaxValue : from + duration;
            Save();
        }

        /// <summary>Makes the block permanent. A temporary block under it keeps running out on its own.</summary>
        public void AddPermanent()
        {
            _permanent = true;
            Save();
        }

        /// <summary>Removes both the permanent and the temporary block.</summary>
        public void Remove()
        {
            _permanent = false;
            _endsAtUtc = null;
            Save();
        }

        /// <summary>
        /// Forgets a temporary block that has run out. True when that ended the block — false when there was none, when
        /// it is still running, or when a permanent block goes on anyway.
        /// </summary>
        public bool ForgetExpired()
        {
            if (!_endsAtUtc.HasValue || IsTemporaryRunning)
            {
                return false;
            }

            _endsAtUtc = null;
            Save();
            return !_permanent;
        }

        private void Load()
        {
            _permanent = _storage.GetInt(PermanentKey) == 1;

            long ticks;
            string stored = _storage.GetString(EndsAtKey);
            if (string.IsNullOrEmpty(stored) ||
                !long.TryParse(stored, NumberStyles.Integer, CultureInfo.InvariantCulture, out ticks) ||
                ticks <= DateTime.MinValue.Ticks ||
                ticks > DateTime.MaxValue.Ticks)
            {
                return;
            }

            _endsAtUtc = new DateTime(ticks, DateTimeKind.Utc);
            if (!IsTemporaryRunning)
            {
                // Ran out while the game was closed: nothing to announce, just nothing to keep.
                _endsAtUtc = null;
                Save();
            }
        }

        private void Save()
        {
            if (_permanent)
            {
                _storage.SetInt(PermanentKey, 1);
            }
            else
            {
                _storage.Delete(PermanentKey);
            }

            if (_endsAtUtc.HasValue)
            {
                _storage.SetString(EndsAtKey, _endsAtUtc.Value.Ticks.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                _storage.Delete(EndsAtKey);
            }

            // Written through at once: an ad block is usually something the player has just paid for.
            _storage.Save();
        }
    }
}
