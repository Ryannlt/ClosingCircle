using ClosingCircle.Core;
using ClosingCircle.Systems;

namespace ClosingCircle.ConsoleCommands
{
    public class ValidateCommand : IConsoleCommand
    {
        public string Name => "validate";
        public string Usage => "validate";

        public bool Validate(string[] args, out string error)
        {
            error = null;
            return true;
        }

        public void Execute(int playerId, string[] args) =>
            GameFacade.Reply(playerId, PlanAudit.Describe(PlanAudit.Run()));
    }
}
