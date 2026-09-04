using ClosingCircle.Domain;
using UnityEngine;

// Blur:0 to 100, as a percentage to match Opacity. 0 is a flat wall; above that it refracts what is
// behind it, which costs more the more of the screen the wall fills.

namespace ClosingCircle.ConfigVariables
{
    public class SetBlur : IConfigVariable
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.Blur;

        public bool Validate(string value) =>
            Parse.Float(value, out float percent) && SettingRanges.Get("Blur").Holds(percent);

        public void Execute(string value)
        {
            Parse.Float(value, out float percent);
            ZoneService.Blur = Mathf.Clamp01(percent / 100f);
            ZoneService.MarkLookChanged();
        }
    }
}
