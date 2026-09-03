using ClosingCircle.Domain;
using NUnit.Framework;
using UnityEngine;

namespace ClosingCircle.Tests
{
    [TestFixture]
    public class PushMathTests
    {
        private const float Elapsed = 1f / 30f;
        private const float Cap = 14f;

        // The zone is west of them, so the push runs that way and moving east is moving away from it.
        private static readonly Vector2 Inward = Vector2.left;
        private static readonly Vector2 Start = new Vector2(50f, 10f);

        [Test]
        public void ABodyThatLandsWhereItWasSentIsNotMoving()
        {
            Vector2 applied = Inward * 0.13f;

            Assert.AreEqual(0f, PushMath.OutwardSpeed(Start, applied, Start + applied, Inward, Elapsed, Cap),
                            0.001f);
        }

        // The bug this file exists for: measuring a rider while teleporting the horse under them left a fixed
        // offset in every sample, which read as speed and drove the push far harder than it should have.
        [Test]
        public void AConstantOffsetReadsAsSpeedAndScalesWithTheRate()
        {
            Vector2 applied = Inward * 0.13f;
            var offset = new Vector2(0.3f, 0f);

            Assert.AreEqual(9f, PushMath.OutwardSpeed(Start, applied, Start + applied + offset, Inward, Elapsed,
                                                      Cap), 0.001f);

            // Halving the interval doubles it, which is why raising the push rate made this worse rather than
            // better.
            Assert.AreEqual(Cap, PushMath.OutwardSpeed(Start, applied, Start + applied + offset, Inward,
                                                       Elapsed * 0.5f, Cap), 0.001f);
        }

        [Test]
        public void GenuineOutwardMovementIsMeasured()
        {
            Vector2 applied = Inward * 0.13f;
            Vector2 travelled = -Inward * (10f * Elapsed);

            Assert.AreEqual(10f, PushMath.OutwardSpeed(Start, applied, Start + applied + travelled, Inward,
                                                       Elapsed, Cap), 0.001f);
        }

        [Test]
        public void MovingInwardOrSidewaysNeverAddsToTheStep()
        {
            Vector2 applied = Inward * 0.13f;

            Vector2 towards = Inward * (6f * Elapsed);
            Assert.AreEqual(0f, PushMath.OutwardSpeed(Start, applied, Start + applied + towards, Inward, Elapsed,
                                                      Cap), 0.001f);

            // Along the boundary is not away from it, so a rider holding the edge gets no extra push.
            var sideways = new Vector2(0f, 8f * Elapsed);
            Assert.AreEqual(0f, PushMath.OutwardSpeed(Start, applied, Start + applied + sideways, Inward, Elapsed,
                                                      Cap), 0.001f);
        }

        [Test]
        public void NothingOutrunsTheCap()
        {
            Vector2 travelled = -Inward * (100f * Elapsed);

            Assert.AreEqual(Cap, PushMath.OutwardSpeed(Start, Vector2.zero, Start + travelled, Inward, Elapsed,
                                                       Cap), 0.001f);
        }

        [Test]
        public void AStalledClockCannotDivideByZero()
        {
            Assert.AreEqual(0f, PushMath.OutwardSpeed(Start, Vector2.zero, Start + Vector2.right, Inward, 0f,
                                                      Cap), 0.001f);
        }
    }
}
