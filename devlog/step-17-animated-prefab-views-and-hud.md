# Step 17 - Animated Prefab Views And HUD

## Date

2026-03-20

## Goal

为玩家、敌人、障碍物建立真正的 prefab 表现层，并用 HUD 直接显示对象状态，验证状态变更能驱动视觉和 UI。

## Implemented

- 新增 prefab library：
  - `BattlePrefabEntry`
  - `BattlePrefabLibrary`
  - `Resources/Battle/Presentation/DefaultBattlePrefabLibrary.tres`
- 新增 AnimatedSprite2D 视图基类：
  - `BattleAnimatedViewBase`
- 新增具体视图脚本：
  - `BattlePlayerView`
  - `BattleEnemyView`
  - `BattleObstacleView`
- 现有 token 场景已升级为带 `AnimatedSprite2D` 的 prefab：
  - `Scene/Battle/Tiles/BattlePlayerToken.tscn`
  - `Scene/Battle/Tiles/BattleEnemyToken.tscn`
  - `Scene/Battle/Tiles/BattleObstacleToken.tscn`
- 新增 `BattlePieceViewManager` 用于把房间 marker 解析出来的对象状态转成运行时 prefab 视图
- 新增 HUD：
  - `Scene/Battle/UI/BattleHud.tscn`
  - `BattleHudController.cs`

## Behavior

- 主角、敌人、障碍物现在会作为 prefab 视图实例化到 `PieceRoot`
- HUD 会显示所有对象的：
  - 名称
  - 位置
  - HP
  - Move
  - 当前动画名
- 主角移动时会触发基础 `move` 动画
- 主角脚本已预留：
  - `PlayIdle()`
  - `PlayMove()`
  - `PlayAction()`
  - `PlayDefend()`
  - `PlayCustom(...)`

## Quick Validation

- 运行中按 `PageUp / PageDown` 会修改 `GlobalGameSession.PlayerMovePointsPerTurn`
- HUD 中的 Move 数值会立即变化
- 棋盘可达范围和预览路径也会立即按新的移动力刷新

## Result

这版已经满足“修改角色状态里的移动力，运行中能体现出来”的最小验证要求，同时把对象状态、动画和 UI 三层接通了。
