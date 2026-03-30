# 2026-03-30 角色状态与装备接口同步

这组文件用于给后续地图、成长、背包或多人协作对接时快速交接本次改动。

本次最关键的三份文件是：

1. `Scripts/Battle/Shared/GlobalGameSession.cs`
2. `Scripts/Map/UI/SystemFeatureLabController.cs`
3. `Scene/SystemFeatureLab.tscn`

对应说明见：

- `01-GlobalGameSession-变更说明.md`
- `02-SystemFeatureLabController-变更说明.md`
- `03-SystemFeatureLab-tscn-变更说明.md`

本次交接重点不是“做了一个完整正式装备系统”，而是：

- 把角色状态页和背包页拆开
- 让 `GlobalGameSession` 开始承担最小装备槽与升级进度接口
- 让 battle 能读取经过装备 / 天赋修正后的玩家属性

当前阶段判断：

- 接口层：已开始覆盖角色状态、经验进度、装备穿脱
- 实现层：仍是竞赛 demo 级最小实现
- 后续演进方向：保持现有接口名稳定，再逐步替换为资源化正式实现
