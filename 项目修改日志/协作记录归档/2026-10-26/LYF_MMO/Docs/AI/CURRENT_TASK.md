# 当前任务

## 2026-10-25 任务 UI 对齐与间距修复（页签间距 / 重合 / 任务名偏右下角）

- 状态：静态修复完成并经 Unity 导入验证；运行时效果需退出重进 play 模式确认（未截图）。
- 需求（用户原话）：「还是过大 而且打标签 和任务标签有些重合 而且任务标签的任务名 有点太过在右下角」。
- 根因③（任务名偏右下角）：上午 Text→TMP 迁移的对齐映射表错误——MiddleCenter(4) 被映射成 BottomRight、MiddleLeft(3) 被映射成 BottomLeft。本版本 TMP 3.x 对齐分 `m_HorizontalAlignment`(1/2/4) + `m_VerticalAlignment`(256/512/1024) 两字段存，遗留 `m_textAlignment:65535` 为派生值；按原版 UGUI `m_FontData.m_Alignment` 逐一还原 13 处（Task.prefab）。
- 根因①（间距过大）：root-space 实测（`GetWorldCorners`+`InverseTransformPoint`，处理 UIRoot Canvas 0.11 缩放）间距 44.13px；链路 = Toggroup 居中锚定损失 21.99px + 实例根 `m_AnchoredPosition.y=-9` + VLG `padding.top=21`；公式「页签顶 = 204.28 + 实例根aPos.y」实测验证。实例根 aPos.y 改 −9 → 19，预期间距 16.13px。
- 根因②（重合）：上轮「居中修复」误把 ON/Off 容器 pivot(0.5,0.5) 原点当左下角，文字被甩到面板左沿外压住第一行任务条目；页签文字矩形还原 RO 原版拉伸（ON aPos(1.08,2.0)/sd(−8.55,−12.0)，Off aPos(2.0,0.49)/sd(−6.7,−11.0)），居中靠对齐字段而非手工偏移。
- 改动：`Task.prefab` 13 对齐 + 6 页签文字矩形 + 实例根 aPos.y；`Toggle_List.prefab` 4、`QuickTaskPanel.prefab` 16、`Btn_Big_01.prefab` 1 全部 BottomRight→MiddleCenter；脚本 `_temp/fix_task_ui_layout.py`（幂等），实例修改无对齐覆盖由 `_temp/map_instance_mods.py` 论证。
- 验证：Unity MCP 导入 4 预制体成功；读回 Task.prefab 16 Center + 14 应保持（12 TopLeft+2 Left）；Toggle_List 4/4、QuickTaskPanel 16/16、Btn_Big_01 1/1；Task.prefab 文档数 432=432。
- 备份：`_temp/prefab_backup_20261025/layout_align_fix_20261025/`。
- 遗留：页签 ON/Off 容器（185×83/181×78）比按钮大、向下探出约 9px，与相邻任务行重叠 6.9~9.4px——RO 原版设计未动，若仍感重合可再缩容器。
- 详细日志：`项目修改日志/2026-10-25-任务UI对齐与间距修复_CHANGELOG.md`。

## 2026-10-25 全项目字体统一为 Common SDF

- 状态：TMP 字体已 100% 统一（388/388），编译与实机未验证（MCP 桥未启动）。
- 需求：检查项目字体全部改为 `Common SDF`，改前记录字体大小/居中等属性。
- 改动前属性记录：`_temp/font_properties_before_common_sdf.txt`（821 行，156 个组件的字号/最小最大/自动适配/对齐/颜色/行距/字距/换行/溢出/富文本/射线）。
- 替换范围：18 个文件 315 处——`m_fontAsset` ×156（只换 guid）、`m_sharedMaterial` ×156（fileID+guid 成对）、`m_GlobalFontAsset` ×2（BuyDialog/UIRoot 的 TMP 全局设置）、`objectReference` ×1（UIRoot 材质引用）；另改 `TMP Settings.asset` 的 `m_defaultFontAsset`。原字体：FeiHuaSongTi-2 ×152、LiberationSans ×4（技能 CD/快捷键）。
- 验证：全项目 TMP 388 个组件 100% Common SDF + 材质配对正确；18 文件 YAML 结构对比备份零丢失（18/18）；UI 文件旧 guid 零残留。
- 未动：`LiberationSans SDF - Fallback.asset` 内部 fallback 引用（TMP 包机制）；497 个旧 UGUI Text（31 文件，字体为 Common.TTF——Common SDF 的源字体，同源同字形），要迁需先做组件迁移，待用户决策。
- 备份：`_temp/prefab_backup_20261025/font_unify/`（18 文件 + TMP_Settings.asset.bak）。

## 2026-10-25 任务 UI 文本组件迁移 TMP（Text → TextMeshPro - Text (UI)）

