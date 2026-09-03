
namespace ClosingCircle.ConfigVariables
{
    public class EnableDebugLogging : IConfigVariable
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.EnableDebugLogging;

        public bool Validate(string value) => Parse.Bool(value, out _);

        public void Execute(string value)
        {
            Parse.Bool(value, out bool enabled);
            Logger.SetEnableDebugLogging(enabled);
        }
    }
}
