using ClosingCircle.Domain;
using ClosingCircle.Visual.Menu;
using UnityEngine;

// Client side. A pushed plan simply overwrites whatever config produced, and because a push can only arrive
// after config has been applied, last write wins is the whole precedence rule.

namespace ClosingCircle.Sync
{
    public static class PlanReceiver
    {
        private static readonly PlanReassembler Reassembler = new PlanReassembler();

        public static void Reset() => Reassembler.Reset();

        // Returns true if the message was ours, so the caller knows to stop treating it as chat.
        public static bool OnMessage(string text)
        {
            if (!Reassembler.Accept(text, out string payload)) return false;

            if (!PlanCodec.TryDecode(payload, out ZoneState state))
            {
                Logger.Log("Received a zone push that could not be read. Keeping the current zone.",
                           LogLevel.WARNING);
                return true;
            }

            Apply(state);
            Logger.Log($"Applied a pushed zone: {ZoneService.Describe()}", LogLevel.DEBUG);

            // An open panel is showing these values, so it has to follow them. Staged edits survive the
            // rebuild, which is the point of holding them outside the widgets.
            if (ZonePanel.IsOpen) ZonePanel.Refresh();

            return true;
        }

        private static void Apply(ZoneState state)
        {
            ZoneService.Enabled = state.Enabled;
            ZoneService.Solid = state.Solid;
            ZoneService.ForceDisplay = state.ForceDisplay;
            ZoneService.Hud = state.Hud;

            ZoneService.Plan.StartRadius = state.StartRadius;
            ZoneService.Plan.StartCentre = state.StartCentre;
            ZoneService.Plan.Clear();
            if (state.Stages != null)
                foreach (Stage stage in state.Stages) ZoneService.Plan.Add(stage);

            ZoneService.Color = new Color(state.ColorR / 255f, state.ColorG / 255f, state.ColorB / 255f);
            ZoneService.Opacity = Mathf.Clamp01(state.OpacityPercent / 100f);
            ZoneService.Height = state.Height;
            ZoneService.Fade = state.Fade;
            ZoneService.Blur = state.Blur;

            // Both of these bump the versions the visual watches, so the mesh and ramp rebuild on their own.
            ZoneService.SetShape(state.Sides, state.Rotation);
            ZoneService.MarkLookChanged();

            // And the revision, which is what an open preview watches to know it has gone stale. Safe on a
            // client: only the server broadcasts, so this cannot echo back out.
            ZoneService.MarkChanged();
        }
    }
}
