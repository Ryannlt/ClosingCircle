# ClosingCircle

A shrinking zone for Holdfast. The zone closes over the round and anyone outside it is slapped.

Install on both the server and the client. The server does the damage, the client draws the wall.

```
mods_installed <workshopId> #closing circle
load_mod <workshopId> #closing circle
```

## Requirements

The rotation must have a round timer. `round_time_minutes -1` gives the game no clock to report, so the mod
logs a warning and stays inactive.

## Config variables

Use `mod_variable` for every rotation or `mod_variable_local` for one.

| Variable | Data | Default |
| --- | --- | --- |
| `EnableCircle` | `true` or `false` | `true` |
| `EnableDebugLogging` | `true` or `false` | `false` |
| `Shape` | `Circle` `Triangle` `Square` `Pentagon` `Hexagon` `Heptagon` `Octagon`, or a side count of 3 or more | `Circle` |
| `Rotation` | degrees to turn the shape | `0` |
| `StartRadius` | metres from centre to edge before the first stage | `200` |
| `StartCentre` | `x,z` | `0,0` |
| `AddStage` | `fromTime,toTime,radius,centreX,centreZ`, or `fromTime,toTime,radius,mode` | none |
| `Damage` | damage dealt on leaving the zone | `300` |
| `RepeatSeconds` | seconds between repeat hits while outside, `0` for one hit per crossing | `0` |
| `Solid` | `true` or `false`, a ring of colliders along the boundary | `false` |
| `Hud` | `true` or `false`, on-screen zone status for every player | `true` |
| `Color` | `r,g,b`, each 0 to 255 | `255,60,60` |
| `Opacity` | 0 to 100 | `24` |
| `Height` | metres the wall rises above the ground | `40` |
| `Fade` | 0 to 1, the share of the height the wall fades over, counted from the top | `0.6` |
| `Announce` | `true` or `false`, broadcast a message when a stage starts closing | `true` |
| `Bisector` | `x,z,heading` in degrees clockwise from north, the fair line | none |
| `Spread` | 0 to 1, how much of the legal range a randomised centre may use | `1` |

`AddStage` may be repeated. Times are seconds remaining in the round, so `fromTime` is the larger number.
Radius always means centre to edge, so a square with radius 100 is a 200 metre box.

## Centre modes

A stage either states its centre or names a mode that decides one. The mode **replaces** the coordinates,
because a position that will not be used should not be written down.

| Written as | Where the centre lands |
| --- | --- |
| `from,to,radius,x,z` | Exactly there |
| `from,to,radius,Bisector` | Randomly along the line set by `Bisector` |
| `from,to,radius,Random` | Randomly inside the previous circle |

Every stage's circle is kept **inside the previous one**, so the next zone is always reachable from the current
one and no stage can strand the players inside it.

`Bisector` is the fair option. On a map where both spawns sit the same distance from a line, every point on
that line leaves them equidistant, so the zone can move without giving either side ground. Set the line with a
point and a bearing, which on a symmetric map is the map centre and the front's bearing:

```
mod_variable_local ClosingCircle:Bisector:0,0,90
mod_variable_local ClosingCircle:Spread:0.6
mod_variable_local ClosingCircle:AddStage:540,300,60,Bisector
```

`Spread` is the number a league publishes. **0 puts the zone in the same place every round** and **1 uses the
full legal range**, narrowing symmetrically in between.

Centres are decided **as early as they can be known** and pushed to every client, so a `preview` shows where
each stage will actually land rather than an estimate that jumps when the dice are rolled. `Bisector`, `Random`
and a written centre all need nothing but the previous circle, so they are settled up front. A mode that depends
on something only knowable later stops the chain: it and every stage after it stay undecided and are simply not
drawn, because everything after a stage nests inside it.

Deciding early does not make the zone jump. It holds the previous stage's shape until `fromTime`, then slides
across, so when a centre was chosen makes no difference to what players see.

### Keeping the line in reach

A `Random` stage before a `Bisector` stage can wander so far that the fair stage cannot get back to the line,
which quietly costs one side ground in the stage that decides the round. The mod prevents it: each stage gets a
**budget**, the furthest from the line it may end up, worked backwards from the next bisector stage. Budgets
grow the further back you are, because the stages in between have room to pull the centre back.

So **`Random` is narrower when a `Bisector` stage follows it**, and only then. The guard moves the dice, never
what you wrote: a `Fixed` centre is obeyed exactly, and if it strands a later bisector stage `validate` reports
that as an error rather than the mod silently moving it.

If a config leaves the line genuinely out of reach, the zone still stays reachable and the log names the stage.

## Example

```
mod_variable ClosingCircle:EnableCircle:true
mod_variable ClosingCircle:Damage:300

mod_variable_local ClosingCircle:Shape:Circle
mod_variable_local ClosingCircle:StartRadius:200
mod_variable_local ClosingCircle:StartCentre:0,0
mod_variable_local ClosingCircle:AddStage:540,480,100,0,0
mod_variable_local ClosingCircle:AddStage:420,360,40,30,20

mod_variable ClosingCircle:Color:255,60,60
mod_variable ClosingCircle:Opacity:24
mod_variable ClosingCircle:Height:40
mod_variable ClosingCircle:Fade:0.6
```

