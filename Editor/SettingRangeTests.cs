using ClosingCircle.Domain;
using NUnit.Framework;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// The round trip the panel was missing. MenuCommandsTests pinned the string the panel sends and nothing asked
// whether the receiving end would accept it, which is how 'preview on' and a 0-100 Spread both shipped green.

namespace ClosingCircle.Tests
{
    [TestFixture]
    public class SettingRangeTests
    {
        // The invariant that makes a slider safe: both ends of what the panel offers are legal on the wire.
        [Test]
        public void EverySliderStaysInsideTheRangeThatGuardsIt()
        {
            foreach (string key in SettingRanges.Keys)
            {
                SettingRange range = SettingRanges.Get(key);

                Assert.IsTrue(range.Holds(range.Wire(range.SliderMin)),
                              $"{key}: slider minimum {range.SliderMin} is not a legal value.");

                Assert.IsTrue(range.Holds(range.Wire(range.SliderMax)),
                              $"{key}: slider maximum {range.SliderMax} is not a legal value.");
            }
        }

        // Walking the slider and checking what would actually be sent. This is the test that fails on the
        // Spread bug: the panel showed 0-100 and the rule wanted 0-1.
        [Test]
        public void EveryPositionOnEverySliderSendsSomethingLegal()
        {
            foreach (string key in SettingRanges.Keys)
            {
                SettingRange range = SettingRanges.Get(key);

                for (int step = 0; step <= 20; step++)
                {
                    float shown = range.SliderMin + (range.SliderMax - range.SliderMin) * (step / 20f);
                    if (range.Whole) shown = Mathf.Round(shown);

                    float wire = range.Wire(shown);

                    Assert.IsTrue(range.Holds(wire),
                                  $"{key}: the panel would send {wire} for a slider at {shown}.");
                }
            }
        }

        [Test]
        public void SpreadAndFadeAreSentAsFractionsNotPercentages()
        {
            SettingRange spread = SettingRanges.Get("Spread");

            Assert.AreEqual(0.8f, spread.Wire(80f), 0.0001f);
            Assert.AreEqual(80f, spread.Shown(0.8f), 0.0001f);

            Assert.IsFalse(spread.Holds(80f), "80 is a percentage and must not pass as a fraction.");
            Assert.IsTrue(spread.Holds(0.8f));
        }

        [Test]
        public void KeysThatMustBeAboveZeroRejectZero()
        {
            Assert.IsFalse(SettingRanges.Get("Damage").Holds(0f));
            Assert.IsFalse(SettingRanges.Get("StartRadius").Holds(0f));
            Assert.IsFalse(SettingRanges.Get("Height").Holds(0f));

            Assert.IsTrue(SettingRanges.Get("RepeatSeconds").Holds(0f), "0 means one hit per crossing.");
        }

        [Test]
        public void DamageIsWholeNumbersOnly()
        {
            SettingRange damage = SettingRanges.Get("Damage");

            Assert.IsTrue(damage.Holds(150f));
            Assert.IsFalse(damage.Holds(150.5f));
        }

        [Test]
        public void AnUnknownKeyAcceptsAnythingRatherThanBlockingIt()
        {
            Assert.IsTrue(SettingRanges.Get("Bisector").Holds(12.5f));
        }

        /* ---------------- batched set ---------------- */

        // The panel's Apply sends one command for the whole batch, so the reader has to see it as pairs.
        [Test]
        public void ABatchedSetIsReadBackAsPairs()
        {
            var pairs = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Opacity", "40"),
                new KeyValuePair<string, string>("Damage", "200"),
                new KeyValuePair<string, string>("Spread", "0.8")
            };

            string command = MenuCommands.Set(pairs);
            Assert.AreEqual("rc closingCircle set Opacity 40 Damage 200 Spread 0.8", command);

            Assert.IsTrue(SetArgs.LooksLikePairs(Tail(command), Known));
        }

        // The case the whole heuristic exists for: a typed value split by stray spaces must NOT be mistaken
        // for pairs, or 'set Bisector 0, 0, 90' would be read as three settings.
        [Test]
        public void ASplitValueIsNotMistakenForPairs()
        {
            var args = new[] { "Bisector", "0,", "0,", "90" };

            Assert.IsFalse(SetArgs.LooksLikePairs(args, Known));
            Assert.AreEqual("0,0,90", SetArgs.JoinValue(args));
        }

