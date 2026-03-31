# update-20260331 Scene01 教程场景与可交互 Enemy 搭建

## 目标
- 使用桌面 `resource` 素材 + 现有项目脚本，在 `Scene01` 搭建可试玩教程场景。
- 在 `Scene01` 放置一个可交互 Enemy，按 `E` 可进入战斗。

## 本次改动

### 1) 导入桌面素材（转为项目内 ASCII 命名）
新增目录：`Assets/TutorialResource`
- `enemy_cowboy.png`（来自桌面 `人物/牛仔.png`）
- `heal_station_icon.png`（来自桌面 `场景/回血站1.png`）
- `tile_01.png`（来自桌面 `地图/1.png`）
- `tile_10.png`（来自桌面 `地图/10.png`）

### 2) 新增教程敌人场景
新增文件：`Scene/InteractableItem/TutorialEnemy.tscn`
- 复用脚本：`Scripts/Map/Interaction/Enemy.cs`
- 贴图：`Assets/TutorialResource/enemy_cowboy.png`
- 默认交互：`PromptText = 挑战训练敌人`
- 战斗目标：`BattleScenePath = res://Scene/Battle/Battle.tscn`
- 遭遇 ID：`grunt_debug`
- `DisableAfterInteract = false`（便于反复测试）

### 3) 在 Scene01 落地教程内容
修改文件：`Scene/Scene01.tscn`
- 加入 `TutorialEnemy` 实例（可交互）
- 加入桌面素材装饰：`TutorialHealIcon` / `TutorialEnemyMarker`
- 加入 `TutorialUI`（底部教程说明面板）
  - 文案：WASD 四向走格移动、E 交互、目标是靠近敌人发起战斗
- 将 `MainPlayer` 初始位置设为 `Vector2(80, 64)`，敌人位置设为 `Vector2(208, 64)`

## 验证
- `Scene/Scene01.tscn`：无错误
- `Scene/InteractableItem/TutorialEnemy.tscn`：无错误
- `Scripts/Map/Interaction/Enemy.cs`：无错误
- `Scripts/Map/Actors/Player.cs`：无错误

## 备注
- 项目默认启动场景仍是 `Scene/SystemFeatureLab.tscn`。
- 若要直接体验教程场景，请在 Godot 中单独运行 `Scene/Scene01.tscn`。
