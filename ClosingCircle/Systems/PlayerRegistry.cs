using HoldfastSharedMethods;
using System.Collections.Generic;
using UnityEngine;

// Who is alive and where they are. The alive flag is not optional: a corpse's player object stays non-null and
// the same player id comes back on respawn, so liveness cannot be read off the object.

namespace ClosingCircle.Systems
{
    public static class PlayerRegistry
    {
        public struct Entry
        {
            public GameObject Body;
            public bool Alive;
            public FactionCountry Faction;
        }

        private static readonly Dictionary<int, Entry> Players = new Dictionary<int, Entry>();

        public static IEnumerable<KeyValuePair<int, Entry>> All => Players;

        public static void Reset()
        {
            Players.Clear();
            Bots.Clear();
        }

        public static void OnSpawned(int playerId, GameObject body, FactionCountry faction) =>
            Players[playerId] = new Entry { Body = body, Alive = true, Faction = faction };

        public static void OnHurt(int playerId, byte newHp)
        {
            if (newHp > 0) return;
            if (!Players.TryGetValue(playerId, out Entry entry)) return;

            entry.Alive = false;
            Players[playerId] = entry;
        }

        // Bots have no client, so nothing that talks to one should try. Tracked separately from the spawn
        // record because OnPlayerJoined tells us this before OnPlayerSpawned tells us anything else.
        private static readonly HashSet<int> Bots = new HashSet<int>();

        public static void OnJoined(int playerId, bool isBot)
        {
            if (isBot) Bots.Add(playerId);
            else Bots.Remove(playerId);
        }

        public static bool IsBot(int playerId) => Bots.Contains(playerId);

        public static void OnLeft(int playerId)
        {
            Players.Remove(playerId);
            Bots.Remove(playerId);
        }

        // The full position, which the push needs because it has to cast along the step it is about to take.
        public static bool TryGetPosition(int playerId, out Vector3 position)
        {
            position = Vector3.zero;

            if (!Players.TryGetValue(playerId, out Entry entry)) return false;
            if (!entry.Alive || entry.Body == null) return false;

            position = entry.Body.transform.position;
            return true;
        }

        // Ground position on the XZ plane, or false if this player is not somewhere we can act on.
        public static bool TryGetGroundPosition(int playerId, out Vector2 position)
        {
            position = Vector2.zero;

            if (!Players.TryGetValue(playerId, out Entry entry)) return false;
            if (!entry.Alive || entry.Body == null) return false;

            Vector3 world = entry.Body.transform.position;
            position = new Vector2(world.x, world.z);
            return true;
        }
    }
}
