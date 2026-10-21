# 展开功能组底板透明

- 需求：用户追加要求背包、角色、技能、设置所在整组深色背景透明。
- 范围：仅UIRoot.prefab相应Image的颜色alpha归零，保留组件及原有图标、文字、布局、射线和业务；客户端main_menu_client负责正式资源，主只协调验收和记录。
- 设计：沿用既有UI结构，透明显示不删除资源或对象，不新增代码或框架。
- 正式文件：`LYF_MMO/Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab`，组底板及四入口Image共5行alpha归零，RGB及全部其余字节保持；5530对象无增删、5525块保持。Root SHA256 `459b275fb59c32946eff834aaa4df491e2b85edda6ac9f14b354e210a0fbf710`。
- 验证：主独立逐行差分、4冻结输入及展开图目检通过，主汇总4项PASS；原生五底板alpha0、四按钮raycast=true、默认收起、缺脚本0。纯资源不重复编译，不启停Play、保存正式场景或发布。
- 证据：`_temp/main-menu-content-transparent-20261020`；专题`项目分析/MAIN_FUNCTION_MENU_UI.md`同步当前背景表现。
- 验证边界与下一步：当前主城Play、新箭头字段已加载，检查前后dirty=false；预览为临时克隆手动展示，不称正式Play交互或包端验收。重新加载UIRoot查看透明背景，包端沿原资源更新流程。无本轮阻断问题，临时证据保留。
- 交接末态原始响应已主复核，主城Play/非编译/dirty=false、预览残留0、Console0；客户端明细见`项目修改日志/2026-10-20-MAIN-MENU-CONTENT-TRANSPARENT-CLIENT.md`。
