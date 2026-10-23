# 项目协作上下文
> 【双语记忆】本文件为开发人员中文版本，完整历史正文在此；Agent 默认读取对应的精简英文版 `Docs/AI/PROJECT_CONTEXT.en.md`。英文为精简工作记忆，旧编号尚未迁移的英文段落，按需由子 Agent 按编号补录（路由见本文件 DOCS-READ/DOCS-ROUTES 与 AGENTS.md）。

> 本轮恢复并保留整理前稳定结构和历史事实，仅增加标签路由。历史进展有日期，不等于现时状态；当前约束以 AGENTS 与最新授权为准。

## [INDEX][协作][DOCS-READ] 按标签读取
- 【双语路由 2026-10-23，QWEN-MEM-20261023】Codex 及其他 Agent 默认读取 `Docs/AI/*.en.md` 英文版，按主题和唯一编号只读单段；本文件为开发人员中文版本，完整历史在本文。旧编号英文缺段时，Codex 委派 Qwen 或其他子 Agent 对该中文段做限定翻译，按同类别/主题/编号补录英文后再读，不整份重译。
- 变更后 Codex 先维护英文事实，再由外部子 Agent 按本次编号差异同步对应中文段，并只对本次新增或修改的段落、字段执行中英一致性检查，核对编号、路径、执行身份、轮数、结果与未验证边界；不全文复核或重译未改动的历史。Qwen 中文同步为一次轻量尝试，失败即按 `AGENTS.md` 回退切换。子 Agent 交付检查范围、差异或问题、修正结果及检查证据；Codex 主 Agent 只核对实际修改范围、差异与该证据，不再自行逐项重复中英检查。检查已通过且相关输入未变时不重复检查，发现问题只补验受影响的增量。

- 所有任务先遵守 `AGENTS.md`；此处是文档路由规则，不替代全局约束。
- 每次任务都要向用户说明本次实际使用的技能名称及其具体工作：开始使用时说明，执行中新增技能时补充，最终交付时汇总实际完成的工作与结果；未使用任何技能时明确写出「本次未使用技能」。仅发现或阅读但未实际应用不算使用，不得把工具调用或子 Agent 的产出冒称为技能成果。要求子 Agent 回报其使用的技能与具体工作，汇总时注明执行者，未披露的情况如实说明。
- 先用 `rg -n '^## .*\[主题\]' Docs/AI/文件名.md` 查看相关标题，将“主题”替换为“导航”“网络”“NPC”“技能”“背包商城”“界面”“协作”“构建”“盘点”“场景实体”“性能”或“技能工具”。同一标题可含多个主题。
- 再按唯一编号定位，例如 `rg -n '^## .*\[CT-027\]' Docs/AI/QUERY_TASKS.md`；命中后仅读该标题至下一个二级标题前的正文。PowerShell 可用 `Get-Content 文件路径 | Select-Object -Skip (起始行-1) -First 行数`。
- 类别：[FILES] 文件修改任务、[VERIFY] 修改验收、[DECISION] 实现/授权决策、[QUERY] 查询问答、[STRUCTURE] 稳定结构、[CONTEXT] 历史上下文、[HANDOFF] 当前交接。
- `[DOCS-20261023]` 是本次整理的当前记录；带 CT/TR/DC/PC 编号的旧段保留原日期和结论，仅用于追溯。历史“运行中/待提交”等不能当作现时状态；轮次、任务标识和未验边界保持原样。决策按最新用户授权与 `AGENTS.md` 判定，不因历史归档失效。
- 只读查询/询问/方案只更新 `QUERY_TASKS.md`；如另有正式文档产物，CURRENT_TASK/TEST_REPORT仅记录文件变化及文档检查，查询正文只留一处。代码/文件修改继续更新两个执行入口与根修改日志。
- 归档仅在需要核对原文时按对应行号读取，不默认加载全文。新任务标题采用 `[类别][主题][唯一任务编号]`，同一任务跨入口沿用编号，不复用本轮 CT/TR/DC/PC 历史编号。

## [STRUCTURE][协作][DOCS-ROUTES] 文件职责与稳定结构入口
- 【双语版本 2026-10-23，QWEN-MEM-20261023】五类记忆各有两版：Agent 默认读 `*.en.md`（精简英文工作记忆），开发人员读原名 `.md`（完整中文历史）。文件职责不变：PROJECT_CONTEXT 稳定上下文与路由、CURRENT_TASK 文件/代码修改与交接、DECISIONS 实现与授权决策、TEST_REPORT 构建检查与验证边界、QUERY_TASKS 查找询问与方案。
- 旧中文段编号（PC/CT/TR/DC 等）继续有效；英文按需按编号补录，不要求一次性全量翻译，也不用英文精简版替代中文历史。

- `Docs/AI/CURRENT_TASK.md`：代码/文件修改状态、范围和交接。
- `Docs/AI/DECISIONS.md`：实现、文件组织及授权决策。
- `Docs/AI/TEST_REPORT.md`：代码/文件变更相关验证。
- `Docs/AI/QUERY_TASKS.md`：查找、询问、参考分析与方案正文。
- 本文件稳定结构入口：`[PC-080]` 项目位置、`[PC-081]` 客户端、`[PC-082]` 服务端、`[PC-083]` 分层/逻辑线程/协议位置、`[PC-084]` 协作约束。只读命中段落，旧约束冲突时遵守顶部规则及 AGENTS。

## [CONTEXT][协作][QWEN-MEM-20261023] 2026-10-23 本地轻量委派与双语记忆（当前）

- 工作分支 `codex/qwen-bilingual-memory`，起点 `446ff741867f5579b532b5079ecf1ef84ef99743`；本任务为文档/配置任务，不含游戏源码、Unity 资源、协议或网关实现；保留全部既有用户改动，不合 main。
- Qwen 为本地 Ollama 外部模型 `qwen3:8b-light`，不是 Codex 注册的原生 Agent；调用必须显式传完整模型名，不得静默改用 `qwen3:8b` 或 `qwen3:8b-cpu`；优先承担限定查找、注释、小文件修改与中文记忆同步等预期一次完成的任务，复杂诊断与实现仍走豆包/WorkBuddy。
- 调用走 `http://127.0.0.1:11434/api/chat`，限定输入，轻量起始上下文 4096、think=false；审批读写与实际文件核对由调用方执行，模型文本不构成工具执行证据，不执行模型自由生成的命令。
- 连接失败、输出不完整或验收不通过时，查找交豆包、修改/同步交 WorkBuddy（必要时豆包）；不反复返工 Qwen，其尝试不消耗两个外部模型的各 7 轮额度；新写入方接手前确认原执行已停止。
- 本次迁移保留完整中文历史、新增精简英文工作记忆，不宣称每个历史标题已翻译；英文由 Codex 维护，中文任务差异由子 Agent 同步，再核对实际文件。
- 真实身份：Qwen 角色说明初稿（252 输入/266 输出 token）因把调用方核验落盘误写成模型职责、未写明查找/修改路由而未采纳，无文件写入；豆包同项目本地会话 `38448830092170754`、角色文件完成 run `59464168910756610`（桌面所列 豆包 2.1 Pro）新增 `AIconfig/agent/qwen_executor.md`，Codex 已回读接受。
- WorkBuddy Hub `hub-7d83b097-b15a-476e-bcaa-fe419f5ae9da` 因原生客户端缺网关端口未领取，取消返回 `cancelled`、`holdsCapacity=false`；连接停滞不计开发轮次。
- 变更清单：`项目修改日志/2026-10-23-QWEN-BILINGUAL-MEMORY-CHANGELOG.md`；临时证据：`_temp/qwen-bilingual-memory-20261023/`；不承诺固定 token/费用节省。

## [CONTEXT][协作][QWEN-LIGHT-20261023] 2026-10-23 Qwen 轻量模型精确约束

- 当前轻量任务统一使用本机 Ollama 模型 `qwen3:8b-light`，调用方必须显式传完整模型名，不得静默替换为 `qwen3:8b` 或 `qwen3:8b-cpu`。
- 2026-10-23 本机 Ollama 标签检查已返回 `qwen3:8b-light`；指定标签不可用时仍按一次尝试规则切豆包或 WorkBuddy，不重复尝试 Qwen，也不无记录换模型。
- 这是当前生效配置约束；历史 CT-455 对 `qwen3:8b` 的测试证据保持原样，不代表当前默认模型。

## [CONTEXT][协作][PC-001] 2026-10-22 当前生效：上下文过长自动新开对话

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 1—5 行；历史时点记录，不代表当前仍在执行。

- 新需求关联较小且上下文较长，或上下文本身已过长影响处理时，按用户长期授权自动新建同项目本地对话，以最小必要摘要交接；保留分支、未提交修改、原任务身份和各7轮计数。
- 逐项文件、验证与Git回执：项目修改日志/2026-10-22-AUTO-NEW-CHAT-CHANGELOG.md。

## [CONTEXT][协作][PC-002] 2026-10-22 当前生效：精简额度流程与各7轮回退

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 6—11 行；历史时点记录，不代表当前仍在执行。

- 同一未解决问题豆包、WorkBuddy各7轮（首次1轮、返工6轮），两者各7轮仍失败才由Codex/GPT独立接手；提前通过停止，用户明确提前接手优先。等待、状态查询、连接失败、自检和Codex核验不计轮，新会话不清零。
- 短上下文、按顺序只读四入口相关段落；开发者自检后一次必要验收，检查通过且输入未变不重复验证，小修复不扩大验证工程，完整过程集中一份日志。新规则优先于下方历史3轮及非固定轮数描述。
- 逐项文件、验证与Git回执：项目修改日志/2026-10-22-QUOTA-SEVEN-ROUNDS-CHANGELOG.md。

## [CONTEXT][网络][PC-003] 2026-10-22 RO网络公共接口：源码与隔离验收完成

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 12—19 行；历史时点记录，不代表当前仍在执行。

- 沿RO真实接口职责改名ServerBase→BaseLink、NetSocketMgr→MNetClient，meta保原GUID；三个bool Send重载与公共OnRecv承接完整消息。客户端普通业务Send、TCP/KCP共用主线程派发；商城仍保捕获连接语义。五实时协议固定KCP，其余及Gate↔Game为TCP，不增框架。
- WorkBuddy桌面实现与审查返工，Codex设计、独立检查、插件集成及Git。原Hub超时后原生审批已按用户新授权自动允许；原轮idle实查和service recover确认停止，Hub cancelled/holdsCapacity=false。沿同原生会话5af3d7d9-4c42-406d-a6c7-7a8949f0dcf0登记限定收尾Hub hub-d6df4730-7ed9-4817-9d11-dc02bc773288，未重做原开发或清零问题轮次。
- Codex独立五服务工程和289客户端源0错误；Socket/KCP122/0、入场8/0、客户端薄边界54/0。正式Network.dll已配套，引用正式插件路径编译289源0错误，插件SHA与最终构建一致。源码输入、33机械迁移语义、meta身份及9用户文件保护通过。
- 不替用户GM_Game.dll.bytes；Unity双号、真实动画/MP、业务自动重连、实体早到、端到端弱网及IL2CPP/HybridCLR发布未验。运行需使用同版网络库与重新构建的热更程序集，不能把保留的旧字节当已部署。
- 逐项文件、验证及提交回执见项目修改日志/2026-10-22-RO-NETWORK-INTERFACE-CHANGELOG.md，接口对照见项目分析/RO_NETWORK_INTERFACE_COMPARISON.md。

## [CONTEXT][协作][PC-004] 2026-10-22 当前授权：任务内权限自动同意

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 20—24 行；历史时点记录，不代表当前仍在执行。

- 用户明确长期授权：本项目已授权任务内的编译、测试、只读查找与允许范围文件操作所触发的工具权限，Agent自动同意，优先Allow/allow_once，不再要求用户在桌面找按钮或重复确认。该用户授权优先于Skill通用手动审批约定；保持原任务和真实审批身份，不扩大任务范围、重复派工或伪造回执。
- 已同步AGENTS、AIconfig总策略/流程及WorkBuddy和豆包角色。当前原生审批单次允许已接受、原请求已消失；命令执行和业务验收结果另核，不将审批成功当作代码完成。逐项文件和Git回执见项目修改日志/2026-10-22-AUTO-PERMISSION-APPROVAL-CHANGELOG.md。

## [CONTEXT][网络][技能][PC-005] 2026-10-22 当前策略：移动与技能固定 KCP

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 25—30 行；历史时点记录，不代表当前仍在执行。

- 用户要求实时移动与技能直接 KCP，取代下方历史绑定前 TCP 回退机制；其他业务与 Gate↔Game 仍 TCP。专用分支 codex/network-kcp-development，不合 main。
- 模型加载前完成绑定，失败走既有断线恢复；入场请求字段 pos=2 带出生坐标，服务器附近过滤前保存，取消入场对首移动跨通道顺序的依赖。不增加全局排序或消息队列。
- 当前最终状态以 CURRENT_TASK、TEST_REPORT 和项目修改日志/2026-10-22-KCP-ONLY-REALTIME-CHANGELOG.md 为准；历史数字不作为本轮验收。正式 Unity 双号及打包仍未验。

## [CONTEXT][网络][PC-006] 2026-10-22 KCP网络第一版源码与隔离验收完成

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 31—36 行；历史时点记录，不代表当前仍在执行。

- 当前网络专用分支codex/network-kcp-development，起点756ca35。Unity↔Gate的11180/11181移动与11170/11171/11173技能走同一可靠有序KCP，其余业务及Gate↔Game仍TCP；没有改移动/伤害结算或服务器权威寻路。
- WorkBuddy桌面3轮初版后由豆包本项目local任务补实证修复、注释与测试收尾。Codex核对实际diff并编译、真实Socket弱网回环与独立同步探针；正式Network.dll已配套，用户原有GM_Game.dll.bytes、字体/布局等保留。
- 运行与发布边界：没有真实Unity双客户端联机、MP扣除链路、跨通道实体早到、Unity自动重连或HybridCLR/IL2CPP打包验收；隔离通过不等于正式联机或上线。最终文件清单、真实结果及Git回执见项目修改日志/2026-10-22-KCP-NETWORK-CHANGELOG.md；实现入口项目分析/KCP_NETWORK_IMPLEMENTATION.md。

## [CONTEXT][协作][PC-007] 2026-10-22 当前进展：WorkBuddy三轮后转豆包限定修复

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 37—42 行；历史时点记录，不代表当前仍在执行。

- WorkBuddy同一KCP问题已实际交付3轮，最新run-17f0279d-aac8-4e1b-a15e-535ee8d45c35已completed、桌面原会话不变，尚未accept。Codex四服务工程及客户端289源编译0错误、真实Socket主套PASSED=78 FAILED=0，但独立探针确定复现两处遗漏，不能当最终验收通过。
- 启用后零实时发送即释放，技能11170错误写TCP14字节；绑定完成与真实TCP Abandoned交错后，Peer字典已空但Session重挂死Peer。独立证据_temp/network-kcp-20261022/review/ROUND3_REVIEW.md；另须改GateKcpCtrl新增三目。
- 已按各3轮规则转豆包第1轮：本项目local会话38448538786202114、run59342789144677378，当前running；限定修复、自检及同会话返工。Codex不代改业务、不重复派工，正式Network.dll尚未替换，业务尚未提交推送。

## [CONTEXT][网络][PC-008] 2026-10-22 当前进展：KCP首轮返工与连接恢复授权

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 43—48 行；历史时点记录，不代表当前仍在执行。