- 状态：预制体与代码字段已完成，编译与实机显示未验证（Unity MCP 桥未启动）。
- 需求：任务 UI 旧版 UGUI `Text` 全部换成 TMP；替换前记录字体大小与对齐；经用户确认后同步代码字段类型、并一并迁移 `QuickTaskPanel`。
- 预制体（共 46 个 Text → TMP + 4 个 stripped 引用）：`Task.prefab` 24+4、`Toggle_List.prefab` 4、`Btn_Big_01.prefab` 1、`Panel_Common_Medium.prefab` 1、`QuickTaskPanel.prefab` 16。
- 方案：纯 YAML 操作，保留组件 fileID 只换脚本 GUID 与字段（跨组件引用不断）；字体统一 `Common SDF.asset`（`Assets/artres/Resources/UI/Font/`，guid `8d1c33eb43cd7e24d9faf1a7087d2dd2`）——曾按用户要求短暂换成 `FeiHuaSongTi-2 SDF`（guid `3993037bb5ace8e4d9f2555109577b24`），因观感与标题显示问题已回退；属性按既有映射表转换（对齐/字号/自动适配/换行/溢出/富文本/颜色 BGRA）。
- 文本居中修复（2026-10-25 上午晚些）：页签 6 个 Text 改为相对按钮 `TogL` 精确居中（ON/Off 大容器偏右上是根因）；`TaskName` 与快捷面板 10 个流式文本节点（rect 宽/高为 0）统一改 anchor(0.5,0.5) + MiddleCenter。
- 代码：`TaskWindow` / `TaskTagItemWidget` / `TaskChildItemWidget` / `TaskParentItemWidget` / `QuickTaskItemWidget` 共 18 处 `Text` 字段与签名改 `TMP_Text`，补 `using TMPro;`，逻辑零改动（与 Chat/Knapsack/Skill 等窗口惯例一致）。
- 宿主安全：`UIRoot`/`Task`/`Friends`/`Theme_NewTask` 中上述预制体实例对旧 Text 的属性覆盖均为 0，迁移不破坏嵌套实例。
- 属性备份：`_temp/task_text_properties.txt`、`_temp/quicktask_text_properties.txt`；预制体备份 `_temp/prefab_backup_20261025/`。
- 边界：`UIRoot.prefab` 自有 180 个旧 Text、`BuyItemWidget` 3 个旧 Text 不在任务范围未动；Common SDF 与旧字体渲染宽度差异未逐项测量。
- 详细日志：`项目修改日志/2026-10-25-任务UI文本组件迁移TMP_CHANGELOG.md`。

## 2026-10-24 任务页签改为 RO 竖排形态

- 状态：已完成（含实机自检）。
- 需求：用户质疑上一轮自造的「顶部三个小胶囊」页签 RO 是否有，核查后确认 **RO 有这三页签但形态不同**，按用户选择改为 RO 原版竖排大按钮。
- RO 依据：三个页签由 `TaskCtrl.lua:118 CreateTaskTag()` 遍历 `ETaskTag` 生成，文案 `TASK_TAG_01/02/03` = 任务/玩法/奇遇；位置在左侧 `TaskSelectPart`（199.9×471.57，x=-359）；模板为 `Toggle_List.prefab` 的 `TaskParentItem/TogL`（ON/Off 双态）；图标 `UI_Task_Icon_Left_0{2*tag}` / `_0{2*tag-1}`。
- 预制体：删除 `TaskSelectPart/TabBar`；在列表 Content 顶部克隆出常驻 `Tag_Adventure` / `Tag_Story` / `Tag_Other`（模板取自 `Templent/TaskParentItem`），挂 `TaskTagItemWidget` 并写入标题与图标；`_pageTabs` 重新绑定。
- 代码：新增 `TaskTagItemWidget.cs`（标题 / ON-Off 选中态 / 点击派发）；`TaskWindow` 的 `_pageTabs` 改为该控件数组，`RebuildTaskList()` 不再生成分组父条目，改为把任务条目插到选中页签正下方（`SetSiblingIndex`）；删掉 `_parentTemplate` / `_parentItem` / `BuildChildSnapshot`。
- 自检结果：任务页 4 条、玩法页 3 条、奇遇页 2 条，条目紧跟各自页签；点快捷面板后 `TaskMgr.currentPage=Adventure` 而任务窗口仍停奇遇页（解耦成立）；`TaskWindow` 无 Missing Script、Console 0 新增错误。
- 详细日志：`项目修改日志/2026-10-24-任务页签改RO竖排形态_CHANGELOG.md`。
- 边界：页签保留 RO 原版箭头但不实现折叠；`Templent/TaskParentItem` 作为模板保留在预制体内。

## 2026-10-24 日常任务窗口接入 + 任务窗口页签自持

