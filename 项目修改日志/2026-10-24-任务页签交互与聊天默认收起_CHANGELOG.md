# 任务窗口页签交互 + 聊天默认收起 + 任务图标补齐

日期：2026-10-24
分支：`codex/task-system-ui`

## 一、这次做了什么

### 1. 任务主窗口（`Task.prefab`）新增页签栏

任务窗口原先**完全没有页签**，页签只存在于左上角快捷面板。本次在左侧列表区顶部新增一排页签：

| 节点 | 路径 | 尺寸 | 说明 |
|---|---|---|---|
| `TabBar` | `TaskSelectPart/TabBar` | 200×44 | anchor(0.5,1)/pivot(0.5,1)，pos(0,0) |
| `Tab_MainLine` | `TaskSelectPart/TabBar/Tab_MainLine` | 62×38 | pos(34,0)，默认选中，标签「主线」 |
| `Tab_Daily` | `TaskSelectPart/TabBar/Tab_Daily` | 62×38 | pos(100,0)，标签「日常」 |
| `Tab_Activity` | `TaskSelectPart/TabBar/Tab_Activity` | 62×38 | pos(166,0)，标签「活动」 |

每个页签是标准 uGUI `Toggle`：`Background`（`UI_Common_Bg_8Fillet_01`，灰色未选中）/ `Checkmark`（同图，白色选中）/ `Label`（TMP，`Common SDF`，20 号，居中）。

为给页签让位，列表容器同步收缩：

- `TaskSelectPart/Toggroup`：size 471.60 → **427.60**，pos.y 0.02 → **-21.98**
- `TaskSelectPart/Toggroup/Obj_Toggle`：size.y 471.60 → **427.60**

`Toggroup` 的 anchor 是 (0.5,0.5)，高度减 44 后中心自然下移 22，顶部正好让出 44 给页签，因此 `pos.y` 应为 -21.98 而非 -44。

### 2. 页签逻辑（`TaskWindow.cs`）

新增：

- 字段 `Toggle[] _pageTabs`（已在预制体上绑定 `Tab_MainLine` / `Tab_Daily` / `Tab_Activity`）
- 事件 `event Action<TaskPageFilter> PageChanged`
- `RegisterPageTabs()`：只在被选中时触发切换，取消选中不触发
- `SyncPageTabs()`：把按钮选中态镜像成 `TaskMgr.GetCurrentPage()`，用 `SetIsOnWithoutNotify` + `_syncingTabs` 标志避免回调自激
- `IndexToPage(int)`：索引 0/1/2 → `MainLine` / `Daily` / `Activity`

`InitWindow()` 增加 `RegisterPageTabs()`，`ReFreshUI()` 增加 `SyncPageTabs()`，保证从快捷面板切换页签后打开任务窗口，页签选中态一致。

### 3. 控制器接线（`TaskCtrl.cs`）

- 订阅/退订 `_taskWindow.PageChanged += OnPageChanged`，复用已有的 `TaskMgr.SetCurrentPage()` → `TaskDataChanged` → `RefreshAll()` 链路，主窗口与快捷面板共用同一份当前页签。
- 编辑器下调用 `TaskDevSampleData.InjectIfEmpty()`。

### 4. 修一个重建残留问题

`DestroyRow()` 原先直接 `Object.Destroy`，运行时销毁延迟到帧末。同一帧内连续切换页签时，旧行仍参与布局，会出现跨页签内容叠加。改为**先 `SetParent(null, false)` 再销毁**，旧行立即退出列表布局。

### 5. 快捷面板页签文案统一

`QuickTaskPanel.prefab` 的页签文案原为「任务 / 玩法 / 奇遇」，与数据层 `TaskPageFilter`（MainLine/Daily/Activity）语义不一致，统一改为「**主线 / 日常 / 活动**」，与任务窗口完全对齐。

### 6. 聊天窗改为进游戏默认收起

上一轮把聊天窗改成默认展开并覆盖任务区，实际观感不佳且遮挡视野。桌面端聊天应按需展开，因此改回：

- `ChatWindowCollapseController.startCollapsed`：0 → **1**
- `ChatWindow/BG` `m_IsActive`：1 → **0**
- `ChatWindow/QuickChat` `m_IsActive`：0 → **1**

进主城后只显示快捷聊天条，点它展开完整面板，点面板右侧箭头收回。

> 注意：本轮之前"聊天打不开"的根因是手改 YAML 后 Unity 未重新导入，运行时用的还是旧内存副本。以后手改预制体文本必须 `AssetDatabase.ImportAsset(..., ForceUpdate | ForceSynchronousImport)` 再进播放模式。

### 7. 补齐功能菜单「任务」按钮缺失的图标

功能菜单五个入口里，`TaskButton` 只有 `Label`，没有 `Icon`；其余四个（背包/角色/技能/设置）都有。

