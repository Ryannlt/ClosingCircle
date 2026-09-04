using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Rewrites the Built date in ClosingCircle/Core/BuildInfo.cs, which the panel shows along its bottom edge.
//
// An editor script, so it sits outside the mod folder and the uMod verifier never sees it. That matters: this
// uses System.IO, which is on the deny list and would fail the build outright if it shipped.
//
// Run it before a release build. Nothing runs it automatically, because uMod's build goes through its own
// engine rather than Unity's player pipeline and offers no hook to attach to.

namespace ClosingCircle.EditorTools
{
    public static class StampBuildInfo
    {
        private const string Path = "Assets/Mods/ClosingCircle/ClosingCircle/Core/BuildInfo.cs";
        private const string Pattern = "(public const string Built = \")([^\"]*)(\";)";

        [MenuItem("Tools/ClosingCircle/Stamp build date")]
        public static void Stamp()
        {
            if (!File.Exists(Path))
            {
                Debug.LogError($"[ClosingCircle] {Path} is not where it was expected, so nothing was stamped.");
                return;
            }

            string source = File.ReadAllText(Path);
            string today = DateTime.Now.ToString("yyyy-MM-dd");

            var match = Regex.Match(source, Pattern);
            if (!match.Success)
            {
                Debug.LogError("[ClosingCircle] Could not find the Built constant, so nothing was stamped.");
                return;
            }

            if (match.Groups[2].Value == today)
            {
                Debug.Log($"[ClosingCircle] Build date is already {today}.");
                return;
            }

            File.WriteAllText(Path, Regex.Replace(source, Pattern, "${1}" + today + "${3}"));
            AssetDatabase.ImportAsset(Path);

            Debug.Log($"[ClosingCircle] Build date stamped {today}. Rebuild the mod to ship it.");
        }
    }
}
