# Step 43 - Impact Gain Pipeline And Action Pacing

## Date

2026-03-23

## Goal

Improve combat readability further by making floating numbers easier to distinguish, extending the typed impact pipeline to non-damage gains, and slowing enemy action playback enough that the current prototype no longer resolves whole enemy turns in one visual burst.

## Implemented

- Extended `CombatImpactType` beyond damage so the same pipeline now supports:
  - `HealthDamage`
  - `ShieldDamage`
  - `HealthHeal`
  - `ShieldGain`
- Updated `BoardObject` so:
  - `ApplyDamage(...)` still returns typed impacts
  - `GainShield(...)` now returns typed gain impacts
  - `RestoreHealth(...)` was added for future healing sources
- Upgraded `BattleFloatingTextLayer`:
  - added slight random per-number jitter
  - added scale pop / settle animation
  - added per-impact stagger inside the same burst
  - added colors for healing and shield gain
- Routed shield-gain card resolution through `BattleActionService` so it uses the same impact display system.
- Added simple attack presentation motion:
  - attacker lunges slightly toward the target
  - target receives a short knockback in the opposite direction
- Updated enemy turn resolution to run asynchronously with pre / post action delays so actions no longer visually stack into one frame.

## Behavior

- Floating numbers are now easier to separate because they:
  - do not share exactly the same start position
  - do not all pop at exactly the same moment
  - have a small scale bounce before settling
- Shield-gain effects now use the same typed impact display path as damage.
- Enemy actions now play with a visible beat between units instead of resolving as one instant block.
- Normal attacks now have a clearer hit feel because of the attacker lunge and opposite-direction hit reaction.

## Quick Validation

- Run `dotnet build` and confirm the battle scripts compile cleanly.
- Hit a shielded target and confirm shield / HP numbers no longer appear as one indistinguishable stack.
- Play a shield card and confirm shield gain now appears through the same floating-number system.
- End the player turn and confirm enemy units act one by one with a short delay between actions.
- Confirm attack exchanges visibly show forward lunge and backward hit reaction.

## Result

This step turns the combat feedback from static prototype output into a more readable timing-aware presentation layer: impacts now share one expandable pipeline for both damage and gains, and enemy action pacing leaves enough visual space for numbers and hit reactions to be understood before the next unit acts.
