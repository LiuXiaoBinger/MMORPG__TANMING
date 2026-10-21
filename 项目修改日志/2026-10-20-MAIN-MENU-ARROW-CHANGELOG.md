# 功能开关使用透明箭头

- 用户需求：截图中的“收起-”文字按钮改为箭头图标，按钮背景透明。
- 最终范围：只改功能开关，默认收起箭头向下、展开向上；透明底板仍保可点击区域，文字提示不再显示。四个RO入口图标/业务、展开组背景及其布局保持。
- 职责：main_menu_client负责正式MainView/UIRoot最小适配；main_menu_review独立审查与编译；主Agent协调、验收和记录，不直接改业务。
- 设计：复用现有RO箭头素材，以RectTransform方向表达原有菜单显隐状态，不添加新框架或动画状态；保留原点击流程。主最初解释曾扩及四入口底板，已在开发动手前撤回，以用户截图开关为准确范围。
- 正式文件：`LYF_MMO/Assets/GM_Game/Scripts/UI/Main/MainView.cs`、`LYF_MMO/Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab`。前者将文字字段换为箭头RectTransform并控制0/180度，后者原Label改RO Image、尺寸28×20、开关alpha0；MainCtrl和其他UI业务不动。协作四入口及`项目分析/MAIN_FUNCTION_MENU_UI.md`同步当前交付。
- 素材：直接复用Common.png的UI_Common_Shouzhanjiantou_03.png，GUID22781455b3685ea4bae5d7ef77d885ea/fileID21300460，PNG/meta与RO逐字一致，无新增/删除正式资源。
- 验证完成：两方完整281源编译均0错误、38既有警告，无新增警告签名；独立21项与主13项全通过。5530对象无增删、5525块保持，四入口/组背景/设置2304对象保持，主11保护输入和两方编译输入SHA保持。两态局部预览目检通过，透明开关原生射线6点命中，正式源缺脚本0；本轮未扩大为正式交互验收。
- 证据：`_temp/main-menu-arrow-20261020`，专题在`项目分析/MAIN_FUNCTION_MENU_UI.md`更新当前开关说明。
- 边界与下一步：当前主城Play仍旧MainView字段，预览手动切换临时克隆，正式新方法/MainCtrl点击与包端未验；正常结束运行并编译、重新加载UIRoot后查看，包端需原热更和资源更新。原生检查前后主城dirty=false，未主动启停Play/保存场景/清标记/发布，临时文件保留。
- 交接末态：Scene_MainCity、play=true、compiling=false、dirty=false、loadedArrowField=false、previewLeftovers=0，Console0；主已核原始状态/Console响应，无临时克隆残留。角色明细见`项目修改日志/2026-10-20-MAIN-MENU-ARROW-CLIENT.md`和`项目修改日志/2026-10-20-MAIN-MENU-ARROW-REVIEW.md`。
- 末稿：MainView SHA256 `0ce28ce80803ab6e0c78a04e59524f0e444a53be8f72e024fc30ceac2b0ae4a4`，Root `71c332e2f2fc6ab9d410926b78f4e05d0f0b5045ea30352860a821d7fc2f80f8`；详见主汇总`_temp/main-menu-arrow-20261020/root-final-validation.json`及独立`review/final-audit.json`。主首次猜测编译子目录不存在，定位实际final目录后核验通过；仅证据读取错误，无业务返工。本轮常规UI小改，不新增面试难点。
