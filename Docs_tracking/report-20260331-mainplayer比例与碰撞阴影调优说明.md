# report-20260331 MainPlayer 比例与碰撞阴影调优说明

## 调整目的
提升像素风探索场景的角色阅读性，使主角尺寸与 16px 网格场景更协调。

## 调整策略
- 采用推荐的 `0.38` 显示缩放。
- 碰撞体改为脚底小碰撞盒，避免大体积角色碰撞破坏走格子手感。
- 阴影缩放与位置同步下调，减少“人物悬浮感”。

## 调整项
- `AnimatedSprite2D.scale = (0.38, 0.38)`
- `Shadow.position = (0, 9)`
- `Shadow.scale = (0.42, 0.42)`
- `CollisionShape2D.shape size = (12, 10)`
- `CollisionShape2D.position = (0, 9)`
- `InteractionArea radius = 96`

## 说明
若后续你希望角色显得稍微更“主角化”，可在 `0.40 ~ 0.44` 区间微调；
若要更接近老宝可梦体量，可降到 `0.34 ~ 0.36`。
