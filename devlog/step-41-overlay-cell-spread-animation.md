# Step 41 - Overlay Cell Spread Animation

## Date

2026-03-23

## Goal

Improve battle readability by making movement and attack indicators reveal outward from the acting unit instead of appearing as static full-cell highlights.

## Implemented

- Reworked `BattleBoardOverlay` highlight rendering into reusable animated cell-layer logic.
- Added shared animated reveal behavior for board indicator cells:
  - cells reveal in rings based on Manhattan distance from the origin
  - each individual cell expands from its center to fill the full tile
- Applied the same reveal system to:
  - reachable movement cells
  - normal attack target cells
  - card attack target cells
- Changed attack overlay generation so it now shows actual attack range cells instead of only already-valid enemy target cells.
- Added a darker per-cell border so indicator tiles read more clearly against the board.
- Animated preview path lines so segments now grow forward in path order instead of appearing instantly.
- Added exposed tuning parameters for:
  - reveal duration
  - per-ring delay
  - minimum reveal scale
  - path segment reveal duration
  - path segment delay
- Updated `BattleSceneController` so movement and attack overlays now pass the acting unit cell as the reveal origin.

## Behavior

- Movement range now spreads outward from the player instead of popping in all at once.
- Attack range highlights now use the same outward spread timing and remain visible even when no target is standing inside the range.
- Card targeting and normal attack targeting now share one visual expansion method instead of maintaining separate one-off draw logic.
- Preview path now grows segment by segment from the unit toward the hovered destination.

## Quick Validation

- Run `dotnet build` and confirm the battle scripts compile cleanly.
- Start a battle and enter movement mode; confirm reachable cells spread outward from the player.
- Enter normal attack mode and confirm attack target cells reveal with the same spread animation.
- Select a targeted attack card and confirm card target cells use the same animated reveal.

## Result

This step makes the board indicators feel more intentional and less debug-like while also consolidating the spread animation into one reusable overlay method for future highlight types.
