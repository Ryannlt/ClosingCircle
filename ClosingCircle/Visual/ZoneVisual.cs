using ClosingCircle.Core;
using ClosingCircle.Domain;
using UnityEngine;

// The wall the players see. Built once per shape and then only moved and scaled, so the per-frame cost is a
// transform write and two material floats.

namespace ClosingCircle.Visual
{
    public static class ZoneVisual
    {
        private const string ObjectName = "ClosingCircleWall";

        // How far the wall is sunk below the lowest ground under it, so a slope never shows daylight underneath.
        private const float BuryDepth = 20f;

        private const float TerrainStepSeconds = 0.2f;
        private const int MaxTerrainSamples = 16;

        private static GameObject _object;
        private static MeshFilter _filter;
        private static MeshRenderer _renderer;
        private static Material _material;
        private static Texture2D _ramp;

        private static int _builtShapeVersion = -1;
        private static int _builtLookVersion = -1;

        private static float _nextTerrainSampleAt;
        private static float _groundHigh;
        private static float _groundLow;

        public static void Reset()
        {
            Teardown();

            // By name and unconditionally: the assembly reloads between rounds and takes every static with it,
            // so a leftover object is otherwise unreachable. The same hole left an invisible wall behind once.
            GameObject stray = GameObject.Find(ObjectName);
            if (stray != null) Object.Destroy(stray);
            _builtShapeVersion = -1;
            _builtLookVersion = -1;
            _nextTerrainSampleAt = 0f;
        }

        // Cheap enough to call every frame, unlike Reset, which also sweeps for strays by name.
        public static void Hide() => Teardown();

        public static void Update(ZoneSnapshot zone)
        {
            EnsureBuilt();
            if (_object == null) return;

            SampleTerrain(zone);

            float top = _groundHigh + ZoneService.Height;
            float floor = _groundLow - BuryDepth;
            float total = Mathf.Max(top - floor, 0.01f);

            _object.transform.position = new Vector3(zone.Centre.x, floor, zone.Centre.y);
            _object.transform.localScale = new Vector3(zone.Radius, total, zone.Radius);

            // Map the mesh's own 0..1 height onto the ramp so its zero point lands exactly at ground level.
            float height = Mathf.Max(ZoneService.Height, 0.01f);
            ZoneMaterial.SetRamp(_material, total / height, (floor - _groundHigh) / height);
        }

        private static void EnsureBuilt()
        {
            if (_object == null)
            {
                // Deliberately not DontDestroyOnLoad: this belongs to the round, and outliving the scene is how
                // a previous round's wall ends up sitting in the middle of the next one.
                _object = new GameObject(ObjectName);
                _filter = _object.AddComponent<MeshFilter>();
                _renderer = _object.AddComponent<MeshRenderer>();
                _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _renderer.receiveShadows = false;
            }

            if (_builtShapeVersion != ZoneService.ShapeVersion)
            {
                _builtShapeVersion = ZoneService.ShapeVersion;
                if (_filter.sharedMesh != null) Object.Destroy(_filter.sharedMesh);
                _filter.sharedMesh = ZoneMeshBuilder.Build(ZoneService.Shape);
            }

            if (_builtLookVersion != ZoneService.LookVersion)
            {
                _builtLookVersion = ZoneService.LookVersion;
                if (_ramp != null) Object.Destroy(_ramp);
                _ramp = GradientTexture.Build(ZoneService.Fade);

                if (_material == null)
                    _material = ZoneMaterial.Build(_ramp, ZoneService.Tinted);
                else
                    ZoneMaterial.Apply(_material, _ramp, ZoneService.Tinted);

                _renderer.sharedMaterial = _material;
                _renderer.enabled = _material != null;
            }
        }

        // Terrain barely moves under a closing zone, so this runs at 5 Hz rather than every frame.
        private static void SampleTerrain(ZoneSnapshot zone)
        {
            if (Time.time < _nextTerrainSampleAt) return;
            _nextTerrainSampleAt = Time.time + TerrainStepSeconds;

            ZoneShape shape = ZoneService.Shape;
            int stride = Mathf.Max(1, shape.Sides / MaxTerrainSamples);
            Vector2[] ring = shape.Vertices(zone.Centre, zone.Radius);

            float low = TerrainSampler.GetYAt(zone.Centre);
            float high = low;

            for (int i = 0; i < ring.Length; i += stride)
            {
                float y = TerrainSampler.GetYAt(ring[i]);
                if (y < low) low = y;
                if (y > high) high = y;
            }

            _groundLow = low;
            _groundHigh = high;
        }

        private static void Teardown()
        {
            if (_filter != null && _filter.sharedMesh != null) Object.Destroy(_filter.sharedMesh);
            if (_material != null) Object.Destroy(_material);
            if (_ramp != null) Object.Destroy(_ramp);
            if (_object != null) Object.Destroy(_object);

            _object = null;
            _filter = null;
            _renderer = null;
            _material = null;
            _ramp = null;
        }
    }
}
