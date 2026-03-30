# Step 85 - 重构综合测试场景构筑页并修正超限语义

## 日期

2026-03-30

## 目标

继续完善 `SystemFeatureLab` 综合测试场景，把原本崩坏的构筑页替换为紧凑版专用 UI，并修正“超限携带”与影响因子预算的语义混淆。

## 本次改动

### 1. 重构综合测试场景里的构筑页

更新：

- `Scripts/Map/UI/SystemFeatureLabController.cs`
- `Scene/SystemFeatureLab.tscn`

主要调整：

- 不再嵌入旧版 `DeckBuilder.tscn` 作为主要构筑 UI
- 改为在 `SystemFeatureLabController` 内部直接管理紧凑版构筑页
- 新构筑页包含：
  - 可选牌列表
  - 当前牌组列表
  - 加入 / 移除按钮
  - 默认牌组 / 恢复会话 / 保存构筑按钮
  - 卡牌详情面板
  - 构筑校验面板

### 2. 继续固定训练敌人进入 roomB

更新：

- `Scene/SystemFeatureLab.tscn`
- `Scene/Battle/SystemFeatureLabBattle.tscn`

保持：

- 训练敌人通过专用 battle 场景进入战斗
- 专用 battle 场景继续固定 `ForcedBattleRoomScene = GruntDebugRoomB.tscn`

### 3. 修正“超限 / 超规携带”语义

更新：

- `Scripts/Battle/Cards/BattleCardTemplate.cs`
- `Docs/天赋成长与卡组构筑正式方案.md`
- `Docs/卡组构筑平衡规则方案.md`
- `Docs/设计模板/README.md`

明确改为：

- 超限 / 超规携带 = 已拥有，但尚未满足正常携带要求时的勉强携带
- 不表示可以突破牌组影响因子预算

## 结果

现在综合测试场景中的构筑页已经不再依赖之前那套超大面板缩放方案，结构更适合在当前测试窗口里直接使用。
