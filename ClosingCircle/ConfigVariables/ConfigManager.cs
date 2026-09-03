using System;
using System.Collections.Generic;

// Reads mod_variable lines. The whole batch arrives together and arrives again on every map change, so the
// staged plan is cleared first and rebuilt, which is what stops a rotation config accumulating duplicate stages.

namespace ClosingCircle.ConfigVariables
{
    public static class ConfigManager
    {
        public const string ModId = "ClosingCircle";

        private static readonly Dictionary<ConfigCommandEnum, IConfigVariable> Variables =
            new Dictionary<ConfigCommandEnum, IConfigVariable>();

        static ConfigManager()
        {
            // uMod forbids reflection, so the registry is written out by hand.
            Register(new EnableCircle());
            Register(new EnableDebugLogging());
            Register(new SetShape());
            Register(new SetRotation());
            Register(new SetStartRadius());
            Register(new SetStartCentre());
            Register(new AddStage());
            Register(new SetDamage());
            Register(new SetRepeatSeconds());
            Register(new SetSolid());
            Register(new SetHud());
            Register(new SetColor());
            Register(new SetOpacity());
            Register(new SetHeight());
            Register(new SetFade());
            Register(new SetAnnounce());
            Register(new SetBisector());
            Register(new SetSpread());
        }

        private static void Register(IConfigVariable variable) => Variables[variable.CommandName] = variable;

        // The single entry point for setting one tunable, shared by the config file and the rc 'set' command,
        // so there is exactly one validator per key.
        public static bool TryApply(string key, string data, out string error)
        {
            error = null;

            if (!Enum.TryParse(key, true, out ConfigCommandEnum parsed))
            {
                error = $"Unknown setting '{key}'.";
                return false;
            }

            if (!Variables.TryGetValue(parsed, out IConfigVariable variable))
            {
                error = $"No handler registered for '{parsed}'.";
                return false;
            }

            if (!variable.Validate(data))
            {
                error = $"Invalid value '{data}' for '{parsed}'.";
                return false;
            }

            variable.Execute(data);
            return true;
        }

        public static string Keys()
        {
            var names = new List<string>();
            foreach (ConfigCommandEnum key in Variables.Keys) names.Add(key.ToString());
            names.Sort();
            return string.Join(" ", names);
        }

        public static void Process(string[] entries)
        {
            if (entries == null || entries.Length == 0)
            {
                Logger.Log("Received an empty config array.", LogLevel.DEBUG);
                return;
            }

            ZoneService.ClearStaged();

            foreach (string entry in entries)
            {
                if (string.IsNullOrEmpty(entry)) continue;

                string[] parts = entry.Split(new[] { ':' }, 3);
                if (parts.Length < 2) continue;
                if (!parts[0].Equals(ModId, StringComparison.OrdinalIgnoreCase)) continue;

                string key = parts[1];
                string data = parts.Length > 2 ? parts[2] : string.Empty;

                if (!TryApply(key, data, out string error)) Logger.Log(error, LogLevel.WARNING);
            }

            ZoneService.MarkChanged();
            Logger.Log($"Config applied. {ZoneService.Describe()}", LogLevel.INFO);
        }
    }
}
