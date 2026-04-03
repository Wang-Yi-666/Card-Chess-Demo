# Step 119 - 恢复战斗棋盘地板层显示

## 日期

2026-04-03

## 调整

在确认战斗背景已经能通过运行时动态创建正确显示后，将战斗房间的地板层重新恢复显示。

更新文件：

- `Scripts/Battle/BattleSceneController.cs`

改动内容：

- `ShowBattleFloorLayer` 默认值从 `false` 改回 `true`

## 结果

现在进入战斗时：

- 背景图继续显示
- 棋盘地板层重新可见

由于背景节点现在插入在 `FloorLayer` 之后，恢复棋盘显示不会再回到“背景完全消失”的旧问题。
