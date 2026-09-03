using System.Globalization;
using UnityEngine;

// Every number in a config line is read through here. Invariant culture is not optional: a server whose locale
// uses a comma decimal separator would otherwise silently reject or mis-read every float.

namespace ClosingCircle.ConfigVariables
{
    public static class Parse
    {
        private const NumberStyles FloatStyle = NumberStyles.Float;

        public static bool Float(string value, out float result) =>
            float.TryParse(value?.Trim(), FloatStyle, CultureInfo.InvariantCulture, out result);

        public static bool Int(string value, out int result) =>
            int.TryParse(value?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result);

        public static bool Bool(string value, out bool result) =>
            bool.TryParse(value?.Trim(), out result);

        public static bool Vector2(string value, out Vector2 result)
        {
            result = UnityEngine.Vector2.zero;

            string[] parts = value?.Split(',');
            if (parts == null || parts.Length != 2) return false;
            if (!Float(parts[0], out float x) || !Float(parts[1], out float z)) return false;

            result = new Vector2(x, z);
            return true;
        }
    }
}
