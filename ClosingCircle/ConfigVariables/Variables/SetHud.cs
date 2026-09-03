
namespace ClosingCircle.ConfigVariables
{
    public class SetHud : IConfigVariable
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.Hud;

        public bool Validate(string value) => Parse.Bool(value, out _);

        public void Execute(string value)
        {
            Parse.Bool(value, out bool hud);
            ZoneService.Hud = hud;
        }
    }
}
