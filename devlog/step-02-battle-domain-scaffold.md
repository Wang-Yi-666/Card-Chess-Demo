# Step 02 - Battle Domain Scaffold

## Date

2026-03-20

## Goal

把战斗域从根目录散落脚本模式，整理成后续可扩展的模块化目录。

## Added Structure

- `Scripts/Battle/BattleSceneController.cs`
- `Scripts/Battle/BattleBoardDebugView.cs`
- `Scripts/Battle/Board/*`
- `Scripts/Battle/Data/*`
- `Scripts/Battle/Turn/*`

## Why

- 对齐 `docs/项目总体架构设计.md` 里建议的 `Battle Domain` 分层。
- 将运行时棋盘逻辑、数据定义、回合状态与场景入口解耦。
- 为后续接入 `TargetingService`、`TurnFlowController`、卡牌与能量系统保留清晰插槽。

## Result

战斗层现在已经有一个单独入口，不再需要把棋盘管理代码塞回现有地图探索脚本。
