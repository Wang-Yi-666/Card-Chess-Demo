# update-20260331 MainPlayer 改为 1.3 预览

## 变更目标
- 按用户要求将主角尺寸改回更大的预览值，快速对比视觉手感。

## 修改内容
- 文件：`Scene/Character/MainPlayer.tscn`
- 调整项：
  - `AnimatedSprite2D.scale`：`0.38 -> 1.3`
  - `Shadow.scale`：`0.42 -> 1.05`

## 说明
- 本次为“预览型调整”，未改动移动逻辑。
- 若后续需要可再联动微调碰撞体积与阴影位置。
