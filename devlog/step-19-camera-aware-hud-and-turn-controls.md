# Step 19 - Camera Aware HUD And Turn Controls

## Date

2026-03-20

## Goal

Fix the battle status HUD placement by reserving screen space through the main camera, and improve the debug test loop with explicit move-point and end-turn controls.

## Implemented

- `BattleSceneController` now configures the battle `Camera2D` at runtime:
  - reads `BattleRoomTemplate.BoardSize`
  - reserves a right-side HUD column
  - computes `Camera2D.Zoom` from viewport size
  - offsets the camera so the board stays on the left and the HUD stays on the right
- `BattleHudController` now exposes debug UI signals:
  - `EndTurnRequested`
  - `MovePointDeltaRequested(int delta)`
- HUD buttons were added:
  - `Move -`
  - `Move +`
  - `End Turn` / `Next Turn`
- Turn debug hotkeys were expanded:
  - `T`
  - `Enter`
  - keypad `Enter`
- `Scene/Battle/Battle.tscn` now explicitly enables its `Camera2D`

## Behavior

- On battle start, the camera zooms and shifts to leave room for the HUD.
- The board is framed on the left instead of being covered by the status panel.
- Move points can be tested through HUD buttons as well as keyboard input.
- End turn can be triggered from the HUD, and the button switches to `Next Turn` after the current turn has ended.

## Quick Validation

- Run the battle scene and confirm the board is on the left while the HUD sits on the right.
- Click `Move +` and `Move -` and confirm both the HUD and reachable overlay update.
- Click `End Turn` and confirm the current turn becomes ended and movement is blocked.
- Click `Next Turn` and confirm the turn index increases and movement becomes available again.

## Result

This step ties camera framing, HUD placement, and turn debug controls into a single usable battle test layout.
