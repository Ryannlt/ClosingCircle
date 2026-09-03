using ClosingCircle.Domain;
using UnityEngine;

namespace ClosingCircle.Centres
{
    // Everything a selector needs to place one stage, so none of them has to reach into ZoneService.
    public struct CentreContext
    {
        public int Index;
        public Vector2 PreviousCentre;
        public float PreviousRadius;
        public float Radius;
        public Vector2 Configured;

        // How far from the fair line this stage may end up, and the line itself. A selector that respects this
        // never meets the resolver's hard clamp, which is what keeps the distribution smooth instead of piling
        // every over-long roll onto the boundary.
        public float Budget;
        public Vector2 LinePoint;
        public Vector2 LineDirection;
    }

    public interface ICentreSelector
    {
        CentreMode Mode { get; }

        // True when the centre needs nothing but the previous circle and the dice, so it can be rolled ahead of
        // time and shown. A mode reading live world state cannot, and stops the walk where it sits.
        bool CanResolveEarly { get; }
        Vector2 Resolve(CentreContext context);
    }
}
