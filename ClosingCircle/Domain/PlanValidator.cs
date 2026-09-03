using System.Collections.Generic;
using UnityEngine;

// Answers "will this config do what you meant" before anyone spawns, rather than in a warning nobody reads
// mid-match. Pure, so every finding is testable without a round running.

namespace ClosingCircle.Domain
{
    public enum FindingLevel
    {
        Note,
        Warning,
        Error
    }

    public struct Finding
    {
        public FindingLevel Level;
        public string Message;
    }

    // Everything outside the plan that a config can be wrong about, so the validator needs no global state.
    public struct ValidationContext
    {
        public bool HasBisector;
        public Vector2 BisectorPoint;
        public float BisectorHeading;
        public bool Solid;
        public int Damage;

        // Highest round clock seen this round, or 0 when no round has started yet.
        public float RoundSeconds;
    }

    public static class PlanValidator
    {
        public static List<Finding> Validate(ZonePlan plan, ValidationContext context)
        {
            var findings = new List<Finding>();
            if (plan == null) return findings;

            if (plan.Count == 0)
                Add(findings, FindingLevel.Warning, "No stages, so the zone holds its start size all round.");

            if (!context.Solid && context.Damage <= 0)
                Add(findings, FindingLevel.Warning,
                    "Damage is 0 and the zone is not solid, so leaving it costs nothing.");

            for (int i = 0; i < plan.Count; i++) CheckStage(findings, plan, context, i);

            return findings;
        }

        private static void CheckStage(List<Finding> findings, ZonePlan plan, ValidationContext context, int i)
        {
            Stage stage = plan.Stages[i];
            float previous = FairLine.Previous(plan, i);
            float allowed = CentreMath.AllowedRadius(previous, stage.Radius);

            if (stage.Radius >= previous)
                Add(findings, FindingLevel.Warning, stage.Mode == CentreMode.Fixed
                    ? $"Stage {i} radius {stage.Radius:0.#}m is not smaller than the {previous:0.#}m before it, " +
                      "so it cannot move and its written centre will be ignored."
                    : $"Stage {i} radius {stage.Radius:0.#}m is not smaller than the {previous:0.#}m before it, " +
                      "so its circle cannot move.");

            // Sorted by descending FromTime, so the next stage starting before this one ends is an overlap.
            if (i + 1 < plan.Count && plan.Stages[i + 1].FromTime > stage.ToTime)
                Add(findings, FindingLevel.Warning,
                    $"Stage {i} is still closing at {stage.ToTime:0}s when stage {i + 1} starts at " +
                    $"{plan.Stages[i + 1].FromTime:0}s.");

            if (context.RoundSeconds > 0f && stage.FromTime > context.RoundSeconds)
                Add(findings, FindingLevel.Warning,
                    $"Stage {i} starts at {stage.FromTime:0}s but the round is only " +
                    $"{context.RoundSeconds:0}s long, so it begins immediately.");

            if (stage.ToTime < 0f)
                Add(findings, FindingLevel.Warning,
                    $"Stage {i} ends at {stage.ToTime:0}s, which the round never reaches, so it never finishes.");

            if (stage.Mode != CentreMode.Bisector) return;

            if (!context.HasBisector)
            {
                Add(findings, FindingLevel.Error,
                    $"Stage {i} asks for a bisector centre but no Bisector line is set.");
                return;
            }

            if (!FairLine.Reachable(plan, i, context.BisectorPoint, context.BisectorHeading))
            {
                Add(findings, FindingLevel.Error, Strands(plan, i)
                    ? $"Stage {i} can never reach the bisector: a written centre earlier in the plan puts it " +
                      "out of range, and a written centre is never moved for you."
                    : $"Stage {i} can never reach the bisector. It may move {allowed:0.#}m, which is not " +
                      "enough from where the plan can get to. Widen it or move the line.");
                return;
            }

            NoteTheGuard(findings, plan, context, i);
        }

        // Which earlier stages are held closer to the line than they would otherwise roll, so a narrower Random
        // is explained up front rather than looking like a bug.
        private static void NoteTheGuard(List<Finding> findings, ZonePlan plan, ValidationContext context, int i)
        {
            for (int k = 0; k < i; k++)
            {
                if (plan.Stages[k].Mode != CentreMode.Random) continue;

                float budget = FairLine.Budget(plan, k);
                if (budget >= FairLine.Unlimited) continue;

                Add(findings, FindingLevel.Note,
                    $"Stage {k} is held within {budget:0.#}m of the bisector so stage {i} can still reach it.");
            }
        }

        // A written centre the guard will not move is the only way Reachable can fail on a plan whose radii are
        // otherwise generous enough.
        private static bool Strands(ZonePlan plan, int index)
        {
            for (int k = 0; k < index; k++)
                if (plan.Stages[k].Mode == CentreMode.Fixed) return true;

            return false;
        }

        private static void Add(List<Finding> findings, FindingLevel level, string message) =>
            findings.Add(new Finding { Level = level, Message = message });
    }
}
