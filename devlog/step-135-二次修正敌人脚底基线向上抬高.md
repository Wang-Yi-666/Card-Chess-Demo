# Step 135 - 二次修正敌人脚底基线向上抬高

## 日期

2026-04-05

## 问题

上一次把敌人的 `SpriteDrawOffset.Y` 从 `-24` 调到 `-16`，方向判断错了，导致敌人进一步下沉。

用户确认的正确方向是：

- 原本敌人已经偏低
- 需要继续往上提
- 不能再往下压

## 本次改动

更新文件：

- `Scripts/Battle/Presentation/BattleEnemyView.cs`

调整内容：

- `SpriteDrawOffset.Y` 从 `-16` 改为 `-28`

## 结果

- 敌人会比最初版本再向上抬高 4 像素
- 这次修正只影响敌人的战斗表现落点，不影响切帧、朝向和动画播放

## 验证

执行：

- `dotnet build`

结果：

- 编译通过
- 0 errors
