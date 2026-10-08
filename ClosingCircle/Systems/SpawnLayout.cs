using System;
using ClosingCircle.Domain;
using UnityEngine;

// StartCenter and Bisector can follow the map's spawns instead of fixed numbers. Holdfast Custom Spawns moves a
// map's spawns every seeded round, so a written center and fair line would be wrong by the next round.
//
// The two mods are separate assemblies and reflection is off limits, so the map publishes the layout as a plain
// object and this finds it by name. Any map can meet the contract by hand: an active GameObject named
// "Holdfast Spawn Layout" whose position is the layout center and whose forward vector lies along the line
// between the spawns. The fair line runs through the center, square to that line.

namespace ClosingCircle.Systems
{
    public static class SpawnLayout
    {
        public const string Argument = "CustomSpawns";
        public const string MarkerName = "Holdfast Spawn Layout";

        public static bool IsArgument(string value) =>
            string.Equals(value?.Trim(), Argument, StringComparison.OrdinalIgnoreCase);

        // An rc 'set' during a round takes effect at once. From the config file it waits for the round start,
        // because the map may not have published its layout yet when config arrives.
        public static void ApplyIfRunning()
        {
            if (ZoneService.RoundSeconds > 0f) Apply();
        }

        // Called at every round start on both sides. The map publishes during its own config pass, which the
        // game runs before any round details go out, so the layout is there by now.
        public static void Apply()
        {
            if (!ZoneService.StartFromSpawns && !ZoneService.BisectorFromSpawns) return;

            GameObject marker = GameObject.Find(MarkerName);
            if (marker == null)
            {
                Logger.Log($"StartCenter or Bisector is set to {Argument}, but this map publishes no spawn layout, " +
                           "so the configured values stay.", LogLevel.WARNING);
                return;
            }

            Vector3 position = marker.transform.position;
            Vector3 forward = marker.transform.forward;
            var center = new Vector2(position.x, position.z);
            var axis = new Vector2(forward.x, forward.z);

            if (ZoneService.StartFromSpawns) ZoneService.Plan.StartCenter = center;

            // A marker turned to face straight up or down has no direction on the ground to be square to.
            bool lined = axis.sqrMagnitude > 0.0001f;
            if (ZoneService.BisectorFromSpawns && lined)
            {
                ZoneService.BisectorPoint = center;
                ZoneService.BisectorHeading = SpawnFrame.FairLineHeading(axis);
                ZoneService.HasBisector = true;
            }
            else if (ZoneService.BisectorFromSpawns)
            {
                Logger.Log("The spawn layout has no direction along the ground, so the configured Bisector stays.",
                           LogLevel.WARNING);
            }

            ZoneService.MarkChanged();

            string where = $"({center.x:0.#}, {center.y:0.#})";
            string start = ZoneService.StartFromSpawns ? $"start center {where}" : string.Empty;
            string line = ZoneService.BisectorFromSpawns && lined
                ? $"fair line through {where} at {ZoneService.BisectorHeading:0.#}deg"
                : string.Empty;
            string joined = start.Length > 0 && line.Length > 0 ? start + ", " + line : start + line;

            Logger.Log($"Following the map's spawns: {joined}.", LogLevel.INFO);
        }
    }
}
