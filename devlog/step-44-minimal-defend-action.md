# Step 44 - Minimal Defend Action

## Date

2026-03-23

## Goal

Add a simple standalone defend action that reduces incoming damage until the unit's next own turn, while keeping the implementation extensible for future talent-driven defense bonuses such as extra shield.

## Implemented

- Added `DefenseActionDefinition` as the reusable defense-effect description layer.
- Added board-side defense stance data to `BoardObject`:
  - active damage reduction percent
  - expire-on-faction
  - expire-on-turn-index
- Added board-side defense methods:
  - `EnterDefenseStance(...)`
  - `ResolveTurnStart(...)`
- Updated incoming damage calculation so active defense reduces the final damage before shield / HP split.
- Current reduction rule:
  - reduce incoming damage by `50%`
  - round the resulting actual damage up
- Added `Def` to the HUD action column.
- Wired the player defend action through `BattleActionService` instead of hardcoding it directly in the controller.
- Added turn-start cleanup so defense expires at the start of that unit's next own turn.
- Mirrored defense state into `BattleObjectState` so hover UI can show when a unit is defending.

## Behavior

- Clicking `Def` applies a temporary defense stance and ends the player's action.
- The stance remains active through the opposing side's turn.
- At the start of the defender's next own turn, the effect expires automatically.
- Hovering a defending unit now shows its current defense reduction percent.
- The defense action already supports future extension points such as adding shield on defend.

## Quick Validation

- Run `dotnet build` and confirm the battle scripts compile cleanly.
- Use `Def`, let an enemy hit the player, and confirm incoming damage is reduced by half with upward rounding.
- Confirm the defense effect is still active during the enemy turn and disappears at the start of the player's next turn.
- Hover the player before and after expiration and confirm the defense indicator updates correctly.

## Result

This step adds the first standalone defend action to the prototype and does it through a reusable definition layer rather than a one-off special case, which keeps the door open for later talent modifiers like extra shield, retaliation, or defense-triggered secondary effects.
