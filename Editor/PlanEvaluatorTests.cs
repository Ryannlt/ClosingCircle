using ClosingCircle.Domain;
using NUnit.Framework;
using UnityEngine;

namespace ClosingCircle.Tests
{
    [TestFixture]
    public class PlanEvaluatorTests
    {
        private ZonePlan _plan;

        [SetUp]
        public void Setup()
        {
            _plan = new ZonePlan { StartRadius = 500f, StartCentre = new Vector2(256f, 256f) };
        }

        private void AddStage(float from, float to, float radius, float x, float z) =>
            _plan.Add(new Stage { FromTime = from, ToTime = to, Radius = radius, Centre = new Vector2(x, z) });

        [Test]
        public void Evaluate_WithNoStagesHoldsTheStartState()
        {
            Assert.AreEqual(500f, PlanEvaluator.Evaluate(_plan, 900f).Radius, 0.001f);
            Assert.AreEqual(500f, PlanEvaluator.Evaluate(_plan, 0f).Radius, 0.001f);
        }

        [Test]
        public void Evaluate_BeforeTheFirstStageHoldsTheStartState()
        {
            AddStage(600f, 480f, 250f, 256f, 256f);

            Assert.AreEqual(500f, PlanEvaluator.Evaluate(_plan, 700f).Radius, 0.001f);
        }

        [Test]
        public void Evaluate_AtTheStartTimeHasNotMovedYet()
        {
            AddStage(600f, 480f, 250f, 256f, 256f);

            Assert.AreEqual(500f, PlanEvaluator.Evaluate(_plan, 600f).Radius, 0.001f);
        }

        [Test]
        public void Evaluate_AtTheEndTimeHasFullyArrived()
        {
            AddStage(600f, 480f, 250f, 256f, 256f);

            Assert.AreEqual(250f, PlanEvaluator.Evaluate(_plan, 480f).Radius, 0.001f);
        }

        [Test]
        public void Evaluate_HalfwayThroughAStageIsHalfwayBetweenTheTwoRadii()
        {
            AddStage(600f, 480f, 250f, 256f, 256f);

            Assert.AreEqual(375f, PlanEvaluator.Evaluate(_plan, 540f).Radius, 0.001f);
        }

        [Test]
        public void Evaluate_HoldsStillInTheGapBetweenStages()
        {
            AddStage(600f, 480f, 250f, 256f, 256f);
            AddStage(400f, 300f, 80f, 300f, 220f);

            ZoneSnapshot gap = PlanEvaluator.Evaluate(_plan, 440f);

            Assert.AreEqual(250f, gap.Radius, 0.001f);
            Assert.AreEqual(256f, gap.Centre.x, 0.001f);
        }

        [Test]
        public void Evaluate_PastTheLastStageHoldsItsEndState()
        {
            AddStage(600f, 480f, 250f, 256f, 256f);
            AddStage(400f, 300f, 80f, 300f, 220f);

            ZoneSnapshot settled = PlanEvaluator.Evaluate(_plan, 5f);

            Assert.AreEqual(80f, settled.Radius, 0.001f);
            Assert.AreEqual(300f, settled.Centre.x, 0.001f);
            Assert.AreEqual(220f, settled.Centre.y, 0.001f);
        }

        [Test]
        public void Evaluate_SecondStageStartsFromWhereTheFirstFinished()
        {
            AddStage(600f, 480f, 250f, 256f, 256f);
            AddStage(400f, 300f, 80f, 300f, 220f);

            // Halfway through the second stage, so halfway from 250 to 80 and from 256 to 300 across.
            ZoneSnapshot midway = PlanEvaluator.Evaluate(_plan, 350f);

            Assert.AreEqual(165f, midway.Radius, 0.001f);
            Assert.AreEqual(278f, midway.Centre.x, 0.001f);
        }

        [Test]
        public void Add_SortsStagesByTheRoundClockRegardlessOfConfigOrder()
        {
            AddStage(400f, 300f, 80f, 300f, 220f);
            AddStage(600f, 480f, 250f, 256f, 256f);

            Assert.AreEqual(600f, _plan.Stages[0].FromTime, 0.001f);
            Assert.AreEqual(250f, PlanEvaluator.Evaluate(_plan, 480f).Radius, 0.001f);
        }

        [Test]
        public void FinalRadius_IsWhateverTheLastStageLeavesBehind()
        {
            Assert.AreEqual(500f, _plan.FinalRadius, 0.001f);

            AddStage(600f, 480f, 250f, 256f, 256f);
            AddStage(400f, 300f, 80f, 300f, 220f);

            Assert.AreEqual(80f, _plan.FinalRadius, 0.001f);
        }
    }
}
