# 设计模板说明

本目录提供三份可直接填写的设计模板：

- `卡牌设计模板.csv`
- `天赋设计模板.csv`
- `装备设计模板.csv`

推荐使用方式：

1. 用 Excel、WPS 或任意表格工具打开 `csv`
2. 复制一份作为你的正式设计表
3. 不要直接改模板原件，保留模板作为空白基线

## 1. 使用原则

这些模板的目标不是完全约束创意，而是保证：

- 字段统一
- 后续能和程序结构对齐
- 平衡风险能提前被看见
- 美术与文案需求能同步整理

## 2. 卡牌模板填写建议

卡牌设计时，优先确认这些列：

- `card_id`
- `display_name`
- `category`
- `cost`
- `impact_factor`
- `max_copies_in_deck`
- `is_quick`
- `exhausts_on_play`
- `cycle_tags`
- `description`
- `design_goal`
- `balance_risk`

如果是通过学习机制获得的牌，还要重点填写：

- `source_type`
- `learned_from_enemy_id`
- `can_overlimit_carry`
- `overlimit_penalty_profile`

## 3. 天赋模板填写建议

天赋设计时，优先确认这些列：

- `talent_id`
- `display_name`
- `layer_type`
- `branch_tag`
- `cost_mastery_points`
- `prerequisite_talent_ids`
- `prerequisite_branch_mastery`
- `grants_branch_mastery`
- `unlock_card_ids`
- `description`
- `design_goal`

如果天赋会影响构筑，重点填写：

- `grants_impact_capacity_bonus`
- `grants_card_copy_bonus`
- `grants_overlimit_slots`

## 4. 装备模板填写建议

装备设计时，优先确认这些列：

- `equipment_id`
- `display_name`
- `slot_type`
- `rarity`
- `branch_affinity`
- `grants_branch_mastery`
- `grants_impact_capacity_bonus`
- `granted_card_ids`
- `description`
- `drawback`

## 5. 字段约定

### 多值字段

以下字段如果需要填写多个值，统一用 `|` 分隔：

- `branch_tags`
- `prerequisite_talent_ids`
- `unlock_card_ids`
- `granted_card_ids`
- `required_talent_ids`
- `required_equipment_tags`
- `cycle_tags`

### 布尔字段

统一填写：

- `true`
- `false`

### 枚举字段

建议先统一使用以下值，避免后续口径混乱：

- `category`: `Attack` / `Skill`
- `targeting_mode`: `None` / `EnemyUnit` / `StraightLineEnemy` / `FriendlyUnit`
- `layer_type`: `card_branch` / `role_ability` / `cross`
- `slot_type`: `weapon` / `core` / `module` / `trinket`
- `source_type`: `starter` / `talent` / `shop` / `exploration` / `learned` / `boss_reward`

## 6. 推荐流程

推荐你实际设计时按这个顺序做：

1. 先填 `天赋设计模板.csv`
2. 再填 `卡牌设计模板.csv`
3. 最后填 `装备设计模板.csv`

原因是：

- 天赋树先决定长期方向
- 卡牌再对应该方向落具体内容
- 装备最后负责做临时修正和补短板

## 7. 后续可扩展

如果之后你需要，我可以继续基于这三份模板再补：

- 一份敌人招牌技能 / 学习卡设计模板
- 一份精英 / Boss 奖励表模板
- 一份卡牌平衡审查清单
- 一份可直接导入程序的数据字段映射文档