On a ten minute round, the zone holds at 200 for the first minute, closes to 100 between 540 and 480 seconds
remaining, holds there until 420, then closes to 40 by 360 while sliding its centre to `30,20`, and holds for
the rest of the round.

`0,0` is the middle of every stock Holdfast map.

## The solid wall

`Solid:true` builds a ring of colliders along the boundary on the client, and layer 18 is confirmed to stop
a player. The damage check stays the authority either way, and a wall can only ever bind a client running the
mod.

A collider cannot push: Holdfast resolves collisions only when the player moves, so a zone closing past someone
standing still passes through them. Two things follow.

The wall **stands itself down while you are outside it**, so you can walk back in, and re-arms once you are
clear of it. It also stands down entirely in free roam, which flies through the boundary like anything else, and
a free-roaming player is never slapped or pushed either.

And the server **walks you back in**, in small steps about thirty times a second, so it reads as movement
rather than a jump. The step matches whatever you are doing yourself, so a galloping horse is turned around at
the same pace as somebody on foot. That matters for the person being moved and more for everyone shooting at them. It slides
around anything in the way and will not walk you up a cliff. Because this half is server side it works on every
client, including anyone without the mod, which the collider cannot do.

**A solid zone never damages anyone.** Being put back is the whole consequence. Turn it off and leaving costs
health again.

The wall sits on layer 18, Players Out of Bounds, which collides with Player and Horse and nothing else, so
shots and melee cross the boundary untouched. That is why it beat Static Environment, which map walls use but
which would block fire across the line.

The mod logs how many colliders it built and on which layer, so a wall that did nothing is never confused with
a wall that was never made.

## The HUD

`Hud:true` draws a read-only status for every player running the mod, as two pills side by side:

```
CIRCLE RADIUS 191M      CLOSING TO 100M IN 34S     (while a stage is closing)
CIRCLE RADIUS 100M      NEXT PHASE IN 1:12         (between stages)
```

The right-hand pill says where the zone is heading while it closes, and how long you have while it is holding.
It disappears once the last stage has run, leaving the radius pill centred on its own.

They sit **top centre, under the game's own reinforcement pills**, because the corners belong to the HUD
elements players reposition in settings, and they are styled to match that row: black panel, white capitals, and
the font borrowed off a live Holdfast label at runtime rather than shipped. `Color` does not apply here — it
stays a wall setting.

The pills are parented into the game's own `Main Canvas` and follow the visibility of its `Top Info Bar`, so
they scale with the player's UI scale setting and stay off the spawn and faction screens where that bar is
hidden. They are placed first in the canvas so every other UI element draws over them. If the game ever renames
either object the mod falls back to its own overlay and says so in the log.

Client-side drawing fed by the same synchronised clock as everything else, so nothing extra is sent for it.

## Runtime commands

Admin commands, over `rc`. **The game console will report each one as unrecognised** because it dispatches
before handing the command to mods. That message is expected; the real answer arrives as a private message.

```
rc closingCircle stage add <from> <to> <radius> <x> <z>
rc closingCircle stage add <from> <to> <radius> <Bisector|Random>
rc closingCircle stage remove <index>
rc closingCircle stage list
rc closingCircle stage clear
rc closingCircle set <key> <value>
rc closingCircle preview [seconds]
rc closingCircle status
rc closingCircle validate
rc closingCircle push
```

`set` takes the config variable names above, so `set Opacity 40` and
`mod_variable ClosingCircle:Opacity:40` are the same thing. The key is the noun, since the command is already
the verb.

`validate` checks the whole config and answers before anyone spawns: a bisector stage that can never reach the
line, a written centre that strands one, a radius that does not shrink, overlapping stages, times outside the
round, a bisector mode with no line set, or a zone that costs nothing to leave. It reports `ERROR` for something
that will not work, `WARNING` for something probably unintended, and `NOTE` for the stages the fairness guard is
holding in. **It also runs itself once a round** and logs what it finds, so an admin who never types it is still
told.

`preview` draws the stages still to come at once, for the admin who ran it only, so a schedule can be walked
before the round starts. A stage stops being drawn once it has run, and one whose centre is not yet decided is
left out rather than guessed at. `push` re-sends the current zone to every client; there is no `reload`, because the game only
hands a mod its config at load and cannot be asked for it again.

Any change made this way is pushed to every client immediately, and a player who joins later is caught up when
they spawn.

## Building

Build the mod folder `ClosingCircle/ClosingCircle` with the uMod window. The `Editor` folder sits outside it and
is not part of the mod.

`.meta` files are not tracked, so Unity will generate its own on first import. Nothing here is referenced by
GUID — no scene, no prefab, no MonoBehaviour placed in one — so the fresh ones are as good as any. The uMod
export profile is not tracked either and has to be made by hand.

## Tests

The fixtures in `Editor/` run in the Unity Test Runner under EditMode: `PlanEvaluatorTests`,
`ZoneShapeTests`, `PlanCodecTests`, `CentreMathTests`, `PlanWindowTests`, `PlanValidatorTests` and
`PushMathTests`.
