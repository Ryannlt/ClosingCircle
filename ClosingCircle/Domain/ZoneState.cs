using System.Collections.Generic;
using UnityEngine;

// A flat snapshot of everything a client needs to draw the zone. Kept separate from ZoneService so the wire
// format has no dependency on mutable global state, which is also what lets the codec be tested outside Unity.

namespace ClosingCircle.Domain
{
    public struct ZoneState
    {
        public bool Enabled;
        public bool Solid;
        public bool Hud;

        // The server insisting its own look is what everyone sees.
        public bool ForceDisplay;

        public int Sides;
        public float Rotation;

        public float StartRadius;
        public Vector2 StartCentre;
        public List<Stage> Stages;

        // Color channels stay 0-255 and opacity stays a percentage, matching both the config and the wire.
        public int ColorR;
        public int ColorG;
        public int ColorB;
        public float OpacityPercent;

        public float Height;
        public float Fade;
        public float Blur;
    }
}
