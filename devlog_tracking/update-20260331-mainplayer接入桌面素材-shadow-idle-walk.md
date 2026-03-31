# update-20260331 mainplayer 接入桌面素材（shadow / idle / walk）

## 目标
将桌面目录 `c:\Users\11370\Desktop\mainplayer` 中的主角素材接入 Godot 项目，并在 `MainPlayer` 场景生效。

## 素材结构确认
- `Idle/*.png`：横向条带动画，尺寸 384x64（逐方向）
- `Walk/*.png`：横向条带动画，尺寸 384x64（逐方向）
- `Shadow.png`：尺寸 48x64

## 已执行变更

### 1) 复制素材到项目
- 复制源：`c:\Users\11370\Desktop\mainplayer`
- 目标目录：`Assets/Character/MainPlayer`

### 2) 玩家动画系统改造（运行时切条带）
- 修改文件：`Scripts/Map/Actors/Player.cs`
- 新增能力：
  - 运行时将条带图切分为动画帧（默认 48x64）
  - 构建 12 组动画：
    - idle: down / up / left_down / left_up / right_down / right_up
    - walk: down / up / left_down / left_up / right_down / right_up
  - 按输入方向自动切换 idle / walk 动画
- 说明：无需手动将条带图拆成单帧文件。

### 3) MainPlayer 场景加入阴影
- 修改文件：`Scene/MainPlayer.tscn`
- 新增 `Shadow` 子节点（Sprite2D），纹理：
  - `res://Assets/Character/MainPlayer/Shadow.png`

## 验证结果
- `dotnet build newproject.csproj`：成功（仅 warnings，无 errors）
- `get_errors`：
  - `Scripts/Map/Actors/Player.cs` 无错误
  - `Scene/MainPlayer.tscn` 无错误

## 备注
- 当前大量 `CS8632` 为 nullable 警告，与本次接入无关。
- 若需要，可后续再加：
  - 动画速度分方向微调
  - 冲刺/受击动画扩展
  - 与武器/手提箱状态联动
