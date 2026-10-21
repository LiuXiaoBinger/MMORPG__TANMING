# 主菜单切换RO图标

- 需求：用户明确改用RO游戏图标。本轮只替左上功能菜单的背包、角色、技能、设置四图标；布局、中文、展开收起和窗口打开流程保持。
- 职责：main_menu_client定位RO真实对应Sprite并修改必要资源；main_menu_review独立审查；主Agent验收协调及记录。
- 方案：优先沿RO正式Main/Lua映射使用本项目已有同源图集，不另画图、不继续用一梦江湖截图裁图，不删除旧资源或修改参考项目。
- 文件范围：正式只改`LYF_MMO/Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab`中四Image的m_Sprite和m_PreserveAspect共8属性；没有尺寸、对象或业务源码变更。
- 验证：完成，独立11项通过；原生导入四Sprite和保比例显示正常、0缺脚本、Console0，主/审查已目检末图。没有业务源码修改，本轮不重复客户端编译。
- 证据：`_temp/ro-main-menu-icons-20261020`；专题在`项目分析/MAIN_FUNCTION_MENU_UI.md`更新当前图标来源。
- 未解决事项：正式运行新菜单点击仍沿上轮边界，不能把本次静态图标预览当玩法/包端测试；不启停Play、保存正式场景、构建或发布。

## 资源来源与最小修改

- 使用现有`LYF_MMO/Assets/artres/Resources/UI/Atlas/CommonIcon.png`，图集及meta与`F:/UnityPro/ro/Ro_cs422nb/clientproj/Assets/artres/Resources/UI/Atlas/CommonIcon.png`及meta逐字一致，GUID为6cc2d0922412f38498c6300249ed9e82。
- 四图为UI_Commonicon_Icon_top_Bag/Role/Skil/Setting.png，fileID依次21300682/21300664/21300668/21300666。角色为头盔、技能为星球样式，保留RO原色、白色Tint、开启保持比例；没有重绘或截图加工。
- RO最新MainButtonTemplate动态图标使用IconMain，本项目没有该图集；本轮准确使用已有RO CommonIcon同系列功能图标，不宣称移植了最新IconMain或整套主界面。
- 原4裁图及meta保留；所有布局、按钮事件、快捷键/镜头、窗口正文、源码及原图集均不改。正式5530对象无增删，5526块逐字保持；1094路径仅Root变化，设置2304对象保持。
- Root末稿SHA acef4917b4a48e432f28c0a80ba75e3c668ecdf502908580a5dd2709ebd0dc4b，主12项无关输入及审查16保护输入保持。证据`_temp/ro-main-menu-icons-20261020/client/final-validation.json`、`review/final-audit.json`与主root-final-validation.json。

## 验收与交接

- 开发原生检查正式菜单16Transform、5Button、缺脚本0；四Sprite精确名、白Tint、preserveAspect=True可读。隔离展开/收起预览与`_temp/ro-main-menu-icons-20261020/client/preview-menu-crop.png`已主/审查目检，无额外布局变化。
- 期间由主城Play变成Boot非Play，新MainView/MainCtrl方法True；本轮没有主动启停或刷新脚本。Boot内存dirty=True缺少前值，不能归因，本轮保留原状不保存或清dirty。Boot磁盘仅记录末态SHA，不能声称有前后配对；主城场景磁盘基线保持。
- 末Console0错误警告，预览清理；未新增正式Play点击/Player测试，未构建/发布。正常加载UIRoot即可查看本轮图标；包端沿原资源更新流程。
- 文档更新：Docs/AI四入口、`项目分析/README.md`及`项目分析/MAIN_FUNCTION_MENU_UI.md`已注明当前RO来源，原裁图信息保留为历史。角色记录见`项目修改日志/2026-10-20-RO-MAIN-MENU-ICONS-CLIENT.md`与`项目修改日志/2026-10-20-RO-MAIN-MENU-ICONS-REVIEW.md`。本轮为常规资源替换，不新增面试难点。
