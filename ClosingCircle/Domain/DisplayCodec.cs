using System.Globalization;
using UnityEngine;

// The portable form of a player's display settings, so a look can move to another PC or to a friend.
//
// Versioned like the zone's own wire format, so a string from a later build is refused outright rather than
// half read. Values are clamped on the way in as well as on the way out: a hand-edited string must not be able
// to smuggle the zone past the visibility floor.

namespace ClosingCircle.Domain
{
    public struct DisplaySettings
    {
        public Color Colour;

        // Percentages and metres, matching what the config and the panel show rather than the shader's units.
        public float OpacityPercent;
        public float Height;
        public float FadePercent;
        public float BlurPercent;
        public bool Hud;
    }

    public static class DisplayCodec
    {
        public const string Version = "CC1";

        // A zone nobody can see is a zone nobody can avoid, so the floor is part of the format rather than
        // something only the slider enforces.
        public const float MinOpacityPercent = 8f;
        public const float MinHeight = 5f;
        public const float MaxHeight = 120f;

        private const int Fields = 7;
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        public static string Encode(DisplaySettings settings)
        {
            var clamped = Clamp(settings);

            return string.Join("-", new[]
            {
                Version,
                Hex(clamped.Colour),
                Whole(clamped.OpacityPercent),
                Whole(clamped.Height),
                Whole(clamped.FadePercent),
                Whole(clamped.BlurPercent),
                clamped.Hud ? "1" : "0"
            });
        }

        public static bool TryDecode(string text, out DisplaySettings settings, out string problem)
        {
            settings = default(DisplaySettings);
            problem = null;

            if (string.IsNullOrEmpty(text))
            {
                problem = "Paste a settings string first.";
                return false;
            }

            string[] parts = text.Trim().Split('-');

            if (parts.Length != Fields || parts[0] != Version)
            {
                problem = "That is not a settings string this build understands.";
                return false;
            }

            if (!TryHex(parts[1], out Color colour))
            {
                problem = "The colour in that string is malformed.";
                return false;
            }

            var numbers = new float[4];
            for (int i = 0; i < numbers.Length; i++)
            {
                if (float.TryParse(parts[i + 2], NumberStyles.Float, Invariant, out numbers[i])) continue;

                problem = "That string has a number missing.";
                return false;
            }

            settings = Clamp(new DisplaySettings
            {
                Colour = colour,
                OpacityPercent = numbers[0],
                Height = numbers[1],
                FadePercent = numbers[2],
                BlurPercent = numbers[3],
                Hud = parts[6] == "1"
            });

            return true;
        }

        public static DisplaySettings Clamp(DisplaySettings settings)
        {
            settings.OpacityPercent = Mathf.Clamp(settings.OpacityPercent, MinOpacityPercent, 100f);
            settings.Height = Mathf.Clamp(settings.Height, MinHeight, MaxHeight);
            settings.FadePercent = Mathf.Clamp(settings.FadePercent, 0f, 100f);
            settings.BlurPercent = Mathf.Clamp(settings.BlurPercent, 0f, 100f);

            settings.Colour = new Color(Mathf.Clamp01(settings.Colour.r), Mathf.Clamp01(settings.Colour.g),
                                        Mathf.Clamp01(settings.Colour.b));

            return settings;
        }

        private static string Hex(Color colour) =>
            Byte(colour.r).ToString("x2", Invariant) +
            Byte(colour.g).ToString("x2", Invariant) +
            Byte(colour.b).ToString("x2", Invariant);

        private static bool TryHex(string text, out Color colour)
        {
            colour = Color.white;
            if (text == null || text.Length != 6) return false;

            var channels = new int[3];
            for (int i = 0; i < 3; i++)
            {
                if (!int.TryParse(text.Substring(i * 2, 2), NumberStyles.HexNumber, Invariant, out channels[i]))
                    return false;
            }

            colour = new Color(channels[0] / 255f, channels[1] / 255f, channels[2] / 255f);
            return true;
        }

        private static int Byte(float unit) => Mathf.Clamp(Mathf.RoundToInt(unit * 255f), 0, 255);

        private static string Whole(float value) =>
            Mathf.RoundToInt(value).ToString(Invariant);
    }
}
