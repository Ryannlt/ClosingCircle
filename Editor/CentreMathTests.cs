using ClosingCircle.Domain;
using NUnit.Framework;
using UnityEngine;

namespace ClosingCircle.Tests
{
    [TestFixture]
    public class CentreMathTests
    {
        private static readonly Vector2 Previous = new Vector2(10f, -20f);
        private const float PreviousRadius = 100f;
        private const float StageRadius = 40f;
        private const float Allowed = PreviousRadius - StageRadius;

        [Test]
        public void AllowedRadius_IsTheRoomTheSmallerCircleHasToMoveIn()
        {
            Assert.AreEqual(60f, CentreMath.AllowedRadius(100f, 40f), 0.001f);

            // A stage no smaller than the one before it has nowhere to go.
            Assert.AreEqual(0f, CentreMath.AllowedRadius(40f, 40f), 0.001f);
            Assert.AreEqual(0f, CentreMath.AllowedRadius(40f, 90f), 0.001f);
        }

        [Test]
        public void Direction_IsDegreesClockwiseFromNorth()
        {
            Vector2 north = CentreMath.Direction(0f);
            Assert.AreEqual(0f, north.x, 0.001f);
            Assert.AreEqual(1f, north.y, 0.001f);

            Vector2 east = CentreMath.Direction(90f);
            Assert.AreEqual(1f, east.x, 0.001f);
            Assert.AreEqual(0f, east.y, 0.001f);
        }

        [Test]
        public void TryChord_ThroughTheCentreSpansTheWholeDiameter()
        {
            Assert.IsTrue(CentreMath.TryChord(Previous, Allowed, Previous, CentreMath.Direction(90f),
                                              out float tMin, out float tMax));

            Assert.AreEqual(Allowed * 2f, tMax - tMin, 0.001f);
            Assert.AreEqual(0f, (tMin + tMax) * 0.5f, 0.001f);
        }

        [Test]
        public void TryChord_OffsetFromTheCentreIsShorter()
        {
            // A line running east, half the allowed radius north of the centre.
            Vector2 linePoint = Previous + new Vector2(0f, Allowed * 0.5f);

            Assert.IsTrue(CentreMath.TryChord(Previous, Allowed, linePoint, CentreMath.Direction(90f),
                                              out float tMin, out float tMax));

            float expected = 2f * Mathf.Sqrt(Allowed * Allowed - (Allowed * 0.5f) * (Allowed * 0.5f));
            Assert.AreEqual(expected, tMax - tMin, 0.01f);
            Assert.Less(tMax - tMin, Allowed * 2f);
        }

        [Test]
        public void TryChord_MissingTheDiscEntirelyFails()
        {
            Vector2 linePoint = Previous + new Vector2(0f, Allowed * 2f);

            Assert.IsFalse(CentreMath.TryChord(Previous, Allowed, linePoint, CentreMath.Direction(90f),
                                               out _, out _));
        }

        [Test]
        public void PickOnChord_WithNoSpreadAlwaysReturnsTheMidpoint()
        {
            for (float unit = 0f; unit <= 1f; unit += 0.1f)
                Assert.AreEqual(15f, CentreMath.PickOnChord(-5f, 35f, 0f, unit), 0.001f, $"unit {unit}");
        }

        [Test]
        public void PickOnChord_WithFullSpreadReachesBothEnds()
        {
            Assert.AreEqual(-5f, CentreMath.PickOnChord(-5f, 35f, 1f, 0f), 0.001f);
            Assert.AreEqual(35f, CentreMath.PickOnChord(-5f, 35f, 1f, 1f), 0.001f);
        }

        [Test]
        public void PickOnChord_NeverLeavesTheChordAtAnySpread()
        {
            for (float spread = 0f; spread <= 1f; spread += 0.1f)
            {
                for (float unit = 0f; unit <= 1f; unit += 0.1f)
                {
                    float t = CentreMath.PickOnChord(-5f, 35f, spread, unit);
                    Assert.IsTrue(t >= -5.001f && t <= 35.001f, $"spread {spread} unit {unit} gave {t}");
                }
            }
        }

        [Test]
        public void Nest_LeavesAPointThatAlreadyFitsAlone()
        {
            Vector2 inside = Previous + new Vector2(Allowed * 0.5f, 0f);

            Assert.AreEqual(inside, CentreMath.Nest(Previous, PreviousRadius, StageRadius, inside));
        }

        [Test]
        public void Nest_PullsADistantPointBackToTheEdge()
        {
            Vector2 far = Previous + new Vector2(500f, 500f);
            Vector2 nested = CentreMath.Nest(Previous, PreviousRadius, StageRadius, far);

            Assert.AreEqual(Allowed, Vector2.Distance(Previous, nested), 0.001f);
        }

        [Test]
        public void Nest_HoldsThePreviousCentreWhenThereIsNoRoom()
        {
            // A stage the same size as the one before it can only sit exactly where it did.
            Vector2 nested = CentreMath.Nest(Previous, 40f, 40f, Previous + new Vector2(30f, 10f));

            Assert.AreEqual(Previous.x, nested.x, 0.001f);
            Assert.AreEqual(Previous.y, nested.y, 0.001f);
        }

