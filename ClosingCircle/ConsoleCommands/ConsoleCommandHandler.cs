using ClosingCircle.Core;
using System;
using System.Collections.Generic;

// Reads rc commands. The game console does not know our keyword, so it reports an unrecognised command to the
// admin and only then hands it to mods. That is why every answer here goes back as a private message.

namespace ClosingCircle.ConsoleCommands
{
    public static class ConsoleCommandHandler
    {
        public const string Keyword = "closingCircle";

        private static readonly Dictionary<string, IConsoleCommand> Commands =
            new Dictionary<string, IConsoleCommand>(StringComparer.OrdinalIgnoreCase);

        static ConsoleCommandHandler()
        {
            // uMod forbids reflection, so the registry is written out by hand.
            Register(new StageCommand());
            Register(new SetCommand());
            Register(new PreviewCommand());
            Register(new StatusCommand());
            Register(new ValidateCommand());
            Register(new PushCommand());
        }

        private static void Register(IConsoleCommand command) => Commands[command.Name] = command;

        public static void Process(int playerId, string input)
        {
            if (string.IsNullOrEmpty(input)) return;

            string[] parts = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            // Anything that is not ours passes silently. Every other mod's commands come through here too.
            if (!parts[0].Equals(Keyword, StringComparison.OrdinalIgnoreCase)) return;

            if (parts.Length == 1)
            {
                GameFacade.Reply(playerId, "commands: " + string.Join(" ", Names()));
                return;
            }

            if (!Commands.TryGetValue(parts[1], out IConsoleCommand command))
            {
                GameFacade.Reply(playerId, $"unknown command '{parts[1]}'. try: " + string.Join(" ", Names()));
                return;
            }

            string[] args = parts.Length > 2 ? parts[2..] : Array.Empty<string>();

            if (!command.Validate(args, out string error))
            {
                GameFacade.Reply(playerId, $"{error} usage: {Keyword} {command.Usage}");
                return;
            }

            command.Execute(playerId, args);
        }

        private static List<string> Names()
        {
            var names = new List<string>(Commands.Keys);
            names.Sort();
            return names;
        }
    }
}
