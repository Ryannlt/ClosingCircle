using ClosingCircle.Domain;
using ClosingCircle.Systems;
using System.Collections.Generic;
using UnityEngine;

// Closes on everyone alive at once, counted as individuals, with no regard for which side they are on.
//
// The difference from Team is deliberate rather than a simplification. Team takes each side's center of mass
// and meets in the middle, so twenty against five gets the same answer as five against five. This one averages
// the bodies, so the side with more players on the field pulls the zone towards its own ground. That is the
// point of the mode: it follows where the fighting actually is, which is what you want for a free-for-all or
// a last-man-standing round where sides are not the thing being balanced.
//
// Live, like Team, so it cannot be rolled in advance: the resolver stops its walk here and the preview shows
// nothing beyond this stage. Nesting and the bisector budget are applied by the resolver to whatever this
// returns, so the stage is still reachable from the previous circle.

namespace ClosingCircle.Centers
{
    public class PlayersCenter : ICenterSelector
    {
        public CenterMode Mode => CenterMode.Players;

        public bool CanResolveEarly => false;

        public Vector2 Resolve(CenterContext context)
        {
            var everyone = new List<Vector2>();

            foreach (KeyValuePair<int, PlayerRegistry.Entry> player in PlayerRegistry.All)
            {
                if (!player.Value.Alive) continue;

                Vector2 position;
                if (!PlayerRegistry.TryGetGroundPosition(player.Key, out position)) continue;

                everyone.Add(position);
            }

            if (everyone.Count == 0)
            {
                Logger.Log($"Stage {context.Index} asks for a players center but nobody is alive. Holding the " +
                           "previous center.", LogLevel.WARNING);
                return context.PreviousCenter;
            }

            Vector2 center = CenterOfMass.Centroid(everyone);

            Logger.Log($"Stage {context.Index}: closing on {everyone.Count} player(s) regardless of side, " +
                       $"at ({center.x:0.#}, {center.y:0.#}).", LogLevel.INFO);

            return center;
        }
    }
}
