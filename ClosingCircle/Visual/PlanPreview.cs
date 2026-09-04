using ClosingCircle.Core;
using ClosingCircle.Domain;
using ClosingCircle.Systems;
using UnityEngine;

// Every stage still to come, drawn at once and faintly, so an admin can walk the schedule before the round
// starts. Each ring is a mesh fitted to the ground it stands on, because a preview that floats over a slope is
// telling you the wrong thing about exactly the question it exists to answer.
//
// Only stages whose centre has actually been decided are drawn, and only until they have run. A stage waiting
// on something the mod cannot know yet is left out rather than guessed at.

namespace ClosingCircle.Visual
{
    public static class PlanPreview
    {
        // Kept below the real wall's opacity because several rings overlap: the preview follows every other
        // look setting exactly, but at full opacity a four stage plan is a solid block of colour.
        private const float OpacityShare = 0.6f;

        // Ground is sampled per ring vertex, so terrain can still bulge above the straight line between two
        // samples. Starting slightly under the surface hides that without the ring appearing to float.
        private const float BaseSink = 2f;

        private static Mesh[] _meshes;
        private static Material _material;
        private static Texture2D _ramp;
        private static float _until;

        private static int _builtRevision = -1;
        private static int _builtLook = -1;
        private static int _builtFrom = -1;
        private static int _builtCount = -1;

        public static void Reset()
        {
            Dispose();
            _until = 0f;
        }

        public static void Show(float seconds)
        {
            // Built on the next draw instead, which is where the round clock is.
            Invalidate();
            _until = Time.time + seconds;

            Logger.Log($"Previewing the remaining stages for {seconds:0}s.", LogLevel.INFO);
        }

        public static void Draw(float timeRemaining)
        {
            if (Time.time > _until) return;

            PlanWindow.Visible(ZoneService.Plan, timeRemaining, out int from, out int count);

            // A preview that stays on screen while an admin tunes the zone has to follow the tuning, and it has
            // to drop a stage the moment it has run.
            // LookVersion as well as Revision, so a colour or height changed while the preview is up is
            // reflected straight away rather than at the next plan edit.
            if (_builtRevision != ZoneService.Revision || _builtLook != ClientDisplay.LookVersion ||
                _builtFrom != from || _builtCount != count)
                Build(from, count);

            if (_meshes == null || _material == null) return;

            for (int i = 0; i < _meshes.Length; i++)
                Graphics.DrawMesh(_meshes[i], Matrix4x4.identity, _material, 0);
        }

        // Terrain is sampled once per rebuild rather than per frame, because a preview never moves on its own.
        private static void Build(int from, int count)
        {
            Dispose();

            _builtRevision = ZoneService.Revision;
            _builtLook = ClientDisplay.LookVersion;
            _builtFrom = from;
            _builtCount = count;

            if (count <= 0) return;

            // Read through ClientDisplay, exactly as the live wall does, so a wall turned short and blue
            // previews short and blue.
            _ramp = GradientTexture.Build(ClientDisplay.Fade);

            Color color = ClientDisplay.Tinted;
            color.a *= OpacityShare;

            _material = ZoneMaterial.Build(_ramp, color);
            ZoneMaterial.SetRamp(_material, 1f, 0f);
            ZoneMaterial.SetBlur(_material, ClientDisplay.Blur);

            float height = ClientDisplay.Height;

            ZoneShape shape = ZoneService.Shape;
            _meshes = new Mesh[count];

            for (int i = 0; i < count; i++)
            {
                Stage stage = ZoneService.Plan.Stages[from + i];
                Vector2[] ring = shape.Vertices(stage.Centre, stage.Radius);
                var ground = new float[ring.Length];

                for (int j = 0; j < ring.Length; j++) ground[j] = TerrainSampler.GetYAt(ring[j]) - BaseSink;

                _meshes[i] = ZoneMeshBuilder.BuildFitted(ring, ground, height + BaseSink);
            }
        }

        private static void Invalidate()
        {
            _builtRevision = -1;
            _builtLook = -1;
            _builtFrom = -1;
            _builtCount = -1;
        }

        // Deliberately leaves the expiry alone, so a rebuild mid-preview does not cut the preview short.
        private static void Dispose()
        {
            if (_meshes != null)
                for (int i = 0; i < _meshes.Length; i++)
                    if (_meshes[i] != null) Object.Destroy(_meshes[i]);

            if (_material != null) Object.Destroy(_material);
            if (_ramp != null) Object.Destroy(_ramp);

            _meshes = null;
            _material = null;
            _ramp = null;

            Invalidate();
        }
    }
}
