# Step 136 - 重做击杀特效为当前帧快照 sprite 并关闭击杀聚焦

## 日期

2026-04-05

## 目标

把当前“看不见敌人碎裂”的击杀特效重做成更稳定的表现链路：

- 不再使用临时白色方块或低信息量几何替身
- 直接抓取敌人当前动画帧的 sprite 图
- 生成独立 `Sprite2D` 做击退、停顿、变白、碎裂散开
- 如果抓不到当前帧，则回退到 `idle` 动画第 1 帧
- 暂时关闭击杀时的镜头聚焦，先专注确认特效本身是否正确出现

## 本次改动

### 1. 动画视图基类增加按指定动画帧抓图接口

更新文件：

- `Scripts/Battle/Presentation/BattleAnimatedViewBase.cs`

新增内容：

- `CaptureAnimationFrameTexture(string animationName, int frameIndex)`

作用：

- 当当前动画帧抓图失败时，可以稳定退回到 `idle` 第 1 帧
- 统一复用帧纹理复制逻辑，避免直接拿运行时资源引用导致后续显示异常

### 2. 击杀特效从“白色 Polygon 方块”改为“当前帧快照 sprite”

更新文件：

- `Scripts/Battle/Presentation/BattlePieceViewManager.cs`

改动内容：

- `PlayKillSequenceAsync()` 改为：
  - 优先抓取敌人当前动画帧
  - 如果抓不到，则抓 `idle` 动画第 1 帧
  - 根据当前 `FlipH` 先生成最终快照纹理
  - 创建独立 `Sprite2D`
  - 删除原始敌人 view
  - 快照 sprite 按攻击方向的反方向击退
  - 停住后整体逐渐变白
  - 再切成 4 个 atlas 分片 sprite 向四周飞散并淡出

效果上更接近：

- “敌人残影/尸体被打飞”
- “停住发白”
- “最后裂开消散”

### 3. 暂时关闭击杀时的镜头聚焦

更新文件：

- `Scripts/Battle/BattleSceneController.cs`

改动内容：

- 普通攻击击杀敌人时，不再触发 `TriggerBattleCameraFocusForCells(...)`
- 攻击卡击杀敌人时，也不再触发同类镜头聚焦
- 荒川造物聚焦保留不变

这样现在更容易直接观察击杀特效是否真的出现，以及出现的位置和节奏是否正确。

## 当前表现逻辑

当前击杀特效顺序为：

1. 抓取敌人当前可见帧
2. 生成独立快照 sprite
3. 原始敌人节点删除
4. 快照向攻击反方向短距离击退
5. 停住后变白
6. 拆成 4 片 sprite 四散飞开并淡出

## 验证

执行：

- `dotnet build`

结果：

- 编译通过
- 0 errors

## 备注

- 这次没有再沿用“白色方块/低信息量调试图形”的方案
- 这次也没有强依赖敌人一定挂了完整 shader 或专门的死亡动画
- 如果后续仍然看不到效果，下一步优先排查的是：
  - 快照 sprite 的层级与位置
  - 击退距离是否太小
  - 分片飞散时间是否过短
