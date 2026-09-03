using UnityEngine;

// The geometry behind placing a stage centre. Pure, and it takes unit randoms as arguments rather than calling
// Random itself, so a test can drive the whole distribution instead of sampling it and hoping.

namespace ClosingCircle.Domain
{
    public static class CentreMath
    {
        // Squared metres, so it survives the scale these discs are measured at.
        private const float Tangent = 0.01f;

        // How far a stage centre may sit from the previous centre while its circle stays inside the previous
        // one. This is what guarantees the next zone is reachable from the current one.
        public static float AllowedRadius(float previousRadius, float radius) =>
            Mathf.Max(0f, previousRadius - radius);

        // Degrees clockwise from north, the same convention the shape and the bot code use.
        public static Vector2 Direction(float headingDegrees)
        {
            float radians = headingDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
        }

        public static Vector2 PointOnLine(Vector2 linePoint, Vector2 direction, float t) =>
            linePoint + direction * t;

        // Where an infinite line crosses a disc, as the two distances along the line from linePoint. False when
        // the line misses entirely, which is the case where fairness and reachability cannot both be had.
        public static bool TryChord(Vector2 discCentre, float discRadius, Vector2 linePoint, Vector2 direction,
                                    out float tMin, out float tMax)
        {
            tMin = tMax = 0f;

            Vector2 offset = linePoint - discCentre;
            float b = Vector2.Dot(offset, direction);
            float c = Vector2.Dot(offset, offset) - discRadius * discRadius;
            float discriminant = b * b - c;

            // A tangent line does meet the disc, and a chain of clamps lands exactly on tangency often enough
            // that rounding must not be what decides whether a stage is placeable.
            if (discriminant < -Tangent) return false;

            float root = Mathf.Sqrt(Mathf.Max(0f, discriminant));
            tMin = -b - root;
            tMax = -b + root;
            return true;
        }

        // Spread 0 always returns the chord midpoint, which is its closest point to the disc centre. Spread 1
        // uses the whole chord. Anything between narrows it symmetrically about that midpoint.
        public static float PickOnChord(float tMin, float tMax, float spread, float unit01)
        {
            float mid = (tMin + tMax) * 0.5f;
            float half = (tMax - tMin) * 0.5f;

            return mid + (unit01 * 2f - 1f) * Mathf.Clamp01(spread) * half;
        }

        // Uniform by area rather than by radius, so the middle is not over-represented.
        public static Vector2 PointInDisc(Vector2 centre, float radius, float angle01, float radius01) =>
            centre + Direction(angle01 * 360f) * (radius * Mathf.Sqrt(Mathf.Clamp01(radius01)));

        // The last word on reachability. Every resolved centre goes through this, so a selector cannot break
        // the nesting rule however it arrives at its answer.
        public static Vector2 Nest(Vector2 previousCentre, float previousRadius, float radius, Vector2 desired)
        {
            float allowed = AllowedRadius(previousRadius, radius);
            Vector2 delta = desired - previousCentre;
            float distance = delta.magnitude;

            if (distance <= allowed) return desired;
            if (distance < 1e-5f) return previousCentre;

            return previousCentre + delta / distance * allowed;
        }

        // The point on a line nearest a disc centre, which is the fairest position still worth having when the
        // line misses the disc altogether.
        public static Vector2 ClosestOnLine(Vector2 linePoint, Vector2 direction, Vector2 target) =>
            linePoint + direction * Vector2.Dot(target - linePoint, direction);

        public static float DistanceToLine(Vector2 linePoint, Vector2 direction, Vector2 target) =>
            Vector2.Distance(target, ClosestOnLine(linePoint, direction, target));

        // Pulls a centre back into the band within budget of the fair line, without leaving the disc it is
        // allowed to sit in. Straight at the line first; if that leaves the disc, the disc's own closest point
        // to the line is taken instead, which always satisfies the budget because the stage before respected
        // its own.
        public static Vector2 ClampToLine(Vector2 desired, Vector2 previousCentre, float previousRadius,
                                          float radius, Vector2 linePoint, Vector2 direction, float budget)
        {
            Vector2 onLine = ClosestOnLine(linePoint, direction, desired);
            float distance = Vector2.Distance(desired, onLine);

            if (distance <= budget) return desired;

            Vector2 pulled = distance < 1e-5f
                ? desired
                : Vector2.Lerp(desired, onLine, (distance - budget) / distance);

            float allowed = AllowedRadius(previousRadius, radius);
            if (Vector2.Distance(pulled, previousCentre) <= allowed) return pulled;

            return Nest(previousCentre, previousRadius, radius,
                        ClosestOnLine(linePoint, direction, previousCentre));
        }
    }
}
