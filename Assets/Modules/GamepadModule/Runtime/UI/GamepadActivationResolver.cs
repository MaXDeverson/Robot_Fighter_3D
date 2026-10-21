using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Game.GamepadSupport
{
    /// <summary>
    /// Switches objects on or off with the pad: each entry says whether its object is shown while a pad is connected,
    /// and without a pad it gets the opposite. For the parts of a screen that only make sense for touch — or only for
    /// a pad — and that nobody else switches on and off.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GamepadActivationResolver : MonoBehaviour
    {
        [Tooltip("The objects to switch. Not this object or a parent of it — it keeps listening while they are off.")]
        [SerializeField] private List<GamepadActiveState> _activeInGamepadMode = new List<GamepadActiveState>();

        private IGamepadService _gamepad;
        private bool _isSubscribed;

        [Inject]
        public void Construct(IGamepadService gamepad)
        {
            _gamepad = gamepad;
            if (isActiveAndEnabled)
                Subscribe();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            if (_isSubscribed)
                _gamepad.Changed -= Refresh;
            _isSubscribed = false;
        }

        private void Subscribe()
        {
            // Not injected (yet): leave the objects as authored, the touch layout.
            if (_gamepad == null)
                return;

            if (!_isSubscribed)
                _gamepad.Changed += Refresh;
            _isSubscribed = true;
            Refresh();
        }

        private void Refresh()
        {
            bool isConnected = _gamepad.IsConnected;
            for (int i = 0; i < _activeInGamepadMode.Count; i++)
                _activeInGamepadMode[i]?.Apply(isConnected);
        }
    }

    /// <summary>One object of a <see cref="GamepadActivationResolver"/> and whether it is shown while a pad is connected.</summary>
    [Serializable]
    public sealed class GamepadActiveState
    {
        [SerializeField] private GameObject _target;
        [Tooltip("On: shown only while a pad is connected. Off: hidden while a pad is connected.")]
        [SerializeField] private bool _isActive;

        internal void Apply(bool isGamepadConnected)
        {
            if (_target == null)
                return;

            bool active = isGamepadConnected == _isActive;
            if (_target.activeSelf != active)
                _target.SetActive(active);
        }
    }
}
