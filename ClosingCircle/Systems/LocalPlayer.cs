using UnityEngine;

// Which player this client is, and where they are. Followed rather than asked for, because a mod gets no direct
// handle on the local player: OnIsClient gives a steam id, OnPlayerJoined maps that to a player id, and
// OnPlayerSpawned finally hands over the object.

namespace ClosingCircle.Systems
{
    public static class LocalPlayer
    {
        private static ulong _steamId;
        private static int _playerId = -1;
        private static GameObject _body;

        // A free-roaming admin is a camera, not a participant, so nothing the zone does should apply to them.
        public static bool FreeFlying { get; private set; }

        public static void Reset()
        {
            _body = null;
            FreeFlying = false;
        }

        public static void OnIsClient(ulong steamId) => _steamId = steamId;

        public static void OnJoined(int playerId, ulong steamId)
        {
            if (steamId != _steamId || _steamId == 0UL) return;

            _playerId = playerId;
            Logger.Log($"This client is player {playerId}.", LogLevel.DEBUG);
        }

        // Raised on this client for its own player only, so there is no id to match against.
        public static void OnFreeflight(bool flying) => FreeFlying = flying;

        public static void OnSpawned(int playerId, GameObject body)
        {
            if (playerId != _playerId) return;

            _body = body;
        }

        public static void OnLeft(int playerId)
        {
            if (playerId != _playerId) return;

            _body = null;
        }

        // False while spectating, in free flight, or before the first spawn, which all read as "nowhere in
        // particular" and are handled by the caller rather than guessed at here.
        public static bool TryGetGroundPosition(out Vector2 position)
        {
            position = Vector2.zero;
            if (_body == null) return false;

            Vector3 world = _body.transform.position;
            position = new Vector2(world.x, world.z);
            return true;
        }
    }
}
