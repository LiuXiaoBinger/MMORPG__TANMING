# 任务面板折叠方式与功能菜单重合修正（QUICKTASK-LAYOUT-20261024）

- 日期：2026-10-24
- 分支：`codex/task-system-ui`
- 需求来源：用户指出主界面上功能菜单一行与快捷任务面板控件重合，并要求折叠改为“侧面收缩（横向）”，不是“上下收缩”；折叠按钮图标保留现有的铅笔样式
- 执行者：WorkBuddy 会话（本机 Unity MCP + 文件编辑）
- 实际使用技能：本次未使用技能，全部工作由本机原生工具完成（文件读写、Bash/grep、Unity MCP 只读查询与预制体内容读取）

## 一、问题定位

用 Unity MCP 在当前已加载的 `UIRoot` 上读取画布坐标（画布 1920×1080，原点居中）：

| 控件 | 路径 | X 范围 | Y 范围 |
|---|---|---|---|
| 功能菜单五按钮 | `Canvas/MainView/FunctionMenu/FunctionMenuContent/{KnapsackButton..TaskButton}` | −930 ~ −522 | 382 ~ 466 |
| 页签行 | `Canvas/MainView/QuickTaskPanel/TypeGrp` | −913 ~ −700 | 353 ~ **387** |
| 折叠按钮 | `Canvas/MainView/QuickTaskPanel/Btn_Fold` | −692 ~ −592 | 345 ~ **395** |
| 任务列表 | `Canvas/MainView/QuickTaskPanel/TaskList` | −916 ~ −700 | 183 ~ 353 |

`TypeGrp`（下沿 387）与 `Btn_Fold`（上沿 395）都越过了功能菜单按钮的下沿 382，`Btn_Fold` 还横向压在“设置”（−684~−604）与“任务”（−602~−522）两个按钮下方。这就是用户截图里“任务”文字被蓝色按钮遮住的原因。

折叠方式方面，原实现是 `SetFolded` 里直接 `_taskListRoot.gameObject.SetActive(false)`，即把列表整块隐藏（上下方向消失），与参考项目 `QuickPanelCtrl.lua` 的 `shrink_btn → TweenToLeft / TweenToRight` 横向收拢不一致。

## 二、修改内容

### 2.1 `LYF_MMO/Assets/artres/Resources/UI/Prefabs/QuickTaskPanel.prefab`

| 节点 | 字段 | 修改前 | 修改后 |
|---|---|---|---|
| `TypeGrp` | `m_AnchoredPosition.y` | −170.31 | −200.31 |
| `TaskList` | `m_AnchoredPosition.y` | −272.22 | −302.22 |
| `Btn_Fold`（嵌套 `Btn_Small_02` 实例覆盖） | `m_AnchoredPosition.y` | −170.31 | −200.31 |

三处同步下移 30 像素，保持页签行、任务列表、折叠按钮原有的相对关系不变，仅让整块面板离开功能菜单按钮行。

修改后画布坐标：

| 控件 | Y 范围 | 与功能菜单下沿（382）的间距 |
|---|---|---|
| `TypeGrp` | 323 ~ 357 | 25 |
| `Btn_Fold` | 315 ~ 365 | 17 |
| `TaskList` | 153 ~ 323 | — |

### 2.2 `LYF_MMO/Assets/GM_Game/Scripts/UI/Main/Window/Widget/QuickTaskPanelWidget.cs`

