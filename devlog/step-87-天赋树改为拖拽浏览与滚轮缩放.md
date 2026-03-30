# Step 87 - 天赋树改为拖拽浏览与滚轮缩放

## 日期

2026-03-30

## 目标

把综合测试场景中的天赋树浏览方式从“依赖滚动条”改成更接近游戏内体验的：

- 鼠标拖动画布浏览
- 滚轮缩放

并隐藏掉两侧滚动条，避免界面看起来像网页或编辑器。

## 本次改动

### 1. 隐藏滚动条

更新：

- `Scripts/Map/UI/SystemFeatureLabController.cs`

新增：

- `HideTalentTreeScrollBars()`

处理：

- 隐藏 `TalentTreeScroll` 的横向和纵向滚动条
- 同时禁用其鼠标交互

### 2. 支持鼠标拖动浏览

更新：

- `Scripts/Map/UI/SystemFeatureLabController.cs`

在：

- `OnTalentTreeGuiInput(...)`

中补入：

- 左键按下后记录拖动起点
- 鼠标移动时通过修改 scroll 值平移视图

### 3. 支持滚轮缩放

更新：

- `Scripts/Map/UI/SystemFeatureLabController.cs`

新增：

- `ApplyTalentTreeZoom(...)`

行为：

- 鼠标滚轮上滚：放大
- 鼠标滚轮下滚：缩小
- 缩放中心尽量围绕当前鼠标位置
- 缩放范围限制为：
  - `0.75 ~ 1.6`

## 结果

现在综合测试场景中的天赋树浏览方式已经更接近游戏内地图或技能树浏览：

- 不再依赖可见滚动条
- 可以直接拖动画布
- 可以通过滚轮缩放观察整体结构或局部细节
