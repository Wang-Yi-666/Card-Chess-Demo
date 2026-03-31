# report-20260331 MainPlayer 1.3 尺寸预览说明

## 背景
用户反馈角色过小，要求先将角色改为 `1.3` 做视觉确认。

## 生效文件
- `Scene/Character/MainPlayer.tscn`

## 生效参数
- `AnimatedSprite2D.scale = Vector2(1.3, 1.3)`
- `Shadow.scale = Vector2(1.05, 1.05)`

## 后续建议
若确认采用 1.3，可继续做两步细化：
1. 微调 `CollisionShape2D` 以匹配新体型。
2. 微调 `Shadow.position` 让脚底贴合更自然。