| 变化 | 说明 |
|---|---|
| 新增 `using DG.Tweening;` | 使用项目已有的 DOTween 做横向补间 |
| 新增常量 `FoldOffsetX = -280f` | 收起时的横向位移，负值向屏幕左侧，保证整列落到画布左边界之外 |
| 新增常量 `FoldDuration = 0.22f` | 收起/展开补间时长（秒） |
| 新增字段 `_listBaseAnchoredX` | 记录任务列表展开态的锚定 X，收起与展开都以它为基准，不依赖当前值累加 |
| 新增字段 `_listTween` | 补间句柄，重复点击时先 `Kill` 再起新补间，避免位移叠加 |
| `InitWidget()` | 新增记录 `_listBaseAnchoredX` |
| `SetFolded(bool)` | 由 `SetActive(false)` 改为调用 `MoveTaskListSideways`，页签行与折叠按钮不再隐藏 |
| 新增 `MoveTaskListSideways(bool)` | 用 `DOAnchorPosX` + `Ease.OutQuad` 把任务列表在“原位”与“原位 − 280”之间横向移动 |
| 注释 | `_taskListRoot`、`_foldButton` 的中文说明同步改为横向收起语义 |

折叠按钮的 180° 旋转（`RotateFoldButton`）按用户“保留现有铅笔图标”的要求未改动，行为与原来一致。

## 三、验证

| 项 | 结果 |
|---|---|
| 编译 | Unity 强制刷新 + 请求编译后，Console error 计数 0 |
| 预制体回读 | `TypeGrp (153.92, −200.31)`、`TaskList (152.10, −302.22)`、`Btn_Fold (318.44, −200.31)`，与目标一致 |
| 实例覆盖检查 | `UIRoot.prefab` 中对 `TypeGrp`（`883840342155908034`）、`TaskList`（`6419135032212389032`）、`Btn_Fold`（`6360310713568796913`）的匹配条数为 0，预制体改动可透传到主界面实例 |
| 折叠终值 | 临时实例上执行 `InitWidget → SetFolded(true) → DOTween.CompleteAll()`：`TaskList.anchoredPosition.x` 由 152.10 变 −127.90（画布右边界换算为 −979.55，已在画布左边界 −960 之外），再次 `SetFolded(false)` 回到 152.10 |
| 节点存活 | 收起后 `TaskList.activeSelf` 仍为 true（不再隐藏节点） |
| 清理 | 临时实例使用 `DestroyImmediate` 销毁，未保存任何资源或场景 |

## 四、未验证与边界

- 当前编辑器活动场景已被切到 `Scene_Boot`（不含 `UIRoot`），**未在 `Scene_MainCity` 播放态实际点击折叠按钮观察补间观感**；补间终值用 `DOTween.CompleteAll()` 强制完成验证，非播放态 DOTween 不走 Update。
- 任务列表下移 30 后，`TaskList` 与 `ChatWindow`（x −926~−396，y −322~308）的重叠范围比修改前略增；该重叠在修改前就已存在，不属于本次需求范围，未作处理。
- 折叠按钮图标仍为 `Photograph.png` 图集里的 `UI_Photograph_Icon_Redact`（铅笔/编辑），按用户要求保留。

## 五、文件清单

| 类型 | 文件 | 变化 |
|---|---|---|
| 修改 | `LYF_MMO/Assets/artres/Resources/UI/Prefabs/QuickTaskPanel.prefab` | 三个节点的 `m_AnchoredPosition.y` 下移 30 |
| 修改 | `LYF_MMO/Assets/GM_Game/Scripts/UI/Main/Window/Widget/QuickTaskPanelWidget.cs` | 折叠由隐藏列表改为横向滑出，新增补间与基准坐标 |
| 新增 | `项目修改日志/2026-10-24-任务面板折叠与重合修正_CHANGELOG.md` | 本文件 |
| 修改 | `Docs/AI/CURRENT_TASK.en.md`、`Docs/AI/CURRENT_TASK.md` | 追加 `[CT-012]` 记录 |
| 修改 | `Docs/AI/TEST_REPORT.en.md`、`Docs/AI/TEST_REPORT.md` | 追加 `[VERIFY-QUICKTASK-LAYOUT-20261024]` 记录 |

未新增或删除资源、`.meta`、场景文件；既有无关改动（`MainCityMgr.cs`、`Scene_Boot.unity`、字体 SDF 资源、`UserSettings/`、`yoo/` 等）保持原样，未纳入本次提交。
