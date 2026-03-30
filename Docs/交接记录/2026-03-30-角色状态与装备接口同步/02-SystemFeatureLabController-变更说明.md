# SystemFeatureLabController 变更说明

文件：

- `Scripts/Map/UI/SystemFeatureLabController.cs`

## 本次新增内容

### 1. `C` 菜单新增角色状态页

当前页签顺序变为：

1. 角色
2. 背包
3. 天赋
4. 图鉴
5. 构筑

角色页职责：

- 显示主角等级、经验、升级进度、专精点
- 显示生命、移动、攻击、防御等关键状态
- 显示荒川成长等级与能量
- 提供装备槽切换、穿戴、卸下与装备详情查看

### 2. 原背包页职责收窄

背包页现在只负责：

- 显示物品列表
- 显示额外解锁卡牌

不再承担角色数值展示。

### 3. 新增最小示范装备定义

控制器内新增了 demo 级装备定义表：

- `rusted_blade`
- `ion_pistol`
- `patched_coat`
- `reactive_plate`
- `signal_charm`
- `tactical_chip`

用途：

- 先把 UI、session 和 battle 读取链路打通
- 不是正式资源化装备实现

### 4. 新增角色页相关刷新和事件

新增逻辑包括：

- `RefreshStatusView()`
- `RefreshBagView()`
- `RefreshEquipmentSection()`
- `RefreshEquipmentDetail(...)`
- `OnEquipmentSlotSelected(...)`
- `OnEquipmentCandidateSelected(...)`
- `OnEquipButtonPressed()`
- `OnUnequipButtonPressed()`

### 5. 测试背包默认注入装备

`SeedInventoryDefaults()` 现在会注入示范装备。

额外处理：

- 清空背包时会同步卸下三槽装备，避免 session 出现脏状态

## 对接时必须注意

- 这个控制器是综合测试场景控制器，不是最终正式系统 UI
- 但它已经体现了当前 `GlobalGameSession` 的真实接口使用方式
- 如果后续要做正式状态页 / 正式背包页，应优先复用这里对 session 的读取逻辑，而不是重新定义一套角色状态来源

## 当前局限

- 仍有部分历史中文编码污染文本存在
- 装备列表仍基于控制器内示范定义，不是资源加载
- 当前更偏向“功能验证 UI”而不是最终美术完成版
