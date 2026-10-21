using UnityEngine;

namespace Game.GamepadSupport
{
    /// <summary>What a frame of <see cref="StickPull.Update"/> came to.</summary>
    public enum StickPullPhase
    {
        /// <summary>The stick is at rest and nothing is being aimed.</summary>
        Idle,

        /// <summary>The stick has just left the rest zone: an aim starts this frame.</summary>
        Began,

        /// <summary>The aim goes on.</summary>
        Held,

        /// <summary>The stick has been let go of while it was pushed far enough: fire along <see cref="StickPull.Pull"/>.</summary>
        Released,

        /// <summary>The stick came back without having been pushed far enough, or too slowly: nothing is fired.</summary>
        Cancelled,
    }

    /// <summary>
    /// Reads a stick as a pull that is aimed while the stick is pushed and fired by letting go of it.
    /// <para>
    /// Letting go is the hard part. A released stick springs back to the middle on its own, and on its way it is read
    /// at whatever it passes through — a shorter push in the same direction, or, as it overshoots the middle, a little
    /// push the other way. So the direction fired is not the one read on the frame the stick comes to rest: it is the
    /// last one read while the stick was pushed at least <see cref="ArmDeflection"/> far, as long as that was no more
    /// than the release grace ago. A stick eased back slowly outlives the grace, and nothing is fired — the same as a
    /// pull shortened below the threshold before the finger is lifted.
    /// </para>
    /// Time is passed in rather than read, so the same gesture is measured in real seconds while the game is slowed down.
    /// </summary>
    public sealed class StickPull
    {
        private readonly float _startDeflection;
        private readonly float _armDeflection;
        private readonly float _releaseGrace;

        private bool _isPulling;
        private Vector2 _armedPull;
        private float _armedTime = float.NegativeInfinity;

        /// <param name="startDeflection">How far the stick has to be pushed for an aim to start, 0 to 1.</param>
        /// <param name="armDeflection">How far it has to be pushed for letting go to fire, 0 to 1.</param>
        /// <param name="releaseGrace">How long, in seconds, a push that was far enough still counts after the stick has dropped below it.</param>
        public StickPull(float startDeflection, float armDeflection, float releaseGrace)
        {
            _startDeflection = Mathf.Clamp01(startDeflection);
            _armDeflection = Mathf.Clamp(armDeflection, _startDeflection, 1f);
            _releaseGrace = Mathf.Max(0f, releaseGrace);
        }

        public float StartDeflection => _startDeflection;

        public float ArmDeflection => _armDeflection;

        public bool IsPulling => _isPulling;

        /// <summary>
        /// The pull, in stick units: the stick as it is now, or, while it springs back, the push it was let go of from.
        /// On <see cref="StickPullPhase.Released"/> this is the direction to fire along.
        /// </summary>
        public Vector2 Pull { get; private set; }

        public StickPullPhase Update(Vector2 stick, float time)
        {
            float deflection = stick.magnitude;

            if (!_isPulling)
            {
                if (deflection < _startDeflection)
                {
                    Pull = Vector2.zero;
                    return StickPullPhase.Idle;
                }

                _isPulling = true;
                _armedTime = float.NegativeInfinity;
                Follow(stick, deflection, time);
                return StickPullPhase.Began;
            }

            if (deflection >= _startDeflection)
            {
                Follow(stick, deflection, time);
                return StickPullPhase.Held;
            }

            _isPulling = false;

            if (IsArmed(time))
            {
                Pull = _armedPull;
                return StickPullPhase.Released;
            }

            Pull = Vector2.zero;
            return StickPullPhase.Cancelled;
        }

        /// <summary>Drops the aim without firing, as if the stick had never been pushed. The next push starts a new one.</summary>
        public void Cancel()
        {
            _isPulling = false;
            _armedTime = float.NegativeInfinity;
            Pull = Vector2.zero;
        }

        private void Follow(Vector2 stick, float deflection, float time)
        {
            if (deflection >= _armDeflection)
            {
                _armedPull = stick;
                _armedTime = time;
                Pull = stick;
                return;
            }

            // Below the threshold right after being above it is most likely the stick springing back: it keeps showing
            // the push it is about to fire, so the aim does not shrink for a frame before the shot.
            Pull = IsArmed(time) ? _armedPull : stick;
        }

        private bool IsArmed(float time) => time - _armedTime <= _releaseGrace;
    }
}
