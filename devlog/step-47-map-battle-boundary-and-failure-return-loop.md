# Step 47 - Map Battle Boundary And Failure Return Loop

## Date

2026-03-24

## Goal

Finish the most important map-side interfaces so the project no longer treats map and battle as completely disconnected prototypes.

## Implemented

- Added `MapResumeContext` as the minimal map return payload.
- Extended `GlobalGameSession` to store:
  - pending map resume context
  - pending battle encounter id
- Implemented `Player.ReceiveHeal(int amount)` so map-side healing now writes directly back into shared player runtime state.
- Added `MapSceneController`:
  - restores player position from `MapResumeContext`
  - consumes the latest `BattleResult` on map load
- Reworked `SceneDoor` into a cleaner two-mode interface:
  - normal scene transition
  - battle entry
- In battle-entry mode, `SceneDoor` now:
  - builds and stores a `BattleRequest`
  - stores the pending encounter id
  - stores `MapResumeContext`
  - changes to the battle scene
- Updated `BattleSceneController` so it now:
  - consumes pending encounter id
  - returns to the stored map scene on battle failure
- Wired `Scene/Mainlevel.tscn` to the new map controller and set its existing door up as a battle-entry test door.

## Behavior

- The map prototype can now enter battle through a standard interactable door.
- The battle prototype now knows which map scene and position it should return to if the player loses.
- On defeat, the game returns to the original map scene and restores the player's previous map position.
- Heal stations on the map now operate against real shared player HP instead of a missing placeholder method.

## Quick Validation

- Run `dotnet build` and confirm the project compiles cleanly.
- Start `Scene/Mainlevel.tscn`, interact with the configured battle door, and confirm the game enters battle.
- Lose the battle and confirm:
  - the defeat sequence still plays
  - the scene returns to `Mainlevel`
  - the player reappears at the original entry position
- Use the healing station on the map and confirm shared player HP is restored through `ReceiveHeal(...)`.

## Result

This step turns the old map-side placeholders into a real minimal boundary: the map can now launch battle through a standard interface, battle can fail back into the map with position restoration, and shared player runtime data is respected on both sides.
