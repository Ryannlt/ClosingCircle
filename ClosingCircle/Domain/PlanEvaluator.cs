using UnityEngine;

// Turns a plan and a round clock into a zone. Pure and absolute: the answer depends only on the time passed in,
// never on the previous frame, so the server and every client agree without exchanging anything.

namespace ClosingCircle.Domain
{
    public static class PlanEvaluator
    {
        public static ZoneSnapshot Evaluate(ZonePlan plan, float timeRemaining)
        {
            var current = new ZoneSnapshot { Centre = plan.StartCentre, Radius = plan.StartRadius };

            for (int i = 0; i < plan.Count; i++)
            {
                Stage stage = plan.Stages[i];
                var target = new ZoneSnapshot { Centre = stage.Centre, Radius = stage.Radius };

                // Still ahead of this stage, so nothing after it can have started either.
                if (timeRemaining >= stage.FromTime) return current;

                // Past it. Carry its end state forward, which also holds the zone still across any gap.
                if (timeRemaining <= stage.ToTime)
                {
                    current = target;
                    continue;
                }

                float span = stage.FromTime - stage.ToTime;
                float u = Mathf.Clamp01((stage.FromTime - timeRemaining) / span);

                return new ZoneSnapshot
                {
                    Centre = Vector2.Lerp(current.Centre, target.Centre, u),
                    Radius = Mathf.Lerp(current.Radius, target.Radius, u)
                };
            }

            return current;
        }

        // Which stage is mid-close right now, or -1 between and outside them. The announcer watches this rather
        // than comparing radii, so a stage that closes by only a metre still announces.
        public static int ActiveStageIndex(ZonePlan plan, float timeRemaining)
        {
            for (int i = 0; i < plan.Count; i++)
            {
                Stage stage = plan.Stages[i];
                if (timeRemaining < stage.FromTime && timeRemaining > stage.ToTime) return i;
            }

            return -1;
        }
    }
}
