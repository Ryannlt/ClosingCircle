// Which stages are worth showing. Stages resolve in order and pass in order, so the answer is always one
// contiguous run: from the first that has not finished, to the last whose centre has actually been decided.

namespace ClosingCircle.Domain
{
    public static class PlanWindow
    {
        public static void Visible(ZonePlan plan, float timeRemaining, out int from, out int count)
        {
            from = 0;
            count = 0;

            if (plan == null) return;

            for (int i = 0; i < plan.Count; i++)
            {
                Stage stage = plan.Stages[i];

                // Finished. Nothing is learned from drawing where the zone has already been.
                if (timeRemaining <= stage.ToTime)
                {
                    from = i + 1;
                    continue;
                }

                // Undecided, and everything after nests inside it, so the run ends here rather than guessing.
                if (!stage.Resolved) break;

                count++;
            }
        }
    }
}
