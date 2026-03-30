# GlobalGameSession 变更说明

文件：

- `Scripts/Battle/Shared/GlobalGameSession.cs`

## 本次新增内容

### 1. 新增最小装备槽字段

新增字段：

- `EquippedWeaponItemId`
- `EquippedArmorItemId`
- `EquippedAccessoryItemId`

意义：

- `GlobalGameSession` 不再只保存战斗快照、成长和构筑，也开始承担局外最小装备状态

### 2. 新增解析后角色属性接口

新增方法：

- `GetResolvedPlayerMaxHp()`
- `GetResolvedPlayerMovePointsPerTurn()`
- `GetResolvedPlayerAttackDamage()`
- `GetResolvedPlayerDefenseDamageReductionPercent()`
- `GetResolvedPlayerDefenseShieldGain()`

意义：

- 当前天赋修正与装备修正统一在 session 内收口
- battle 和局外 UI 都应优先依赖这些方法，而不是自己重复计算

### 3. 新增装备穿脱接口

新增方法：

- `IsEquipmentOwned(string itemId)`
- `GetEquippedItemId(string slotId)`
- `TryEquipItem(string slotId, string itemId, out string failureReason)`
- `UnequipItem(string slotId)`

当前槽位固定为：

- `weapon`
- `armor`
- `accessory`

### 4. 新增升级进度读取接口

新增方法：

- `GetExperienceRequiredForNextLevel()`
- `GetExperienceProgressWithinLevel()`
- `GetExperienceNeededToLevelUp()`

意义：

- 角色状态页、成长页、升级提示现在可以统一读取 session，而不需要自己维护另一套经验曲线

### 5. `BuildPlayerSnapshot()` 含义发生变化

当前：

- `max_hp` 输出解析后的生命上限
- `move_points_per_turn` 输出解析后的移动力
- `attack_damage` 输出解析后的攻击力

这意味着：

- `BattleRequest.PlayerSnapshot` 已开始隐式携带装备 / 天赋修正后的战斗关键数值
- 但装备明细本身仍没有平铺进 `BattleRequest`

## 对接时必须注意

- 不要绕过 `GetResolved...()` 系列方法直接读原始字段做战斗计算
- 不要自行发明新的装备槽位字符串
- 如果要把装备改成正式资源系统，优先保留现有装备接口名不变

## 当前局限

- 装备效果仍是代码内硬编码映射
- 仍没有正式的装备资源表与装备服务
- 仍没有为装备单独建立正式 DTO
