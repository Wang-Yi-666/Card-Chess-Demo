# report-20260401 MainPlayer 交互提示线隐藏说明

## 背景
玩家希望隐藏主角身上的交互提示线。

## 方案
通过关闭 `Player` 脚本中的 `ShowInteractionGizmo` 默认值实现：
- 不再绘制交互范围圆弧
- 不再绘制视锥边线
- 不再绘制目标提示线

## 保持不变
- `InteractionArea` 重叠检测
- 交互目标筛选逻辑
- 按 `E` 执行交互

## 文件
- `Scripts/Map/Actors/Player.cs`