- WorkBuddy桌面首轮实现已落盘但未通过审查；原Hub任务第2轮修KCP线程安全、空闲状态、握手丢包、UDP密钥暴露和旧通道回调隔离，并补专项测试。Codex不代改业务，正式DLL尚未替换，未报告验收或业务上传完成。
- 用户已授权WorkBuddy端口连接异常由Agent自行正常退出/重启并恢复原任务，无需再询问托盘退出；必须先核原执行状态，不重复派工，不强杀未知任务，不覆盖未保存成果。AGENTS与WorkBuddy角色已同步。
- 当前范围与文件清单见CURRENT_TASK及项目修改日志/2026-10-22-KCP-NETWORK-CHANGELOG.md，证据_temp/network-kcp-20261022。

## [CONTEXT][网络][协作][PC-009] 2026-10-22 当前生效：KCP网络专用开发分支

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 49—53 行；历史时点记录，不代表当前仍在执行。

- 用户已批准KCP改造并明确要求创建专用Git分支。本任务使用codex/network-kcp-development，起点756ca35，已推送同名远端并设置上游；该用户新授权优先于历史固定codex/feature-development要求。仍不合main，最终检查后由Codex统一提交推送。
- 第一版实时移动/技能走KCP，其余业务TCP，Gate↔Game不变。WorkBuddy按桌面本项目任务开发，Codex最后核验；事实与进度以CURRENT_TASK、TEST_REPORT及项目修改日志/2026-10-22-KCP-NETWORK-CHANGELOG.md为准。

## [CONTEXT][协作][PC-010] 2026-10-22 历史策略：各3轮（已被顶部各7轮规则取代）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 54—58 行；历史时点记录，不代表当前仍在执行。

- 同一未解决问题豆包3轮、WorkBuddy3轮，各含首次1轮与返工2轮。先满3轮仍失败的模型交另一个在相同范围补充；双方各3轮仍未通过才由Codex独立接手，提前解决即停止。未满不能仅因失败或自报不能解决切GPT。
- 等待、状态查询、连接失败、自检及Codex核验不计轮数，新会话不清零；记录模型、问题、任务/会话/轮次ID、当前轮数、实际结果、失败证据和下一轮要求。工具阻断恢复原任务，不凑次数；用户明确要求提前接手优先。本条取代下方历史“不强求固定轮数”及“路线错误直接独立查找”的描述。

## [CONTEXT][协作][PC-011] 2026-10-22 当前生效：查找先豆包，Codex沿路线核验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 59—65 行；历史时点记录，不代表当前仍在执行。

- 代码查找、参考项目查找与Bug定位默认先派豆包本项目官方桌面只读工作任务，返回精简路线、真实路径行号、关键调用链、证据与未确认项；Bug附实际复现、现象或日志。用户另有明确安排时优先遵从。
- Codex不并行重复完整查找，先沿豆包路线读真实文件、核对证据并验证；路线错误、证据不足或结论不成立时再独立查找并记录纠正，减少额度消耗。豆包自报完成不替代最终核验。
- 查找任务不授权自行修复，开发与验收分工保持；已受理超时恢复原任务，入口不可用记录实际阻断。读取协作入口、维护约束与记录无需再派查找任务。
- 本轮仅同步协作约束，逐项文件、检查和Git结果见 项目修改日志/2026-10-22-DOUBAO-FIRST-SEARCH-RULES-CHANGELOG.md。

## [CONTEXT][协作][PC-012] 2026-10-22 当前生效：豆包、WorkBuddy分工与失败回退

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 66—71 行；历史时点记录，不代表当前仍在执行。

- Codex负责需求拆解、范围和验收；豆包负责只读查找定位，WorkBuddy按Codex核验路线开发和自检，Codex定向复核、测试、记录和Git集成，减少重复读取与上下文消耗。
- 有明确修正方向时沿原任务补充或返工；确认豆包和WorkBuddy无法解决时，Codex停止依赖两者，独立查找、实现或修复业务代码并验证，记录失败原因，不循环重派或凑固定轮数。单次超时先核原任务状态，接手修改前确认原执行者结束或停止。
- 回退到Codex由用户本次明确授权，覆盖常规子Agent委派门槛，但仍遵守任务范围、中文注释、测试、修改日志、功能分支、提交和推送要求。

## [CONTEXT][协作][PC-013] 2026-10-21 当前生效：Codex主控、开发子Agent实现、失败退回复测

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 72—78 行；历史时点记录，不代表当前仍在执行。

- 主Agent固定为Codex，负责解读用户需求、设计开发逻辑、拆分和发布任务、最后测试与验收，并维护协作记录及统一集成；不直接代改客户端或服务端业务代码。
- WorkBuddy明确作为本项目外部开发子Agent，与豆包一样可承担开发、修复和授权范围内的本地代码、资源、配置及产物修改；沿已配置的桌面工作模式，项目固定LYFMMORGP／D:/unitypro/LYFMMORGP，不改网页或其他项目。
- 开发子Agent先自检交付；Codex最后核对实际文件并执行必要测试。未通过时将失败证据、复现步骤、预期结果退回原开发子Agent重新开发或修复，完成后Codex复测；不接手代写业务修复，未通过或受阻不标验收完成。
- 已同步AGENTS、总策略、协作流程及两个桌面开发角色；本次仅写入约束，不派发业务任务。检查、逐项文件与提交推送记录见 项目修改日志/2026-10-21-CODEX-DEVELOPMENT-ROLES-CHANGELOG.md。

## [CONTEXT][协作][PC-014] 2026-10-21 当前生效：豆包与WorkBuddy采用本项目桌面工作任务

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 79—86 行；历史时点记录，不代表当前仍在执行。

- 用户明确替换旧豆包网页调用：豆包只经E:/doubao-cli/doubao-windows.cmd调用官方桌面，工作任务使用local运行环境、LYFMMORGP项目ID38435310952198914及D:/unitypro/LYFMMORGP目录；新建和返工均显式传项目、目录及AskOnRisk权限，不使用网页、headless或project none。
- WorkBuddy按workbuddy-executor Skill走现有Agent Hub／桌面网关，provider=workbuddy，workspace固定D:/unitypro/LYFMMORGP，创建或复用本项目原生工作会话；两者均使用工作模式，不派普通对话、其他项目或历史测试会话。
- 主Agent继续负责委派、测试审查和验收，外部执行者可在授权范围直接改本地文件；仅交付补丁时由本地开发子Agent落盘。保存各自任务／会话编号，返工沿原任务，已受理超时恢复原轮次。
- 已同步AGENTS、AIconfig总策略与流程、豆包角色和新增WorkBuddy角色，以及桌面CLI示例。旧网页云电脑和隔离实验段落保留为历史，本条为当前规则；本次不额外派发开发任务，不声称项目绑定任务已执行验收。
- 本轮验证与逐项文件、提交推送结果见 项目修改日志/2026-10-21-DESKTOP-EXECUTOR-RULES-CHANGELOG.md；不改游戏业务代码或执行器源码，保留已有用户改动。

## [CONTEXT][NPC][协作][PC-015] 2026-10-21 点地测试、NPC初始化与Git规则（当前）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 87—93 行；历史时点记录，不代表当前仍在执行。

- 用户追问A*、NPC不见及无法点地测试。本轮保正式NPC/任务入口，新增仅Editor Play临时点地组件与启停菜单；主城Start补同物体NpcManager.Initialize，实际配置只有4001 QuQu。真实画面仍待正常重载后现场验证。
- 当前功能开发分支codex/feature-development；只有用户明确要求才合main。每次代码交付在回复及项目修改日志列文件和用途，检查后提交本任务代码与日志、推送功能分支，中文提交正文含验证和日志路径；不得混入无关用户修改、不强推，失败如实报告。规则已写AGENTS、AIconfig/config.toml及workflow。
- 本轮6代码/元文件路径，客户端291源和Editor4源双侧0错，独立14审计/26薄边界PASS。导航累计清单与测试步骤见项目分析/导航代码文件清单与测试说明.md。为上传之前未提交的导航，单独暂存导航实现；MMove/MainCity其他跳跃同步改动保留工作区，暂存快照290源0错37警告、Editor4源0错1环境警告。
- 本轮日志项目修改日志/2026-10-21-NAVIGATION-TEST-GIT-RULES-CHANGELOG.md，证据_temp/navigation-test-entry-20261021；提交/推送状态以该日志最后Git记录为准。

## [CONTEXT][协作][PC-016] 项目协作上下文

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 94—95 行；历史时点记录，不代表当前仍在执行。


## [CONTEXT][导航][PC-017] 2026-10-21 导航入口与运行问题修正（当前）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 96—100 行；历史时点记录，不代表当前仍在执行。

- 用户明确正式不需要点地，仅NPC及未来任务触发；已移除地面派发，NPC走近原三维10米后开原对话，默认动作承认导航移动，实际位置消费路点且不跨角直线切障碍。任务仅保显式坐标事件，未实现任务系统。
- 截图同区候选实证大绕路根因为长细三角重心g费用偏差：同高连通2米直路计20.0648米。共享查询改沿RO运行版门户入口位置计价，地图不改；本区域361候选明显绕路234→0，非全图最短或精确点击回放保证。实际6源、双方290源0错38旧警告、主1720冻结1714保持。正式Unity/Player动画碰撞仍待本轮重载实测，最终专项看CURRENT_TASK和TEST_REPORT。

## [CONTEXT][导航][PC-018] 2026-10-21 运行导航已接入，源码/独立专项通过

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 101—107 行；历史时点记录，不代表当前仍在执行。

- 参考RO保MNavigationComponent/MNavigationMgr/MNavigation及路点字段、调用职责；自有公共三角网纯C#查询同源编入Unity/服务端Common，非Detour移植。点地/路点/到达/方向中断接现有碰撞/跳跃/同步，当前无目标技能原语义保持。
- 主城重复XYZ及真实分块误差已处理共线重叠接缝；A*+漏斗/门户高度交点，完整可达才成功。Windows从StreamingAssets/NavigationData读取同份bytes，重导后需同步副本；服务器运行加载/移动权威未接。
- 290源0错38旧警告、Common0错4旧警告，独立2090查询/32导航/10派发链及复跑26移动输入通过；1941冻结1935保持，21正式路径与末稿编译SHA一致。RO6米到达限制保留；MEntity未分发LateUpdate的差异仅导航Update适配。
- 无Unity MCP，新源未确认Unity原生加载，真实主城/Player物理/联机待正常重载后验。不保存场景/启停Play/替插件/发布。事实见CURRENT_TASK、TEST_REPORT与项目分析/RO_NAVIGATION_IMPLEMENTATION.md；证据_temp/navigation-runtime-20261021。

## [CONTEXT][导航][PC-019] 2026-10-21 草地三角缺口回归（当前）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 108—113 行；历史时点记录，不代表当前仍在执行。

- 用户d332截图指出原生覆盖而自绘漏出大块草地。继续修Editor显示，须同时满足贴地范围完整和柱树石遮挡，不能在LessEqual漏面/无界Greater蜘蛛网之间反复切换。
- 开发独占MCP诊断，独立只读冻结；保原数据/地图/烘焙/移动，当前结果看CURRENT_TASK、TEST_REPORT。新证据_temp/navigation-preview-depth-gap-20261021，新日志项目修改日志/2026-10-21-NAVIGATION-DEPTH-GAP-CHANGELOG.md。
- 正式Preview+已有Shader已修有界Terrain片元深度，草地/转镜头/柱树石末图主与独立通过。随后MCP工具撤下且localhost8082连接失败，新增资源最终运行专项/Console未取。用户追加原生网格不显，授权恢复MainCityNavigation激活/隐藏自绘/原生开，但尚未执行；已给手动步骤，不宣称恢复成功。连接恢复优先执行该请求，状态看CURRENT_TASK。

## [CONTEXT][综合][PC-020] 2026-10-20 行走网格可读性修正（当前）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 114—119 行；历史时点记录，不代表当前仍在执行。

- 用户最新be57df参考图要求清楚观察道路可走面、树石柱障碍边界，认为上轮自绘仍不可读。上轮自绘可见不等于本轮可读性达标，当前继续修Editor显示；保留自绘公共文件要求。
- 已实证Greater透线及面不写深度导致后层杂乱；现蓝面写深度、无透画层，精确XYZ边去重后内部淡/轮廓深，专用Editor线Shader消共面遮线。主/独立含柱树石实图通过，XYZ/面/几何边集保真与清理、编译/Shader/Console0通过。UInt64键卡顿最小改Vector2Int，末次本机Show406毫秒。
- 两Editor源+新增shader/meta，427冻结425保持，数据/地图/烘焙/移动不改。最终self-current/hidden-current冻结，native-current无蓝不作参考；用户移动镜头后保其新视角，自绘开启/原生暂false，非Play/dirty原true。具体边界看CURRENT_TASK、TEST_REPORT，证据walkable-readable子目录。

## [CONTEXT][综合][PC-021] 2026-10-20 用户明确参考外观、必须自行绘制（当前任务）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 120—126 行；历史时点记录，不代表当前仍在执行。

- 用户最新纠正：“为什么显示NavMeshSurface，我让你自己照着画”。原生接入已判为需求理解错误，下文原生完成记录仅作历史技术结果。
- 当前已改为反读公共bytes自绘透明可走面及清晰线网格，参考Surface外观；不再使用原生绘制作为交付。仅两Editor源及去多余引用的asmdef变化。实际Scene显隐图主/独立目检、27万顶点/索引/边保真、资源生命周期及真实编译/重载清理通过，Console0；公共数据/烘焙/移动保持。
- 交付主城非Play/dirty原true/近景镜头保持，自绘显示、原生暂false防叠加，隐藏恢复；窗口原场景/烘焙资产选择已恢复。首黑底Gizmo中间层图不作视觉证据，最终CaptureSceneView实图在self-draw。现数据含树冠石顶，不声称已修烘焙或角色可达。
- 当前协作与最终事实看CURRENT_TASK.md、TEST_REPORT.md；专题项目分析/UNITY_NAVIGATION_EXPORT.md，日志项目修改日志/2026-10-20-NAVIGATION-PATHMESH-VIEW-CHANGELOG.md。

## [CONTEXT][导航][PC-022] 2026-10-20 用户指定NavMeshSurface后直接复用原生显示

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 127—132 行；历史时点记录，不代表当前仍在执行。

- 当前以NavMeshSurface原生网格观察为准，移除此前自绘蓝面/Mesh/Material/相机回调。窗口按场景+导航资产匹配活动Surface并选中，开启原生及仅所选显示，结束/关闭/重载恢复原设置；画面明确来自场景烘焙资产，非公共bytes反读，导出和两端格式保持。
- 两Editor源及asmdef必要引用已完成；同镜头手动Surface/窗口正式入口网格边界一致，全部通道差≤2，四组合恢复12项/Close/旧回调0及编译Console0通过。定位用SurfaceEditor同位姿变换sourceBounds（空组件FrameSelected已实测不适用）。
- 用户期间操作镜头不强行恢复旧参考值；主城非Play/dirty原true/123977面，原生和仅所选开启，已选MainCityNavigation。现烘焙仍含部分树冠/石顶，此为RenderMeshes第7层采集内容，未修改烘焙或声称角色可达正确。专题项目分析/UNITY_NAVIGATION_EXPORT.md；日志项目修改日志/2026-10-20-NAVIGATION-PATHMESH-VIEW-CHANGELOG.md。

