# Step 01 - Architecture Bootstrap

## Date

2026-03-20

## Goal

根据 `docs` 中的战棋架构设计、接口文档和需求表，先为战斗层搭建一套可扩展的基础框架，而不是直接堆功能脚本。

## Decisions

- 保留现有地图探索层脚本，不在本次改造里打断地图侧开发。
- 新增 `Scripts/Battle` 作为战斗域入口，按 `Board / Data / Turn` 分层。
- `Battle.tscn` 作为战斗层根场景，先接入最小可运行的棋盘管理框架。
- 当前阶段优先实现文档里的 P0 地基：
  - `BoardObject`
  - `BoardCellState`
  - `BoardState`
  - `BoardObjectRegistry`
  - `OccupancyRules`
  - `BoardQueryService`
  - `BoardInitializer`
  - `TurnActionState`

## Notes

- 文档里部分示例使用 `gdscript` 命名方式，本项目当前是 Godot C#，因此本次实现将用 C# 结构等价落地。
- 房间内容资源化编辑器链路先留出接口，首版允许 `BattleSceneController` 在未指定布局资源时生成一个调试布局，避免框架落地前没有可验证入口。
