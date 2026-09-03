using ClosingCircle.ConfigVariables;
using ClosingCircle.Core;
using ClosingCircle.Domain;
using System;
using UnityEngine;

// stage add|remove|list|clear. The one command that changes the schedule itself.

namespace ClosingCircle.ConsoleCommands
{
    public class StageCommand : IConsoleCommand
    {
        public string Name => "stage";
        public string Usage => "stage add <from> <to> <radius> <x> <z> | " +
                                "stage add <from> <to> <radius> <Bisector|Random> | " +
                                "stage remove <index> | stage list | stage clear";

        public bool Validate(string[] args, out string error)
        {
            error = null;
            if (args.Length == 0) { error = "stage needs a verb."; return false; }

            switch (args[0].ToLowerInvariant())
            {
                case "list":
                case "clear":
                    return true;

                case "remove":
                    if (args.Length != 2 || !Parse.Int(args[1], out _)) { error = "remove needs an index."; return false; }
                    return true;

                case "add":
                    if (args.Length != 6 && args.Length != 5)
                    {
                        error = "add needs from, to, radius and then either x z or a centre mode.";
                        return false;
                    }
                    if (!TryRead(args, out Stage stage)) { error = "add could not read those numbers."; return false; }
                    if (!stage.IsValid) { error = "the end time must be below the start time and the radius above zero."; return false; }
                    return true;
            }

            error = $"unknown verb '{args[0]}'.";
            return false;
        }

        public void Execute(int playerId, string[] args)
        {
            switch (args[0].ToLowerInvariant())
            {
                case "list":
                    GameFacade.Reply(playerId, ZoneService.DescribeStages());
                    return;

                case "clear":
                    ZoneService.ClearStages();
                    GameFacade.Reply(playerId, "cleared every stage.");
                    return;

                case "remove":
                    Parse.Int(args[1], out int index);
                    GameFacade.Reply(playerId, ZoneService.RemoveStage(index)
                        ? $"removed stage {index}. {ZoneService.DescribeStages()}"
                        : $"there is no stage {index}.");
                    return;

                case "add":
                    TryRead(args, out Stage stage);
                    ZoneService.AddStage(stage);
                    ZoneService.MarkChanged();
                    GameFacade.Reply(playerId, $"added {stage}. {ZoneService.DescribeStages()}");
                    return;
            }
        }

        private static bool TryRead(string[] args, out Stage stage)
        {
            stage = default;

            if (!Parse.Float(args[1], out float from)) return false;
            if (!Parse.Float(args[2], out float to)) return false;
            if (!Parse.Float(args[3], out float radius)) return false;

            var mode = CentreMode.Fixed;
            var centre = Vector2.zero;

            if (args.Length == 6)
            {
                if (!Parse.Float(args[4], out float x)) return false;
                if (!Parse.Float(args[5], out float z)) return false;
                centre = new Vector2(x, z);
            }
            else if (!Enum.TryParse(args[4], true, out mode) || mode == CentreMode.Fixed)
            {
                return false;
            }

            stage = new Stage
            {
                FromTime = from, ToTime = to, Radius = radius, Centre = centre, Mode = mode
            };
            return true;
        }
    }
}
