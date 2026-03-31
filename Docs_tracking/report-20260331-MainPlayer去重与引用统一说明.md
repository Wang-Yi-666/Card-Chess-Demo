# report-20260331 MainPlayer 去重与引用统一说明

## 调整结果
- 项目中的 MainPlayer 场景已去重。
- 统一保留：`Scene/Character/MainPlayer.tscn`。
- 已剔除：`Scene/MainPlayer.tscn`。

## 引用统一范围
- Scene1/Scene2/Scene3
- GridTest30x20
- Legacy/GridTest30x20

## 兼容性与状态
- 引用路径均已更新为 `res://Scene/Character/MainPlayer.tscn`。
- 当前无编译报错。
- 场景加载链路不再受“双 MainPlayer 场景”干扰。