        [Test]
        public void Nest_KeepsTheNewCircleInsideThePreviousOneForEveryCase()
        {
            // The invariant the whole design rests on: whatever a selector asks for, players inside the
            // current zone can always reach the next one.
            float[] previousRadii = { 200f, 100f, 40f };
            float[] radii = { 150f, 40f, 40f, 5f };
            float[] bearings = { 0f, 37f, 123f, 250f, 310f };

            foreach (float previousRadius in previousRadii)
            {
                foreach (float radius in radii)
                {
                    foreach (float bearing in bearings)
                    {
                        Vector2 desired = Previous + CentreMath.Direction(bearing) * 1000f;
                        Vector2 nested = CentreMath.Nest(Previous, previousRadius, radius, desired);
                        float moved = Vector2.Distance(Previous, nested);

                        Assert.IsTrue(moved <= CentreMath.AllowedRadius(previousRadius, radius) + 0.001f,
                            $"prev {previousRadius} radius {radius} bearing {bearing} moved {moved}");

                        // A stage larger than the one before it cannot be contained by it, and holding the
                        // centre still is the best available answer, so only check containment when it can
                        // actually hold.
                        if (radius <= previousRadius)
                            Assert.IsTrue(moved + radius <= previousRadius + 0.001f,
                                $"prev {previousRadius} radius {radius} bearing {bearing} reached {moved + radius}");
                    }
                }
            }
        }

        [Test]
        public void PointInDisc_StaysInside()
        {
            for (float angle = 0f; angle <= 1f; angle += 0.1f)
            {
                for (float radius = 0f; radius <= 1f; radius += 0.1f)
                {
                    Vector2 point = CentreMath.PointInDisc(Previous, Allowed, angle, radius);
                    Assert.IsTrue(Vector2.Distance(Previous, point) <= Allowed + 0.001f);
                }
            }
        }

        [Test]
        public void ClosestOnLine_DropsAPerpendicular()
        {
            // A line running east through the origin: the nearest point to (7, 25) is (7, 0).
            Vector2 closest = CentreMath.ClosestOnLine(Vector2.zero, CentreMath.Direction(90f),
                                                       new Vector2(7f, 25f));

            Assert.AreEqual(7f, closest.x, 0.001f);
            Assert.AreEqual(0f, closest.y, 0.001f);
        }

        [Test]
        public void ChordMidpointIsTheClosestPointOnTheLine()
        {
            // This is what gives spread 0 its meaning, so it is worth pinning rather than assuming.
            Vector2 linePoint = Previous + new Vector2(0f, Allowed * 0.5f);
            Vector2 direction = CentreMath.Direction(90f);

            Assert.IsTrue(CentreMath.TryChord(Previous, Allowed, linePoint, direction,
                                              out float tMin, out float tMax));

            Vector2 midpoint = CentreMath.PointOnLine(linePoint, direction, (tMin + tMax) * 0.5f);
            Vector2 closest = CentreMath.ClosestOnLine(linePoint, direction, Previous);

            Assert.AreEqual(closest.x, midpoint.x, 0.001f);
            Assert.AreEqual(closest.y, midpoint.y, 0.001f);
        }
        // A line that just touches a disc does meet it. Chains of clamps land on tangency often enough that
        // rounding must not be what decides whether a stage can be placed.
        [Test]
        public void TryChord_AcceptsATangentLine()
        {
            var centre = new Vector2(50f, -20f);

            Assert.IsTrue(CentreMath.TryChord(centre, 20f, Vector2.zero, Vector2.right,
                                              out float tMin, out float tMax));
            Assert.AreEqual(50f, tMin, 0.2f);
            Assert.AreEqual(50f, tMax, 0.2f);
        }

        [Test]
        public void TryChord_StillRejectsALineThatClearlyMisses()
        {
            Assert.IsFalse(CentreMath.TryChord(new Vector2(50f, -40f), 20f, Vector2.zero, Vector2.right,
                                               out _, out _));
        }

        // The hard clamp is the invariant of last resort: whatever it returns is inside the allowed disc and
        // within budget of the line.
        [Test]
        public void ClampToLine_LandsInsideBothTheDiscAndTheBand()
        {
            var previous = new Vector2(10f, 5f);
            const float previousRadius = 100f;
            const float radius = 40f;
            const float budget = 12f;

            float allowed = CentreMath.AllowedRadius(previousRadius, radius);

            foreach (float bearing in new[] { 0f, 47f, 131f, 218f, 305f })
            {
                Vector2 desired = previous + CentreMath.Direction(bearing) * allowed;
                Vector2 clamped = CentreMath.ClampToLine(desired, previous, previousRadius, radius,
                                                         Vector2.zero, Vector2.right, budget);

                Assert.LessOrEqual(Vector2.Distance(clamped, previous), allowed + 0.01f, $"disc at {bearing}");
                Assert.LessOrEqual(CentreMath.DistanceToLine(Vector2.zero, Vector2.right, clamped),
                                   budget + 0.01f, $"band at {bearing}");
            }
        }

    }
}