- 状态：已完成（含实机自检）。
- 需求：① 点左上小面板页签不许改变任务大窗口列表；② 「奇遇」在任务 UI 里做成页签；③ 日常界面接入项目，做成独立窗口，不加其他功能。
- 任务窗口页签自持：`TaskWindow` 新增本地 `_currentPage`，列表/详情/分组标题全按本地页签；`OnPageTabValueChanged` 不写 `TaskMgr`、不通知快捷面板。`TaskCtrl` 去掉窗口页签转发。
- 日常窗口：新建 `DailyTaskWindow.cs`（只绑 `CloseBtn` + 关窗）；`WindowType` 增 `DailyTaskWindow`；`MainView` 增 `_dailyTaskWindow` / `_dailyTaskButton` 并注册进 `windowDic`；`MainCtrl` 增日常入口回调。
- 预制体：`DailyTask.prefab` **删掉根与 `Panel_DailyList` 两层嵌套 Canvas**（嵌套 Canvas 导致渲染批次与射线隔离，`GraphicRaycaster` 实测 0 命中），改为普通 UI 并入主画布；根 1136×640 @1.4（对齐 `SettingPanel` 惯例）；挂 `DailyTaskWindow` 并绑 `CloseBtn`。
- 主界面：`UIRoot.prefab` 的 `MainView` 下新增 `DailyTaskWindow` 实例（默认 inactive、最后一层）；`FunctionMenuContent` 新增 `DailyTaskButton`（`Label`=「日常」，x=416），容器宽 344→424。
- 自检结果：三页签列表 4/3/2；点快捷面板后 `TaskMgr.currentPage=Story` 而任务窗口仍停奇遇页（解耦成立）；日常窗口可开、`CloseBtn` 射线最上层命中并可关窗；Console 0 错误。
- 边界：日常面板**未接数据与逻辑**（RO 的活动图标/列表/奖励由 `DailyTaskMgr` 运行时生成），`Img_DailyTaskBg` 底图在 RO 原工程也已丢失，未造图。
- 详细日志：`项目修改日志/2026-10-24-日常任务窗口接入_CHANGELOG.md`。

## 2026-10-24 任务页签对齐 RO + 修复窗口点击无响应

- 状态：已完成（含实机验证）。
- 需求：任务页签不许用「主线/日常/活动」，必须照 RO 的 `ETaskTag`（任务/玩法/奇遇）；修复任务窗口与快捷面板页签点击无响应。
- 页签语义：`TaskPageFilter` 成员改为 `Adventure=1 任务 / Story=2 玩法 / Other=3 奇遇`，依据 `RO Lua/ModuleMgr/TaskMgr.lua:153`；归属由任务配置的 Theme 决定。
- 点击无响应根因：`TaskWindow` / `QuickTaskPanel` 根节点是嵌套 Canvas 且缺 `GraphicRaycaster`，鼠标射线进不来；嵌套 Canvas 的 Graphic 只注册到自身 Canvas，父 Canvas 射线无效。已在 Task / QuickTaskPanel / Bag 三个预制体补齐射线器（共 5 处）。
- 列表过滤：`TaskMgr.GetTrackedTasks()` 按当前页签过滤并删掉 `NeedTrack` 裁剪，对齐 `ShowTaskFilter(false)`（TaskMgr.lua:1576）。
- 预制体：Task.prefab 页签文案与节点名（Tab_Adventure/Tab_Story/Tab_Other）、QuickTaskPanel.prefab 文案复核（原本已正确）。
- 现场事实：QuickTaskPanel.prefab 的 TypeGrp 上没有 ToggleGroup，互斥靠代码手动同步 ON/Off。
- 详细日志：`项目修改日志/2026-10-24_任务页签对齐RO与点击无响应修复.md`。
- 遗留：全项目另有 28 个预制体 Canvas 缺射线器，未批量补（防误挡场景点击）；背包交互未逐项回归。

- 状态：已完成。
- 需求：说明 `MTimerMgr` 从哪里更新时间，并在何处执行到期回调。
- 本次新增文档：`项目分析/MTimerMgr更新时间与回调链路分析.md`。
- 未修改业务代码、配置、资源或场景。
- 下一步：若需追踪某个具体技能回调，可从其 `SetTimer` 注册位置和 `TimerEventHandler` 委托继续定位。

## 2026-10-21 SkillRootMotionCollector 解析

- 状态：完成静态解析。
- 需求：解释当前脚本的用途及其与技能、模型、移动组件的关系。
- 新增：`项目分析/ro项目解析/SkillRootMotionCollector作用与调用链.md`、该子目录 `README.md` 及 `项目修改日志/2026-10-21_SkillRootMotionCollector解析.md`。
- 修改：本记录及 `Docs/AI/TEST_REPORT.md`，追加本次协作和验收信息。
- 验收：确认原生 Animator 采集、技能窗口筛选、移动组件消费与碰撞提交链路；业务代码无改动。
- 未解决事项：未运行具体技能验证动画位移与碰撞表现；本次源码用途说明已完成。
- 交接：如继续排查某个技能的实际移动距离，应结合该剪辑根运动和 RootMotionSegments 配置。
