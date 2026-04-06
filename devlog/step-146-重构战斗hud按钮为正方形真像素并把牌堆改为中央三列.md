# Step 146 - 重构战斗 HUD 按钮为正方形真像素并把牌堆改为中央三列

## 日期

2026-04-05

## 目标

根据最新要求，调整战斗 HUD：

- 按钮使用真像素显示，不允许缩放畸变
- 按钮外框统一为正方形
- 牌堆面板移到更靠近屏幕中央的位置
- 抽牌堆、弃牌堆、消耗堆三列同时显示
- 每张牌完整显示，不再重叠
- 到底部时允许继续下滚一小段，并自动回弹

## 本次改动

### 1. 战斗按钮改为正方形像素按钮

更新文件：

- `Scripts/Battle/UI/BattleHudController.cs`
- `Scene/Battle/UI/BattleHud.tscn`

调整内容：

- 右侧战斗按钮统一改为 `28x28`
- 按钮图标保持 `16x16`
- 图标不再依赖 `Button.Icon` 的默认绘制逻辑
- 改为在按钮内部创建 `TextureRect`
- 使用：
  - `TextureFilter = Nearest`
  - `StretchMode = KeepCentered`
  - 固定 `16x16`

结果：

- 图标会按原始像素大小居中显示
- 不再因为按钮尺寸或布局拉伸导致像素模糊/畸变

### 2. 牌堆面板改为中央三列

更新文件：

- `Scene/Battle/UI/BattleHud.tscn`
- `Scripts/Battle/UI/BattleHudController.cs`

调整内容：

- `PilePopup` 从右上角靠边布局改为屏幕中央附近布局
- 原分页结构删除
- 改为 `PileColumns`
  - `DrawColumn`
  - `DiscardColumn`
  - `ExhaustColumn`

每列独立包含：

- 空状态提示
- 自己的 `ScrollContainer`
- 自己的牌堆内容容器
- 自己的底部回弹 spacer

### 3. 牌堆卡牌改为纵向完整显示

更新文件：

- `Scene/Battle/UI/BattleHud.tscn`

调整内容：

- 每列 `PileGrid.columns = 1`
- 纵向间距提高到 `6`

结果：

- 每张牌完整显示
- 同列内不再重叠

### 4. 增加底部 overscroll 回弹

更新文件：

- `Scripts/Battle/UI/BattleHudController.cs`

新增逻辑：

- `SetupPileScrollBounce(...)`
- `OnPileScrollGuiInput(...)`
- `StartPileOverscrollBounce(...)`

当前行为：

- 当滚动到底部后继续滚轮下翻
- 会临时增加底部 spacer 高度
- 允许继续多滚一小段
- 随后自动以 `Back` 曲线回弹到正常底端

## 结果

当前牌堆查看逻辑已经变为：

- 一个牌堆入口按钮
- 打开后中央三列同时显示三种牌堆
- 每列可独立滚动
- 每张牌完整展示
- 底部带回弹冗余

## 验证

执行：

- `dotnet build`

结果：

- 编译通过
- 0 errors
