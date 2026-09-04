// Who built this and when, shown along the bottom of the panel whenever it has no command to report.
//
// Constants rather than anything discovered at runtime: reading assembly attributes means System.Reflection,
// which the uMod verifier refuses outright. Built is stamped by the editor helper in Editor/StampBuildInfo.cs,
// so it is one menu click before a release rather than a date anyone has to remember to type.

namespace ClosingCircle.Core
{
    public static class BuildInfo
    {
        public const string Name = "ClosingCircle";
        public const string Author = "Ryan";

        // Keep in step with the version in the uMod export profile, which is what the workshop shows.
        public const string Version = "1.0.0";

        public const string Built = "2026-09-03";

        public static string Line => $"{Name} {Version}   by {Author}   built {Built}";
    }
}
