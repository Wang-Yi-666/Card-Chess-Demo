# Step 04 - Battle Scene Wiring

## Date

2026-03-20

## Goal

把 `Battle.tscn` 从空壳场景接成一个真正可挂载战斗域逻辑的入口场景。

## Scene Changes

- 给 `Battle` 根节点挂上 `BattleSceneController`
- 新增 `BoardRoot`
- 新增 `PieceRoot`
- 新增 `EffectRoot`
- 在 `BoardRoot` 下接入 `BattleBoardDebugView`

## Runtime Behavior

- 若未指定布局资源，`BattleSceneController` 会自动生成一个调试布局
- 调试布局内包含：
  - 1 个玩家单位
  - 1 个敌方单位
  - 1 个可破坏掩体
  - 1 个可同格场地对象
- 这让场景在内容资源链路尚未补齐前，仍然可以验证棋盘框架是否正常启动

## Why

这一步的目标不是做正式战斗表现，而是先保证：

- 战斗场景有明确根控制器
- 棋盘层和表现层有命名清晰的挂点
- 后续增加棋子实例、动画层、特效层时不用再重排场景树
