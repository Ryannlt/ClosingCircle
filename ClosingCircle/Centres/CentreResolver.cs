using ClosingCircle.Domain;
using System.Collections.Generic;
using UnityEngine;

// Server side. Decides each stage's centre as early as it can be known, then leaves the existing broadcaster to
// push it, so the preview shows where the zone will actually go rather than a guess that snaps on the day.
//
// Resolving early does not make the zone jump. PlanEvaluator returns the previous stage's state untouched until
// a stage's FromTime arrives, so a future centre has no effect on the zone at all until its close begins; the
// slide belongs to the evaluator, not to when the dice were rolled.

namespace ClosingCircle.Centres
{
    public static class CentreResolver
    {
        private static readonly Dictionary<CentreMode, ICentreSelector> Selectors =
            new Dictionary<CentreMode, ICentreSelector>();

        private static int _seenPlanVersion = -1;

        static CentreResolver()
        {
            // uMod forbids reflection, so the registry is written out by hand.
            Register(new FixedCentre());
            Register(new BisectorCentre());
            Register(new RandomCentre());
        }

        private static void Register(ICentreSelector selector) => Selectors[selector.Mode] = selector;

        public static void Reset()
        {
            ZonePlan plan = ZoneService.Plan;
            for (int i = 0; i < plan.Count; i++) plan.UnresolveStage(i);

            _seenPlanVersion = ZoneService.PlanVersion;
        }

        public static void Step(float timeRemaining)
        {
            ZonePlan plan = ZoneService.Plan;

            bool changed = DropStaleFutures(plan, timeRemaining);

            Vector2 previousCentre = plan.StartCentre;
            float previousRadius = plan.StartRadius;

            // Walked by index rather than tracked with a counter, because adding a stage at runtime re-sorts
            // the list and would leave any counter pointing at the wrong one.
            for (int i = 0; i < plan.Count; i++)
            {
                Stage stage = plan.Stages[i];

                if (!stage.Resolved)
                {
                    bool due = timeRemaining < stage.FromTime;

                    // Everything after this nests inside its circle, so if this one cannot be known yet then
                    // nothing beyond it can be either.
                    if (!due && !CanResolveEarly(stage.Mode)) return;

                    plan.ResolveStage(i, Decide(plan, i, stage, previousCentre, previousRadius));
                    changed = true;

                    Logger.Log($"Resolved stage {i}: {plan.Stages[i]}", LogLevel.INFO);
                }

                previousCentre = plan.Stages[i].Centre;
                previousRadius = plan.Stages[i].Radius;
            }

            if (changed) ZoneService.MarkChanged();
        }

        // Adding or removing a stage changes what the ones after it nest inside, so their rolls are void. A
        // stage already closing or finished keeps its centre: the zone is flying to it.
        private static bool DropStaleFutures(ZonePlan plan, float timeRemaining)
        {
            if (_seenPlanVersion == ZoneService.PlanVersion) return false;

            _seenPlanVersion = ZoneService.PlanVersion;

            bool changed = false;

            for (int i = 0; i < plan.Count; i++)
            {
                if (!plan.Stages[i].Resolved || timeRemaining < plan.Stages[i].FromTime) continue;

                plan.UnresolveStage(i);
                changed = true;
            }

            return changed;
        }

        private static bool CanResolveEarly(CentreMode mode) =>
            Selectors.TryGetValue(mode, out ICentreSelector selector) && selector.CanResolveEarly;

        private static Vector2 Decide(ZonePlan plan, int index, Stage stage, Vector2 previousCentre,
                                      float previousRadius)
        {
            if (!Selectors.TryGetValue(stage.Mode, out ICentreSelector selector)) selector = Selectors[CentreMode.Fixed];

            var context = new CentreContext
            {
                Index = index,
                PreviousCentre = previousCentre,
                PreviousRadius = previousRadius,
                Radius = stage.Radius,
                Configured = stage.ConfiguredCentre,
                Budget = ZoneService.HasBisector ? FairLine.Budget(plan, index) : FairLine.Unlimited,
                LinePoint = ZoneService.BisectorPoint,
                LineDirection = CentreMath.Direction(ZoneService.BisectorHeading)
            };

            // Nesting is enforced here rather than trusted to each selector, so no mode can produce a zone the
            // players inside the previous one cannot reach.
            Vector2 centre = CentreMath.Nest(previousCentre, previousRadius, stage.Radius,
                                             selector.Resolve(context));

            return KeepFairLineInReach(plan, index, stage, previousCentre, previousRadius, centre);
        }

        // The same idea one step further: a randomised stage must not wander so far that a later bisector stage
        // can no longer reach the fair line. A written centre is an instruction and is left alone, so a Fixed
        // stage that strands a later one is the validator's to report rather than this method's to move.
        private static Vector2 KeepFairLineInReach(ZonePlan plan, int index, Stage stage, Vector2 previousCentre,
                                                   float previousRadius, Vector2 centre)
        {
            if (stage.Mode == CentreMode.Fixed || !ZoneService.HasBisector) return centre;

            float budget = FairLine.Budget(plan, index);
            if (budget >= FairLine.Unlimited) return centre;

            Vector2 direction = CentreMath.Direction(ZoneService.BisectorHeading);
            Vector2 clamped = CentreMath.ClampToLine(centre, previousCentre, previousRadius, stage.Radius,
                                                     ZoneService.BisectorPoint, direction, budget);

            if (clamped == centre) return centre;

            Logger.Log($"Stage {index} held within {budget:0.#}m of the bisector so a later stage can still " +
                       "reach it.", LogLevel.DEBUG);

            return clamped;
        }
    }
}
