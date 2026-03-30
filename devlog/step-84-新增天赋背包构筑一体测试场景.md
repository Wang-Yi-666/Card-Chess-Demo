# Step 84 - 新增天赋背包构筑一体测试场景

## 日期

2026-03-30

## 目标

新增一张专门用于测试：

- 背包
- 天赋
- 构筑
- 战斗入口

的一体化地图场景，并把系统面板统一绑定到 `C` 键切换。

## 本次改动

### 1. 新增综合系统测试控制器

新增：

- `Scripts/Map/UI/SystemFeatureLabController.cs`

主要能力：

- 按 `C` 打开 / 关闭系统面板
- 面板内切换：
  - 背包
  - 天赋
  - 构筑
- 天赋购买 / 退款后，实时回写到 `GlobalGameSession`
- 构筑页签直接复用现有 `BattleDeckBuilderController`
- 统一显示当前攻击、防御减伤、护盾加成、构筑预算等测试信息

### 2. 新增综合测试地图

新增：

- `Scene/SystemFeatureLab.tscn`

场景内容包括：

- 当前地图域玩家
- 战斗入口敌人
- 综合系统面板
- 可视化提示文本

### 3. 补充构筑面板外部刷新入口

更新：

- `Scripts/Battle/UI/BattleDeckBuilderController.cs`

新增：

- `RefreshFromExternalState()`

让天赋页签修改成长状态后，构筑页签可以即时刷新。

### 4. 切换项目入口到测试场景

更新：

- `project.godot`

调整：

- `run/main_scene = "res://Scene/SystemFeatureLab.tscn"`

方便直接运行验证综合功能。

## 结果

现在项目已经有一张可直接用于验证：

- 背包展示
- 天赋购买
- 构筑变化
- 地图进入战斗

这整条链路的综合测试场景。
