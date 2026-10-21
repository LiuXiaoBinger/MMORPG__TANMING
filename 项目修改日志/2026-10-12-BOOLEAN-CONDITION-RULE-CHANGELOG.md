# 连续布尔判断数量约束

- 用户最后要求只加入约束、不修改代码，覆盖此前检查整个项目并修改的要求；本轮未改业务、资源或执行批量条件重写。
- 修改文件：AGENTS.md、AIconfig/config.toml、AIconfig/workflow.md、AIconfig/agent/unity_client.md、AIconfig/agent/game_server.md、AIconfig/agent/code_reviewer.md，以及Docs/AI/CURRENT_TASK.md、Docs/AI/TEST_REPORT.md与本日志。
- 规则：单个布尔表达式中&&/||连续组合的判断项最多三个，混合与括号合并统计；超出拆为清晰分支或具名条件，保持原求值顺序、短路和业务语义。不用换行规避、不增加多余判断；仅后续新增/修改代码遵守，未经用户另行要求不批量改既有代码。
- 设计思路：项目入口、全局模板、开发和审查职责同步，避免后续角色继续生成长条件链；不改变当前游戏行为。
- 验证：六份约束文本一致性检查及TOML解析；不涉及业务编译或运行测试。无未解决业务问题，后续开发/审查按约束执行。
