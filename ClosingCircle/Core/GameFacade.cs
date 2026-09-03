using HoldfastBridge;
using System;
using System.Globalization;
using UnityEngine;

// The only route out to the game. Everything the mod does to the world goes through a console command here, so
// this file and HoldfastSharedMethodsInterface are the only two that name a Holdfast type.

namespace ClosingCircle.Core
{
    public static class GameFacade
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        private static IHoldfastGameMethods _gameMethods;

        public static bool Ready => _gameMethods != null;

        public static void Initialize(IHoldfastGameMethods gameMethods)
        {
            _gameMethods = gameMethods;
            Logger.Log(gameMethods == null ? "Game methods were not supplied." : "Game methods ready.",
                       gameMethods == null ? LogLevel.ERROR : LogLevel.INFO);
        }

        // logResult=false marks a routine command so a busy round does not flood the log.
        public static void Execute(string command, bool logResult = true)
        {
            if (_gameMethods == null)
            {
                Logger.Log($"Cannot execute '{command}', game methods are not ready.", LogLevel.ERROR);
                return;
            }

            bool success = _gameMethods.ExecuteConsoleCommand(command, out string output, out Exception exception);

            if (exception != null)
            {
                Logger.Log($"Failed to execute '{command}': {exception}", LogLevel.ERROR);
                return;
            }

            // A refused command is always worth a warning. Logging failures at DEBUG once hid a broadcast that
            // was failing every time it fired, and nothing said so unless debug logging happened to be on.
            if (!success)
            {
                Logger.Log($"Command refused: '{command}' -> {output}", LogLevel.WARNING);
                return;
            }

            if (!logResult) return;

            Logger.Log($"Executed '{command}' -> output='{output}'", LogLevel.DEBUG);
        }

        // The reason is passed as None on purpose: SendPMWithActionReason skips the private message for exactly
        // that value, and the wall is explanation enough. An empty reason would suppress it too but would leave
        // the admin action log reading 'Slap Reason: ' with nothing after it.
        public static void Slap(int playerId, int damage) =>
            Execute($"serverAdmin slap {playerId} {damage} None", logResult: false);

        // Moving a player is the only way a mod can displace one, and it is silent for a plain id with
        // coordinates: the chat notice in the teleport command only fires for the "teleport me to them" forms.
        public static void Teleport(int playerId, Vector3 position) =>
            Execute($"teleport {playerId} " +
                    $"{position.x.ToString(Invariant)},{position.y.ToString(Invariant)},{position.z.ToString(Invariant)}",
                    logResult: false);

        // Command feedback. Visible rather than quiet, because the game console shows the admin an error for
        // every command it does not recognise, so this is the only place they learn it actually worked.
        public static void Reply(int playerId, string message)
        {
            if (playerId < 0)
            {
                Logger.Log(message, LogLevel.INFO);
                return;
            }

            Execute($"serverAdmin privateMessage {playerId} {message}", logResult: false);
        }

        // Top-level command, not a serverAdmin subcommand. ExecuteConsoleCommand passes an admin id of -1,
        // which the game reads as a game message rather than an admin speaking.
        public static void Broadcast(string message) =>
            Execute($"broadcast {message}", logResult: false);
    }
}
