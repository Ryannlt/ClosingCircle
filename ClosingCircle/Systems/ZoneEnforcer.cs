using ClosingCircle.Core;
using ClosingCircle.Domain;
using System.Collections.Generic;
using UnityEngine;

// Server side. Walks live players against the zone and slaps whoever has just left it. One slap per crossing,
// which at a lethal damage value means the player dies outright and never plays the flinch.

namespace ClosingCircle.Systems
{
    public static class ZoneEnforcer
    {
        private const float StepSeconds = 0.25f;

        private static readonly Dictionary<int, float> SlappedAt = new Dictionary<int, float>();

        // Nobody is slapped until they have been inside the zone at least once since spawning. That covers a
        // player who spawns outside a zone that has already closed past their spawn, and costs nothing for a
        // player who spawns inside, who leaves this set on their first tick.
        private static readonly HashSet<int> AwaitingEntry = new HashSet<int>();

        // Free-roam is observation, not participation: an admin flying the round is neither slapped nor pushed.
        private static readonly HashSet<int> FreeFlying = new HashSet<int>();

        private static readonly List<int> Scratch = new List<int>();

        private static float _nextStepAt;

        public static void Reset()
        {
            SlappedAt.Clear();
            AwaitingEntry.Clear();
            FreeFlying.Clear();
            _nextStepAt = 0f;
        }

        public static void OnSpawned(int playerId) => AwaitingEntry.Add(playerId);

        public static void OnFreeflight(int playerId, bool flying)
        {
            if (flying) FreeFlying.Add(playerId);
            else FreeFlying.Remove(playerId);
        }

        public static bool Exempt(int playerId) => AwaitingEntry.Contains(playerId) || FreeFlying.Contains(playerId);

        // Driven from the per-frame round clock, then throttled here, so the mod needs no coroutine of its own.
        public static void Step(float timeRemaining)
        {
            if (Time.time < _nextStepAt) return;
            _nextStepAt = Time.time + StepSeconds;

            ZoneSnapshot zone = ZoneService.Evaluate(timeRemaining);

            Scratch.Clear();
            foreach (KeyValuePair<int, PlayerRegistry.Entry> player in PlayerRegistry.All)
                Scratch.Add(player.Key);

            for (int i = 0; i < Scratch.Count; i++)
                Evaluate(Scratch[i], zone);
        }

        private static void Evaluate(int playerId, ZoneSnapshot zone)
        {
            if (!PlayerRegistry.TryGetGroundPosition(playerId, out Vector2 position))
            {
                // Dead or gone, so forget them. A respawn then counts as a fresh crossing.
                SlappedAt.Remove(playerId);
                return;
            }

            if (ZoneService.Contains(zone, position))
            {
                AwaitingEntry.Remove(playerId);
                SlappedAt.Remove(playerId);
                return;
            }

            if (Exempt(playerId)) return;

            // A solid zone puts people back rather than hurting them, so the two modes are one sentence each:
            // solid walks you in, and not solid hurts you.
            if (ZoneService.Solid) return;

            if (SlappedAt.TryGetValue(playerId, out float last))
            {
                // Already dealt with on the way out. Repeating is off by default so a survivor is left alone.
                if (ZoneService.RepeatSeconds <= 0f) return;
                if (Time.time - last < ZoneService.RepeatSeconds) return;
            }

            SlappedAt[playerId] = Time.time;
            GameFacade.Slap(playerId, ZoneService.Damage);
            Logger.Log($"Player {playerId} left the zone at ({position.x:0.#}, {position.y:0.#}), " +
                       $"slapped for {ZoneService.Damage}.", LogLevel.DEBUG);
        }
    }
}
