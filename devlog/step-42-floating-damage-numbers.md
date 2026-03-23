# Step 42 - Floating Damage Numbers

## Date

2026-03-23

## Goal

Make combat feedback easier to read by adding floating damage numbers for shield and HP damage, while keeping the system extensible for future damage types.

## Implemented

- Added `DamageApplicationResult` and `CombatImpact` so damage resolution now returns typed damage breakdown data instead of only mutating HP / shield silently.
- Updated `BoardObject.ApplyDamage(...)` so it now reports:
  - shield damage
  - health damage
- Added `BattleFloatingTextLayer` to the battle scene.
- Added typed floating-number rendering:
  - HP damage uses red text
  - shield damage uses gray text
- Added per-target burst layout so multiple numbers from the same hit do not overlap:
  - impacts from one hit spread horizontally
  - consecutive bursts on the same target stack upward in lanes
- Routed both normal attacks and card damage through the same damage-number output path.

## Behavior

- Hitting only shield shows a gray floating number.
- Hitting only HP shows a red floating number.
- Hitting shield and HP in the same attack shows two separate numbers.
- Multiple simultaneous damage numbers on the same target now offset themselves instead of drawing directly on top of each other.

## Quick Validation

- Run `dotnet build` and confirm the battle scripts compile cleanly.
- Hit a shielded enemy with damage lower than current shield and confirm only one gray number appears.
- Hit a shielded enemy with damage high enough to break shield and damage HP and confirm both gray and red numbers appear.
- Use both a normal attack and a damage card and confirm they both produce floating numbers through the same system.

## Result

This step makes combat feedback much more legible and establishes a typed combat-impact pipeline that can later be extended to healing, poison, burn, true damage, and other future number categories without rewriting the display layer.
