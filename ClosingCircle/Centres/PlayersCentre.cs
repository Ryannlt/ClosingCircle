using ClosingCircle.Domain;
using ClosingCircle.Systems;
using HoldfastSharedMethods;
using System.Collections.Generic;
using UnityEngine;

// Closes on where the players actually are: each faction's centre of mass, then the point between them.
//
// The only mode that reads live world state, so it is the only one that cannot be rolled in advance. That has
// two consequences the rest of the mod already handles: the resolver stops its walk here, and the preview
// therefore shows nothing beyond this stage rather than guessing at it.
//
// Nesting and the bisector budget are applied by the resolver to whatever this returns, so a stage placed here
// is still reachable from the previous circle and still leaves a later Bisector stage able to reach its line.

namespace ClosingCircle.Centres
{
    public class PlayersCentre : ICentreSelector
    {
        public CentreMode Mode => CentreMode.Players;

        public bool CanResolveEarly => false;

        public Vector2 Resolve(CentreContext context)
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

            Vector2 centre = TeamCentre.Midpoint(attackers, defenders, context.PreviousCentre);

            Describe(context.Index, attackers.Count, defenders.Count, centre);
            return centre;
        }

        // Said out loud because a centre that came from a fallback rather than from the players is exactly the
        // thing that looks like a bug three rounds later.
        private static void Describe(int index, int attackers, int defenders, Vector2 centre)
        {
            if (attackers == 0 && defenders == 0)
            {
                Logger.Log($"Stage {index} asks for a player centre but nobody is alive. Holding the previous " +
                           "centre.", LogLevel.WARNING);
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
                       $"at ({centre.x:0.#}, {centre.y:0.#}).", LogLevel.INFO);
        }
    }
}