## [CONTEXT][综合][PC-023] 2026-10-20 主城范围漏显返工已完成

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 133—138 行；历史时点记录，不代表当前仍在执行。

- 用户明确“可走范围不一致”，本轮同主城相机完整Editor绘制四态证实：旧LessEqual裁掉被地表遮住的导航面，另现场原生/自写双开叠浓。末稿改完整烘焙区域透明叠加，Show暂停原生，Hide/关闭/重载恢复；取代下文前轮“仅显示深度可见面”的方案。
- 本轮当前全局网格与文件三数组逐项一致（270735顶点/123977面）及位姿一致，修后主城同镜头覆盖核验通过，中央缺口补齐。编译/Console0、Game排除、true/false原生开关恢复与清理通过，预览已显示，主城非Play/dirty原true/镜头保持。地图与数据未改。
- 用户范围目标已落实，颜色略深/未复制原生线框是保留的外观差异，不将图像覆盖统计当导航正确率；当前位置连通与实际移动仍未接入。专题项目分析/UNITY_NAVIGATION_EXPORT.md；日志项目修改日志/2026-10-20-NAVIGATION-PREVIEW-SCENE-FIX-CHANGELOG.md。

## [CONTEXT][导航][PC-024] 2026-10-20 Scene预览纠正为可行走面

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 139—144 行；历史时点记录，不代表当前仍在执行。

- 用户指出穿透线框不能直观看角色可走区域；现两Editor源改为公共文件UInt32 Mesh的半透明蓝面，Scene相机onPostRender沿用深度，Game相机排除；隐藏/关闭/重载销毁Mesh/Material及回调。取代下文前轮Handles显示选择。
- 文件与原资产三数组逐项一致；真实Unity编译、27万顶点保真、前后遮挡/Game排除、重复显示/清理通过。独立426输入仅两授权源变化，公共文件SHA4659a2c3…3984b84保持。窗口已显示主城填充；主城非Play/原dirty=true保持，未保存场景。
- 资产Agent0规格0.29米半径/2.1米高/45°坡/0.75米爬高；不能断言匹配当前玩家（MJS台阶0.3米），也不是从当前站点计算的连通范围；实际移动未接入。主城最终合成截图未独立目检。专题项目分析/UNITY_NAVIGATION_EXPORT.md；日志项目修改日志/2026-10-20-NAVIGATION-WALKABLE-PREVIEW-CHANGELOG.md。

## [CONTEXT][综合][PC-025] 2026-10-20 公共文件可直接在Scene预览

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 145—150 行；历史时点记录，不代表当前仍在执行。

- 导出窗口新增显示/重新读取、隐藏、定位和滚动；MNavigationMapPreview反读公共文件，缓存世界顶点及32位边索引，通过Handles.DrawLines绘制蓝色三角线框，不注册NavMesh、不创建场景对象/Mesh/Material。查看可留在主城，只有导出仍要求空导航世界。
- 末稿Unity编译/原生270735顶点和743862边端点/重复显示单监听/隐藏关闭清理通过；正式窗口已重新打开并启用预览，主城非Play/dirty=true保持本轮用户基线。原场景/资产/插件/公共文件未改；最终合成截图未独立目检，不把旧受遮挡图片当视觉验收。
- 制作阶段烘焙仍依赖Unity AI Navigation及引擎；自写的是格式、导出、读取与文件预览，不是独立烘焙器。服务端纯.NET读文件不需要Unity，完整算路未实现。说明项目分析/UNITY_NAVIGATION_EXPORT.md；日志项目修改日志/2026-10-20-NAVIGATION-SCENE-PREVIEW-CHANGELOG.md。

## [CONTEXT][综合][PC-026] 2026-10-20 自有公共地图网格导出

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 151—157 行；历史时点记录，不代表当前仍在执行。

- 用户允许采用简单自研方案，现Unity菜单“工具/导航/导出公共地图网格”直接导出已烘焙NavMeshData，主城/后续副本选资产复用，不依赖RO外部工具。正式数据源NavigationData/<SceneName>.navmesh.bytes，主城已实际生成5232487字节、270735顶点/123977面。
- 纯C#三类型在客户端GM_Game、服务端MMoRpgCommon同源编译，Unity及.NET4.8同文件读取验证通过；两方Common0错4旧警告，共享220/独立107、原生双图位姿与清理10项通过，用户原场景/NavMesh/包/既有业务保持。
- 当前只完成网格导出及读取，不含完整A*、邻接/接缝、动态障碍、服务器运行加载或移动接入。工具须Boot/空导航世界，使用资产保存位姿；同名覆盖。未来更新Unity Common插件须统一共享类型归属。
- 使用说明项目分析/UNITY_NAVIGATION_EXPORT.md，日志项目修改日志/2026-10-20-NAVIGATION-EXPORT-TOOL-CHANGELOG.md；本条取代前轮RO工具候选方案，历史分析保留。最终Boot非Play/未脏，未发布。

## [CONTEXT][导航][PC-027] 2026-10-20 RO寻路参考分析

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 158—162 行；历史时点记录，不代表当前仍在执行。

- 用户要求查看RO寻路实现并整理，正文项目分析/RO_PATHFINDING_ANALYSIS.md已按点地、技能追近、Detour A*、路点移动、到达中断、跨图和同步记录源码事实及本项目差异。
- 本项目同名导航组件当前不算路、直接进入施法到达动作；本轮没有实施移植。部分路径/吸附返回值及运行验证边界见正文，勿把参考工程功能当成本项目已实现能力。日志项目修改日志/2026-10-20-RO-PATHFINDING-ANALYSIS-CHANGELOG.md。

## [CONTEXT][综合][PC-028] 2026-10-20 顶部关闭补齐与菜单点击保持展开

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 163—169 行；历史时点记录，不代表当前仍在执行。

- 背包/角色属性/技能3个顶部关闭按钮原持久监听为空，现Root补CloseWindow；背包/角色新增最短override，先隐藏再走原CameraMgr对应WindowAngle关闭分支，技能沿基类。已有对话/商城/设置/服务器列表/注册返回/购买弹窗不重复接线。
- MainCtrl取消四入口自动收起，现点击背包/角色/技能/设置保持菜单展开，仅箭头手动收起，InitView默认收起保留。正式4文件、Root5530对象保留/5527块保持，MainView/CameraMgr不改。
- 两方281源0错38既有警告，独立18项/主11项通过；原生隔离6按钮隐藏/0缺脚本及生产方法专项28项通过。当前Play两新override未加载，镜头/菜单专项为薄边界，正式运行/包端未验；正常编译重载查看。未主动启停Play/保存/发布。
- 日志项目修改日志/2026-10-20-UI-CLOSE-MENU-STAY-CHANGELOG.md，证据_temp/ui-close-menu-stay-20261020；菜单专题项目分析/MAIN_FUNCTION_MENU_UI.md同步当前交互。

## [CONTEXT][综合][PC-029] 2026-10-20 展开功能组底板也已透明

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 170—175 行；历史时点记录，不代表当前仍在执行。

- 用户追加要求整组背包/角色/技能/设置背景透明。仅Root组底板及四入口Image共5行alpha归零，组件/图标/中文/布局/点击保持；此条为当前外观，前轮“组背景保持”是历史范围。
- 主精确差分及四保护输入核验通过，5530对象保留，原生隔离展开图目检、五底板alpha0/缺脚本0通过。纯资源未重复编译，未启停Play/保存场景/发布；重新加载UIRoot查看，正式点击/包端未新增验收。
- 专题项目分析/MAIN_FUNCTION_MENU_UI.md，日志项目修改日志/2026-10-20-MAIN-MENU-CONTENT-TRANSPARENT-CHANGELOG.md，证据_temp/main-menu-content-transparent-20261020。

## [CONTEXT][综合][PC-030] 2026-10-20 功能开关为透明RO箭头

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 176—181 行；历史时点记录，不代表当前仍在执行。

- 用户要求将“功能+/收起-”改为箭头且按钮透明。现有MainView字段换RectTransform并随容器0/180度转向，Root原Label原位改为RO Common收展箭头，开关底板alpha0，108×38点击区保持；四入口与展开组背景不动。
- 两方281源0错38既有警告、独立21项/主13项通过；5530对象保留，5525块与设置2304对象保持。原生隔离两态目检、六点射线命中及0缺脚本通过。
- 当前主城Play仍旧字段，未主动启停或保存，检查前后dirty=false；正常编译并重新加载UIRoot后查看，正式点击与包端未验/未发布。专题项目分析/MAIN_FUNCTION_MENU_UI.md，日志项目修改日志/2026-10-20-MAIN-MENU-ARROW-CHANGELOG.md，证据_temp/main-menu-arrow-20261020。

## [CONTEXT][界面][PC-031] 2026-10-20 主菜单图标现采用RO CommonIcon

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 182—187 行；历史时点记录，不代表当前仍在执行。

- 用户纠正使用RO游戏图标，已将左上四入口换为现有RO CommonIcon的top_Bag/Role/Skil/Setting；图集与F盘RO源PNG/meta逐字一致。取代上一轮一梦江湖风格裁图引用，旧资源保留不删。
- 只改Root四Image的Sprite/preserveAspect共8属性，无对象增删/尺寸或逻辑变化。独立11项通过、5530对象保留、5526块保持、设置2304对象保持，主12/审查16保护输入保持。原生0缺脚本/Console0，末图通过；纯资源无新业务编译。
- 期间已变Boot非Play、新菜单方法True，未主动启停或刷新；Boot内存dirty=True无前值，不保存/清标记或推定原因。未新增正式点击/包端测试或发布。专题项目分析/MAIN_FUNCTION_MENU_UI.md，日志项目修改日志/2026-10-20-RO-MAIN-MENU-ICONS-CHANGELOG.md，证据_temp/ro-main-menu-icons-20261020。

## [CONTEXT][界面][PC-032] 2026-10-20 左上可收缩主菜单

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 188—194 行；历史时点记录，不代表当前仍在执行。

- 用户明确左上位置，现有MainView/MainCtrl接入默认收起的功能菜单，展开背包/角色/技能/设置，点击入口自动收起；背包/角色/技能复用B/I/L窗口及原镜头调用。原设置按钮迁入、设置内容保持，不实现新设置效果。
- 图标取既有ExtractedMenuUi裁出4图，原图保持；只改3既有文件加9资源路径。独立281源0错38旧警告/24项审查通过，原5472对象保留、5465原块逐字保持，设置子树2304对象未改，10关键输入保持。
- 原生源菜单5按钮/0缺脚本；隔离展开/收起图布局通过。当前主城Play加载旧MainView/MainCtrl，新方法False，未停Play或保存场景；正式点击待正常编译重载后验，包端未构建发布。
- 专题项目分析/MAIN_FUNCTION_MENU_UI.md，日志项目修改日志/2026-10-20-MAIN-MENU-CHANGELOG.md，证据_temp/main-menu-integration-20261020。当前入口位置以本条左上菜单为准，替代上轮右上独立设置入口。

## [CONTEXT][界面][PC-033] 2026-10-20 RO设置界面已接入（仅界面）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 195—201 行；历史时点记录，不代表当前仍在执行。

- 用户只要界面，现已展开RO系统/个人/挂机/推送四页到正式UIRoot，沿MainView/WindowType/WindowBase/MainCtrl接入主城设置按钮；不实现任何设置效果或Lua框架。UI设计模式约束同步AGENTS及AIconfig三文件。
- 独立281源0错38既有警告；正式新增2313对象引用核查通过，旧3159对象保留/仅3字段变化、10冻结输入保持。5转换中间prefab/meta归档_temp，不参与正式资源。
- PreviewScene正式子树克隆四页/滚动/关闭/重开及中文末图验收通过，缺脚本0、原画质/帧率/音量参数保持。克隆临时调整监听运行状态并显式OnEnable，不能称正式Play/Player点击验收；选框/挂机槽仍为静态模板。
- 当前Boot非Play且未脏，未主动启停Play/发布；正常重载UIRoot可从主城设置入口查看。专题项目分析/RO_SETTINGS_UI_INTEGRATION.md，日志项目修改日志/2026-10-20-RO-SETTINGS-UI-CHANGELOG.md，证据_temp/ro-settings-integration-20261020。真实业务与包端按后续需求验收。

## [CONTEXT][性能][PC-034] 2026-10-20 主城Editor低帧率的双视图重绘

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 202—207 行；历史时点记录，不代表当前仍在执行。

- 真实MCP原始Profiler中SceneView.Paint/GameView.Paint各约8ms，基准299净帧main均19.927ms；临时Game最大化后300帧SceneView0/main均11.288ms。镜头期间移动，不能当严格FPS收益或正式Player结果。Update脚本阶段约0.407（不含LateUpdate/FixedUpdate）/蒙皮0.187ms，未证为主要瓶颈。
- 活动54skin为三个模型部件、已有1932静态批处理Renderer/675LODGroup，不能凭图47或Saved26推断47角色/未合批。RTX4070、Ultra、vsync1；单次GPU6.094ms仅瞬时数据。
- 窗口/Profiler恢复，场景dirty=false/Console0；无业务资源改动或发布。专题项目分析/MAIN_CITY_FPS_RUNTIME_20261020.md，证据_temp/fps-investigation-20261020，后续固定镜头独立Player采样。

## [CONTEXT][技能][PC-035] 2026-10-20 落地后才移动的动画交接修复

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 208—213 行；历史时点记录，不代表当前仍在执行。

- 落地瞬间无方向进入_isEnding，之后新方向虽开始移动，默认动画被保护早退且刷新标记已消费，原逻辑等待完整落地阶段才恢复。单改MJumpComponent.Update16行，落地中有效移动即清跳跃表现再触发默认；技能优先、空中/静止/声音保持。
- 开发/独立280源0错38/39警告，修前32/修后59项生产方法专项、独立14项审查通过；主283输入SHA一致。动画/碰撞为薄边界，无MCP原生/正式包验收，未更新正式DLL或发布。
- 本轮无协议/AOT改动，需原GM_Game热更/资源更新生效。专题项目分析/LANDING_WALK_ANIMATION_HANDOFF.md，根日志项目修改日志/2026-10-20-LANDING-WALK-SLIDE-CHANGELOG.md，证据_temp/landing-walk-slide-20261020。

## [CONTEXT][网络][构建][PC-036] 2026-10-19 打包主角消失：协议AOT主包需配套

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 214—219 行；历史时点记录，不代表当前仍在执行。

- 新895热更已调用JumpCount，但11:29旧主包Network AOT没有setter；真实Player日志绑定失败并每帧刷MissingMethod。原CreateRole先UpdateMove后EnterWorld，异常catch销毁主角且入场未发，11080技能快照也未取得。
- 原包日志/实际Bundle DLL及方法IL/旧Metadata闭环确认；不是shared或模型缺包。不增加反射/吞错/绕过上报，不复制托管Network去替旧IL2CPP，改为完整配套主包。
- 本轮非Play下临时Windows64 IL2CPP完整主包构建通过，真实BuildReport158.891秒/0错4警告，输出_temp/packaged-character-missing-20261019/player。独立新Metadata四存取匹配原Build缓存895热更两方法引用、7输入SHA保持；未HTTP核当前资源服、未运行登录/发布，原Build/正式资源/服务保持。正式人物与HUD画面待验。专题项目分析/PACKAGED_CHARACTER_MISSING_AOT_PROTOCOL.md。

