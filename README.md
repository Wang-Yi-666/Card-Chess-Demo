# Card-Chess-Demo

当前仓库是一个基于 Godot 4.6 + C# 的竞赛原型工程，核心目标是做出一个“卡牌驱动的短闭环战棋战斗”切片。

目前默认主入口是 `Scene/Battle/Battle.tscn`，战斗主链已优先保留并持续推进。

## 目录约定

- `Scripts/Battle`：战斗域脚本
- `Scripts/Map`：地图域脚本
- `Scene/Battle`：战斗场景
- `Scene`：地图与基础原型场景
- `Resources/Battle`：战斗资源
- `Docs`：项目文档
- `devlog`：开发变更记录

## 当前维护约定

- 地图侧新增脚本统一放入 `Scripts/Map/...`
- 战斗侧新增脚本统一放入 `Scripts/Battle/...`
- 文档统一更新到 `Docs`
- 从本次整理开始，后续变更统一以中文记录到 `devlog`

## 参考文档

- `Docs/项目目录结构说明.md`
- `Docs/当前项目进度总览.md`
- `Docs/项目接口文档.md`
- `Docs/战斗对外交互接口方案.md`
- `Docs/战斗机制规则书.md`
- `Docs/卡牌系统与局外构筑README.md`
- `Docs/天赋成长与卡组构筑正式方案.md`
- `Docs/卡组构筑平衡规则方案.md`
- `Docs/设计模板/README.md`
- `Docs/设计示范-标准内容/README.md`
- `Docs/场景脚本文稿-序章与世界观整合版.md`
- `Docs/游戏机制与玩法循环深度拆解.md`
- `Docs/游戏整体策划案-竞赛版.md`
- `Docs/美术需求文稿-竞赛冲刺版.md`
- `Docs/游戏策划拆解分析-竞赛版.html`
- `Docs/项目阶段会议提纲.html`
