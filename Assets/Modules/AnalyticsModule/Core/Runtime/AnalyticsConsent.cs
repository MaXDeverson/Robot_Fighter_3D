using System;

namespace Game.Analytics
{
    /// <summary>
    /// Default <see cref="IAnalyticsConsent"/>. The answer is kept in <see cref="IAnalyticsStorage"/>, so the player is
    /// asked once, not on every launch.
    /// </summary>
    public sealed class AnalyticsConsent : IAnalyticsConsent
    {
        /// <summary>Storage key of the saved answer.</summary>
        public const string StorageKey = "consent";

        private readonly IAnalyticsStorage _storage;
        private AnalyticsConsentState _state;

        /// <summary>Creates the consent and reads the saved answer.</summary>
        /// <param name="settings">Supplies <see cref="AnalyticsSettings.RequireConsent"/>. <c>null</c> means not required.</param>
        /// <param name="storage">Where the answer is kept. <c>null</c> keeps it in memory only.</param>
        public AnalyticsConsent(AnalyticsSettings settings, IAnalyticsStorage storage)
        {
            _storage = storage ?? new MemoryAnalyticsStorage();
            IsRequired = settings != null && settings.RequireConsent;
            _state = Parse(_storage.GetInt(StorageKey, (int)AnalyticsConsentState.Unknown));
        }

        /// <inheritdoc />
        public event Action<AnalyticsConsentState> Changed;

        /// <inheritdoc />
        public bool IsRequired { get; }

        /// <inheritdoc />
        public AnalyticsConsentState State
        {
            get { return _state; }
        }

        /// <inheritdoc />
        public bool CollectionAllowed
        {
            get
            {
                return _state == AnalyticsConsentState.Granted ||
                       (!IsRequired && _state == AnalyticsConsentState.Unknown);
            }
        }

        /// <inheritdoc />
        public void Grant()
        {
            SetState(AnalyticsConsentState.Granted);
        }

        /// <inheritdoc />
        public void Deny()
        {
            SetState(AnalyticsConsentState.Denied);
        }

        /// <inheritdoc />
        public void Reset()
        {
            SetState(AnalyticsConsentState.Unknown);
        }

        private void SetState(AnalyticsConsentState state)
        {
            if (state == _state)
            {
                return;
            }

            _state = state;
            if (state == AnalyticsConsentState.Unknown)
            {
                _storage.Delete(StorageKey);
            }
            else
            {
                _storage.SetInt(StorageKey, (int)state);
            }

            // An answer is exactly the kind of write that must not be lost to the app being killed a moment later.
            _storage.Save();

            Action<AnalyticsConsentState> handler = Changed;
            if (handler != null)
            {
                handler(state);
            }
        }

        private static AnalyticsConsentState Parse(int stored)
        {
            return stored == (int)AnalyticsConsentState.Granted || stored == (int)AnalyticsConsentState.Denied
                ? (AnalyticsConsentState)stored
                : AnalyticsConsentState.Unknown;
        }
    }
}
