# 2026-10-20 左上可收缩功能菜单：独立审查与编译

## 范围与结论

- 用户明确入口位于左上角，参照一梦江湖的功能区收缩隐藏，纳入背包、角色、技能、设置四个现有入口；本轮不实现设置选项效果。
- 审查结论：通过，无需业务返工。没有发现 Block、Critical 或需要修改的 Warning 问题。
- 审查角色只读取正式源码与资源，未修改客户端或服务端业务，未调用 Unity MCP。主 Agent 统一维护 `Docs/AI` 四份协作记录。

## 修改文件与思路核查

- `LYF_MMO/Assets/GM_Game/Scripts/UI/Main/MainView.cs`：持有正式菜单开关、内容容器、文字和四入口引用；初始化默认收起，以容器自身显隐切换菜单与提示。
- `LYF_MMO/Assets/GM_Game/Scripts/controller/UIController/MainCtrl.cs`：注册、成对注销五个按钮监听；背包、角色、技能分别复用原 B、I、L 流程；设置仍调用现有 `ShowMainWindow(WindowType.SettingPanel)`。选入口后只收起功能区，不引入额外业务状态或 UI 框架。
- `LYF_MMO/Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab`：左上锚点、距左上各 24 设计像素，正式保存控件与引用；复用原设置按钮，原设置窗口内部不变。
- `LYF_MMO/Assets/GM_Game/UISprites/MainMenu.meta` 与目录下四份 `Menu_Knapsack/Role/Skill/Setting.png`、对应 `.meta`：从现有 `UISprites/ExtractedMenuUi` 对应图标裁切，保留原图。本轮正式变化恰为 12 路径，没有增加其他玩法或协议。

## 独立验证

- 完整客户端编译：`python -X utf8 _temp/main-menu-integration-20261020/review/compile_client.py final`，实际 281 源，0 错误、38 警告。与上一轮末稿相比无新增警告消息签名；21 种签名不等于警告条数。编译源 SHA 在末稿核查中全部保持。
- 限定资源审计：`review/audit.py` 对照审查前冻结的 1085 路径，只有上述三旧文件修改和九新路径，无删除；新增及变化对象的本地引用、外部 GUID 均能解析。十个无关关键输入 SHA 保持。
- `UIRoot` 原 5472 对象全部保留，新增 58 对象；5465 原对象块逐字保持。仅主界面挂接、MainView 引用、原设置按钮及文字布局等七个必要旧对象变化，共 33 个字段差异。原设置窗口子树 2304 对象逐字保持。
- `review/final-audit.json` 的 24 项检查全通过：正式范围与开发冻结 SHA、独立编译源、保护输入、旧对象、引用、左上锚点、默认收起、入口层级、监听对称、原快捷键流程、设置窗口接入和代码规范等。
- 四张裁图逐像素对照源图指定区域：角色图仅三像素、设置图仅二十二像素的 Alpha 清零，RGB 不改；另两图无像素改动。尺寸、GUID、四份原图 SHA 均一致，裁切来源见 `client/icon-sources.json`。
- 人工源码审查：新增字段、属性、方法及 `visible` 参数均有具体中文注释；无新增命名空间、三目、`foreach`、`object/dynamic` 业务载荷、组合布尔表达式或设置业务效果。既有未修改代码不扩大整改。
- 独立目检开发原生隔离两图 `client/preview-collapsed.png` 与 `client/preview-expanded.png`：收起仅显示“功能 +”，展开显示四个对应图标和中文标签，左上留白清楚，没有覆盖底部 HUD 或右上小地图。

## 验证边界、未解决事项与交接

- 当前 Play 仍加载旧 `MainView/MainCtrl`，开发的原生预览是在正式资源克隆中移除业务脚本、调整克隆显隐和文字后渲染，证明导入与布局；不能等同新生产方法、主城真实鼠标点击或打包端验收。审查没有启停 Play、保存正式场景、构建或发布。
- 编译与静态接线未发现阻断。下一步正常加载新程序集和 `UIRoot` 后，在真实主界面点击展开、收起和四个入口，核对背包、角色原镜头流程；包端按原热更与资源更新流程验收。设置项仍按用户要求暂不接效果。
- 临时布局解析最初两次未兼容 Unity stripped 对象，分别发生 `m_Name`、`m_GameObject` KeyError；只修 `_temp` 解析脚本，未修改生产代码迎合测试。最终审计已兼容 stripped 对象并通过，首次事实在此保留。
- 证据统一位于 `_temp/main-menu-integration-20261020/review/`，保留基线、编译产物和报告，待产品经理确认后处理。总体说明由主 Agent 汇总至 `项目分析/MAIN_FUNCTION_MENU_UI.md`，本日志路径供该专题引用。
