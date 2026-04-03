# Step 122 - 改为仅在玩家击杀敌人时聚焦并新增碎裂击杀特效

## 日期

2026-04-03

## 目标

调整战斗表现规则：

- 去掉普通攻击命中 / 卡牌攻击命中的常规镜头聚焦
- 仅保留“玩家攻击击杀敌方单位”时的镜头聚焦
- 为敌方单位死亡新增更强的击杀演出

## 本次改动

### 1. 攻击聚焦规则收紧

更新文件：

- `Scripts/Battle/BattleSceneController.cs`

改动内容：

- 普通攻击命中时不再默认触发镜头聚焦
- 卡牌攻击命中时不再默认触发镜头聚焦
- 只有当玩家造成击杀，并且目标是敌方单位时，才会触发击杀聚焦

### 2. 视图层新增统一击杀序列

更新文件：

- `Scripts/Battle/Presentation/BattleAnimatedViewBase.cs`
- `Scripts/Battle/Presentation/BattlePieceViewManager.cs`
- `Shaders/Battle/KillShatter.gdshader`

击杀演出顺序：

- 被击杀目标沿受击方向击退更远
- 到达终点后停留在该位置
- 当前动画快速定格
- sprite 快速白闪
- 白闪后进入 shader 碎裂散开
- 最终渐隐消失

### 3. 战斗动作服务接入击杀时序

更新文件：

- `Scripts/Battle/Actions/BattleActionService.cs`

改动内容：

- 普通攻击现在会回传是否击杀
- 伤害应用现在允许传入击退方向
- 敌方单位被击杀时，会播放完整碎裂击杀演出
- `LastImpactPresentationDurationSeconds` 会覆盖这段击杀演出时长，确保回合推进不会抢在演出前

## 当前击杀特效参数

当前默认参数为：

- 击退距离：`11 px`
- 击退时长：`0.10 s`
- 白闪时长：`0.07 s`
- 碎裂时长：`0.26 s`

shader 当前表现：

- 先白化
- 再按分块随机方向散开
- 散开过程中同步透明衰减
- 不带重力与物理下坠

## 结果

现在攻击表现会更克制：

- 普通命中不再频繁拉镜头
- 只有真正击杀敌方单位时，才会强调镜头和死亡演出
- 战斗节奏更集中在“击杀时刻”的确认感上

## 验证

- `dotnet build`
  - 结果：`0 errors`
  - 保留项目原有 nullable warnings
