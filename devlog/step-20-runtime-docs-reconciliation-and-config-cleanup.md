# Step 20 - Runtime Docs Reconciliation And Config Cleanup

## Date

2026-03-20

## Goal

Bring the design docs into agreement with the actual project structure, remove leftover config corruption, and make the docs readable in the current Windows tooling environment.

## Implemented

- Reconciled all six files under `Docs/` against the real runtime architecture instead of the older planned architecture.
- Updated the docs to reflect the current project split:
  - map interaction prototype
  - standalone battle prototype
- Corrected the docs so they now describe the real default entry point:
  - `project.godot`
  - `Scene/Battle/Battle.tscn`
  - `GlobalGameSession`
- Expanded the docs to cover the actual battle runtime structure:
  - `Actors/`
  - `BoardTopology`
  - `BattleRoomTileSetFactory`
  - current `Battle.tscn` node chain
  - helper/debug classes that exist in the repository but are not wired into the default battle flow
- Removed the stray BOM ghost config line from `project.godot`.
- Re-saved the six `Docs/*.md` files as `UTF-8 with BOM` so they display correctly through default PowerShell `Get-Content`.

## Behavior

- The project docs now match the codebase that actually runs today.
- Unimplemented systems such as `BattleRequest`, `BattleResult`, cards, energy, combat actions, and map-battle return flow are no longer documented as if they already exist.
- The docs now explicitly distinguish between:
  - classes/resources used by the default runtime path
  - helper or older debug classes that still exist in the repository
- `project.godot` no longer contains the leftover corrupted config entry.

## Quick Validation

- Read all six `Docs/*.md` files through default PowerShell `Get-Content` and confirm Chinese text displays correctly.
- Re-scan docs against `Scripts/Battle`, `Scene/Battle`, and `Resources/Battle` to confirm the documented module list and startup path are accurate.
- Run `dotnet build` and confirm the project still builds successfully after the cleanup.

## Result

This step closes the gap between the repository documentation and the actual playable prototype, while also cleaning up a lingering config artifact and the document encoding issue that made the docs hard to review locally.
