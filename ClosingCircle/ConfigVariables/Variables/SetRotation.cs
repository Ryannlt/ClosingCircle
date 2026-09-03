namespace ClosingCircle.ConfigVariables
{
    public class SetRotation : IConfigVariable
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.Rotation;

        public bool Validate(string value) => Parse.Float(value, out _);

        public void Execute(string value)
        {
            Parse.Float(value, out float rotation);
            ZoneService.SetShape(ZoneService.Shape.Sides, rotation);
        }
    }
}
