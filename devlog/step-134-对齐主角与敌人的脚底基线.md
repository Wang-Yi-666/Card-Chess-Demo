# Step 134 - 对齐主角与敌人的脚底基线

## 日期

2026-04-05

## 问题

当前主角和敌人在战斗中的脚底不在同一条水平线上，看起来像是站在不同高度。

原因是两者虽然都使用了相同的格子锚点与 `CellFootOffsetY`，但敌人的 `SpriteDrawOffset.Y` 偏移更高，导致敌人整体被抬起。

## 本次改动

更新文件：

- `Scripts/Battle/Presentation/BattleEnemyView.cs`

调整内容：

- `SpriteDrawOffset` 从 `(-16, -24)` 改为 `(-16, -16)`

## 结果

- 敌人 sprite 的脚底会与主角落在同一条基线上
- 不改变朝向逻辑，也不影响切帧规格
- 只修正战斗表现层的落地高度

## 验证

执行：

- `dotnet build`

结果：

- 编译通过
- 0 errors
