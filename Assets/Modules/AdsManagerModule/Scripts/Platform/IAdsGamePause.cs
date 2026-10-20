namespace Game.Ads
{
    /// <summary>Holds the game still while a full screen ad is up.</summary>
    internal interface IAdsGamePause
    {
        /// <summary>Stops game time and mutes the sound, remembering how both were.</summary>
        void Pause();

        /// <summary>Puts back what <see cref="Pause"/> found. Does nothing when the game is not paused.</summary>
        void Resume();
    }
}
