# update-20260401 MainPlayer 隐藏交互提示线

## 变更目标
- 隐藏主角在地图上的交互提示线/扇形调试绘制。

## 修改内容
- 文件：`Scripts/Map/Actors/Player.cs`
- 字段：`ShowInteractionGizmo`
- 调整：默认值从 `true` 改为 `false`。

## 影响范围
- 仅影响 `_Draw()` 中的交互可视化（范围圈、视锥、提示线）。
- 不影响交互判定逻辑与按 `E` 交互功能。

## 验证
- `Player.cs`：No errors found
- `Scene/Character/MainPlayer.tscn`：No errors found
