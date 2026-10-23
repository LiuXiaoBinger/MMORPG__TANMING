# Qwen轻量子Agent与中英文协作记忆

## 需求与范围

- 日期：2026-10-23。任务编号：QWEN-MEM-20261023。
- 用户要求将本地Qwen3 8B用于轻量代码查找、注释、小文件修改等一次可完成的任务，不合格即切豆包或WorkBuddy；协作记忆分英文Agent读取版与中文开发人员版，中文由其他子Agent更新。
- 分支：`codex/qwen-bilingual-memory`，起点 `446ff741867f5579b532b5079ecf1ef84ef99743`。未合入main。
- 不修改游戏业务、资源、场景、协议和网关源码。已有Unity、子目录记忆及其他用户未提交文件不混入本次提交。`Docs/AI/QUERY_TASKS.md` 中此前两次询问记录属于本对话任务上下文，必须保留。

## 设计与优先级

- Qwen通过Ollama回环API调用，模型 `qwen3:8b`，轻量短上下文默认4096、think=false；是外部模型，不冒充Codex原生子Agent或自主工具执行器。调用方提供限定输入，核验结果后由实际文件工具落盘。
- 轻量任务最多一次实际尝试，连接不可用/交付不完整/自检或验收失败即切换；查找交豆包，修改或中文同步交WorkBuddy（必要时豆包）。Qwen不适用两模型各7轮，也不计入两者轮次。复杂工作保留既有分工。
- 五类原名 `.md` 保留中文开发人员版，新增对应 `.en.md` 默认Agent读取版；同类别、主题和编号，按段读取，英文缺证据时才读中文。英文精简事实须保留当前状态、证据、会话轮数、失败与未验边界；历史保留来源定位。
- Codex先维护英文验收事实，再委派其他子Agent按本次差异同步中文；核验同步后收尾，不重复翻译整份历史，不承诺未经实测的token或费用节省比例。
- 初始英文采用精简工作记忆：包含当前规则、必要稳定结构及NPC/网络等已核对续作事实；完整1256段旧历史保留在中文。旧编号缺英文时，其他子Agent按编号补录英文后再供Codex读取。这里不宣称全部历史已翻译，也不把创建1256段英文重复文本当用户需求；这是降低日常读取体量的具体实现。

## 执行与真实身份

- Codex负责约束设计、AGENTS/总配置/流程及现有执行角色入口维护、最后核验和Git集成。
- Qwen实际角色说明草拟一次：252输入/266输出token；将调用方核验落盘误写为模型职责、未明确查找和修改的切换路由，初稿不采纳，不继续返工；无实际文件读写。
- WorkBuddy原派工：Hub `hub-7d83b097-b15a-476e-bcaa-fe419f5ae9da`，attempt `run-3daac65a-b9de-47c1-8c1b-9b0b3a5822b7`，本项目session `5af3d7d9-4c42-406d-a6c7-7a8949f0dcf0`。原生未领取，service实际报告客户端缺网关回环端口；未强停客户端。已取消未执行任务，Hub返回 `cancelled`、`holdsCapacity=false`，无落盘成果，不能计实际开发轮次。
- 豆包接续：本项目官方桌面local会话 `38448830092170754`，返回project `38435310952198914`、目录 `D:/unitypro/LYFMMORGP`。原run `59461341008048386` 仅有content_status=100空白处理中消息，实际nativeTaskCount=0、pending=[]、无文件产物；沿原run等待和恢复仍无进展，已原生停止返回 `stopped=true/status=cancelled`，不计交付核验轮次。随后沿同一会话恢复run `59460346204928002`，保持原允许范围，要求先写Qwen角色小文件再分批保存双语文档，使用长连接等待避免重复派工；尚未交付核验，不能宣称完成首轮开发。授权五类双语记忆及新增Qwen角色文档，禁止改游戏代码或提交推送。
- 执行证据与自检报告集中 `_temp/qwen-bilingual-memory-20261023/`；临时产物不提交、不擅自清理。
- 恢复补充：第二个空白run `59460346204928002` 也已原生停止确认；均不以连接/空白状态计开发交付轮次。沿原会话显式选择CLI实际列出的“豆包 2.1 Pro”、high、no-skills，run `59464168910756610` 完成并实际创建 `AIconfig/agent/qwen_executor.md`，Codex已回读核验该小阶段通过。该文件是豆包真实产物，不归为Qwen成果。随后继续同会话分版任务，仍需按实际文档交付另验。没有证据证明自动模型停滞的根因，不能将切换后小文件成功当作所有历史连接问题已修复。
- 大范围全文迁移run `59461978895284482` 及单文件全历史迁移run `59463976975694850` 未产生文件，均已由原生接口确认停止；不是已交付的失败轮。结合用户减少日常上下文的目标，采用精简英文工作记忆加历史按编号补录，原中文完整保留。Codex实际编写五份英文版本（符合本次“Codex维护英文”职责），不能记为豆包翻译成果。随后在同一豆包Pro会话委派仅五份中文顶部/本次记录同步，不再同时写英文文件。

