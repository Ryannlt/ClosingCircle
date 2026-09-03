using System.Collections.Generic;
using UnityEngine;

// Horses, tracked so the pusher can measure the object it is actually going to move: `teleport` on a mounted
// player is forwarded to the vehicle, while the player object stays in the saddle a fixed offset away.
//
// Mounts are resolved by proximity rather than by the owner id the spawn callback carries, because the SDK has
// no mount or dismount event and that id would miss anyone who took a loose horse.

namespace ClosingCircle.Systems
{
    public static class VehicleRegistry
    {
        // Comfortably wider than a saddle offset and narrower than the gap between two horses worth telling
        // apart.
        private const float MountRadius = 2f;

        // Height does the real work: somebody in a saddle sits well above the horse's root, where somebody
        // standing beside it shares the ground with it.
        private const float MountMinRise = 0.5f;
        private const float MountMaxRise = 3f;

        private struct Entry
        {
            public GameObject Body;
            public bool Alive;
        }

        private static readonly Dictionary<int, Entry> Vehicles = new Dictionary<int, Entry>();

        public static void Reset() => Vehicles.Clear();

        public static void OnSpawned(int vehicleId, GameObject body) =>
            Vehicles[vehicleId] = new Entry { Body = body, Alive = true };

        public static void OnHurt(int vehicleId, byte newHp)
        {
            if (newHp > 0) return;
            if (!Vehicles.TryGetValue(vehicleId, out Entry entry)) return;

            entry.Alive = false;
            Vehicles[vehicleId] = entry;
        }

        // The nearest live vehicle this rider is sitting on, or false if they are on foot.
        public static bool TryGetMount(Vector3 riderPosition, out Vector3 position)
        {
            position = Vector3.zero;

            float best = MountRadius * MountRadius;
            bool found = false;

            foreach (KeyValuePair<int, Entry> vehicle in Vehicles)
            {
                Entry entry = vehicle.Value;
                if (!entry.Alive || entry.Body == null) continue;

                Vector3 candidate = entry.Body.transform.position;
                float rise = riderPosition.y - candidate.y;

                if (rise < MountMinRise || rise > MountMaxRise) continue;

                float dx = candidate.x - riderPosition.x;
                float dz = candidate.z - riderPosition.z;
                float distance = dx * dx + dz * dz;

                if (distance > best) continue;

                best = distance;
                position = candidate;
                found = true;
            }

            return found;
        }
    }
}
