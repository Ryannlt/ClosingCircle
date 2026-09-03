using ClosingCircle.Domain;
using UnityEngine;

namespace ClosingCircle.Centres
{
    public class FixedCentre : ICentreSelector
    {
        public CentreMode Mode => CentreMode.Fixed;

        public bool CanResolveEarly => true;

        public Vector2 Resolve(CentreContext context) => context.Configured;
    }
}
