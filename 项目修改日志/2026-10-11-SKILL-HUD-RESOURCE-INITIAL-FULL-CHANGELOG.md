# 真气与气力初始化满条

日期：2026-10-11。状态：完成，验证通过。

用户明确两侧资源初始化为满真气、满气力；中央怒气仍未满暗，不扩展资源玩法。

拟仅修改 `LYF_MMO/Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab` 中两资源填充初始值，各完整三分之一圆；开发先核对真实初始化链是否清零。`项目分析/SKILL_HUD_LAYOUT_AND_STYLE.md` 同步当前默认语义，四份协作入口由主维护。

验证采用当前before精确差异、序列化值/初始覆盖链/怒气状态核对及末稿SHA；两数值调整不重跑历史编译/行为测试。证据 `_temp/skill-hud-resource-initial-full-20261011/`，不保存正式场景、不清临时。结果待补。

实际修改：UIRoot的TrueQiFill/StaminaFill两处m_FillAmount由0改为0.33333334，各自填满120度资源槽；`LYF_MMO/Assets/GM_Game/Scripts/UI/Main/Window/RoleCurrInfoWindow.cs`仅纠正真气字段旧注释，执行逻辑无改；分析和四入口同步最新语义。

开发验证PASS，主核精确差异与初始化链：MainView.InitView→InitWindow→EnsureSkillSlots只建技能槽，WindowBase没有清零、RoleInfoCtrl只刷HPMP，两资源接口当前无调用。中央Core ReadyVisual仍inactive，不因真气/气力满而亮怒气；后续调用接口的无效上限仍按原逻辑显示空。

主3末稿＋6初始化输入共9项SHA一致，root-manifest-check.json为PASS。没有重编译、历史行为测试或原生预览，无新增玩法、测试脚本或场景保存，无未解决项。后续接真实资源数据使用现有两接口，临时文件保留。
