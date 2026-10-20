using System;

namespace Game.Ads
{
    /// <summary>
    /// Whether the player's privacy choices let the game request ads — the privacy notice accepted and, where the law
    /// requires it, the consent form answered.
    /// </summary>
    /// <remarks>
    /// Implemented outside the module, over the game's privacy service, and bound in the <c>ProjectContext</c>. The ad
    /// network is not started, and no ad is shown, until <see cref="CanRequestAds"/> is true: an ad SDK collects device
    /// identifiers from the moment it starts. Without a binding the network starts at once, as before.
    /// </remarks>
    public interface IAdsConsentSource
    {
        /// <summary>Whether ads may be requested now.</summary>
        bool CanRequestAds { get; }

        /// <summary>Raised on the main thread whenever <see cref="CanRequestAds"/> may have changed.</summary>
        event Action Changed;
    }
}
