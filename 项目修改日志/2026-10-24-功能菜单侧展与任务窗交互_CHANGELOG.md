# 功能菜单侧面展开、任务面板间距与任务窗拖动关闭（FUNCTIONMENU-LAYOUT-20261024）

- 日期：2026-10-24
- 分支：`codex/task-system-ui`
- 需求来源：
  1. 功能菜单（背包/角色/技能/设置/任务）与快捷任务页签“挨着太近”；
  2. 用户澄清上一轮理解有误——要侧面展开的是**功能菜单**，点击小箭头后入口组应在**小箭头右侧**展示；
  3. 任务主窗口需要支持鼠标拖动，叉号需要能关闭窗口，可参考其他 UI 窗口。
- 执行者：WorkBuddy 会话（本机 Unity MCP + 文件编辑）
- 实际使用技能：本次未使用技能，全部工作由本机原生工具完成（文件读写、grep、Unity MCP 预制体读取与保存）

## 一、上一轮的理解偏差

上一轮我把“上面 UI 侧面收缩”理解成快捷任务面板的任务列表横向收起，并在 `QuickTaskPanelWidget.SetFolded` 里改成了横向滑出。本轮用户明确：侧面展开指的是**功能菜单入口组**（背包/角色/技能/设置/任务）。任务列表的横向收起按用户上一轮的选择保留，未回退。

## 二、修改内容

### 2.1 功能菜单改为在小箭头右侧展开

`LYF_MMO/Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab`：

| 节点 | 字段 | 修改前 | 修改后 |
|---|---|---|---|
| `FunctionMenuContent` | `m_AnchoredPosition` | (0, -46) | (116, -46) |
| `FunctionMenuButton` | `m_AnchoredPosition` | (0, 0) | (0, -73) |

- 入口组整体右移 116（箭头按钮宽 108 + 8 像素间隙），从“在箭头下方”改为“在箭头右侧”。
- 箭头按钮下移到与入口组同一行的垂直中线（(92−38)/2 = 27，叠加原 −46），不再是浮在入口组上方。
- 两者都是左上锚定（anchor 0,1 / pivot 0,1），改动只动 `m_AnchoredPosition`，不影响内部按钮布局。

改后画布坐标（1920×1080，原点居中）：

| 控件 | X 范围 | Y 范围 |
|---|---|---|
| 箭头按钮 | −936 ~ −828 | 405 ~ 443 |
| 菜单内容 | −820 ~ −476 | 378 ~ 470 |
| 五个入口按钮中心 X | 背包 −774、角色 −692、技能 −610、设置 −528、任务 −446 | 424 |

### 2.2 `LYF_MMO/Assets/GM_Game/Scripts/UI/Main/MainView.cs`

`SetFunctionMenuVisible` 的箭头朝向由“上下翻转”改为“左右指示”：

| 状态 | 修改前 | 修改后 |
|---|---|---|
| 展开 | `(0,0,180)` 朝下 | `(0,0,90)` 朝左，提示可收回 |
| 收起 | `(0,0,0)` 朝上 | `(0,0,-90)` 朝右，提示从右侧展开 |

字段注释同步改为左右语义。

### 2.3 功能菜单与任务页签的间距

`LYF_MMO/Assets/artres/Resources/UI/Prefabs/QuickTaskPanel.prefab` 三处再下移 25：

| 节点 | 修改前 | 修改后 |
|---|---|---|
| `TypeGrp` | −200.31 | −225.31 |
| `TaskList` | −302.22 | −327.22 |
| `Btn_Fold`（嵌套实例覆盖） | −200.31 | −225.31 |

入口按钮下沿 382、页签行上沿 332，间距由 25 增到 50 画布单位。

### 2.4 任务主窗口支持标题栏拖动

`LYF_MMO/Assets/artres/Resources/UI/Prefabs/Task.prefab`：

- `Img_Bg/BG/Img_TitleBg` 的 Image `m_RaycastTarget` 由 0 改为 1，让标题栏能接收射线（原来只有 46×46 的叉号能点，标题栏本身不可命中）。
- 在同一节点新增项目既有组件 `WindowTitleDragHandle`（脚本 guid `a4c44a317e2d4a078b09c8e5f6e0d0ab`），`_targetWindow` 指向 `Task` 根 RectTransform。
- 该节点属于嵌套预制体 `Panel_Common_Medium.prefab` 的实例，因此组件以“实例新增组件”形式写在 `m_AddedComponents` 里，不改动被共用的基础预制体。
- 拖动实现完全复用 `UIRoot/Canvas/MainView/KnapsackWindow/BG/Line`、`RoleAttriibuteWindow/Line` 的同款方案，没有新增脚本。

### 2.5 任务窗口叉号关闭

