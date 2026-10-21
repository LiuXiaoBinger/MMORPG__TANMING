# 2026-10-21 Codex主Agent与WorkBuddy开发子Agent职责约束

## 需求与设计

用户要求明确WorkBuddy可作为开发子Agent修改本地文件，主Agent使用Codex，负责解读需求、设计开发逻辑、发布任务和最后测试；不通过必须返回开发子Agent重新开发。将这一职责分工落实到生效约束、策略和工作流程，保留现有桌面工作模式与本项目绑定。

Codex负责最终测试结论，子Agent先自检；测试失败按原任务反馈实际证据、复现步骤和预期结果，子Agent修复后Codex复测。主Agent继续不代改业务代码；未通过或受阻如实保留未完成状态，不强行报告通过。

## 逐项文件清单

- 修改 AGENTS.md：固定Codex主控及需求解读、逻辑设计、发布任务和最后测试职责；明确WorkBuddy与豆包为外部开发子Agent，可直接修改授权本地文件；加入失败退回原开发子Agent并复测的强制流程。
- 修改 AIconfig/config.toml：增加Codex主控身份与职责列表，更新主Agent边界和失败测试规则，将WorkBuddy角色改为外部开发子Agent并写入自检、最终测试和返工职责。
- 修改 AIconfig/workflow.md：角色表新增Codex主控并明确WorkBuddy开发身份；标准流程改为需求解读、逻辑设计、派发、开发自检、最后测试、失败退回和通过后集成，调整分工表与主Agent边界。
- 修改 AIconfig/agent/workbuddy_executor.md：明确开发子Agent角色，可直接修改授权本地代码、资源、配置与产物；Codex最后测试，失败按原任务重新开发或修复。
- 修改 AIconfig/agent/doubao_executor.md：同步Codex主控职责和开发子Agent自检、失败退回与Codex复测规则。
- 修改 Docs/AI/PROJECT_CONTEXT.md：添加当前主控与开发职责摘要。
- 修改 Docs/AI/CURRENT_TASK.md：记录本轮职责约束修改及交接。
- 修改 Docs/AI/DECISIONS.md：记录用户明确的Codex主控和开发子Agent返工决策。
- 修改 Docs/AI/TEST_REPORT.md：记录配置检查范围和本轮不派发业务任务的验证边界。
- 新增 项目修改日志/2026-10-21-CODEX-DEVELOPMENT-ROLES-CHANGELOG.md：本日志。
- 新增 _temp/codex-development-roles-20261021：修改前文本／哈希、入口片段和静态检查证据，不提交临时文件。

无移动、删除文件，不修改客户端、服务端业务代码或桌面执行器源码；其他角色文件已有用户修改保持原样。

## 检查与边界

TOML解析、主控与开发角色一致性、原任务失败返工流程、角色引用、本项目与桌面工作模式保持、四份协作历史逐字保留、四份无关角色改动保持检查均PASS；证据 _temp/codex-development-roles-20261021/static-verification.json。修改差异空白检查通过，暂存严格限制本轮10个正式文件，CURRENT_TASK及TEST_REPORT先前各39行未提交历史仍留工作区。本轮纯配置／文档修改，不运行游戏编译、不派发开发任务，不将规则写入当作真实业务开发或测试完成。

## Git

分支codex/feature-development，仅提交上述10个正式文件的本轮变化；CURRENT_TASK与TEST_REPORT先前未提交的各39行历史保留工作区，其他角色／业务／资源不暂存。不合main、不强推。

- 规则提交02a5f91已成功推送origin/codex/feature-development，远端由567ea2b推进至02a5f91。
- 本日志收尾仅补记真实提交与推送结果，单独提交推送；收尾提交号通过本文件Git历史与最终回复查询，不修改业务代码。
