# update-20260331 保留 Character/MainPlayer 并移除重复场景

## 目标
- 项目中仅保留 `Scene/Character/MainPlayer.tscn`。
- 删除重复的 `Scene/MainPlayer.tscn`。

## 本次处理
1. 统一场景引用到 `res://Scene/Character/MainPlayer.tscn`：
   - `Scene/Scene1.tscn`
   - `Scene/Scene2.tscn`
   - `Scene/Scene3.tscn`
   - `Scene/GridTest30x20.tscn`
   - `Scene/Legacy/GridTest30x20.tscn`

2. 删除重复文件：
   - `Scene/MainPlayer.tscn`

3. 额外修复：
   - 补回缺失的 `Scripts/UI/Dialogue/GalDialogueOverlay.cs`，修复 `Npc` 对话框编译错误。

## 验证
- `Scene/**/MainPlayer.tscn` 仅剩：`Scene/Character/MainPlayer.tscn`
- 全局搜索 `res://Scene/MainPlayer.tscn` 无结果
- Problems：`No errors found`
