# SystemFeatureLab.tscn 变更说明

文件：

- `Scene/SystemFeatureLab.tscn`

## 本次新增内容

### 1. 新增 `StatusTab`

在原 `Tabs` 下新增 `StatusTab`，作为角色状态页的场景承载节点。

当前主要结构为：

- `StatusTab/Columns/StatusColumn`
- `StatusTab/Columns/EquipmentColumn`

### 2. 左侧状态展示区

新增节点：

- `StatusText`

用途：

- 显示角色等级、经验、专精点、生命、移动、攻击、防御、荒川状态等摘要

### 3. 右侧装备管理区

新增节点：

- `SlotList`
- `CandidateList`
- `EquipButton`
- `UnequipButton`
- `EquipmentDetailPanel`
- `EquipmentDetailText`

用途：

- 查看当前三槽装备状态
- 选择对应槽位可装备物品
- 执行穿戴 / 卸下
- 查看装备说明

### 4. 页签结构发生变化

由于新增角色页，后续若有代码通过 tab 索引硬编码访问页签，需要重新确认索引是否仍然匹配。

当前顺序是：

1. 角色
2. 背包
3. 天赋
4. 图鉴
5. 构筑

## 对接时必须注意

- 如果后续有人继续改 `SystemFeatureLabController.cs`，必须保证这些节点路径不被随意改名
- 该场景现在已经不仅是战斗入口测试场景，也承担角色 / 背包 / 天赋 / 构筑的综合对接验证职责

## 当前局限

- 仍是测试场景，不是最终正式地图 UI
- 视觉样式仍偏功能验证
- 后续若做正式界面，可以换皮，但建议保留当前节点职责划分
