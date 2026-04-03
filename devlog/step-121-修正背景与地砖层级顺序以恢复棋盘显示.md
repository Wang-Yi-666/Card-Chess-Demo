# Step 121 - 修正背景与地砖层级顺序以恢复棋盘显示

## 日期

2026-04-03

## 问题

前面恢复了 `ShowBattleFloorLayer`，但战斗中的棋盘地砖仍然看不到。

原因不是地砖层继续被隐藏，而是当前运行时把背景节点插到了 `FloorLayer` 后面，导致背景绘制在地砖之上，把棋盘地砖盖住了。

## 本次改动

更新文件：

- `Scripts/Battle/BattleSceneController.cs`

调整内容：

- 在 `AttachBattleBackgroundToRoom()` 中，将背景插入索引从：
  - `floorLayer.GetIndex() + 1`
  改为：
  - `floorLayer.GetIndex()`

## 结果

现在房间内部显示顺序恢复为：

- 背景
- 棋盘地砖
- 其他房间内容 / 单位 / 覆盖层

这样地砖就会重新显示出来。
