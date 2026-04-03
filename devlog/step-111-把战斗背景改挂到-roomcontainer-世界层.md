# Step 111 - 把战斗背景改挂到 RoomContainer 世界层

## 日期

2026-04-02

## 问题

上一版把战斗背景作为根节点下的独立世界节点挂载，理论上应当可见，但实机表现仍然只有默认灰色背景，说明当前层级组织不够稳妥。

## 调整方案

将背景图从根节点兄弟层改为直接挂到：

- `RoomContainer/BattleBackground`

这样可以确保：

- 背景与棋盘、单位、覆盖层处于同一套世界坐标体系
- 镜头移动时一定与棋盘同步
- 不经过 UI 层，也不依赖额外 sibling 层级关系

## 本次改动

更新文件：

- `Scene/Battle/Battle.tscn`
- `Scripts/Battle/BattleSceneController.cs`

具体处理：

- 把 `BattleBackground` 从根节点下移到 `RoomContainer` 下
- 将其 `z_index` 设为更靠后的 `-100`
- 背景定位从全局偏移改为 `RoomContainer` 内局部居中

## 结果

现在背景图是 `RoomContainer` 的子节点，属于战斗世界层的一部分，显示稳定性和层级关系都更明确。
