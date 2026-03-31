# update-20260331 Player 四向走格移动实现

## 目标
- 删除斜向移动。
- 实现每次移动一个 tile 的走格效果（类似老版宝可梦/塞尔达）。

## 修改文件
- `Scripts/Map/Actors/Player.cs`

## 关键改动
1. 新增走格参数
- `GridTileSize`（默认 16）
- `SnapToGridOnReady`（默认 true）

2. 移动逻辑改造
- 从 `Input.GetVector` 连续移动改为“卡方向输入 + 分步移动”。
- 输入读取只输出四向（上下左右），不再允许斜向。
- 每次按住方向键，角色按格推进：一步到下一格，再进入下一步。

3. 碰撞处理
- 起步前用 `TestMove` 预检整格位移是否可达。
- 移动中用 `MoveAndCollide`，遇阻即停并吸附回网格。

4. 网格吸附
- `Ready` 时可选对齐到网格（避免出生点偏半格）。
- 每一步终点强制对齐到整格坐标。

5. 动画方向
- 保留原动画系统，方向解析改为四向。
- 左右移动分别映射到 `left_down/right_down`，上下映射 `up/down`。

## 验证
- `get_errors`：`Scripts/Map/Actors/Player.cs` 无错误。
- `dotnet build newproject.csproj`：成功（仅历史 nullable 警告）。

## 结果
- 角色已实现四向走格，不再斜向移动。
- 单步移动长度由 `GridTileSize` 控制，默认 16 像素。