- 图标来源：**项目内** `Assets/artres/Resources/UI/Atlas/CommonIcon.png` 的 sprite `UI_Commonicon_Icon_top_Task`（18×22），与 `Icon_top_Bag` / `Icon_top_Role` 同系列，**无需从 RO 项目取图**。
- 新增节点 `FunctionMenuContent/TaskButton/Icon`，参数对齐兄弟按钮：anchor(0,1)/pivot(0.5,0.5)，pos(40,-29)，size(58,44)，`preserveAspect=true`，`raycastTarget=false`，sibling index 0（在 Label 之前）。

## 二、测试记录（实机，已登录）

登录链路：`Scene_Boot` → `Scene_Login` → 账号 `weq` / 密码 `1111` → 选服「1区 五域大陆(新服)」→ `Scene_CreateRole`「进入游戏」→ `Scene_MainCity`。全程通过。

| 用例 | 结果 | 证据 |
|---|---|---|
| 进主城聊天默认收起 | 通过 | `BG.activeSelf=False`、`QuickChat.activeSelf=True` |
| 打开任务窗口 | 通过 | `TaskWindow.activeInHierarchy=True` |
| 页签「主线」→「日常」 | 通过 | `GetCurrentPage()=Daily`；列表重建为 每日签到 / 日常悬赏 / 公会捐献 |
| 页签「日常」→「活动」 | 通过 | 列表重建为 限时活动：世界BOSS / 钓鱼大赛 |
| 「活动」→「主线」回切 | 通过 | 列表回到 初入主城 / 剿灭城外野狼 / 收集药草 / 前往北境 |
| 同帧连续切三次不残留 | 通过 | `Content` 子节点 6 = 模板 1 + 父条目 1 + 子条目 4，无跨页签残留 |
| 列表滚动 | 通过 | 压测 20 条后 contentH=1178.1 / viewportH=427.6 / 可滚 750.5；置底 normalized=0.000 contentY=750.5，置顶 normalized=1.000 contentY=0.0 |
| 详情区跟随选中 | 通过 | 标题栏显示任务名、橙色剩余时间、任务目标 / 任务描述正文均随页签切换更新 |
| 右上角 X 关窗 | 通过 | `Btn_Close` → `TaskWindow.OnCloseButtonClick`，窗口 active True→False |
| 任务按钮图标 | 通过 | `TaskButton/Icon.sprite = UI_Commonicon_Icon_top_Task.png` |
| 快捷面板文案 | 通过 | `Type_1/2/3` = 主线 / 日常 / 活动 |

**未测量**：

- 标题栏拖拽只确认 `WindowTitleDragHandle` 组件在位，未做真实鼠标拖拽动作。
- 奖励区内容：`TaskViewData` 没有奖励明细字段，`TaskWindow` 也只对 `_rewardItem` 做显隐，从不实例化奖励项，因此「完成奖励」区域始终为空。这是既有的功能缺口，不是本次改动引入。
- 未做 1080p 以外的分辨率适配验证。

## 三、示例数据的边界

任务系统目前**没有服务端数据源**：`TaskMgr.SetTasks()` 全项目没有任何调用点，也没有任务配置表。为了让页签与列表能被实际验证，新增 `TaskDevSampleData.cs`：

- 整个文件用 `#if UNITY_EDITOR` 包裹，**打包产物不包含**；
- 只在三个页签都为空时注入一次（共 9 条：主线 4 / 日常 3 / 活动 2），已有真实数据时绝不覆盖；
- 注入时打印 `Debug.LogWarning("[TaskDevSampleData] ...")`，明确这是联调数据。

正式接入服务端时，删掉 `TaskCtrl` 里那 3 行 `#if UNITY_EDITOR` 调用即可，界面逻辑无需改动。

## 四、文件清单

修改：

- `LYF_MMO/Assets/artres/Resources/UI/Prefabs/Task.prefab`（+1188 行，页签栏）
- `LYF_MMO/Assets/GM_Game/Scripts/UI/Main/Window/TaskWindow.cs`（+102 行）
- `LYF_MMO/Assets/GM_Game/Scripts/controller/UIController/TaskCtrl.cs`（+6 行）
- `LYF_MMO/Assets/artres/Resources/UI/Prefabs/QuickTaskPanel.prefab`（页签文案）
- `LYF_MMO/Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab`（聊天折叠开关 + TaskButton 图标）

新增：

- `LYF_MMO/Assets/GM_Game/Scripts/Manger/TaskDevSampleData.cs`

## 五、过程说明

本轮按用户要求以子代理方式开发：`task-prefab-builder`（general-purpose）负责 `Task.prefab` 的页签节点搭建与自检，主会话负责脚本逻辑、引用绑定、图标补齐与实机验证。子代理在搭建过程中主动纠正了主会话给出的两处规格偏差（`Toggroup` 的 y 应为 -21.98 而非 -44；`TabBar` 兄弟索引应为 2 以保证后渲染），实测结果正确。
