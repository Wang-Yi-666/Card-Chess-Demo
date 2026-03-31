# report-20260401 Scene01 WallLayer 碰撞配置说明

## 你现在在哪里设置“墙不能走”
### 自动方案（已接好）
- 文件：`Scripts/Map/World/WallLayerCollisionBuilder.cs`
- 场景节点：`Scene/Scene01.tscn` -> `WallCollisionBuilder`

该节点会在运行时读取 `WallLayer` 所有已用格子，并自动生成碰撞，不需要在 TileSet 里逐个手配。

## 使用方式
1. 在 `WallLayer` 里画墙砖。
2. 运行场景。
3. 玩家将被墙格阻挡。

## 可调参数
- `CellSize`：墙碰撞每格尺寸（当前 16）
- `CollisionLayer`：碰撞层（当前环境层 2）

## 注意
- 如果以后你的地图改成 32 像素格，只需把 `CellSize` 改为 32。
- 修改墙布局后重跑场景即可自动重建碰撞。
