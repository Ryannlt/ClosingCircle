using UnityEngine;

// How far from the fair line each stage is allowed to wander. Shared by the resolver, which enforces it, and
// the validator, which reports on it, so the two can never disagree about what fair means.
//
// A bisector stage can only reach the line when the centre before it is within its own allowed radius of the
// line. Working backwards from that, every earlier stage gets a budget, and those budgets GROW the further back
// you go: a stage five steps out may wander further, because the stages between it and the fair line have room
// to pull it back.

namespace ClosingCircle.Domain
{
    public static class FairLine
    {
        // No bisector stage ahead, so nothing to keep in reach of.
        public const float Unlimited = float.MaxValue;

        // Held just inside the budget rather than exactly on it, so the stage that follows gets a real chord to
        // pick along instead of a tangent that rounding can turn into "unreachable".
        private const float Margin = 0.05f;

        // The furthest the stage at this index may end up from the line. Radii come from the plan, so a stage
        // that has already resolved is not treated any differently: the budget is a property of the schedule.
        public static float Budget(ZonePlan plan, int index)
        {
            if (plan == null || index < 0 || index >= plan.Count) return Unlimited;

            float carried = 0f;

            for (int i = index + 1; i < plan.Count; i++)
            {
                float allowed = CentreMath.AllowedRadius(Previous(plan, i), plan.Stages[i].Radius);

                if (plan.Stages[i].Mode == CentreMode.Bisector)
                    return Mathf.Max(0f, carried + allowed - Margin);

                carried += allowed;
            }

            return Unlimited;
        }

        // The closest to the line the centre before a bisector stage could possibly be, with every stage from
        // the start pulling toward it as hard as it can. If even that is out of reach, no roll and no guard
        // saves the config.
        public static bool Reachable(ZonePlan plan, int index, Vector2 linePoint, float heading)
        {
            if (plan == null || index < 0 || index >= plan.Count) return true;

            Vector2 direction = CentreMath.Direction(heading);
            float distance = CentreMath.DistanceToLine(linePoint, direction, plan.StartCentre);

            for (int i = 0; i < index; i++)
            {
                // A written centre is obeyed rather than pulled, so it sets the distance outright.
                if (plan.Stages[i].Mode == CentreMode.Fixed)
                {
                    distance = CentreMath.DistanceToLine(linePoint, direction, plan.Stages[i].ConfiguredCentre);
                    continue;
                }

                distance = Mathf.Max(0f, distance - CentreMath.AllowedRadius(Previous(plan, i),
                                                                            plan.Stages[i].Radius));
            }

            return distance <= CentreMath.AllowedRadius(Previous(plan, index), plan.Stages[index].Radius);
        }

        public static float Previous(ZonePlan plan, int index) =>
            index == 0 ? plan.StartRadius : plan.Stages[index - 1].Radius;
    }
}
