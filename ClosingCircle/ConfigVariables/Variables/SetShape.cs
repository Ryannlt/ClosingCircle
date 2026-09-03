using ClosingCircle.Domain;

namespace ClosingCircle.ConfigVariables
{
    public class SetShape : IConfigVariable
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.Shape;

        public bool Validate(string value) => ZoneShape.TryParseSides(value, out _);

        public void Execute(string value)
        {
            ZoneShape.TryParseSides(value, out int sides);
            ZoneService.SetShape(sides, ZoneService.Shape.Rotation);
        }
    }
}
