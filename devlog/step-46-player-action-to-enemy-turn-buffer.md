# Step 46 - Player Action To Enemy Turn Buffer

## Date

2026-03-24

## Goal

Prevent player damage feedback and immediate enemy retaliation from overlapping so heavily that the floating numbers become hard to read.

## Implemented

- Added a short post-action buffer between player action resolution and enemy turn entry.
- The delay is applied only when the player has actually committed an action and entered `TurnPost` through `MarkActed(...)`.
- End-turn transitions without a committed action do not use the extra buffer.

## Behavior

- After player attacks, defensive actions, or non-quick card actions, the game now waits briefly before enemy actions begin.
- This leaves a readable window for:
  - player-side damage numbers
  - hit reactions
  - card / action feedback
- Enemy-turn pacing remains unchanged once enemy actions start.

## Quick Validation

- Run `dotnet build` and confirm the battle scripts compile cleanly.
- Attack an enemy and confirm the enemy does not begin its next action on the exact same beat as the player's impact numbers.
- Play a non-quick card and confirm the same short gap appears before `EnemyTurn`.
- End the turn without acting and confirm the transition still feels immediate.

## Result

This step adds a small readability buffer after player actions so combat feedback lands in a clearer sequence instead of collapsing player and enemy damage moments into one unreadable visual burst.