## [CONTEXT][技能][PC-037] 2026-10-19 远端跳跃动画接入，待配套版本生效

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 220—226 行；历史时点记录，不代表当前仍在执行。

- 原11180/11181只传高度和移动标志，远端没有跳跃事件，所以A上升而B仍播放Walk。现Req jump_count=8/Ntf jump_count=9，0落地、1～3空中段；原网关/逻辑队列/ROMove透传。
- 客户端沿原移动事件接MJumpComponent，段变化即时上传、同段不重播；远端只驱动动画不再积分跳跃，0段等待影子抵达目标才落地。保原移动节拍、技能根位移和本地跳跃。
- 客户端开发/独立280源0错38/39警告，独立修前6/修后37项，服务端回环80项及Network/Game/Gate0错；主527输入SHA一致。当前Play反射仍旧代码，正式双端画面未验，无部署重启/发布/停止Play。
- 专题项目分析/JUMP_ANIMATION_SYNC.md、难点补充案例；日志项目修改日志/2026-10-19-JUMP-ANIMATION-SYNC-CHANGELOG.md。旧JUMP_SYNC_DESIGN.md为扩展设计，不能当本轮已实现服务器跳跃物理。下一步配套更新两个客户端和游戏服后重登验收。

## [CONTEXT][综合][PC-038] 2026-10-19 编辑器IL后处理再次失败与当前恢复

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 227—232 行；历史时点记录，不代表当前仍在执行。

- 本次Unity.Collections/VisualScripting.State.Editor的Jobs IL后处理Trigger退出1，导致Player/HybridCLR AOT构建失败；无CSxxxx业务错误，同段Runner gRPC报request is complete，内部触发原因未定。问题十八追加本轮事实，不能照搬10月13日Burst目标或将compile finish当成功。
- 保存证据后正常重编、真实Player脚本编译_temp成功（typeDB=True/26程序集），Player/Editor Tundra成功、当前Console0错误警告；日志仍有原有警告。无业务/依赖/正式资源修改，无完整AOT或主包验收。
- 记录项目修改日志/2026-10-19-EDITOR-ILPP-DIAGNOSIS-CHANGELOG.md，证据_temp/editor-ilpp-20261019。后续仍需按真实构建结果判断，不称永久修复。

## [CONTEXT][盘点][PC-039] 面试材料补充具体流程与真实故障（2026-10-14）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 233—238 行；历史时点记录，不代表当前仍在执行。

- 项目分析/项目难点.md已补六条具体操作链路、术语解释、18项问题索引及12个追加案例；原六难点和维护模板保留。面试先解释玩家看到的现象，再讲时序、证据、根因和修改，避免只背模块名称。
- 同现象不同根因分别记录：不能施法含时钟停滞/MP耗尽，特效问题含导出计数/Shader变体，编辑器与Player差异含旧热更DLL/AOT主包依赖。未查明耗尽历史、HFS响应、稳态性能及编译器内部起因保持未定。
- 本轮纯文档，历史数据均标隔离/原生/正式只读范围，未重跑业务测试或发布。日志项目修改日志/2026-10-14-PROJECT-INTERVIEW-DETAIL-CHANGELOG.md；校验_temp/project-interview-detail-20261014/verification-result.json。

## [CONTEXT][盘点][PC-040] 面试用难点亮点总览已整理（2026-10-13）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 239—244 行；历史时点记录，不代表当前仍在执行。

- 项目分析/项目难点.md现为面试版统一入口：项目介绍/系统与业务链路、六亮点、六难点、岗位讲述选择、追问、简历表述、改进方向与自查模板。既有面试点总结保持。
- 内容核当前源码与专题最终修订；明确预表现与权威结果、动作与尾段/池化/CD、编辑与正式执行、状态同步与固定Tick的区别。个人贡献按实际参与填写，无压测/FPS/完整伤害/动态AOI或上线结果虚构。
- 本轮纯文档，不新增运行验证/业务改动；日志项目修改日志/2026-10-13-PROJECT-INTERVIEW-SUMMARY-CHANGELOG.md。后续困难问题继续更新该入口。

## [CONTEXT][盘点][PC-041] 项目难点面试复盘入口（2026-10-13）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 245—250 行；历史时点记录，不代表当前仍在执行。

- 用户要求特别难实现的问题必须另行记录。AGENTS.md及AIconfig全局/流程/产品经理职责已要求及时维护项目分析/项目难点.md，未解决先写现状，后续补齐；不只留在对话或修改日志。
- 文档已包含技能退场下沉、远端根运动、Shader变体入包三个真实案例及后续模板，按证据、取舍、验证边界、面试讲述/追问组织，不虚构贡献或收益。既有面试总结保持，后续任务按此持续追加。
- 日志项目修改日志/2026-10-13-PROJECT-DIFFICULTIES-DOC-CHANGELOG.md。本轮仅规则与文档，不修改业务/资源。

## [CONTEXT][技能][PC-042] 技能退场瞬间下沉与MP耗尽（2026-10-13）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 251—257 行；历史时点记录，不代表当前仍在执行。

- 本地/远端所有技能下沉的真实F样本：实体Y稳定，退出时真实Skill过早归还为空占位，脚低于根约0.417米。前轮InState采用RO最新请求语义后，额外释放入口漏适配；唯一MAnimator.TryReleasePendingSkillAnim改读原生采样状态，保过渡保护和InState语义。
- 开发/独立280源零错误、38/39旧警告；10项生产方法专项与独立audit通过。自然退出Play后当前IL是末稿，真实Humanoid隔离请求缝隙/12过渡帧保clip、退出释放PASS，最低脚高0.1100968米/Console0错误警告；未称正式双端全部技能验收。
- 当前不能再施法直接原因为两账号权威MP0/500，客户端默认/空技能/CD0，零费F可释放。服务器20Hz正常、DB500/500、部署IL仅扣MpCost且处理器一次调用，无回蓝；完整消耗序列未还原，不认定一次扣光或增加假设机制。未补MP/改数值/部署/重启。
- 分析项目分析/SKILL_EXIT_CLIP_RELEASE_HEIGHT_DIP.md、项目分析/SKILL_STUCK_SERVER_MP_DIAGNOSIS.md；日志项目修改日志/2026-10-13-SKILL-HEIGHT-DIP-CHANGELOG.md；证据_temp/skill-height-dip-20261013。包须原流程更新热更DLL/资源，回蓝玩法待明确需求。

## [CONTEXT][构建][PC-043] 内置Standard Surface的编译IPC错误状态恢复（2026-10-13）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 258—263 行；历史时点记录，不代表当前仍在执行。

- 用户报0x80000004 Not connected；日志4个Compiler崩溃/协议断连后自动重生、同Shader后续ok1，缓存旧Error继续上报。主单Shader保存旧错误再限定复位与CompilePass，源6passcompiled真/FORWARD可用/消息0，当前控制台0错误；不删Library/不杀正常进程/不改业务材质Shader。
- 用户新正式版本1118已生成，旧1071待更新仅历史。主实际载1118 Fog材质主包与两依赖，MD5匹配，包Shader独立于源，Forward pass2编译并SetPass真/消息0。临时测试包目标programs0不作为字节码通过，实际包材质补验；缓存命中不称全变体重编。
- 正式五输入SHA保持，新增仅_temp证据与根文档。日志增量无新IPC/Crashed，内部进程崩溃触发条件未完全确定，未来新失败按真实日志处理，不加自动重试框架。分析 项目分析/STANDARD_SURFACE_SHADER_COMPILER_IPC.md，日志 项目修改日志/2026-10-13-STANDARD-SURFACE-SHADER-IPC-CHANGELOG.md。正式Player/全量新构建未验。

## [CONTEXT][技能][构建][PC-044] 两技能白矩形/黑轮廓的Shader变体入包修复（2026-10-13）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 264—270 行；历史时点记录，不代表当前仍在执行。

- 用户两图对应Effect_Skill3/Effect_Skill04。shared链及纹理RGBA有效，主要故障是ShaderVariantCollectorSetting保存到Assets根而正式Collector读GM_Game/Shaders旧collection：1071实包17 Shader/20变体，用户新集合77/165未入包。
- 客户端资源职责最小改SavePath＋正式collection/JSON三资源，meta/GUID及根用户新稿保持，不改shared规则/材质/Graph/业务。临时实际43Bundle中collection77/165已原生读取；两技能8采样对与源逐像素一致、白框/黑轮廓消失，48重复控制一致，其余5小差别保留。旧1071未替换/发布，不能据源修复说正式包生效。
- 临时构建仍记录Shader Compiler IPC/Standard Surface编译通信错误；后续HS_Blend_CG包消息0与目标画面通过不等于全构建零错误。模拟清单不可作实际测试依赖闭包，临时夹具改按原生Manifest全部43包，生产依赖没有因此修改。
- 分析 项目分析/SKILL_FX_APPEARANCE_SHADER_VARIANTS.md；日志 项目修改日志/2026-10-13-SKILL-FX-APPEARANCE-CHANGELOG.md；证据 _temp/skill-fx-appearance-20261013/review/native-20261013102847。下一步原正式流程生成资源新版本后客户端现场看两技能，主不保存正式场景/不清临时。

## [CONTEXT][技能][PC-045] 技能导出计数与动作更新空引用修复（2026-10-13）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 271—277 行；历史时点记录，不代表当前仍在执行。

- 用户检查特效打包不全、确认编辑器/包均报错。真实编辑器 MDefaultActionComponent.OnUpdate:296 因布尔重构提前索引未配置脚步表；唯一方法恢复原短路，无新门禁。旧全量等价复审对此方法不成立，当前已更正。
- F加入后Role_MJS正式表现8份，SkillEditorAssetTools历史固定7导致生成导出前/校验末尾失败；现生成不固定数量、校验按正式表，专用BuildVerification同步实际数量。只3源码，资源/Collector/服务器/正式发布保持。
- 独立280源0错39旧警告，before复现/18行为项及真实Unity Animator4项通过；Unity工具编译0错/ValidateAll通过，新临时43Bundle真实读取8Runtime/12FX。现995实际39包加载8Runtime/8动画/12FX、182材质/Shader引用检查通过，无此组漏包实证；不等于最终画面或安装包联机验收。
- 主573清单条目291去重文件SHA一致。用户随后明确移动报错；995实际代码TextAsset与正式Dlls旧字节SHA9D399E04…一致，Mono.Cecil IL实证仍含空脚步表提前访问，Editor与末稿IL已修。打包端Boot反射加载这份旧DLL，须原热更编译/资源更新才能生效；不再等待已有移动错误文本。本轮未替换/发布。分析项目分析/DEFAULT_ACTION_SHORT_CIRCUIT_RUNTIME_FIX.md、SKILL_FX_PACKAGE_RUNTIME_DIAGNOSIS.md，日志项目修改日志/2026-10-13-SKILL-FX-RUNTIME-ACCEPTANCE-CHANGELOG.md，证据_temp/skill-fx-package-runtime-20261013/review/code-version及原验收保留。

## [CONTEXT][技能][PC-046] RO 统一选敌距离校正（2026-10-12）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 278—283 行；历史时点记录，不代表当前仍在执行。

- 用户纠正两倍射程只是经验例子，要求核RO。RO普通技能无目标检索和Tab共用MaxChooseDistance，GlobalTableFight.MaxChooseRange默认18米、副本可Override，与技能UpRange独立；等待阶段失效目标补选才使用UpRange。前轮30为简化值，当前已改18米，只改单源常量/两注释。
- 独立280源0错38旧警告、完整DLL原生28项/审查PASS，18米含边界/18.01拒、五攻击同半径及射程1/100隔离内存证明不乘二，F/轻功保持；主2440输入SHA一致、2073保护无越界。未新增导航追近/目标伤害/协议/表，主城无正式怪物，不称联机或发布。
- 分析项目分析/RO_TARGET_SELECTION_DISTANCE.md，日志项目修改日志/2026-10-12-RO-TARGET-RANGE-CHANGELOG.md，证据_temp/ro-target-range-20261012保留；以下前轮30米为历史。

## [CONTEXT][技能][PC-047] RO 三入口目标选择（2026-10-12）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 284—289 行；历史时点记录，不代表当前仍在执行。

- TouchObject/MSkillTargetMgr/MSkillTargetDefine沿RO命名接现PlayerEntity.Target、统一世界、InputSystem、SkillCtrl与正式HUD：左键选择、瞄准/Tab前四循环、攻击自动选敌；30米、模型层级Collider映射、UI不点穿、失效清理。13源/meta路径，正式资源/服务器保持。
- 怪物/精英/Boss为敌，其他玩家友；六正式技能全TargetMode0，Type1/2仅辅助目标与提交后水平朝向，无敌保原释放；Type3/轻功不参与。不上传客户端UID，不扩目标伤害/刷怪/导航/Buff/自动战斗。
- 独立280源0错38警告、原生完整DLL57＋薄边界回归36/HUD/审查通过；主3792去重输入SHA一致、2067保护无越界。主城无正式怪物，未验正式联机/包或发布；分析项目分析/RO_TARGET_SELECTION_IMPLEMENTATION.md、项目分析/RO_TARGET_SELECTION_SERVER_BOUNDARY.md，日志项目修改日志/2026-10-12-RO-TARGET-SELECTION-CHANGELOG.md，证据_temp/ro-target-selection-20261012保留。

## [CONTEXT][网络][PC-048] RO 移动同步与主城灯光（2026-10-12）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 290—295 行；历史时点记录，不代表当前仍在执行。

- 正式移动链已接PlayerEntity.UpdateMove→11180→GateMoveCtrl→现逻辑队列→MoveOperationHandler→在线ROMoveComponent.OnMoveStart→现位置/附近广播→11181→MainCityMgr→既有远端MMoveComponent.onSyncMoveEvent/LagSyncUpdate；沿RO接口/字段/正常时序，CostTime秒累计，本地不等确认，无额外队列/框架。远端高度同步，未扩跳跃动画/导航高度/车辆/RTT系统。
- 主城仅54实时点光m_Enabled关闭，全部60 Light组件和对象保留、删除0，共享prefab/参数保持；独立原生1活动烘焙方向光/0实时/0活动影。全场景2020活动Renderer/835万三角及一次9036ms加载为实证，不等于可见每帧负载或稳态FPS，Profiler仍待现场。
- 客户端277源0错35警告，Game/Gate0错11/4旧警告，独立80断言与审查通过；主962去重输入SHA一致。共20正式路径，分析项目分析/RO_MOVE_SYNC_CLIENT.md、RO_MOVE_SYNC_SERVER.md、MAIN_CITY_PERFORMANCE.md，日志项目修改日志/2026-10-12-RO-MOVE-CITY-CHANGELOG.md及服务端分日志，证据_temp/ro-move-city-20261012保留。正式联机/包/Profiler未验，未部署重启。

## [CONTEXT][技能][PC-049] RO 客户端与服务器技能组件对照接入（2026-10-12）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 296—302 行；历史时点记录，不代表当前仍在执行。

