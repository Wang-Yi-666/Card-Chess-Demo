# Step 05 - Build Validation

## Date

2026-03-20

## Goal

确认这次新增的战斗框架能进入当前 C# 工程的正常编译链路。

## Validation

- 先发现项目内存在一份 `Card-Chess-Demo-main` 副本，被 MSBuild 默认一起编译，导致旧脚本重名冲突。
- 在 `newproject.csproj` 中排除了 `Card-Chess-Demo-main/**/*.cs`。
- 开启了 `Nullable`，让后续空引用问题可以被标准化识别。
- 修正了 `RoomLayoutDefinition` 中 Godot 不支持导出的字段类型。
- 重新执行 `dotnet build`，结果为：
  - `0 errors`
  - `7 warnings`

## Remaining Warnings

现存 warning 全都来自旧脚本，不是本次新增的战斗框架：

- `Scripts/Player.cs`
- `Scripts/Chest.cs`
- `Scripts/SceneDoor.cs`

这些 warning 主要是旧代码的空引用初始化问题，后续可以单独做一轮整理。
