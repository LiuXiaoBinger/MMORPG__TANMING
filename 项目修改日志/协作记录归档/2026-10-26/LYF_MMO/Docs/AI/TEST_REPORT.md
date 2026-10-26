# 测试报告

## 2026-10-25 任务 UI 对齐与间距修复 静态验证（编译与实机未测）

- 改动范围：4 个预制体——`Task.prefab`（13 对齐 + 6 页签文字矩形 + 实例根 modification）、`Toggle_List.prefab`（4）、`QuickTaskPanel.prefab`（16）、`Btn_Big_01.prefab`（1）；全部 BottomRight/BottomLeft → MiddleCenter/MiddleLeft（按原版 UGUI 对齐逐一还原）。
- Unity 侧验证（MCP `AssetDatabase.ImportAsset` ForceUpdate|ForceSynchronousImport 后读回）：Task.prefab 16 个 Center + 14 个应保持的（12 TopLeft + 2 Left），无过度修复；页签 ON Text align=Center + 原版拉伸矩形；Toggle_List 4/4、QuickTaskPanel 16/16、Btn_Big_01 1/1。
- 结构完整性：Task.prefab 对比备份文档数 432=432，YAML 头与 `--- !u!` 分隔符零丢失；PrefabInstance modification 只改 `m_AnchoredPosition.y`（−9→19），未触碰其他覆盖。
- 测量证据（root-space，激活窗口）：标题栏底 239.41 / TaskSelectPart 顶 247.27 / Tag_Adventure 顶 195.28 → 间距 44.13px；公式「页签顶 = 204.28 + 实例根 aPos.y」以 aPos.y=−9 算出 195.28 与实测一致；修复后预期 223.28 / 间距 16.13px。
- 未验证：运行时实机效果未截图（修复期间 isPlaying=True，需退出重进 play 模式）；16.13px 间距与页签文字视觉居中效果待用户确认；Unity 编译 0 error 未复核（本轮无代码改动，仅预制体）。
- 遗留未动：页签 ON/Off 容器比按钮大、与相邻任务行容器重叠 6.9~9.4px（RO 原版设计）。

## 2026-10-25 全项目字体统一 Common SDF 静态验证（编译与实机未测）

- 替换统计：18 个文件共 315 处（m_fontAsset ×156、m_sharedMaterial ×156、m_GlobalFontAsset ×2、objectReference ×1）+ TMP Settings.asset 的 m_defaultFontAsset ×1。
- 终态复扫：全项目 TMP 组件 388 个，`m_fontAsset` 100% 指向 Common SDF；`m_sharedMaterial` 388 个全部为 `6874971904994996115` + Common SDF guid（成对无错配）。
- 残留检查：UI prefab/unity 文件中旧 guid（FeiHuaSongTi-2 `3993037b…`、LiberationSans `8f586378…`）零残留；唯一保留是 `LiberationSans SDF - Fallback.asset` 内部 `referencedFontAssetGUID`（TMP 包 fallback 链机制，故意不动）。
- 结构完整性：18 个目标文件与转换前备份逐项对比——YAML 头完好、`--- !u!` 分隔符数一致（如 UIRoot 5565→5565）、MonoBehaviour/GameObject/RectTransform 的 fileID 集合零丢失，18/18 通过。
- 属性留档：`_temp/font_properties_before_common_sdf.txt`（156 组件，含字号/对齐/颜色/行距/字距/换行/溢出/富文本/射线），回退时可作核对基准。
- 未验证：Unity 编译 0 error（MCP 桥未启动）；运行时文字显示未截图；FeiHuaSongTi-2/LiberationSans → Common SDF 的字宽差异未逐项测量（尤其 SkillCoreWidget/SkillSlotWidget 的 CD 与快捷键文本由 TMP 默认字体切换，需实机确认）。

## 2026-10-25 任务 UI 文本组件迁移 TMP 静态验证（编译与实机未测）

