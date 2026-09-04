using ClosingCircle.Domain;
namespace ClosingCircle.ConfigVariables
{
    public class SetRepeatSeconds : IConfigVariable
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.RepeatSeconds;

        public bool Validate(string value) =>
            Parse.Float(value, out float seconds) && SettingRanges.Get("RepeatSeconds").Holds(seconds);

        public void Execute(string value)
        {
            Parse.Float(value, out float seconds);
            ZoneService.RepeatSeconds = seconds;
        }
    }
}
