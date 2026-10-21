# 2026-10-20 功能菜单展开组背景透明：客户端记录

用户最新截图要求展开的四入口整组深色背景透明。本次正式资源仅修改 `LYF_MMO/Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab`，将FunctionMenuContent与KnapsackButton、RoleButton、SkillButton、SettingButton五个背景Image的颜色alpha设为0，保留RGB及其余属性，不删除组件。没有修改图标、中文、布局、点击区域、箭头或源码。

修改前冻结全文在 `_temp/main-menu-content-transparent-20261020/client/UIRoot-before.prefab`。精确字节审计确认5530块无增删、5525块保持、变化只有五处m_Color.a；末稿SHA为 `459b275fb59c32946eff834aaa4df491e2b85edda6ac9f14b354e210a0fbf710`。证据 `client/change-details.json`、`final-validation.json`。

Unity仅导入Root，在PreviewScene克隆正式菜单与既有HUD，移除克隆业务组件后渲染一次展开图；局部图 `client/preview-expanded-crop.png` 已目检，原深色块已消失。原生读到五背景alpha均0、四入口射线true、组背景射线false，源默认收起、缺脚本0。当前主城Play，新箭头字段已加载；预览前后主城dirty均false，没有启停Play或保存场景。纯资源改动无需重复业务编译，也没有增加正式点击或打包端验收。

专题 `项目分析/MAIN_FUNCTION_MENU_UI.md` 已更新当前背景说明；主Agent统一协作入口与验收记录，临时资料保留不清理。
