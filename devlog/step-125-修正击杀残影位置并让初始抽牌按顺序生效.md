# Step 125 - 修正击杀残影位置并让初始抽牌按顺序生效

## 日期

2026-04-03

## 问题

用户反馈两点：

- 击杀敌人时仍然看不到碎裂特效
- 测试高伤牌并没有稳定出现在起手

## 原因

### 1. 击杀残影位置没有继承完整的棋盘锚点状态

上一版独立 `KillGhost` 虽然复制了 `Position`，但视图真正用于刷新位置的是：

- `BoardAnchor`
- `MotionOffset`

这会导致残影一开始播放击杀演出时，有机会被重置到错误位置，看起来像直接消失。

### 2. 原型牌组依然会在开局时洗牌

即使把测试牌放进牌组，如果初始化后还会洗牌，那么“放在顶部”就不能稳定等于“起手会抽到”。

## 本次改动

更新文件：

- `Scripts/Battle/Presentation/BattlePieceViewManager.cs`
- `Scripts/Battle/Cards/BattleDeckState.cs`
- `Scripts/Battle/BattleSceneController.cs`

### 1. 修正击杀残影位置继承

在复制 `KillGhost` 时，额外同步：

- `BoardAnchor`
- `MotionOffset`

让残影节点和原敌人保持一致的棋盘位置状态后再播放击杀特效。

### 2. 初始牌组改为真正按顺序发牌

在 `BattleDeckState` 中：

- 去掉开局建立牌堆后的默认洗牌
- 抽牌方向改为按列表前端顺序抽取

这样原型牌组里放在最前面的牌，就会稳定进入最早的起手抽牌顺序。

### 3. 测试高伤牌保留在原型牌组顶部

撤销了额外的起手插牌逻辑，恢复为直接通过原型牌组顺序控制。

## 结果

现在：

- 击杀残影不应再因为位置重置而瞬间消失
- `debug_finisher` 会按原型牌组顺序稳定进入起手抽牌
