using ClosingCircle.Domain;
using ClosingCircle.Systems;
using HoldfastSharedMethods;
using System.Collections.Generic;
using UnityEngine;

// Closes between the two sides: each faction's center of mass, then the point between them.
//
// Reads live world state, so it cannot be rolled in advance. That has two consequences the rest of the mod
// already handles: the resolver stops its walk here, and the preview therefore shows nothing beyond this
// stage rather than guessing at it. PlayersCenter is the same idea without the sides.
//
// Nesting and the bisector budget are applied by the resolver to whatever this returns, so a stage placed here
// is still reachable from the previous circle and still leaves a later Bisector stage able to reach its line.

namespace ClosingCircle.Centers
{
    public class TeamCenter : ICenterSelector
    {
        public CenterMode Mode => CenterMode.Team;

        public bool CanResolveEarly => false;

        public Vector2 Resolve(CenterContext context)
        {
            var attackers = new List<Vector2>();
            var defenders = new List<Vector2>();

            FactionCountry first = FactionCountry.None;

            foreach (KeyValuePair<int, PlayerRegistry.Entry> player in PlayerRegistry.All)
            {
                if (!player.Value.Alive) continue;

                Vector2 position;
                if (!PlayerRegistry.TryGetGroundPosition(player.Key, out position)) continue;

                // Which faction is which does not matter, only that the two are kept apart, so the first one
                // seen names the side rather than hard-coding an attacker and a defender.
                if (first == FactionCountry.None) first = player.Value.Faction;

                if (player.Value.Faction == first) attackers.Add(position);
                else defenders.Add(position);
            }

            Vector2 center = CenterOfMass.Midpoint(attackers, defenders, context.PreviousCenter);

            Describe(context.Index, attackers.Count, defenders.Count, center);
            return center;
        }

        // Said out loud because a center that came from a fallback rather than from the players is exactly the
        // thing that looks like a bug three rounds later.
        private static void Describe(int index, int attackers, int defenders, Vector2 center)
        {
            if (attackers == 0 && defenders == 0)
            {
                Logger.Log($"Stage {index} asks for a player center but nobody is alive. Holding the previous " +
                           "center.", LogLevel.WARNING);
                return;
            }

            if (attackers == 0 || defenders == 0)
            {
                Logger.Log($"Stage {index}: only one side is alive, so the circle is closing on the " +
                           $"{attackers + defenders} survivor(s) rather than between two teams.",
                           LogLevel.INFO);
                return;
            }

            Logger.Log($"Stage {index}: closing between {attackers} and {defenders} players, " +
                       $"at ({center.x:0.#}, {center.y:0.#}).", LogLevel.INFO);
        }
    }
}
