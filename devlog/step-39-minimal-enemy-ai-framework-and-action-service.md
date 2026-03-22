# Step 39 - Minimal Enemy AI Framework And Action Service

## Date

2026-03-22

## Goal

Prepare the battle prototype for reusable enemy AI by separating temporary controller-coupled combat logic into smaller runtime services, and by adding a minimal enemy-turn framework driven by `AiId`.

## Implemented

- Added `AiId` to `BoardObjectSpawnDefinition`, `BoardObject`, and `BattleObjectState`.
- Added `BattleActionService` to host shared move / normal-attack logic instead of keeping it inside `BattleSceneController`.
- Added the minimal enemy AI framework:
  - `IEnemyAiStrategy`
  - `EnemyAiDecision`
  - `EnemyAiContext`
  - `EnemyAiRegistry`
  - `EnemyTurnResolver`
- Added the first built-in strategy:
  - `melee_basic`
- Updated the debug room enemy spawn path so current default enemies spawn with `AiId = "melee_basic"`.
- Rewired `BattleSceneController` so:
  - player input still drives the player turn
  - combat actions go through `BattleActionService`
  - enemy turns go through `EnemyTurnResolver`

## Behavior

- `Encounter` data still decides encounter metadata and room-pool selection; it no longer needs to be treated as future enemy-AI config.
- Current debug enemies now have a minimal runnable turn:
  - attack if a valid target is already in range
  - otherwise move toward the nearest hostile unit
  - otherwise wait
- `BattleSceneController` is still the battle entry and UI coordinator, but it no longer owns all move / attack / enemy-turn execution details directly.

## Quick Validation

- Run `dotnet build` and confirm the battle scripts compile cleanly.
- End the player turn and confirm enemy units now perform a minimal action instead of immediately skipping `EnemyTurn`.
- Confirm player-triggered movement and attack still work after the action logic moved into `BattleActionService`.
- Confirm removing or killing an enemy mid-turn does not break the rest of the enemy-turn iteration.

## Result

This step turns the old enemy-turn placeholder into a minimal reusable framework: AI selection now comes from `AiId`, shared combat actions live outside the scene controller, and future enemy types can be introduced by adding new strategies rather than writing one-off enemy scripts.
