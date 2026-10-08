using ClosingCircle.ConfigVariables;
using ClosingCircle.Domain;
using NUnit.Framework;
using UnityEngine;

namespace ClosingCircle.Tests
{
    [TestFixture]
    public class SpawnFrameTests
    {
        [SetUp]
        public void Clean()
        {
            ZoneService.ResetRoundClock();
            ZoneService.StartFromSpawns = false;
            ZoneService.BisectorFromSpawns = false;
            ZoneService.HasBisector = false;
            ZoneService.Plan.StartCenter = Vector2.zero;
        }

        [Test]
        public void Heading_IsCenterMathDirectionRunBackwards()
        {
            foreach (float heading in new[] { 0f, 30f, 90f, 135f, 180f, 225f, 300f, 359f })
                Assert.AreEqual(heading, SpawnFrame.Heading(CenterMath.Direction(heading)), 0.01f);
        }

        [Test]
        public void FairLine_IsAQuarterTurnFromTheSpawnAxis()
        {
            // Spawns north and south of each other face off across an east-west line.
            Assert.AreEqual(90f, SpawnFrame.FairLineHeading(new Vector2(0f, 1f)), 0.01f);

            // Spawns east and west face off across a north-south line, which reads as 0 or 180.
            float across = SpawnFrame.FairLineHeading(new Vector2(1f, 0f));
            Assert.IsTrue(Mathf.Abs(across - 180f) < 0.01f || across < 0.01f, $"was {across}");
        }

        [Test]
        public void FairLine_IsTheSameLineWhicheverWayTheAxisPoints()
        {
            var axis = new Vector2(0.6f, -0.8f);
            float one = SpawnFrame.FairLineHeading(axis);
            float other = SpawnFrame.FairLineHeading(-axis);
            Assert.AreEqual(180f, Mathf.Abs(one - other), 0.01f);
        }

        [Test]
        public void FairLine_HeadingsStayBetweenZeroAnd360()
        {
            foreach (Vector2 axis in new[] { new Vector2(-1f, 0f), new Vector2(-0.3f, -0.9f), new Vector2(0.2f, 0.98f) })
            {
                float heading = SpawnFrame.FairLineHeading(axis);
                Assert.GreaterOrEqual(heading, 0f);
                Assert.Less(heading, 360f);
            }
        }

        [Test]
        public void FairLine_KeepsTwoSpawnsEquidistant()
        {
            var center = new Vector2(120f, -40f);
            Vector2 axis = new Vector2(1f, 2f).normalized;
            Vector2 attacking = center + axis * 150f;
            Vector2 defending = center - axis * 150f;
            Vector2 direction = CenterMath.Direction(SpawnFrame.FairLineHeading(axis));

            foreach (float t in new[] { -200f, -35f, 0f, 60f, 310f })
            {
                Vector2 point = CenterMath.PointOnLine(center, direction, t);
                Assert.AreEqual(Vector2.Distance(point, attacking), Vector2.Distance(point, defending), 0.01f);
            }
        }

        [Test]
        public void StartCenter_TakesCustomSpawnsInAnyCaseOrNumbers()
        {
            var setting = new SetStartCenter();
            Assert.IsTrue(setting.Validate("CustomSpawns"));
            Assert.IsTrue(setting.Validate(" customspawns "));
            Assert.IsTrue(setting.Validate("10,-20"));
            Assert.IsFalse(setting.Validate("Spawns"));
            Assert.IsFalse(setting.Validate("10"));
        }

        [Test]
        public void StartCenter_CustomSpawnsWaitsForTheRoundAndNumbersTakeOver()
        {
            var setting = new SetStartCenter();
            setting.Execute("CustomSpawns");
            Assert.IsTrue(ZoneService.StartFromSpawns);
            Assert.AreEqual(0f, ZoneService.Plan.StartCenter.magnitude, 0.001f);

            setting.Execute("10,-20");
            Assert.IsFalse(ZoneService.StartFromSpawns);
            Assert.AreEqual(10f, ZoneService.Plan.StartCenter.x, 0.001f);
            Assert.AreEqual(-20f, ZoneService.Plan.StartCenter.y, 0.001f);
        }

        [Test]
        public void Bisector_TakesCustomSpawnsAndNumbersTakeOver()
        {
            var setting = new SetBisector();
            Assert.IsTrue(setting.Validate("CUSTOMSPAWNS"));
            Assert.IsTrue(setting.Validate("0,0,45"));
            Assert.IsFalse(setting.Validate("0,0"));

            setting.Execute("CustomSpawns");
            Assert.IsTrue(ZoneService.BisectorFromSpawns);
            Assert.IsFalse(ZoneService.HasBisector);

            setting.Execute("5,6,45");
            Assert.IsFalse(ZoneService.BisectorFromSpawns);
            Assert.IsTrue(ZoneService.HasBisector);
            Assert.AreEqual(45f, ZoneService.BisectorHeading, 0.001f);
        }
    }
}
