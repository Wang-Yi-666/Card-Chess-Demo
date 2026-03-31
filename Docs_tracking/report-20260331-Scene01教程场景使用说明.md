# report-20260331 Scene01 教程场景使用说明

## 场景目的
`Scene01` 现已作为“基础教程场景”可直接试玩：
- 四向走格移动（WASD）
- 交互键（E）
- 可交互敌人（Enemy）
- 进入战斗场景的完整链路

## 场景组成
- 地形层：`TileMapLayer`（沿用现有地图层）
- 玩家：`Scene/Character/MainPlayer.tscn`
- 教程敌人：`Scene/InteractableItem/TutorialEnemy.tscn`
- 教程提示 UI：`TutorialUI/TutorialTipPanel`

## 交互流程
1. 进入 `Scene01`
2. 用 `WASD` 接近敌人
3. 按 `E` 触发交互
4. 场景切换到 `Scene/Battle/Battle.tscn`

## 使用到的桌面素材（已导入项目）
- `Assets/TutorialResource/enemy_cowboy.png`
- `Assets/TutorialResource/heal_station_icon.png`
- `Assets/TutorialResource/tile_10.png`

## 后续扩展建议
- 在 Scene01 增加第二个教程交互点（如回血站）
- 加入分步教学（移动 -> 交互 -> 战斗）状态门控
- 战斗返回后显示“教程完成”提示
