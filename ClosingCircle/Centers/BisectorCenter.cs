using ClosingCircle.Domain;
using UnityEngine;

// Places the center somewhere along the configured fair line. On a map where both spawns sit equidistant from
// that line, every point on it leaves them equidistant, so this is the only randomness that costs nobody
// ground.

namespace ClosingCircle.Centers
{
    public class BisectorCenter : ICenterSelector
    {
        public CenterMode Mode => CenterMode.Bisector;

        public bool CanResolveEarly => true;

        public Vector2 Resolve(CenterContext context)
        {
            if (!ZoneService.HasBisector)
            {
                Logger.Log($"Stage {context.Index} asks for a bisector center but none is configured. " +
                           "Holding the previous center. Set Bisector to x,z,heading.", LogLevel.WARNING);
                return context.PreviousCenter;
            }

            Vector2 direction = CenterMath.Direction(ZoneService.BisectorHeading);
            float allowed = CenterMath.AllowedRadius(context.PreviousRadius, context.Radius);

            if (!CenterMath.TryChord(context.PreviousCenter, allowed, ZoneService.BisectorPoint, direction,
                                     out float tMin, out float tMax))
            {
                // Fair and reachable cannot both be had here, so keep it reachable and make the config error
                // loud rather than quietly playing an unfair round.
                Logger.Log($"Stage {context.Index}: the bisector does not pass within reach of the previous " +
                           "circle, so this stage cannot be placed fairly. Check Bisector against the " +
                           "stage radii.", LogLevel.WARNING);

                return CenterMath.ClosestOnLine(ZoneService.BisectorPoint, direction, context.PreviousCenter);
            }

            float t = CenterMath.PickOnChord(tMin, tMax, ZoneService.Spread, Random.value);
            return CenterMath.PointOnLine(ZoneService.BisectorPoint, direction, t);
        }
    }
}
