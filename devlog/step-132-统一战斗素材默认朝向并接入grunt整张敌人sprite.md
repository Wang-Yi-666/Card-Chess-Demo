# Step 132 - 统一战斗素材默认朝向并接入 grunt 整张敌人 sprite

## 日期

2026-04-05

## 目标

把战斗表现层的朝向逻辑整理成“素材原生朝向”和“单位逻辑朝向”分离的结构，保证后续素材导入时可以直接使用：

- 主角素材默认朝右
- 敌人素材默认朝左
- 房间未显式绘制初始朝向时，主角默认朝右、敌人默认朝左
- grunt 敌人改为直接读取 `Assets/Character/Battle/Enemy/Grunt` 目录里的整张 sprite sheet

## 本次改动

### 1. 动画视图基类新增“素材原生朝向”概念

更新文件：

- `Scripts/Battle/Presentation/BattleAnimatedViewBase.cs`

改动内容：

- 新增 `GetSourceArtFacingSign()`，用于声明素材原生朝向
- 新增 `GetDefaultFacingSign()`，用于声明该单位在没有外部朝向数据时的默认逻辑朝向
- `ApplySpriteFacing()` 不再简单按“朝左就翻转”，而是改成“逻辑朝向”和“素材原生朝向”不一致时才翻转”
- `_Ready()` 阶段就会先按默认逻辑朝向初始化，避免 prefab 初次显示时方向错误
- `Bind()` 时如果外部状态没有给出有效朝向，会回落到单位自己的默认朝向

结果：

- 主角资源画成朝右时，不需要再手动镜像
- 敌人资源画成朝左时，也不会再被错误二次镜像

### 2. 主角与敌人分别声明自己的默认规则

更新文件：

- `Scripts/Battle/Presentation/BattlePlayerView.cs`
- `Scripts/Battle/Presentation/BattleEnemyView.cs`

改动内容：

- `BattlePlayerView` 明确声明：
  - 素材原生朝向为右
  - 默认逻辑朝向为右
- `BattleEnemyView` 完整重写：
  - 不再读取旧的三张散图
  - 不再运行时镜像图片
  - 改为从 `Assets/Character/Battle/Enemy/Grunt` 目录自动寻找第一张 `.png`
  - 按 16x32、3 帧横向 sprite sheet 切帧
  - 默认素材朝向为左
  - 默认逻辑朝向为左

这样后续美术只要把 grunt 敌人的待机图按约定放进对应目录，默认就能直接跑起来。

### 3. 初始朝向的默认回退规则补齐

更新文件：

- `Scripts/Battle/Rooms/BattleRoomTemplate.cs`
- `Scripts/Battle/State/BattleObjectStateManager.cs`

改动内容：

- `BattleRoomTemplate.BuildLayoutDefinition()` 现在区分：
  - 玩家未画朝向时回退为朝右
  - 敌人未画朝向时回退为朝左
- `ResolveFacingForCell()` 不再硬编码具体 tile id，而是使用导出的 facing tile 配置
- `BattleObjectStateManager.ResolveInitialFacing()` 现在支持：
  - 如果 spawn payload 有合法朝向，则使用 payload
  - 如果没有，则玩家回退为右，敌人回退为左

结果：

- `FacingLayer` 仍然是房间级最高优先级配置
- 如果某个房间没画或没画全，也不会再把敌人全部默认为朝右

## 验证

执行：

- `dotnet build`

结果：

- 编译通过
- 0 errors
- 现存大量 `CS8632` 警告仍未处理，这次没有扩散新的编译错误

## 当前约束

- 现在默认资源导入约定是：
  - 主角战斗素材：原生朝右
  - 敌人战斗素材：原生朝左
- 如果后续新增别的敌人类型，推荐继续沿用“敌人原生朝左”的统一规范
- 如果某个特殊敌人素材不是朝左，可以只在对应 view 类里覆写 `GetSourceArtFacingSign()`，不需要改房间层或地图层接口

## 风险与后续

- `BattleEnemyView` 当前自动读取 grunt 目录下第一张 `.png`，适合当前 demo 阶段单资源目录；如果未来同目录下出现多张不同用途贴图，需要再补一个更明确的资源命名约束或资源引用字段
- 本次只整理了战斗表现层与房间朝向默认值，没有改地图层交互接口
