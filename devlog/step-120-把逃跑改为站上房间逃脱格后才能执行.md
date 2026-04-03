# Step 120 - 把逃跑改为站上房间逃脱格后才能执行

## 日期

2026-04-03

## 目标

将原本“任意位置都可点击逃跑”的逻辑改为：

- 只有站在房间定义的可逃脱格上
- 才会显示逃跑按钮
- 才能执行逃跑动作

同时房间中的逃跑区域改为使用单独的 scene tilemap 进行绘制。

## 本次改动

### 1. 房间模板新增 EscapeLayer

更新：

- `Scripts/Battle/Rooms/BattleRoomTemplate.cs`
- `Scripts/Battle/Rooms/BattleRoomTileSetFactory.cs`
- `Scene/Battle/Rooms/GruntDebugRoomA.tscn`
- `Scene/Battle/Rooms/GruntDebugRoomB.tscn`
- `Scene/Battle/Tiles/BattleEscapeTile.tscn`

内容：

- 新增 `EscapeLayer` 作为单独的 scene tilemap 层
- 新增 `DefaultEscapeCells`
- 新增 `GetEscapeCells()`
- 房间运行时会从 `EscapeLayer` 读取所有可逃脱格

对于测试房间 B：

- 逃脱格示例放在最右侧
- 使用 `(15,2) (15,3) (15,4)`
- 避开了现有敌人位置

### 2. 战斗覆盖层新增逃脱格视觉

更新：

- `Scripts/Battle/Visual/BattleBoardOverlay.cs`

效果：

- 可逃脱格程序化显示为偏绿色
- 自动绘制指向棋盘外的箭头
- 四角箭头方向按约定处理：
  - 左上朝左
  - 右上朝上
  - 右下朝右
  - 左下朝下

### 3. 逃跑按钮改为站位触发

更新：

- `Scripts/Battle/BattleSceneController.cs`
- `Scripts/Battle/UI/BattleHudController.cs`

改动：

- HUD 只有当玩家站在可逃脱格上时才显示逃跑按钮
- `OnRetreatRequested()` 会再次校验当前位置是否在逃脱格上

## 结果

现在逃跑动作与房间空间结构绑定：

- 逃跑路线和出口位置成为房间设计的一部分
- 不同预制房间可以画不同的逃脱区域
- 玩家必须真正移动到出口位才能进行逃跑