- 用户确认先完整接入当前技能释放链并列外部缺项。客户端 MSkillComponent 补 RO 同名装配/查询/停止和动作入口，SkillCtrl 真实回包接 OnAttachSkill/OnSkillStart/OnSkillEnd/OnSkillCoreSync；保现核心持有、CD、跳跃、Type3 和 Timer/尾迹。
- 服务器正式在线施法采用全局 ROSkillComponent : RoleComponentBase、RoleSkillComponent : ROSkillComponent，ROSkillCore/ROSkill 为独立强类型；原 OnlineRole 与 SkillCastHandler 实调基类，不另建场景宿主。SceneEntity/SkillComponent 仍是未绑世界的历史占位，不代表正式技能执行。
- 当前无目标释放链的学习/MP/CD/组合段/开始/结束和生命周期完成；完整目标、墙体、QTE、Buff/被动替换、无动画、命中伤害、完整重连/变身等缺外部依赖，不计实现。服务器 CD API 为毫秒、内部固定 Tick，客户端为秒。
- 277 源客户端 0 错/37 旧、服务器及引用 0 错/11 旧，独立 165 项/审查 PASS；主2145去重文件SHA一致、1502关键保护保持。正式联机/现场/包待验。分析 `项目分析/RO_SKILL_COMPONENT_PARITY.md`，日志 `项目修改日志/2026-10-12-RO-SKILL-COMPONENTS-CHANGELOG.md`，证据 `_temp/ro-skill-components-20261012/` 保留。

## [CONTEXT][技能][PC-050] RO 技能发送模块接入（2026-10-12，已完成）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 303—308 行；历史时点记录，不代表当前仍在执行。

- 参考RO接入CastSkill → SetWaitSkill/DoCastSkill → MEvent_Navigation → OnGetEvent/closeTargetAfter → UpdateMove/OnAfterAction，最后保原CanStart/NextSkillUUID/SkillCastReq/BeginPrediction。导航普通组件由Builder创建一次，NavComp从既有容器查询。
- GetTargetFilterSkill 返回空目标，Interrupt/UpdateMove 空体；导航即时交接，不新增等待、确认、队列、重试或目标协议。原 Type3/CanSlideCancel、CD、跳跃限制及权威回包保持。
- 277源编译零错/37旧，独立25专项＋24回归/审查PASS，413项SHA一致；未正式联机/安装包验收，下一步游戏回归。分析 `项目分析/RO_SKILL_SEND_MODULE_FLOW.md`，日志 `项目修改日志/2026-10-12-RO-SKILL-SEND-MODULES-CHANGELOG.md`，证据 `_temp/ro-skill-send-flow-20261012/` 保留。

## [CONTEXT][技能][PC-051] F持续状态拒绝的服务器计时故障（2026-10-12）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 309—314 行；历史时点记录，不代表当前仍在执行。

- 真实编辑器连续F发包，两端部署特殊技能代码正确；被动读实际服务器角色1全部就绪、在线、HP1000/职业1，首F已接受UUID1/10080截止Tick22，但相隔16秒LogicTick恒10。高速忙循环每次整毫秒截断后覆盖采样原点，丢光亚毫秒余量导致技能/CD不结束。
- 唯一GameGlobal.Update原点仅前进已计入整毫秒，保留刻度余量，一行逻辑加四中文注释，不改技能门禁/背包/客户端或增加框架。服务端Rebuild零错11旧、独立审查PASS；真实Update before1200ms tick1/活动1→after tick25/活动0，667ms/500ms自然到期再次F成功，7项通过，主233/1/58/38/36输入SHA一致。
- 正式GameServer未部署/重启，需重新编译重启后游戏回归；隔离真实业务但无网关连接，不称正式联机。分析`项目分析/SERVER_LOGIC_CLOCK_MS_REMAINDER.md`，日志`项目修改日志/2026-10-12-ROLL-STATEBLOCKED-FIX-CHANGELOG.md`，证据`_temp/roll-stateblocked-20261012/`。保Type3/CanSlideCancel特殊技能与所有前次成果，不清临时。

## [CONTEXT][技能][PC-052] F恢复特殊技能（2026-10-12，当前需求覆盖本地移动）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 315—319 行；历史时点记录，不代表当前仍在执行。

- 用户最新要求仍为特殊技能，前次7源本地接入已定向撤销；恢复请求、预测、CurrentSkill及服务器同步，SceneMgr授权拼写修复保持。仅客户端CanSkill/服务器TryCast按新Type3→旧CanSlideCancel==1特殊取消；普通InteruptSkill及学习/职业/MP/CD/跳跃保持，服务器类别3准入替代10080硬编码。不改表、状态或框架。
- 两源新逻辑开发与独立审查PASS：客户端272编译0错37旧，后端及引用Rebuild0错11旧，客户端63源24专项/真实后端63专项通过，主272/233/1/29/63/42输入SHA一致。客户端Unity/网络/状态机薄边界，后端无网关，未正式联机/包验收或发布；下一步正式游戏重新运行。前次无网络55项不计当前成果；日志`项目修改日志/2026-10-12-ROLL-SPECIAL-SKILL-CHANGELOG.md`，分析`项目分析/F_ROLL_SLIDE_DESIGN.md`，证据`_temp/roll-special-skill-20261012/`。以下本地移动记录为历史且已撤销。

## [CONTEXT][技能][PC-053] F滑步本地移动（2026-10-12，覆盖历史普通技能请求）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 320—324 行；历史时点记录，不代表当前仍在执行。

- 用户明确F作为移动动作、不走施法请求；现SkillCtrl按Type3调用MMoveComponent.TrySlide，独立SlideAction复用原MArtSkill/Timer残影/根Collector，不入CurrentSkill、不建预测、不发SkillCastReq、不等回包、不扣MP。原0.5秒CD复用现记录，约667ms动作自然归池；约5米配置、F最右、2攻击保持，W/Shift不叠跑速且结束恢复移动，普通技能网络与跳跃限制保持。
- 7必要业务适配；用户授权SceneMgr字段拼写另补一字符。272编译零错37旧、独立64真实源55断言/审查PASS，主272/8/64/42输入SHA一致；无新增类型/状态/协议/表/资产/服务器改动或气力/无敌玩法。薄Unity/网络/状态机边界，未正式现场或发布；下一步重新运行游戏，包更新配套热更代码。分析`项目分析/F_ROLL_SLIDE_DESIGN.md`，日志`项目修改日志/2026-10-12-ROLL-LOCAL-MOVE-CHANGELOG.md`，证据`_temp/roll-local-move-20261012/`。

## [CONTEXT][技能][PC-054] 主城输入恢复（2026-10-12）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 325—329 行；历史时点记录，不代表当前仍在执行。

- PlayerEntity在创角缓存的相机随Single销毁，GetMainCamera现失效时用原缓存方法重取主城相机。主界面先建/输入模型晚到且旧MainRoleCtrl禁用，MainCityMgr模型完成后显式调用原RegisterMainUIKeyHandler补齐技能和UI订阅。只有两源最小修复，不改旧Controller禁态/SceneMgr/输入框架。
- 272源编译0错37既有警告、独立真实InputSystem/Unity27项通过；生产相机/注册方法抽取、角色移动桥薄边界，未完整正式现场或发布。日志`项目修改日志/2026-10-12-ENTER-GAME-MOVE-FIX-CHANGELOG.md`，证据`_temp/enter-game-move-20261012/`。

## [CONTEXT][场景实体][PC-055] 场景管理恢复原简单流程（2026-10-12，覆盖历史复杂调度方案）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 330—334 行；历史时点记录，不代表当前仍在执行。

- 用户要求删除无用校验；真实安装包主城被“SceneBusinessRoot未默认禁用”误拒并卸载，引起创角背景残留及小地图空引用。当前仅SceneMgr恢复原三个字段、Init/LoadSceneMode、YooAsset Single及EveryUpdate完成回调，不改正式场景来迎合旧门禁。
- 末稿80行、272源Rebuild零错37既有警告，独立原生28项通过，主核272源/268依赖/5原生输入SHA一致。真实SceneMgr/UniRx运行，外围业务/UI与Yoo为隔离边界；未正式联机/包验收或发布。规则已写入AGENTS.md及AIconfig，无需求或已复现故障依据不得增加防御包装。分析`项目分析/SCENE_SIMPLE_LOGIN_FLOW.md`，日志`项目修改日志/2026-10-12-SCENE-SIMPLE-FLOW-CHANGELOG.md`，证据`_temp/scene-simple-20261012/`。

## [CONTEXT][场景实体][PC-056] 创角场景活动状态误判修复（2026-10-12）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 335—339 行；历史时点记录，不代表当前仍在执行。

- SceneMgr.CommitLatestRequest先判断目标是否已活动，未活动才调用YooAsset.ActivateScene。Unity卸载旧场景后自动活动目标，重复SetActive返回false；独立原生PlayMode已确认，修复防止创角业务根被误报失败阻断。
- 唯一业务方法最小修改，272源编译零错/37旧、独立审查通过，主272SHA一致；未正式登录回归或发布。分析`项目分析/SCENE_ACTIVE_COMMIT_FIX.md`，日志`项目修改日志/2026-10-12-SCENE-ACTIVE-COMMIT-FIX-CHANGELOG.md`。

## [CONTEXT][技能][PC-057] 残影按Excel技能类别（2026-10-12）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 340—344 行；历史时点记录，不代表当前仍在执行。

- MArtSkill.FireEvents使用真实主表cfg.SkillInfo.Type==3滑行登记残影，替代10080硬编码；MSkill.SkillType为Art等逻辑身份不混用。一次注册读取已有LubanMgr，无新Core字段/签名/框架，表和身份绑定保持。
- 唯一源码精确条件适配，272编译0错/14类别专项通过，主字节/编译输入SHA审计一致；10081/Type3有影、10080/Type2无影、正式滑行原参数保持。日志 `项目修改日志/2026-10-12-SLIDE-CATEGORY-CHANGELOG.md`，分析 `项目分析/F_ROLL_SLIDE_DESIGN.md`，证据 `_temp/slide-category-20261012/`；薄Unity/网络未现场。

## [CONTEXT][界面][PC-058] 翻滚最终位置为最右（2026-10-12，覆盖最左布局）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 345—349 行；历史时点记录，不代表当前仍在执行。

- 用户纠正位置为整个技能栏最右：左Q/E/1/2/空｜R｜右3/4/5/6/F。唯一UIRoot七个X含空槽同级Inset关联位置，其余字节保持，F输入/原攻击2、约5米/白残影/CD0.5及跳跃限制保持。
- 首漏Inset遮挡图已返工，原生全图/局部主目检通过，主18项SHA一致；纯布局不重编业务/重跑旧专项。日志 `项目修改日志/2026-10-12-F-ROLL-RIGHT-CHANGELOG.md`，证据 `_temp/f-roll-right-20261012/`，分析 `项目分析/F_ROLL_SLIDE_DESIGN.md`；未正式现场/发布，临时保留。

## [CONTEXT][技能][PC-059] 翻滚最新参数与最左栏（2026-10-12，覆盖首轮6秒/无特效）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 350—356 行；历史时点记录，不代表当前仍在执行。

- 用户确认每次完整滑步约5米；3008001作者/Runtime根倍率1.458485，保80/120秒唯一窗口与空Charge，独立原生单Skill状态累计约5.00米。F移最左，左槽F/Q/E/1/2，原攻击仍2，其他HUD保持。
- 10080主CD0.5秒/效果500ms，动作仍667ms、零MP无伤害，动作结束可再按F；不实现按住自动循环或输入缓存，跳跃门禁保持。两源表/两bytes正常生成一致，三次权威连续17项已通过。
- 原特效是旧GhostEffect白色网格残影，以MArtSkill.FireEvents→MTimerMgr→InnerProcessTimer约30Hz恢复，原四0.2参数/已出影自然淡出；没有新编辑轨道/组件类型/协议。真实失活再启缓存空网格复现后仅置两缓存null修复，GraphicsSettings只追加原Ghost Shader引用。
- 末稿272源编译/真实作者导出及原生HUD/残影通过，独立63源34专项及17权威连续通过，主748项SHA一致；6554保护6544保持/10授权改＋Graphics单独精确比对，11正式路径零新增。分析 `项目分析/F_ROLL_SLIDE_DESIGN.md`，日志 `项目修改日志/2026-10-12-F-ROLL-REFINE-CHANGELOG.md`，证据 `_temp/f-roll-refine-20261012/`。未正式现场联机或发布，保既有成果及临时。

## [CONTEXT][技能][PC-060] F滑步与原F移至2（2026-10-12）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 357—362 行；历史时点记录，不代表当前仍在执行。

- 复用原10080滑行、现成Anim_JX_Slider_F和Skill_330600图标，补Effect2008001/Presentation3008001及作者/绑定/Runtime。667ms动作、CD6秒、零耗蓝无伤害，唯一完整Animation根窗口Scale1/Charge空，沿当前朝向与既有Collector→MMove→实体碰撞。
- 现专用模拟11080将10060原F改2、10080改F；中心服仅规范已有10080/F至少1级和10060旧F→2回包，不写活DB。新角色原Type3自动学习/F保持，游戏服只放行已接入10080/Type3。无无敌/气力玩法/新协议或组件，跳跃门禁保持。
- 三源xlsx和三共享Luban bytes同步；导出工具只额外允许指定动画精确路径。客户端/两服务编译及真实作者导出通过，独立24专项/19权威/7只读回包通过，主1010项SHA一致，旧资源/场景/注释保持。分析 `项目分析/F_ROLL_SLIDE_DESIGN.md`，日志 `项目修改日志/2026-10-12-F-ROLL-SLIDE-CHANGELOG.md`，证据 `_temp/f-roll-slide-20261012/`；墙体/网络为薄测试边界，未正式现场联机或发布Bundle。

## [CONTEXT][技能][PC-061] 跳跃禁止技能与普攻（2026-10-12）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 363—368 行；历史时点记录，不代表当前仍在执行。

- 统一MSkillComponent.CanStart复用Entity.IsJump，起跳当帧至接地EndJump禁止新的普攻/技能请求和直接预测；多段跳/下降同限制，落地按原CD/MP恢复，不缓存空中输入，不改已存在技能收尾。
- 唯一业务源码改动，272编译0错37旧、开发27/独立93本轮专项通过，before复现空中Q仍施法；独立6545保护仅一授权改、主418输入SHA一致。薄Unity/网络夹具，未现场/联机；服务端暂无真实跳跃状态，不扩协议。
- 分析 `项目分析/JUMP_SKILL_CAST_GATE.md`，日志 `项目修改日志/2026-10-12-JUMP-CAST-BLOCK-CHANGELOG.md`，证据 `_temp/jump-cast-block-20261012/`。全部既有HUD/场景/注释修改保留。

## [CONTEXT][界面][场景实体][PC-062] 气力场景覆盖修复（2026-10-12）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 369—373 行；历史时点记录，不代表当前仍在执行。

- 前次只核Prefab漏查New Scene.unity的UIRoot实例气力0覆盖，解释真气满而气力空；现精准移除4行覆盖，让气力继承Prefab满值。全部Prefab/源码及其他注释保持，场景其余字节保持。
- 正式Prefab默认原生图双满暗怒；独立真实场景覆盖解析after双1/3及保护通过，主19项SHA一致。场景变体临时预览失败未计通过，正式现场未验。日志 `项目修改日志/2026-10-12-SKILL-HUD-STAMINA-FULL-FIX-CHANGELOG.md`，证据 `_temp/skill-hud-stamina-full-fix-20261012/`，分析已同步。

## [CONTEXT][界面][PC-063] 真气与气力初始满条（2026-10-11，覆盖历史默认空）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 374—378 行；历史时点记录，不代表当前仍在执行。

