using NUnit.Framework;
using UnityEngine;

namespace Game.GamepadSupport.Tests
{
    [TestFixture]
    internal sealed class StickPullTests
    {
        private const float Frame = 1f / 60f;

        private static StickPull Pull() => new StickPull(startDeflection: 0.25f, armDeflection: 0.5f, releaseGrace: 0.1f);

        [Test]
        public void AStickAtRestAimsNothing()
        {
            StickPull pull = Pull();

            Assert.That(pull.Update(new Vector2(0.1f, 0f), 0f), Is.EqualTo(StickPullPhase.Idle));
            Assert.That(pull.IsPulling, Is.False);
        }

        [Test]
        public void PushingTheStickStartsAnAimThatFollowsIt()
        {
            StickPull pull = Pull();

            Assert.That(pull.Update(new Vector2(0.3f, 0f), 0f), Is.EqualTo(StickPullPhase.Began));
            Assert.That(pull.Update(new Vector2(0f, 0.9f), Frame), Is.EqualTo(StickPullPhase.Held));
            Assert.That(pull.Pull, Is.EqualTo(new Vector2(0f, 0.9f)));
        }

        [Test]
        public void LettingGoFiresAlongThePushTheStickWasLetGoFrom()
        {
            StickPull pull = Pull();
            pull.Update(new Vector2(0.9f, 0.1f), 0f);
            pull.Update(new Vector2(0.95f, 0.1f), Frame);

            // Springing back: a shorter push the same way, then a little past the middle.
            Assert.That(pull.Update(new Vector2(0.4f, 0.05f), 2 * Frame), Is.EqualTo(StickPullPhase.Held));
            Assert.That(pull.Pull, Is.EqualTo(new Vector2(0.95f, 0.1f)), "the spring-back does not shrink the aim");
            Assert.That(pull.Update(new Vector2(-0.05f, 0f), 3 * Frame), Is.EqualTo(StickPullPhase.Released));
            Assert.That(pull.Pull, Is.EqualTo(new Vector2(0.95f, 0.1f)));
        }

        [Test]
        public void AStickNeverPushedFarEnoughFiresNothing()
        {
            StickPull pull = Pull();
            pull.Update(new Vector2(0.4f, 0f), 0f);
            pull.Update(new Vector2(0.45f, 0f), Frame);

            Assert.That(pull.Update(Vector2.zero, 2 * Frame), Is.EqualTo(StickPullPhase.Cancelled));
        }

        [Test]
        public void AStickEasedBackSlowlyFiresNothing()
        {
            StickPull pull = Pull();
            pull.Update(new Vector2(0.9f, 0f), 0f);
            pull.Update(new Vector2(0.4f, 0f), 0.05f);
            pull.Update(new Vector2(0.3f, 0f), 0.3f);

            Assert.That(pull.Update(Vector2.zero, 0.35f), Is.EqualTo(StickPullPhase.Cancelled));
        }

        [Test]
        public void ACancelledAimNeedsAFreshPush()
        {
            StickPull pull = Pull();
            pull.Update(new Vector2(0.9f, 0f), 0f);

            pull.Cancel();

            Assert.That(pull.IsPulling, Is.False);
            Assert.That(pull.Update(Vector2.zero, Frame), Is.EqualTo(StickPullPhase.Idle), "letting go after a cancel fires nothing");
            Assert.That(pull.Update(new Vector2(0.9f, 0f), 2 * Frame), Is.EqualTo(StickPullPhase.Began));
        }
    }
}
