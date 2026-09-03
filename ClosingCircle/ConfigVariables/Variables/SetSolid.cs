
namespace ClosingCircle.ConfigVariables
{
    public class SetSolid : IConfigVariable
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.Solid;

        public bool Validate(string value) => Parse.Bool(value, out _);

        public void Execute(string value)
        {
            Parse.Bool(value, out bool solid);
            ZoneService.Solid = solid;
        }
    }
}