- 用户明确初始化满真气、满气力；UIRoot两资源填充值为0.33333334，各完整120度槽。怒气默认未满暗保持，未新增玩法。
- 仅两序列化值、一条旧注释与分析同步，初始化链不会清零；精确差异/默认状态验证PASS，主9项SHA一致，无业务执行逻辑改动，不重跑历史编译/行为/原生预览。日志 `项目修改日志/2026-10-11-SKILL-HUD-RESOURCE-INITIAL-FULL-CHANGELOG.md`，证据 `_temp/skill-hud-resource-initial-full-20261011/`。

## [CONTEXT][界面][PC-064] 中央圆盘嵌槽视觉（2026-10-11，覆盖盘外弧稿）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 379—384 行；历史时点记录，不代表当前仍在执行。

- 最新局部参考已落实为暗灰宽倒角硬框、向上五剑浮雕、下R；左右真气/气力缩至144单位直径并嵌入154单位外壳窄槽。前轮悬浮外弧不再是当前视觉。
- 仅两Prefab与三新PNG/meta，全部业务源码/三显示接口、侧槽和底HPMP保持；正式无数据默认暗空，玩法仍未接入。首同心细线稿打回后返工通过，主/独立三态目检、原生11图、五比例几何/引用/保护通过，主148项SHA一致。无源码改，不重计旧272/24。
- 当前分析 `项目分析/SKILL_HUD_LAYOUT_AND_STYLE.md`，日志 `项目修改日志/2026-10-11-SKILL-HUD-CENTER-INSET-CHANGELOG.md`，证据 `_temp/skill-hud-center-inset-20261011/`；未验正式现场或发布Bundle，临时保留。

## [CONTEXT][界面][PC-065] 中央真气/气力弧条（2026-10-11，最新）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 385—390 行；历史时点记录，不代表当前仍在执行。

- 用户已澄清中央圆盘两侧另加弧条，底HPMP长条保持。左青蓝真气/右浅金气力外弧，中央满怒内环独立；只界面无玩法来源，默认空，不显示数值文字。
- RoleCurrInfoWindow.UpdateTrueQi/UpdateStamina仅各自更新Image，max<=0为空、负值/超上限裁剪，三分之一圆范围。UIRoot局部四Image配专用616px透明细环PNG/meta，不扩业务类型/协议/积累消耗/施法门禁。
- 四路径，两源资源旧改/两新资产；272源0错37旧、24真实方法断言、689SHA一致、6474保护仅两授权改/两新，18原生图空/部分/满怒组合与五比例通过。分析 `项目分析/SKILL_HUD_LAYOUT_AND_STYLE.md`，日志 `项目修改日志/2026-10-11-SKILL-HUD-QI-STAMINA-CHANGELOG.md`，证据 `_temp/skill-hud-qi-stamina-20261011/`；未验真实资源玩法/现场或Bundle。

## [CONTEXT][技能][界面][PC-066] 技能 HUD 去信息与怒气暗态（2026-10-11，覆盖前稿视觉）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 391—396 行；历史时点记录，不代表当前仍在执行。

- 用户认为上一版太丑：当前技能槽灰黑透明/细蓝白边，头像昵称职业及生命法力文字数值已去掉，原HPMP条保持；键/CD改引用项目已有LiberationSans。中央默认中性暗盘，满态才青蓝细环和技能图标，不常亮。
- 用户已确认先改界面，怒气玩法以后接入；项目无真实怒气源。本轮UpdateRage/SetRageFull仅界面状态，当前无调用默认暗，不用CD/MP替代、不加协议/积攒消耗/施法门禁，原R输入和CD仍原行为。
- 两源码三Prefab，272源零错37旧、独立29真实方法断言、685项SHA一致、6474保护仅五授权修改；暗满五比例与CD原生图通过。分析 `项目分析/SKILL_HUD_LAYOUT_AND_STYLE.md` 已重写，日志 `项目修改日志/2026-10-11-SKILL-HUD-REFINE-CHANGELOG.md`，证据 `_temp/skill-hud-refine-20261011/`；未真实服务器/现场施法或发布Bundle。

## [CONTEXT][技能][背包商城][界面][PC-067] 技能 HUD 布局与背包蓝白视觉（2026-10-11）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 397—402 行；历史时点记录，不代表当前仍在执行。

- 底部左右五视觉格、中央原R圆槽、下HP/MP，原头像昵称职业上方；左Q/E/F/1/2、右3/4/5/6＋纯空格，十固定键和真实技能/CD/角色数据保持。背包Common1图集与原字体复用，不复制参考游戏素材。
- RoleCurrInfoWindow仅R选SkillCoreWidget样式及同名锚点挂接；两旧Prefab调整、新中央Prefab/meta仍同SkillSlotWidget类型，未新增管理层/点击系统/药品。原CollectAll寻址适用，安装包需同步热更代码和资源，编辑器原模式可用。
- 开发/独立272源零错37旧、两报告PASS，主568项SHA一致，五比例原生隔离预览及CD外观通过。真实服务器登录/正式场景施法未验，不称现场行为回归。分析 `项目分析/SKILL_HUD_LAYOUT_AND_STYLE.md`，日志 `项目修改日志/2026-10-11-SKILL-HUD-LAYOUT-CHANGELOG.md`，证据 `_temp/skill-hud-layout-20261011/`。

## [CONTEXT][技能][PC-068] 技能 CD 与表现实例回收独立（2026-10-11，最新修正）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 403—409 行；历史时点记录，不代表当前仍在执行。

- 本条覆盖下文历史Cease调用Core.Reload清CD方案。独立已复现十秒CD/半秒表现：无回包正常收尾后0.61秒CD被清0、第二次输入发包。两源最小删除Cease→Reload联动及唯一无其他调用Reload入口；表现结束/归池不清角色CD，施放/倒计时/权威同步和明确清场维护原CD。
- 不等服务器确认不等于允许跳过CD；保原简单RO动作OnUpdate/列表倒序Update、MTimerMgr及自然声尾/位移职责，不增加字段、确认门禁、快照或管理层。
- 当前正式效果xlsx与真实生成读取器解析.bytes同值：10010/10040/10050/10060/10070分别0.5/8/10/9/13秒，组全0；当前表无零CD行，零CD仅接口合法条件。日志 `项目修改日志/2026-10-11-SKILL-CD-EARLY-RESET-CHANGELOG.md`，分析 `项目分析/RO_SKILL_CD_LIFETIME.md`，本轮终验由TEST_REPORT记录。
- 独立298运行/272源零错37旧、两报告审计PASS，主608输入SHA一致；2799保护仅两源码修改/2797保持。正常回池0.61秒仍9.39秒CD，20次输入被阻断、到期释放通过，现场Unity/网络仍未验。

## [CONTEXT][协作][PC-069] 参考移植约束与 RO 更新精简（2026-10-11，最新）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 410—416 行；历史时点记录，不代表当前仍在执行。

- AGENTS.md 与 AIconfig/workflow.md 已加入用户要求：参考指定项目优先沿原代码结构/接口/职责/时序实现，仅作工程必要接入；不自加需求外功能、管理层、快照/代次包装等防御机制，扩展由用户提出后再做，审核不为人造场景扩生产流程。
- 此条覆盖历史技能常驻统一更新/池化快照方案：按 RO 当前动作 OnUpdate Update/Cease/IsEnd，常驻 Update 只倒序遍历存活列表 Update 为 false 就 Cease(true)；删除 MSkillUpdateEntry 源/meta/工程索引与快照专用属性，异步计时的原施法代次保持。
- 列表不能统称“脱手”：参考 RO 还用于独立运行技能；本地原动作退出移交时序保持，没有另造全技能统一管理层。日志 `项目修改日志/2026-10-11-RO-SKILL-UPDATE-SIMPLE-CHANGELOG.md`，分析 `项目分析/RO_SKILL_CD_LIFETIME.md`，本轮结果由 TEST_REPORT 记录。
- 终验独立274运行、272源零错/37旧，主600输入SHA与两删除before一致；2801保护仅三修改/两授权删除。工程修复BOM后精确只删包装Compile的89字节。旧快照人造重入测试移除，正常列表自删除/动作一次推进和既有真实业务回归通过，现场Unity/网络边界仍未验。

## [CONTEXT][技能][PC-070] RO 技能去除额外确认等待（2026-10-11，最新）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 417—423 行；历史时点记录，不代表当前仍在执行。

- 本条覆盖历史逐笔预测确认超时和必须服务器 End 才回收的方案。删除组件 `_pendingSkillIds`、Core `_predictions`/Deadline/Expire/Cancel 与 HasPending 门禁；本地立即表现/CD，动作与 CD 允许后可续放，无回包不阻断。
- Core 只保最近施法及其已应用结果 UUID，供迟到权威 CD/MP 校正和重复结果去重；没有按帧确认事务。RO 同名 Reload 由 Cease 查当前安装核心/原 UUID 调用，旧等级旧实例不能清新或已权威 CD；已回池本地 Start 不重播。
- Update 按累计技能时间和 RO 预表现 2 秒/正式 10 秒上限；动作退槽且表现完成可自行回收，不需确认/End；制作/完整音尾范围与此自然回收是本地适配，未来 Timer/冷 GO/完整声尾保持，FireEvents/MTimerMgr/OnProcessTimer职责不变。
- 四既有源，独立 275 运行、273 完整客户端源零错/37旧、主605项SHA全一致；2801保护2797保持/四授权改动，真实工程before完整字节保持。分析 `项目分析/RO_SKILL_CD_LIFETIME.md`，日志 `项目修改日志/2026-10-11-RO-SKILL-NO-CONFIRM-WAIT-CHANGELOG.md`。真实网络/Unity画面与听感仍未验，不自动发布资产。

## [CONTEXT][技能][PC-071] RO 技能冷却与脱手管理（2026-10-11，已完成）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 424—429 行；历史时点记录，不代表当前仍在执行。

- MSkillComponent.Update(float)常驻统一推进当前及脱手实例，int毫秒入口桥接一次；动作OnUpdate仅IsEnd退出。MSkill bool Update返回生命周期存活，Cease默认/true真结束→组件OnSkillEnd解除→工厂Reset/归池；自然End仍原ApplyServerEnd(false)保晚事件/冷GO/完整声尾。
- SkillCDGroupInfo承载两类同名列表/字典，普通键技能ID、组键组号，CD/CurrentCD秒。Get/Set/Clear与Factory.singleton借还接预测/权威/回滚/CanStart/旧UI；已安装Core读取组件唯一CD源，不缩放realtime递减，技能推进仍实体fDeltaT。原引用代次池快照防虚Update集合重入/池重借。
- 独立253当前运行、273真实源编译零错/37旧，主613项SHA一致；五旧业务源＋两新类型/meta＋生成工程共10路径。2796保护仅5授权旧源变化/4新增，生成工程无before字节限制见日志。F13/end111/F49及资源保持；现场Unity/听感/网络未验。分析项目分析/RO_SKILL_CD_LIFETIME.md，记录项目修改日志/2026-10-11-RO-SKILL-CD-LIFETIME-CHANGELOG.md。

## [CONTEXT][技能][场景实体][PC-072] 位移组件实体挂接纠正（2026-10-11，已完成）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 430—436 行；历史时点记录，不代表当前仍在执行。

- 后续精简已删除MEntity.OnBeforeInitialize的AppendExtraComponents空扩展调用；当前全客户端无附加登记，位移直接CreateComponent后由基类初始化普通容器。独立本轮271源编译零错、189回归重新执行通过，318项SHA一致；记录项目修改日志/2026-10-11-ENTITY-EXPLICIT-COMPONENTS-CHANGELOG.md。
- 用户明确RO位移组件挂在实体，由MComponentMgr.singleton.CreateComponent创建；先前MMove构造持有偏离结构，已纠正为普通EntityComponentBase组件，实体_displacementComp管理快捷引用，组件容器统一生命周期/池回收，直接卸载立即停止。
- 普通容器运动优先阶段在原专用Move/Buff/Skill/Machine之前仅推进位移，后续普通阶段跳过位移，移动组件不手动推进；保同帧唯一提交及末帧Move先于Skill离槽。具体分析项目分析/RO_SKILL_MOTION_IMPLEMENTATION.md，日志项目修改日志/2026-10-11-DISPLACEMENT-ENTITY-COMPONENT-CHANGELOG.md。
- 独立189运行断言通过（63运动、92生命周期重跑、34实体组件），271客户端与65共享源码编译零错；主734项SHA零差异。共享追加Displacement=32，旧0–31及类型身份保持，Common/Net两DLL同步同次产物。下方155/64是历史证据，本轮未新跑原生导出，未验真实Unity画面/墙体/Animator精确末帧与听感。现3007001为F13/end111/F49，不自动重导或回退资产。

## [CONTEXT][技能][场景实体][PC-073] 粒子制作区间导出兼容（2026-10-11）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 437—443 行；历史时点记录，不代表当前仍在执行。

- SkillEditorAssetTools.ReadFx不再要求作者endKeyNumber为0；原预览每事件开始帧单次生成，结束字段为制作标记，不截断/重复，ReadTimelineDuration和ActionTiming.ContentEnd仍只读取特效/声音开始帧。原负帧/显式退出校验保留。
- 未接入的混音、倍率、自定义寿命和声音变更逐项报绑定/fxList/字段和值，不再笼统把合法区间报错。仅ReadFx及summary改，现特效范围UI、RO正式运行/运动链保持。
- 用户当前3007001落剑已F13/end111、末声音F49，以此新基线保护，历史F45/F60不得恢复。64原生与8宿主源零错/2旧、2788保护仅1授权修改；实际隔离导出FX=13/30秒、TimelineDuration=49/30秒，正式Runtime未自动发布。
- 使用项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md，归档项目修改日志/2026-10-11-SKILL-FX-EXPORT-CHANGELOG.md及TEST_REPORT；证据_temp/skill-fx-export-20261011，正式GUI点击未验。

## [CONTEXT][技能][PC-074] 特效播放范围与独立预览（2026-10-11）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 444—450 行；历史时点记录，不代表当前仍在执行。

- 上轮时间文字现扩为四行+独立下沿范围条：白触发/灰预计延迟/淡绿预计发射及尾粒/橙估算末端。Structure按文字和参数范围并集分行并扩纯显示滚动尾；Timeline保原事件marker/拖动命中；SerializedInspector沿原Seek/Sample提供三个暂停定位。仅这三源修改，正式预览/RO运行/运动链不变。
- 常量与同值TwoConstants能推预计首发，随机/曲线/距离条件显示未知；末帧是预览保守参数估算，非首末像素。资源身份缓存沿旧projectChanged清理，倍率/FPS即时算；不改作者END、资源或Runtime。
- 真实1作者F4/F45/F60保持，F45第二实例立即独立生成，F46双计时推进；粒子计数蓄力末F49/落剑首F53解释视觉先后，不能称排队或画面重叠。43原生、36编辑器源0错/4旧、602保护仅3授权变化；正式GUI/挂点画面未验。
- 使用项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md，归档项目修改日志/2026-10-11-SKILL-FX-RANGE-CHANGELOG.md；证据_temp/skill-fx-range-overlap-20261011/review，两报告与主116项SHA重核已通过。

## [CONTEXT][技能][PC-075] RO 程序技能位移与动画根运动（2026-10-11）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 451—458 行；历史时点记录，不代表当前仍在执行。

