# 2026-10-20 窗口关闭按钮与菜单常驻：独立审查

## 结论与修改范围

- 用户要求检查各界面顶部关闭按钮，补实际未接通的关闭行为；点击背包、角色、技能、设置时不再自动收起左上菜单。
- 独立审查通过，无需业务返工。正式仅修改四文件：`LYF_MMO/Assets/GM_Game/Scripts/controller/UIController/MainCtrl.cs`、`LYF_MMO/Assets/GM_Game/Scripts/UI/Main/Window/KnapsackWindow.cs`、`LYF_MMO/Assets/GM_Game/Scripts/UI/Main/Window/RoleAttriibuteWindow.cs`、`LYF_MMO/Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab`。
- 审查角色只读正式文件；冻结、扫描、编译和静态审计输出均位于 `_temp/ui-close-menu-stay-20261020/review/`，没有调用 Unity MCP 或修改正式业务、场景、资源。主 Agent 统一维护四份协作入口。

## 独立缺口扫描与实现核查

- 修改前扫描正式 `Scripts/UI` 与全部 `UIPrefabs`，在 Root 中确认正式背包 `BG/Btn_Close`、角色 `btn_close`、技能 `pan_right/Image/tex_close` 已有按钮但持久监听为空。没有为此新增重复按钮。
- 对话、NPC 商城、设置已有 `CloseWindow`，服务器列表已有 `OnCloseBtnClicked`，注册已有 `OnBackBtnClicked`，购买弹窗已有 `CloseDialog`，保留原路径。登录、选角、HUD、小地图等流程或常驻视图没有当成可随意关闭的弹窗；遗弃背包未接正式入口，不恢复其旧业务。
- Root 只为三个空按钮补指向各自真实窗口组件的 `CloseWindow`，不改其他关闭路径、布局或状态。
- 背包和角色各增加一个短 `CloseWindow` 重写：先调用基类隐藏窗口，再分别调用原 `CameraMgr.KnapsackWindowAngle(this)`、`RoleAttrWindowAngle(this)`。原镜头代码由 `activeSelf=False` 进入原 B/I 键关闭分支，不重复实现镜头恢复或增加状态管理。
- MainCtrl 四入口只移除 `SetFunctionMenuVisible(false)`，仍沿原 B/I/L 与设置窗口流程。手动箭头切换、初始化默认收起、按钮注册/注销保持原样。

## 独立验证

- 修改前冻结 162 路径，覆盖 MainCtrl、CameraMgr、正式 Scripts/UI 全目录与 UIPrefabs。最终基线对照只有上述四路径变化，无新增、删除；开发四文件末稿 SHA 与独立读取一致。
- 完整编译命令：`python -X utf8 _temp/ui-close-menu-stay-20261020/review/compile_client.py final`，实际 281 源，0 错误、38 既有警告，无新增警告消息签名。全部编译输入 SHA 与最终源码一致。
- 静态审计命令：`python -X utf8 _temp/ui-close-menu-stay-20261020/review/audit.py`，`final-audit.json` 共 18 项检查全部通过。
- Root 原 5530 对象无增删，5527 原对象块逐字保持。仅三个 Button 的 `m_OnClick` 变化，每个只有一项启用的无参数 `CloseWindow` 持久调用，真实目标有效、本地引用无悬空。技能目标为 SkillInfoWindow 组件，声明方法可由 WindowBase 继承，绑定合法。
- MainCtrl 四方法的代码体除自动收起一行外完全保持；`OnFunctionMenuRequested`、`MainUIKeyHandler`、`RegisterViewActions`、`Dispose` 逐方法保持。五个菜单监听 Add/Remove 仍各一次。
- MainView 与 CameraMgr 整文件保持，默认收起和箭头仍有效；技能仍继承基类隐藏，其他所有持久按钮事件保持。背包/角色新重写严格先隐藏后调用原镜头入口。
- 新增两个重写和修改四入口的中文注释用途具体，无新增命名空间、三目、`foreach`、通用载荷或组合布尔判断，没有引入假设门禁、通用 UI 框架或测试入口。
- 首次临时审计把技能持久调用的声明类型 `WindowBase` 误要求成 `SkillInfoWindow`，一项失败；基于真实继承关系改为核实际组件目标，保留 `audit-first.json`，只修 `_temp` 夹具，没有要求生产代码迎合错误检查。

## 原生证据核阅与边界

- 只读核阅开发 `client/native-close.cs` 与 `native-close-report.txt`：背包、角色、技能、对话、商城、设置六个真实资源窗口克隆的按钮持久目标正确，Invoke 后均隐藏；正式源缺脚本 0，主城场景 dirty 前后均 False。
- 该原生夹具在 PreviewScene 的失活父节点下克隆窗口，避免正式网络及窗口生命周期；仅克隆的持久监听临时改 `EditorAndRuntime`。临时相机管理器引用在 finally 恢复，没有改正式场景或窗口资源。
- 当时背包/角色新重写尚未加载，`knapsackOverrideLoaded=False`、`roleOverrideLoaded=False`。因此六次原生调用证明按钮绑定及继承隐藏路径，不能称新镜头恢复、正式玩家鼠标关闭或完整主城联调已通过。
- 开发 `client/methods/result.log` 的 28 项生产方法专项通过。审查独立复核清单中 14 段方法的 SHA 与当前真实源码一致，并且原文逐字存在于 `Program.cs`；覆盖四入口开/关后菜单保持、原属性/背包组合与镜头恢复参数、顶部关闭后快捷键再开和手动箭头收起。`ShowMainWindow` 字典显隐、Unity 对象和 Cinemachine 补间为薄边界，仅验证真实方法提交的状态与参数，不代表正式画面、动画补间或完整窗口生命周期。未为审查另加或重跑镜像业务测试。
- 原生夹具 `finally` 恢复 `CameraMgr.Instance` 已独立核对，四正式末稿 SHA 在专项核阅后仍保持；结果见 `review/method-evidence-review.json`。
- 本审查未启停 Play、未保存场景、未构建发布。无已发现阻断；下一步正常加载新代码/Root 后在实际主城验证关闭按钮、角色镜头恢复、背包状态及菜单保持展开，包端按原热更与资源更新流程生效。
- 独立证据保留 `before-sha.json`、`close-baseline.json`、`dialog-buttons-before.json`、`final/` 编译结果、`final-audit.json`、`developer-freeze-match.json`，供主 Agent 汇总协作入口及最终交接。
