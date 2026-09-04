using ClosingCircle.ConfigVariables;
using ClosingCircle.Core;
using ClosingCircle.Domain;
using System.Text;

// set <key> <value>, where the keys are exactly the config variable names. The validation is the config
// variable's own, so a value accepted here is accepted in a config file and vice versa.
//
// Several pairs may be given at once, which is what the panel's Apply sends: one command and one answer for a
// batch of changes rather than one of each per setting.

namespace ClosingCircle.ConsoleCommands
{
    public class SetCommand : IConsoleCommand
    {
        public string Name => "set";

        public string Usage => "set <key> <value> [<key> <value> ...]";

        public bool Validate(string[] args, out string error)
        {
            error = null;
            if (args.Length >= 2) return true;

            error = "set needs a key and a value. keys: " + ConfigManager.Keys();
            return false;
        }

        public void Execute(int playerId, string[] args)
        {
            if (SetArgs.LooksLikePairs(args, ConfigManager.IsKey))
            {
                ApplyPairs(playerId, args);
                return;
            }

            string data = SetArgs.JoinValue(args);

            string error;
            if (!ConfigManager.TryApply(args[0], data, out error))
            {
                GameFacade.Reply(playerId, error);
                return;
            }

            ZoneService.MarkChanged();
            GameFacade.Reply(playerId, $"{args[0]} = {data}.");
        }

        private static void ApplyPairs(int playerId, string[] args)
        {
            // Checked in full first: a batch that applied half of itself and then complained would leave the
            // zone in a state nobody asked for.
            for (int i = 0; i < args.Length; i += 2)
            {
                string problem;
                if (ConfigManager.TryValidate(args[i], args[i + 1], out problem)) continue;

                GameFacade.Reply(playerId, problem);
                return;
            }

            var summary = new StringBuilder();

            for (int i = 0; i < args.Length; i += 2)
            {
                ConfigManager.TryApply(args[i], args[i + 1], out _);

                if (summary.Length > 0) summary.Append(", ");
                summary.Append(args[i]).Append(" = ").Append(args[i + 1]);
            }

            ZoneService.MarkChanged();
            GameFacade.Reply(playerId, summary.Append('.').ToString());
        }
    }
}