- 最新运动链：MArtSkill.Start仅base.Start/FireEvents，FireEvents将Fx/Audio/ChargeList一次注册MTimerMgr，OnProcessTimer→InnerProcessTimer→charge交独立MDisplacementComponent.onStart；技能Update不扫描或消费未来事件。持续曲线差量由独立运动Update求值，MMove统一合输入、重力、动画根和程序量，一次MoveCharacter，侧撞停止。
- 原作者moveSegmentList的Program/Forward/Self/固定方向水平累计曲线导为MSkillChargeData；At/End按FPS直接秒换算、不加动画末帧+1，零时长瞬时。保Y三策略及maxDistance硬上限，Continue可回拉；目标、纵向、动态方向、忽略重力或碰撞等未接入非默认参数拒绝。Animation与程序窗口重叠拒绝，原Collector算法保留。
- Fire冻结朝向、全局时钟原点、Init会话及动作时长；后续Core被服务器确认切段不能改变当前指令校验。冷配置/模型只执行窗内剩余差量，过窗跳过，同模型同步换NativeObject清旧pending并重建基线。自然离槽/Interrupted停运动不拉终点，原音效/粒子自然保尾契约保持。
- 九既有源、四新增类型/meta，业务由ro_skill_motion_develop修改，ro_skill_motion_review独立复验；真实RO分析与接口见项目分析/RO_SKILL_MOTION_IMPLEMENTATION.md，使用见项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md，修改和实际验收边界见项目修改日志/2026-10-11-RO-SKILL-MOTION-CHANGELOG.md与TEST_REPORT。
- 现七原绑定中3007001作者声音F60/30=2秒而发布Runtime Timeline为1.8秒/54帧，旧导出源亦同样失败，须原绑定入口显式发布后同步；本轮不自动改写资产。其余原资产与资源保持。

## [CONTEXT][技能][PC-076] 编辑器特效两类时间显示（2026-10-11）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 459—465 行；历史时点记录，不代表当前仍在执行。

- 集中Window的Timeline/Structure/SerializedInspector/SoundTimeline四既有源提供触发帧秒、持续估算或custom截断、原资源名三行旁注及tooltip/选中摘要。纯显示lane和滚动尾按字体宽度计算，真实marker/拖动/作者END保持。
- 持续复用GetPreviewParticleDuration预览保守参数算法，注明估算，不代表随机/曲线/实际停播精确测量。源循环与自动寿命技能单轮播放明确区分，custom秒数不含预览收尾宽限。
- AssetDatabase原Prefab不能按activeInHierarchy判资源内活跃；以实际生成particleSystem.gameObject根内activeSelf祖先路径只读过滤，源根失活仍排除。资源身份缓存沿既有projectChanged/关闭→ClearSoundWaveforms清，事件speed/custom/FPS即时读。
- 使用项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md，变更及边界项目修改日志/2026-10-11-SKILL-FX-TIME-DISPLAY-CHANGELOG.md和TEST_REPORT。

## [CONTEXT][技能][PC-077] 最新技能到期回调与模型 GO 边界（2026-10-11）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 466—474 行；历史时点记录，不代表当前仍在执行。

- 本节替代下文历史 PresentationReady/pending 方案。MArtSkill.Start 只 base.Start/FireEvents；protected virtual void FireEvents 一次登记 MTimerMgr，OnProcessTimer→InnerProcessTimer→fx/audio 处理具体到期指令。Update 只基类时钟与已结束声源回收，不登记、补播或绑定根。
- 删除启动模型订阅、OnModelReady、PresentationReady 事件与技能两套 pending。RO 同名 MModel.AddLoadGoCallback/RemoveLoadGoCallback 按需持有已到期 GO 操作；暖同步返回零，冷非零可取消，实际登记顺序不依赖字典槽位。真正 Reset 清等待，同模型 Load/SetGameObject 保尚未交付请求，交付核加载会话。
- RO FX 到期后申请 GO 回调；RO audio 直接调用 FMOD 模型音频接口。本项目 Unity AudioSource 需要 Transform，冷模型音效也等待 GO 是现有契约的适配，不声称与 RO 声音原样相同。
- 动画由统一 TriggerAnim 登记当前动作所有者，模型加载完成只恢复当前动画；离槽即解除。Collector→MMove 继续负责水平 Animator.deltaPosition、重力与碰撞，RO ECharge 是配置位移，不是动画根运动。
- 自然 End 保未来计时器、已到期 GO 请求与完整声尾，Interrupted 立即取消；锁定配置行、UUID/施法代次/池复用与粒子独立租约保持。完整已加载模型缺固定挂点报告并跳过该粒子，不再无限待重载或阻塞后续指令。
- 业务修改范围为 MArtSkill.cs、MSkill.cs、MModel.cs，当前用户基线与资源保护保持；最终验收见 TEST_REPORT，设计见 `项目分析/RO_SKILL_TIMELINE_RESPONSIBILITY.md`，归档 `项目修改日志/2026-10-11-SKILL-TIMELINE-RO-CALLBACK-CHANGELOG.md`。

## [CONTEXT][技能][PC-078] 技能导出与制作尾段（2026-10-11）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 475—482 行；历史时点记录，不代表当前仍在执行。

- 完整源动画的自然末帧与零完整哨兵均可导出；按源采样率核对，不使用作者时间轴FPS判断裁剪。
- SkillRuntimePresentation.DurationSeconds继续旧表原动画时长；TimelineDurationSeconds独立保存制作末触发范围，零兼容旧资产。FX/Audio可晚于动画；RootMotion和动作解锁仍原窗口，纯预览波形/粒子尾迹不延长制作范围。
- ReadAudio与现有编辑预览一致：开始帧固定音量单次完整音频，结束帧是制作标记；不新增截断或循环。此条替代旧声音非单帧门禁记录，其他未接入声音参数继续校验。
- SkillCtrl透传已有Interrupted；false自然End退动作到原_skills，等待已注册/未来票据/pending/声源完成后回池；true打断/拒绝/清场立即注销。RO计时器、UUID和池代次原链保持，原宽限含完整声尾，已自然结束的实例不因旧预测到期误清。
- 正式资产通过编辑态原绑定按钮显式导出；Inspector运行态禁用按钮并说明，其他校验错误就地显示。修改归档项目修改日志/2026-10-11-SKILL-EXPORT-FIX-CHANGELOG.md，使用说明项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md，验证边界见TEST_REPORT。

## [CONTEXT][技能][PC-079] 正式技能时间线入口（2026-10-11）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 483—496 行；历史时点记录，不代表当前仍在执行。

- MArtSkill 的 `protected virtual void FireEvents()` 一次注册本次已锁定运行资产的 FxEvents/AudioEvents，通过 `MTimerMgr.singleton.SetTimer` → `MSkill.OnProcessTimer` → `InnerProcessTimer` 实际执行，替代每帧扫描未来配置事件。
- `Scripts/Common/MTimerMgr.cs` 使用 RO 同名管理入口与整数毫秒时钟；Global 从原实体 deltaMilliseconds 唯一驱动。`SceneEntity/Skill/MSkillTimerArgs.cs` 是具名强类型载荷，携带 UUID、施法代次及具体声音/粒子事件。
- AddedTimerToken/KillTimerAll 管理本次票据，Reset 先注销且更新施法代次；冷配置按本次累计时钟补发，模型迟到只重试已经到期的 pending。Q 旧施法仍存活推进，真正取消停止，粒子独立租约收尾保持。
- 编译/回归及未验实机边界见 TEST_REPORT；完整修改归档 `项目修改日志/2026-10-11-SKILL-TIMELINE-RO-CHANGELOG.md`。制作范围与原未接入轨道门禁保持原能力。

### 最新时间线职责纠正（2026-10-11）

- 上轮防重标记没有完成职责迁移；最新 `MArtSkill.Update` 仅推进基类时钟与回收已结束声源，注册在Start/CacheReady一次执行，MTimerMgr到期回调直接消费，模型迟到由真实MModel.FinishLoad的PresentationReady补已到期pending。Reset解除可取消订阅与票据，保留UUID/施法代次及资源身份保护，不用Update等待或重试表现。
- Global取得YooPackage后调用SkillRuntimePresentationCache.NotifyResourcePackageReady，一次启动包迟到前已持有记录；缓存暖终态同步、冷终态一次、失败空值通知，取消后旧闭包不得写复用施法。
- TriggerAnim/模型就绪登记当前动作根归属；实际Animator水平delta仍由SkillRootMotionCollector采集，经MMoveComponent合并重力走原碰撞入口，两层本轮未改。RO ECharge是配置曲线/距离事件，不能与动画根运动混同；本轮未新增完整Charge执行器。
- 最新TimelineDurationSeconds独立制作范围、自然End保未来票据/pending/声尾和Interrupted立即清理契约保持；当前94独立断言、267源零错及实机边界见TEST_REPORT。职责事实与最终实现见 `项目分析/RO_SKILL_TIMELINE_RESPONSIBILITY.md`，修改归档 `项目修改日志/2026-10-11-SKILL-TIMELINE-RESPONSIBILITY-CHANGELOG.md`。

## [STRUCTURE][综合][PC-080] 项目位置

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 497—504 行；历史时点记录，不代表当前仍在执行。

- 项目根目录：`D:/unitypro/LYFMMORGP`
- Unity 客户端：`D:/unitypro/LYFMMORGP/LYF_MMO`
- .NET 服务端：`D:/unitypro/LYFMMORGP/LYF_Server`
- Unity 版本：2023.2.20f1c1
- Unity MCP 实例：`LYF_MMO@47ea91c229a904f3`

## [STRUCTURE][综合][PC-081] 客户端

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 505—514 行；历史时点记录，不代表当前仍在执行。

- 主程序集：`LYF_MMO/Assets/GM_Game/GM_Game.asmdef`
- 启动和热更新：`LYF_MMO/Assets/Boot`
- 业务脚本：`LYF_MMO/Assets/GM_Game/Scripts`
- UI：`LYF_MMO/Assets/GM_Game/Scripts/UI`
- 网络和管理器：`LYF_MMO/Assets/GM_Game/Scripts/Common`、`Manger`
- 依赖：YooAsset、UniRx、DOTween、HybridCLR、Google.Protobuf、Network.dll
- 当前已知活动场景：`Assets/GM_Game/Scenes/New Scene.unity`

## [STRUCTURE][综合][PC-082] 服务端

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 515—523 行；历史时点记录，不代表当前仍在执行。

- 解决方案：`LYF_Server/LYF_Server.sln`
- 服务：CenterServer、LoginServer、GateServer、GameServer
- 公共库：Network、MMoRpgCommon
- 协议：`LYF_Server/ProtoBuf/proto`，生成代码位于 Network 协议目录
- 默认端口：中心 10110、登录 10120、游戏 10130、网关 10140
- 技术栈：.NET Framework 4.8、Google.Protobuf、TCP Socket、SqlSugar/MySQL、Redis、Luban

## [STRUCTURE][综合][PC-083] 项目结构总览

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 524—587 行；历史时点记录，不代表当前仍在执行。

### Unity 客户端分层

- `Assets/Boot`：启动、资源初始化和热更新入口。
- `Assets/GM_Game/Scripts/Common`：客户端通用基础设施、网络和公共数据。
- `Assets/GM_Game/Scripts/SceneEntity/Entity/PlayerEntity.cs`：本地玩家场景实体，直接继承 `ROObject`；本地角色业务组件与场景实体组件分属不同容器。
- `Assets/GM_Game/Scripts/SceneEntity/Entity/RoleEntity.cs`：其他角色场景实体，直接继承 `ROObject`；角色同步数据作为实体运行时字段，不另建同名业务根对象。
- `Assets/GM_Game/Scripts/FunlModule/Role/SceneWorldSystem.cs`：客户端场景实体唯一管理器；本地 `PlayerEntity`、其他 `RoleEntity` 及其他实体统一进入同一个 `SceneEntityWorld`，通过实体 ID 查询和移除。
- `Global.SceneWorld`：跨场景访问场景实体世界的进程级入口；业务模块从 `SceneWorld.LocalRole` 获取本地 `PlayerEntity`，禁止恢复 `ClientRole`、`RoleWorldSystem` 或独立业务同名实体模型。
- `Assets/GM_Game/Scripts/Manger`：客户端管理器，例如资源、提示和全局状态管理。
- `Assets/GM_Game/Scripts/controller`：界面控制器和服务器协议回包注册，例如 `MainCtrl`。
- `Assets/GM_Game/Scripts/UI/Main`：正式窗口和页面，例如背包、NPC 商城。
- `Assets/GM_Game/Scripts/UI/Tips`：提示、购买确认等弹窗。
- `Assets/GM_Game/Scripts/UI/test`：测试脚本和 Mock 入口，只能用于专用测试场景或手动临时挂载。
- `Assets/GM_Game/Prefabs`、`Scenes`、`UISprites`、`artres`：正式预制体、场景、UI 资源和美术资源。
- `Assets/GM_Game/Plugins/Net/Network.dll`：客户端使用的网络与协议程序集，必须与服务端协议生成版本匹配。

### .NET 服务端分层

- `GameServer`：游戏逻辑服；网络回调只生产 `GameCommand`，主循环消费队列并驱动模块 Tick。
- `GameServer/manger/RoleManager.cs`：在线角色、角色/会话双索引、角色协议、进入世界和附近同步。
- `GameServer/Ctrl/Game_LoginCtrl.cs`：登录、创建角色、开始游戏及 CenterServer 结果转发；不负责角色运行时逻辑。
- `GameServer/Service`：领域服务，例如 `KnapsackService`，负责背包容量等权威业务校验和状态修改。
- `GameServer/Entity`：游戏服运行时实体，例如 `OnlineRole`。
- `GameServer/SceneEntity`：场景实体体系按职责分目录：`Base`（5份基础类型）、`Entity`（15份实体）、`Component`（24份业务组件）、`Skill`（ROSkill/ROSkillCore）、`World`（EntityWorld/EntityFactory/SceneUidAllocator）。2026-10-21仅目录整理，源码和现有命名空间保持，GameServer.csproj编译路径同步；勿与正式在线角色的`GameServer/Entity`及其组件目录混淆。
- `GameServer/Entity/Components`：角色组件接口、组件容器和各领域角色组件；新增角色组件优先放入此目录，避免与角色根实体混放。
- 背包容量按 `KnapsackType` 的 `RolePackPlain`、`RolePackEquip`、`RolePackConsume`、`RolePackMaterial` 分类型持久化；四类背包相互独立，`RoleCurrtEquipPack` 为穿戴栏，不参与扩容。
- `RolePackPlain` 是独立普通背包，只包含自身物品，不汇总装备、消耗品和材料；客户端页签按类型读取独立容器。
- `GameServer/manger/Command`：跨线程消息信封、总队列、模块路由和固定帧注册表。
- `GameServer/manger/ClientMessageManager.cs`：统一接收 Gate/Center 网络回调，在逻辑线程按来源和协议分发客户端及中心服消息。
- `GameServer/MessageHandler`：按登录、角色、背包等领域存放一个协议一个处理器；处理器文件名只使用业务协议名，不包含 Gate/Center 来源；双来源协议由同一处理器根据 `GameMessageHandlerContext.Source` 分支处理。`GameMessageProtocolRegister` 统一登记网络回调与处理器，`ClientMessageManager` 只维护队列和处理器注册表。
- `CenterServer`：角色、背包等持久化数据和中心服协议处理。
- `GateServer`：客户端连接接入、会话管理和协议转发。
- `LoginServer`：账号登录和登录前置流程。
- `Network`：TCP、协议包、连接和公共网络基础设施；本项目当前不在其中实现游戏业务。
- `MMoRpgCommon`：各服务共享的基础类型和工具。
- `ProtoBuf/proto`：协议源文件；生成代码位于 `Network` 协议目录。

