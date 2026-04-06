# Step 141 - 敌人击杀改为减速击退并在击退过程中逐渐发白

## 日期

2026-04-05

## 目标

根据最新反馈，敌人击杀特效已经能看到碎裂，但仍存在两个关键问题：

- 看不出明显击退
- 看不出逐渐变白

这次调整只针对敌人击杀链路，目标是：

- 击退阶段要明显可见
- 击退速度呈现“前快后慢、逐渐停下”的曲线
- 在击退过程中同步逐渐变成全白

## 本次改动

### 1. 敌人白化改成独立白色覆盖层

更新文件：

- `Scripts/Battle/Presentation/BattlePieceViewManager.cs`

改动内容：

- 在敌人 `PlayKillSequenceAsync(...)` 中，不再依赖原先的 shader 白化
- 现在额外创建一个与快照完全同位的 `whiteOverlaySprite`
- 该覆盖层初始 alpha 为 0
- 在击退 tween 的同一段时间内，alpha 从 0 逐渐升到 1

结果：

- 玩家可以直接看到敌人在被打飞的过程中逐步被白色覆盖
- 白化与位移不再是分开的两个阶段

### 2. 击退位移改为更明显的减速曲线

更新文件：

- `Scripts/Battle/Presentation/BattlePieceViewManager.cs`
- `Scripts/Battle/Actions/BattleActionService.cs`

改动内容：

- 击退 tween 继续使用 `Ease.Out + Cubic`
  - 起步更快
  - 结尾明显减速
- 击退时长从 `0.18` 提升到 `0.32`
- 击退距离从 `11` 提升到 `18`

结果：

- 敌人的击退轨迹更容易被肉眼观察到
- 终点停住感也更明确

### 3. 击退完成后保留短暂停顿再碎裂

更新文件：

- `Scripts/Battle/Presentation/BattlePieceViewManager.cs`

改动内容：

- 击退结束后，保留约 `0.18s` 的停顿
- 再隐藏主体并让分片碎裂散开

这样敌人不会在刚停下的那一瞬间就直接碎掉。

## 当前敌人击杀顺序

现在敌人击杀演出顺序为：

1. 抓取当前帧快照
2. 沿攻击反方向击退
3. 同时逐渐叠上白色覆盖
4. 到达终点后停住一小段时间
5. 主体隐藏
6. 分片四散裂开

## 验证

执行：

- `dotnet build`

结果：

- 编译通过
- 0 errors