- 完整性（对比备份）：Task 分隔符一致、旧 Text 28→0、新 TMP 28、组件 fileID 零丢失；Toggle_List 4→0/4；Btn_Big_01 1→0/1；Panel_Common_Medium 1→0/1；QuickTaskPanel 分隔符 253→253、旧 Text 16→0、新 TMP 16、组件 fileID 零丢失。
- 引用面：`Toggle.graphic` / `Button.targetGraphic` / `GameObject.m_Component` 均未受影响（组件 fileID 保留）；`TitleText`/`TargetText1`/`DesText`/`LimitText` 等绑定节点存在。
- 嵌套宿主：`UIRoot.prefab`（36 个 PrefabInstance）、`Task.prefab`、`Friends.prefab`、`Theme_NewTask.prefab` 中，已迁移预制体实例针对旧 Text 的 modifications 覆盖数全部为 0。
- 代码面：`GM_Game/Scripts` 无 `GetComponent<Text>` / `AddComponent<Text>` / `as Text` / `(Text)` 残留；5 个改动文件 `using TMPro;` 均在；18 处字段/签名全部为 `TMP_Text`。
- 未测量 / 未做：Unity 编译 0 error 未验证（编辑器开着但 MCP 桥 502，未启动）；运行时文字显示与布局未截图验证；FeiHuaSongTi-2 SDF 与旧字体的渲染宽度差异未逐项测量；`UIRoot.prefab` 自有 180 个旧 Text 与 `BuyItemWidget` 3 个旧 Text 未迁（不在范围）。
- 追加改动（字体与标题，静态验证）：5 个预制体 TMP 字体全部由 Common SDF 切到 `FeiHuaSongTi-2 SDF`（guid `3993037bb5ace8e4d9f2555109577b24`，material `6718054227848337956`），旧 Common SDF 引用 0 残留，组件数与分隔符数量不变；`Panel_Common_Medium` 标题 `标题最大六字` → `任务`，该预制体仅 Task.prefab 引用、宿主无 m_text 覆盖、代码无运行时写标题。FeiHuaSongTi-2 项目内此前无 prefab 使用，运行时渲染效果未验证。
- 待办：用户切回 Unity 窗口触发刷新编译后，确认 Console 0 error 并进游戏看任务窗口/快捷面板文字。

## 2026-10-24 任务页签改 RO 竖排形态 实机验证（weq/1111 → 1区五域大陆 → Scene_MainCity）

- 编译：`TaskTagItemWidget` 与 `TaskWindow` 均可反射到，0 error。
- 结构：`TaskSelectPart/TabBar` 已删除（返回 null）；`Content` 常驻三个页签 `Tag_Adventure`(0) / `Tag_Story`(1) / `Tag_Other`(2) + `Templent`(末尾)，页签高 65.07。
- 默认页（任务）：Content 顺序 = `Tag_Adventure` → 4 条任务(1001-1004) → `Tag_Story` → `Tag_Other` → `Templent`；`Tag_Adventure ON=True`、其余 Off。
- 点「玩法」：屏幕坐标 (601.15, 405.16) 的 `EventSystem.RaycastAll` 最上层命中 `Off@TogL`；点击后 `Tag_Story ON=True`，Content 顺序 = `Tag_Adventure` → `Tag_Story` → 3 条玩法任务(2001-2003) → `Tag_Other`。
- 点「奇遇」：坐标 (601.15, 392.09)，命中 `Off`；点击后 `Tag_Other ON=True`，Content 顺序 = 三个页签 → 2 条奇遇任务(3001-3002)。
- 解耦：点快捷面板 `Type_1/Off` 后 `TaskMgr.GetCurrentPage()=Adventure`，任务窗口仍 `Tag_Other ON=True`、列表仍 3001/3002。
- 完整性：运行时 `TaskWindow` 的 `GetMonoBehavioursWithMissingScriptCount=0`，`null MonoBehaviour = 0 / 189`；Console 无新增错误（既有 `NiceIO`、`Location have existed`、`PlayerStats`、`TaskDevSampleData` 提示与本次无关）。
- 截图：`_temp/selftest_tags_v2.png`（任务页）、`_temp/selftest_tags_other.png`（奇遇页）。
- 未测量 / 未做：页签点击后滚动位置未逐页记录；页签箭头保留但不做折叠；未用真实鼠标事件回归整窗（本轮用 `RaycastAll` + `pointerClick` 派发）。
- 已知视觉项：页签 ON/Off 用 RO 原图 `UI_Common_Tab_Left_01/02`，选中态与未选中态在截图中明暗差异受背后 `TaskSelectPart` 底图影响，未改美术。

## 2026-10-24 日常窗口接入 + 页签自持 实机验证（weq/1111 → 1区五域大陆 → Scene_MainCity）

