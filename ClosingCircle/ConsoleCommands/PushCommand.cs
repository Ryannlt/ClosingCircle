using ClosingCircle.Core;
using ClosingCircle.Sync;

// Named push rather than reload because a reload is impossible: PassConfigVariables is raised by the game at
// load and cannot be asked for again. Re-sending the current plan is the only refresh available.

namespace ClosingCircle.ConsoleCommands
{
    public class PushCommand : IConsoleCommand
    {
        public string Name => "push";
        public string Usage => "push";

        public bool Validate(string[] args, out string error)
        {
            error = null;
            return true;
        }

        public void Execute(int playerId, string[] args)
        {
            PlanBroadcaster.PushToAll();
            GameFacade.Reply(playerId, $"pushed revision {ZoneService.Revision} to every client.");
        }
    }
}
