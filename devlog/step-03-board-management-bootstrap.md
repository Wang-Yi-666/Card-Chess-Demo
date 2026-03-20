# Step 03 - Board Management Bootstrap

## Date

2026-03-20

## Goal

实现文档中要求的最小棋盘管理地基，让玩家、敌人、障碍物、场地对象能在同一套运行时模型里管理。

## Implemented

- `BoardObject`
  - 统一对象模型
  - 支持类型、阵营、标签、HP、挡路、挡视线、同格规则
- `BoardCellState`
  - 单位槽
  - resident 集合
  - blocking 集合
- `BoardState`
  - 棋盘尺寸
  - 格子状态容器
  - 房间级 `RoomTags`
- `BoardObjectRegistry`
  - `object_id -> BoardObject`
- `OccupancyRules`
  - 单位唯一占位
  - 可同格与不可同格规则
- `BoardQueryService`
  - 查询格上对象
  - 计算移动代价
  - 尝试移动 object
- `BoardInitializer`
  - 依据 `RoomLayoutDefinition` 初始化运行时 board

## Notes

- 当前版本优先把语义边界搭稳，没有提前做路径搜索、视线追踪和伤害结算。
- `layout.Tags` 已按房间级标签存进 `BoardState`，没有错误地下放到每个格子。
