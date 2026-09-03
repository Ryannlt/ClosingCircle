using ClosingCircle.Core;
using ClosingCircle.Domain;
using System.Collections.Generic;
using UnityEngine;

// Server side. Sends the authoritative zone to clients whenever it changes, so an rc edit mid-round does not
// leave everyone drawing yesterday's plan.
//
// Catching up a client who arrived after the last push matters more than it looks: without the plan they fall
// back on what their own config built, where no stage is resolved and every centre reads as its placeholder, so
// they watch the zone close on the wrong place entirely.

namespace ClosingCircle.Sync
{
    public static class PlanBroadcaster
    {
        // A connecting client is still loading the map, and its own copy of the mod is not listening yet, so a
        // single push lands on nothing. There is no ack channel, so try a few times and stop.
        private static readonly float[] Attempts = { 5f, 15f, 30f };

        private static int _sentRevision = -1;

        // Everyone connected, against when their catch-up clock started. Held apart from the retry state so a
        // round change can re-queue the people already watching, who will never connect again to trigger it.
        private static readonly Dictionary<int, float> Connected = new Dictionary<int, float>();

        // Who still needs the plan, against the index of their next attempt.
        private static readonly Dictionary<int, int> Pending = new Dictionary<int, int>();

        private static readonly List<int> Scratch = new List<int>();

        public static void Reset()
        {
            _sentRevision = -1;
            Pending.Clear();

            // A new round means a new plan, and the client loading into it can miss the round-start broadcast
            // exactly as a joining one does.
            Scratch.Clear();
            foreach (KeyValuePair<int, float> player in Connected) Scratch.Add(player.Key);

            for (int i = 0; i < Scratch.Count; i++) Queue(Scratch[i]);
        }

        public static void OnConnected(int playerId) => Queue(playerId);

        public static void OnLeft(int playerId)
        {
            Connected.Remove(playerId);
            Pending.Remove(playerId);
        }

        // Called from the round clock. Cheap: an int compare on every frame, work only when something moved.
        public static void Step()
        {
            CatchUp();

            if (_sentRevision == ZoneService.Revision) return;

            _sentRevision = ZoneService.Revision;
            PushToAll();
        }

        public static void PushToAll()
        {
            foreach (string chunk in Chunks())
                GameFacade.Execute($"serverAdmin quietBroadcastMessage {chunk}", logResult: false);

            Logger.Log($"Pushed zone revision {ZoneService.Revision} to all clients.", LogLevel.DEBUG);
        }

        // The spawn and preview paths. Both also settle the retries: they have the plan now.
        public static void PushTo(int playerId)
        {
            Pending.Remove(playerId);
            Send(playerId);
        }

        private static void Queue(int playerId)
        {
            Connected[playerId] = Time.time;
            Pending[playerId] = 0;
        }

        private static void CatchUp()
        {
            if (Pending.Count == 0) return;

            Scratch.Clear();
            foreach (KeyValuePair<int, int> player in Pending)
                if (Time.time - Connected[player.Key] >= Attempts[player.Value]) Scratch.Add(player.Key);

            for (int i = 0; i < Scratch.Count; i++)
            {
                int playerId = Scratch[i];
                int next = Pending[playerId] + 1;

                if (next < Attempts.Length) Pending[playerId] = next;
                else Pending.Remove(playerId);

                // Send rather than PushTo, which would drop the attempts still to come.
                Send(playerId);
            }
        }

        private static void Send(int playerId)
        {
            foreach (string chunk in Chunks())
                GameFacade.Execute($"serverAdmin quietPrivateMessage {playerId} {chunk}", logResult: false);

            Logger.Log($"Pushed zone revision {ZoneService.Revision} to player {playerId}.", LogLevel.DEBUG);
        }

        private static string[] Chunks() =>
            PlanCodec.Chunk(PlanCodec.Encode(Capture()), ZoneService.Revision);

        public static ZoneState Capture()
        {
            var stages = new List<Stage>(ZoneService.Plan.Count);
            for (int i = 0; i < ZoneService.Plan.Count; i++) stages.Add(ZoneService.Plan.Stages[i]);

            return new ZoneState
            {
                Enabled = ZoneService.Enabled,
                Solid = ZoneService.Solid,
                Hud = ZoneService.Hud,
                Sides = ZoneService.Shape.Sides,
                Rotation = ZoneService.Shape.Rotation,
                StartRadius = ZoneService.Plan.StartRadius,
                StartCentre = ZoneService.Plan.StartCentre,
                Stages = stages,
                ColorR = Mathf.RoundToInt(ZoneService.Color.r * 255f),
                ColorG = Mathf.RoundToInt(ZoneService.Color.g * 255f),
                ColorB = Mathf.RoundToInt(ZoneService.Color.b * 255f),
                OpacityPercent = ZoneService.Opacity * 100f,
                Height = ZoneService.Height,
                Fade = ZoneService.Fade
            };
        }
    }
}
