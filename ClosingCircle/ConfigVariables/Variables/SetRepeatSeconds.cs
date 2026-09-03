namespace ClosingCircle.ConfigVariables
{
    public class SetRepeatSeconds : IConfigVariable
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.RepeatSeconds;

        public bool Validate(string value) => Parse.Float(value, out float seconds) && seconds >= 0f;

        public void Execute(string value)
        {
            Parse.Float(value, out float seconds);
            ZoneService.RepeatSeconds = seconds;
        }
    }
}
