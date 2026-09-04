using ClosingCircle.Domain;
using UnityEngine;

// The share of the visible height the wall takes to fade out, counted down from the top. 0 is a hard edge.

namespace ClosingCircle.ConfigVariables
{
    public class SetFade : IConfigVariable
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.Fade;

        public bool Validate(string value) =>
            Parse.Float(value, out float fade) && SettingRanges.Get("Fade").Holds(fade);

        public void Execute(string value)
        {
            Parse.Float(value, out float fade);
            ZoneService.Fade = Mathf.Clamp01(fade);
            ZoneService.MarkLookChanged();
        }
    }
}
