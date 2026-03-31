# report-20260331 MainPlayer 旧素材预览问题修复说明

## 结论
你看到旧人物是“场景默认预览帧未更新”，不是运行时动画逻辑没生效。

## 已修复文件
- `Scene/MainPlayer.tscn`
- `Scene/Character/MainPlayer.tscn`

## 处理方式
- 移除默认预览中对旧素材的依赖：
  - `Assets/Character/Idle/idle_001.png`
  - `Assets/Character/Idle/idle_002.png`
- 默认预览改为新素材条带图首帧（AtlasTexture 裁切）。

## 当前素材策略
- 编辑器预览：显示新主角素材首帧。
- 运行时：由 `Scripts/Map/Actors/Player.cs` 自动切分条带并播放 idle/walk 六方向动画。

## 结果
- 不再使用旧人物素材。
- 编辑器和运行时视觉保持一致。
