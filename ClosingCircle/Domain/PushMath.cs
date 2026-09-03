using UnityEngine;

// How fast somebody is moving away from the zone under their own power, separated out from the pusher so the
// one invariant that matters can be tested: a body that lands exactly where it was sent is not moving.

namespace ClosingCircle.Domain
{
    public static class PushMath
    {
        public static float OutwardSpeed(Vector2 previous, Vector2 applied, Vector2 current, Vector2 inward,
                                         float elapsed, float cap)
        {
            if (elapsed <= 0f) return 0f;

            // Subtract the step we applied last tick, or the push reads its own work as their movement. This
            // only cancels if the position measured and the position commanded belong to the same object.
            Vector2 own = current - previous - applied;

            return Mathf.Clamp(Vector2.Dot(own, -inward) / elapsed, 0f, cap);
        }
    }
}
