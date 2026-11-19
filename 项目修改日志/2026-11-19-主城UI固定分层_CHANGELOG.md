# 主城 UI 固定分层与窗口排序

任务编号 `MAIN-UI-LAYERS-20261119`。分支 `codex/feature-development`。开发期间分支整合了其他任务，最终代码差异以 `76bf2cf` 为基线核对。

## 文件清单

- 修改 `LYF_MMO/Assets/LYF_Game/Prefabs/UIPrefabs/UIRoot.prefab`。在 `MainView` 下依次新增 `NormalLayer`、`ChatLayer`、`FunctionLayer`、`TipsLayer` 四个全拉伸容器，并绑定三个运行时用到的层引用。迁移 28 个原节点，保留 `fileID`、锚点、激活状态和各层内原相对顺序。技能信息窗和背包拖拽图标改为继承画布顺序，拖拽图标排在背包内部最后。
- 修改 `LYF_MMO/Assets/LYF_Game/Scripts/UI/Main/MainView.cs`。初始化时按预制体给功能层和提示层直接子窗口绑定父节点，将 `YimengChat` 模块实例化到 `ChatLayer`。
- 修改 `LYF_MMO/Assets/LYF_Game/Scripts/UI/WindowBase.cs`。新增 `SetPanelParentData` 和 `SetPanelParent`，在窗口显示时应用所属容器及同层绘制顺序。
- 记录入口使用 `Docs/AI/CURRENT_TASK.en.md`、`Docs/AI/CURRENT_TASK.md`、`Docs/AI/TEST_REPORT.en.md` 和 `Docs/AI/TEST_REPORT.md`，生成对应 `Docs/AI/RECORD_INDEX.md` 条目。此次业务变更均落在原文件与预制体内部节点，无新增 Unity 资源或 `.meta`。

## 设计与协作

- RO 参考入口为 `clientproj/Assets/Scripts/Lua/Framework/UIManager/UIManager.lua` 的 `GetGroupContainerTransform` 及 `UI/UIBaseCtrl.lua` 的 `SetPanelParentData` 和 `setPanelParent`。LYF 用 `UIRoot` 预制体表达层级归属，在既有 `WindowBase.Show` 流程中先挂父节点并调用 `SetAsLastSibling()`，再执行显隐。常驻 HUD、聊天、功能和提示的层次由容器顺序决定。
- Codex 原生子 Agent `/root/main_ui_layer_code` 承担 `MAIN-UI-LAYERS-20261119-CODE-1`，`/root/main_ui_layer_prefab` 承担 `MAIN-UI-LAYERS-20261119-PREFAB-1`。两个开发子任务各执行一轮，本地原始会话记录的模型均为 `gpt-6.2-sol`。主 Agent 核对差异、集成验证和 Git 提交范围。
- `/root/main_ui_layer_code` 在同一子会话执行一轮 `MAIN-UI-LAYERS-20261119-DOCS-1`，同步并核对本次两条中文记录。标题、标识符、验收结论和未验证边界一致，历史段落的原始字节得到保留。

## 验证

- 使用 Unity 2023.2.20f1c1 的 Roslyn 和项目生成的 `LYF_Game.rsp` 编译成功。共 50 条警告，均位于本次两份脚本以外。原始输出在 `_temp/2026-11-19-main-ui-layer-code/compile.log`。
- Unity 导入隔离预制体，确认四层顺序、三处序列化引用、十个已登记窗的父容器、技能信息窗与背包拖拽 Canvas 继承，以及 `ShowMainWindow` 对聊天、背包、角色属性和装备详情的开关顺序。背包关闭保留当前兄弟位置，重开后升到功能层前面。
- Play 环境为 `ScreenSpaceCamera` 和 1920×1080。隔离实例的四个同点探针经 `EventSystem.RaycastAll` 返回 `Tips>Function>Chat>HUD`。依次关闭提示、功能、聊天和重开聊天的命中结果为 `Function>Chat>HUD`、`Chat>HUD`、`HUD`、`Chat>HUD`。编辑器 Console 错误数为 0，测试后退出本次 Play。证据在 `_temp/main-ui-layers-20261119-01a1f424/unity-layer-check.json`。
- 隔离测试中通过测试实例跳过了依赖 YooAsset 启动的异步技能槽加载。使用同点探针使四层都命中一个位置。真实主城登录后的整套交互、鼠标拖拽道具、发布包仍待验证。

## 本地用量记录

本轮主会话用量和两个原生子会话的汇总来源为本地 Codex JSONL，暂存前主会话用量统计区间为北京时间 2026-11-19 22:35:35 至 23:10:09。主会话输入 10,302,255 token，其中缓存输入 10,193,664，输出 61,062。子会话、覆盖边界及后续统计见 `_temp/main-ui-layers-20261119-01a1f424/current-task-usage.json`，压缩计数见同目录 `context-compactions.json`。本地日志无账户剩余额度字段。

## Git

- 本次实现提交为 `c9d5d571691c984cbcf6604d6b0a11057bc47a0d`。提交包括三个业务文件、本修改日志、四份协作记录的对应条目及生成后的目录。`MainView.cs` 中本地已有的货币兑换开发改动保留在工作区。
- `git push origin HEAD:refs/heads/codex/feature-development` 成功，远端范围为 `76bf2cf..c9d5d57`。`git ls-remote` 核对了 `refs/heads/codex/feature-development` 的实现提交。推送原始回执在 `_temp/main-ui-layers-20261119-01a1f424/git-feature-push.txt`。
