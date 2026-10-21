# 2026-10-21 豆包与WorkBuddy统一本项目桌面工作任务约束

## 需求与设计

用户要求豆包改走已验证的官方桌面CLI，两者均使用工作模式，项目固定本项目。同步实际执行入口和角色文件，保留旧实验事实作为历史；本次不另行派发任务、不调整执行器源码或模型。

豆包固定项目LYFMMORGP／38435310952198914、runtime local、workspace D:/unitypro/LYFMMORGP，桌面原生agent_mode路径不使用headless专用mode work。WorkBuddy固定同目录原生工作会话，通过现有Hub／桌面网关调用。两者新建和返工明确对应项目，禁止投到普通对话、其他项目或旧隔离目录；直接本地开发与仅补丁交付分别按实际产物验收。

## 逐项文件清单

- 修改 AGENTS.md：替换网页云电脑规则为两者的桌面工作模式、本项目绑定、任务恢复和本地验收规则，更新角色列表。
- 修改 AIconfig/config.toml：豆包改desktop-cli、本地运行环境、固定项目ID及命令；新增WorkBuddy桌面工作执行配置。
- 修改 AIconfig/workflow.md：统一两者派工、项目核对、返工恢复和本地交付流程；加入WorkBuddy角色入口。
- 修改 AIconfig/agent/doubao_executor.md：重写固定桌面CLI入口及参数、原生工作任务、同会话返工和真实文件验收，说明历史实验边界。
- 新增 AIconfig/agent/workbuddy_executor.md：明确Hub／网关、本项目目录工作会话、任务追踪和验收职责。
- 修改 Docs/AI/PROJECT_CONTEXT.md：添加当前桌面模式及本项目绑定约束摘要。
- 修改 Docs/AI/CURRENT_TASK.md：添加本轮任务状态及交接，保留既有历史。
- 修改 Docs/AI/DECISIONS.md：记录用户对旧网页云电脑路径的明确替换及两者统一规则。
- 修改 Docs/AI/TEST_REPORT.md：记录本次规则检查范围及未新增实发测试的边界，保留既有测试历史。
- 修改 项目分析/DOUBAO_CLI_WINDOWS.md：正式任务示例改本项目ID和根目录，新建与返工参数一致；保留project none历史实验，不将其当项目绑定验收。该说明由上一轮创建、此前未提交，本次作为本规则的配套使用说明一并纳入。
- 新增 项目修改日志/2026-10-21-DESKTOP-EXECUTOR-RULES-CHANGELOG.md：本日志。
- 新增 _temp/desktop-executor-rules-20261021：本轮修改前哈希、入口段落和静态验收证据，仅作临时检查文件，不提交。

无移动或删除文件；游戏客户端、服务端、执行器安装与账号配置均不修改。

## 验证与未解决事项

TOML解析、角色引用、两者固定项目／目录／工作模式一致性、旧生效网页约束清除和历史记录保留检查均PASS；证据 _temp/desktop-executor-rules-20261021/static-verification.json。实际导入上游CLI的parseOptions，按正式新建与返工参数各解析一次，均正确得到项目38435310952198914、runtime local、本项目根目录及AskOnRisk，未发网络任务。暂存差异空白检查通过，暂存清单严格等于上述11个正式文件；CURRENT_TASK与TEST_REPORT各39行旧未提交历史保留工作区，未混入本提交。手工暂存初次因入口片段CRLF出现空白检查错误，已只对暂存片段规范LF后通过，工作区历史未删除。纯配置／文档修改，不运行游戏编译，不派发额外模型任务。既有豆包与WorkBuddy临时目录两轮实验仍只证明桌面执行能力，不声称本项目正式开发已验收。

## Git

分支codex/feature-development；仅纳入本轮规则、相关使用说明及日志。Docs/AI两份已有未提交历史以局部暂存保留，不混入其他角色、业务或资源的用户改动。不合main，不强推。

- 规则提交13299fb（11文件）已成功推送origin/codex/feature-development，远端从702001b推进到13299fb。
- 本日志收尾只补记上述真实提交／推送结果，单独提交并推送；收尾提交号通过本文件Git历史及本次最终回复查询，不重复修改业务或重新运行未变化的配置检查。
