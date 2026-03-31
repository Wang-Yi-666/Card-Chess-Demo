# report-20260331 MainPlayer 动画素材接入说明

## 接入范围
- 场景：`Scene/MainPlayer.tscn`
- 脚本：`Scripts/Map/Actors/Player.cs`
- 素材：`Assets/Character/MainPlayer/{Idle,Walk,Shadow.png}`

## 实现方式
采用“运行时切条带”而非手动拆帧：
1. 读取 `Idle` 与 `Walk` 方向条带。
2. 根据宽高按默认帧宽 48 自动切分。
3. 生成 `SpriteFrames` 并绑定到 `AnimatedSprite2D`。
4. 根据玩家输入方向自动切换 `idle_*` / `walk_*` 动画。

## 阴影处理
- 在 `Player` 节点下添加 `Shadow(Sprite2D)`。
- 使用 `res://Assets/Character/MainPlayer/Shadow.png`。
- 设置 `z_index = -1` 让阴影位于角色下方。

## 动画命名
- `idle_down`, `idle_up`, `idle_left_down`, `idle_left_up`, `idle_right_down`, `idle_right_up`
- `walk_down`, `walk_up`, `walk_left_down`, `walk_left_up`, `walk_right_down`, `walk_right_up`

## 可调参数（Player.cs）
- `SpriteFrameWidth`（默认 48）
- `SpriteFrameHeight`（默认 64）
- `SpriteFps`（默认 10）
- `AnimatedSpritePath`（默认 `AnimatedSprite2D`）

## 结果
主角已套用桌面素材，进入场景后可看到：
- 阴影显示
- 待机动画播放
- 移动时 walk 动画按方向切换
