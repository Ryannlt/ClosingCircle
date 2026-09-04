namespace ClosingCircle.Domain
{
    public enum CentreMode
    {
        // The centre written in the config, which is the default and today's behaviour.
        Fixed,

        // Anywhere along a configured fair line, which keeps two equidistant spawns equidistant.
        Bisector,

        // Anywhere inside the previous circle.
        Random,

        // Between the two teams, wherever they actually are. The only mode that reads live world state, so it
        // is the only one that cannot be decided before its stage begins.
        Players
    }
}
