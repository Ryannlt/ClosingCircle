using UnityEngine;

// The fair line between two spawns is the perpendicular bisector of the line joining them. Headings are degrees
// clockwise from north, the same as CenterMath.Direction, so Heading is that function run backwards.

namespace ClosingCircle.Domain
{
    public static class SpawnFrame
    {
        // 0 up to but not including 360, so a heading reads the same in every log line and config value.
        public static float Heading(Vector2 direction)
        {
            float heading = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
            return heading < 0f ? heading + 360f : heading;
        }

        // A quarter turn clockwise from the line between the spawns. Which way the axis points does not matter:
        // the opposite axis gives the opposite heading, which is the same line.
        public static float FairLineHeading(Vector2 spawnAxis) => Heading(new Vector2(spawnAxis.y, -spawnAxis.x));
    }
}
