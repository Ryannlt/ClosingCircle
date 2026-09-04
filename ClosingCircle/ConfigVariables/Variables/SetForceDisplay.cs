// ForceDisplay:true or false. Makes every client draw the server's circle rather than its own look settings.
// Admins keep their own, so they can still see the zone the way a player reporting a problem sees it.

namespace ClosingCircle.ConfigVariables
{
    public class SetForceDisplay : IConfigVariable
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.ForceDisplay;

        public bool Validate(string value) => Parse.Bool(value, out _);

        public void Execute(string value)
        {
            Parse.Bool(value, out bool force);
            ZoneService.ForceDisplay = force;
            ZoneService.MarkLookChanged();
        }
    }
}
