# Step 86 - 统一天赋树结构并补图鉴页

## 日期

2026-03-30

## 目标

把综合测试场景中的天赋页从分离式结构继续收敛为统一大树，并在同一个 `C` 面板里加入图鉴页。

## 本次改动

### 1. 统一天赋树为单画布结构

更新：

- `Scripts/Map/UI/SystemFeatureLabController.cs`
- `Scene/SystemFeatureLab.tscn`

主要调整：

- 天赋页改为：
  - `TalentTreeScroll`
  - `TalentTreeCanvas`
- 卡牌树与角色能力树放入同一画布
- 允许拖动画布查看完整天赋树
- 增加：
  - `CardTreeLabel`
  - `RoleTreeLabel`
  作为两棵树的中心锚点

### 2. 加入前置关系连线

更新：

- `Scripts/Map/UI/SystemFeatureLabController.cs`

新增：

- `RefreshTalentTreeLines()`
- `AddTreeLine(...)`

逻辑：

- 无前置节点：从树名根节点连出
- 有前置节点：从前置节点连到目标节点
- 已购 / 未购前置会使用不同线色

### 3. 新增图鉴页

更新：

- `Scripts/Map/UI/SystemFeatureLabController.cs`
- `Scene/SystemFeatureLab.tscn`

新增页签：

- `图鉴`

并细分：

- `卡牌图鉴`
- `敌人图鉴`

当前规则：

- 已解锁：显示名字、描述和数据
- 未解锁：显示 `■■■■` 与解锁方式提示

## 验证

执行：

- `dotnet build`

结果：

- `0 error`

## 结果

现在 `SystemFeatureLab` 的 `C` 面板已经具备：

- 背包
- 天赋树
- 图鉴
- 构筑

四个可切换的测试模块，且天赋页和图鉴页的结构已经统一到当前控制器实现。
