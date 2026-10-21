# RO设置界面融合

## 本轮范围

用户要求查看RO设置界面并融合到本项目，随后明确“功能可以先不写，只需要将对方设置界面集成到我们项目中，然后遵循我们UI设计模式进行约束”。因此本轮只处理界面资源、现有窗口入口及打开/关闭/分页/滚动等必要交互，不实现画质、帧率、音量、挂机、推送或FPS统计。

## 参考来源

参考工程：`F:/UnityPro/ro/Ro_cs422nb`。正式界面位于`clientproj/Assets/artres/Resources/UI/Prefabs`：Setting.prefab为外框、关闭按钮和分类模板；SettingSystem、SettingPlayer、SettingAuto、SettingPushNotification为四类内容页。Lua中的SettingCtrl负责分类切换，SettingSystemHandler等负责业务设置，本次只取界面结构和必要展示交互。

| 页面 | RO界面内容 | 本轮处理原则 |
|---|---|---|
| 系统 | 画质、人数、帧率、伤害数字、描边、音量 | 保留界面结构，不应用游戏参数 |
| 个人 | 目标优先级、操作、视角、隐私、账号等 | 页面展示，不接业务与登录行为 |
| 挂机 | 自动战斗、技能槽、拾取和范围 | 页面展示，不实现自动战斗与拾取 |
| 推送 | 总推送、活动、夜间及列表 | 页面展示，不新增通知SDK或请求 |

## 本项目UI模式与约束

沿现有`MainView → UIBase.windowDic/WindowType → WindowBase`登记窗口，由现有`MainCtrl → ShowMainWindow`处理主城入口。窗口和Widget只管理控件展示与界面交互，业务处理留在对应控制器；本轮不创建业务设置层。

正式控件及引用保存到预制体，复用本项目字体、窗口层级和已有视觉素材；RO专用Lua绑定、事件脚本和外部依赖不得原样带入。必要的原生UGUI组件和素材按实际引用适配，不为原版组件补建兼容框架。

约束同步位置：`AGENTS.md`、`AIconfig/config.toml`、`AIconfig/workflow.md`、`AIconfig/agent/unity_client.md`。规则同时适用于后续UI新增和参考移植。

## 验收与边界

独立完整客户端281源编译通过，0错误、38条既有警告。正式UIRoot新增2313个序列化对象的本地引用与外部GUID核查通过，原3159对象全部保留，原对象只有MainView两项新引用和子节点列表共3字段变化；10项冻结输入SHA保持。

Unity原生PreviewScene中克隆正式设置子树，验证四页切换、系统内容滚动到底、关闭、重新打开默认系统页；缺脚本0。五张1920×1080末图已目检，中文、页签图标与页面布局可显示。画质5、vSync1、目标帧率-1、总音量1保持。末Console读取0错误/警告，当前Boot非Play且场景未脏。

PreviewScene不运行正式Play生命周期，因此只在临时克隆上将原按钮持久监听改为EditorAndRuntime并显式调用原OnEnable，再触发原事件；未修改正式事件或另写业务实现。主城入口由源码、编译和正式序列化接线核实，尚未做正式Play主城点击或打包客户端验收。选框和挂机技能槽为原模板静态外观，不代表真实参数或已装备技能；本轮未接入选项效果。

开发与审查产物统一保存在`_temp/ro-settings-integration-20261020`，正式修改日志：`项目修改日志/2026-10-20-RO-SETTINGS-UI-CHANGELOG.md`。

## 实际实现文件

- `LYF_MMO/Assets/GM_Game/Scripts/UI/Main/Window/SettingPanel.cs`及meta：继承WindowBase，沿RO的SelectOneHandler入口切换四页，重新激活窗口默认展示系统页。
- `LYF_MMO/Assets/GM_Game/Scripts/UI/Main/MainView.cs`：序列化持有窗口和入口按钮，注册WindowType。
- `LYF_MMO/Assets/GM_Game/Scripts/controller/UIController/MainCtrl.cs`：沿既有注册/释放流程监听入口，调用ShowMainWindow。
- `LYF_MMO/Assets/GM_Game/Scripts/Common/EnumDefine.cs`：在原枚举末尾追加SettingPanel，保持已有枚举数值。
- `LYF_MMO/Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab`：展开保存原RO外框、四页和入口，只修改原MainView引用和子节点列表，不改变既有HUD子树。
- `LYF_MMO/Assets/GM_Game/UISprites/Setting`：8张缺失UI纹理及meta；公共图集与字体使用本项目既有资源。5份转换中间预制体归档_temp，不额外作为正式收集资源。

## 迁移中的实际问题

原版虽然已经使用UGUI，但部分原生组件以旧UI DLL的fileID引用，直接复制会缺脚本；Lua绑定同时负责填页签图标、账号样例、推送列表和Toggle初始状态。只有移除Lua而不处理显示初始化，会留下白方块、全选模板和假日期。

本轮将旧UGUI引用映射到当前组件，移除RO Lua组件和原业务监听，不引入兼容框架。保留原静态云装饰并使用默认UI材质，不迁移装饰Shader/Animator。补齐四页签图标、清除账号样例和推送列表模板、隐藏无sprite图层；Toggle序列化isOn为false，隔离渲染仍保留模板勾图外观，不能声称画面已反映实际选中值。设置选项只展示。

独立审计还在转换中间件找到10处已移除Graphic的TargetGraphic残引用；原生组装并序列化到正式Root时已清零。最终交付以正式Root完整引用闭包为准，中间件保留为迁移证据。首轮截图只作问题证据，不能作为清理后的验收图。

## 查看与后续接入

入口已随后续左上菜单任务调整：正常加载新代码和UIRoot后，从主城左上“功能 + → 设置”打开，详见`项目分析/MAIN_FUNCTION_MENU_UI.md`。四页分别为系统、个人、挂机和推送；业务控件当前禁交互，仅分页、关闭及滚动可操作。本轮不构建或发布正式热更资源；打包客户端需沿原流程更新GM_Game和UIRoot资源。

转换用5份prefab及meta已确认不是基线资源、正式Root不依赖其GUID后，移动到`_temp/ro-settings-integration-20261020/client/converted-prefabs`保留；正式8张纹理位于UISprites/Setting，不在归档范围。最终审计`test/reference-audit.json`通过；编译证据`test/final`，交互证据`test/final-preview-result.json`，末图`test/final-*.png`，汇总`_temp/ro-settings-integration-20261020/root-final-validation.json`。
