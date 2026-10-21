# 2026-10-20 左上功能菜单：客户端开发记录

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scripts/UI/Main/MainView.cs`：序列化开关、入口组、提示文字和背包/角色/技能按钮，提供展开收起展示方法，初始化默认收起。
- `LYF_MMO/Assets/GM_Game/Scripts/controller/UIController/MainCtrl.cs`：成对注册/注销菜单及四入口事件，复用B/I/L原窗口和镜头流程，设置沿原ShowMainWindow打开。
- `LYF_MMO/Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab`：左上新增功能开关及横排入口组，将原设置按钮迁入，保留所有既有对象及完整设置窗口。
- `LYF_MMO/Assets/GM_Game/UISprites/MainMenu.meta`、`MainMenu/Menu_Knapsack.png`、`Menu_Role.png`、`Menu_Skill.png`、`Menu_Setting.png`及四份meta：由项目已有图裁出四个功能图案，原图保持。
- `项目分析/MAIN_FUNCTION_MENU_UI.md`：布局、既有UI接入和图标来源说明。

## 设计思路

用户已确认位置为左上。只整合已有四入口和展开收起，不增加框架、动画、持久化、协议或设置业务。正式控件与引用保存在UIRoot，View负责展示，Ctrl沿现有窗口流程响应。截图及正式坐标确认现有角色信息HUD在底部，小地图在右上，因此菜单距左上24参考像素，无需移动旧HUD。

## 验证结果

- 修改前冻结三正式文件全文和SHA：`_temp/main-menu-integration-20261020/client/before`、`before-sha.json`。
- 开发完整281源Rebuild：0错误、38既有警告，输入和日志见 `client/final/compiled-inputs.json`、`build.log`、`build-result.json`。
- 正式Root精确差分：旧5472对象全部保留，7个必要变化，新增58个对象；完整记录 `client/prefab-edit.json`。未通过Unity保存整个Root，避免旧运行程序集丢弃新字段。
- 图标裁切及原图/新图GUID/SHA：`client/icon-sources.json`，预览 `client/cropped-icons.png`。仅新裁图移除红黄边缘杂点，原图未动。
- 原生预览 `client/preview-expanded.png`、`preview-collapsed.png` 同框包含正式菜单、底部HUD及小地图，已目检。预览为隔离克隆手动切换显隐，不能称正式点击验收。
- 原生菜单16个Transform、5个Button、0缺脚本；主城Play、dirty=False、非编译，末Console0错误/警告，临时PreviewScene已关闭。证据 `client/native-audit-result.json`、`console-final.json`。

## 未解决问题与交接

无开发编译阻断。当前Play加载的MainView/MainCtrl均无本轮新方法（原生反射结果False），因此没有真实点击菜单或主城业务窗口验收；不强制刷新、停止用户Play或保存场景。正常重载本轮代码和UIRoot后，由正式入口验证展开/收起与四窗打开、背包/角色镜头、B/I/L快捷键；设置仍仅界面。未构建、发布或替换热更DLL，所有临时证据保留待主Agent确认。

独立审查已通过，无需返工：281源0错误、38既有警告，新增警告签名0；24项检查全真，12正式SHA及1085路径基线限定差异一致，原设置子树2304块保持，引用闭包、四裁图来源与清理检查通过。详见 `项目修改日志/2026-10-20-MAIN-MENU-REVIEW.md`。主验收仍按当前Play旧程序集边界记录，未将隔离预览当成正式业务点击。

专题见 `项目分析/MAIN_FUNCTION_MENU_UI.md`；主验收与四入口协作记录由主Agent统一汇总。
