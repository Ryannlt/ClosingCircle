using ClosingCircle.Domain;
using System;
using UnityEngine;

// AddStage:from,to,radius,centreX,centreZ for a fixed centre, or AddStage:from,to,radius,mode when the mode
// decides the centre. The mode replaces the coordinates rather than sitting beside them, because writing a
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

            var mode = CentreMode.Fixed;
            var centre = Vector2.zero;

            if (parts.Length == FixedFields)
            {
                if (!Parse.Float(parts[3], out float x)) return false;
                if (!Parse.Float(parts[4], out float z)) return false;
                centre = new Vector2(x, z);
            }
            else
            {
                string name = parts[3].Trim();

                if (!Enum.TryParse(name, true, out mode))
                {
                    Logger.Log($"Rejected stage '{value}': '{name}' is not a centre mode. " +
                               "Use Bisector or Random, or give x,z for a fixed centre.", LogLevel.WARNING);
                    return false;
                }

                if (mode == CentreMode.Fixed)
                {
                    Logger.Log($"Rejected stage '{value}': a fixed centre needs coordinates. " +
                               "Write from,to,radius,x,z instead.", LogLevel.WARNING);
                    return false;
                }
            }

            stage = new Stage
            {
                FromTime = from, ToTime = to, Radius = radius, Centre = centre, Mode = mode
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
