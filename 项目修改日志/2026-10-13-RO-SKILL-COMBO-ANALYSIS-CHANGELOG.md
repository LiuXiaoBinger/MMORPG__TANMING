# RO MSkillComboTableConfig 只读分析记录

- 用户问题：该类在RO项目中的用途。
- 修改文件：项目分析/RO_SKILL_COMBO_TABLE_CONFIG.md、Docs/AI/CURRENT_TASK.md、Docs/AI/TEST_REPORT.md及本日志；未修改参考项目或本项目业务/配置。
- 核对范围：配置单例、SkillComboTable/FirstComboSkillTable、MNavigationComponent、MSkillComponent、经典技能栏、服务器连招通知与重连状态恢复入口。
- 结论：连招匹配与效果替换查询、起手/接招资格及UI提示；GetComboReplaceSkillID实际返回效果ID，Time使用服务器同步的连招时间配合表内秒窗口。
- 验证：读取函数全体及实际调用处，关键字段/返回值/单位核对完成。只读分析不执行编译、运行测试或移植；未加载实际连招表配置，不将A/B/C示例当真实配置。
