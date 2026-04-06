# Step 138 - 恢复背景地砖显示并让障碍物销毁走碎裂特效

## 日期

2026-04-05

## 目标

根据新一轮排查结果：

- 背景和地砖隐藏后，敌人击杀特效仍然看不到
- 说明问题大概率不在底层遮挡

因此本次改为：

- 恢复正常战斗显示
- 让可破坏障碍物在被打碎时也走同一套碎裂快照特效

这样可以进一步判断：

- 快照碎裂特效本身是否在障碍物上能正常出现
- 如果障碍物可见而敌人不可见，就说明问题更接近敌人 view / 帧抓取链路

## 本次改动

### 1. 恢复背景与地砖显示

更新文件：

- `Scripts/Battle/BattleSceneController.cs`

调整内容：

- `ShowBattleBackgroundLayer` 从 `false` 恢复为 `true`
- `ShowBattleFloorLayer` 从 `false` 恢复为 `true`

### 2. 障碍物销毁改为走碎裂特效

更新文件：

- `Scripts/Battle/Actions/BattleActionService.cs`

调整内容：

- 目标被摧毁时：
  - `Unit` 继续走 `PlayKillSequenceAsync(...)`
  - `Obstacle` 现在也改为走 `PlayKillSequenceAsync(...)`
- 障碍物碎裂时的击退距离设为单位的 `0.75` 倍，避免视觉上飞得过远
- 原来的障碍物销毁 `PlayDefeat()` 路径移除

## 结果

现在如果玩家打碎可破坏障碍物：

- 障碍物会进入与单位同一套的快照碎裂演出
- 可以用来验证碎裂系统本身是否工作

## 验证

执行：

- `dotnet build`

结果：

- 编译通过
- 0 errors

## 排查意义

如果下一轮测试里：

- 障碍物碎裂可见，敌人击杀仍不可见
  - 说明问题更可能出在敌人当前帧抓图或敌人 view 状态
- 障碍物碎裂也不可见
  - 说明问题更可能在快照 sprite 的生成位置、生命周期或 kill fx 根节点链路
