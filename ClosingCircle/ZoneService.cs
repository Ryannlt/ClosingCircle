using ClosingCircle.Domain;
using System.Text;
using UnityEngine;

// Everything mutable lives here and nothing else changes it. Config variables are one adapter onto this, the
// console commands are another, and an admin panel is a third that only has to emit rc strings.

namespace ClosingCircle
{
    public static class ZoneService
    {
        public const int DefaultDamage = 300;

        public static bool Enabled = true;
        public static bool Announce = true;
        public static ZonePlan Plan = new ZonePlan();
        public static ZoneShape Shape = ZoneShape.Circle;

        public static int Damage = DefaultDamage;
        public static float RepeatSeconds;
        public static bool Solid;
        public static bool Hud = true;


        public static Color Color = new Color(1f, 60f / 255f, 60f / 255f);
        public static float Opacity = 0.24f;
        public static float Height = 40f;
        public static float Fade = 0.6f;

        // The fair line, as a point and a heading in degrees clockwise from north. Configured rather than
        // derived, because a league may run maps whose spawns are themselves randomised.
        public static bool HasBisector;
        public static Vector2 BisectorPoint;
        public static float BisectorHeading;

        // How much of the legal range a randomised centre may use. 0 is the same place every round, 1 is all
        // of it.
        public static float Spread = 1f;

        // Color and opacity are configured apart and only meet here.
        public static Color Tinted => new Color(Color.r, Color.g, Color.b, Opacity);

        // Bumped whenever something the visual is built from changes, so it can rebuild only when it must.
        public static int LookVersion { get; private set; }
        public static int ShapeVersion { get; private set; }

        // Bumped by any mutation at all. The broadcaster watches this to know when clients are out of date.
        public static int Revision { get; private set; }

        // The longest round clock seen this round, which is the only way a mod learns the round length. Zero
        // until the first tick, so anything reading it has to cope with not knowing yet.
        public static float RoundSeconds { get; private set; }

        public static void NoteRoundClock(float timeRemaining)
        {
            if (timeRemaining > RoundSeconds) RoundSeconds = timeRemaining;
        }

        public static void ResetRoundClock() => RoundSeconds = 0f;

        // Bumped only when the stage list itself changes shape. The resolver watches this rather than the
        // revision, so editing something like opacity never re-rolls a circle that has already been decided.
        public static int PlanVersion { get; private set; }

        // These bump only what the visual watches. The revision is the adapter's job, so one command or one
        // config batch counts as one change rather than two.
        public static void MarkLookChanged() => LookVersion++;

        public static void MarkShapeChanged() => ShapeVersion++;

        // Adapters call this once they have finished mutating, rather than every setter bumping it, so a whole
        // config batch counts as one revision instead of fifteen.
        public static void MarkChanged() => Revision++;

        // A config batch arrives whole and re-arrives on every map change, so the stage list is rebuilt from
        // scratch each time rather than accumulating duplicates.
        public static void ClearStaged()
        {
            Plan.Clear();
            PlanVersion++;
        }

        public static void SetShape(int sides, float rotation)
        {
            Shape = new ZoneShape { Sides = sides, Rotation = rotation };
            MarkShapeChanged();
        }

        public static void AddStage(Stage stage)
        {
            Plan.Add(stage);
            PlanVersion++;
            Logger.Log($"Staged zone step: {stage}", LogLevel.INFO);
        }

        public static bool RemoveStage(int index)
        {
            bool removed = Plan.RemoveAt(index);
            if (removed)
            {
                PlanVersion++;
                MarkChanged();
            }
            return removed;
        }

        public static void ClearStages()
        {
            Plan.Clear();
            PlanVersion++;
            MarkChanged();
        }

        public static ZoneSnapshot Evaluate(float timeRemaining) => PlanEvaluator.Evaluate(Plan, timeRemaining);

        public static bool Contains(ZoneSnapshot zone, Vector2 point) =>
            Shape.Contains(zone.Centre, zone.Radius, point);

        public static Vector2 NearestInside(ZoneSnapshot zone, Vector2 point, float inset) =>
            Shape.NearestInside(zone.Centre, zone.Radius, point, inset);

        public static string Describe() =>
            $"enabled={Enabled} shape={Shape.Sides}-gon rot={Shape.Rotation:0.#} " +
            $"start={Plan.StartRadius:0.#} at ({Plan.StartCentre.x:0.#}, {Plan.StartCentre.y:0.#}) " +
            $"stages={Plan.Count} damage={Damage} solid={Solid} spread={Spread:0.##} " +
            (HasBisector
                ? $"bisector=({BisectorPoint.x:0.#}, {BisectorPoint.y:0.#}) at {BisectorHeading:0.#}deg"
                : "bisector=none");

        // One line per stage, indexed, because 'stage remove' takes the index this prints.
        public static string DescribeStages()
        {
            if (Plan.Count == 0) return "no stages. The zone will hold at its start size all round.";

            var sb = new StringBuilder();
            for (int i = 0; i < Plan.Count; i++)
            {
                if (i > 0) sb.Append(" | ");
                sb.Append('[').Append(i).Append("] ").Append(Plan.Stages[i]);
            }

            return sb.ToString();
        }
    }
}
