using TMPro;
using UnityEngine;

// Holdfast's own UI face, borrowed off a live label. A mod ships no font, and the alternative is a typeface
// that looks nothing like the rest of the HUD.
//
// Assigns only the font asset, never the shared material: taking the material as well would inherit whatever
// colour and outline that label happened to have.

namespace ClosingCircle.Visual
{
    public static class GameFont
    {
        // The search is not cheap and the game's UI may not be up when the mod first ticks, so it is retried on
        // a timer rather than per frame.
        private const float RetrySeconds = 1f;

        private static TMP_FontAsset _font;
        private static float _nextAttemptAt;

        public static TMP_FontAsset Current => _font;

        public static void Reset()
        {
            _font = null;
            _nextAttemptAt = 0f;
        }

        // Returns true on the tick it is first found, so a caller knows to apply it to labels already built.
        public static bool Find()
        {
            if (_font != null || Time.time < _nextAttemptAt) return false;

            _nextAttemptAt = Time.time + RetrySeconds;

            TextMeshProUGUI[] labels = Object.FindObjectsOfType<TextMeshProUGUI>();

            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] == null || labels[i].font == null) continue;

                // Ours are named so they can be skipped; borrowing our own font back would find nothing.
                if (labels[i].name.StartsWith(Mine)) continue;

                _font = labels[i].font;
                Logger.Log($"Adopted the game font '{_font.name}'.", LogLevel.DEBUG);
                return true;
            }

            return false;
        }

        public const string Mine = "ClosingCircle";

        public static void Apply(TextMeshProUGUI label)
        {
            if (label == null || _font == null) return;

            label.font = _font;
        }
    }
}
