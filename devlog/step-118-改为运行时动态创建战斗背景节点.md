# Step 118 - 改为运行时动态创建战斗背景节点

## 日期

2026-04-03

## 问题

调试日志已经确认，当前问题不在 JPG 格式，而在于运行时没有拿到场景中的 `BattleBackground` 节点：

- `battle background node or current room missing before attach`
- `battle background missing or texture unresolved`

## 本次改动

更新文件：

- `Scene/Battle/Battle.tscn`
- `Scripts/Battle/BattleSceneController.cs`

处理方式：

- 移除 `Battle.tscn` 中预摆放的 `BattleBackground`
- 改为在运行时通过固定资源路径：
  - `res://Assets/Background/94180512_p2_master1200.jpg`
  直接加载贴图
- 运行时动态创建 `Sprite2D`
- 创建后再继续执行后续的挂房间、定位和显示逻辑

## 结果

现在战斗背景不再依赖场景里是否存在预放节点，只要资源路径可加载，背景节点就会在运行时被明确创建出来。
