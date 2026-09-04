using UnityEngine;

// Bringing the next stage forward to now.
//
// The zone is a pure function of the round clock, which a mod cannot move, so advancing the schedule means
// moving the schedule rather than the clock: the next stage's times are shifted up to the present, and every
// stage after it is shifted by the same amount. Shifting the whole tail by one delta is what keeps the gaps
// between stages intact and makes it impossible to create the overlap the validator warns about.
//
// Times are seconds remaining, so a stage that has not begun has a FromTime *below* the current clock and the
// delta is positive. Everything moves earlier in the round, never later, so nothing can be pushed past the end.

namespace ClosingCircle.Domain
{
    public static class StageAdvance
    {
        // The first stage that has not begun closing, or -1. The list is sorted by descending FromTime, so the
        // first one the clock has not yet fallen to is the earliest that is still ahead.
        public static int NextIndex(ZonePlan plan, float timeRemaining)
        {
            if (plan == null) return -1;

            for (int i = 0; i < plan.Count; i++)
                if (timeRemaining > plan.Stages[i].FromTime) return i;

            return -1;
        }

        public static bool TryPlan(ZonePlan plan, float timeRemaining, out int index, out float delta)
        {
            delta = 0f;
            index = NextIndex(plan, timeRemaining);
            if (index < 0) return false;

            float start = timeRemaining;

            // Never start before the stage ahead of it has finished closing, or two would be sliding at once
            // and the evaluator would have to pick one. A stage that has already finished has a ToTime the
            // clock has passed, so this leaves the start at the present.
            if (index > 0) start = Mathf.Min(start, plan.Stages[index - 1].ToTime);

            delta = start - plan.Stages[index].FromTime;
            return delta > 0f;
        }

        // True when the stage will not begin at once because the one before it is still closing.
        public static bool WaitsForCurrent(ZonePlan plan, float timeRemaining, int index) =>
            index > 0 && timeRemaining > plan.Stages[index - 1].ToTime;
    }
}
