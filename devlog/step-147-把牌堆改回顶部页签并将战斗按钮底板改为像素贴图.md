# Step 147 - 把牌堆改回顶部页签并将战斗按钮底板改为像素贴图

## 日期

2026-04-05

## 目标

对齐最新 UI 要求：

- 牌堆界面不是三列同时展开，而是像地图 `C` 面板那样，顶部一排标签页可切换
- 标签在同一行显示
- 战斗按钮底板改为真正的像素贴图，不再使用容易发糊的圆角样式

## 本次改动

### 1. 牌堆弹窗改回顶部页签结构

更新文件：

- `Scene/Battle/UI/BattleHud.tscn`
- `Scripts/Battle/UI/BattleHudController.cs`

调整内容：

- `PilePopup` 继续保持居中
- 内容区从三列同时展开改为 `TabContainer`
- 页签一行显示：
  - 抽牌堆
  - 弃牌堆
  - 消耗堆
- 每个页签内部仍保留：
  - 完整卡面展示
  - 单列不重叠
  - 底部 overscroll 回弹 spacer

### 2. 按钮底板改为程序生成的像素贴图

更新文件：

- `Scripts/Battle/UI/BattleHudController.cs`

调整内容：

- sprite 按钮改为：
  - `flat = true`
  - 自带 `BgRect` 背景贴图
  - 自带 `IconRect` 图标贴图
- 背景不再依赖 `StyleBoxFlat` 圆角绘制
- 改为代码直接生成 `28x28` 像素风圆角方底图
- 图标继续使用 `16x16` 真像素显示

## 验证

执行：

- `dotnet build`

结果：

- 编译通过
- 0 errors

## 备注

这次已经把结构改到用户要求的方向，但还没有做运行时截图级别的视觉确认。
