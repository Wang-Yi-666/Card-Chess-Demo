# Step 126 - 修正真实默认卡组入口并把逃跑改成立即成功

## 日期

2026-04-03

## 问题

用户确认了两个关键点：

- 当前默认起手看不到测试高伤牌，说明之前改的不是项目当前真正使用的默认卡组入口
- 新逃跑方式仍然要等敌方回合结束，不符合“踩到出口格后立即脱离战斗”的需求

## 本次改动

### 1. 修正真实默认卡组入口

更新文件：

- `Resources/Battle/Cards/DefaultBattleCardLibrary.tres`
- `Scripts/Battle/Shared/GlobalGameSession.cs`
- `Scripts/Battle/Cards/BattleDeckState.cs`
- `Scripts/Battle/BattleSceneController.cs`

处理内容：

- 在真实默认卡牌库 `DefaultBattleCardLibrary.tres` 中新增 `debug_finisher`
- 将其放到 `Entries` 最前面，并设置 `DefaultStarterCopies = 1`
- `GlobalGameSession.EnsureDeckBuildInitialized()` 现在会兼容已经初始化过的 session：
  - 如果当前 deck build 中还没有 `debug_finisher`
  - 会自动前插一张
- `BattleDeckState` 开局不再默认洗牌
- 抽牌顺序改为从牌堆前端按定义顺序抽取

这意味着“把测试牌放在默认卡组顶部”现在终于会真实影响起手顺序。

### 2. 逃跑改为到出口即立即成功

更新文件：

- `Scripts/Battle/BattleSceneController.cs`

改动内容：

- 站在可逃脱格上点击逃跑按钮后
- 不再进入 `_retreatPending` 等待敌方回合的旧流程
- 直接提交 `BattleOutcome.Retreat`

## 结果

现在：

- 测试高伤牌走的是真实默认卡组入口
- 默认抽牌顺序按定义顺序生效
- 到达逃脱格后可立即脱离战斗

## 备注

这次同时清理了 `BattleSceneController.cs` 中几处旧乱码造成的坏字符串，恢复了编译稳定性。

## 验证

- `dotnet build`
  - 结果：`0 errors`
  - 仍保留项目原有 nullable warnings
