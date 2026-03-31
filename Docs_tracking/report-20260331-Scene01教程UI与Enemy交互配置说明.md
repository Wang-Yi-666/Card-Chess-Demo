# report-20260331 Scene01 教程 UI 与 Enemy 交互配置说明

## 需求落地状态
- [x] 底部正文对话出现（开场）
- [x] 按 E 推进正文
- [x] 正文结束后自动消失
- [x] 操作引导放右上角
- [x] Enemy 使用 MainPlayer 素材，不再依赖额外贴图
- [x] 在 Scene01 内完成基础移动 -> 遇敌 -> 交互进战斗链路

## 关键文件
- `Scripts/Map/UI/Scene01TutorialController.cs`
- `Scene/Scene01.tscn`
- `Scene/InteractableItem/TutorialEnemy.tscn`

## 文本配置（当前）
### 底部正文
- `你终于醒了，虽然不知道这是哪里，但是准备四处走走。按e继续......`

### 右上角操作提示
- `使用wasd进行移动`

## 说明
- 当前正文仅一条，结构已支持后续追加多条。
- 后续要新增“战斗交互引导”时，建议继续使用右上角提示标签，保持风格一致。
