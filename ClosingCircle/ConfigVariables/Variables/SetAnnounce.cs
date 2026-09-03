namespace ClosingCircle.ConfigVariables
{
    public class SetAnnounce : IConfigVariable
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.Announce;

        public bool Validate(string value) => Parse.Bool(value, out _);

        public void Execute(string value)
        {
            Parse.Bool(value, out bool announce);
            ZoneService.Announce = announce;
        }
    }
}
