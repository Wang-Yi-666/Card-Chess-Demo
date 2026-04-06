# Step 144 - 接入战斗按钮 sprite 并改造牌堆为单按钮分页查看

## 日期

2026-04-05

## 目标

根据 `Assets/UI/Battle/Buttons` 中新增的按钮资源：

- 把战斗 HUD 里现有纯文字按钮替换成 sprite 按钮
- 保持按钮尺寸一致
- 在 sprite 下方保留统一的圆角矩形打底
- 牌堆入口从多个独立按钮改为单一入口
- 点开后使用分页查看：
  - 抽牌堆
  - 弃牌堆
  - 消耗牌堆

## 本次改动

### 1. 战斗按钮接入 sprite 资源

更新文件：

- `Scripts/Battle/UI/BattleHudController.cs`

改动内容：

- 新增 `Assets/UI/Battle/Buttons` 的按钮贴图扫描与缓存
- 新增 `ApplyBattleSpriteButton(...)`
- 使用统一圆角矩形 stylebox 作为底板
- 当前已接入 sprite 的按钮：
  - 攻击
  - 防御
  - 冥想
  - 逃跑
  - 日志
  - 牌堆

说明：

- `EndTurn` 与 `Arakawa` 相关按钮当前未发现对应资源，继续保留文字按钮
- 这样不会为缺失资源额外引入不必要的占位图

### 2. 牌堆入口收束为单按钮

更新文件：

- `Scene/Battle/UI/BattleHud.tscn`
- `Scripts/Battle/UI/BattleHudController.cs`

改动内容：

- 原先的：
  - `DrawPileButton`
  - `DiscardPileButton`
  - `ExhaustPileButton`
  被收束为：
  - `PileButton`

行为改为：

- 点击一次打开牌堆面板
- 再次点击关闭牌堆面板

### 3. 牌堆弹窗改为分页结构

更新文件：

- `Scene/Battle/UI/BattleHud.tscn`
- `Scripts/Battle/UI/BattleHudController.cs`

改动内容：

- `PilePopup` 内部不再是单个 `RichTextLabel + Scroll + Grid`
- 改为 `TabContainer`
- 包含三个分页：
  - `DrawTab`
  - `DiscardTab`
  - `ExhaustTab`
- 每个分页都有：
  - 空状态 `EmptyLabel`
  - 对应的 `ScrollContainer`
  - 对应的 `GridContainer`

### 4. 牌堆分页内容按需刷新

更新文件：

- `Scripts/Battle/UI/BattleHudController.cs`

新增逻辑：

- `RefreshPilePopup(...)`
- `PopulatePileTab(...)`
- `OpenPilePopup()`

作用：

- 打开牌堆时，按当前战斗状态生成三页内容
- 面板打开期间，如果三类牌堆内容变化，会自动刷新分页
- 标签标题会实时显示数量：
  - `抽牌堆 n`
  - `弃牌堆 n`
  - `消耗堆 n`

## 当前 UI 约束

- sprite 按钮目前统一使用相同尺寸与统一底板
- 牌堆查看逻辑已经从“三个入口”改成“一个入口 + 三分页”
- 卡牌展示仍复用现有 `BattleCardView`

## 验证

执行：

- `dotnet build`

结果：

- 编译通过
- 0 errors
