using ClosingCircle.Core;
using ClosingCircle.Domain;
using UnityEngine;

// An optional ring of colliders along the zone boundary. Client side, because Holdfast moves players on the
// client and the server only hears where they claim to be, so a wall can only ever bind a client running this
// mod. The damage check stays the authority and this is a comfort on top of it.
//
// Layer 18 is confirmed to stop a player. What a collider cannot do is push one: the game's mover resolves
// collisions only when the player moves, so a wall closing past somebody standing still goes straight through
// them and leaves them outside. That is why the wall switches itself off for a player who is already out.

namespace ClosingCircle.Systems
{
    public static class ZoneWall
    {
        private const string ObjectName = "ClosingCircleWallColliders";

        // Generous on purpose: a horse at speed against a thin collider is the classic tunnelling case.
        private const float Thickness = 4f;

        // Tall enough that nobody rides over it and deep enough that terrain never opens a gap underneath,
        // which is also why this needs no terrain sampling of its own.
        private const float Height = 400f;

        private const float TerrainStepSeconds = 0.2f;

        // Players Out of Bounds. Collides with Player and Horse and nothing else, so shots and melee cross the
        // boundary untouched, which is why it beat Static Environment.
        private const int Layer = 18;

        private static GameObject _object;
        private static BoxCollider[] _boxes;

        private static int _builtShapeVersion = -1;
        private static float _nextGroundSampleAt;
        private static float _ground;
        private static bool _blocking = true;

        public static void Reset()
        {
            Teardown();

            // By name and unconditionally, because the assembly reloads between rounds and takes every static
            // with it. A wall left behind that way is invisible and solid, so nothing else would ever find it.
            GameObject stray = GameObject.Find(ObjectName);
            if (stray != null) Object.Destroy(stray);

            _builtShapeVersion = -1;
            _nextGroundSampleAt = 0f;
            _blocking = true;
        }

        // Cheap enough to call every frame, unlike Reset, which also sweeps for strays by name.
        public static void Hide() => Teardown();

        public static void Update(ZoneSnapshot zone)
        {
            if (!ZoneService.Solid)
            {
                Teardown();
                return;
            }

            EnsureBuilt();
            if (_boxes == null) return;

            SampleGround(zone);
            Place(zone);
            FollowLocalPlayer(zone);
        }

        // A wall exists to keep people in, so it must not keep out somebody it has already closed past. The
        // collider only ever binds the local player anyway, since everyone else on this client is moved by
        // network updates rather than physics.
        private static void FollowLocalPlayer(ZoneSnapshot zone)
        {
            // Free roam flies through everything else in the game, and a zone boundary is no different.
            if (LocalPlayer.FreeFlying)
            {
                if (!_blocking) return;

                _blocking = false;
                _object.SetActive(false);
                Logger.Log("Local player is in free roam, wall stood down.", LogLevel.DEBUG);
                return;
            }

            if (!LocalPlayer.TryGetGroundPosition(out Vector2 position)) return;

            bool inside = ZoneService.Contains(zone, position);

            // Coming back in only re-arms the wall once they are clear of the volume it occupies, otherwise it
            // would switch on around somebody still standing in it.
            if (!_blocking && inside)
            {
                ZoneSnapshot inner = zone;
                inner.Radius = Mathf.Max(0f, zone.Radius - Thickness * 2f);
                if (!ZoneService.Contains(inner, position)) return;
            }

            if (inside == _blocking) return;

            _blocking = inside;
            _object.SetActive(inside);
            Logger.Log(inside ? "Local player is inside, wall armed." : "Local player is outside, wall stood down.",
                       LogLevel.DEBUG);
        }

        private static void EnsureBuilt()
        {
            bool stale = _object == null || _builtShapeVersion != ZoneService.ShapeVersion;

            if (!stale) return;

            Teardown();

            _builtShapeVersion = ZoneService.ShapeVersion;

            GameObject stray = GameObject.Find(ObjectName);
            if (stray != null) Object.Destroy(stray);

            // Deliberately not DontDestroyOnLoad: this belongs to the round, and outliving the scene is exactly
            // how an invisible wall from a previous round ends up frozen in the middle of the next one.
            _object = new GameObject(ObjectName);
            _blocking = true;

            int sides = ZoneService.Shape.Sides;
            _boxes = new BoxCollider[sides];

            for (int i = 0; i < sides; i++)
            {
                var segment = new GameObject($"Segment{i}") { layer = Layer };
                segment.transform.SetParent(_object.transform);
                _boxes[i] = segment.AddComponent<BoxCollider>();
            }

            Logger.Log($"Built {sides} wall colliders on layer {Layer} ({LayerMask.LayerToName(Layer)}).",
                       LogLevel.INFO);
        }

        private static void SampleGround(ZoneSnapshot zone)
        {
            if (Time.time < _nextGroundSampleAt) return;

            _nextGroundSampleAt = Time.time + TerrainStepSeconds;
            _ground = TerrainSampler.GetYAt(zone.Centre);
        }

        private static void Place(ZoneSnapshot zone)
        {
            ZoneShape shape = ZoneService.Shape;
            Vector2[] ring = shape.Vertices(zone.Centre, zone.Radius);

            for (int i = 0; i < _boxes.Length && i < ring.Length; i++)
            {
                Vector2 a = ring[i];
                Vector2 b = ring[(i + 1) % ring.Length];
                Vector2 midpoint = (a + b) * 0.5f;
                Vector2 outward = (midpoint - zone.Centre).normalized;

                // Pushed fully outside the boundary rather than straddling it, so a player standing at the edge
                // is never already inside the volume the wall occupies.
                Vector2 seated = midpoint + outward * (Thickness * 0.5f);

                Transform segment = _boxes[i].transform;
                segment.position = new Vector3(seated.x, _ground, seated.y);
                segment.rotation = Quaternion.LookRotation(new Vector3(outward.x, 0f, outward.y), Vector3.up);

                // LookRotation puts local Z along the outward normal and local X along the edge, so the box is
                // as wide as the edge, as thick as the wall, and tall enough to be unclimbable.
                _boxes[i].size = new Vector3(Vector2.Distance(a, b), Height, Thickness);
            }
        }

        private static void Teardown()
        {
            if (_object != null) Object.Destroy(_object);

            _object = null;
            _boxes = null;
        }
    }
}
