using ClosingCircle.Domain;
using UnityEngine;

// Opacity:0 to 100, as a percentage, because that reads better in a config than 0.24.

namespace ClosingCircle.ConfigVariables
{
    public class SetOpacity : IConfigVariable
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.Opacity;

        public bool Validate(string value) =>
            Parse.Float(value, out float percent) && SettingRanges.Get("Opacity").Holds(percent);

        public void Execute(string value)
        {
            Parse.Float(value, out float percent);
            ZoneService.Opacity = Mathf.Clamp01(percent / 100f);
            ZoneService.MarkLookChanged();
        }
    }
}
