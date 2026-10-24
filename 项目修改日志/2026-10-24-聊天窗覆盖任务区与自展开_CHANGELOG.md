# 聊天窗改为自展开并覆盖快捷任务区（2026-10-24）

## 背景

上一轮为解决聊天窗与快捷任务面板重叠，把 `ChatWindow` 整体下移 198.33 画布单位，用户反馈「这样有一点丑」，并给出新要求：

1. 聊天界面**自己打开**，并**把任务面板那一侧遮住**，不要再靠下移避让；
2. 顺手改造这个聊天 UI 控件；
3. 测试发送、接收消息与上下滚动是否正常。

## 关键发现（先纠偏，避免改错对象）

排查 `UIRoot/Canvas/MainView/ChatWindow` 时确认：可见的聊天面板**不是**由 `GM_Game/Scripts/UI/Main Window/ChatWindow.cs` 驱动的。

| 对象 | 实际角色 |
| --- | --- |
| `MainView/ChatWindow`（可见面板） | 由 `Assets/artres/Resources/Scripts/Chat/ChatWindows.cs` + `ChatWindowCollapseController.cs` 驱动，频道页签、消息池、未读角标、快捷聊天都在这里 |
| `MainView/ChatWindow_遗弃`（inactive） | 挂着 `GM_Game` 的旧 `ChatWindow` 组件，`_content/_dropdown/_iptChat` 全指向自身子树，运行时不参与 |

因此：

- 上一轮「发送按钮没有回调」的判断是错的。可见面板的 `BtnSend` 由 `ChatWindows.Awake` 里 `sendButton.onClick.AddListener(SendInputMessage)` 在代码中绑定，预制体上没有 persistent call 是正常的。
- 真正让人工「打不开」的是 `ChatWindowCollapseController.startCollapsed = 1`：首帧 `SetExpanded(false)` 会把 `BG`（展开态面板）隐藏、只留 `QuickChat`（快捷聊天条）。
- 真正让人工「丑」的是渲染层级：`QuickTaskPanel` 在 `MainView` 子节点里排第 13，`ChatWindow` 排第 9，因此任务面板画在聊天窗**上面**，两者重叠时是互相穿插而不是干净遮挡。

## 改动清单

全部集中在 `LYF_MMO/Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab`，未改任何脚本。

### 1. 打开方式：默认展开

- `ChatWindowCollapseController.startCollapsed`：`1` → `0`
- `BG.m_IsActive`：`0` → `1`
- `QuickChat.m_IsActive`：`1` → `0`

进入主城后聊天面板自己就是展开态，右侧雪佛龙按钮（`BtnClose`）仍可一键收回成快捷聊天条。

### 2. 位置与尺寸：改成左侧整列

`Canvas/MainView/ChatWindow`：锚点 `(0, 0.5)` 不变，`(299, −205) / 530×630` → `(299, −90) / 530×860`。

得到画布矩形 x[−926, −396] y[−520, 340]：

- 上沿 340 压过 `Btn_Fold`(290~340)、`TypeGrp`(298~332)、`TaskList`(128~298)，即整块快捷任务区；
- 下沿 −520 距画布底边 20；
- 与功能菜单（x[−936, −476] y[378, 470]）留 38 空隙。

### 3. 渲染层级：聊天窗压住任务面板

`MainView` 子节点里把 `QuickTaskPanel` 从索引 13 调到 8，其余顺延：

```
… 7 NPCShopWindow / 8 QuickTaskPanel / 9 RoleAttriibuteWindow / 10 ChatWindow / 11 KnapsackWindow / 12 SettingPanel / 13 TaskWindow
```

`ChatWindow` 因此高于 `QuickTaskPanel`（干净遮挡），但仍低于 `TaskWindow`，任务主窗口不会被打字面板盖住。

### 4. 面板内部按新高度重排

