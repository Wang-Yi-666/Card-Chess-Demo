# Step 105 - 修正 Godot 战斗焦点镜头缩放语义

## 日期

2026-04-02

## 问题

用户反馈焦点运镜触发后，棋盘看起来反而变小，不像是“聚焦放大”。

排查后确认：

- `step-104` 文档里写的是 `focusZoom = previousZoom / zoomMultiplier`
- 但 `Scripts/Battle/BattleSceneController.cs` 里的实际代码仍然是乘法
- 同时之前的实现思路把 `Camera2D.Zoom` 当成了 Unity 风格语义，和 Godot 实际表现相反

## 原因

在 Godot 的 `Camera2D` 中：

- `Zoom` 数值更小，看到的内容更少，画面会更放大
- `Zoom` 数值更大，看到的内容更多，画面会更缩小

也就是说，项目里把焦点倍率写成乘法时：

- `1.35` 不会带来“更近的聚焦”
- 反而会让镜头看到更多棋盘，导致视觉上像缩小

## 本次修复

更新文件：

- `Scripts/Battle/BattleSceneController.cs`

把焦点镜头的缩放计算从：

- `focusZoom = previousZoom * zoomMultiplier`

修正为：

- `focusZoom = previousZoom / zoomMultiplier`

并补充注释，明确说明：

- `CameraFocusZoomMultiplier` 是策划层使用的“放大倍率”
- 进入 Godot `Camera2D.Zoom` 时会换算成更小的 `Zoom` 值

## 当前结果

现在参数语义恢复正常：

- `CameraFocusZoomMultiplier = 1.0`：不额外放大
- `CameraFocusZoomMultiplier > 1.0`：放大更强

因此后续调焦点镜头时，可以继续按“倍率越大，聚焦越强”的直觉来配。

## 影响范围

这次只修正战斗镜头表现层，不改 battle 与 map 的接口，不影响：

- 遭遇触发
- 房间池选择
- 战斗结果回传
- 构筑 / 天赋 / 背包数据流

## 验证

建议在以下动作下确认：

- 普通攻击
- 攻击卡命中
- 荒川造物

预期表现：

- 触发焦点时镜头更近
- 焦点结束后恢复到原视角
