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
        }

        private static readonly Dictionary<int, Entry> Players = new Dictionary<int, Entry>();

        public static IEnumerable<KeyValuePair<int, Entry>> All => Players;

        public static void Reset() => Players.Clear();

        public static void OnSpawned(int playerId, GameObject body) =>
            Players[playerId] = new Entry { Body = body, Alive = true };

        public static void OnHurt(int playerId, byte newHp)
        {
            if (newHp > 0) return;
            if (!Players.TryGetValue(playerId, out Entry entry)) return;

            entry.Alive = false;
            Players[playerId] = entry;
        }

        public static void OnLeft(int playerId) => Players.Remove(playerId);

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
