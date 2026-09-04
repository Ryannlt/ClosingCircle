using ClosingCircle.Domain;
namespace ClosingCircle.ConfigVariables
{
    public class SetStartRadius : IConfigVariable
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.StartRadius;

        public bool Validate(string value) =>
            Parse.Float(value, out float radius) && SettingRanges.Get("StartRadius").Holds(radius);

        public void Execute(string value)
        {
            Parse.Float(value, out float radius);
            ZoneService.Plan.StartRadius = radius;
        }
    }
}
