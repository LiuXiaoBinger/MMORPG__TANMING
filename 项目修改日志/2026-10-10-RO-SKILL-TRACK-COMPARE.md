# RO技能时间轴内容核对

- 请求：说明RO技能编辑器除了特效还有哪些内容。
- 只读范围：RO pbl_skill.proto的MSkillData及具体结构、MArtSkill.FireEvents、现有编辑辅助.config，复核既有项目分析/RO_SKILL_EDITOR_REFERENCE.md。
- 结果：可确认动画、音效、位移、转向、预警、震屏、镜头、变色、技能条，以及命中/弹道、召唤、AI事件和Buff数据类别。弹道嵌套ResultList.LongAttackData；组合子技能为CombineSkillList，不能一概叫独立轨道。
- 限制：未找到RO窗口UI源码，只能说明配置/运行支持类别，不能给出真实按钮名称、独立轨道数或布局；后端类别不冒充客户端FireEvents已直接执行。
- 修改文件：项目分析/RO_SKILL_EDITOR_REFERENCE.md补类别与证据边界，Docs/AI/CURRENT_TASK.md和TEST_REPORT.md追加只读结论；本日志。无业务/资产/参考仓库修改，无编译/运行测试。
