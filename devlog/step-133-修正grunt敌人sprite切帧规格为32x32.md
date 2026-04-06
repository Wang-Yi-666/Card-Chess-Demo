# Step 133 - 修正 grunt 敌人 sprite 切帧规格为 32x32

## 日期

2026-04-05

## 问题

美术提供的 grunt 敌人图不是 `16x32` 连续帧，而是：

- 整张尺寸 `96x32`
- 共 `3` 帧
- 每帧占 `32x32`

之前敌人视图仍按错误的 `16x32` 宽度切帧，导致切割线落在两帧中间，显示错位。

## 本次改动

更新文件：

- `Scripts/Battle/Presentation/BattleEnemyView.cs`

调整内容：

- `FrameWidth` 从 `16` 改为 `32`
- `FrameHeight` 保持 `32`
- `FrameCount` 保持 `3`
- `SpriteDrawOffset` 从 `(-8, -24)` 改为 `(-16, -24)`，匹配 32 像素宽角色的中心落点

## 结果

- grunt 敌人现在会按 `96x32 -> 3 x 32x32` 正确切帧
- 不会再切到两帧中间
- 敌人落格中心也会与新的 32 宽 sprite 对齐

## 验证

执行：

- `dotnet build`

结果：

- 编译通过
- 0 errors
