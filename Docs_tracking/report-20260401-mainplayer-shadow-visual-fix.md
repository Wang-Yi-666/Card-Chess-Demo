# report-20260401 MainPlayer 阴影视觉修复说明

## 修复目标
- 解决阴影与人物距离过大。
- 解决阴影被地面图层遮挡。

## 实施方案
- 通过 CanvasItem 的 z_index 控制层级：
  - Player > GroundLayer
  - AnimatedSprite2D > Shadow
- 通过 Shadow.position 与 Shadow.scale 微调脚底贴合感。

## 当前参数
- Player.z_index = 5
- Shadow.position = (0, 6)
- Shadow.scale = (0.9, 0.9)
- Shadow.z_index = 0
- AnimatedSprite2D.z_index = 1

## 验证建议
- 在 Scene01 运行时观察角色脚底。
- 打开可见碰撞/调试层，确认仅视觉改动不影响碰撞。
