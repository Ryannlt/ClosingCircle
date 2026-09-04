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
                                "stage remove <index> | stage list | stage clear | stage next";

        public bool Validate(string[] args, out string error)
        {
            error = null;
            if (args.Length == 0) { error = "stage needs a verb."; return false; }

            switch (args[0].ToLowerInvariant())
            {
                case "list":
                case "clear":
                case "next":
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

                case "next":
                    Advance(playerId);
                    return;

                case "clear":
                    ZoneService.ClearStages();
                    GameFacade.Reply(playerId, "cleared every stage.");
                    return;

                case "remove":
                    Parse.Int(args[1], out int index);
                    GameFacade.Reply(playerId, ZoneService.RemoveStage(index)
                        ? $"removed stage {index}. {ZoneService.Plan.Count} stage(s) left."
                        : $"there is no stage {index}.");
                    return;

                case "add":
                    TryRead(args, out Stage stage);
                    ZoneService.AddStage(stage);
                    ZoneService.MarkChanged();
                    GameFacade.Reply(playerId, $"added {stage}. {ZoneService.Plan.Count} stage(s).");
                    return;
            }
        }

        // Brings the next stage forward. Only Revision is bumped, not PlanVersion: the nesting is untouched,
        // so a centre already rolled for a later stage stays legal and must not be thrown away and re-rolled.
        private static void Advance(int playerId)
        {
            ZonePlan plan = ZoneService.Plan;
            float now = ZoneService.TimeRemaining;

            if (plan.Count == 0)
            {
                GameFacade.Reply(playerId, "there are no stages to advance to.");
                return;
            }

            if (!StageAdvance.TryPlan(plan, now, out int index, out float delta))
            {
                GameFacade.Reply(playerId, index < 0
                    ? "every stage has already started."
                    : "the next stage is already due.");
                return;
            }

            bool waits = StageAdvance.WaitsForCurrent(plan, now, index);

            plan.ShiftFrom(index, delta);
            ZoneService.MarkChanged();

            GameFacade.Reply(playerId, waits
                ? $"stage {index} brought forward {delta:0}s; it starts as soon as stage {index - 1} finishes."
                : $"stage {index} brought forward {delta:0}s; closing to {plan.Stages[index].Radius:0}m now.");

            Logger.Log($"Stage {index} advanced by {delta:0}s, and every stage after it with it.",
                       LogLevel.INFO);
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
