using ClosingCircle.Domain;
using UnityEngine;

namespace ClosingCircle.Centers
{
    public class FixedCenter : ICenterSelector
    {
        public CenterMode Mode => CenterMode.Fixed;

        public bool CanResolveEarly => true;

        public Vector2 Resolve(CenterContext context) => context.Configured;
    }
}
