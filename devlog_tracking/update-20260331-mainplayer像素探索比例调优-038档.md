# update-20260331 MainPlayer 像素探索比例调优（0.38 档）

## 目标
根据像素风探索建议，将主角视觉尺寸从偏大状态调到更接近老宝可梦/塞尔达的阅读比例，并同步碰撞与阴影。

## 修改文件
- `Scene/Character/MainPlayer.tscn`

## 修改内容
1. 角色显示缩放
- `AnimatedSprite2D.scale`：`(1.4, 1.4)` -> `(0.38, 0.38)`

2. 阴影对齐与缩放
- `Shadow.position`：`(0, 18)` -> `(0, 9)`
- `Shadow.scale`：新增 `(0.42, 0.42)`

3. 角色碰撞体（贴脚底）
- `RectangleShape2D.size`：`(64, 68)` -> `(12, 10)`
- `CollisionShape2D.position`：`(-1, -0.5)` -> `(0, 9)`

4. 交互范围圈
- `InteractionArea CircleShape2D.radius`：`160` -> `96`

## 结果
- 主角视觉体积明显缩小，更符合像素探索比例。
- 脚底碰撞更接近“走格子”风格，不再使用过大的全身碰撞。
- 交互范围由超大圈回归常规范围。

## 验证
- 当前检查未发现新增错误。
