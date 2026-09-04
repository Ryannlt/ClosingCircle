using ClosingCircle.Domain;
using NUnit.Framework;
using UnityEngine;

namespace ClosingCircle.Tests
{
    [TestFixture]
    public class StageAdvanceTests
    {
        // 780-740, 660-620, 540-500. Gaps of 80s between them.
        private static ZonePlan Plan()
        {
            var plan = new ZonePlan { StartRadius = 250f, StartCentre = Vector2.zero };

            plan.Add(new Stage { FromTime = 780f, ToTime = 740f, Radius = 150f, Mode = CentreMode.Random });
            plan.Add(new Stage { FromTime = 660f, ToTime = 620f, Radius = 90f, Mode = CentreMode.Random });
            plan.Add(new Stage { FromTime = 540f, ToTime = 500f, Radius = 50f, Mode = CentreMode.Random });

            return plan;
        }

        [Test]
        public void TheNextStageIsTheEarliestThatHasNotBegun()
        {
            ZonePlan plan = Plan();

            Assert.AreEqual(0, StageAdvance.NextIndex(plan, 900f));
            Assert.AreEqual(1, StageAdvance.NextIndex(plan, 700f));
            Assert.AreEqual(2, StageAdvance.NextIndex(plan, 600f));

            Assert.AreEqual(-1, StageAdvance.NextIndex(plan, 400f), "everything has started by then");
        }

        [Test]
        public void AdvancingStartsItNowAndKeepsItsDuration()
        {
            ZonePlan plan = Plan();

            Assert.IsTrue(StageAdvance.TryPlan(plan, 900f, out int index, out float delta));
            Assert.AreEqual(0, index);
            Assert.AreEqual(120f, delta, 0.001f);

            plan.ShiftFrom(index, delta);

            Assert.AreEqual(900f, plan.Stages[0].FromTime, 0.001f);
            Assert.AreEqual(860f, plan.Stages[0].ToTime, 0.001f, "the 40s close is preserved");
        }

        // The reason the whole tail moves: shifting one stage alone would eventually walk it into the next.
        [Test]
        public void EveryLaterStageMovesWithItSoTheGapsSurvive()
        {
            ZonePlan plan = Plan();

            StageAdvance.TryPlan(plan, 900f, out int index, out float delta);
            plan.ShiftFrom(index, delta);

            Assert.AreEqual(780f, plan.Stages[1].FromTime, 0.001f);
            Assert.AreEqual(740f, plan.Stages[1].ToTime, 0.001f);
            Assert.AreEqual(660f, plan.Stages[2].FromTime, 0.001f);

            // Gap between stage 0 ending and stage 1 starting, unchanged at 80s.
            Assert.AreEqual(80f, plan.Stages[0].ToTime - plan.Stages[1].FromTime, 0.001f);
        }

        [Test]
        public void TheOrderStillHoldsAfterAShift()
        {
            ZonePlan plan = Plan();

            StageAdvance.TryPlan(plan, 700f, out int index, out float delta);
            plan.ShiftFrom(index, delta);

            for (int i = 1; i < plan.Count; i++)
                Assert.Less(plan.Stages[i].FromTime, plan.Stages[i - 1].FromTime, $"stage {i} out of order");
        }

        // Called while a stage is mid-close, the next one is queued behind it rather than started on top of
        // it, because two stages sliding at once is a state the evaluator has no answer for.
        [Test]
        public void AStageStillClosingIsNotOverlapped()
        {
            ZonePlan plan = Plan();

            // 770 is inside stage 0's 780-740 close.
            Assert.IsTrue(StageAdvance.TryPlan(plan, 770f, out int index, out float delta));
            Assert.AreEqual(1, index);
            Assert.IsTrue(StageAdvance.WaitsForCurrent(plan, 770f, index));

            plan.ShiftFrom(index, delta);

            Assert.AreEqual(740f, plan.Stages[1].FromTime, 0.001f, "starts exactly as stage 0 finishes");
            Assert.LessOrEqual(plan.Stages[1].FromTime, plan.Stages[0].ToTime);
        }

        [Test]
        public void BetweenStagesItStartsAtOnce()
        {
            ZonePlan plan = Plan();

            Assert.IsTrue(StageAdvance.TryPlan(plan, 700f, out int index, out float delta));
            Assert.AreEqual(1, index);
            Assert.IsFalse(StageAdvance.WaitsForCurrent(plan, 700f, index));

            plan.ShiftFrom(index, delta);
            Assert.AreEqual(700f, plan.Stages[1].FromTime, 0.001f);
        }

        [Test]
        public void NothingLeftToAdvanceIsRefused()
        {
            ZonePlan plan = Plan();

            Assert.IsFalse(StageAdvance.TryPlan(plan, 400f, out int index, out _));
            Assert.AreEqual(-1, index);

            Assert.IsFalse(StageAdvance.TryPlan(new ZonePlan(), 900f, out _, out _));
        }

        // Nothing is ever pushed later in the round, so a shift cannot walk a stage past the round's end.
        [Test]
        public void ShiftingOnlyEverMovesStagesEarlier()
        {
            ZonePlan plan = Plan();
            float lastEnd = plan.Stages[plan.Count - 1].ToTime;

            StageAdvance.TryPlan(plan, 900f, out int index, out float delta);
            plan.ShiftFrom(index, delta);

            Assert.Greater(delta, 0f);
            Assert.Greater(plan.Stages[plan.Count - 1].ToTime, lastEnd, "the last stage now finishes sooner");
        }
    }
}
