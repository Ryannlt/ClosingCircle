namespace ClosingCircle.Domain
{
    // Ordinals go over the wire in PlanCodec, so a new mode is appended rather than inserted.
    public enum CenterMode
    {
        // The center written in the config, which is the default and today's behaviour.
        Fixed,

        // Anywhere along a configured fair line, which keeps two equidistant spawns equidistant.
        Bisector,

        // Anywhere inside the previous circle.
        Random,

        // Between the two teams, wherever they actually are. Reads live world state, so it cannot be decided
        // before its stage begins.
        Team,

        // On everyone alive at once, as individuals, with no regard for which side they are on. Also live, and
        // unlike Team it can be dragged by whichever side has more players left on the field.
        Players
    }
}
