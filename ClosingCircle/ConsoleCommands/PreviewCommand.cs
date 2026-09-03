using ClosingCircle.ConfigVariables;
using ClosingCircle.Core;
using ClosingCircle.Sync;
using UnityEngine;

// Draws every planned stage at once so an admin can walk the whole schedule before a round starts. Only the
// caller sees it, because it is a setup aid rather than something to show a server full of players.

namespace ClosingCircle.ConsoleCommands
{
    public class PreviewCommand : IConsoleCommand
    {
        private const float DefaultSeconds = 15f;
        private const float MaxSeconds = 300f;

        public string Name => "preview";
        public string Usage => "preview [seconds]";

        public bool Validate(string[] args, out string error)
        {
            error = null;
            if (args.Length == 0) return true;

            if (!Parse.Float(args[0], out float seconds) || seconds <= 0f || seconds > MaxSeconds)
            {
                error = $"seconds must be between 0 and {MaxSeconds:0}.";
                return false;
            }

            return true;
        }

        public void Execute(int playerId, string[] args)
        {
            float seconds = DefaultSeconds;
            if (args.Length > 0) Parse.Float(args[0], out seconds);

            // Sent first: a command whose whole job is showing somebody the schedule should not depend on the
            // catch-up having reached them yet.
            PlanBroadcaster.PushTo(playerId);

            GameFacade.Execute($"serverAdmin quietPrivateMessage {playerId} {PreviewSignal.Encode(seconds)}",
                               logResult: false);
            GameFacade.Reply(playerId, $"previewing {ZoneService.Plan.Count} stage(s) for {seconds:0}s.");
        }
    }
}