        [Test]
        public void OnePairIsLeftToTheSingleValuePath()
        {
            Assert.IsFalse(SetArgs.LooksLikePairs(new[] { "Opacity", "40" }, Known));
            Assert.IsFalse(SetArgs.LooksLikePairs(new[] { "Opacity", "40", "Damage" }, Known), "odd count");
        }

        [Test]
        public void ValueFormattingMatchesTheSingleKeyOverloads()
        {
            Assert.AreEqual("true", MenuCommands.Value(true));
            Assert.AreEqual("false", MenuCommands.Value(false));
            Assert.AreEqual("0.8", MenuCommands.Value(0.8f));
            Assert.AreEqual("Hexagon", MenuCommands.Value("Hexagon"));
        }

        // Every key the panel can stage has to be one the reader recognises, or a batch containing it would
        // silently fall through to the single-value path and be applied as nonsense.
        [Test]
        public void EveryRangedKeyIsRecognisedAsAKey()
        {
            foreach (string key in SettingRanges.Keys) Assert.IsTrue(Known(key), key);
        }

        private static readonly string[] ConfigKeys =
        {
            "EnableCircle", "EnableDebugLogging", "Shape", "Rotation", "StartRadius", "StartCentre",
            "AddStage", "Damage", "RepeatSeconds", "Solid", "Hud", "Color", "Opacity", "Height", "Fade",
            "Blur", "Announce", "Bisector", "Spread"
        };

        private static bool Known(string key)
        {
            foreach (string candidate in ConfigKeys)
                if (string.Equals(candidate, key, System.StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }

        // Everything after 'rc closingCircle set', which is what the handler passes a command as its args.
        private static string[] Tail(string command)
        {
            string[] parts = command.Split(' ');
            var tail = new string[parts.Length - 3];
            System.Array.Copy(parts, 3, tail, 0, tail.Length);
            return tail;
        }

        /* ---------------- preview ---------------- */

        [Test]
        public void PreviewAcceptsEveryStringThePanelCanSend()
        {
            AssertPreviewAccepts(MenuCommands.Preview(true), PreviewArgs.HoldSeconds);
            AssertPreviewAccepts(MenuCommands.Preview(false), 0f);
            AssertPreviewAccepts(MenuCommands.Preview(60f), 60f);
        }

        [Test]
        public void PreviewWithNoArgumentUsesTheDefault()
        {
            Assert.IsTrue(PreviewArgs.TryParse(new string[0], out float seconds, out string error));
            Assert.IsNull(error);
            Assert.AreEqual(PreviewArgs.DefaultSeconds, seconds, 0.0001f);
        }

        [Test]
        public void PreviewStillRefusesNonsense()
        {
            Assert.IsFalse(PreviewArgs.TryParse(new[] { "banana" }, out _, out string error));
            Assert.IsNotNull(error);

            Assert.IsFalse(PreviewArgs.TryParse(new[] { "0" }, out _, out _));
            Assert.IsFalse(PreviewArgs.TryParse(new[] { "-5" }, out _, out _));
            Assert.IsFalse(PreviewArgs.TryParse(new[] { "301" }, out _, out _));
        }

        [Test]
        public void PreviewOnOffAreCaseInsensitive()
        {
            Assert.IsTrue(PreviewArgs.TryParse(new[] { "ON" }, out float on, out _));
            Assert.AreEqual(PreviewArgs.HoldSeconds, on, 0.0001f);

            Assert.IsTrue(PreviewArgs.TryParse(new[] { "Off" }, out float off, out _));
            Assert.AreEqual(0f, off, 0.0001f);
        }

        // Takes the whole command the panel would send and feeds the tail to the parser, so the argument split
        // is part of what is being checked rather than assumed.
        private static void AssertPreviewAccepts(string command, float expected)
        {
            string[] parts = command.Split(' ');
            var args = new string[parts.Length - 3];
            System.Array.Copy(parts, 3, args, 0, args.Length);

            Assert.IsTrue(PreviewArgs.TryParse(args, out float seconds, out string error),
                          $"'{command}' was refused: {error}");

            Assert.AreEqual(expected, seconds, 0.0001f,
                            $"'{command}' parsed to {seconds.ToString(CultureInfo.InvariantCulture)}.");
        }
    }
}
