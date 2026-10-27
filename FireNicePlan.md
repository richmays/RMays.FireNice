# FireNice – Game Plan

> Status: planning only. No code yet.

## 1. Overview

**FireNice** is a 2D, side-view, grid-based puzzle game built as a **Windows Forms** app in **C# / .NET 8**
(`net8.0-windows`, `UseWindowsForms=true`).

The player controls a character on a rectangular grid of variable width and height. The goal of each
level is to **put out every fire** by pushing or dropping ice onto them. When ice puts out a fire, both
the ice and the fire disappear. The character walks left and right, climbs one-cell steps, falls safely
from any height, and can create or remove ice diagonally below-ahead with an action button. Ice can be
**attached** on its left and/or right side to a neighboring block or ice cube; attached ice can't be pushed.
Walking into fire kills the player. A **Reset** button puts the level back to how it started.

The game runs on a steady clock ("ticks"), so falling and sliding take visible time. The player can't
move while falling.

## 2. Confirmed requirements

| # | Requirement |
|---|-------------|
| R1 | Windows Forms app, C# .NET, project and plans live in the `FireNice` folder |
| R2 | Level is a grid of square cells; width and height vary per level. Every level has a solid border of blocks around the outside (no wrapping, no falling through the floor) |
| R3 | The player occupies one cell, faces left or right, and starts every level **facing right** (levels don't set it) |
| R4 | Left Arrow / **A** and Right Arrow / **D** turn and move the player (see R5–R6) |
| R5 | Pressing the direction opposite to the one the player faces only **turns** them; they stay in the same cell and the player's picture shows the new facing. Facing decides where the action button creates or removes ice |
| R6 | Pressing the direction the player already faces moves them one cell. **Holding** the key keeps walking at about **3 cells per second**. Holding the opposite direction turns, pauses briefly, then walks |
| R6a | If both Left and Right are held, the **last key pressed** wins |
| R6b | The player **can't move left or right while falling** |
| R7 | Cell contents: **Air**, **Block**, **Ice**, **Fire** (plus the player) |
| R8 | **Air**: anything can move through it freely |
| R9 | **Block**: nothing can pass through it; it never moves |
| R10 | **Ice** comes in 4 kinds: attached or unattached on the left, and attached or unattached on the right |
| R11 | An attached side must always have something next to it on that side (Ice or Block) |
| R12 | **Unattached ice** (neither side attached) can be pushed. Ice attached on **either** side can't be pushed at all |
| R13 | Attachment is two-way: when ice attaches to a neighboring ice cube, that neighbor also attaches back. Attached cubes act as **one larger ice piece** spanning several cells |
| R13a | Ice is affected by gravity **only if its piece isn't attached to a block**. A piece not attached to a block falls when **every** cell under it is empty (e.g. a 2-cell piece needs both cells below empty). Fire counts as empty |
| R13b | A falling piece stays joined together, but doesn't attach to new neighbors (ice or blocks) it lands next to |
| R13c | When an ice cube is removed (by the player's action, or by falling onto a fire), every ice cube it was attached to becomes unattached on that side |
| R14 | Pushed ice slides away from the player until it hits a wall (Block or Ice) or has empty space below it. Then it falls, and once it has fallen it doesn't slide anymore |
| R15 | When the player pushes ice, the player stays in place |
| R16 | Only a single ice cube can be pushed. Two ice cubes side by side can't be pushed |
| R17 | **Fire**: doesn't move on its own, but falls if whatever it stands on is removed |
| R18 | Ice (unless held by a block, see R13a), the player and fire are all affected by gravity |
| R19 | The player survives a fall from any height |
| R20 | The player climbs up automatically when walking into a block (or immobile ice) that has empty space above it |
| R21 | Ice landing on the player is harmless |
| R22 | The player dies only by moving onto a cell that has fire (walking or falling into it) |
| R23 | A fire is put out only when ice is pushed into it or ice falls onto it. Ice falls onto fire as if it were empty, then **both the fire and the ice disappear** |
| R24 | Fire can fall onto ice and survives; the ice also stays |
| R25 | The level is won when every fire is out **and the player is alive**. If the player dies at the same time the last fire goes out, it's a loss |
| R26 | **Action button** (configurable in a key-binding file; Space by default) creates or removes ice one cell ahead and one cell down from the player, in the direction the player is facing |
| R27 | The action creates ice only if that cell is Air. If that cell has ice (of any kind), the action removes it instead |
| R27a | **Holding** the action button repeats it about **3 times per second**, so it keeps creating and removing ice in turn |
| R28 | Created ice is automatically attached on each side that has a Block (or jar, pipe or iron crate) or Ice next to it. Because the target is diagonal from the player, one side is always the block or ice the player is standing on, so created ice **always** attaches to the player's footing |
| R29 | There's no limit on how much ice the player can create or remove |
| R30 | Levels are saved as **human-readable text**, one character per grid cell, and also store the **grid width and height** and the **player's starting position** |
| R31 | If the player's starting position has air underneath, the player falls until they land |
| R32 | Gravity is checked as soon as a level is loaded, so ice (and fire) may fall right away |
| R33 | A **Reset** button puts the level back to its original state |
| R34 | **Jars** are fixed cells: never affected by gravity and never move. A jar is **unlit** (blue) or **lit** (red/orange). Levels can start with either |
| R35 | An **unlit jar** acts exactly like a block |
| R36 | If a fire is ever directly on top of an unlit jar, the jar becomes **lit**. A lit jar never becomes unlit |
| R37 | If the player is ever directly on top of a lit jar (walking, climbing or falling onto it), the player **dies** |
| R38 | If ice is ever directly on top of a lit jar (pushed, falling or created there), that ice **disappears** |
| R39 | **Iron crates** act like a single free ice cube: they can be pushed and are affected by gravity |
| R40 | A crate puts out a fire it's pushed into or falls onto, and the crate **remains**. Crates can never be destroyed |
| R41 | A crate can move across jars, lit and unlit, without harm |
| R42 | A pushed crate always moves one space, and keeps sliding only while the ground under it is ice |
| R43 | When ice or a crate is pushed, the player stays where they are; they don't step into the space it left |
| R44 | **Pipes** never fall or move. There are 6 kinds, each open on two of Up, Down, Left and Right: vertical, horizontal and four bends |
| R45 | Walking into a pipe that's open on the side facing the player enters it. Up enters a pipe above that's open at the bottom; Down enters a pipe below that's open at the top |
| R46 | Inside a pipe the player moves automatically until they come out the other end. If that exit is blocked, they turn around and come back out where they entered |
| R47 | While the player is inside, the pipe is drawn slightly larger to show where they are |

## 3. Game rules (detailed)

These turn the requirements into exact rules for the code. Items marked **(assumed)** are still
unconfirmed; see section 9.

### 3.1 Grid and edges
- Coordinates: `(x, y)`, with `x = 0` at the left and `y = 0` at the **top** (matches WinForms drawing).
- Every level has a solid border of **Blocks** around the outside, enforced by the level loader. Nothing
  wraps around, and nothing falls through the floor. As a safety net, the code also treats anything
  outside the grid as a Block.
- **"Touching" fire** means moving into a cell that has fire. Being next to a fire is safe.

### 3.2 Who blocks whom

| Moving thing → into cell holding | Air | Block | Ice | Fire | Player |
|---|---|---|---|---|---|
| **Player** (walking/falling) | moves | blocked / climb | push (if unattached and free behind) or climb | **player dies** | – |
| **Ice** (sliding/falling) | moves | stops | stops | **both disappear** | stops (rests on player) |
| **Fire** (falling) | moves | stops | stops (rests on ice; both survive) | stops | **player dies** |

### 3.3 Time: the game clock
The game moves forward in **ticks**, driven by a timer. Each tick is about **100 ms** (configurable),
so falling and sliding move at about 10 cells per second. **(assumed speed)**

On every tick, in this order:
1. **Sliding ice** moves one cell (3.6).
2. **Gravity** moves everything that's falling one cell, from the bottom row up (3.10).
3. **Win / lose** is checked (3.11).

Player input (steps, turns, actions) happens between ticks, as the player presses or holds keys.

- **Falling** means the cell under the player is empty (Air or Fire) and they're moving down. The player
  is falling from the moment their support disappears until they land.
- While the player is **falling**, Left/Right input is **ignored**. Turning and the action button are
  also ignored. **(assumed)**
- Other things (sliding or falling ice, falling fire) don't stop the player from acting. The player can
  walk while a pushed cube is still sliding away. **(assumed)**
- When the level loads, the clock starts right away, so anything unsupported, including the player,
  starts falling at once (R31, R32).

### 3.4 Facing and walking input
- The player starts every level facing **Right**.
- Press a direction (only while not falling):
  - If the player faces the **other** way, they turn to face this way and stay in the same cell. The
    player's picture flips to show the new facing.
  - If the player already faces this way, they take one step (3.5).
- **Holding** a direction key repeats every ~333 ms (about 3 cells per second, configurable).
  - The repeat is driven by a game timer, not Windows key auto-repeat, so the speed is the same on every PC.
  - If the hold started with a turn, the player turns, pauses briefly (one repeat interval), then starts
    walking. Example: facing left and holding Right, the player turns right, pauses, then walks right.
  - If both directions are held, the **last key pressed** wins. Releasing it hands control back to the
    other key if it's still held. **(assumed)**
  - Releasing the key stops walking.
  - If the player walks off a ledge while holding the key, they fall with no sideways movement. Once
    they land, walking continues at the next repeat. **(assumed)**

### 3.5 Player step
When the player steps, look at the target cell `T` beside them in the facing direction:

1. **Air**: the player moves into `T`. If there's nothing under `T`, they start falling on the next tick.
2. **Fire**: the player moves into `T` and **dies**.
3. **Ice that can move**: **push** (3.6). The player stays put.
   Ice can move only if it is **unattached on both sides**, isn't already moving, and the cell beyond it
   is Air or Fire.
4. **Block, or ice that can't move** (attached, moving, or Block/Ice behind it): **climb** (3.7).

### 3.6 Pushing ice
- Only unattached ice can be pushed. Once pushed, it **slides one cell per tick** in the push direction.
  On each tick:
  1. If the cell below the ice is Air or Fire, the ice **stops sliding and falls** (3.10). Once it falls
     it never slides again.
  2. If the cell ahead is **Fire**, the ice moves into it and **both disappear**.
  3. If the cell ahead is solid (Block, Ice or player), the ice **stops**.
  4. Otherwise, the ice moves ahead one cell.
- Only the pushed cube moves. Anything resting on top of it (ice, fire or player) is not carried along
  and falls by gravity once its support is gone.
- Ice with another ice cube right behind it can't be pushed, so the player climbs it instead.
- Pushed ice stays unattached when it stops, even if it ends up next to a block or ice. Only created ice
  gets attached.

### 3.7 Climbing
- The player climbs onto `T` if the cell above the player is Air **and** the cell above `T` is Air.
- The player ends up on top of `T` (`x ± 1, y − 1`). Otherwise the player doesn't move.
- Climbing is automatic; there is no jump key. The player can't climb two cells or more.

### 3.8 Ice attachment
- Every ice cube has two flags: `AttachedLeft` and `AttachedRight`. That gives 4 kinds:

  | Kind | Left | Right | Pushable? |
  |------|------|-------|-----------|
  | Free | – | – | Yes |
  | Left-attached | ✔ | – | No |
  | Right-attached | – | ✔ | No |
  | Both-attached | ✔ | ✔ | No |

- **Rule:** an attached side always has a Block or Ice next to it on that side.
- **Two-way.** If ice A is attached on its right to ice B, then B is attached on its left to A. Attached
  cubes form one **ice piece** that spans several cells in a row and acts as a single larger cube.
- **Gravity for ice pieces:**
  - A piece attached to a **Block** at either end is **held in place** and never falls.
  - A piece not attached to a block falls **as a whole**, one cell per tick, only when **every** cell
    under it is empty. If even one cell below is solid (Block, Ice or the player), the whole piece stays.
  - "Empty" means Air **or Fire**: fire doesn't hold ice up. Ice falls onto fire as if it were Air, and
    then the ice cube and the fire **annihilate each other** (both disappear).
  - Example: a 2-cell piece with Ice under one cell and Fire under the other does **not** fall, because
    the ice below holds it up. With Air under one cell and Fire under the other, it **does** fall.
  - A single free cube is just a 1-cell piece and follows the same rule.
- **Falling doesn't attach.** A falling piece stays joined to itself, but never attaches to new
  neighbors (ice or blocks) it falls past or lands beside. Its outer sides stay unattached.
- **Losing a neighbor.** When an ice cube disappears (removed by the action, or put out by landing on a
  fire), every cube it was attached to becomes unattached on that side. The piece splits into smaller
  pieces, and each one follows the gravity rules on its own (one may still be held by a block while the
  other falls).
- **Pushing:** any cube that's part of a piece (attached on either side) can't be pushed. Only a 1-cell
  piece with no attachments can.

### 3.9 Player action (create / remove ice)
When the player presses the action button (while not falling), look at the target cell
`A = (x + facing, y + 1)`. That's one cell ahead in the facing direction and one cell down.

| `A` holds | Result |
|-----------|--------|
| Air | Create an Ice cube in `A`, attached on every side that has Block or Ice next to it (3.8). Neighboring ice attaches back, joining the pieces into one |
| Ice (any kind) | Remove that Ice cube; neighbors lose their attachment on that side (3.8) |
| Block or Fire | Nothing happens |

- **Holding** the action button repeats it every ~333 ms (about 3 times per second, configurable). Each
  repeat looks at `A` again, so holding it over the same cell creates, removes, creates, removes… If
  created ice falls away, the next repeat creates another cube.
- **Created ice always attaches to the player's footing.** `A` sits right beside the cell under the
  player, and the player is always standing on a Block or Ice. So new ice is always attached on the side
  facing the player.
  - Standing on a **Block**: the new ice is held in place, even over a gap. This lets the player build
    bridges one cube at a time by stepping onto each new cube.
  - Standing on **Ice**: the new ice joins that ice's piece. It's held only if that piece is held by a
    block. Otherwise the new, wider piece falls if every cell under it is empty.
  - Note: creating ice next to a free cube the player is standing on attaches it, so that cube can no
    longer be pushed.
- **Removed ice** lets whatever stood on it (ice, fire) fall, and splits the piece it belonged to.
- There's no limit on how many times the action can be used. `A` is never the player's own cell or the
  cell directly under the player, so the action can't remove the player's own footing.
- Holding a direction and the action button at the same time works; each repeats on its own timer.
  **(assumed)**

### 3.10 Gravity (each tick)
Process from the **bottom row upward** so stacks fall together. Each thing falls at most one cell per tick.
- **Ice pieces** (3.8): a piece held by a block doesn't move. Any other piece falls one cell when every
  cell under it is empty. When a falling cube lands on **Fire**, that cube and the fire **both
  disappear**, its neighbors become unattached on that side, and the remaining parts are checked again on
  the next tick. Ice with the player below rests on the player (harmless).
- **Fire** with Air below falls one cell. Fire with Ice below rests on it (both survive).
  Fire with the player below means the **player dies**.
- **Player** with Air below falls one cell (and can't walk, turn or act). Player with Fire below means the
  **player dies**.

### 3.11 Win / lose
Checked at the end of every tick and after every player input:
- **Lost** as soon as the player dies. If the last fire goes out at the same moment, it's still a loss.
- **Won** if the player is alive and no Fire is left.
- After Won or Lost, the clock stops and all input except Reset (and menu actions) is ignored.

### 3.12 Jars
- Unlit (`J`) and lit (`L`) jars are fixed like blocks: they never fall or move, they stop sliding ice,
  and the player can climb onto them. Created ice attaches to them, and an ice piece attached to a jar
  is held up, the same as with a block. This applies to lit jars too.
- Jars only act on the cell **directly on top** of them. This is checked after every player input and
  at the end of every tick:
  - **Unlit jar with fire on top**: the jar becomes lit, and the fire keeps burning on top of it.
  - **Lit jar with ice on top**: that ice cube disappears, and its neighbors lose their attachment to it.
    Sliding ice disappears as soon as it moves onto the jar. Ice created there disappears at once.
  - **Lit jar with the player on top**: the player dies.
- Lit jars are not fires: they can't be put out and don't count toward winning. The level is won when
  every fire is out, whatever jars are lit.
- Standing or sliding beside a lit jar is safe; only the cell directly on top is affected.
- Reset puts every jar back to how the level started.

### 3.13 Iron crates
- An iron crate (`C`) is pushed like a free ice cube: the player pushes it when the cell beyond it is
  Air or Fire and it's resting on something. Two crates (or a crate and ice) side by side can't be
  pushed, so the player climbs instead. The player stays in place when pushing.
- **Sliding:** a pushed crate always moves one cell. After that, it keeps sliding one cell per tick only
  while the ground directly under it is ice. It stops on any other ground, at a wall, ice, another crate
  or the player, and falls if it runs out of ground (after which it doesn't slide again).
- **Gravity:** a crate falls like a single free ice cube. It rests on the player harmlessly.
- **Fire:** a crate pushed into fire, or falling onto fire, puts the fire out and takes its place. The
  crate is never destroyed. Fire falling onto a crate rests on it.
- **Jars:** crates sit on and slide across lit and unlit jars without harm.
- **Attached ice:** created ice attaches to a crate beside it, and level files can attach ice to a
  crate too (for example `C<`). A crate with ice attached becomes **part of that ice piece**, just like
  attached ice cubes join into one larger piece:
  - If the piece is attached to a fixed block, jar or pipe at either end, the whole piece is held in
    place, crate included. Removing the ice under the crate doesn't make it fall.
  - Otherwise the whole piece falls together, staying attached, but only when nothing is under any of
    its cells. So a crate resting on the ground holds its attached ice up over a gap, and a crate can
    hang from attached ice that's resting on something.
  - When a falling piece lands, a crate in it puts out fire under it and stays; an ice cube in it melts
    with the fire under it.
  - Removing the attached ice cube frees the crate again.
  - A crate in a piece can't be pushed.
  - Falling never creates new attachments.
- Crates are not ice: the action button does nothing to them, and they can't be part of the level's
  outer border.

### 3.14 Pipes
- Six kinds, each open on two sides: vertical (`|`, Up + Down), horizontal (`-`, Left + Right) and
  four bends written with number-pad digits, where the digit's corner on the keypad marks the two
  openings: `7` (Up + Left), `9` (Up + Right), `1` (Down + Left), `3` (Down + Right).
- Pipes are fixed like blocks: they never fall or move, and to everything except the player entering
  them they're solid. Things rest on them, sliding things stop at them, created ice attaches to them,
  and they can be part of the outer border.
- **Entering:** walking (Left/Right) into a pipe that's open on the side facing the player enters it.
  Walking into a side that isn't open climbs onto the pipe like a block. **Up** (or W) enters a pipe
  directly above that's open at the bottom; **Down** (or S) enters a pipe directly below that's open at
  the top. Up and Down fire once per press and do nothing else.
- **Traveling:** the player moves one cell per tick. In each pipe cell they leave through the other
  opening. If the next cell is a pipe open toward them, they continue into it, so connected pipes form
  longer paths.
- **Coming out:** if the next cell is air, the player steps out and gravity applies as normal. Coming
  out into fire kills the player, and coming out on top of a lit jar kills them as usual. Leaving
  sideways turns the player to face the way they came out.
- **Blocked:** if the next cell is anything else, the player turns around and travels back to the
  entrance, coming out where they went in (facing the other way if that's sideways). If the entrance is
  blocked too, they wait inside until it clears.
- While inside, the player ignores all input and isn't affected by gravity. The pipe protects them:
  falling fire or other hazards can't reach them.
- The player isn't drawn while inside; instead the pipe cell they're in is drawn slightly larger and
  brighter.

### 3.15 Solve
- **Game → Solve** searches for the fewest key presses that put out every fire, starting from the game
  exactly as it is now (including anything still falling or sliding).
- The search (`Core/Solver.cs`) is breadth-first. From each position it tries every key (Left, Right,
  Up, Down, Action), lets everything settle until nothing moves, and skips positions it has already
  seen. Positions are explored in order of how many presses they took, so the first win it finds is a
  shortest one. Positions where the player has died are dropped.
- **Limit:** because it waits for everything to settle after each press, it won't find solutions that
  need a key pressed while something is still falling or sliding.
- It runs in the background with a time limit (`SolverTimeLimitMs` in keybindings.json, default 60
  seconds; an existing keybindings.json that sets its own value keeps it). The status bar shows
  "solving..." and game keys are ignored until it finishes. Reset or loading a level cancels it.
- **Fallback:** the shortest search gets the first half of the time limit. If it hasn't finished by
  then (or hits its position limit), a second search looks for *any* solution for the rest of the
  time (`Solver.SolveAny`). It's a greedy best-first search: it always continues from the most
  promising position found so far, scored by fires left (1000 each) plus how far each ice cube and
  crate is from the nearest fire; ties go to the position found first, so it's deterministic. It
  usually finds a solution far sooner than the shortest search but often a longer one (level05: 20
  presses instead of 11; Round 10-10: 98 instead of 30). If it checks every reachable position
  without a win, the result is still "Impossible".
- **Speed:** each position is stored packed into a few 64-bit words: only the cells that can ever
  change (everything except blocks and pipes), 4 bits each, plus the player and pipe state
  (`GameState.Pack` / `Unpack`, internal). One scratch game per thread is loaded from those bits to try
  each key, so games are never cloned per press, and lookups use a custom hash table instead of a set
  of strings. Keys that can't do anything are skipped without trying them (Up/Down with no pipe
  opening that way, Action when the target isn't air or ice, every key while waiting in a blocked
  pipe). Each layer of positions (all the same number of presses from the start) is expanded on all
  cores, then the new positions are added in exactly the order a one-at-a-time search would add them,
  so the solution found is the same as before and the same every run. It gives up after 25 million
  positions (about 2 GB).
- **Benchmark:** `dotnet run -c Release --project FireNice.Bench -- [seconds] [file filter]
  [shortest-search seconds]` solves every level file and prints the outcome, presses, positions,
  time and positions per second (a shortest-search time of 0 tries only the fallback). On a 2-core /
  4-thread machine the shortest search went from about 56,000 to about 345,000 positions per second
  (about 180,000 on one thread). Every level solves in under half a second, with either search,
  except Round 10-6. Its shortest solution is longer than 55 presses: 60 seconds of shortest search
  checks every position up to 55 presses (21 million positions) without a win, and each extra press
  adds about 17% more positions. The fallback doesn't find it either (8 million positions in 60
  seconds): the solution has to drop four crates one at a time into the pit over the lit jars and
  climb back out of the shaft each time on an ice step, and a general-purpose score can't see that
  progress. Other general searches tried and dropped for the same reason: other scores, best-first
  with novelty, hill climbing (with and without backtracking) and beam search.
- **Results:**
  - Solution found: "Solution found!  Playback?" with Yes and No. No returns control to the player.
  - Only the fallback found one: "Couldn't find the shortest solution in time, but found one that
    takes N presses (there may be a shorter one).  Playback?" with Yes and No.
  - Every reachable position was checked without a win: "Impossible to solve from here!" (also shown
    if the player has already died).
  - Time ran out before either search found one: "Couldn't find a solution in time."
  - No fires left: "All the fires are already out!"
- **Playback** (`UI/Playback.cs`) replays the presses on the real game with everything at double
  speed: ticks, walking animation and the gap between presses are all halved. Like the solver, it
  waits for everything to settle before each press, so the replay matches what the solver found. Any
  game key stops the replay where it is and gives control back; Reset also resets the level.

### 3.16 Reset
- A **Reset** button (toolbar button, menu item and the `R` key) puts the level back exactly as it was
  loaded: original grid, original ice attachments, player at the start position facing right, status
  Playing.
- Reset works at any time, including after winning or losing, and while things are falling.
- It doesn't re-read the file. The game keeps an untouched copy of the loaded level and rebuilds the
  game from that copy. Held-key repeats are cancelled on reset.
- After reset, the clock restarts, so the start-of-level falling (R31, R32) happens again.

### 3.17 Rewind
- A **Rewind** button (toolbar button, menu item and the `Z` key) undoes the player's last input and
  everything it set off: falls, slides, melting, pipe trips, even dying. The game goes back exactly
  as it was just before that key press, including the way the player was facing.
- Every input since the level started (or was reset) is kept on a stack. Each rewind pops the latest
  one, so pressing Rewind again and again, or holding it, leads all the way back to the start.
  Holding the key repeats at half the walk repeat interval (twice as fast as walking).
- Playing on after a rewind pushes new steps on top of the ones still kept.
- Inputs that change nothing (walking into a wall, using the action on a block) don't count as a step.
- Rewind works after winning or losing. It stops a solution replay and cancels a solve in progress.
- The animation plays the step backwards at double speed: the player retraces the cells they went
  through, each taking half as long as it did going forwards, and anything that moved glides straight
  back at half a tick per cell.

## 4. Architecture

Game logic is kept separate from the UI so the rules can be unit-tested without a window. The core has
no timers of its own: the UI calls `Tick()`, and tests call it directly.

```
FireNice/
  FireNicePlan.md
  FireNice.sln
  FireNice/                      – WinForms app
    FireNice.csproj
    Program.cs                   – entry point, starts MainForm
    Core/
      CellType.cs                – enum: Air, Block, Ice, Fire
      Cell.cs                    – struct: Type, AttachedLeft, AttachedRight
      Direction.cs               – enum: Left, Right (with helper: Dx = -1 / +1)
      GameAction.cs              – enum: MoveLeft, MoveRight, Action, Reset
      GameStatus.cs              – enum: Playing, Won, Lost
      LevelData.cs               – untouched copy of a loaded level: name, width, height, cells, player start
      Level.cs                   – live grid: Width, Height, Cell[,] cells, Get/Set, IsSolid(x, y), CountFires()
      IcePieces.cs               – finds attached ice pieces, checks if a piece is held by a Block,
                                   checks if every cell under a piece is empty, splits pieces on removal
      SlidingIce.cs              – tracks cubes that are currently sliding and their direction
      Player.cs                  – X, Y, Facing, IsAlive, IsFalling
      GameState.cs               – built from LevelData; Press(Direction); UseAction(); Tick(); Reset()
    IO/
      LevelReader.cs             – parses a level text file into LevelData and validates it
      LevelWriter.cs             – writes LevelData back to the same text format
    Input/
      Settings.cs                – key bindings + timings; loads/saves keybindings.json
      InputRepeater.cs           – tracks held keys; fires direction steps and actions every ~333 ms;
                                   last-pressed direction wins; ignores Windows auto-repeat
    UI/
      MainForm.cs                – menu + toolbar (Reset, Load Level, Next Level, Exit), keyboard input,
                                   game clock Timer, win/lose messages, hosts GamePanel
      GamePanel.cs               – double-buffered panel that draws the level and player
    Levels/
      level01.txt …              – level files, copied to the output folder
    keybindings.json             – user-editable key bindings and timings (created with defaults if missing)
  FireNice.Tests/                – xUnit tests for Core and IO
    FireNice.Tests.csproj
    TestHelpers.cs               – build a level from a small text grid; RunUntilStill(game)
    MovementTests.cs
    FacingTests.cs
    FallingTests.cs
    ClimbTests.cs
    IcePushTests.cs
    IceAttachmentTests.cs
    IceActionTests.cs
    GravityTests.cs
    FireTests.cs
    ResetTests.cs
    LevelFileTests.cs
    InputRepeaterTests.cs
```

Fire and ice are stored in the grid as cells, because gravity moves them. Each cell carries its type plus
the two attachment flags (only used for ice). The player is stored separately because it's controlled by input.

### Responsibilities
- **GameState.Press(Direction)**: ignored unless `Status == Playing` and the player isn't falling. Turns
  the player or steps (3.4–3.7). Updates `Status`. Raises `StateChanged`.
- **GameState.UseAction()**: ignored unless `Status == Playing` and the player isn't falling. Applies 3.9.
  Updates `Status`. Raises `StateChanged`.
- **GameState.Tick()**: one clock step: sliding ice, gravity, win/lose (3.3). Raises `StateChanged` if
  anything changed. `IsStill` tells whether anything is still moving.
- **GameState.Reset()**: rebuilds everything from the stored `LevelData` (3.12).
- **LevelReader / LevelWriter**: read and write the text format in section 5. Reading then writing a
  level gives back the same file.
- **Settings**: maps keys to `GameAction`, plus the repeat interval (333 ms) and tick length (100 ms).
  Loaded from `keybindings.json` if present, otherwise defaults. The core never sees keys, only actions.
- **InputRepeater**: on KeyDown of a direction or the action key, fires once and starts repeating every
  interval while held. KeyUp stops it. Handles "turn, pause, then walk" and "last-pressed direction wins".
  Takes a clock so it can be tested without real time.
- **MainForm**: overrides `ProcessCmdKey` so arrow keys and Space aren't taken by focused controls. Runs a
  WinForms `Timer` that calls `GameState.Tick()`. Sends key presses to `InputRepeater`. The Reset button
  and `R` call `GameState.Reset()`. Shows the win/lose message when `Status` changes.
- **GamePanel**: `DoubleBuffered = true`. Cell size is `min(panelWidth / levelWidth, panelHeight / levelHeight)`,
  recalculated on `Resize`. Draws the player facing the right way. Shows a "Fires left: N" counter.

### Default settings (`keybindings.json`)

| Keys | Action |
|------|--------|
| `Left`, `A` | Turn left / move / push / climb left |
| `Right`, `D` | Turn right / move / push / climb right |
| `Up`, `W` | Enter a pipe above that's open at the bottom |
| `Down`, `S` | Enter a pipe below that's open at the top |
| `Space` | Action: create / remove ice |
| `R` | Reset level |
| `Z` | Rewind one step (hold to keep rewinding) |

| Timing | Default |
|--------|---------|
| Walk / action repeat interval | 333 ms (about 3 per second) |
| Tick length (fall and slide speed) | 100 ms (about 10 cells per second) |

## 5. Level file format

Plain text that a person can read and edit in Notepad. A short header gives the level's name, size and
the player's start, then the grid follows with one character per cell. The player always starts facing
right, so the file doesn't store facing.

```
; Lines starting with ';' are comments.
name: Bridge Practice
width: 12
height: 6
player: 1, 2
grid:
############
#..........#
#.......I..#
###====###.#
#....F....F#
############
```

- `player: x, y` is the start cell, counting from 0 at the top-left. That cell must be `.` (Air) in
  the grid. If it has air underneath, the player falls when the level starts.
- The grid doesn't contain a player character; the start position comes only from the header.
- Header keys can appear in any order before `grid:`. `name` is optional.

| Char | Meaning |
|------|---------|
| `.`  | Air |
| `#`  | Block |
| `I`  | Ice, free (unattached both sides) |
| `<`  | Ice, attached on the left only |
| `>`  | Ice, attached on the right only |
| `=`  | Ice, attached on both sides |
| `F`  | Fire |
| `J`  | Unlit jar |
| `L`  | Lit jar |
| `C`  | Iron crate |
| `\|` | Pipe, vertical (Up + Down) |
| `-`  | Pipe, horizontal (Left + Right) |
| `7`  | Pipe bend, Up + Left |
| `9`  | Pipe bend, Up + Right |
| `1`  | Pipe bend, Down + Left |
| `3`  | Pipe bend, Down + Right |

The example has a bridge of attached ice between two blocks, with a fire below it, and a free ice cube
that can be pushed right so it falls onto the second fire.

The reader rejects a level, with a clear message saying which line is wrong, if:
- `width`, `height`, `player` or `grid:` is missing, or a value isn't a number;
- the grid doesn't have exactly `height` rows of exactly `width` characters;
- an unknown character is used;
- the outer border (top row, bottom row, left and right columns) isn't all solid (`#`, `J` or `L`);
- the player's start is outside the grid or isn't on a `.` cell;
- there are no fires (`F`);
- an attached side doesn't have Block or Ice next to it;
- attachments between ice cubes don't match on both sides (if A is attached right, the ice to its right
  must be attached left).

Levels don't have to start "settled". Gravity runs from the first tick (R31, R32).

## 6. Rendering

- Draw order: background, cells, player.
- Block: grey/brick. Fire: orange/red flame shape. Player: simple figure that clearly faces left or right.
- Ice: light blue with a highlight. Attached sides are drawn with a thick "frozen" edge or small clamp
  marks so the player can tell pushable ice from attached ice.
- **(Optional)** Faintly highlight the action's target cell so the player can see where ice will be
  created or removed.
- **Smooth movement.** The rules still move things in whole cells; the screen glides between them.
  The game reports what caused each change (a reset, a key press or a tick) and which ice, fire and
  crates moved during each tick. `UI/Animator.cs` turns that into in-between drawing positions:
  - Player steps and climbs take about 150 ms (`WalkAnimationMs` in keybindings.json; 0 turns it off).
    A new key press skips the step in progress to its end, so quick presses never lag behind.
  - Falling, sliding and coming out of a pipe glide over exactly one tick, starting from wherever the
    thing is currently drawn, so a long fall looks like one continuous motion. A fall that starts in
    the middle of a step curves smoothly down from there.
  - Ice, fire and crates glide when they fall or slide. Things that appear or vanish (created, removed
    or melted ice, fires put out) still pop instantly.
  - Reset and loading a level jump straight to the new positions.
  - The window redraws about 60 times a second while anything is moving, and stops when it's all still.
- Sprites can come later.

## 7. Milestones

1. **Setup**: `FireNice.sln`, the WinForms project and the xUnit test project.
2. **Level files**: `LevelData`, `LevelReader`, `LevelWriter`, validation. Tests use small text grids,
   so this comes early.
3. **Core model**: `Cell`, `Level`, `Player` (with `Facing`), `GameState`, turning, walking, wall blocking.
4. **Clock and gravity**: `Tick()`, player falling (no input while falling), single ice cubes and fire
   falling, settling on load.
5. **Climbing**: automatic one-cell climb.
6. **Ice pushing**: sliding one cell per tick, stop at a wall or ice, fall off a ledge, two cubes can't
   be pushed.
7. **Fire**: ice and fire annihilate each other (pushed and falling), fire resting on ice, player death,
   falling fire, win/lose status.
8. **Ice attachment**: flags, attached ice can't be pushed, multi-cell pieces, held-by-block, piece
   falling, losing a neighbor.
9. **Ice action**: create (with auto-attach) and remove ice at the target cell.
10. **Reset**: rebuild from `LevelData`.
11. **Input and rendering**: `Settings`, `InputRepeater`, `GamePanel`, `MainForm` with the clock timer,
    toolbar Reset button, resize scaling, win/lose messages.
12. **Polish and later**: Load Level / Next Level, fires-left counter, level progression, a few
    hand-made levels; later smooth animation, sprites, sound, undo, an in-game key-binding screen.

## 8. Test cases to cover

Tests build a level from a small text grid, then call `Press`, `UseAction` and `Tick` directly.
`RunUntilStill` keeps ticking until nothing moves.

**Facing and movement**
- The player starts facing right.
- Facing left and pressing Right: the player turns and doesn't move. Pressing Right again: steps right.
- Walk into Air, into a Block, and into the border wall.

**Falling**
- A player starting with air underneath falls one cell per tick until they land, unhurt.
- Walk off a ledge: the player falls 1 cell or many cells and lands safely.
- While falling, Left/Right, turning and the action are ignored.
- After landing, input works again.

**Climbing**
- Climb onto a Block, onto attached ice, and onto immobile free ice.
- Blocked by a ceiling above the player or above the target. Can't climb a 2-high wall.

**Ice pushing**
- Push free ice across open floor: it slides one cell per tick to the wall and the player doesn't move.
- Sliding ice stops at another ice cube.
- Push ice off a ledge: it falls and doesn't slide after landing.
- Push ice against a wall: it doesn't move, and the player climbs it instead.
- Two cubes side by side can't be pushed.
- Ice attached on the left, the right or both sides can't be pushed (the player climbs instead).
- Push ice with ice, fire or the player resting on top: that thing falls.
- Ice falls onto the player: the player is unharmed and the ice rests on them.

**Ice attachment and gravity**
- Ice attached to a block stays up over a gap.
- A piece of attached ice held by a block at one end stays up.
- A 2-cell piece with no block and both cells below empty falls as one piece.
- A 2-cell piece with no block and only one cell below empty stays put.
- A 2-cell piece over Ice + Fire stays put. Over Air + Fire it falls, and that cube and the fire disappear.
- A falling piece lands next to a block or ice and doesn't attach to it.
- Removing a cube from the middle of a bridge splits it; each half falls or stays depending on whether
  it's still held by a block.
- Ice that is unsupported when the level loads falls on the first ticks.

**Fire**
- Push ice into fire on the same row: both disappear.
- Push ice off a ledge onto fire below: both disappear.
- Ice falls from height onto fire: both disappear.
- Fire falls onto ice: both survive.
- Walk into fire, fall onto fire, or have fire fall onto the player: the player dies.
- Remove the support under fire: the fire falls.
- Last fire put out with the player alive: Won.
- Last fire put out at the same moment the player dies: Lost.
- No input is accepted after Won or Lost (except Reset).

**Ice action**
- Standing on a block, target Air over a gap: ice is created, attached to the block, and stays.
- Target Air between two blocks or ice: ice is created, attached on both sides.
- Standing on free ice over a gap: new ice joins it into a 2-cell piece, and it falls if both cells
  below are empty.
- Creating ice next to a free cube makes that cube unpushable.
- Target is Ice (any kind): it's removed, neighbors lose that attachment, and anything resting on it falls.
- Target is Block or Fire: nothing happens.
- Facing left vs. right picks the correct target cell, including right after turning in place.

**Jars**
- Unlit jars act like blocks: sliding ice crosses them, the player climbs them, created ice attaches to them.
- Jars never fall.
- Fire landing on or starting on an unlit jar lights it, and the fire keeps burning.
- A lit jar stays lit after its fire is put out.
- Climbing or falling onto a lit jar kills the player. Standing beside one is safe.
- Ice pushed onto, falling onto or created on top of a lit jar disappears. In a piece, only that cube
  disappears and its neighbor is detached.
- Lit jars don't stop the level from being won.
- Reset turns a jar that was lit during play back to unlit.

**Iron crates**
- A pushed crate moves one space on solid ground; the player stays put.
- A pushed crate slides while the ground is ice, and stops where the ground stops being ice.
- Sliding crates stop at ice; two crates side by side can't be pushed.
- A crate pushed off a ledge falls.
- A crate pushed into fire or falling onto fire puts it out and remains.
- Fire falling onto a crate survives.
- Crates move onto and fall onto lit jars without being destroyed.
- A crate landing on the player is harmless.
- The action button does nothing to a crate.
- Created ice attaches to a crate, and the crate holds it up over a gap.
- A crate attached to ice that's held by a block, jar or pipe stays put when the ice under it is removed.
- A crate and its attached ice with nothing holding them fall together and stay attached.
- A crate hangs from attached ice that's resting on something.
- A crate with ice attached can't be pushed.
- A falling crate-and-ice piece lands on fire: the crate puts it out and stays.
- Reset puts the crate back.

**Pipes**
- Walking into an open end enters the pipe, and the player comes out the other side one tick later.
- Connected pipes are followed one cell per tick, including chains of bends over a wall.
- A blocked exit sends the player back out the entrance, facing the other way.
- Walking into a closed side climbs onto the pipe.
- Up enters a pipe open at the bottom; Down enters one open at the top, and the player falls out the
  bottom. Up and Down do nothing without a matching pipe.
- Input is ignored while inside a pipe.
- Pipes never fall. Created ice attaches to them.
- Coming out into fire kills the player; fire above a pipe can't reach a player inside it.
- Reset takes the player out of the pipe.

**Solve**
- Finds the shortest solution, from the start or from partway through a level.
- Doesn't change the game it's solving.
- Handles things still moving when Solve is pressed.
- Reports impossible (unreachable fire, or the player has died), already solved, and gave up (time).
- Finds a solution for every built-in level, no longer than the known one, and the solution wins when
  replayed.
- Playback replays a solution to a win, and waits for things to settle before each press.

**Reset**
- After moves, pushes, created/removed ice and fires put out, Reset restores the exact original grid,
  attachments, player position and facing right.
- Reset works after Won and after Lost, and the start-of-level falling happens again.

**Level files**
- A valid file loads with the right size, player start, cells and ice attachment flags.
- Reading a file and writing it back gives the same text.
- Each rejection rule in section 5 gives a clear error.

**Input repeating** (`InputRepeater`, tested with a fake clock)
- Holding the facing direction: one step now, then one step every ~333 ms.
- Holding the opposite direction: turn now, pause one interval, then step every ~333 ms.
- Holding both directions: the last pressed wins.
- Holding the action button: fires now, then every ~333 ms.
- Releasing a key stops its repeat.

## 9. Decisions

All earlier assumptions are confirmed:
- Turning and the action button are ignored while the player is falling.
- The player can act while other things are sliding or falling.
- Falling and sliding move at about 10 cells per second.
- No extras (undo, move counter, level select, level editor) for now.
