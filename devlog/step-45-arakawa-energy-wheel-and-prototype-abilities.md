# Step 45 - Arakawa Energy Wheel And Prototype Abilities

## Date

2026-03-24

## Goal

Add the first minimal version of Arakawa as a battle-side support system: global energy tracking, top-bar HUD display, a wheel-style ability panel, and two prototype abilities that do not consume the player's move / main action state.

## Implemented

- Extended `GlobalGameSession` with global Arakawa energy:
  - `ArakawaMaxEnergy`
  - `ArakawaCurrentEnergy`
  - spend / restore helpers
  - runtime signal
- Included Arakawa energy in the current shared battle snapshot path so it stays aligned with the rest of the session runtime data.
- Added top-bar HUD support for:
  - current Arakawa energy
  - Arakawa wheel open button
- Added a simple wheel-style Arakawa panel with:
  - `build_wall`
  - `enhance_card`
  - center cancel button
- Added Arakawa ability mode handling to `BattleSceneController`.
- Arakawa abilities are now:
  - only usable during player phases
  - independent from move / attack / card action state
  - unavailable during enemy turns
- Added `build_wall`:
  - spends 1 Arakawa energy
  - targets an empty cell
  - spawns an indestructible wall obstacle
  - plays a blue pulse / glitch-like construct effect
- Added card enhancement support:
  - `BattleCardEnhancementDefinition`
  - mutable `BattleCardInstance` enhancement state
  - per-card prototype enhancement table
- Added `enhance_card`:
  - spends 1 Arakawa energy
  - enhances one hand card
  - updates the card definition in-place for that instance
  - adds blue mask / pulse feedback on the card view

## Behavior

- Arakawa energy now appears in the battle HUD top bar.
- Opening the Arakawa wheel does not mark the player as moved or acted.
- Building a wall and enhancing a card both leave the player's normal movement / action flow intact.
- Enhanced cards now visibly differ from normal cards and carry modified battle effects.
- Current prototype card enhancement table covers the whole built-in debug deck.

## Quick Validation

- Run `dotnet build` and confirm the battle scripts compile cleanly.
- Start a battle and confirm the top HUD shows Arakawa energy.
- Open the Arakawa wheel during `PlayerMove` and `PlayerAction` and confirm it is unavailable during `EnemyTurn`.
- Use `build_wall` on an empty tile and confirm an indestructible obstacle is created with the blue construct pulse.
- Use `enhance_card` on a hand card and confirm:
  - Arakawa energy decreases
  - the card gains the blue enhancement styling
  - the modified card effect is used when the card is played

## Result

This step turns Arakawa from a planned system into the first working support layer in battle: the resource is now global, the wheel UI is available from the HUD, abilities are independent from the player's main action state, and both terrain creation and card enhancement have playable prototype implementations.
