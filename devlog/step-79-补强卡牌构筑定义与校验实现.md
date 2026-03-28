# Step 79 - 补强卡牌构筑定义与校验实现

## 日期

2026-03-28

## 目标

继续只在 battle / card 这一侧推进，把最近文档里已经确定的卡牌构筑规则真正落到定义、校验、默认资源和 battle 消费逻辑中。

## 本次改动

### 1. 扩充卡牌模板与构筑结果定义

更新：

- `Scripts/Battle/Cards/BattleCardTemplate.cs`
- `Scripts/Battle/Cards/BattleDeckBuildRules.cs`
- `Scripts/Battle/Cards/BattleDeckValidationResult.cs`
- `Scripts/Battle/Cards/BattleDeckResolvedCard.cs`

新增或补强：

- 循环标签
- 学习牌标记
- 超规携带相关元数据
- 危险循环牌默认约束字段
- 构筑结果中的 warnings、超规槽位和循环限制摘要

### 2. 重写构筑校验主逻辑

更新：

- `Scripts/Battle/Cards/BattleDeckConstructionService.cs`

主要变化：

- 改为按最低卡数 + 影响因子预算校验
- 不再把固定最大卡数当作主要约束
- 新增 cycle / quick_cycle / energy_positive 的数量限制
- 新增超规携带槽位限制
- 对超规带入的卡自动附加额外影响因子与运行时惩罚

### 3. deck builder 显示接入新规则

更新：

- `Scripts/Battle/UI/BattleDeckBuilderController.cs`

新增显示：

- 学习牌标记
- 超规候选标记
- 影响因子摘要
- 超规槽位使用情况
- 循环标签限制摘要
- validation warnings

### 4. 让 battle 初始化真正消费新的构筑结果

更新：

- `Scripts/Battle/BattleSceneController.cs`

主要变化：

- 战斗初始化牌组时优先通过 `BattleDeckConstructionService` 生成运行时卡牌定义
- 这样超规携带的费用 / 效果惩罚会真正进入 battle
- 卡牌目录不再只按“当前正常解锁”过滤，避免合法构筑卡在初始化时解析失败

### 5. 收紧默认原型卡库中的危险循环牌

更新：

- `Resources/Battle/Cards/DefaultBattleDeckBuildRules.tres`
- `Resources/Battle/Cards/DefaultBattleCardLibrary.tres`

主要变化：

- 默认最低卡数提升到 `10`
- 默认影响因子预算提升到 `20`
- 补入默认循环标签与超规槽位规则
- 给明显危险的 Quick 抽牌 / 回能牌补上 `Exhaust`
- 收紧其影响因子和同名上限

## 结果

现在卡牌构筑系统在 battle 侧已经更接近最近文档里的正式规则：

- 最低卡数负责防最小循环
- 影响因子负责限制总体负载
- 危险循环牌有明确标签限制
- 超规携带和学习牌都有正式元数据
- deck builder 和 battle 初始化都开始消费这些规则

本次改动没有主动修改地图层接口。
