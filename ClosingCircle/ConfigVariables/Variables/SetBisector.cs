using ClosingCircle.Systems;
using UnityEngine;

// Bisector:x,z,heading. The fair line, as a point and a bearing in degrees clockwise from north. On a
// symmetric map that is the map center and the front's bearing. Bisector:CustomSpawns takes the line from the
// spawn layout the map publishes instead, at every round start.

namespace ClosingCircle.ConfigVariables
{
    public class SetBisector : IConfigVariable
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.Bisector;

        public bool Validate(string value) => SpawnLayout.IsArgument(value) || TryRead(value, out _, out _);

        public void Execute(string value)
        {
            if (SpawnLayout.IsArgument(value))
            {
                ZoneService.BisectorFromSpawns = true;
                SpawnLayout.ApplyIfRunning();
                return;
            }

            if (!TryRead(value, out Vector2 point, out float heading)) return;

            ZoneService.BisectorFromSpawns = false;
            ZoneService.BisectorPoint = point;
            ZoneService.BisectorHeading = heading;
            ZoneService.HasBisector = true;
        }

        private static bool TryRead(string value, out Vector2 point, out float heading)
        {
            point = Vector2.zero;
            heading = 0f;

            string[] parts = value?.Split(',');
            if (parts == null || parts.Length != 3) return false;

            if (!Parse.Float(parts[0], out float x)) return false;
            if (!Parse.Float(parts[1], out float z)) return false;
            if (!Parse.Float(parts[2], out heading)) return false;

            point = new Vector2(x, z);
            return true;
        }
    }
}
