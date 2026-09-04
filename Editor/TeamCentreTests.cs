using ClosingCircle.Domain;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace ClosingCircle.Tests
{
    [TestFixture]
    public class TeamCentreTests
    {
        [Test]
        public void TwoSidesGiveThePointBetweenThem()
        {
            var one = new List<Vector2> { new Vector2(-100f, 0f) };
            var two = new List<Vector2> { new Vector2(100f, 0f) };

            Vector2 centre = TeamCentre.Midpoint(one, two, Vector2.zero);

            Assert.AreEqual(0f, centre.x, 0.001f);
            Assert.AreEqual(0f, centre.y, 0.001f);
        }

        // The property the whole mode exists for: numbers must not move the answer, or the bigger team drags
        // the zone onto its own ground and the mode is no fairer than following the crowd.
        [Test]
        public void NumbersDoNotDragTheCentre()
        {
            var even = TeamCentre.Midpoint(
                new List<Vector2> { new Vector2(-80f, 40f) },
                new List<Vector2> { new Vector2(80f, -40f) },
                Vector2.zero);

            // Five stacked on one spot against one on the other. Same two positions, wildly uneven counts.
            var lopsided = TeamCentre.Midpoint(
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

        [Test]
        public void ASideSpreadOutIsAveragedBeforeTheMidpoint()
        {
            var one = new List<Vector2> { new Vector2(-100f, 0f), new Vector2(-100f, 100f) };
            var two = new List<Vector2> { new Vector2(100f, 50f) };

            // One's centroid is (-100, 50), so the midpoint is (0, 50).
            Vector2 centre = TeamCentre.Midpoint(one, two, Vector2.zero);

            Assert.AreEqual(0f, centre.x, 0.001f);
            Assert.AreEqual(50f, centre.y, 0.001f);
        }

        [Test]
        public void OneSideWipedFallsBackToTheSurvivors()
        {
            var survivors = new List<Vector2> { new Vector2(30f, -20f), new Vector2(10f, -20f) };

            Vector2 centre = TeamCentre.Midpoint(survivors, new List<Vector2>(), new Vector2(999f, 999f));

            Assert.AreEqual(20f, centre.x, 0.001f);
            Assert.AreEqual(-20f, centre.y, 0.001f);
        }

        [Test]
        public void OneSideWipedWorksWhicheverSideItIs()
        {
            var survivors = new List<Vector2> { new Vector2(30f, -20f) };

            Vector2 centre = TeamCentre.Midpoint(null, survivors, new Vector2(999f, 999f));

            Assert.AreEqual(30f, centre.x, 0.001f);
            Assert.AreEqual(-20f, centre.y, 0.001f);
        }

        [Test]
        public void NobodyAliveHoldsWhereTheZoneAlreadyIs()
        {
            var fallback = new Vector2(12f, -34f);

            Assert.AreEqual(fallback, TeamCentre.Midpoint(null, null, fallback));
            Assert.AreEqual(fallback, TeamCentre.Midpoint(new List<Vector2>(), new List<Vector2>(), fallback));
        }
    }
}
