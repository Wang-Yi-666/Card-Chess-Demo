# update-20260401 移除 MainPlayer 血量与伙伴能量 UI

## 目标
- 隐藏并移除主角界面中的血量与伙伴能量显示，不影响交互和背包。

## 修改文件
- `Scene/Character/MainPlayer.tscn`

## 具体改动
- 删除 `UI/BottomStatusBar` 整个节点树，包括：
  - `PlayerHpLabel`
  - `PlayerHpBar`
  - `PartnerEnergyLabel`
  - `PartnerEnergyBar`
- 保留：
  - `UI/InteractionHintLabel`
  - `UI/InventoryPanel`
  - `UI/InventoryController`

## 结果
- 运行时不再显示底部 HP/EN 状态条。
- 交互提示与背包功能不受影响。
