# Step 128 - 强化击杀残影可见性并新增战斗返回地图黑影扫屏转场

## 日期

2026-04-03

## 本次改动

### 1. 强化击杀残影可见性

更新文件：

- `Scripts/Battle/Presentation/BattlePieceViewManager.cs`
- `Scripts/Battle/Presentation/BattleAnimatedViewBase.cs`
- `Scripts/Battle/Board/BoardObject.cs`

改动内容：

- 击杀残影不再从透明开始，而是先保留一张可见底图
- 白闪后底图再渐隐
- 碎片散开与底图消失同步发生
- 残影 `z_index` 提高，降低被其他对象遮挡的概率
- 生命伤害数字改为显示经过护盾后剩余的总伤害，支持溢出值

### 2. 新增战斗返回地图的退场转场

新增文件：

- `Scripts/Map/Transitions/BattleReturnTransitionOverlay.cs`
- `Shaders/Transitions/BattleReturnSweep.gdshader`
- `Scene/Transitions/BattleReturnTransitionOverlay.tscn`

并更新：

- `Scripts/Battle/BattleSceneController.cs`

效果：

- 战斗结束切回地图前
- 屏幕会先出现一个黑影从右往左快速掠过
- 然后再切回地图场景
