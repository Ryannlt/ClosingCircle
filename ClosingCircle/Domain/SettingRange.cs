using System;
using System.Collections.Generic;

// One definition of what a numeric setting will accept, read by both the validator that guards it and the
// panel that offers it. The panel therefore cannot present a value the server refuses, which is the bug this
// exists to make impossible rather than to fix once.
//
// FairLine already sets this pattern: the guard and the validator both call it, so they cannot disagree about
// what fair means. The same reasoning applies to a slider and the rule behind it.

namespace ClosingCircle.Domain
{
    public struct SettingRange
    {
        // The legal range on the wire, which is what a config file and an rc command are held to.
        public float Min;
        public float Max;
        public bool MinExclusive;
        public bool Whole;

        // What a slider offers, in shown units, so Wire() of either end must still be legal. Separate from the
        // range above because most keys are unbounded and a slider cannot be.
        public float SliderMin;
        public float SliderMax;

        // Multiply a wire value by this to show it. Spread and Fade are 0-1 on the wire but read as
        // percentages, and sending the percentage is exactly the mistake this removes.
        public float Scale;

        public bool Holds(float value)
        {
            if (float.IsNaN(value)) return false;
            if (MinExclusive ? value <= Min : value < Min) return false;
            if (value > Max) return false;

            return !Whole || Math.Abs(value - (float)Math.Round(value)) < 0.0001f;
        }

        public float Shown(float wire) => wire * Scale;

        public float Wire(float shown) => Scale == 0f ? shown : shown / Scale;
    }

    public static class SettingRanges
    {
        private const float Open = float.PositiveInfinity;

        private static readonly Dictionary<string, SettingRange> Known =
            new Dictionary<string, SettingRange>(StringComparer.OrdinalIgnoreCase)
            {
                { "StartRadius", Make(0f, Open, 20f, 600f, exclusive: true) },
                { "Damage", Make(0f, Open, 1f, 500f, exclusive: true, whole: true) },
                { "RepeatSeconds", Make(0f, Open, 0f, 30f) },
                { "Height", Make(0f, Open, 5f, 120f, exclusive: true) },
                { "Opacity", Make(0f, 100f, 0f, 100f) },
                { "Blur", Make(0f, 100f, 0f, 100f) },
                { "Rotation", Make(-Open, Open, 0f, 360f) },

                // Stored 0-1, shown 0-100.
                { "Fade", Make(0f, 1f, 0f, 100f, scale: 100f) },
                { "Spread", Make(0f, 1f, 0f, 100f, scale: 100f) }
            };

        public static bool TryGet(string key, out SettingRange range) => Known.TryGetValue(key, out range);

        public static SettingRange Get(string key)
        {
            SettingRange range;
            if (TryGet(key, out range)) return range;

            return Make(-Open, Open, 0f, 1f);
        }

        // Every key with a range, so a test can walk them rather than listing them again and drifting.
        public static IEnumerable<string> Keys => Known.Keys;

        private static SettingRange Make(float min, float max, float sliderMin, float sliderMax,
                                         bool exclusive = false, bool whole = false, float scale = 1f) =>
            new SettingRange
            {
                Min = min,
                Max = max,
                MinExclusive = exclusive,
                Whole = whole,
                SliderMin = sliderMin,
                SliderMax = sliderMax,
                Scale = scale
            };
    }
}
