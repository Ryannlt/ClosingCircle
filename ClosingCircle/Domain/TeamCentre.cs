using System.Collections.Generic;
using UnityEngine;

// Where two teams are, as one point.
//
// Each side's centre of mass first, then the midpoint between them, rather than one average over everybody.
// The difference is the whole reason the mode is fair: an average over all players lets the larger or more
// tightly grouped team drag the zone onto its own ground, while a midpoint of centroids gives twenty against
// five the same answer as five against five. Same reasoning as the bisector.
//
// Takes plain points rather than anything faction-shaped, so the arithmetic is testable without the game.

namespace ClosingCircle.Domain
{
    public static class TeamCentre
    {
        public static Vector2 Midpoint(IList<Vector2> one, IList<Vector2> two, Vector2 fallback)
        {
            bool hasOne = one != null && one.Count > 0;
            bool hasTwo = two != null && two.Count > 0;

            if (hasOne && hasTwo) return (Centroid(one) + Centroid(two)) * 0.5f;

            // One side wiped or not yet spawned is still better information than no information.
            if (hasOne) return Centroid(one);
            if (hasTwo) return Centroid(two);

            return fallback;
        }

        public static Vector2 Centroid(IList<Vector2> points)
        {
            if (points == null || points.Count == 0) return Vector2.zero;

            Vector2 sum = Vector2.zero;
            for (int i = 0; i < points.Count; i++) sum += points[i];

            return sum / points.Count;
        }
    }
}
