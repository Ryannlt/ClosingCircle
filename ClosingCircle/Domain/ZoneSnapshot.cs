using UnityEngine;

// What the zone looks like at one instant. Both sides derive this from the same plan and the same round clock.

namespace ClosingCircle.Domain
{
    public struct ZoneSnapshot
    {
        public Vector2 Center;
        public float Radius;
    }
}
