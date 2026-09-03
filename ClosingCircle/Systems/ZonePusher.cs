using ClosingCircle.Core;
using ClosingCircle.Domain;
using System.Collections.Generic;
using UnityEngine;

// Walks players back inside when the zone is solid. Server side, so it binds every client including anyone not
// running the mod, which is what makes it the real enforcement and leaves the collider as the thing that stops
// modded clients ever needing it.
//
// Teleport is the only way a mod can move a player, and as a single jump it would be exactly wrong: disorienting
// to the person moved, and hard to shoot for everyone else. Many small steps instead, at a rate set against the
// network: Competitive sends a nearby player or vehicle up to 64 times a second.

namespace ClosingCircle.Systems
{
    public static class ZonePusher
    {
        // The visible jump is one step and a step scales with this, so correcting slower than the send rate
        // shows several frames of drift before each correction.
        private const float StepSeconds = 1f / 30f;

        // Metres per second the zone gains on somebody, over and above whatever they are doing themselves.
        private const float PushSpeed = 4f;

        // Below this the collider has it covered and the difference is sampling noise, not a player out of
        // bounds.
        private const float OutsideMargin = 1f;

        // Aim slightly inside the boundary so nobody is left standing exactly on the line.
        private const float Inset = 2f;

        // A ceiling on one step, so a bad velocity estimate can never become a long jump.
        private const float MaxStep = 2f;

        // Nothing outruns a horse, so anything past this is a broken estimate rather than a fast player.
        private const float MaxCompensation = 14f;

        // A shorter interval means a smaller position difference, so the speed estimate needs averaging.
        private const float SpeedSmoothing = 0.5f;

        // Roughly a player's width, and cast from the chest rather than the feet so a kerb is not an obstacle.
        private const float CastRadius = 0.4f;
        private const float ChestHeight = 1f;

        // A rise larger than this is a wall or a cliff rather than a slope, and walking someone up it would be
        // worse than leaving them out.
        private const float MaxStepUp = 1f;

        // The same layers MDS avoids when steering bots around the map.
        private static readonly int ObstacleMask = LayerMask.GetMask("Static Environment", "Damageable Collider");

        // What each player was doing last tick, so the push can be measured against their own movement rather
        // than assume they are standing still.
        private struct Tracked
        {
            public Vector2 Position;
            public Vector2 Applied;
            public float Speed;
            public bool Known;
        }

        private static readonly Dictionary<int, Tracked> History = new Dictionary<int, Tracked>();
        private static readonly List<int> Scratch = new List<int>();

        private static float _nextStepAt;
        private static float _lastStepAt;

        public static void Reset()
        {
            History.Clear();
            _nextStepAt = 0f;
            _lastStepAt = 0f;
        }

        public static void Step(ZoneSnapshot zone)
        {
            if (!ZoneService.Solid) return;
            if (Time.time < _nextStepAt) return;

            // Measured rather than assumed, so a frame hitch moves people the distance they should have gone.
            float elapsed = _lastStepAt > 0f ? Time.time - _lastStepAt : StepSeconds;
            elapsed = Mathf.Clamp(elapsed, 0.001f, 0.5f);

            _lastStepAt = Time.time;
            _nextStepAt = Time.time + StepSeconds;

            Scratch.Clear();
            foreach (KeyValuePair<int, PlayerRegistry.Entry> player in PlayerRegistry.All)
                Scratch.Add(player.Key);

            for (int i = 0; i < Scratch.Count; i++)
                Nudge(Scratch[i], zone, elapsed);
        }

        private static void Nudge(int playerId, ZoneSnapshot zone, float elapsed)
        {
            // Somebody who spawned outside is already exempt from damage, and so is anyone in free roam. Dragging
            // either off where they are would be a surprise.
            if (ZoneEnforcer.Exempt(playerId)) return;

            if (!PlayerRegistry.TryGetPosition(playerId, out Vector3 position))
            {
                History.Remove(playerId);
                return;
            }

            // A mounted teleport is forwarded to the horse, so measuring the rider would compare a saddle to a
            // hoof and read the offset between them as speed.
            if (VehicleRegistry.TryGetMount(position, out Vector3 mount)) position = mount;

            var flat = new Vector2(position.x, position.z);

            if (ZoneService.Contains(zone, flat))
            {
                History.Remove(playerId);
                return;
            }

            // Follows the actual edges rather than a circle, so a square pushes somebody back through the face
            // they crossed instead of dragging them toward the middle.
            Vector2 target = ZoneService.NearestInside(zone, flat, Inset);
            float remaining = Vector2.Distance(flat, target);

            if (remaining <= OutsideMargin)
            {
                Remember(playerId, flat, Vector2.zero, 0f);
                return;
            }

            Vector2 inward = (target - flat) / remaining;
            float outward = OutwardSpeed(playerId, flat, inward, elapsed);
            float distance = Mathf.Min(PushSpeed + outward, MaxStep / elapsed) * elapsed;
            distance = Mathf.Min(distance, remaining);

            // Straight in first. If something is in the way, slide along the boundary rather than grinding into
            // it, which is what frees somebody pinned behind a rock.
            Vector2 side = new Vector2(inward.y, -inward.x);

            if (TryStep(playerId, position, flat, flat + inward * distance, outward)) return;
            if (TryStep(playerId, position, flat, flat + (inward + side).normalized * distance, outward)) return;
            if (TryStep(playerId, position, flat, flat + (inward - side).normalized * distance, outward)) return;

            // All three blocked. Skipping is safe rather than final: the zone keeps shrinking and the geometry
            // around them changes. Remember where they are so the next estimate is not polluted by the gap.
            Remember(playerId, flat, Vector2.zero, outward);
        }

        // A horse keeps its momentum through a teleport, so without this the gallop simply outruns a fixed step
        // and the rider streams outward.
        private static float OutwardSpeed(int playerId, Vector2 flat, Vector2 inward, float elapsed)
        {
            if (!History.TryGetValue(playerId, out Tracked last) || !last.Known) return 0f;

            float sample = PushMath.OutwardSpeed(last.Position, last.Applied, flat, inward, elapsed,
                                                 MaxCompensation);

            // Averaged, because this sets the step size and a jittering step is the roughness the rate exists
            // to remove.
            return Mathf.Lerp(last.Speed, sample, SpeedSmoothing);
        }

        private static void Remember(int playerId, Vector2 position, Vector2 applied, float speed) =>
            History[playerId] = new Tracked
            {
                Position = position,
                Applied = applied,
                Speed = speed,
                Known = true
            };

        private static bool TryStep(int playerId, Vector3 from, Vector2 flat, Vector2 to, float speed)
        {
            float ground = TerrainSampler.GetYAt(to);
            if (ground - from.y > MaxStepUp) return false;

            Vector3 origin = from + Vector3.up * ChestHeight;
            Vector3 delta = new Vector3(to.x, ground + ChestHeight, to.y) - origin;
            float length = delta.magnitude;

            if (length < 1e-4f) return false;
            if (Physics.SphereCast(origin, CastRadius, delta / length, out _, length, ObstacleMask)) return false;

            GameFacade.Teleport(playerId, new Vector3(to.x, ground, to.y));
            Remember(playerId, flat, to - flat, speed);
            return true;
        }
    }
}
