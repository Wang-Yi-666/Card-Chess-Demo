# update-20260401 MainPlayer 阴影层级与偏移修复

## 问题
- 阴影离人物脚底过远。
- 阴影被 GroundLayer 压在下面。

## 修改文件
- Scene/Character/MainPlayer.tscn

## 修改内容
1. 提升角色整体绘制层级
- Player.z_index: 5

2. 调整阴影位置与大小
- Shadow.position: (0, 9) -> (0, 6)
- Shadow.scale: (1.05, 1.05) -> (0.9, 0.9)
- Shadow.z_index: -1 -> 0

3. 确保角色本体在阴影上层
- AnimatedSprite2D.z_index: 1

## 结果
- 阴影显示在地面层之上。
- 阴影更贴近人物脚底，视觉更自然。
