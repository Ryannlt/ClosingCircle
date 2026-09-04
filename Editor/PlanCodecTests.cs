using ClosingCircle.Domain;
using ClosingCircle.Sync;
using NUnit.Framework;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using UnityEngine;

namespace ClosingCircle.Tests
{
    [TestFixture]
    public class PlanCodecTests
    {
        private static Stage MakeStage(float from, float to, float radius, float x, float z,
                                      bool resolved = false, CentreMode mode = CentreMode.Fixed) =>
            new Stage
            {
                FromTime = from, ToTime = to, Radius = radius, Centre = new Vector2(x, z), Resolved = resolved,
                Mode = mode
            };

        private static ZoneState MakeState(int stageCount = 2)
        {
            var stages = new List<Stage>();
            // Mixed on purpose: the preview depends on telling a decided stage from an undecided one.
            if (stageCount > 0) stages.Add(MakeStage(540f, 480f, 100f, 0f, 0f, resolved: true));
            if (stageCount > 1) stages.Add(MakeStage(420f, 360f, 40f, 30f, 20f));
            for (int i = 2; i < stageCount; i++) stages.Add(MakeStage(300f - i, 200f - i, 30f - i, i, -i));

            return new ZoneState
            {
                Enabled = true,
                Solid = true,
                Hud = true,
                ForceDisplay = true,
                Sides = 64,
                Rotation = 15f,
                StartRadius = 200f,
                StartCentre = new Vector2(-12.5f, 7.25f),
                Stages = stages,
                ColorR = 255,
                ColorG = 60,
                ColorB = 60,
                OpacityPercent = 24f,
                Height = 40f,
                Fade = 0.6f
            };
        }

        [Test]
        public void RoundTrip_PreservesEveryField()
        {
            ZoneState original = MakeState();
            Assert.IsTrue(PlanCodec.TryDecode(PlanCodec.Encode(original), out ZoneState back));

            Assert.AreEqual(original.Enabled, back.Enabled);
            Assert.AreEqual(original.Solid, back.Solid);
            Assert.AreEqual(original.Hud, back.Hud);
            Assert.AreEqual(original.ForceDisplay, back.ForceDisplay);
            Assert.AreEqual(original.Sides, back.Sides);
            Assert.AreEqual(original.Rotation, back.Rotation, 0.001f);
            Assert.AreEqual(original.StartRadius, back.StartRadius, 0.001f);
            Assert.AreEqual(original.StartCentre.x, back.StartCentre.x, 0.001f);
            Assert.AreEqual(original.StartCentre.y, back.StartCentre.y, 0.001f);
            Assert.AreEqual(original.ColorR, back.ColorR);
            Assert.AreEqual(original.OpacityPercent, back.OpacityPercent, 0.001f);
            Assert.AreEqual(original.Height, back.Height, 0.001f);
            Assert.AreEqual(original.Fade, back.Fade, 0.001f);
            Assert.AreEqual(2, back.Stages.Count);
            Assert.AreEqual(40f, back.Stages[1].Radius, 0.001f);
            Assert.AreEqual(30f, back.Stages[1].Centre.x, 0.001f);
            Assert.IsTrue(back.Stages[0].Resolved);
            Assert.IsFalse(back.Stages[1].Resolved);
        }

        // Every field a stage carries has to survive, not just the ones something happens to read today. Mode
        // was left off the wire entirely, so every stage arrived as Fixed and the panel misreported the plan,
        // and a round trip that only checked times and radii had nothing to say about it.
        [Test]
        public void EveryCentreModeSurvivesTheRoundTrip()
        {
            var modes = new[] { CentreMode.Fixed, CentreMode.Bisector, CentreMode.Random, CentreMode.Players };

            foreach (CentreMode mode in modes)
            {
                ZoneState state = MakeState(1);

                Stage stage = state.Stages[0];
                stage.Mode = mode;
                state.Stages[0] = stage;

                Assert.IsTrue(PlanCodec.TryDecode(PlanCodec.Encode(state), out ZoneState back), mode.ToString());
                Assert.AreEqual(mode, back.Stages[0].Mode, $"{mode} did not survive the wire.");
            }
        }

        [Test]
        public void RoundTrip_SurvivesAPlanWithNoStages()
        {
            Assert.IsTrue(PlanCodec.TryDecode(PlanCodec.Encode(MakeState(0)), out ZoneState back));
            Assert.AreEqual(0, back.Stages.Count);
            Assert.AreEqual(200f, back.StartRadius, 0.001f);
        }

        [Test]
        public void Encode_NeverEmitsASpace()
        {
            // serverAdmin quietBroadcastMessage rejoins its arguments with single spaces, so a space in the
            // payload would survive one round trip and silently corrupt the next.
            string payload = PlanCodec.Encode(MakeState(6));
            Assert.IsFalse(payload.Contains(" "), payload);

            foreach (string chunk in PlanCodec.Chunk(payload, 3))
                Assert.IsFalse(chunk.Contains(" "), chunk);
        }

