
namespace ClosingCircle.ConfigVariables
{
    public class EnableCircle : IConfigVariable
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.EnableCircle;

        public bool Validate(string value) => Parse.Bool(value, out _);

        public void Execute(string value)
        {
            Parse.Bool(value, out bool enabled);
            ZoneService.Enabled = enabled;
            Logger.Log($"Zone {(enabled ? "enabled" : "disabled")}.", LogLevel.INFO);
        }
    }
}
