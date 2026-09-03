using ClosingCircle.Domain;
using UnityEngine;

// Places the centre somewhere along the configured fair line. On a map where both spawns sit equidistant from
// that line, every point on it leaves them equidistant, so this is the only randomness that costs nobody
// ground.

namespace ClosingCircle.Centres
{
    public class BisectorCentre : ICentreSelector
    {
        public CentreMode Mode => CentreMode.Bisector;

        public bool CanResolveEarly => true;

        public Vector2 Resolve(CentreContext context)
        {
            if (!ZoneService.HasBisector)
            {
                Logger.Log($"Stage {context.Index} asks for a bisector centre but none is configured. " +
                           "Holding the previous centre. Set SetBisector to x,z,heading.", LogLevel.WARNING);
                return context.PreviousCentre;
            }

            Vector2 direction = CentreMath.Direction(ZoneService.BisectorHeading);
            float allowed = CentreMath.AllowedRadius(context.PreviousRadius, context.Radius);

            if (!CentreMath.TryChord(context.PreviousCentre, allowed, ZoneService.BisectorPoint, direction,
                                     out float tMin, out float tMax))
            {
                // Fair and reachable cannot both be had here, so keep it reachable and make the config error
                // loud rather than quietly playing an unfair round.
                Logger.Log($"Stage {context.Index}: the bisector does not pass within reach of the previous " +
                           "circle, so this stage cannot be placed fairly. Check SetBisector against the " +
                           "stage radii.", LogLevel.WARNING);

                return CentreMath.ClosestOnLine(ZoneService.BisectorPoint, direction, context.PreviousCentre);
            }

            float t = CentreMath.PickOnChord(tMin, tMax, ZoneService.Spread, Random.value);
            return CentreMath.PointOnLine(ZoneService.BisectorPoint, direction, t);
        }
    }
}