        [Test]
        public void Encode_UsesInvariantNumbersUnderACommaDecimalLocale()
        {
            CultureInfo previous = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
                string payload = PlanCodec.Encode(MakeState());

                Assert.IsTrue(payload.Contains("-12.5"), payload);
                Assert.IsTrue(PlanCodec.TryDecode(payload, out ZoneState back));
                Assert.AreEqual(-12.5f, back.StartCentre.x, 0.001f);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }

        [Test]
        public void TryDecode_RejectsGarbageAndWrongVersions()
        {
            Assert.IsFalse(PlanCodec.TryDecode(null, out _));
            Assert.IsFalse(PlanCodec.TryDecode("", out _));
            Assert.IsFalse(PlanCodec.TryDecode("banana", out _));
            Assert.IsFalse(PlanCodec.TryDecode("99|sh:64,0", out _));
        }

        [Test]
        public void TryDecode_IgnoresFieldsFromANewerBuild()
        {
            string payload = PlanCodec.Encode(MakeState()) + "|zz:1,2,3";
            Assert.IsTrue(PlanCodec.TryDecode(payload, out ZoneState back));
            Assert.AreEqual(64, back.Sides);
        }

        [Test]
        public void Chunk_SplitsLongPayloadsAndEachPartStaysWithinTheLimit()
        {
            string payload = PlanCodec.Encode(MakeState(12));
            string[] chunks = PlanCodec.Chunk(payload, 5);

            Assert.IsTrue(chunks.Length > 1, $"expected several chunks for {payload.Length} chars");

            foreach (string chunk in chunks)
            {
                Assert.IsTrue(PlanCodec.TryReadChunk(chunk, out int seq, out _, out int count, out string body));
                Assert.AreEqual(5, seq);
                Assert.AreEqual(chunks.Length, count);
                Assert.IsTrue(body.Length <= PlanCodec.MaxChunkLength);
            }
        }

        [Test]
        public void Reassemble_InOrder()
        {
            string payload = PlanCodec.Encode(MakeState(12));
            var reassembler = new PlanReassembler();
            string got = null;

            foreach (string chunk in PlanCodec.Chunk(payload, 1))
                if (reassembler.Accept(chunk, out string done)) got = done;

            Assert.AreEqual(payload, got);
        }

        [Test]
        public void Reassemble_OutOfOrder()
        {
            string payload = PlanCodec.Encode(MakeState(12));
            string[] chunks = PlanCodec.Chunk(payload, 1);
            var reassembler = new PlanReassembler();
            string got = null;

            for (int i = chunks.Length - 1; i >= 0; i--)
                if (reassembler.Accept(chunks[i], out string done)) got = done;

            Assert.AreEqual(payload, got);
        }

        [Test]
        public void Reassemble_NeverCompletesWhenAChunkIsMissing()
        {
            string[] chunks = PlanCodec.Chunk(PlanCodec.Encode(MakeState(12)), 1);
            var reassembler = new PlanReassembler();

            for (int i = 1; i < chunks.Length; i++)
                Assert.IsFalse(reassembler.Accept(chunks[i], out _));
        }

        [Test]
        public void Reassemble_ANewerPushAbandonsAHalfAssembledOlderOne()
        {
            string older = PlanCodec.Encode(MakeState(12));
            string newer = PlanCodec.Encode(MakeState(10));

            string[] oldChunks = PlanCodec.Chunk(older, 1);
            string[] newChunks = PlanCodec.Chunk(newer, 2);

            var reassembler = new PlanReassembler();
            reassembler.Accept(oldChunks[0], out _);

            string got = null;
            foreach (string chunk in newChunks)
                if (reassembler.Accept(chunk, out string done)) got = done;

            Assert.AreEqual(newer, got);
        }

        [Test]
        public void Reassemble_IgnoresAStragglerFromAnOlderPush()
        {
            string[] oldChunks = PlanCodec.Chunk(PlanCodec.Encode(MakeState(12)), 1);
            string[] newChunks = PlanCodec.Chunk(PlanCodec.Encode(MakeState(12)), 2);

            var reassembler = new PlanReassembler();
            reassembler.Accept(newChunks[0], out _);

            Assert.IsFalse(reassembler.Accept(oldChunks[1], out _));
        }

        [Test]
        public void TryReadChunk_RejectsAnythingWithoutOurMarker()
        {
            Assert.IsFalse(PlanCodec.TryReadChunk("hello everyone", out _, out _, out _, out _));
            Assert.IsFalse(PlanCodec.TryReadChunk("[CC]nonsense", out _, out _, out _, out _));
            Assert.IsFalse(PlanCodec.TryReadChunk(null, out _, out _, out _, out _));
        }

        [Test]
        public void PreviewSignal_RoundTripsAndIgnoresOtherMessages()
        {
            Assert.IsTrue(PreviewSignal.TryDecode(PreviewSignal.Encode(12.5f), out float seconds));
            Assert.AreEqual(12.5f, seconds, 0.001f);

            Assert.IsFalse(PreviewSignal.TryDecode("[CC]1.0/1:payload", out _));
            Assert.IsFalse(PreviewSignal.TryDecode("just chat", out _));
        }
    }
}
