using UnityEngine;

namespace ClosingCircle.ConfigVariables
{
    public class SetStartCentre : IConfigVariable
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.StartCentre;

        public bool Validate(string value) => Parse.Vector2(value, out _);

        public void Execute(string value)
        {
            Parse.Vector2(value, out Vector2 centre);
            ZoneService.Plan.StartCentre = centre;
        }
    }
}