## 逐项文件清单

- 修改 `AGENTS.md`：英文记忆默认读取、中文同步委派、Qwen轻量职责和一次失败切换，以及旧分工/全中文规则的明确例外。
- 修改 `AIconfig/config.toml`：同步优先调度与双语规则，登记Qwen本地外部模型及默认参数；不伪称运行时自动注册。
- 修改 `AIconfig/workflow.md`：更新轻量任务优先级、两版读写顺序与角色表。
- 修改 `AIconfig/agent/doubao_executor.md`：Qwen失败接收、英文读取及中文同步职责。
- 修改 `AIconfig/agent/workbuddy_executor.md`：Qwen失败接收、英文读取及中文同步职责。
- 新增 `AIconfig/agent/qwen_executor.md`：本地调用、轻量边界、失败切换、中文同步与真实交付要求，由豆包执行。
- 修改 `Docs/AI/PROJECT_CONTEXT.md`：中文开发人员用途、英文入口路由及本任务协作记录，旧历史保留。
- 修改 `Docs/AI/CURRENT_TASK.md`：中文本次文件/交接记录，由外部子Agent更新。
- 修改 `Docs/AI/TEST_REPORT.md`：中文本次文档验收记录及未验证边界，由外部子Agent更新。
- 修改 `Docs/AI/DECISIONS.md`：中文双语分版与轻量分工决策。
- 修改 `Docs/AI/QUERY_TASKS.md`：中文用途及英文入口，保留本对话既有查询记录。
- 新增 `Docs/AI/PROJECT_CONTEXT.en.md`：英文路由、稳定结构与当前协作事实；旧历史按编号补录。
- 新增 `Docs/AI/CURRENT_TASK.en.md`：英文文件任务、状态与交接。
- 新增 `Docs/AI/TEST_REPORT.en.md`：英文验证证据、结果与未验边界。
- 新增 `Docs/AI/DECISIONS.en.md`：英文决策与生效优先级。
- 新增 `Docs/AI/QUERY_TASKS.en.md`：英文查询事实与原文定位。
- 修改 `项目修改日志/README.md`：说明双语记忆与修改日志的职责差异。
- 新增本文件：集中设计、逐项清单、执行证据、核验、未完成项与Git回执。
- 无移动或删除文件；无Unity `.meta` 变化。

## 验证、未解决项与Git

- 豆包中文首次同步run `59469831083523330` 已实际更新五份中文文件。Codex独立运行 `_temp/qwen-bilingual-memory-20261023/verify_memory.py`：exit 0、errors=[]；迁移前1256个中文历史段均保留，允许扩展的DOCS-READ/DOCS-ROUTES之外历史正文无变化；18段已迁移英文所需编号齐全、双语同编号标签一致、无重复编号、英文正文有实质内容。
- 五份英文初始合计31230字节，中文同步后合计2006511字节；基线中文1996598字节。英文保留当前事实与必要续作段，1242个历史段尚未翻译，按编号需要时由子Agent补录；不能以文件体量差异虚构token或费用节省率，也不报告所有历史已翻译。
- Codex沿实际中文原段核对PC-080至PC-084结构、NPC CT-005/TR-005和网络CT-011/TR-011的业务事实、测试数字、外部身份、路径与未验证边界；本轮没有重新核验这些历史游戏行为。此前CT-454/455查询记录保留。
- Python/tomli独立解析总配置，并断言Qwen模型/一次尝试/回退模型/4096上下文/think=false、角色文件存在、五英文文件存在及历史补录规则，全部通过。
- 英文当前任务/验证段补入独立检查事实后，豆包沿原会话增量更新中文CURRENT_TASK/TEST_REPORT同编号段和临时报告；增量run `59464466997844738` 产生实际文件操作后被同会话状态续接取消，状态续接run `59472735577917186` 返回 `completed` 和三个实际文件路径。最终原生stop核对分别为completed/cancelled且stopped=true，无继续写入任务；不把取消轮称为独立完整开发交付。
- Codex已定向回读两份中文同编号段，核对1256历史保留、18英文段、1242旧段按需翻译、首次同步run和未验边界；增量后重跑保留/编号/标签检查exit 0、errors=[]。英文初始体量与最终体量已明确区分，报告算术与首次同步身份已修正。
- 文档/配置任务不运行Unity或服务器构建，不把文档验收当正式联机或游戏功能验证。
- 文件交付、双语同步及文档/配置核验通过，无本任务未解决文档项；历史未全译、实际游戏行为未验仍为明确边界。精确暂存本日志列出的18项文件，格式/清单核对及提交/推送实际回执在下方集中追记。