### 服务端消息与逻辑流程

1. GateServer 或 CenterServer 网络线程收到包，统一交给 `ClientMessageManager` 回调，只复制为不可变 `GameCommand` 并放入 `GameCommandDispatcher` 总队列。
2. GameServer 主循环每轮消费总队列，直接投递到 `ClientMessageManager` 的命令队列；客户端消息管理器在逻辑线程按协议和来源调用登录、角色及中心服处理入口。注册为 `CenterMessageRoute.ForwardToGate` 的 Center 回包必须在调用 Handler 前按原 Gate 会话原样转发；`Internal` 消息只由 GameServer 消费。
3. 累计达到 50ms 后，`GameGlobal` 按 roserver 固定顺序直接调用各业务管理器 `Update(delta)`；业务管理器不继承消息模块基类，也不承担网络队列消费。
4. 需要回包时，在逻辑线程通过命令携带的传输链路发送，禁止在网络回调线程直接修改游戏状态。

### 协作记录位置

- `PROJECT_CONTEXT.md`：长期稳定的项目结构、职责和技术约束。
- `CURRENT_TASK.md`：当前任务、验收标准、修改范围、状态和下一步交接。
- `DECISIONS.md`：已经确认的架构和设计决策，后续实现必须遵守。
- `TEST_REPORT.md`：每次构建、静态检查、运行验证和失败复现记录。
- `D:/unitypro/LYFMMORGP/项目修改日志/`：逐项项目修改的变更说明，覆盖代码、配置、协议、表格、资源、场景、工具及文档；禁止在其他目录散落项目修改日志。
- `D:/unitypro/LYFMMORGP/项目分析/`：参考项目解析、架构设计、方案说明及专题使用文档；`Docs/AI` 不再存放这些专题设计文件。
- `_temp`：构建、生成和验证产生的临时文件，未经产品经理确认不清理。
- `AIconfig`：项目 Agent 模板；`config.toml`、`workflow.md` 和 `agent/*.md` 必须按本项目实际环境维护。

### 文档分类约束

- 每次项目修改后的变更说明必须创建或追加到 `项目修改日志/`；历史根目录变更日志已统一迁入该目录。
- 项目分析、参考项目解析、架构设计和功能方案文档必须新建或维护在 `项目分析/`；历史专题设计文档已统一迁入该目录。
- `Docs/AI/PROJECT_CONTEXT.md`、`CURRENT_TASK.md`、`DECISIONS.md`、`TEST_REPORT.md` 是协作入口记录，继续固定保留在 `Docs/AI/`。
- 文档互相引用时使用从项目根目录开始的路径，例如 `项目修改日志/xxx_CHANGELOG.md`、`项目分析/xxx_DESIGN.md`。

## [STRUCTURE][协作][PC-084] 协作约束

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 588—604 行；历史时点记录，不代表当前仍在执行。

- 所有回复和代码注释使用中文。
- 新增或修改的业务代码必须为每个类型/接口、函数（含构造函数）、字段、属性、常量、事件、参数及局部变量补充具体中文注释；注释必须说明用途、数据语义、单位/范围或生命周期等实际信息，禁止只重复标识符名称。修改既有代码时，须在修改范围内补齐缺失注释。
- 新增或修改代码禁止声明命名空间（禁止使用 `namespace ...` 代码块）；类型直接使用全局命名空间，引用外部程序集时仅使用必要的 `using`。
- 每个顶级类、接口、枚举和委托必须独立放置在一个同名源文件中，禁止在同一源文件定义多个顶级类型。
- 主 Agent 只负责需求拆解、任务委派、测试审查协调、验收和协作记录；客户端、服务端业务代码必须由对应开发子 Agent 修改。
- 修改前先读取本文件、`CURRENT_TASK.md` 和相关决策。
- 保留用户现有修改，不使用回退或覆盖命令恢复工作区。
- 客户端和后端新增或修改代码时，为协议字段、状态处理、复杂计算和容易误用的逻辑添加简洁中文注释。
- 修改后必须进行对应编译或脚本校验，并把结果写入 `TEST_REPORT.md`。
- 开发、构建、验证产生的中间文件和临时文件统一放入 `D:/unitypro/LYFMMORGP/_temp`，禁止散落在根目录或客户端/服务端目录。
- 临时文件使用完成后先记录用途，再由产品经理确认清理。
- 项目代码禁止使用 `foreach`；新增或修改代码统一使用 `for` 遍历。字典等不可直接按整数索引的集合，先创建快照列表再按下标遍历；列表元素为引用类型时，修改元素对象会作用于原对象，但替换列表元素不会回写原容器。
- 测试按钮、Mock 数据、调试入口和测试逻辑只能放在测试脚本或专用测试组件中，禁止写入正式业务脚本。
- 测试组件不得挂载到正式场景或正式预制体；测试需要使用专用测试场景、测试预制体或手动临时挂载。

## [CONTEXT][技能][PC-085] 技能编辑与运行配置（2026-10-07）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 605—614 行；历史时点记录，不代表当前仍在执行。

- 编辑器为固定 MIT 嵌入包 `LYF_MMO/Packages/com.ethan.act-action-editor`，适配 Unity 2023.2，保留 YCombatEditor 原 `SkillEditorWindow`、`SkillConfigSO`；原参考仓库不修改。
- 编辑器源码集中于`LYF_MMO/Assets/GM_Game/Editor/Skill/ActionEditor`，按Window/Timeline/Infrastructure职责分文件，保持原`Ethan.ActionEditor.Editor`程序集、meta及GUID；嵌入包保留Runtime、文档和原测试。帮助与自动化版本报告通过Runtime类型定位包。位移、碰撞、飞行物、音效、镜头、流程等制作功能全部保留，本游戏运行支持范围继续按导出链校验。源码阅读索引为`项目分析/SKILL_EDITOR_CODE_READING_GUIDE.md`。
- 技能配置根目录 `LYF_MMO/Assets/GM_Game/Scripts/Data/Skill`：Editor/Authoring 保存七 MJS 配置与绑定、JX_Attack01 参考与绑定；Runtime 保存七正式表现；SkillCastingRulesConfig.asset 是施法权限编辑源。原 Luban 技能表与原动画/特效资源继续保留。
- 2026-10-08目录分离：Data/Skill只存配置与JSON；运行/配置类型源码位于 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill`，宿主编辑工具与 LYF.SkillAuthoring.Editor 程序集位于 `LYF_MMO/Assets/GM_Game/Editor/Skill`，独立验证代码与 LYF.SkillAuthoring.Verification.Editor 程序集位于 `LYF_MMO/Assets/GM_Game/Scripts/UI/test/Editor/SkillVerification`。游戏服通过更新后的Compile路径共享规则源码；资产路径、GUID与程序集名称保持。
- 客户端 Resources/SkillCastingRules.json 与游戏服 Config/SkillCastingRules.json 同源导出，服务端 Build 复制部署目录。InteruptSkill 使用新技能可打断旧技能的名单方向；MoveCasting 控制客户端水平移动，技能主动起跳仍固定禁止。
- 正式动画仍由 MSkillComponent.PlayAnim → MSkill.TriggerAnim 统一驱动，不在正式角色挂 Y 播放器。2026-10-08按用户要求移除运行时 AnimationEvent 特效兼容链，所有表现（含10070蓄力/落剑）统一使用编辑导出的 FxEvents。单次施法锁定表现行ID、动画和时长；冷配置就绪后按本次已累计时钟补发，不随Q换段改读其他行，粒子独立持有运行资产到尾迹结束。
- 编辑操作与已支持字段详见 `项目分析/YCOMBAT_EDITOR_INTEGRATION_GUIDE.md`、`项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md`，验证边界见 TEST_REPORT。服务端目前没有水平移动权威结算，不能将客户端权限字段当作服务器坐标校验。

## [CONTEXT][技能][PC-086] RO 轨道数据与源码入口（2026-10-10）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 615—622 行；历史时点记录，不代表当前仍在执行。

- 编辑器九类新增制作轨道在集中源码 Window/SkillEditorWindow.RoTracks.cs 注册并处理操作；SkillEditorRoTrackLabels.cs 显示对应字段及原枚举的中英文字。
- 包 Runtime/Data 新增九个原 RO 数据类及九个原值枚举，Runtime/Core/MSkillTrackTiming.cs 处理原秒字段的时间投影、末尾和校验；SkillConfigSO 追加九个对应字段，原 TrackType 身份保持。
- 这些入口用于编辑保存；正式运行仍按已有单动画/单次粒子导出范围，宿主拒绝未接入的非默认内容。原七技能、Q1/1/2与3-2局部参数、原表与预览修复保护。
- 阅读入口项目分析/SKILL_EDITOR_CODE_READING_GUIDE.md，方案项目分析/RO_SKILL_TRACK_EXTENSION_DESIGN.md。

## [CONTEXT][技能][PC-087] 技能音效和普通动作声音入口（2026-10-10）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 623—628 行；历史时点记录，不代表当前仍在执行。

- Assets/GM_Game/Audios/Skill/MJX保存12真实RO解码波形；Data/Skill下七原技能稿/Runtime新增AudioEvents和Animation根窗口，Editor/Authoring另有六SkillConfigSO_MJS_*普通稿，Runtime/SkillRuntimeLocomotionConfig_MJS.asset由SoundComponent加载。
- Editor/Skill/SkillEditorAudioMotionSetup显式制作及ExportLocomotion；SkillEditorAudioMotionTools负责音/根导出和一致校验；集中ActionEditor的Infrastructure/SkillEditorRootMotionPreview和Window.AudioPreview负责绝对根预览及专用隐藏声音源。
- 运行MArtSkill派技能声，MModel驱动SoundComponent普通动作并绑定SkillRootMotionCollector；MMove合并水平动画增量和重力走原碰撞入口。操作和边界见项目分析/SKILL_AUDIO_ROOT_MOTION_INTEGRATION.md及YCOMBAT_EDITOR_INTEGRATION_USAGE.md。

## [CONTEXT][技能][PC-088] 多段技能声音制作定稿（2026-10-11）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 629—632 行；历史时点记录，不代表当前仍在执行。

- MJX原12声音保留，新增RO原样Swing_Big_b、Swing_Short_h、Swing_Big_c。现有AudioEvents多事件执行器不变；E/R独立两阶段、F后刀声音F142独立于粒子F84，Q3/F第二声音量平衡。
- Setup显式制作仅精确识别旧资源/旧帧/默认音量迁移；自定义量保留，旧资源/帧改变则保留原项并补缺默认。作者/Runtime已同步；最新声音表项目分析/SKILL_AUDIO_ROOT_MOTION_INTEGRATION.md，方案项目分析/SKILL_AUDIO_POLISH_NOTES.md，实际听感边界见TEST_REPORT。

## [CONTEXT][技能][PC-089] 角色音频分类入口（2026-10-11）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 633—636 行；历史时点记录，不代表当前仍在执行。

- MJX下十五音频按移动、跳跃、技能分类；技能内通用挥刀、E、R、1、备用。文件名中文用途化，原meta/GUID与波形保留，Setup采用新相对路径，所有作者/Runtime强引用继续指同音频。
- 目录、原RO名对照及每声音的技能/动作帧秒/音量查项目分析/AUDIO_ASSET_USAGE_GUIDE.md；技能共享音源只保留一份，时间以事件配置为准。旧记录平铺英文路径仅历史来源含义。

## [CONTEXT][技能][PC-090] Audios最新英文结构（2026-10-11，替代上一轮中文分类）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 637—640 行；历史时点记录，不代表当前仍在执行。

- 全音频根Assets/GM_Game/Audios，角色声Characters/MJS/{Movement,Jump,Skills}；Skills为Shared/E/R/SwordDrop/Legacy。三原技能特效声Shared/Skills/Effects。十八声音名用Movement_、Jump_、Skill_英文前缀，GUID/波形/导入参数及原目录身份保持，Setup同步英文路径。
- 最新目录和原来源/用途/22事件帧秒量见项目分析/AUDIO_ASSET_USAGE_GUIDE.md。旧MJX中文路径是历史迁移记录，当前英文要求优先；声音强引用不需要重制配置或改预制体。

## [CONTEXT][技能][PC-091] 技能编辑器声音独立行与拖动语义（2026-10-11）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 641—646 行；历史时点记录，不代表当前仍在执行。

- Sound保持独立表现轨道，原fxList仍保存粒子/声音。旧缺Sound布局有声音时仅恢复显示，不自动写布局；显式contentKind区分空Audio/Visual，Legacy推断兼容。Fx/Warning仍统一视觉行；音效不参与其高度和lane排序。
- Interaction的MovedEventEnd统一十类事件末帧平移：正数点跟随起点，0/负旧单次哨兵保持，区间保持跨度。音效仅在Sound复制粘贴，不跨入Fx菜单。
- 声音正式单次门禁仍保留；ReadAudio诊断区间资源/起末帧与修正入口。参考项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md和项目修改日志/2026-10-11-SKILL-TRACK-DRAG-FIX-CHANGELOG.md；回归和现场边界见TEST_REPORT。

## [CONTEXT][技能][PC-092] 技能编辑器暂停与声音对齐入口

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 647—652 行；历史时点记录，不代表当前仍在执行。

- 集中ActionEditor窗口的SetPreviewPlaying切换播放／暂停，SeekPreviewFrame统一逐帧与播放头定位，SampleAnim使用时间轴固定步推进粒子／飞行物；停止清理与暂停保留分开。AudioPreview为每个声音事件维护独立隐藏AudioSource，支持定位、暂停和继续。
- Infrastructure/SkillEditorAudioWaveform读取真实PCM包络与精确主峰，Window/SkillEditorWindow.SoundTimeline绘制完整声音、分配重叠行并支持Alt选点；Inspector的AlignSoundTrigger沿原Undo链平移事件。完整波形只影响显示范围，不改事件末帧或正式技能END。
- 原用户配置和正式播放链不在本轮修改范围。使用步骤见项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md，验收及现场界面／设备声音边界见TEST_REPORT；归档项目修改日志/2026-10-11-SKILL-PREVIEW-PAUSE-ALIGN-CHANGELOG.md。

## [CONTEXT][技能][PC-093] 编辑器纯预览尾段

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/PROJECT_CONTEXT.md`，第 653—657 行；历史时点记录，不代表当前仍在执行。

- GetPreviewContentEndFrame保留正式事件结束门限，GetPreviewEndFrame只缓存粒子／声音尾帧并扩预览导航；动画与根运动在正式END保持终帧，END后不补发新的FX／声音，已触发实例满足收尾条件后循环。
- 手工Simulate＋Pause下IsAlive可能一直为true。编辑器有限粒子以包含延迟／发射／尾粒／各系统及事件倍率／子发射器的保守时间轴上界，加实际particleCount归零判断结束；仍有粒子时继续保留。自定义寿命与手动预览原清理语义保持，正式运行未改。
- 1技能真实后段与其它轨道核查结果见Docs/AI/TEST_REPORT.md，操作见项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md；归档项目修改日志/2026-10-11-SKILL-PREVIEW-MULTISTAGE-CHANGELOG.md。
