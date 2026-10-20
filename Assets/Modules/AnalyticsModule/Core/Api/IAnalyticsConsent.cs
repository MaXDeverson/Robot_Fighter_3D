using System;

namespace Game.Analytics
{
    /// <summary>What the player has said about analytics.</summary>
    public enum AnalyticsConsentState
    {
        /// <summary>Not asked yet, or the answer was reset.</summary>
        Unknown = 0,

        /// <summary>The player allowed collection.</summary>
        Granted = 1,

        /// <summary>The player refused it.</summary>
        Denied = 2,
    }

    /// <summary>
    /// The player's consent to analytics (GDPR, ATT), remembered across launches.
    /// </summary>
    /// <remarks>
    /// With <see cref="IsRequired"/> off — the default — nothing waits: events flow until the player opts out with
    /// <see cref="Deny"/>. With it on, events wait in a bounded queue until <see cref="Grant"/>, and
    /// <see cref="Deny"/> discards them. The consent dialog is the game's: it calls these methods, nothing here shows
    /// any UI.
    /// </remarks>
    public interface IAnalyticsConsent
    {
        /// <summary>Whether events wait for an explicit <see cref="Grant"/>.</summary>
        bool IsRequired { get; }

        /// <summary>The current answer.</summary>
        AnalyticsConsentState State { get; }

        /// <summary>Whether events may go out right now: consent granted, or not required and not refused.</summary>
        bool CollectionAllowed { get; }

        /// <summary>Raised after <see cref="State"/> changes, with the new state.</summary>
        event Action<AnalyticsConsentState> Changed;

        /// <summary>The player allows collection.</summary>
        void Grant();

        /// <summary>The player refuses collection.</summary>
        void Deny();

        /// <summary>Forgets the answer, as if the player had never been asked.</summary>
        void Reset();
    }
}
