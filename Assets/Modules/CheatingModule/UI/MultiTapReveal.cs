using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Cheating.UI
{
    /// <summary>
    /// Opens a hidden object after several quick taps on this element — the way to the <see cref="DeviceIdView"/> on a
    /// phone that is not allowed yet. The element needs a graphic that takes raycasts (a TMP text with Raycast Target on).
    /// </summary>
    public sealed class MultiTapReveal : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("Activated when the taps are done.")]
        [SerializeField] private GameObject _target;

        [SerializeField, Min(1)] private int _requiredTaps = 5;

        [Tooltip("The longest pause between two taps, in seconds; a longer one starts the count over.")]
        [SerializeField, Min(0.05f)] private float _maxInterval = 0.6f;

        private int _taps;
        private float _lastTapTime = float.NegativeInfinity;

        public void OnPointerClick(PointerEventData eventData)
        {
            RegisterTap(Time.unscaledTime);
        }

        /// <summary>Counts a tap made at <paramref name="time"/>; true when it completes the series.</summary>
        internal bool RegisterTap(float time)
        {
            _taps = time - _lastTapTime <= _maxInterval ? _taps + 1 : 1;
            _lastTapTime = time;

            if (_taps < _requiredTaps)
                return false;

            _taps = 0;
            _lastTapTime = float.NegativeInfinity;

            if (_target != null)
                _target.SetActive(true);

            return true;
        }
    }
}
