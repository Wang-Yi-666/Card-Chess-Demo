# Step 110 - 接入战斗世界背景图并让其随棋盘同屏移动

## 日期

2026-04-02

## 目标

把 `Assets/background` 里的背景图暂时作为战斗背景使用，并满足：

- 不作为 UI 元素
- 随战斗相机和棋盘一起移动
- 不做视差

## 本次改动

更新文件：

- `Scene/Battle/Battle.tscn`
- `Scripts/Battle/BattleSceneController.cs`

具体处理：

- 在战斗场景新增 `BackdropRoot/BattleBackground`
- 使用 `Sprite2D` 挂载：
  - `res://Assets/background/94180512_p2_master1200.jpg`
- 背景节点放在世界层，位于 `RoomContainer` 后方，而不是 `BattleHud` 或其他 `Control` 层

同时在 `BattleSceneController` 中新增背景初始化逻辑：

- 根据当前房间的棋盘尺寸自动计算背景中心点
- 根据棋盘尺寸自动计算一个覆盖比例
- 将背景居中到棋盘中部

## 结果

现在战斗背景图属于战斗世界坐标系的一部分：

- 镜头移动时，背景会和棋盘一起移动
- 不会像 UI 那样钉在屏幕上
- 不会出现视差分层效果

## 备注

当前是临时接入方案，后续如果要换正式背景或做不同房间背景池，可以继续沿用这个入口，只替换贴图资源或把背景选择逻辑外提即可。
