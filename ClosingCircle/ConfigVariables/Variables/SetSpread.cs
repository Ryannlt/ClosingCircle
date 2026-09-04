using ClosingCircle.Domain;
using UnityEngine;

// How much of the legal range a randomised centre may use. 0 puts the zone in the same place every round,
// 1 uses all of it, which makes this the number a league publishes.

namespace ClosingCircle.ConfigVariables
{
    public class SetSpread : IConfigVariable
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.Spread;

        public bool Validate(string value) =>
            Parse.Float(value, out float spread) && SettingRanges.Get("Spread").Holds(spread);

        public void Execute(string value)
        {
            Parse.Float(value, out float spread);
            ZoneService.Spread = Mathf.Clamp01(spread);
        }
    }
}
