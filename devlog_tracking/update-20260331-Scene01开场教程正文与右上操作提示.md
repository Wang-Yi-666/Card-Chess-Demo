# update-20260331 Scene01 开场教程正文与右上操作提示

## 目标
- 开场显示底部正文（Gal/Undertale 风格）并支持按 E 推进。
- 正文结束后自动消失。
- 操作引导文本放在右上角显示。
- Enemy 不再使用额外贴图，改用 MainPlayer 贴图。

## 本次改动

### 1) 新增 Scene01 教程控制脚本
- 文件：`Scripts/Map/UI/Scene01TutorialController.cs`
- 功能：
  - 进入场景后自动弹出底部正文。
  - 按 `E` 推进正文行（当前一行）。
  - 正文结束后底部面板滑出隐藏。
  - 显示右上角引导：`使用wasd进行移动`。
  - 正文期间禁用玩家移动与交互输入，结束后恢复。

### 2) Scene01 场景接线
- 文件：`Scene/Scene01.tscn`
- 改动：
  - 挂载 `Scene01TutorialController` 到根节点。
  - 底部正文文本替换为：
    - `你终于醒了，虽然不知道这是哪里，但是准备四处走走。按e继续......`
  - 新增右上角 `GuideLabel`（操作提示）。
  - 实例化 `TutorialEnemy` 并放置在玩家右侧，形成“走一段路再遇敌”的流程。

### 3) TutorialEnemy 贴图替换
- 文件：`Scene/InteractableItem/TutorialEnemy.tscn`
- 改动：
  - 敌人贴图改为 `MainPlayer` 的 `Idle_Down`。
  - 使用 `AtlasTexture` 只取第一帧区域（48x64），避免整条序列图拉伸显示。

## 验证
- 关键文件检查：无错误。
- 项目构建：成功（仅现有 nullable 警告）。

## 当前可体验流程
1. 进入 Scene01。
2. 底部出现正文，按 E 继续。
3. 正文消失，右上角显示 `使用wasd进行移动`。
4. 向右移动一段距离遇到敌人。
5. 靠近敌人按 E，进入战斗场景。
