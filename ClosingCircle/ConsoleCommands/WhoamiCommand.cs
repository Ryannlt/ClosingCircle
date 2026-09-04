using ClosingCircle.Core;

// The panel's admin probe. It changes nothing and says nothing a player would see: its whole purpose is that
// the server answers at all, because RequestCommandExecute stays silent for anyone who is not an admin.
//
// The reply also carries the current plan, since an admin about to open the panel is exactly who needs it.

namespace ClosingCircle.ConsoleCommands
{
    public class WhoamiCommand : IConsoleCommand
    {
        public string Name => "whoami";
        public string Usage => "whoami";

        public bool Validate(string[] args, out string error)
        {
            error = null;
            return true;
        }

        public void Execute(int playerId, string[] args)
        {
            Sync.PlanBroadcaster.PushTo(playerId);
            GameFacade.Reply(playerId, "you have admin tools for the circle.");
        }
    }
}
