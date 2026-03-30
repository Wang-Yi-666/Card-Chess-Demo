# Step 89 - 拆分角色状态与背包并接入最小装备槽

## 日期

2026-03-30

## 目标

把综合测试场景里原本混在一起的“角色信息 + 背包”拆成更接近传统 JRPG 的两页：

- 角色状态
- 背包

同时补上最小可运行的装备系统，让状态页可以进行穿脱装备，并显示等级、经验、升级进度、专精点等成长数据。

## 本次改动

### 1. GlobalGameSession 增加最小装备槽

更新：

- `Scripts/Battle/Shared/GlobalGameSession.cs`

新增：

- `EquippedWeaponItemId`
- `EquippedArmorItemId`
- `EquippedAccessoryItemId`

补充能力：

- 按槽位穿戴 / 卸下装备
- 判断装备是否已拥有
- 解析装备提供的角色加成
- 计算：
  - 生命上限
  - 移动力
  - 攻击力
  - 防御减伤
  - 防御附盾
- 计算当前等级的经验进度与升级所需经验

当前采用最小示范装备池：

- 武器：`rusted_blade`、`ion_pistol`
- 护甲：`patched_coat`、`reactive_plate`
- 饰品：`signal_charm`、`tactical_chip`

### 2. 装备加成接入战斗玩家状态读取

更新：

- `Scripts/Battle/State/BattleObjectStateManager.cs`

处理：

- 玩家对象初始化与同步时，改为读取 session 的已解析属性
- 装备加成现在会影响：
  - 玩家最大生命
  - 玩家移动力
  - 玩家攻击力

这样测试场景里穿脱装备后，不只是 UI 数字变化，也会传导到战斗侧玩家状态。

### 3. 测试场景 C 菜单拆分为“角色 / 背包”

更新：

- `Scene/SystemFeatureLab.tscn`
- `Scripts/Map/UI/SystemFeatureLabController.cs`

处理：

- 在原 TabContainer 中新增 `StatusTab`
- 现在页签顺序为：
  - 角色
  - 背包
  - 天赋
  - 图鉴
  - 构筑

角色页内容：

- 左侧：
  - 角色等级
  - 当前经验 / 升级所需
  - 专精点
  - 当前生命
  - 移动力
  - 攻击
  - 攻击范围
  - 防御减伤
  - 防御附盾
  - 荒川成长等级
  - 荒川能量
  - 已解锁天赋与卡牌数量

- 右侧：
  - 装备槽列表
  - 当前槽位可装备物品列表
  - 装备 / 卸下按钮
  - 装备详情说明

背包页内容：

- 专门显示物品列表
- 显示额外解锁卡牌
- 不再把角色属性和背包内容混在同一页

### 4. 测试背包补充装备物品

更新：

- `Scripts/Map/UI/SystemFeatureLabController.cs`

处理：

- `SeedInventoryDefaults()` 现在会同时注入示范装备
- 清空背包时会同步卸下全部装备，避免出现“背包没物品但仍然穿着”的脏状态

## 结果

现在综合测试场景中的 `C` 菜单已经更接近传统 JRPG 的系统页结构：

- `角色` 页负责看数值、成长和穿脱装备
- `背包` 页负责看物品

同时，装备系统已经不是纯 UI 展示，而是接入了全局状态与战斗侧玩家属性读取。

## 验证

- `dotnet build`
  - 结果：`0 errors`
  - 仍保留项目历史 nullable warnings，本次未处理
