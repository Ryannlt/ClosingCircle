using UnityEngine;

// Finds a usable shader and dresses it for transparency. Shader availability in a shipped build is the one
// thing here that cannot be settled by reading code, so the chain is ordered by preference and logs its answer.

namespace ClosingCircle.Visual
{
    public static class ZoneMaterial
    {
        private static readonly string[] Candidates =
        {
            "Universal Render Pipeline/Unlit",
            "Universal Render Pipeline/Lit",
            "Unlit/Transparent",
            "Sprites/Default",
            "Unlit/Texture"
        };

        private const int TransparentQueue = 3000;

        public static Material Build(Texture2D ramp, Color color)
        {
            Shader shader = Resolve();
            if (shader == null)
            {
                Logger.Log("No usable shader was found, so the zone will be invisible. Damage is unaffected.",
                           LogLevel.ERROR);
                return null;
            }

            var material = new Material(shader) { name = "ClosingCircle_Wall" };
            MakeTransparent(material);
            Apply(material, ramp, color);
            return material;
        }

        public static void Apply(Material material, Texture2D ramp, Color color)
        {
            if (material == null) return;

            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", ramp);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", ramp);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        }

        // Slides the alpha ramp so its zero point sits at ground level whatever the terrain is doing.
        public static void SetRamp(Material material, float scaleY, float offsetY)
        {
            if (material == null) return;

            var st = new Vector4(1f, scaleY, 0f, offsetY);
            if (material.HasProperty("_BaseMap_ST")) material.SetVector("_BaseMap_ST", st);
            if (material.HasProperty("_MainTex_ST")) material.SetVector("_MainTex_ST", st);
        }

        private static Shader Resolve()
        {
            foreach (string name in Candidates)
            {
                Shader shader = Shader.Find(name);
                if (shader == null) continue;

                Logger.Log($"Zone wall is using shader '{name}'.", LogLevel.INFO);
                return shader;
            }

            return Scavenge();
        }

        // Last resort for a build that stripped every shader we asked for by name: borrow one already loaded.
        private static Shader Scavenge()
        {
            Material[] loaded = Resources.FindObjectsOfTypeAll<Material>();

            foreach (Material material in loaded)
            {
                if (material == null || material.shader == null) continue;
                if (material.renderQueue < TransparentQueue) continue;

                Logger.Log($"Falling back to shader '{material.shader.name}' borrowed from the loaded scene.",
                           LogLevel.WARNING);
                return material.shader;
            }

            return null;
        }

        private static void MakeTransparent(Material material)
        {
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_AlphaClip")) material.SetFloat("_AlphaClip", 0f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);

            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = TransparentQueue;
        }
    }
}
