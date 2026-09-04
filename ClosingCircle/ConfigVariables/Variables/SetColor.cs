using UnityEngine;

// Color:r,g,b with each channel 0 to 255. Opacity is its own variable.

namespace ClosingCircle.ConfigVariables
{
    public class SetColor : IConfigVariable
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.Color;

        public bool Validate(string value) => TryRead(value, out _);

        public void Execute(string value)
        {
            if (!TryRead(value, out Color color)) return;
            ZoneService.Color = color;
            ZoneService.MarkLookChanged();
        }

        private static bool TryRead(string value, out Color color)
        {
            color = Color.white;

            string[] parts = value?.Split(',');
            if (parts == null || parts.Length != 3) return false;

            var channels = new float[3];
            for (int i = 0; i < 3; i++)
            {
                if (!Parse.Float(parts[i], out float channel)) return false;
                channels[i] = Mathf.Clamp01(channel / 255f);
            }

            color = new Color(channels[0], channels[1], channels[2]);
            return true;
        }
    }
}
