# RO 目标选择只读分析记录

- 用户询问RO怎样选中目标，按敌方实体为主要解释并补充地面点区别。
- 修改文件：项目分析/RO_TARGET_SELECTION_FLOW.md、Docs/AI/CURRENT_TASK.md、Docs/AI/TEST_REPORT.md与本日志；未修改业务、参考工程或配置。
- 核对：TouchObject射线/碰撞体UID解析/点击完成、MSkillTargetMgr手动和自动选敌、MSkillTargetDefine可达/敌友/排序、UISkillController按技能筛选与点击普攻、MTargetFxComponent选中效果。
- 验证结论：三种实体目标入口与目标流转已源码核对；地面点由Terrain射线取得。未实测RO输入键位/场景，不把瞄准切换接口直接宣称绑定键盘Tab；未实施本项目移植。
