using ClosingCircle.Domain;
using NUnit.Framework;
using UnityEngine;

namespace ClosingCircle.Tests
{
    [TestFixture]
    public class ZoneShapeTests
    {
        private const float Radius = 100f;
        private static readonly Vector2 Centre = new Vector2(256f, 256f);

        private static ZoneShape Square => new ZoneShape { Sides = 4, Rotation = 0f };
        private static ZoneShape Hexagon => new ZoneShape { Sides = 6, Rotation = 0f };

        // A point at the given bearing and distance from the centre, using the mod's own convention of degrees
        // clockwise from north.
        private static Vector2 At(float bearingDegrees, float distance)
        {
            float radians = bearingDegrees * Mathf.Deg2Rad;
            return Centre + new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)) * distance;
        }

        [Test]
        public void Contains_CentreIsAlwaysInside()
        {
            Assert.IsTrue(Square.Contains(Centre, Radius, Centre));
            Assert.IsTrue(Hexagon.Contains(Centre, Radius, Centre));
            Assert.IsTrue(ZoneShape.Circle.Contains(Centre, Radius, Centre));
        }

        [Test]
        public void Contains_EdgeMidpointSitsExactlyAtTheConfiguredRadius()
        {
            // A square with rotation 0 has vertices at 0, 90, 180 and 270, so its edge midpoints are at 45.
            Assert.IsTrue(Square.Contains(Centre, Radius, At(45f, Radius - 0.1f)));
            Assert.IsFalse(Square.Contains(Centre, Radius, At(45f, Radius + 0.1f)));
        }

        [Test]
        public void Contains_CornersReachFurtherOutThanEdges()
        {
            // The vertex direction for this square is bearing 0, and a corner sits at radius times root two.
            float justInsideCorner = Radius * Mathf.Sqrt(2f) - 0.5f;

            Assert.IsTrue(Square.Contains(Centre, Radius, At(0f, justInsideCorner)));
            Assert.IsFalse(Square.Contains(Centre, Radius, At(45f, justInsideCorner)));
        }

        [Test]
        public void Contains_NothingReachesBeyondTheCircumradius()
        {
            float beyond = Square.Circumradius(Radius) + 0.1f;

            for (float bearing = 0f; bearing < 360f; bearing += 7f)
                Assert.IsFalse(Square.Contains(Centre, Radius, At(bearing, beyond)), $"bearing {bearing}");
        }

        [Test]
        public void Contains_EverythingInsideTheInradiusIsInside()
        {
            for (float bearing = 0f; bearing < 360f; bearing += 7f)
                Assert.IsTrue(Hexagon.Contains(Centre, Radius, At(bearing, Radius - 0.1f)), $"bearing {bearing}");
        }

        [Test]
        public void Contains_RotationTurnsTheBoundaryWithIt()
        {
            var turned = new ZoneShape { Sides = 4, Rotation = 45f };

            // Rotating by half a step swaps which bearings are corners and which are edge midpoints.
            Assert.IsFalse(turned.Contains(Centre, Radius, At(0f, Radius + 0.1f)));
            Assert.IsTrue(turned.Contains(Centre, Radius, At(45f, Radius + 0.1f)));
        }

        [Test]
        public void Vertices_SitOnTheCircumradiusAndTheirMidpointsOnTheRadius()
        {
            Vector2[] points = Hexagon.Vertices(Centre, Radius);
            Assert.AreEqual(6, points.Length);

            for (int i = 0; i < points.Length; i++)
            {
                Assert.AreEqual(Hexagon.Circumradius(Radius), Vector2.Distance(Centre, points[i]), 0.001f);

                Vector2 midpoint = (points[i] + points[(i + 1) % points.Length]) * 0.5f;
                Assert.AreEqual(Radius, Vector2.Distance(Centre, midpoint), 0.001f);
            }
        }

        // The bug this replaced: the pusher clamped to a circle, which on a square drags somebody toward the
        // centre instead of back through the face they crossed.
        [Test]
        public void NearestInside_ComesBackThroughTheFaceNotTowardTheCentre()
        {
            const float inset = 2f;

            // Straight out along a face normal, so the way back is straight in along it.
            Vector2 outside = At(45f, 200f);
            Vector2 back = Square.NearestInside(Centre, Radius, outside, inset);

            Assert.AreEqual(At(45f, Radius - inset).x, back.x, 0.1f);
            Assert.AreEqual(At(45f, Radius - inset).y, back.y, 0.1f);
        }

        // Past a corner the nearest point is the corner, not somewhere off the end of the edge facing you.
        [Test]
        public void NearestInside_LandsOnTheCornerWhenThatIsNearest()
        {
            const float inset = 2f;

            Vector2 back = Square.NearestInside(Centre, Radius, At(0f, 300f), inset);
            Vector2 corner = At(0f, Square.Circumradius(Radius - inset));

            Assert.AreEqual(corner.x, back.x, 0.1f);
            Assert.AreEqual(corner.y, back.y, 0.1f);
        }

        // Whatever it returns has to actually be inside the shrunk shape, from every direction.
        [Test]
        public void NearestInside_AlwaysLandsInside()
        {
            const float inset = 2f;

            foreach (ZoneShape shape in new[] { Square, Hexagon, ZoneShape.Circle })
            {
                foreach (float bearing in new[] { 0f, 17f, 45f, 96f, 180f, 231f, 359f })
                {
                    foreach (float distance in new[] { 101f, 130f, 250f })
                    {
                        Vector2 back = shape.NearestInside(Centre, Radius, At(bearing, distance), inset);

                        Assert.IsTrue(shape.Contains(Centre, Radius, back),
                                      $"{shape.Sides}-gon at {bearing} deg, {distance}m");
                    }
                }
            }
        }

        [Test]
        public void NearestInside_LeavesSomebodyAlreadyInsideWhereTheyAre()
        {
            Vector2 inside = At(30f, 50f);

            Assert.AreEqual(inside, Square.NearestInside(Centre, Radius, inside, 2f));
            Assert.AreEqual(inside, ZoneShape.Circle.NearestInside(Centre, Radius, inside, 2f));
        }

        [Test]
        public void TryParseSides_AcceptsNamesAndRawCounts()
        {
            Assert.IsTrue(ZoneShape.TryParseSides("Hexagon", out int hexagon));
            Assert.AreEqual(6, hexagon);

            Assert.IsTrue(ZoneShape.TryParseSides("circle", out int circle));
            Assert.AreEqual(ZoneShape.CircleSides, circle);

            Assert.IsTrue(ZoneShape.TryParseSides("12", out int twelve));
            Assert.AreEqual(12, twelve);
        }

        [Test]
        public void TryParseSides_RejectsShapesThatCannotEncloseAnything()
        {
            Assert.IsFalse(ZoneShape.TryParseSides("2", out _));
            Assert.IsFalse(ZoneShape.TryParseSides("banana", out _));
        }
    }
}
