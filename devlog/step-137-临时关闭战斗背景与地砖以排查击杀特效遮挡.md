# Step 137 - 临时关闭战斗背景与地砖以排查击杀特效遮挡

## 日期

2026-04-05

## 目标

当前击杀特效仍然“打死后直接消失”，为了先排除底层渲染遮挡问题，临时把战斗背景和地砖都隐藏，保留单位与特效层单独观察。

## 本次改动

更新文件：

- `Scripts/Battle/BattleSceneController.cs`

改动内容：

- 新增 `ShowBattleBackgroundLayer`
  - 默认值改为 `false`
- `ShowBattleFloorLayer`
  - 默认值从 `true` 改为 `false`
- `ConfigureBattleBackground(...)`
  - 在背景缩放和定位完成后，显式应用 `ShowBattleBackgroundLayer`

## 结果

当前默认进入战斗时：

- 背景图隐藏
- 棋盘地砖隐藏
- 单位、浮字、击杀特效层仍会保留

这样可以更直接判断：

- 击杀特效是否真的被创建
- 是否只是被背景或地砖层挡住
- 特效的实际出现位置是否正确

## 验证

执行：

- `dotnet build`

结果：

- 编译通过
- 0 errors

## 备注

这是针对击杀特效排查的临时调试状态，后续定位完成后再恢复正式显示。
