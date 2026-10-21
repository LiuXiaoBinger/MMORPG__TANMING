# 2026-10-21 WorkBuddy Windows 网关本机安装记录

## 范围和来源

用户要求将 wuxuanren2008-ux/codex-workbuddy-gateway-windows 拉到E盘并配置，随后要求说明使用方式。安装目录 E:/codex-workbuddy-gateway-windows，来源 main 提交 e68e3bce8c7fd577b93191add90beb74481dc136。没有修改该项目业务源码，也没有修改 Unity 客户端或服务端。

## 文件和系统配置清单

- E:/codex-workbuddy-gateway-windows/：新增完整仓库，保留上游源码、文档、测试与资源。
- E:/codex-workbuddy-gateway-windows/.env：新增私有本机配置；desktop-queue、WorkBuddy程序路径、原生数据目录及Node固定路径。
- E:/codex-workbuddy-gateway-windows/node_modules/：npm ci 安装运行及编译依赖。
- E:/codex-workbuddy-gateway-windows/dist/：npm run build 重新生成上游编译产物；无源码修改，Git numstat未报告文本内容差异，工作区仍可见产物行尾相关状态。
- E:/codex-workbuddy-gateway-windows/data/：程序生成本机任务数据库、Hub连接信息、私有令牌及计划任务监督脚本/日志，保留本机，不上传。
- C:/Users/lyf/.codex/config.toml：新增 agent-hub-windows 和 workbuddy-windows 两个 stdio MCP；其他MCP保留。CLI省略node_repl原有空args=[]，语义不变。修改前备份仅保存在本机_temp，不展示私有内容。
- C:/Users/lyf/.codex/skills/workbuddy-executor/SKILL.md：安装上游任务委派流程。
- C:/Users/lyf/.codex/skills/workbuddy-executor/agents/openai.yaml：安装上游Skill入口信息。
- C:/Users/lyf/.codex/skills/workbuddy-executor/references/setup.md：安装上游连接参考。
- C:/Users/lyf/.codex/skills/workbuddy-executor/references/agent-hub.md：安装上游Hub使用参考。
- Docs/AI/CURRENT_TASK.md：追加本次安装进度、未完成连接条件与交接。
- Docs/AI/TEST_REPORT.md：追加实际编译、测试、健康检查及未验边界。
- 项目修改日志/2026-10-21-WORKBUDDY-GATEWAY-SETUP-CHANGELOG.md：新增本记录。
- _temp/workbuddy-gateway-20261021/：本机配置备份、npm缓存与测试日志；不提交、不清理。
- Windows计划任务 Codex-WorkBuddy-hub-89169d30ae23：新增当前登录用户常驻Hub；监听127.0.0.1:18492。

## 验证

npm ci、npm run build通过。Hub健康接口status=ok/version=0.7.0；MCP启动及agent_hub_providers/workbuddy_list_workspaces调用成功，provider为needs_setup、workers为空。Windows专项95项中92项通过，3项创建符号链接时EPERM。未变更系统权限来绕过失败。第二次测试TEMP采用正斜杠造成启动器路径字符串断言失败，规范为Windows反斜杠后恢复通过；最终证据为_temp/workbuddy-gateway-20261021/windows-tests-final-normalized.log。

## 使用与待办

工作台 http://127.0.0.1:18492/ 可填写目标、已有项目绝对目录、成果形式，提交后查看任务记录、成果、返工及人工验收。也可在Codex指定使用 $workbuddy-executor，经Agent Hub派给WorkBuddy；新聊天加载Skill/MCP。模型及登录使用WorkBuddy自身设置，余额及扣费未接入。

当前未开启18491连接端口，worker服务未安装，未发送任何实际模型任务，不能宣称桌面执行通过。Windows适配器明确要求运行中的WorkBuddy先由用户保存并从托盘退出一次；后续网关再启动并准备真实会话。普通关闭窗口可能只是隐藏。文档docs/WINDOWS.md优先于上游macOS shell命令，Windows应使用node scripts/launch.mjs。

## Git

Unity项目当前功能分支codex/feature-development；本次仅安装外部工具、私有配置及协作记录，没有修改业务代码，未创建代码提交、未推送任何仓库，未合入main。外部仓库仅克隆上游main及构建，没有功能开发或推送。私有.env、data、配置备份与任务记录不上传。

## 真实派工实验及用户决定

- 用户要求实际实验。按 workbuddy-executor Skill，经 Agent Hub 提交隔离目录创建 result.json 的任务 hub-b765b208-e02b-4885-b75a-629724499592，稳定键 gateway-local-smoke-20261021-01；实际工作目录 _temp/workbuddy-gateway-20261021/live-smoke。
- 平台记录成功，随后 failed / DISPATCH_NOT_SENT，holdsCapacity=false，无执行轮次attemptId。错误明确为运行中的WorkBuddy没有网关回环端口，需要保存后从托盘退出一次。当前仅18492监听，18491未监听；未终止进程，未发送到原生模型，未生成成果，未执行同会话返工或验收。
- 用户明确选择“暂时不退出”，故停止依赖桌面退出的连接测试，保留既有桌面与Hub。后续用户方便退出后先检查原预留工作区映射及失败任务状态，不盲目重复创建未知会话；本次已确认任务未发送。
- 本次新增临时smoke-request.json与空测试目录；修改Docs/AI/CURRENT_TASK.md、Docs/AI/TEST_REPORT.md及 项目修改日志/2026-10-21-WORKBUDDY-GATEWAY-SETUP-CHANGELOG.md。未修改业务源码，无代码提交推送。


