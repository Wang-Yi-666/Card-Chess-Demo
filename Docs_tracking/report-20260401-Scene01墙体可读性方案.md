# report-20260401 Scene01 墙体可读性方案

## 结论
不需要等待美术即可显著提高墙体识别度，当前通过层级、染色、投影层三种手段完成。

## 已落地方案
- GroundLayer：底层
- WallShadowLayer：中层阴影投影
- WallLayer：上层实体墙

## 关键参数
- WallShadowLayer.position = Vector2(0, 2)
- WallShadowLayer.modulate = Color(0, 0, 0, 0.24)
- WallLayer.modulate = Color(0.72, 0.8, 0.94, 1)

## 玩家引导文案
- 右上提示新增：深色墙体不可通行

## 后续可选优化
1. 按地图区域切换不同墙色调
2. 增加墙顶高光层
3. 墙角添加单独贴图强化边界
