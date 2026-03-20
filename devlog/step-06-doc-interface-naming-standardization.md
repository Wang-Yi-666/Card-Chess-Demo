# Step 06 - Doc Interface Naming Standardization

## Date

2026-03-20

## Goal

统一 `docs` 中同一功能的接口命名规范，避免同一系统同时出现 GDScript 风格、C# 风格和多套接口签名。

## Unified Rules

- 运行时类型、服务、控制器、命令、资源类型统一使用 `PascalCase`
- 方法名统一使用 `PascalCase`
- 目录与文件名统一对齐当前 C# 工程，使用 `Scripts/Battle/.../*.cs`
- 只有内容层字段、存档键名、表结构字段保留 `snake_case`

## Updated Docs

- `docs/项目接口文档.md`
- `docs/90度方格战棋架构设计.md`
- `docs/项目总体架构设计.md`

## Main Changes

- `initialize_from_layout(...)` 统一为 `InitializeFromLayout(...)`
- `has_line_of_sight(...)` 统一为 `HasLineOfSight(...)`
- `get_cells_by_rule(...)` 统一为 `GetCellsByRule(...)`
- `get_objects_in_cells(...)` 统一为 `GetObjectsInCells(...)`
- `filter_objects(...)` 统一为 `FilterObjects(...)`
- 文件列表从 `board_initializer.gd` 等命名统一为 `BoardInitializer.cs` 等 C# 文件名

## Notes

- `TargetingService` 以 `项目接口文档.md` 中的分层接口为准，架构文档已同步对齐，不再保留 `get_objects_in_shape` / `filter_attackable_objects` 这套并行命名。
