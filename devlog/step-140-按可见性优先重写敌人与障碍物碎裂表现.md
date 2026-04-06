# Step 140 - 按可见性优先重写敌人与障碍物碎裂表现

## 日期

2026-04-05

## 背景

在上一轮说明中已经确认，原实现的问题不在“有没有调用”，而在于：

- 快照锚点和原 sprite 不一致
- 主体过早隐藏
- 碎片初始透明且持续时间过短
- 障碍物不需要白闪，但当前实现仍带有类似闪白阶段

因此本次改动的目标不是“更华丽”，而是“调试阶段每一步都必须肉眼可见”。

## 本次改动

### 1. 补充原 sprite 的居中信息抓取

更新文件：

- `Scripts/Battle/Presentation/BattleAnimatedViewBase.cs`

新增内容：

- `CaptureSpriteCentered()`

作用：

- 快照节点不再只拿位置和翻转状态
- 现在还会读取原 `AnimatedSprite2D` 的 `Centered` 设置
- 用于计算快照真实左上角，从而把快照摆回与原 sprite 一致的位置

### 2. 敌人击杀特效改为“锚点对齐 + 主体延后隐藏 + 碎片初始可见”

更新文件：

- `Scripts/Battle/Presentation/BattlePieceViewManager.cs`

调整内容：

- `killGhost` 的根位置改为单位 `view.Position`
- 快照实际贴图位置改为根据：
  - 原 sprite 的 `Position`
  - 原 sprite 是否 `Centered`
  - 当前帧纹理尺寸
  共同计算出的真实左上角
- 主体 `baseSprite` 不再立刻隐藏
- 在击退后增加短暂停顿
- 白化阶段不再只是“白到白”，而是使用 shader 的 `white_mix`
- 白化完成后再额外停留一小段时间
- 碎片创建时默认可见，不再从全透明开始
- 碎裂飞散距离和旋转幅度增大
- 总时长显著拉长，便于肉眼确认

### 3. 障碍物碎裂改为“无白闪、原地停留后直接裂开”

更新文件：

- `Scripts/Battle/Presentation/BattlePieceViewManager.cs`
- `Scripts/Battle/Actions/BattleActionService.cs`

调整内容：

- 障碍物专属 `PlayObstacleBreakSequenceAsync(...)`
  - 保留原地停留
  - 取消白闪过程
  - 主体短暂停留后再隐藏
  - 分片初始直接可见
  - 分片飞散距离和旋转幅度增大
- `BattleActionService` 中障碍物碎裂时长改长

### 4. 调试阶段显著延长表现时间

更新文件：

- `Scripts/Battle/Actions/BattleActionService.cs`

调整内容：

- 敌人：
  - `KillKnockbackPresentationDurationSeconds` 提高到 `0.18`
  - `KillWhitenPresentationDurationSeconds` 提高到 `0.24`
  - `KillShatterPresentationDurationSeconds` 提高到 `0.52`
- 障碍物：
  - `ObstacleBreakWhitenPresentationDurationSeconds` 调整为停留用时 `0.18`
  - `ObstacleBreakShatterPresentationDurationSeconds` 提高到 `0.58`

## 预期结果

当前调试阶段：

- 敌人击杀时应该能清楚看到：
  - 原位快照
  - 击退
  - 停住
  - 明显白化
  - 裂开散落
- 障碍物击碎时应该能清楚看到：
  - 原位停留
  - 主体消失
  - 分片明显从原地裂开散落

## 验证

执行：

- `dotnet build`

结果：

- 编译通过
- 0 errors
