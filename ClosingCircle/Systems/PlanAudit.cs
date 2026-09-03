using ClosingCircle.Domain;
using System.Collections.Generic;
using System.Text;

// Runs the validator against whatever is configured right now, and is the one place that knows how to turn a
// finding into a log line or a reply. Server side: a client's plan is whatever was pushed to it, so validating
// there would only report on the server's own work.

namespace ClosingCircle.Systems
{
    public static class PlanAudit
    {
        private static bool _reported;
        private static int _auditedPlanVersion = -1;

        public static void Reset()
        {
            _reported = false;
            _auditedPlanVersion = -1;
        }

        public static List<Finding> Run() =>
            PlanValidator.Validate(ZoneService.Plan, new ValidationContext
            {
                HasBisector = ZoneService.HasBisector,
                BisectorPoint = ZoneService.BisectorPoint,
                BisectorHeading = ZoneService.BisectorHeading,
                Solid = ZoneService.Solid,
                Damage = ZoneService.Damage,
                RoundSeconds = ZoneService.RoundSeconds
            });

        // Once a round on the first tick that knows the round length, and again whenever the stage list changes
        // shape, so an rc edit that breaks the config says so as it is made rather than waiting to be asked.
        public static void ReportOnce()
        {
            if (_reported && _auditedPlanVersion == ZoneService.PlanVersion) return;

            _reported = true;
            _auditedPlanVersion = ZoneService.PlanVersion;

            List<Finding> findings = Run();

            if (findings.Count == 0)
            {
                Logger.Log("Config validated with nothing to report.", LogLevel.DEBUG);
                return;
            }

            for (int i = 0; i < findings.Count; i++) Logger.Log(findings[i].Message, LevelOf(findings[i].Level));
        }

        // One message, because every reply in the mod goes out as a single private message.
        public static string Describe(List<Finding> findings)
        {
            if (findings.Count == 0) return "config looks sound. Nothing to report.";

            var sb = new StringBuilder();
            for (int i = 0; i < findings.Count; i++)
            {
                if (i > 0) sb.Append(" | ");
                sb.Append(findings[i].Level.ToString().ToUpperInvariant()).Append(": ")
                  .Append(findings[i].Message);
            }

            return sb.ToString();
        }

        private static LogLevel LevelOf(FindingLevel level)
        {
            if (level == FindingLevel.Error) return LogLevel.ERROR;

            return level == FindingLevel.Warning ? LogLevel.WARNING : LogLevel.INFO;
        }
    }
}
