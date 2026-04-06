# Step 142 - 修正敌人击退方向并改为真正的白色覆盖层

## 日期

2026-04-05

## 问题

最新一轮测试表明：

- 敌人击退已经出现，但方向反了
- 速度仍然偏快
- 白化不正确

其中白化问题的根因是：

- 之前叠加的所谓“白色图层”本质上仍然是原贴图
- `modulate = white` 只会保留原色，不会把贴图真正变白

## 本次改动

### 1. 修正敌人击退方向

更新文件：

- `Scripts/Battle/Presentation/BattlePieceViewManager.cs`

改动内容：

- 敌人击退方向从“攻击方向的反方向”改为“攻击方向本身”

结果：

- 例如从左向右攻击时，敌人会向右被打飞，而不是反向回弹

### 2. 白化覆盖层改成真正的白色 alpha 蒙版

更新文件：

- `Scripts/Battle/Presentation/BattlePieceViewManager.cs`

新增逻辑：

- 基于快照纹理生成一张新的 `whiteOverlayTexture`
- 这张图会保留原贴图 alpha，但把所有可见像素的 RGB 强制改为纯白
- 敌人击退时叠加的已经不再是“原图第二层”，而是真正的白色覆盖层

### 3. 提高白色覆盖层层级

更新文件：

- `Scripts/Battle/Presentation/BattlePieceViewManager.cs`

改动内容：

- `baseSprite.ZIndex = 0`
- `whiteOverlaySprite.ZIndex = 1`

结果：

- 白色覆盖层会明确压在主体快照之上
- 不再依赖默认子节点顺序碰运气

### 4. 再次放慢敌人击退速度

更新文件：

- `Scripts/Battle/Actions/BattleActionService.cs`

改动内容：

- `KillKnockbackPresentationDurationSeconds`
  - 从 `0.32` 提高到 `0.50`

结果：

- 击退仍使用 `Ease.Out + Cubic`
- 但整体过程明显更慢，更容易看出“前快后慢、逐渐停下”

## 当前敌人击杀表现顺序

现在敌人击杀应当表现为：

1. 沿攻击方向被击退
2. 击退过程中同步被白色覆盖层逐渐盖满
3. 到终点停住
4. 再进入碎裂散开

## 验证

执行：

- `dotnet build`

结果：

- 编译通过
- 0 errors
