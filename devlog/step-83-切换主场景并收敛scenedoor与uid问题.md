# Step 83 - 切换主场景并收敛 SceneDoor 与 UID 问题

## 日期

2026-03-29

## 目标

把项目主场景切换到当前地图域中带有最新敌人交互的地图场景，并收敛 merge 后暴露出来的 `SceneDoor` 集中报错与资源 UID 混乱问题。

## 本次改动

### 1. 切换主场景到当前地图域主链

更新：

- `project.godot`

调整：

- `run/main_scene` 从 `res://Scene/Scene1.tscn`
- 切换为 `res://Scene/Mainlevel.tscn`

原因：

- `Scene1.tscn` 仍然主要挂着旧 `Character / Item` 体系的场景资源
- `Mainlevel.tscn` 已接入：
  - `Scripts/Map/Actors/Player.cs`
  - `Scripts/Map/Controllers/MapSceneController.cs`
  - `Scene/BattleEncounterEnemy.tscn`
  - `Scene/SceneDoor.tscn`

它更符合当前地图层与 battle 层的最新交互方向。

### 2. SceneDoor 集中报错原因分析

定位结果：

- `Scripts/Map/Interaction/SceneDoor.cs` 在 `_Ready()` 阶段，只要门没有配置目标，就会直接发出 warning
- merge 后项目里存在多张“可复用但未配置目标”的门场景或门实例
- 因此进入地图或打开相关场景时，`SceneDoor` 会集中刷出“未配置目标”的报错 / 警告

这类问题本质上不是门脚本逻辑崩溃，而是：

- 可复用模板场景默认空配置
- 但脚本在加载期就把这种空配置当成异常输出

### 3. 收敛 SceneDoor 的集中报错

更新：

- `Scripts/Map/Interaction/SceneDoor.cs`

调整：

- 移除 `_Ready()` 中对“未配置目标”的启动期 warning

保留：

- 真正交互时如果目标无效，仍然会报错

效果：

- 模板门、空配置门不再在场景加载时集中刷屏
- 真实错误仍保留在交互时暴露

### 4. 修正主链地图里的门配置

更新：

- `Scene/Mainlevel.tscn`

调整：

- 给当前主链地图里的 `SceneDoor` 补入明确目标：
  - `res://Scene/Scene2.tscn`
- 同时补入提示文本

这样主场景里的门不再是“空门”。

### 5. 修正旧测试场景中的错误 UID 与旧式跳转

更新：

- `Scene/Scene2.tscn`
- `Scene/Scene3.tscn`

调整：

- 把它们对 `Scene/Door.tscn` 的错误 `uid` 修正为当前实际 `uid`
- 把 `NextScenePath` 从 `uid://...` 形式收敛为明确的 `res://...` 路径

结果：

- 减少 merge 后资源 `uid` 丢失 / 错配带来的不稳定性
- 旧测试链路也不再依赖脆弱的 `uid` 跳转

### 6. 收敛旧脚本与新脚本的 UID 冲突

处理：

- 恢复根目录兼容版：
  - `IInteractable.cs`
  - `InteractableTemplate.cs`
- 但为它们分配新的独立 `uid`

同时：

- 给旧 `Character / Item` 体系保留的脚本分配新的独立 `uid`
- 更新对应旧场景中的脚本 `uid`

目的：

- 避免旧 `Character / Item` 路径与当前 `Map` 域路径继续共享同一个 `uid`
- 减少 Godot 资源解析时的“路径对了但 uid 混了”的问题

## 验证

执行：

- `dotnet build`

结果：

- `0 error`

补充检查：

- 当前 `.uid` 文件已无重复值
- `project.godot` 主场景已切换到 `Mainlevel.tscn`

## 结果

当前项目的主链状态已经更清晰：

- 主场景切到当前地图域主链
- `SceneDoor` 不再因模板空配置在加载时集中刷错
- 主链门已有明确目标
- 旧测试场景中的错误 `uid` 与脆弱跳转已收敛
- merge 后最明显的资源 UID 冲突已清理
