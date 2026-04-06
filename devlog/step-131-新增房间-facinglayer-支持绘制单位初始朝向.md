# Step 131 - 新增房间 FacingLayer 支持绘制单位初始朝向

## 日期

2026-04-05

## 目标

把单位初始朝向从硬编码/默认推断，改成可在房间 scene 里直接绘制的独立数据层。

## 本次改动

### 1. 新增 FacingLayer 绘制层

新增文件：

- `Scene/Battle/Tiles/BattleFacingLeftTile.tscn`
- `Scene/Battle/Tiles/BattleFacingUpTile.tscn`
- `Scene/Battle/Tiles/BattleFacingRightTile.tscn`
- `Scene/Battle/Tiles/BattleFacingDownTile.tscn`

并更新：

- `Scene/Battle/Rooms/GruntDebugRoomA.tscn`
- `Scene/Battle/Rooms/GruntDebugRoomB.tscn`

现在房间里会有独立的 `FacingLayer`，可以和出生标记叠画在同一格上，不影响玩家/敌人/障碍物出生配置。

### 2. 房间模板读取 FacingLayer

更新：

- `Scripts/Battle/Rooms/BattleRoomTemplate.cs`
- `Scripts/Battle/Rooms/BattleRoomTileSetFactory.cs`
- `Scripts/Battle/Data/BoardObjectSpawnDefinition.cs`
- `Scripts/Battle/State/BattleObjectState.cs`
- `Scripts/Battle/State/BattleObjectStateManager.cs`
- `Scripts/Battle/Presentation/BattleAnimatedViewBase.cs`

改动内容：

- `BuildLayoutDefinition()` 现在会从 `FacingLayer` 读取每个出生格的初始朝向
- 朝向作为 `initial_facing_x / initial_facing_y` 写入 spawn payload
- `BattleObjectState` 增加 `InitialFacing`
- 视图 `Bind()` 时会按这个朝向初始化

### 3. 默认规则

如果 `FacingLayer` 没画任何格子：

- 玩家默认朝右
- 敌人默认朝左

这样旧房间也不会失效。
