using ClosingCircle.Domain;
using NUnit.Framework;
using UnityEngine;

namespace ClosingCircle.Tests
{
    [TestFixture]
    public class PlanWindowTests
    {
        // Three stages back to back: 540-480, 420-360, 300-240.
        private static ZonePlan MakePlan(params bool[] resolved)
        {
            var plan = new ZonePlan { StartRadius = 200f, StartCentre = Vector2.zero };

            var times = new[] { new Vector2(540f, 480f), new Vector2(420f, 360f), new Vector2(300f, 240f) };

            for (int i = 0; i < resolved.Length; i++)
                plan.Add(new Stage
                {
                    FromTime = times[i].x,
                    ToTime = times[i].y,
                    Radius = 100f - i * 20f,
                    Mode = CentreMode.Bisector,
                    Resolved = resolved[i]
                });

            return plan;
        }

        [Test]
        public void EverythingDecidedAndStillToComeIsShown()
        {
            PlanWindow.Visible(MakePlan(true, true, true), 600f, out int from, out int count);

            Assert.AreEqual(0, from);
            Assert.AreEqual(3, count);
        }

        [Test]
        public void AStageThatHasFinishedStopsBeingShown()
        {
            ZonePlan plan = MakePlan(true, true, true);

            // Mid way through the second stage, so only the first has finished.
            PlanWindow.Visible(plan, 400f, out int from, out int count);
            Assert.AreEqual(1, from);
            Assert.AreEqual(2, count);

            // Past every one of them.
            PlanWindow.Visible(plan, 100f, out from, out count);
            Assert.AreEqual(3, from);
            Assert.AreEqual(0, count);
        }

        // A stage is still closing until its ToTime, and where it is heading is exactly what you want drawn.
        [Test]
        public void TheStageCurrentlyClosingIsStillShown()
        {
            PlanWindow.Visible(MakePlan(true, true, true), 500f, out int from, out int count);

            Assert.AreEqual(0, from);
            Assert.AreEqual(3, count);
        }

        [Test]
        public void AnUndecidedStageAndEverythingAfterItAreLeftOut()
        {
            PlanWindow.Visible(MakePlan(true, false, true), 600f, out int from, out int count);

            Assert.AreEqual(0, from);
            Assert.AreEqual(1, count);
        }

        [Test]
        public void NothingDecidedShowsNothing()
        {
            PlanWindow.Visible(MakePlan(false, false), 600f, out int from, out int count);

            Assert.AreEqual(0, from);
            Assert.AreEqual(0, count);
        }

        [Test]
        public void AnEmptyOrMissingPlanIsNotAnError()
        {
            PlanWindow.Visible(MakePlan(), 600f, out int from, out int count);
            Assert.AreEqual(0, from);
            Assert.AreEqual(0, count);

            PlanWindow.Visible(null, 600f, out from, out count);
            Assert.AreEqual(0, count);
        }
    }
}
