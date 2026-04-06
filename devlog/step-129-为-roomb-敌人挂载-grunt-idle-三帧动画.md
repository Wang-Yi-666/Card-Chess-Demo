# Step 129 - 为 roomB 敌人挂载 grunt idle 三帧动画

## 日期

2026-04-05

## 目标

将美术提供的 grunt 敌人 idle 三帧动画接到当前 roomB 使用的敌人 prefab 上。

资源位置：

- `Assets/Character/Battle/Enemy/Grunt/敌人1.png`
- `Assets/Character/Battle/Enemy/Grunt/敌人2.png`
- `Assets/Character/Battle/Enemy/Grunt/敌人3.png`

## 本次改动

更新文件：

- `Scripts/Battle/Presentation/BattleEnemyView.cs`

改动内容：

- `BattleEnemyView` 现在会优先读取 grunt 的三张独立帧图
- 生成 `idle / move / action / hit / defeat` 的基础动画集
- 其中：
  - `idle` 使用三帧循环
  - `move` / `action` 复用这组三帧
  - `hit` / `defeat` 使用单帧兜底

## 绘制挂载方式

为了按“常规 16x32 角色”挂到战斗格子上：

- `AnimatedSprite2D` 改为 `Centered = false`
- 敌人默认绘制偏移设置为：
  - `SpriteDrawOffset = (-16, -24)`
- 同时保留脚底落格偏移：
  - `CellFootOffsetY = 8`

这样敌人的脚底会更接近战斗格子的底边，符合 16x32 角色常见的站位方式。

## 结果

当前 roomB 中使用 `battle_enemy` prefab 的敌人，进入战斗后应显示 grunt 的 idle 三帧动画，而不再只是纯色程序方块。
