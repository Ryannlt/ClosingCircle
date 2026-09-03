using ClosingCircle.Core;
using ClosingCircle.Domain;

// Tells players when the zone starts moving. Server side, and it fires on the frame the active stage changes
// rather than on every frame that a stage is active.

namespace ClosingCircle.Systems
{
    public static class StageAnnouncer
    {
        private static int _announced = -1;

        public static void Reset() => _announced = -1;

        public static void Step(float timeRemaining)
        {
            int active = PlanEvaluator.ActiveStageIndex(ZoneService.Plan, timeRemaining);
            if (active == _announced) return;

            _announced = active;
            if (active < 0 || !ZoneService.Announce) return;

            Stage stage = ZoneService.Plan.Stages[active];
            GameFacade.Broadcast($"The circle is closing to {stage.Radius:0}m.");
        }
    }
}
