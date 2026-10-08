using ClosingCircle.Domain;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace ClosingCircle.Tests
{
    [TestFixture]
    public class CenterOfMassTests
    {
        [Test]
        public void TwoSidesGiveThePointBetweenThem()
        {
            var one = new List<Vector2> { new Vector2(-100f, 0f) };
            var two = new List<Vector2> { new Vector2(100f, 0f) };

            Vector2 center = CenterOfMass.Midpoint(one, two, Vector2.zero);

            Assert.AreEqual(0f, center.x, 0.001f);
            Assert.AreEqual(0f, center.y, 0.001f);
        }

        // The property the whole mode exists for: numbers must not move the answer, or the bigger team drags
        // the zone onto its own ground and the mode is no fairer than following the crowd.
        [Test]
        public void NumbersDoNotDragTheCenter()
        {
            var even = CenterOfMass.Midpoint(
                new List<Vector2> { new Vector2(-80f, 40f) },
                new List<Vector2> { new Vector2(80f, -40f) },
                Vector2.zero);

            // Five stacked on one spot against one on the other. Same two positions, wildly uneven counts.
            var lopsided = CenterOfMass.Midpoint(
                new List<Vector2>
                {
                    new Vector2(-80f, 40f), new Vector2(-80f, 40f), new Vector2(-80f, 40f),
                    new Vector2(-80f, 40f), new Vector2(-80f, 40f)
                },
                new List<Vector2> { new Vector2(80f, -40f) },
                Vector2.zero);

            Assert.AreEqual(even.x, lopsided.x, 0.001f);
            Assert.AreEqual(even.y, lopsided.y, 0.001f);
        }

        // The mirror of the test above, and the reason Players is a separate mode rather than a tidier Team.
        // Same two clumps, same uneven counts, and here the numbers are SUPPOSED to drag the center. If this
        // ever agrees with Midpoint, one of the two modes has quietly stopped doing its job.
        [Test]
        public void CentroidIsDraggedByNumbersOnPurpose()
        {
            var positions = new List<Vector2>
            {
                new Vector2(-80f, 40f), new Vector2(-80f, 40f), new Vector2(-80f, 40f),
                new Vector2(-80f, 40f), new Vector2(-80f, 40f),
                new Vector2(80f, -40f)
            };

            Vector2 centroid = CenterOfMass.Centroid(positions);
            Vector2 midpoint = CenterOfMass.Midpoint(
                new List<Vector2>
                {
                    new Vector2(-80f, 40f), new Vector2(-80f, 40f), new Vector2(-80f, 40f),
                    new Vector2(-80f, 40f), new Vector2(-80f, 40f)
                },
                new List<Vector2> { new Vector2(80f, -40f) },
                Vector2.zero);

            // Five sixths of the way to the crowd, against the midpoint's halfway.
            Assert.AreEqual(-53.333f, centroid.x, 0.01f);
            Assert.AreEqual(26.667f, centroid.y, 0.01f);
            Assert.Greater(Vector2.Distance(centroid, midpoint), 1f);
        }

        // A side with nobody left must not count as a point at the origin, which is the middle of every stock
        // Holdfast map and so the least obvious wrong answer to spot in a live round.
        [Test]
        public void CentroidIgnoresAnEmptyList()
        {
            Assert.AreEqual(Vector2.zero, CenterOfMass.Centroid(new List<Vector2>()));
            Assert.AreEqual(Vector2.zero, CenterOfMass.Centroid(null));
        }

        [Test]
        public void ASideSpreadOutIsAveragedBeforeTheMidpoint()
        {
            var one = new List<Vector2> { new Vector2(-100f, 0f), new Vector2(-100f, 100f) };
            var two = new List<Vector2> { new Vector2(100f, 50f) };

            // One's centroid is (-100, 50), so the midpoint is (0, 50).
            Vector2 center = CenterOfMass.Midpoint(one, two, Vector2.zero);

            Assert.AreEqual(0f, center.x, 0.001f);
            Assert.AreEqual(50f, center.y, 0.001f);
        }

        [Test]
        public void OneSideWipedFallsBackToTheSurvivors()
        {
            var survivors = new List<Vector2> { new Vector2(30f, -20f), new Vector2(10f, -20f) };

            Vector2 center = CenterOfMass.Midpoint(survivors, new List<Vector2>(), new Vector2(999f, 999f));

            Assert.AreEqual(20f, center.x, 0.001f);
            Assert.AreEqual(-20f, center.y, 0.001f);
        }

        [Test]
        public void OneSideWipedWorksWhicheverSideItIs()
        {
            var survivors = new List<Vector2> { new Vector2(30f, -20f) };

            Vector2 center = CenterOfMass.Midpoint(null, survivors, new Vector2(999f, 999f));

            Assert.AreEqual(30f, center.x, 0.001f);
            Assert.AreEqual(-20f, center.y, 0.001f);
        }

        [Test]
        public void NobodyAliveHoldsWhereTheZoneAlreadyIs()
        {
            var fallback = new Vector2(12f, -34f);

            Assert.AreEqual(fallback, CenterOfMass.Midpoint(null, null, fallback));
            Assert.AreEqual(fallback, CenterOfMass.Midpoint(new List<Vector2>(), new List<Vector2>(), fallback));
        }
    }
}
