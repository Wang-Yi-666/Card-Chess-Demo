# Step 106 - 按项目实际表现修正焦点镜头倍率方向

## 日期

2026-04-02

## 问题

在上一轮修复后，用户实机验证发现焦点动作仍然是反的：

- 触发焦点镜头时，棋盘看起来更小
- 不符合“聚焦时镜头更近”的预期

这说明上一轮把 `Camera2D.Zoom` 的方向判断错了。

## 结论

这次不再依据推断，而是以当前项目的实际运行结果为准：

- 在本工程中，`CameraFocusZoomMultiplier` 越大，焦点镜头应该越近
- 因此焦点缩放必须使用乘法，而不是除法

## 本次改动

更新文件：

- `Scripts/Battle/BattleSceneController.cs`

将焦点镜头缩放计算修正为：

- `focusZoom = previousZoom * zoomMultiplier`

同时把误导性的注释改掉，明确说明：

- 当前项目内采用“倍率越大，聚焦越强”的配置语义
- 后续调参时直接围绕这个语义即可

## 结果

修正后，`CameraFocusZoomMultiplier` 的使用规则为：

- `1.0`：不额外放大
- `> 1.0`：镜头更近

这次修改只影响战斗镜头表现，不影响 battle 与 map 的交互接口。

## 验证

- `dotnet build`
  - 结果：`0 errors`
  - 仍保留项目原有 nullable warnings
