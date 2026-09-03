using UnityEngine;

// The zone outline, as a regular polygon. A circle is just a 64-gon, so the mesh and the containment test read
// the same two numbers and can never disagree about where the boundary is.

namespace ClosingCircle.Domain
{
    public struct ZoneShape
    {
        public const int CircleSides = 64;
        public const int MinSides = 3;

        public int Sides;
        public float Rotation;

        public static ZoneShape Circle => new ZoneShape { Sides = CircleSides, Rotation = 0f };

        // Radius is configured centre-to-edge, so the vertices sit further out than that by this factor.
        public float Circumradius(float radius) => radius / Mathf.Cos(Mathf.PI / Sides);

        public float StepDegrees => 360f / Sides;

        // Outline points at a given centre and radius, ordered anticlockwise from the rotation offset.
        public Vector2[] Vertices(Vector2 centre, float radius)
        {
            var points = new Vector2[Sides];
            float circumradius = Circumradius(radius);

            for (int i = 0; i < Sides; i++)
            {
                float angle = (Rotation + i * StepDegrees) * Mathf.Deg2Rad;
                points[i] = centre + new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * circumradius;
            }

            return points;
        }

        // O(1) rather than a loop over edges. On a regular polygon the binding edge is always the one whose
        // outward normal is nearest in bearing, so one projection settles it.
        public bool Contains(Vector2 centre, float radius, Vector2 point)
        {
            Vector2 delta = point - centre;
            float distance = delta.magnitude;

            if (distance <= radius) return true;
            if (distance > Circumradius(radius)) return false;

            float step = StepDegrees;
            float bearing = Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg;

            // Edge normals sit half a step around from each vertex, so this is the signed angle to the nearest.
            float offset = Mathf.Repeat(bearing - Rotation, step) - step * 0.5f;

            return distance * Mathf.Cos(offset * Mathf.Deg2Rad) <= radius;
        }

        // The nearest point at least inset inside the boundary. Clamping to a circle instead, which is what
        // the pusher used to do, walks somebody toward the centre rather than through the face they crossed:
        // wrong by up to a circumradius on a square, and invisible on a 64-gon.
        public Vector2 NearestInside(Vector2 centre, float radius, Vector2 point, float inset)
        {
            float shrunk = Mathf.Max(0f, radius - inset);
            if (shrunk <= 0f) return centre;
            if (Contains(centre, shrunk, point)) return point;

            Vector2 delta = point - centre;
            if (delta.sqrMagnitude < 1e-10f) return point;

            float step = StepDegrees;
            float bearing = Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg;

            // Vertex i sits at Rotation + i * step, so the wedge a bearing falls in names the edge it faces.
            int edge = Mathf.FloorToInt(Mathf.Repeat(bearing - Rotation, 360f) / step);

            float circumradius = Circumradius(shrunk);
            Vector2 a = centre + CentreMath.Direction(Rotation + edge * step) * circumradius;
            Vector2 b = centre + CentreMath.Direction(Rotation + (edge + 1) * step) * circumradius;

            // Clamped to the segment, because past a corner the nearest point is the corner itself rather than
            // somewhere off the end of the edge that happens to face you.
            Vector2 along = b - a;
            float length = Vector2.Dot(along, along);
            if (length < 1e-10f) return a;

            return a + along * Mathf.Clamp01(Vector2.Dot(point - a, along) / length);
        }

        // Accepts a friendly name or a raw side count.
        public static bool TryParseSides(string value, out int sides)
        {
            switch (value.Trim().ToLowerInvariant())
            {
                case "circle": sides = CircleSides; return true;
                case "triangle": sides = 3; return true;
                case "square": sides = 4; return true;
                case "pentagon": sides = 5; return true;
                case "hexagon": sides = 6; return true;
                case "heptagon": sides = 7; return true;
                case "octagon": sides = 8; return true;
            }

            return int.TryParse(value, out sides) && sides >= MinSides;
        }
    }
}