## 连接恢复与两轮实际验收完成

- 用户确认已退出，网关启动原生WorkBuddy并开启仅回环18491。复核旧预留c6803bb8-cfc6-47f6-b5da-a423695645b7原生不存在后，使用原ID创建；调用使用正斜杠，返回规范化反斜杠导致严格路径比较报错，但只读workbuddy.db确认同ID/同目录已存在，随后ensure复用并标ready，未重复创建。
- 服务标准安装Set-Acl报SeSecurityPrivilege。已核实data原DACL保护开启且只有本用户/SYSTEM/Administrators FullControl，保留原权限，按上游同参数手动登记persistent-service.json及当前用户worker计划任务；未修改网关源码、未提权、未扩大权限。当前serviceRunning=true/desktopRegistered=true。
- 新任务hub-efab5b5e-f3cf-49d9-b8de-039e55980103，旧failed/DISPATCH_NOT_SENT保留不覆盖。第一轮run-a784aef1-91c3-4233-a49a-849d8879f793真实创建result.json（来源WorkBuddy桌面/阶段初稿/数值7），主读取符合后发同会话修改；第二轮run-9cdb1016-1135-422a-8468-6d3ff235773e完成（阶段修改稿/数值11/已核对布尔true）。两轮sessionId相同、runtimeSource均desktop_queue，最终completed/review=accepted/holdsCapacity=false。
- 主仅派工、读文件与验收，没有代写测试成果。最终SHA256=2E2B84FD95C3FDA125C049C0ADF6BB6EBDB8FA34321B59943F42D49F6FF5B291，证据_temp/workbuddy-gateway-20261021/smoke-final-verification.json。服务状态写入曾短暂EPERM、由现有监督脚本自动重启，末次no_task心跳正常；不声称长期稳定、重启登录恢复或复杂业务通过。
- 本轮修改协作两文件、安装日志及项目难点；新增本机服务注册/监督脚本/计划任务、两轮平台结果和_temp测试文件。完整清单见 项目修改日志/2026-10-21-WORKBUDDY-GATEWAY-SETUP-CHANGELOG.md；未修改业务源码，无代码提交推送。后续可在Codex经$workbuddy-executor指定真实业务目录派工，任务记录可在本机工作台查看。


### 本轮逐项配置及产物

- E:/codex-workbuddy-gateway-windows/data/persistent-service.json：新增强类型服务注册数据，绑定已核对的原生测试会话、目录、Node、应用及18491端口。
- E:/codex-workbuddy-gateway-windows/data/service-logs/Codex-WorkBuddy-worker-89169d30ae23.ps1：新增与上游参数一致的隐藏监督脚本；配套stdout.log/stderr.log为运行日志。
- Windows计划任务Codex-WorkBuddy-worker-89169d30ae23：新增当前用户Interactive/Limited唤醒服务；不覆盖Hub任务。
- E:/codex-workbuddy-gateway-windows/data/persistent-service-status.json及data/runs/workspace-workers.sqlite：运行更新服务心跳和测试会话路由状态；不提交。
- E:/codex-workbuddy-gateway-windows/data/runs/run-a784aef1-91c3-4233-a49a-849d8879f793/：程序生成初稿request/state/events/claim/wake；私有凭证不展示。
- E:/codex-workbuddy-gateway-windows/data/runs/run-9cdb1016-1135-422a-8468-6d3ff235773e/：程序生成同会话第二轮记录；不上传。
- E:/codex-workbuddy-gateway-windows/data/platform/results/hub-efab5b5e-f3cf-49d9-b8de-039e55980103/round-1/output.json：WorkBuddy实际写入初稿回报。
- E:/codex-workbuddy-gateway-windows/data/platform/results/hub-efab5b5e-f3cf-49d9-b8de-039e55980103/round-2/output.json：WorkBuddy实际写入修改稿回报。
- _temp/workbuddy-gateway-20261021/live-smoke/result.json：WorkBuddy先创建后修改，Codex仅读取核对。
- _temp/workbuddy-gateway-20261021/inspect-after-exit.json、smoke-connected-request.json、smoke-rework.json、smoke-accept.json、smoke-final-verification.json：本机诊断、任务参数和独立核对证据。
- Docs/AI/CURRENT_TASK.md、Docs/AI/TEST_REPORT.md：更新实际通过结果及边界。
- 项目分析/项目难点.md：追加桌面连接与Windows服务安装故障的事实及面试复盘。
- 项目修改日志/2026-10-21-WORKBUDDY-GATEWAY-SETUP-CHANGELOG.md：追加本次逐项清单、验收与未验边界。

Git仍无业务源码改动，不提交私有配置、数据或临时成果，未提交推送。既有Windows95项测试的3条符号链接权限失败不因本次小文件通过而改写。
