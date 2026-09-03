using System.Globalization;

// A one-off instruction to the admin who asked for it, kept apart from the plan format because it carries no
// state and must never be mistaken for a chunk of a plan.

namespace ClosingCircle.Sync
{
    public static class PreviewSignal
    {
        public const string Marker = "[CCP]";

        public static string Encode(float seconds) =>
            Marker + seconds.ToString("0.##", CultureInfo.InvariantCulture);

        public static bool TryDecode(string message, out float seconds)
        {
            seconds = 0f;

            if (string.IsNullOrEmpty(message) || !message.StartsWith(Marker)) return false;

            return float.TryParse(message.Substring(Marker.Length), NumberStyles.Float,
                                  CultureInfo.InvariantCulture, out seconds);
        }
    }
}
