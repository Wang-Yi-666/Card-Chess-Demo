# Step 145 - 清理 GruntDebugRoomA 过期编辑器缓存以修复 MarkerLayer 旧路径报错

## 日期

2026-04-05

## 问题

控制台报错中出现：

- `Node not found: "../GruntDebugRoomA/MarkerLayer" (relative to "Battle")`
- 大量 `common_parent is null`

排查结果表明：

- 运行时代码和战斗场景源码中没有残留这个旧路径
- 实际残留来源位于 Godot 编辑器缓存：
  - `.godot/editor/GruntDebugRoomA.tscn-editstate-...cfg`

这类 `editstate / folding` 文件会记录编辑器上一次打开场景时的：

- 选中节点
- 折叠状态
- 视图状态

当房间场景被改为运行时挂在 `Battle/RoomContainer` 下后，这种旧缓存里的节点路径就可能失效，并触发编辑器层面的路径与父节点报错。

## 本次改动

删除文件：

- `.godot/editor/GruntDebugRoomA.tscn-editstate-51deb0edf48c16c6271c6ea4e8251053.cfg`
- `.godot/editor/GruntDebugRoomA.tscn-folding-51deb0edf48c16c6271c6ea4e8251053.cfg`

## 结果

- Godot 下次打开相关场景时会基于当前实际场景树重新生成缓存
- 旧的 `../GruntDebugRoomA/MarkerLayer` 路径不会再继续从缓存中被恢复

## 结论

这次报错来源不是战斗逻辑代码，也不是 HUD 改动本身，而是编辑器缓存残留。
