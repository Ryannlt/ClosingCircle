using UnityEngine;

// One closing step. Times are seconds remaining in the round, so FromTime is always the larger of the two.

namespace ClosingCircle.Domain
{
    public struct Stage
    {
        public float FromTime;
        public float ToTime;
        public float Radius;
        public Vector2 Centre;
        public CentreMode Mode;

        // The centre as written. Kept apart from Centre so resolving never destroys the configuration and a
        // second round cannot randomise from the first round's result.
        public Vector2 ConfiguredCentre;

        // Set once the centre has been decided for this round, so a stage is never re-rolled mid-close.
        public bool Resolved;

        public bool IsValid => FromTime > ToTime && Radius > 0f;

        public override string ToString()
        {
            // An unresolved mode has not rolled yet, and its centre is a placeholder rather than a decision.
            string where = Resolved || Mode == CentreMode.Fixed
                ? $"at ({Centre.x:0.#}, {Centre.y:0.#})"
                : "pending";

            return $"from {FromTime:0}s to {ToTime:0}s, radius {Radius:0.#} {where} [{Mode}]";
        }
    }
}
