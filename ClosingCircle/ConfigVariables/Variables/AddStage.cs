using ClosingCircle.Domain;
using System;
using UnityEngine;

// AddStage:from,to,radius,centerX,centerZ for a fixed center, or AddStage:from,to,radius,mode when the mode
// decides the center. The mode replaces the coordinates rather than sitting beside them, because writing a
// position for a stage that will not use it is noise that reads as though it means something.

namespace ClosingCircle.ConfigVariables
{
    public class AddStage : IConfigVariable
    {
        private const int FixedFields = 5;
        private const int ModeFields = 4;

        public ConfigCommandEnum CommandName => ConfigCommandEnum.AddStage;

        public bool Validate(string value) => TryRead(value, out _);

        public void Execute(string value)
        {
            if (!TryRead(value, out Stage stage)) return;
            ZoneService.AddStage(stage);
        }

        private static bool TryRead(string value, out Stage stage)
        {
            stage = default;

            string[] parts = value?.Split(',');
            if (parts == null || (parts.Length != FixedFields && parts.Length != ModeFields)) return false;

            if (!Parse.Float(parts[0], out float from)) return false;
            if (!Parse.Float(parts[1], out float to)) return false;
            if (!Parse.Float(parts[2], out float radius)) return false;

            var mode = CenterMode.Fixed;
            var center = Vector2.zero;

            if (parts.Length == FixedFields)
            {
                if (!Parse.Float(parts[3], out float x)) return false;
                if (!Parse.Float(parts[4], out float z)) return false;
                center = new Vector2(x, z);
            }
            else
            {
                string name = parts[3].Trim();

                if (!Enum.TryParse(name, true, out mode))
                {
                    Logger.Log($"Rejected stage '{value}': '{name}' is not a center mode. " +
                               "Use Bisector or Random, or give x,z for a fixed center.", LogLevel.WARNING);
                    return false;
                }

                if (mode == CenterMode.Fixed)
                {
                    Logger.Log($"Rejected stage '{value}': a fixed center needs coordinates. " +
                               "Write from,to,radius,x,z instead.", LogLevel.WARNING);
                    return false;
                }
            }

            stage = new Stage
            {
                FromTime = from, ToTime = to, Radius = radius, Center = center, Mode = mode
            };

            if (!stage.IsValid)
            {
                // Named rather than counted, because a config with several stages needs to say which one.
                Logger.Log($"Rejected stage '{value}': the end time must be lower than the start time and " +
                           "the radius must be above zero.", LogLevel.WARNING);
                return false;
            }

            return true;
        }
    }
}
