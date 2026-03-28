# Step 81 - 解耦主角战斗属性并补敌方召唤接口

## 日期

2026-03-28

## 目标

继续只在 battle 侧优化现有接口，去掉主角攻击力与防御减伤的硬编码依赖，并补出可供 Boss / 精英后续使用的敌方单位召唤接口。

## 本次改动

### 1. 主角攻击力改为可由天赋约定动态修正

更新：

- `Scripts/Battle/Shared/GlobalGameSession.cs`
- `Scripts/Battle/State/BattleObjectStateManager.cs`

新增：

- `GetResolvedPlayerAttackDamage()`

当前采用的 battle 侧约定是：

- `TalentIds` 中若存在 `stat.attack_bonus.X`
- 则会把其中的数值累加到主角攻击力

这样 battle 侧已经允许后续天赋系统直接影响主角普通攻击伤害，而不需要继续写死在场景控制器里。

### 2. 主角防御动作改为可由 session 动态构建

更新：

- `Scripts/Battle/Shared/GlobalGameSession.cs`
- `Scripts/Battle/BattleSceneController.cs`

新增：

- `PlayerDefenseDamageReductionPercent`
- `PlayerDefenseShieldGain`
- `GetResolvedPlayerDefenseDamageReductionPercent()`
- `GetResolvedPlayerDefenseShieldGain()`

并把原本固定的 `50%` 防御减伤，从场景常量改成运行时构建：

- `BuildPlayerDefenseActionDefinition()`

当前采用的 battle 侧约定是：

- `TalentIds` 中若存在 `stat.defense_reduction_bonus.X`
- `stat.defense_shield_bonus.X`

则会在 battle 内部动态影响防御动作。

### 3. 补出通用运行时生成棋盘对象接口

更新：

- `Scripts/Battle/Actions/BattleActionService.cs`

新增：

- `TrySpawnBoardObject(...)`
- `TrySpawnBoardObjectAsync(...)`

并让原本的：

- `TryCreateIndestructibleObstacle(...)`

改为复用这一层通用接口。

这意味着当前 battle 侧已经正式允许：

- 运行时召唤敌方单位
- 运行时召唤友方单位
- 运行时召唤其他棋盘对象

而不再只有“荒川造墙”这一种特例。

### 4. 敌方 AI 决策层补入召唤类型

更新：

- `Scripts/Battle/AI/EnemyAiDecision.cs`
- `Scripts/Battle/AI/EnemyTurnResolver.cs`

新增：

- `EnemyAiDecisionType.Spawn`
- `EnemyAiDecision.Spawn(...)`

并让 `EnemyTurnResolver` 能消费该决策。

这意味着：

- 现在不仅底层能生成敌方单位
- AI 决策层也已经有正式入口承载“Boss 召唤小怪”这种行为

## 结果

现在 battle 侧已经具备两项关键能力：

- 天赋系统可以在不改地图接口的前提下影响主角攻击力与防御减伤
- Boss / 精英后续可以通过 battle 侧正式接口召唤敌方单位

本次改动仍然没有主动修改地图层接口，也没有要求地图层同步改动。
