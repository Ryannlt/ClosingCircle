using ClosingCircle.Domain;
using NUnit.Framework;
using UnityEngine;

namespace ClosingCircle.Tests
{
    [TestFixture]
    public class DisplayCodecTests
    {
        private static DisplaySettings Sample => new DisplaySettings
        {
            Colour = new Color(1f, 60f / 255f, 60f / 255f),
            OpacityPercent = 24f,
            Height = 40f,
            FadePercent = 60f,
            BlurPercent = 35f,
            Hud = true
        };

        [Test]
        public void EncodesToSomethingShortEnoughToPasteInChat()
        {
            Assert.AreEqual("CC1-ff3c3c-24-40-60-35-1", DisplayCodec.Encode(Sample));
        }

        [Test]
        public void RoundTripsEveryField()
        {
            Assert.IsTrue(DisplayCodec.TryDecode(DisplayCodec.Encode(Sample), out DisplaySettings back, out _));

            Assert.AreEqual(Sample.Colour.r, back.Colour.r, 0.005f);
            Assert.AreEqual(Sample.Colour.g, back.Colour.g, 0.005f);
            Assert.AreEqual(Sample.Colour.b, back.Colour.b, 0.005f);
            Assert.AreEqual(24f, back.OpacityPercent, 0.001f);
            Assert.AreEqual(40f, back.Height, 0.001f);
            Assert.AreEqual(60f, back.FadePercent, 0.001f);
            Assert.AreEqual(35f, back.BlurPercent, 0.001f);
            Assert.IsTrue(back.Hud);
        }

        [Test]
        public void HudOffSurvivesTheRoundTrip()
        {
            DisplaySettings off = Sample;
            off.Hud = false;

            Assert.IsTrue(DisplayCodec.TryDecode(DisplayCodec.Encode(off), out DisplaySettings back, out _));
            Assert.IsFalse(back.Hud);
        }

        // The visibility floor belongs to the format, not just the slider, or a hand-edited string walks past it.
        [Test]
        public void AHandEditedStringCannotHideTheZone()
        {
            Assert.IsTrue(DisplayCodec.TryDecode("CC1-ff3c3c-0-40-60-0-1", out DisplaySettings back, out _));
            Assert.AreEqual(DisplayCodec.MinOpacityPercent, back.OpacityPercent, 0.001f);

            Assert.IsTrue(DisplayCodec.TryDecode("CC1-ff3c3c-24-0-60-0-1", out DisplaySettings squashed, out _));
            Assert.AreEqual(DisplayCodec.MinHeight, squashed.Height, 0.001f);
        }

        [Test]
        public void EncodingClampsTooSoNothingOutOfRangeIsEverHandedOut()
        {
            DisplaySettings silly = Sample;
            silly.OpacityPercent = 0f;
            silly.BlurPercent = 900f;

            Assert.AreEqual("CC1-ff3c3c-8-40-60-100-1", DisplayCodec.Encode(silly));
        }

        // Each refusal says which thing was wrong, because "invalid" tells somebody nothing.
        [Test]
        public void RefusalsAreToldApart()
        {
            Assert.IsFalse(DisplayCodec.TryDecode(null, out _, out string empty));
            Assert.IsTrue(empty.Contains("Paste"), empty);

            Assert.IsFalse(DisplayCodec.TryDecode("CC9-ff3c3c-24-40-60-0-1", out _, out string version));
            Assert.IsTrue(version.Contains("build"), version);

            Assert.IsFalse(DisplayCodec.TryDecode("CC1-ff3c3c-24-40-60-0", out _, out string missing));
            Assert.IsTrue(missing.Contains("build"), missing);

            Assert.IsFalse(DisplayCodec.TryDecode("CC1-nothex-24-40-60-0-1", out _, out string colour));
            Assert.IsTrue(colour.Contains("colour"), colour);

            Assert.IsFalse(DisplayCodec.TryDecode("CC1-ff3c3c-24-xx-60-0-1", out _, out string number));
            Assert.IsTrue(number.Contains("number"), number);
        }

        [Test]
        public void WhitespaceAroundAPastedStringIsForgiven()
        {
            Assert.IsTrue(DisplayCodec.TryDecode("  CC1-ff3c3c-24-40-60-35-1\n", out DisplaySettings back, out _));
            Assert.AreEqual(35f, back.BlurPercent, 0.001f);
        }
    }
}
