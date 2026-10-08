using ClosingCircle.Systems;
using UnityEngine;

// StartCenter:x,z, or StartCenter:CustomSpawns to start on the center of the spawn layout the map publishes.

namespace ClosingCircle.ConfigVariables
{
    public class SetStartCenter : IConfigVariable
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.StartCenter;

        public bool Validate(string value) => SpawnLayout.IsArgument(value) || Parse.Vector2(value, out _);

        public void Execute(string value)
        {
            if (SpawnLayout.IsArgument(value))
            {
                ZoneService.StartFromSpawns = true;
                SpawnLayout.ApplyIfRunning();
                return;
            }

            Parse.Vector2(value, out Vector2 center);
            ZoneService.StartFromSpawns = false;
            ZoneService.Plan.StartCenter = center;
        }
    }
}
