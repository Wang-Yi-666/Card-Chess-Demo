# Step 16 - Shared Player State And Battle Object State

## Date

2026-03-20

## Goal

建立可被地图端和战斗端共用的全局角色状态入口，并在战斗内为每个对象生成独立的运行时状态，供 UI 和表现层读取。

## Implemented

- 新增 autoload：`GlobalGameSession`
  - 路径：`Scripts/Battle/Shared/GlobalGameSession.cs`
  - 当前保存玩家基础运行时数据：
	- `PlayerDisplayName`
	- `PlayerMaxHp`
	- `PlayerCurrentHp`
	- `PlayerMovePointsPerTurn`
- 提供快照接口：
  - `BuildPlayerSnapshot()`
  - `ApplyPlayerSnapshot(...)`
- 新增战斗内对象状态模型：
  - `BattleObjectState`
  - `BattleObjectStateManager`

## Result

- battle 域现在不再只依赖 `BoardObject` 的静态初始化数据
- 玩家对象会从全局 `GlobalGameSession` 读取状态
- 这为后续地图端与战斗端共享玩家状态留出了稳定入口
