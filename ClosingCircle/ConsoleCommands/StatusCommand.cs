using ClosingCircle.Core;

namespace ClosingCircle.ConsoleCommands
{
    public class StatusCommand : IConsoleCommand
    {
        public string Name => "status";
        public string Usage => "status";

        public bool Validate(string[] args, out string error)
        {
            error = null;
            return true;
        }

        public void Execute(int playerId, string[] args) =>
            GameFacade.Reply(playerId, $"{ZoneService.Describe()} revision={ZoneService.Revision}");
    }
}
