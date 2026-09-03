using ClosingCircle.ConfigVariables;
using ClosingCircle.Core;

// set <key> <value>, where the keys are exactly the config variable names. The validation is the config
// variable's own, so a value accepted here is accepted in a config file and vice versa.

namespace ClosingCircle.ConsoleCommands
{
    public class SetCommand : IConsoleCommand
    {
        public string Name => "set";
        public string Usage => "set <key> <value>";

        public bool Validate(string[] args, out string error)
        {
            error = null;
            if (args.Length >= 2) return true;

            error = "set needs a key and a value. keys: " + ConfigManager.Keys();
            return false;
        }

        public void Execute(int playerId, string[] args)
        {
            // Values with commas arrive as one argument, but a stray space would split them, so rejoin.
            string data = string.Join(string.Empty, args, 1, args.Length - 1);

            if (!ConfigManager.TryApply(args[0], data, out string error))
            {
                GameFacade.Reply(playerId, error);
                return;
            }

            ZoneService.MarkChanged();
            GameFacade.Reply(playerId, $"{args[0]} = {data}. {ZoneService.Describe()}");
        }
    }
}
