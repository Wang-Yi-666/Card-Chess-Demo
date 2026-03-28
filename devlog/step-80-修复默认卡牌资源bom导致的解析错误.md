# Step 80 - 修复默认卡牌资源 BOM 导致的解析错误

## 日期

2026-03-28

## 目标

修复 `DefaultBattleCardLibrary.tres` 与 `DefaultBattleDeckBuildRules.tres` 因文件头 BOM 导致 Godot 文本资源解析失败的问题。

## 本次改动

### 1. 定位问题

错误表现：

- Godot 加载 `res://Resources/Battle/Cards/DefaultBattleCardLibrary.tres` 时在第 1 行报：
  - `Parse Error: Expected '['`

排查结果：

- 两个 `.tres` 文件开头存在 UTF-8 BOM
- 文件首字节为：
  - `239,187,191,...`

而 Godot 在当前读取路径下把 BOM 视为了非法前缀，导致第一个 `[` 无法正常识别。

### 2. 修复资源编码

处理：

- `Resources/Battle/Cards/DefaultBattleCardLibrary.tres`
- `Resources/Battle/Cards/DefaultBattleDeckBuildRules.tres`

统一重写为：

- UTF-8 无 BOM

修复后文件首字节变为：

- `91,103,100,95,...`

即直接从 `[` 开始。

## 验证

执行：

- `dotnet build`

结果：

- `0 warning`
- `0 error`

## 结果

当前默认卡牌资源已经可以被 Godot 正常解析，battle 主场景不再会因为默认卡牌库资源头部编码问题在 `_Ready()` 阶段报错。
