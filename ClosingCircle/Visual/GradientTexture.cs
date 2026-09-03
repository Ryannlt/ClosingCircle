using UnityEngine;

// The fade, as a one pixel wide vertical ramp generated in code. Doing it in the texture rather than a shader
// is what keeps color, opacity, height and fade adjustable without shipping a single asset.

namespace ClosingCircle.Visual
{
    public static class GradientTexture
    {
        private const int Steps = 64;

        // fade is the share of the visible height the wall takes to disappear, measured down from the top.
        public static Texture2D Build(float fade)
        {
            var texture = new Texture2D(1, Steps, TextureFormat.RGBA32, mipChain: false)
            {
                name = "ClosingCircle_Fade",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            float solidUntil = Mathf.Clamp01(1f - Mathf.Clamp01(fade));

            for (int y = 0; y < Steps; y++)
            {
                float t = (float)y / (Steps - 1);
                float alpha = t <= solidUntil
                    ? 1f
                    : 1f - Mathf.InverseLerp(solidUntil, 1f, t);

                texture.SetPixel(0, y, new Color(1f, 1f, 1f, alpha));
            }

            texture.Apply();
            return texture;
        }
    }
}
