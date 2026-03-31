# update-20260331 MainPlayer 默认预览切换到新序列素材

## 问题
- Godot 编辑器中 MainPlayer 仍显示旧人物贴图（`Assets/Character/Idle/idle_001.png` / `idle_002.png`）。
- 用户明确要求不再使用旧素材。

## 根因
- 运行时动画已在 `Scripts/Map/Actors/Player.cs` 中替换为 `Assets/Character/MainPlayer/Idle|Walk` 条带图。
- 但 `Scene/MainPlayer.tscn` 与 `Scene/Character/MainPlayer.tscn` 的 `AnimatedSprite2D` 默认 `SpriteFrames` 仍引用旧贴图，因此编辑器预览看起来是旧人物。

## 修复
1. `Scene/MainPlayer.tscn`
   - 将默认 ext_resource 从旧 idle 图替换为：
     - `Assets/Character/MainPlayer/Idle/Idle_Down.png`
     - `Assets/Character/MainPlayer/Idle/Idle_Up.png`
   - 新增 AtlasTexture 子资源，裁切首帧 `Rect2(0, 0, 48, 64)` 作为默认预览。
2. `Scene/Character/MainPlayer.tscn`
   - 做同样替换和首帧裁切。

## 验证
- 搜索 MainPlayer 场景中旧素材路径：无匹配。
- 场景与脚本错误检查：无错误。
- Scene1/2/3/GridTest 均引用 `Scene/MainPlayer.tscn`，会看到更新后的默认预览与运行时动画。
