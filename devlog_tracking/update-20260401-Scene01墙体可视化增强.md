# update-20260401 Scene01 墙体可视化增强

## 目标
提升 Scene01 中墙体与地面的视觉区分度，不等待美术资源。

## 修改文件
- Scene/Scene01.tscn

## 本次改动
1. WallLayer 提升层级并调色
- z_index = 3
- modulate = Color(0.72, 0.8, 0.94, 1)

2. 新增 WallShadowLayer 作为投影层
- 节点类型：TileMapLayer
- 位置偏移：Vector2(0, 2)
- z_index = 2
- modulate = Color(0, 0, 0, 0.24)
- tile_map_data 与 WallLayer 同步

3. GroundLayer 固定底层
- z_index = 0

4. 教程右上提示补充可读性说明
- 文案改为：使用WASD移动  ·  深色墙体不可通行

## 验证
- Scene/Scene01.tscn 无错误
- 运行后墙体在视觉上与地面区分更明显
