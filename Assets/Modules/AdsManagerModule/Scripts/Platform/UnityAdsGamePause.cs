using UnityEngine;

namespace Game.Ads
{
    /// <summary>
    /// Pauses through <c>Time.timeScale</c> and mutes through <c>AudioListener.volume</c>, then puts back the values it
    /// found — a pause menu or a slow motion that was on before the ad is still on after it.
    /// </summary>
    /// <remarks>
    /// The sound is muted rather than paused with <c>AudioListener.pause</c>: menu music and UI sounds are set to ignore
    /// the listener pause, and they must not play over the ad either.
    /// </remarks>
    internal sealed class UnityAdsGamePause : IAdsGamePause
    {
        private bool _paused;
        private float _timeScale;
        private float _volume;

        public void Pause()
        {
            if (_paused)
            {
                return;
            }

            _paused = true;
            _timeScale = Time.timeScale;
            _volume = AudioListener.volume;
            Time.timeScale = 0f;
            AudioListener.volume = 0f;
        }

        public void Resume()
        {
            if (!_paused)
            {
                return;
            }

            _paused = false;
            Time.timeScale = _timeScale;
            AudioListener.volume = _volume;
        }
    }
}
