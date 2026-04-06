# Step 139 - 拆分障碍物专属原地碎裂特效

## 日期

2026-04-05

## 问题

上一轮把障碍物销毁直接接到了和敌人相同的击杀链路里，但这不符合当前需求：

- 不希望障碍物走和敌人相同的一套击杀特效
- 不希望障碍物受击或销毁时发生击退
- 需要一个新的、障碍物专属的“原地碎裂”效果

## 本次改动

### 1. 在视图管理器中新增障碍物专属碎裂接口

更新文件：

- `Scripts/Battle/Presentation/BattlePieceViewManager.cs`

新增方法：

- `PlayObstacleBreakSequenceAsync(...)`

表现逻辑：

- 先抓取障碍物当前帧；如果抓不到则退回 `idle` 第 1 帧
- 生成独立 `Sprite2D` 快照
- 不发生击退、不整体平移
- 原地短暂发白
- 随后切成 4 个 atlas 分片
- 分片从原地向四周裂开散落并淡出

### 2. 战斗逻辑层改为按对象类型分流

更新文件：

- `Scripts/Battle/Actions/BattleActionService.cs`

改动内容：

- `Unit` 被击杀时：
  - 继续走原有 `PlayKillSequenceAsync(...)`
- `Obstacle` 被摧毁时：
  - 改走新的 `PlayObstacleBreakSequenceAsync(...)`
- 新增障碍物专属表现时长常量：
  - `ObstacleBreakWhitenPresentationDurationSeconds`
  - `ObstacleBreakShatterPresentationDurationSeconds`

## 结果

现在两类对象的销毁表现边界是分开的：

- 敌人：击退后停住，再变白碎裂
- 障碍物：原地碎裂，不击退

这样接下来排查时：

- 如果障碍物碎裂能看到，说明快照与碎裂本身可用
- 如果敌人仍不可见，就继续收缩到敌人的击杀链路本身

## 验证

执行：

- `dotnet build`

结果：

- 编译通过
- 0 errors
