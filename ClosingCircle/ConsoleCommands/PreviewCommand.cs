using ClosingCircle.Core;
using ClosingCircle.Domain;
using ClosingCircle.Sync;

// Draws every planned stage at once so an admin can walk the whole schedule before a round starts. Only the
// caller sees it, because it is a setup aid rather than something to show a server full of players.

namespace ClosingCircle.ConsoleCommands
{
    public class PreviewCommand : IConsoleCommand
    {
        public string Name => "preview";

        public string Usage => "preview [seconds|on|off]";

        // PreviewArgs answers both this and the panel's toggle, so the two cannot disagree about the syntax.
        public bool Validate(string[] args, out string error) => PreviewArgs.TryParse(args, out _, out error);

        public void Execute(int playerId, string[] args)
        {
            PreviewArgs.TryParse(args, out float seconds, out _);

            // Sent first: a command whose whole job is showing somebody the schedule should not depend on the
            // catch-up having reached them yet.
            PlanBroadcaster.PushTo(playerId);

            GameFacade.Execute($"serverAdmin quietPrivateMessage {playerId} {PreviewSignal.Encode(seconds)}",
                               logResult: false);

            GameFacade.Reply(playerId, Describe(seconds));
        }

        private static string Describe(float seconds)
        {
            if (seconds <= 0f) return "preview off.";

            return seconds >= PreviewArgs.HoldSeconds
                ? $"previewing {ZoneService.Plan.Count} stage(s) until turned off."
                : $"previewing {ZoneService.Plan.Count} stage(s) for {seconds:0}s.";
        }
    }
}