叉号关闭链路**本来就完整**，本轮复核未发现断点，代码未改：

```
Btn_Close(Button.m_OnClick → OnCloseButtonClick, 目标 TaskWindow 本体)
  → TaskWindow.OnCloseButtonClick
  → CloseRequested 事件
  → TaskCtrl.OnTaskWindowCloseRequested（构造函数 RegisterActions 中订阅）
  → TaskWindow.CloseWindow() → gameObject.SetActive(false)
```

复核证据：`UIRoot.prefab` 实例内 `Btn_Close` 的 `onClick` 持久调用 1 条、方法名 `OnCloseButtonClick`、目标与实例化后的 `TaskWindow` 是同一对象；`MainView._taskWindow` 已绑定到该实例，`TaskCtrl` 在 `UIRoot.InitMainCtrl()` 创建时订阅该事件。

## 三、验证

| 项 | 结果 |
|---|---|
| 编译 | Unity 强制刷新并请求编译，Console error 0 |
| 功能菜单 | 箭头按钮 x[−936,−828] y[405,443]；菜单内容 x[−820,−476] y[378,470]；内容左边缘在箭头右边缘右侧 **8** 像素 |
| 间距 | 页签行 y[298,332]、折叠按钮 y[290,340]、任务列表 y[128,298]；与入口按钮下沿 382 的间距为 **50** |
| 拖动组件 | `TaskWindow/Img_Bg/BG/Img_TitleBg` 上存在 `WindowTitleDragHandle`，`_targetWindow` 指向 `TaskWindow` 自身，标题栏 `raycast=True` |
| 叉号 | 实例内 `calls=1`、`OnCloseButtonClick`、目标类型 `TaskWindow`、`same=True` |
| 文件改动量 | `Task.prefab` +25 行、`UIRoot.prefab` 4 行、`QuickTaskPanel.prefab` 6 行，未出现预制体整体重排 |

## 四、未验证与边界

- 未进入播放态，**没有实际点击叉号、也没有实际拖动窗口**。叉号的运行期链路是按代码与序列化绑定复核的，未做运行时点击验证；拖动的位移手感（`eventData.delta / scaleFactor`）同样未实机验证。
- `WindowTitleDragHandle` 用 `GetComponentInParent<Canvas>()` 取缩放，取到的是窗口自带的嵌套 WorldSpace Canvas，其 `scaleFactor` 为 1；与根画布 `CanvasScaler` 的 1 倍一致，故换算成立。若以后把参考分辨率改到与屏幕宽度不一致，该系数需要复核。
- 功能菜单默认收起（`FunctionMenuContent.m_IsActive = 0`），展开仍是 `SetActive` 直接显示，没有做滑动补间；用户如需要“滑出”动效需另加。
- 标题栏启用射线后，从叉号上按下并拖动也会移动窗口（与 `RoleAttriibuteWindow/Line`、`KnapsackWindow/BG/Line` 现有行为一致）。
- 页签行横向位置未随入口组右移，展开态下页签行比入口组左边缘靠左约 100 像素；用户未提出要求，未改动。
- 任务列表下移后与 `ChatWindow` 的既有重叠进一步增加（修改前即存在，不在需求范围）。

## 五、文件清单

| 类型 | 文件 | 变化 |
|---|---|---|
| 修改 | `LYF_MMO/Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab` | `FunctionMenuContent` 右移 116、`FunctionMenuButton` 下移 73 |
| 修改 | `LYF_MMO/Assets/GM_Game/Scripts/UI/Main/MainView.cs` | 箭头左右朝向、字段注释 |
| 修改 | `LYF_MMO/Assets/artres/Resources/UI/Prefabs/QuickTaskPanel.prefab` | 三处再下移 25 |
| 修改 | `LYF_MMO/Assets/artres/Resources/UI/Prefabs/Task.prefab` | 标题栏射线开启 + 新增 `WindowTitleDragHandle` 实例组件 |
| 新增 | `项目修改日志/2026-10-24-功能菜单侧展与任务窗交互_CHANGELOG.md` | 本文件 |
| 修改 | `Docs/AI/CURRENT_TASK.en.md`、`Docs/AI/CURRENT_TASK.md` | 追加 `[FUNCTIONMENU-LAYOUT-20261024]` |
| 修改 | `Docs/AI/TEST_REPORT.en.md`、`Docs/AI/TEST_REPORT.md` | 追加 `[VERIFY-FUNCTIONMENU-LAYOUT-20261024]` |

未新增或删除资源、`.meta`、场景文件；既有无关改动（`MainCityMgr.cs`、`Scene_Boot.unity`、字体 SDF 资源、`UserSettings/`、`yoo/` 等）保持原样，未纳入本次提交。
