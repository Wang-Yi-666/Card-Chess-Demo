# step-150 从回滚中恢复战斗 HUD 正式化结构

时间：2026-04-06

背景：
- 项目近期发生过一次回滚。
- 回滚后，战斗 HUD 退回到了较早版本：
  - 右侧重新变回文字按钮
  - 牌堆重新拆成多个独立入口
  - 牌堆弹窗退回成单列表结构
  - 关闭按钮与正式化输入遮罩逻辑也一起丢失

本次目标：
- 按 2026-04-05 已完成的 `step-144` 到 `step-149` 记录，把战斗 HUD 正式化内容整体恢复。

本次实际恢复内容：

1. 恢复战斗按钮 sprite 化
- 文件：
  - `Scene/Battle/UI/BattleHud.tscn`
  - `Scripts/Battle/UI/BattleHudController.cs`
- 内容：
  - 右侧按钮恢复为两列布局
  - `牌堆 / 日志 / 攻击 / 防御 / 冥想 / 逃跑` 恢复为 sprite 按钮
  - 使用程序生成的像素底板，不再依赖旧版纯文字按钮

2. 恢复单牌堆入口
- 文件：
  - `Scene/Battle/UI/BattleHud.tscn`
  - `Scripts/Battle/UI/BattleHudController.cs`
- 内容：
  - 删除旧版 `DrawPileButton / DiscardPileButton / ExhaustPileButton`
  - 恢复为单一 `PileButton`
  - 点击后打开中央牌堆弹窗

3. 恢复牌堆中央页签弹窗
- 文件：
  - `Scene/Battle/UI/BattleHud.tscn`
  - `Scripts/Battle/UI/BattleHudController.cs`
- 内容：
  - `PilePopup` 恢复为居中弹窗
  - 内部恢复 `TabContainer`
  - 恢复三个页签：
    - `抽牌堆`
    - `弃牌堆`
    - `消耗堆`
  - 每个页签都恢复为完整卡面预览网格，而不是旧版单列表

4. 恢复牌堆交互约束
- 文件：
  - `Scripts/Battle/UI/BattleHudController.cs`
- 内容：
  - 恢复牌堆弹窗打开时的底层战斗按钮/手牌交互屏蔽
  - 恢复点击弹窗外关闭
  - 恢复滚轮统一接管
  - 恢复底部 overscroll 回弹逻辑

5. 恢复像素关闭按钮
- 文件：
  - `Scripts/Battle/UI/BattleHudController.cs`
  - `Scene/Battle/UI/BattleHud.tscn`
- 内容：
  - 牌堆弹窗和战斗日志弹窗的关闭按钮恢复为像素贴图按钮
  - 不再使用字体渲染的 `X`

说明：
- 这次不是在旧版基础上零碎修补，而是按回滚前最终结构整体重建，以避免旧逻辑和乱码残留继续混在一起。
- 地图层与战斗层接口未改，只恢复战斗 HUD 本侧的表现层与交互层。

后续验证重点：
1. 右侧是否重新显示为 sprite 按钮。
2. 是否只有一个牌堆入口按钮。
3. 牌堆弹窗是否恢复为中央页签结构。
4. 牌堆打开后，底层按钮和手牌是否不再穿透。
5. 关闭按钮是否恢复为像素样式。
