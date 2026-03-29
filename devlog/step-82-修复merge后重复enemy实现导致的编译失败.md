# Step 82 - 修复 merge 后重复 Enemy 实现导致的编译失败

## 日期

2026-03-29

## 目标

修复 merge 后地图侧 `Enemy` 脚本出现两套实现并同时参与编译，导致主链无法通过构建的问题。

## 本次改动

### 1. 定位重复实现来源

发现同时存在：

- `Scripts/Character/Enemy.cs`
- `Scripts/Map/Interaction/Enemy.cs`

而项目当前地图域已经以 `Scripts/Map/Interaction/Enemy.cs` 为主。

`Scene/Enemy.tscn` 仍然引用旧脚本，导致：

- 两套 `Enemy` 实现同时进入编译
- 出现大量字段、方法和 Godot 生成属性重复定义错误

### 2. 统一到地图域脚本

更新：

- `Scene/Enemy.tscn`

主要调整：

- 把脚本引用切换到 `res://Scripts/Map/Interaction/Enemy.cs`
- 把战斗场景路径修正为 `res://Scene/Battle/Battle.tscn`
- 同步写入当前地图域脚本需要的导出字段

### 3. 移除旧的重复实现

删除：

- `Scripts/Character/Enemy.cs`
- `Scripts/Character/Enemy.cs.uid`

这样项目中只保留一份当前有效的 `Enemy` 地图交互实现。

## 验证

执行：

- `dotnet build`

结果：

- `0 error`

仍保留大量历史 nullable warnings，但不再阻塞编译。

## 结果

这次 merge 后最直接的对接阻塞已经排除：

- 地图层 `Enemy` 不再有双实现冲突
- 主链重新恢复可编译状态
- 本次修复没有修改 battle / map 共享接口边界
