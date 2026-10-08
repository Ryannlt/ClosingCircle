using ClosingCircle.Domain;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace ClosingCircle.Tests
{
    [TestFixture]
    public class PlanValidatorTests
    {
        // The fair line from the live config that prompted all this: through the origin, running east.
        private static ValidationContext Context => new ValidationContext
        {
            HasBisector = true,
            BisectorPoint = Vector2.zero,
            BisectorHeading = 90f,
            Solid = true,
            Damage = 1,
            RoundSeconds = 600f
        };

        private static Stage Make(float from, float to, float radius, CenterMode mode, Vector2 center) =>
            new Stage
            {
                FromTime = from, ToTime = to, Radius = radius, Mode = mode,
                Center = center, ConfiguredCenter = center
            };

        // The exact schedule from the 02:33 log: Bisector 100, Random 40, Bisector 20.
        private static ZonePlan LoggedPlan()
        {
            var plan = new ZonePlan { StartRadius = 200f, StartCenter = Vector2.zero };
            plan.Add(Make(540f, 480f, 100f, CenterMode.Bisector, Vector2.zero));
            plan.Add(Make(420f, 360f, 40f, CenterMode.Random, Vector2.zero));
            plan.Add(Make(120f, 60f, 20f, CenterMode.Bisector, Vector2.zero));
            return plan;
        }

        private static bool Any(List<Finding> findings, FindingLevel level)
        {
            for (int i = 0; i < findings.Count; i++)
                if (findings[i].Level == level) return true;

            return false;
        }

        // A server with no Bisector line set is perfectly legal, and every mode that does not need one has to
        // validate cleanly on it. Found live from the panel: it built its trial stage as Bisector whatever the
        // admin had picked, so adding a Fixed stage was refused with "asks for a bisector center".
        [Test]
        public void ModesThatNeedNoBisectorPassWithoutOne()
        {
            var bare = new ValidationContext
            {
                HasBisector = false,
                Solid = true,
                Damage = 1,
                RoundSeconds = 800f
            };

            CenterMode[] modes = { CenterMode.Fixed, CenterMode.Random, CenterMode.Team, CenterMode.Players };

            foreach (CenterMode mode in modes)
            {
                var plan = new ZonePlan { StartRadius = 200f, StartCenter = Vector2.zero };
                plan.Add(Make(660f, 640f, 20f, mode, new Vector2(-71f, 4f)));

                List<Finding> findings = PlanValidator.Validate(plan, bare);

                Assert.IsFalse(Any(findings, FindingLevel.Error), mode + " reported an error without a bisector");
                Assert.IsFalse(Any(findings, FindingLevel.Warning),
                               mode + " reported a warning without a bisector");
            }
        }

        // The other half of the same rule: a Bisector stage with no line really is an error, so the check above
        // is not just asserting that the validator has gone quiet.
        [Test]
        public void ABisectorStageStillNeedsALine()
        {
            var bare = new ValidationContext
            {
                HasBisector = false,
                Solid = true,
                Damage = 1,
                RoundSeconds = 800f
            };

            var plan = new ZonePlan { StartRadius = 200f, StartCenter = Vector2.zero };
            plan.Add(Make(660f, 640f, 20f, CenterMode.Bisector, Vector2.zero));

            Assert.IsTrue(Any(PlanValidator.Validate(plan, bare), FindingLevel.Error));
        }

        // Budgets grow backwards, because the stages between a wanderer and the fair line can pull it back.
        [Test]
        public void BudgetsGrowTheFurtherFromTheBisectorStage()
        {
            ZonePlan plan = LoggedPlan();

            // Stage 1 sits directly before the bisector stage, so it gets that stage's reach of 40 - 20, less a
            // small margin so the chord that follows is real rather than a tangent rounding can lose.
            Assert.AreEqual(19.95f, FairLine.Budget(plan, 1), 0.001f);

            // Stage 0 has stage 1's own 60m of movement on top of that.
            Assert.AreEqual(79.95f, FairLine.Budget(plan, 0), 0.001f);
        }

        // The bug this margin exists for: parking a stage exactly on its budget left the next one tangent to
        // the line, and rounding decided whether it counted as reachable.
        [Test]
        public void AStageHeldAtItsBudgetLeavesTheNextOneARealChord()
        {
            ZonePlan plan = LoggedPlan();

            float budget = FairLine.Budget(plan, 1);
            var held = new Vector2(73.9f, -budget);
            float reach = CenterMath.AllowedRadius(40f, 20f);

            Assert.IsTrue(CenterMath.TryChord(held, reach, Vector2.zero, Vector2.right,
                                              out float tMin, out float tMax));
            Assert.Greater(tMax - tMin, 1f);
        }

        [Test]
        public void NoBisectorStageAheadMeansNoBudget()
        {
            var plan = new ZonePlan { StartRadius = 200f, StartCenter = Vector2.zero };
            plan.Add(Make(540f, 480f, 100f, CenterMode.Random, Vector2.zero));

            Assert.AreEqual(FairLine.Unlimited, FairLine.Budget(plan, 0));
        }

        // The whole point: with the guard the logged config is sound, where before it was a coin flip.
        [Test]
        public void TheLoggedConfigIsFairAndReportsNoProblem()
        {
            List<Finding> findings = PlanValidator.Validate(LoggedPlan(), Context);

            Assert.IsFalse(Any(findings, FindingLevel.Error), Describe(findings));
            Assert.IsFalse(Any(findings, FindingLevel.Warning), Describe(findings));

            // But it does explain that stage 1 is being held in.
            Assert.IsTrue(Any(findings, FindingLevel.Note), Describe(findings));
        }

        [Test]
        public void AStartTooFarFromTheLineToEverReachIsAnError()
        {
            var plan = new ZonePlan { StartRadius = 200f, StartCenter = new Vector2(0f, 500f) };
            plan.Add(Make(540f, 480f, 190f, CenterMode.Bisector, Vector2.zero));

            Assert.IsTrue(Any(PlanValidator.Validate(plan, Context), FindingLevel.Error));
        }

        // A written center is obeyed rather than pulled, so it can strand a later stage where Random cannot.
        [Test]
        public void AWrittenCenterThatStrandsALaterBisectorStageIsAnError()
        {
            var plan = new ZonePlan { StartRadius = 200f, StartCenter = Vector2.zero };
            plan.Add(Make(540f, 480f, 100f, CenterMode.Fixed, new Vector2(0f, 95f)));
            plan.Add(Make(420f, 360f, 90f, CenterMode.Bisector, Vector2.zero));

            Assert.IsTrue(Any(PlanValidator.Validate(plan, Context), FindingLevel.Error));
        }

        [Test]
        public void ABisectorStageWithNoLineConfiguredIsAnError()
        {
            ValidationContext none = Context;
            none.HasBisector = false;

            Assert.IsTrue(Any(PlanValidator.Validate(LoggedPlan(), none), FindingLevel.Error));
        }

        [Test]
        public void ARadiusThatDoesNotShrinkIsFlagged()
        {
            var plan = new ZonePlan { StartRadius = 100f, StartCenter = Vector2.zero };
            plan.Add(Make(540f, 480f, 100f, CenterMode.Random, Vector2.zero));

            Assert.IsTrue(Any(PlanValidator.Validate(plan, Context), FindingLevel.Warning));
        }

        [Test]
        public void OverlappingStagesAreFlagged()
        {
            var plan = new ZonePlan { StartRadius = 200f, StartCenter = Vector2.zero };
            plan.Add(Make(540f, 300f, 100f, CenterMode.Random, Vector2.zero));
            plan.Add(Make(400f, 200f, 40f, CenterMode.Random, Vector2.zero));

            Assert.IsTrue(Any(PlanValidator.Validate(plan, Context), FindingLevel.Warning));
        }

        [Test]
        public void TimesOutsideTheRoundAreFlagged()
        {
            var early = new ZonePlan { StartRadius = 200f, StartCenter = Vector2.zero };
            early.Add(Make(900f, 800f, 100f, CenterMode.Random, Vector2.zero));
            Assert.IsTrue(Any(PlanValidator.Validate(early, Context), FindingLevel.Warning));

            var late = new ZonePlan { StartRadius = 200f, StartCenter = Vector2.zero };
            late.Add(Make(540f, -60f, 100f, CenterMode.Random, Vector2.zero));
            Assert.IsTrue(Any(PlanValidator.Validate(late, Context), FindingLevel.Warning));
        }

        [Test]
        public void AZoneThatCostsNothingToLeaveIsFlagged()
        {
            ValidationContext harmless = Context;
            harmless.Solid = false;
            harmless.Damage = 0;

            Assert.IsTrue(Any(PlanValidator.Validate(LoggedPlan(), harmless), FindingLevel.Warning));
        }

        [Test]
        public void AnEmptyPlanSaysSoAndDoesNotThrow()
        {
            var plan = new ZonePlan { StartRadius = 200f, StartCenter = Vector2.zero };

            Assert.IsTrue(Any(PlanValidator.Validate(plan, Context), FindingLevel.Warning));
            Assert.AreEqual(0, PlanValidator.Validate(null, Context).Count);
        }

        private static string Describe(List<Finding> findings)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < findings.Count; i++)
                sb.Append(findings[i].Level).Append(": ").Append(findings[i].Message).Append('\n');

            return sb.ToString();
        }
    }
}
