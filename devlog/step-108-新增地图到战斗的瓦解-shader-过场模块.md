# Step 108 - 新增地图到战斗的瓦解 Shader 过场模块

## 日期

2026-04-02

## 目标

为地图进入战斗增加一个最小可用的过场表现：

- 先看到当前地图画面发生瓦解
- 再切换到战斗场景

同时把过场做成独立模块，后续如果要换成别的切场动画，只改过场模块本身，不改地图交互入口和战斗数据接口。

## 本次改动

### 1. 新增独立过场模块

新增文件：

- `Scripts/Map/Transitions/MapBattleTransitionOverlay.cs`
- `Shaders/Transitions/MapBattleDissolve.gdshader`
- `Scene/Transitions/MapBattleTransitionOverlay.tscn`

结构说明：

- `MapBattleTransitionOverlay.tscn` 是地图进战斗时统一播放的过场场景
- `MapBattleTransitionOverlay.cs` 负责驱动进度参数和动画时序
- `MapBattleDissolve.gdshader` 负责把当前地图画面做成分块瓦解效果

### 2. 地图进战斗链路改为“先播过场，再切场景”

更新文件：

- `Scripts/Map/Transitions/MapBattleTransitionHelper.cs`

改动内容：

- 保留原有 `BattleRequest` / 地图返回上下文的准备逻辑
- 不再直接立刻 `ChangeScene`
- 改为：
  - 创建过场 overlay
  - 播放瓦解 shader 动画
  - 动画结束后切换到 battle scene

### 3. 地图交互对象补上延迟失败回调

更新文件：

- `Scripts/Map/Interaction/Enemy.cs`
- `Scripts/Map/Interaction/BattleEncounterEnemy.cs`
- `Scripts/Map/Interaction/SceneDoor.cs`

补充内容：

- 如果过场结束后切场景失败，会恢复 `_isTransitioning`
- 避免交互对象停留在“切换中”状态

## 当前效果

现在从地图进入战斗时，会先看到地图画面以分块漂移和溶解的方式收束，再进入战斗场景。

## 预留给后续修改的点

后续可以直接围绕以下模块继续替换或增强：

- `Scene/Transitions/MapBattleTransitionOverlay.tscn`
- `Scripts/Map/Transitions/MapBattleTransitionOverlay.cs`
- `Shaders/Transitions/MapBattleDissolve.gdshader`

后续常改项包括：

- 动画时长
- 分块大小
- 扩散节奏
- 漂移强度
- 边缘颜色
- 最终收束颜色
- 是否叠加扫描线 / 故障 / 色偏

## 影响范围

本次改动只影响地图进入战斗的表现层切换流程，不修改 battle 与 map 之间的核心数据结构：

- `BattleRequest`
- `PendingBattleEncounterId`
- `MapResumeContext`

这些接口保持不变。