- 编译：GM_Game 0 error（`DailyTaskWindow` / `MainView.DailyTaskWindow` / `MainView.DailyTaskButton` 反射均在）。
- 任务窗口三页签：`Tab_Adventure/Tab_Story/Tab_Other` 均存在，切换后列表 4 → 3 → 2 条（1001-1004 / 2001-2003 / 3001-3002），选中态互斥正确。
- 解耦验证：点快捷面板 `Type_2/Off` 后 `TaskMgr.GetCurrentPage()` = `Story`、小面板 `Type_2/ON` 激活；任务大窗口列表仍为 3001/3002、`Tab_Other` 仍选中 → 窗口页签不受小面板影响。
- 日常窗口：功能菜单「日常」点击后 `DailyTaskWindow.activeInHierarchy = true`；关闭按钮屏幕坐标 (230.6, 930.6) 的 `EventSystem.RaycastAll` 最上层命中 `CloseBtn`（depth 155，已压过 RoleButton），派发 `pointerClick` 后窗口关闭。
- 画布修复前后对比（同一坐标）：保留嵌套 Canvas 时日常窗口 `GraphicRaycaster.Raycast` 返回 0 命中、`Graphic.Raycast(点, camera)` 返回 true，最上层命中落到 `RoleButton`；删掉嵌套 Canvas 后同一坐标命中 `CloseBtn`。
- Console：错误 / 警告 0 条，含 Missing Script 检查（`GetMonoBehavioursWithMissingScriptCount = 0`）。
- 截图：`_temp/selftest_task_story.png`、`_temp/selftest_daily_open.png`。
- 未测量 / 未做：日常面板未接数据，活动图标与列表为空；面板无打开动效与相机联动；`Img_DailyTaskBg` 底图缺失（RO 原工程同样缺失），视觉上只有地图与标题栏。

## 2026-10-24 任务页签实机验证（weq/1111 → 1区五域大陆 → Scene_MainCity）

- 编译：GM_Game 程序集 0 error；旧枚举成员 `MainLine/Daily/Activity` 全 Assets 0 命中；反射确认枚举为 Adventure/Story/Other。
- 射线修复验证：TaskWindow / QuickTaskPanel Canvas 均有 `GraphicRaycaster`；`Tab_Story` 屏幕坐标 (601,765) 射线最上层命中 `Background@Tab_Story`；快捷面板 `Type_3/Off` 命中 `Text`；`Btn_Close` 命中自身。
- 页签切换：快捷面板点「奇遇」后 `page Adventure→Other`，追踪列表 `[1001,1002,1003]→[3001,3002]`，ON/Off 显隐正确；任务窗口点「玩法」「奇遇」「任务」三档切换正常。
- 列表与标题：任务页 4 条（标题「任务」）、玩法页 3 条（标题「玩法」）、奇遇页 2 条（标题「奇遇」）。
- 回归：进游戏聊天默认收起（ChatWindow/BG hidden）；X 按钮射线命中并成功关窗。
- 同帧陷阱记录：`SetActive(true)+InitWindow` 同帧内 `EventSystem.RaycastAll` 返回 0 命中，下一帧恢复正常，属 Canvas 注册延迟，非故障。
- 未测量项：背包窗口补射线器后未逐项回归交互；其余 28 个缺射线器预制体未动、未测。

- 任务：`MTimerMgr` 更新时间与到期回调链路解析。
- 静态验证：已核查 `Global.Awake` 初始化、`Global.Update` 调用顺序、`MTimerMgr.Update` 到期循环及 `MArtSkill.SetTimer` 注册用例。
- 结果：调用链可闭合；本次没有业务代码改动，因此未执行 Unity 编译或运行时场景测试。

## 2026-10-21 SkillRootMotionCollector 静态核查

- 已读取目标脚本及 `MModel`、`MArtSkill`、`SkillRuntimeRootMotionSegment`、`MMoveComponent`、`MEntity` 相关代码。
- 确认：采集原生 Animator 水平位移，按技能原动画时间窗口缩放；消费后清零，最终由模型移动入口提交。
- 确认：本地移动消费段位于生效的 `#if true` 分支；模型存在且启用 CharacterController 时执行碰撞移动，否则直接改位置。
- 修改文件：分析正文、分析子目录 README、本次修改日志、CURRENT_TASK 和本报告，均为 Markdown。
- 验证边界：只做源码静态核查和文档格式检查，无业务代码改动，不执行编译或场景测试。
- 未解决事项：具体技能距离、动画更新时序及碰撞表现未经运行时验证。下一步按用户指定的技能复现核查。
