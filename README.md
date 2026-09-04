# ClosingCircle

A shrinking zone for Holdfast. The zone closes over the round and anyone outside it is slapped.

Install on both the server and the client. The server does the damage, the client draws the wall.

```
mods_installed <workshopId> #closing circle
load_mod <workshopId> #closing circle
```

The rotation must have a round timer. `round_time_minutes -1` gives the game no clock, so the mod logs a
warning and stays inactive.

## Contents

- [Config variables](#config-variables)
- [Stages](#stages)
- [Centre modes](#centre-modes)
- [Runtime commands](#runtime-commands)
- [The settings panel](#the-settings-panel)
- [The HUD](#the-hud)
- [Example config](#example-config)

## Config variables

Use `mod_variable` for every rotation or `mod_variable_local` for one. Written as
`ClosingCircle:<key>:<value>`.

### Zone

| Variable | Data | Default |
| --- | --- | --- |
| `EnableCircle` | `true` or `false` | `true` |
| `Shape` | `Circle` `Triangle` `Square` `Pentagon` `Hexagon` `Heptagon` `Octagon`, or a side count of 3 or more | `Circle` |
| `Rotation` | degrees to turn the shape | `0` |
| `StartRadius` | metres from centre to edge before the first stage | `200` |
| `StartCentre` | `x,z` | `0,0` |
| `AddStage` | see [Stages](#stages) | none |

### Enforcement

| Variable | Data | Default |
| --- | --- | --- |
| `Damage` | damage dealt on leaving the zone | `300` |
| `RepeatSeconds` | seconds between repeat hits while outside, `0` for one hit per crossing | `0` |
| `Solid` | `true` or `false`, a barrier along the boundary instead of damage | `false` |

`Solid:true` never damages anyone. Leaving is prevented rather than punished, and players are walked back
inside instead. Free roam passes through the boundary and is never slapped or pushed.

### Fairness

| Variable | Data | Default |
| --- | --- | --- |
| `Bisector` | `x,z,heading` in degrees clockwise from north | none |
| `Spread` | 0 to 1, how much of the legal range a randomised centre may use | `1` |

### Appearance

| Variable | Data | Default |
| --- | --- | --- |
| `Color` | `r,g,b`, each 0 to 255 | `255,60,60` |
| `Opacity` | 0 to 100 | `24` |
| `Height` | metres the wall rises above the ground, 5 or more | `40` |
| `Fade` | 0 to 1, the share of the height the wall fades over, counted from the top | `0.6` |
| `Blur` | 0 to 100, how much the wall refracts what is behind it | `0` |
| `Hud` | `true` or `false`, on-screen zone status for every player | `true` |
| `ForceDisplay` | `true` or `false`, ignore each player's own appearance settings | `false` |

`ForceDisplay:true` makes everyone see the server's circle. A player's own settings are kept and return on a
server that does not force one. Admins keep theirs, so they can see the zone as a player reporting a problem
sees it. The HUD toggle stays the player's own choice either way.

### Logging

| Variable | Data | Default |
| --- | --- | --- |
| `Announce` | `true` or `false`, broadcast a message when a stage starts closing | `true` |
| `EnableDebugLogging` | `true` or `false` | `false` |

## Stages

```
ClosingCircle:AddStage:<fromTime>,<toTime>,<radius>,<x>,<z>
ClosingCircle:AddStage:<fromTime>,<toTime>,<radius>,<mode>
```

`AddStage` may be repeated, once per line, and each line adds one stage.

Times are **seconds remaining in the round**, so `fromTime` is the larger number. The zone holds still until
`fromTime`, slides to the new circle by `toTime`, then holds again.

Radius means centre to edge, so a square with radius 100 is a 200 metre box.

Every stage is kept **inside the one before it**, so the next zone is always reachable. A stage can only move
its centre by `previousRadius - thisRadius`. A stage that barely shrinks can barely move.

## Centre modes

A stage either states its centre as `x,z` or names a mode that picks one. The mode replaces the coordinates.

| Mode | Where the centre lands |
| --- | --- |
| `Bisector` | Randomly along the line set by `Bisector` |
| `Random` | Randomly inside the previous circle |
| `Players` | Between where the two teams actually are |

### Bisector

The fair option. On a map where both spawns sit the same distance from a line, every point on that line leaves
them equidistant, so the zone can move without giving either side ground. Set the line as a point and a bearing.

`Spread` controls how much of the legal range is used. `0` puts the zone in the same place every round, `1`
uses the full range, and values between narrow it symmetrically.

A `Random` or `Players` stage before a `Bisector` stage is held close enough to the line that the later stage
can still reach it. `validate` reports which stages are being held and by how much.

### Players

Takes each faction's centre of mass, then the point between them. Numbers do not count, positions do, so twenty
players against five gives the same centre as five against five.

Only players who have spawned and are alive count, so free-roaming admins and spectators never pull it. Bots
count like anyone else. If one side is wiped it closes on the survivors. If nobody is alive it holds still.

A `Players` centre is not known until its stage begins, so `preview` shows nothing from that stage onwards
until each one is reached.

## Runtime commands

Admin commands over `rc`. **The game console reports each one as unrecognised** because it dispatches before
handing the command to mods. That message is expected. The real answer arrives as a private message.

### Stages

| Command | Effect |
| --- | --- |
| `rc closingCircle stage add <from> <to> <radius> <x> <z>` | Add a stage with a written centre |
| `rc closingCircle stage add <from> <to> <radius> <mode>` | Add a stage with a centre mode |
| `rc closingCircle stage remove <index>` | Remove one stage by the index `stage list` prints |
| `rc closingCircle stage list` | List every stage |
| `rc closingCircle stage clear` | Remove every stage |
| `rc closingCircle stage next` | Start the next stage immediately |

`stage next` brings the next stage forward to now, and every stage after it moves by the same amount, so the
gaps between them are unchanged. Called while a stage is closing, the next one is queued to begin the moment
that one finishes.

### Settings

| Command | Effect |
| --- | --- |
| `rc closingCircle set <key> <value>` | Set one config variable |
| `rc closingCircle set <key> <value> <key> <value> ...` | Set several at once |

`set` takes the config variable names above, so `set Opacity 40` and `ClosingCircle:Opacity:40` are the same
thing. Several pairs are checked in full before any are applied.

### Information

| Command | Effect |
| --- | --- |
| `rc closingCircle status` | The whole current state on one line |
| `rc closingCircle validate` | Check the config and report problems |
| `rc closingCircle preview [seconds]` | Draw the stages still to come, for the caller only |
| `rc closingCircle preview on` / `off` | Hold the preview until turned off |
| `rc closingCircle push` | Re-send the current zone to every client |
| `rc closingCircle whoami` | Answers only if the server considers you an admin |

`validate` reports `ERROR` for something that will not work, `WARNING` for something probably unintended, and
`NOTE` for stages the fairness guard is holding in. It also runs once a round on its own and logs what it finds.

`preview` lasts 15 seconds with no argument. It uses the same colour, height, fade and blur as the live wall
and follows them as they change.

Any change made this way reaches every client immediately, and a player who joins later is caught up when they
spawn. There is no `reload`, because the game only hands a mod its config at load.

## The settings panel

**F3** opens and closes it, and so does the X in its corner. Escape closes an open dropdown and otherwise
leaves the panel alone, so it stays up over the game's own menu.

Opening the panel tells the game a UI has focus, so your character does not read the keyboard while you type.

| Tab | Who | Contents |
| --- | --- | --- |
| Display | Everyone | Colour, opacity, height, fade, blur, HUD, and a settings string to copy between machines |
| Zone | Admin | Every setting above, staged and sent on Apply |
| Stages | Admin | The stage list, a builder, and Clear all |
| Actions | Admin | Preview, validate, status, re-send, and advance |

Display settings are client side and are saved between rounds and servers. Nothing on the Zone tab is sent
until **Apply**, since those settings are seen by everybody. Touched rows turn red, and **Revert** discards
them. **Clear all** and **Advance** take a second press.

The admin tabs unlock by asking the server whether you are an admin. A whitelisted admin unlocks with one
press and nothing typed. If the server uses a password, a field appears for it.

## The HUD

`Hud:true` draws a read-only status for every player running the mod, as two pills at the top of the screen.

```
CIRCLE RADIUS 191M      CLOSING TO 100M IN 34S     while a stage is closing
CIRCLE RADIUS 100M      NEXT PHASE IN 1:12         between stages
```

The right pill disappears once the last stage has run. The pills sit under the game's own reinforcement row and
move up to fill the gap when that row is not shown.

Beneath them, `PRESS F3 FOR CIRCLE SETTINGS` shows until the panel has been opened, and returns once per
server session.

## Example config

A fifteen minute round, closing four times, with the last two stages on the fair line.

```
mod_variable_local ClosingCircle:EnableCircle:true
mod_variable_local ClosingCircle:Shape:Hexagon
mod_variable_local ClosingCircle:Rotation:15
mod_variable_local ClosingCircle:StartRadius:220
mod_variable_local ClosingCircle:StartCentre:0,0

mod_variable_local ClosingCircle:Damage:150
mod_variable_local ClosingCircle:RepeatSeconds:5
mod_variable_local ClosingCircle:Solid:false

mod_variable_local ClosingCircle:Bisector:0,0,90
mod_variable_local ClosingCircle:Spread:0.8

mod_variable_local ClosingCircle:Color:60,140,255
mod_variable_local ClosingCircle:Opacity:35
mod_variable_local ClosingCircle:Height:55
mod_variable_local ClosingCircle:Fade:0.35
mod_variable_local ClosingCircle:Blur:40
mod_variable_local ClosingCircle:ForceDisplay:true

mod_variable_local ClosingCircle:AddStage:780,690,170,20,-15
mod_variable_local ClosingCircle:AddStage:600,510,120,Random
mod_variable_local ClosingCircle:AddStage:420,330,80,Bisector
mod_variable_local ClosingCircle:AddStage:240,150,40,Bisector
```
