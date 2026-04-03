# Step 124 - 补强方块敌人的击杀特效并加入测试高伤牌

## 日期

2026-04-03

## 问题

在当前敌人仍使用程序化方块而非正式 sprite 的情况下，击杀特效仍然不稳定，看起来像直接消失。

同时为了便于反复验证击杀表现，还需要让默认原型手牌里稳定出现一张高伤测试牌。

## 本次改动

### 1. 补强击杀残影的初始化时机

更新文件：

- `Scripts/Battle/Presentation/BattleAnimatedViewBase.cs`

处理内容：

- 如果击杀残影开始播放时尚未拿到 `AnimatedSprite2D`
- 会等待一帧后再次获取
- 如果 `SpriteFrames` 仍为空，则自动补上 fallback 方块帧

这样即便敌人当前还没有正式美术资源，击杀残影节点也能稳定显示白闪与碎裂演出。

### 2. 再缩短敌人之间的行动停顿

更新文件：

- `Scripts/Battle/AI/EnemyTurnResolver.cs`

调整：

- `PreActionDelaySeconds` 从 `0.08` 改到 `0.04`
- `PostActionDelaySeconds` 从 `0.08` 改到 `0.05`

作用：

- 敌人与敌人之间的行动衔接更紧
- 最后一个敌人行动结束到玩家回合开始也更快

### 3. 默认原型手牌固定加入测试高伤牌

更新文件：

- `Scripts/Battle/BattleSceneController.cs`

改动：

- 新增测试牌 `debug_finisher`
- 在没有外部 battle request 覆盖时
- 默认运行时起手手牌固定加入这张测试牌

当前测试牌效果：

- 近战攻击
- 1 费
- 99 伤害
- 打出后 Exhaust

## 结果

现在更适合快速测试：

- 方块敌人也应能看到击杀演出
- 敌方行动之间停顿更短
- 起手就有一张高伤测试牌可直接验证击杀镜头与碎裂表现
