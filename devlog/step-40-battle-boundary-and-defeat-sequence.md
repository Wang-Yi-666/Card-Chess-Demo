# Step 40 - Battle Boundary And Defeat Sequence

## Date

2026-03-23

## Goal

Extend the minimal enemy-turn framework so enemies can actually damage the player, and add the first reusable boundary objects between map-side state and battle-side state.

## Implemented

- Added `BattleRequest` and `BattleResult` as minimal boundary objects.
- Updated `GlobalGameSession` to:
  - build and cache a pending `BattleRequest`
  - store the latest `BattleResult`
  - update player HP through a dedicated setter
- Updated `BattleSceneController` so battle startup now consumes a pending `BattleRequest` from `GlobalGameSession`.
- Updated `BattleActionService` so enemy attacks against the player now write HP back into `GlobalGameSession` instead of being overwritten on the next state sync.
- Kept the player object on the board at `0 HP` long enough to support a defeat sequence instead of removing it immediately.
- Added generic view animation hooks:
  - `PlayCue(...)`
  - `PlayDefeat()`
- Added a minimal battle defeat overlay and tweened failure animation.
- On player defeat, battle now writes a failed `BattleResult` back to `GlobalGameSession`.

## Behavior

- Enemy attacks now actually reduce player HP.
- If player HP reaches `0`, enemy-turn processing stops early.
- The battle scene plays a simple defeat animation:
  - player defeat cue
  - red flash overlay
  - centered defeat label reveal
- The current failed battle snapshot is now available through `GlobalGameSession.LastBattleResult`.

## Quick Validation

- Run `dotnet build` and confirm the battle scripts compile cleanly.
- Let an enemy attack the player and confirm player HP decreases instead of snapping back.
- Let the player die and confirm:
  - enemy actions stop
  - the defeat overlay animates
  - a failed `BattleResult` is written
- Confirm existing player attacks and enemy death removal still work after the player-HP persistence change.

## Result

This step turns the old isolated battle prototype into a cleaner boundary-aware runtime: player state can now enter battle through `BattleRequest`, leave through `BattleResult`, enemies can actually defeat the player, and the battle scene has a visible minimal failure sequence instead of silently continuing after `0 HP`.
