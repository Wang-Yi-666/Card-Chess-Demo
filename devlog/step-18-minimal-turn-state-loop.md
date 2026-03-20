# Step 18 - Minimal Turn State Loop

## Date

2026-03-20

## Goal

把现有“可移动可显示”的战斗样机推进到带最小回合状态约束的版本，验证玩家每回合只能移动一次，并能显式结束回合、进入下一回合。

## Implemented

- `TurnActionState` 新增：
  - `CanMove`
  - `CanAct`
  - `AdvanceToNextTurn()`
- `BattleSceneController` 现在真正使用 `TurnState`：
  - 玩家本回合移动后会 `MarkMoved()`
  - 已移动或已结束回合时，不再显示可达范围和预览路径
  - 左键移动会被回合状态拦截
  - 新增 `T` 键：
	- 第一次按下：结束当前回合
	- 第二次按下：进入下一回合
- `BattleHudController` 现在显示：
  - 当前回合号
  - `HasMoved`
  - `HasEndedTurn`
  - `CanMove`
- `Scene/Battle/UI/BattleHud.tscn` 新增 `TurnSummary` 标签

## Behavior

- 玩家每回合只能移动一次。
- 玩家移动后：
  - 棋盘移动高亮消失
  - HUD 中 `Moved` 变为 `True`
  - 再次点击棋盘不会继续移动
- 按一次 `T`：
  - HUD 中 `Ended` 变为 `True`
  - 当前回合不能继续移动
- 再按一次 `T`：
  - 回合号加一
  - `Moved / Ended / CanMove` 重置为新回合状态

## Quick Validation

- 运行战斗场景，左键移动一次后确认无法再次移动。
- 观察 HUD 中的 `Moved`、`Ended`、`Turn` 字段变化。
- 连按两次 `T`，确认第二次会进入下一回合并恢复移动权限。
- `PageUp / PageDown` 仍可实时调整玩家移动力，HUD 和高亮响应不受影响。

## Result

这一步把第 17 步的“状态驱动视觉”继续往前推进成“状态驱动回合限制”，为后续接普通攻击、防御、冥想和卡牌主行动提供了一个可复用的回合壳。
