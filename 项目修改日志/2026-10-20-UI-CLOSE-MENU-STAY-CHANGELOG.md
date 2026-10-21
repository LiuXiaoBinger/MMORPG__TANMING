# 顶部关闭按钮与功能菜单保持展开

- 用户需求：检查各界面顶部关闭按钮，补未接关闭功能的按钮；点击背包、角色、技能、设置后不自动折叠功能菜单。
- 范围：客户端调查正式UI按钮与现有窗口/View/Ctrl关闭链，只补真实缺失；菜单保初始收起与箭头手动开关，仅取消四入口自动收起。
- 职责：main_menu_client修改正式客户端，main_menu_review独立冻结审查与编译，主协调验收和协作记录。
- 设计：复用既有关闭和镜头恢复流程，不增加新UI框架或状态门禁，不改无关功能。
- 调查：正式背包Btn_Close、技能tex_close、角色属性btn_close已有Button但持久监听为空；对话/商城/设置有CloseWindow，服务器列表/注册返回/购买弹窗已有各自流程，不重复增加；常驻HUD/小地图及没有顶部关闭按钮的流程界面不新增X，遗弃背包只记录。
- 正式范围：`LYF_MMO/Assets/GM_Game/Scripts/controller/UIController/MainCtrl.cs`取消四句自动收起；`LYF_MMO/Assets/GM_Game/Scripts/UI/Main/Window/KnapsackWindow.cs`及`RoleAttriibuteWindow.cs`增加最短CloseWindow override，隐藏后分别调用原CameraMgr的KnapsackWindowAngle/RoleAttrWindowAngle；`LYF_MMO/Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab`补3Button的CloseWindow持久监听。不改CameraMgr或WindowBase。
- 验证完成：两方完整281源编译0错误/38既有警告，无新增签名；独立18项、主11项通过。162冻结路径仅4正式文件变化，Root5530对象保留/5527块保持，仅3Button.m_OnClick变更。主保护输入及两方编译源SHA保持。
- 原生验证：PreviewScene克隆6主城窗口按钮真实Invoke后均隐藏/目标正确，缺脚本0；克隆监听临时EditorAndRuntime、CameraMgr单例临时隔离后finally恢复。当前Play两个新override未加载，不能把此结果当镜头恢复已原生验收；主城前后dirty=false。
- 生产方法专项：逐字提取14段真实方法，28项验证四入口保持菜单、两顶部关闭后的背包/属性状态、原800/460偏移和保存视角恢复、B/I重开、箭头手动收起。Unity对象/窗口数据刷新/补间是薄边界，正式Play画面与包端仍待正常编译重载后验证；未主动启停、保存场景、构建或发布。最初方法数量口头13已依真实清单纠为14，独立逐字/SHA复核通过。
- 证据：`_temp/ui-close-menu-stay-20261020`；相关专题`项目分析/MAIN_FUNCTION_MENU_UI.md`，关闭按钮调查专题按实际结论归档到`项目分析/`。
- 验证工具事实：审查首夹具把技能CloseWindow声明类型误要求SkillInfoWindow，改为实际继承WindowBase，first报告保留；主首次误猜CameraMgr目录后定位Manger，无业务返工。普通接线任务不新增面试难点，不扩无关业务。
- 未解决事项：无本轮实现阻断；正式重载后镜头画面/运行入口及包端未新增验收。4末稿SHA见`_temp/ui-close-menu-stay-20261020/client/after-sha.json`，主汇总`root-final-validation.json`，临时文件保留。
- 末态：主城Play/非编译/dirty=false、临时残留0、Console0，CameraMgr.Instance已恢复正式FreeLook Camera（非Preview）；开发交付7项通过。专题`项目分析/MAIN_FUNCTION_MENU_UI.md`已同步逐界面关闭清单与菜单最新行为，角色记录见`项目修改日志/2026-10-20-UI-CLOSE-MENU-STAY-CLIENT.md`及`项目修改日志/2026-10-20-UI-CLOSE-MENU-STAY-REVIEW.md`。