| 节点 | 旧 | 新 |
| --- | --- | --- |
| `Img_BG01`（底板） | (0, −27.19, 5.96) 480.38×628.95 | (−27, 0) 480.38×860 |
| `Img_BG02`（消息区底） | (16.44, 27.01) 375.49×556.5 | (16.5, 27.5) 375.49×761 |
| `ChatLineGroup`（视口） | (16.44, 27.01) 375.49×558 | (16.5, 27.5) 375.49×761 |
| `ChatSenderPanel`（输入条） | (255, 40) 504×58 | (238, 40) 456×58 |
| `BtnVoice` | (44.2, −1.3) 56×57 | (33, −1) 50×56 |
| `InputMessage` | (−59.49, 3.06) 225.94×40 | (−58, 3) 208×40 |
| `BtnFace` | (77.19, 0.4) 56×56 | (79, 0) 50×56 |
| `BtnSend` | (154.24, 3.31) 111.7×49.79 | (166, 3) 108×50 |
| `BtnClose`（雪佛龙） | (491.9998, 0) | (493, 0)，尺寸不变 30×128 |

- 输入条原本 504 宽（x −923~−419）会伸出底板右沿（−448），现在收到 456 宽（x −916~−460），左右各留 8，四个控件之间 8 等距。
- `BtnClose` 用 anchor `(0, 0.5)`，坐标相对面板左沿，实际只从 491.9998 挪到 493（贴底板右沿外侧 2），视觉位置基本不变。
- 未改 `Tog_Group`(11, −14, 72×357) 与 `SettingBtn`(47.4, 103.3, 56×57)：面板下沿仍是 −520，这两个相对底部的定位结果不变。

### 5. 顺手修正

- `TogProfession/Background/Label` 文案：`队伍` → `职业`（与 `TogTeam` 的「队伍」重名，页签列表里出现两个「队伍」）。
- 未动 `ChatLineGroupContent` 的 `VerticalLayoutGroup` 与锚点：`ChatWindows.ConfigureChatList()` 明确「保留预制体原有的布局系统」，行宽/行高由对象池和原布局决定，本轮不碰，避免影响消息行尺寸。

## 验证情况

已做（静态）：

- 预制体 YAML 结构校验：文档数 5490 与 HEAD 一致，重复 fileID 0，悬空 `{fileID}` 引用 64 处与 HEAD 完全一致（均为嵌套预制体内部引用），本轮未新增。
- 回读关键节点矩形，与上述目标值逐项一致；`ChatWindow` 与任务面板区、功能菜单无意外重叠（仅底部与外层 HUD 有既有的 69 像素横向重叠，改动前即存在）。
- 未改脚本，无编译面风险。

未做（受工具链阻塞）：

- 未能在播放态实测发送/接收/滚动。Unity 编辑器的 MCP 通道在切换到 `Scene_MainCity` 播放后断开，`execute_code` 持续返回空，编辑器侧 `[WebSocket] Keep-alive failed`，插件每 45 秒重连一次但请求无法送达主线程，直到本轮结束仍未恢复。
- 因此本轮 UI 效果与消息收发、滚动均属于「改完待实测」，需要恢复连接或人工过一遍。

## 人工验证清单（约 30 秒）

1. 进入主城：聊天面板应默认展开，占满左侧整列，把「任务 / 玩法 / 奇遇」页签行和任务列表完全盖住（不再互相穿插）。
2. 点右侧雪佛龙：面板收成左侧快捷聊天条，任务面板露出；再点一次恢复展开。
3. 点 `任务` 入口开任务主窗口：任务主窗口应压在聊天窗上面。
4. 发送：页签选「世界」，输入框打字，点「发送」→ 列表出现一条自己的消息并自动滚到最新。
5. 接收：`ChatDemoControls` 的三个演示按钮（他人消息 / 本地消息 / 系统消息）可直接造数据；他人消息会按未读规则提示。
6. 滚动：消息超过一屏后鼠标滚轮上下滚，滚到底部后新消息自动跟随。

## 文件

- 修改：`LYF_MMO/Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab`
- 未修改：`ChatWindow.cs`、`ChatItemWidget.cs`、`ChatItemWidget.prefab`（中途改动后已还原，仓库内与 HEAD 一致）
