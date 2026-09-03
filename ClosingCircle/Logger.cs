using System;
using UnityEngine;

// Tagged logging. Lives in the root namespace on purpose: UnityEngine also has a Logger, and only the
// enclosing-namespace rule outranks a using directive.
// Deliberately a single class rather than MDS's interface and factory, because there is only
// one sink and the indirection would carry no weight here.

namespace ClosingCircle
{
    public enum LogLevel
    {
        INFO,
        WARNING,
        ERROR,
        DEBUG
    }

    public static class Logger
    {
        public static bool EnableDebugLogging { get; private set; }

        public static void SetEnableDebugLogging(bool enabled)
        {
            EnableDebugLogging = enabled;
            Log($"Debug logging {(enabled ? "enabled" : "disabled")}.", LogLevel.INFO);
        }

        public static void Log(string message, LogLevel level = LogLevel.INFO)
        {
            if (level == LogLevel.DEBUG && !EnableDebugLogging) return;

            string formatted = $"[ClosingCircle] [{DateTime.Now:HH:mm:ss}] [{level}] {message}";

            switch (level)
            {
                case LogLevel.WARNING:
                    Debug.LogWarning(formatted);
                    break;
                case LogLevel.ERROR:
                    Debug.LogError(formatted);
                    break;
                default:
                    Debug.Log(formatted);
                    break;
            }
        }
    }
}
