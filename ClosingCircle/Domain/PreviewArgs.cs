using System;
using System.Globalization;

// What 'preview' accepts, kept pure so the panel's toggle and the typed command are answered by one parser.
// The panel invented 'on' and 'off' and the command never learned them, which shipped because the test pinned
// the string the panel sends without ever asking whether anything would accept it.

namespace ClosingCircle.Domain
{
    public static class PreviewArgs
    {
        public const float DefaultSeconds = 15f;
        public const float MaxSeconds = 300f;

        // 'on' outlasts any round, and the mod's statics are wiped at the next map change anyway, so this is
        // 'until turned off' without a second piece of state to say so.
        public const float HoldSeconds = 36000f;

        public static bool TryParse(string[] args, out float seconds, out string error)
        {
            seconds = DefaultSeconds;
            error = null;

            if (args == null || args.Length == 0) return true;

            string word = (args[0] ?? string.Empty).Trim();

            if (word.Equals("on", StringComparison.OrdinalIgnoreCase))
            {
                seconds = HoldSeconds;
                return true;
            }

            // Zero is the off switch the drawing side already understands: it sets its deadline to now, which
            // fails its own check on the very next frame.
            if (word.Equals("off", StringComparison.OrdinalIgnoreCase))
            {
                seconds = 0f;
                return true;
            }

            if (!float.TryParse(word, NumberStyles.Float, CultureInfo.InvariantCulture, out seconds) ||
                seconds <= 0f || seconds > MaxSeconds)
            {
                seconds = DefaultSeconds;
                error = $"seconds must be between 0 and {MaxSeconds:0}, or 'on' or 'off'.";
                return false;
            }

            return true;
        }
    }
}
