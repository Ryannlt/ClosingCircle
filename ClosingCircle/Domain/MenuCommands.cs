using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

// Every string the panel can send. Pure, so "the Add Stage button sends the right command" is a test rather
// than something only a live round can answer, and the panel itself never assembles a command by hand.
//
// The panel gains no authority from this: each of these is a line an admin could already type.

namespace ClosingCircle.Domain
{
    public static class MenuCommands
    {
        public const string Prefix = "rc closingCircle";

        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        public static string Set(string key, string value) => $"{Prefix} set {key} {value}";

        public static string Set(string key, bool value) => Set(key, value ? "true" : "false");

        public static string Set(string key, float value) => Set(key, N(value));

        // Several settings in one command, which is what the panel's Apply sends: one line on the wire and
        // one answer back, rather than one of each per changed setting.
        public static string Set(IEnumerable<KeyValuePair<string, string>> pairs)
        {
            var text = new StringBuilder(Prefix).Append(" set");

            foreach (KeyValuePair<string, string> pair in pairs)
                text.Append(' ').Append(pair.Key).Append(' ').Append(pair.Value);

            return text.ToString();
        }

        // The value half of a Set, formatted exactly as the single-key overloads would.
        public static string Value(object value)
        {
            if (value is bool flag) return flag ? "true" : "false";
            if (value is float number) return N(number);

            return value == null ? string.Empty : value.ToString();
        }

        // Colour is three 0-255 channels, the way the config writes it.
        public static string SetColour(Color colour) =>
            Set("Color", $"{Channel(colour.r)},{Channel(colour.g)},{Channel(colour.b)}");

        public static string SetPoint(string key, Vector2 point) => Set(key, $"{N(point.x)},{N(point.y)}");

        public static string SetBisector(Vector2 point, float heading) =>
            Set("Bisector", $"{N(point.x)},{N(point.y)},{N(heading)}");

        public static string AddStage(float from, float to, float radius, CentreMode mode) =>
            $"{Prefix} stage add {N(from)} {N(to)} {N(radius)} {mode}";

        // A written centre replaces the mode rather than sitting beside it, so these two overloads are the
        // whole vocabulary.
        public static string AddStage(float from, float to, float radius, Vector2 centre) =>
            $"{Prefix} stage add {N(from)} {N(to)} {N(radius)} {N(centre.x)} {N(centre.y)}";

        public static string RemoveStage(int index) => $"{Prefix} stage remove {index}";

        public static string ClearStages() => $"{Prefix} stage clear";

        public static string AdvanceStage() => $"{Prefix} stage next";

        public static string ListStages() => $"{Prefix} stage list";

        public static string Preview(float seconds) => $"{Prefix} preview {N(seconds)}";

        public static string Preview(bool on) => $"{Prefix} preview {(on ? "on" : "off")}";

        public static string Validate() => $"{Prefix} validate";

        public static string Status() => $"{Prefix} status";

        public static string Push() => $"{Prefix} push";

        // The admin probe. Read-only by design: its whole job is to find out whether the server answers us.
        public static string Whoami() => $"{Prefix} whoami";

        public static string Login(string password) => $"rc login {password}";

        private static int Channel(float unit) => Mathf.Clamp(Mathf.RoundToInt(unit * 255f), 0, 255);

        // Invariant everywhere, because a client running a comma-decimal locale would otherwise send a comma
        // into a comma-separated argument.
        private static string N(float value) => value.ToString("0.###", Invariant);
    }
}
