using UnityEngine;

// One closing step. Times are seconds remaining in the round, so FromTime is always the larger of the two.

namespace ClosingCircle.Domain
{
    public struct Stage
    {
        public float FromTime;
        public float ToTime;
        public float Radius;
        public Vector2 Center;
        public CenterMode Mode;

        // The center as written. Kept apart from Center so resolving never destroys the configuration and a
        // second round cannot randomise from the first round's result.
        public Vector2 ConfiguredCenter;

        // Set once the center has been decided for this round, so a stage is never re-rolled mid-close.
        public bool Resolved;

        public bool IsValid => FromTime > ToTime && Radius > 0f;

        public override string ToString()
        {
            // An unresolved mode has not rolled yet, and its center is a placeholder rather than a decision.
            string where = Resolved || Mode == CenterMode.Fixed
                ? $"at ({Center.x:0.#}, {Center.y:0.#})"
                : "pending";

            return $"from {FromTime:0}s to {ToTime:0}s, radius {Radius:0.#} {where} [{Mode}]";
        }
    }
}
