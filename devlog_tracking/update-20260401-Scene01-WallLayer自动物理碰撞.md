# update-20260401 Scene01 WallLayer 自动物理碰撞

## 目标
- 为 `Scene/Scene01.tscn` 的 `WallLayer` 补上可阻挡玩家的物理碰撞。

## 方案
- 新增运行时碰撞构建脚本：
  - `Scripts/Map/World/WallLayerCollisionBuilder.cs`
- 逻辑：读取 `WallLayer` 的已用格子，自动生成 `StaticBody2D + CollisionShape2D` 网格碰撞。

## 场景接入
- 在 `Scene01` 根节点下新增 `WallCollisionBuilder` 子节点。
- 参数：
  - `WallLayerPath = ../WallLayer`
  - `CellSize = 16`
  - `CollisionLayer = 2`（环境层）
  - `CollisionMask = 0`
  - `RebuildOnReady = true`

## 效果
- 只要在 `WallLayer` 画了墙砖，运行时就会自动生成阻挡碰撞。
- 玩家无法穿过墙体。

## 验证
- 脚本与场景问题检查：无错误。
- `dotnet build`：成功（仅既有警告）。
