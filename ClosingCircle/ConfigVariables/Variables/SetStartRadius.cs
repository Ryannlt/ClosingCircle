namespace ClosingCircle.ConfigVariables
{
    public class SetStartRadius : IConfigVariable
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.StartRadius;

        public bool Validate(string value) => Parse.Float(value, out float radius) && radius > 0f;

        public void Execute(string value)
        {
            Parse.Float(value, out float radius);
            ZoneService.Plan.StartRadius = radius;
        }
    }
}
