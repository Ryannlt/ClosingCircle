using ClosingCircle.Domain;
// How far the wall rises above the highest ground beneath it.

namespace ClosingCircle.ConfigVariables
{
    public class SetHeight : IConfigVariable
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.Height;

        public bool Validate(string value) =>
            Parse.Float(value, out float height) && SettingRanges.Get("Height").Holds(height);

        public void Execute(string value)
        {
            Parse.Float(value, out float height);
            ZoneService.Height = height;
            ZoneService.MarkLookChanged();
        }
    }
}
