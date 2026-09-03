using ClosingCircle.Domain;
using UnityEngine;

namespace ClosingCircle.Centres
{
    public class RandomCentre : ICentreSelector
    {
        public CentreMode Mode => CentreMode.Random;

        public bool CanResolveEarly => true;

        public Vector2 Resolve(CentreContext context)
        {
            float allowed = CentreMath.AllowedRadius(context.PreviousRadius, context.Radius);
            float reach = allowed * ZoneService.Spread;

            Vector2 picked = CentreMath.PointInDisc(context.PreviousCentre, reach, Random.value, Random.value);

            return Squash(context, picked, reach);
        }

        // Compresses the whole spread toward the fair line rather than letting the resolver clamp the long
        // rolls, which would park most rounds at exactly the budget and make the stage predictable.
        private static Vector2 Squash(CentreContext context, Vector2 picked, float reach)
        {
            if (context.Budget >= FairLine.Unlimited) return picked;

            float furthest = CentreMath.DistanceToLine(context.LinePoint, context.LineDirection,
                                                       context.PreviousCentre) + reach;

            if (furthest <= context.Budget || furthest < 1e-5f) return picked;

            Vector2 onLine = CentreMath.ClosestOnLine(context.LinePoint, context.LineDirection, picked);

            return Vector2.Lerp(onLine, picked, context.Budget / furthest);
        }
    }
}