## Git集成回执

- 精确暂存18项，逐项核对与本日志清单一致，无LYF_MMO等用户改动；`git diff --cached --check` 退出0。
- 首次暂存遇到陈旧 `.git/index.lock`；只读确认无git.exe进程后，删除操作被自动审批拒绝（blocked by policy），未执行删除。改用可恢复的移动，将该锁保存在 `_temp/qwen-bilingual-memory-20261023/stale-index-20261023.lock`，随后暂存成功；无用户文件删除。
- 功能提交 `7f59746`（父基线 `446ff741867f5579b532b5079ecf1ef84ef99743`），包含上述18项文档/配置及验收。`git push -u origin codex/qwen-bilingual-memory` 退出0，远端新分支建立并设置跟踪成功；未合main、未强推。
- 本回执单独作docs补交并推送同分支；该补交仅更新本日志，最终补交号与推送结果由交付回复报告，不循环改写所有记忆入口。

## qwen3:8b-light 增量

- 用户后续要求将轻量模型切换为 `qwen3:8b-light`。更新 `AGENTS.md`、`AIconfig/config.toml`、`AIconfig/workflow.md`、`AIconfig/agent/qwen_executor.md`，并在 PROJECT_CONTEXT、CURRENT_TASK、DECISIONS、TEST_REPORT 的中英文版本新增 `[QWEN-LIGHT-20261023]` 当前约束、任务和验收记录。
- 保留历史 CT-455 对 `qwen3:8b` 的实际测试描述，不改写历史证据；当前调用禁止静默改用 `qwen3:8b` 或 `qwen3:8b-cpu`，指定标签不可用时按一次尝试规则切换。
- 本机 `http://127.0.0.1:11434/api/tags` 返回 `qwen3:8b-light`；Python/tomli 模型、回退、上下文和思考开关断言通过。未运行游戏或服务器构建。
- 分支沿用 `codex/qwen-bilingual-memory`，本次基线 `acc234d9be294a18ab358c9df0f7266d2e3a90b5`；用户已有Unity及其他改动保留，不纳入提交。
- 本增量逐项修改清单（无新增、移动、删除或.meta变化）：
  - `AGENTS.md`：轻量模型改为完整light标签，禁止静默换标签。
  - `AIconfig/config.toml`：更新轻量调度规则和external_agents.qwen.model。
  - `AIconfig/workflow.md`：统一light标签和不可用时回退路径。
  - `AIconfig/agent/qwen_executor.md`：明确API请求必须传light完整模型名。
  - `Docs/AI/PROJECT_CONTEXT.en.md`：更新英文当前模型并新增精确标签约束段。
  - `Docs/AI/PROJECT_CONTEXT.md`：同步中文当前模型和精确标签约束段。
  - `Docs/AI/CURRENT_TASK.en.md`：新增英文增量范围和交接。
  - `Docs/AI/CURRENT_TASK.md`：对应中文任务变化。
  - `Docs/AI/DECISIONS.en.md`：新增英文模型选择决策。
  - `Docs/AI/DECISIONS.md`：对应中文模型选择决策。
  - `Docs/AI/TEST_REPORT.en.md`：记录英文标签可用性和配置断言结果及边界。
  - `Docs/AI/TEST_REPORT.md`：对应中文验收记录。
  - `项目修改日志/2026-10-23-QWEN-BILINGUAL-MEMORY-CHANGELOG.md`：本增量范围、设计、逐项清单与真实验收/Git回执。
- 原保留验证脚本重跑exit 0、errors=[]，旧历史仍完整，新增四对段落标签/编号一致；QUERY_TASKS双语历史没有变更。格式检查通过。
- 中文同步实际尝试：调用 `qwen3:8b-light`、`think=false`、4096上下文，仅输入本次四段英文，90秒HTTP超时，无模型输出和token计数，不宣称Qwen已翻译或落盘。按一次失败切换，不重试Qwen；中文初稿沿豆包原项目local会话 `38448830092170754`、run `59467964748331266` 限定同步，原生返回completed，实际修改中文CURRENT_TASK/TEST_REPORT并核对另外两段，Codex已回读接受；英文补记相同失败与回退身份，中文同步完成。
