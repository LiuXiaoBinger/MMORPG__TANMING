# 项目代码与文件修改验证
> 【双语记忆】本文件为开发人员中文版本，完整历史正文在此；Agent 默认读取对应的精简英文版 `Docs/AI/TEST_REPORT.en.md`。英文为精简工作记忆，旧编号尚未迁移的英文段落，按需由子 Agent 按编号补录（流程见 AGENTS.md 与 `Docs/AI/PROJECT_CONTEXT.en.md` 的 DOCS-READ/DOCS-ROUTES）。

## [VERIFY][协作][SKILL-USAGE-DISCLOSURE-20261024] 增量技能披露指令校验

- 验收范围仅限新增的 `AGENTS.md` 章节、新增的 `[DOCS-READ]` 条款，以及本编号的任务与验收记录；由子 Agent 同步中文并检查本次增量，Codex 只复核实际范围与提交的证据，不再重复双语检查。最终空白与范围校验、委派结果归入 `项目修改日志/2026-10-24-技能使用说明约束_CHANGELOG.md`。
- 无业务代码、Unity 资源或运行时行为改动；Unity/客户端/服务端构建均不适用。不得由本校验计划推断任务已完成。

## [VERIFY][协作][VERIFY-SUBAGENT-SKILL-UPDATE-20261024] 子 Agent 规则集中化验收

- 已用三条新规则更新既有项目技能：Asia/Shanghai 23:00 后优先 WorkBuddy、逐次记录提供方与实际模型身份、长上下文交接前先读取此前对话；用户本轮明确指定的模型仍然优先。
- `AGENTS.md` 现在对子 Agent 的路由、派工、返修、上下文交接与验收统一指向该技能；项目授权，以及宽泛的代码、文件、分支与文档约束仍保留在根目录。
- `quick_validate.py` 在 Python UTF-8 模式下通过（输出 `Skill is valid!`）；已跟踪改动的 `git diff --check` 通过。内容复核确认只有一条模型优先级规则，且根目录到技能的路由一致。
- 本指令性改动不适用 Unity/客户端/服务端构建。本轮创建了 WorkBuddy 文档同步任务，但 Agent Hub 的 `modelSelection=false`，未通过该任务选择或实测 `Hy4 preview`，也未新建豆包任务实测 `豆包 2.1 Pro`；本轮 WorkBuddy 实际模型未披露/无法确认。记录同步与剩余限制见 `项目修改日志/2026-10-24-子Agent协作技能.md`。

## [VERIFY][协作][BILINGUAL-DELTA-CHECK-20261024] 增量文档校验

- 校验范围仅限本次规则增量、`[DOCS-READ]` 同步，以及新增的 `[BILINGUAL-DELTA-CHECK-20261024]` 任务与验收记录；由子 Agent 执行中英同步与一致性检查，Codex 只复核实际差异与提交的证据，未改动的历史不在范围内。
- 这些纯文档改动无需 Unity 导入、构建或玩法测试。委派结果、空白与范围校验、未解决项记录在 `项目修改日志/2026-10-24-中英增量检查委派约束_CHANGELOG.md`；不得由本校验计划推断任务已完成。

## [VERIFY][任务][TASK-SYSTEM-DESIGN-20261023] RO 任务系统设计验证，2026-10-23

- WorkBuddy 的只读证据报告已按真实路径做过独立抽样核对：RO 状态投影与完成判定的区分、目标物品背包重算、目标 UI 路径、本项目的 QuestInfo、OnlineRole 注册与存档边界、导航回调、MainView 注册以及可用预制体位置。设计对单目标 QuestInfo 与既有任务预制体的修正，都以该证据为依据。
- 文档校验仅限于 Markdown 结构与引用、以及证据一致性。未运行任何 C# 源码改动、生成协议、配置、数据库迁移、Unity 导入与渲染、编译器、服务端与客户端连接、任务状态推进、奖励事务或玩法测试；以上均未宣称完成。
- 这里描述的每一项代码、配置、数据库与协议改动，都需要后续由用户提出实现请求才能进行。设计证据、边界与执行者身份记录在 `项目修改日志/2026-10-23-任务系统RO对照设计_CHANGELOG.md`。

## [VERIFY][任务][QUICKTASK-TEXT-20261023] RO 风格任务文本验证，2026-10-23

- Codex 独立 `node _temp/quicktask-text-20261023/verify.mjs` 通过：YAML 文档 243→247，恰好新增四个原生 Text 与四条 owner 记录，其余每个字节与 meta 均保留，无悬挂组件引用，原生字段与 RO 实文档一致。改前 SHA256 `d46af5362a807248f3535fcab1713ea72d0881872a4b1d338d1ab898b8ddf555`；改后 `cf1669113840623f18d8156bf758fc0c3e0cdee19f192f35885771ac8de03789`。
- 真实 Unity 同步导入/加载通过：57 个节点、Missing Script=0、Text 12→16。五条原路径均解析到预期中文文案；Common 字体、白色 Title/Des/Deadline/Limit 与既有黄色 Target 均正确。未改动节点命名/父子/顺序/active/RectTransform。用户原有的 TargetText2 保持启用。
- 独立预览场景渲染 `_temp/quicktask-text-20261023/after-preview.png` 已目视：五类文本均可见。最初 SceneView 截图为空，未作为验收证据。首次独立渲染记录到一次临时 RenderTexture 清理报错；在销毁前先清空临时相机目标后修复，复跑未新增报错（仍留一条早先记录）。预览场景已关闭，验证过程未保存任何正式场景或 prefab。未运行玩法、交互或客户端/服务端构建。
- WorkBuddy Hub `hub-bc16cc63-ee30-4867-9ef6-8e7c04db3e69` 第 2 轮 `run-4aef2446-cf2a-4445-a493-a2f68eca3369` 依 Codex 证据 accepted。完整变更清单、先前豆包中断、语言同步与 Git 回执：`项目修改日志/2026-10-23-QuickTaskPanel文本与RO层级对齐.md`。保留无关的用户改动，只暂存本任务的资源新增与记录段。
- 一次 `qwen3:8b-light` 同步请求 45 秒超时，无可用输出、无模型文件落盘，未重试。WorkBuddy 第 3 轮仅做中文同步，不是又一次修复失败。

## [VERIFY][任务][QUICKTASK-RECLEAN-20261023] QuickTaskPanel 再次清理验证，2026-10-23

- Codex 确认 PrefabStage 未打开，同步导入当前 QuickTaskPanel 磁盘资源，测得 58 个 Missing Script、57 个节点、78 个有效 UGUI MonoBehaviour。有效组件类型/数量精确快照：`_temp/quicktask-reclean-20261023/unity-before.json`。
- Codex 独立 `verify.mjs` 通过：恰好删除 58 个自定义文档/组件项与 78 条 UGUI 脚本映射；其余每个字节与 meta 均保留，无悬挂组件引用。改后 SHA-256：`9f5624761c5827b7c0451446d2cb82326b1df1cfe5e9789a9983c852157bba6a`。真实 Unity 同步导入/加载通过：missing=0、节点 57、有效组件类型/数量快照完全一致（`unity-after.json`）。
- 相对 HEAD 的最终差异只含用户已有的锚点位置/尺寸两行，保留在暂存区之外。不宣称渲染交互、客户端或服务端编译、玩法验收。范围、同步与 Git 回执：`项目修改日志/2026-10-23-QuickTaskPanel自定义脚本再次清理.md`。
- 一次 `qwen3:8b-light` 中文同步尝试 45 秒超时，无可用输出与模型文件落盘，未重试；WorkBuddy 第 2 轮只同步中文。该事件与已通过的资源检查相互独立，不计为又一次资源修复失败。

## [VERIFY][协作][QWEN-MEM-20261023] 2026-10-23 文档/配置验收（当前，文档/配置核验通过，非业务验收）

- 验收范围：Qwen 轻量路由、一次失败切换、五份英文工作记忆、完整中文历史保留、同步任务 ID 与当前证据一致；本任务不改游戏代码/资源或网关实现。
- 已执行：`AIconfig/config.toml` 用 Python/tomli 解析成功；Qwen 模型、一次尝试限制、回退提供方与双语策略已核验；Codex 独立回读豆包实际写入的 `AIconfig/agent/qwen_executor.md`，接受其角色/职责边界。
- Qwen 角色初稿因混淆调用方落盘职责、缺少查找/修改回退路由被驳回；未反复返工、无 Qwen 文件写入；WorkBuddy 未领取取消任务与豆包停滞传输均不计开发轮次。
- 独立核验结果：Codex 独立运行 `verify_memory.py` 退出 0、无错误；1256 个原始中文历史段（除明确扩展的路由段外）全部保留、18 个英文段存在、标签/编号一致、英文正文实质、无重复编号；Python/tomli 解析，模型/回退/上下文/角色文件与五份英文引用断言通过（文档/配置验收，非 Unity 或业务验收）。
- 中文同步：豆包本地会话 `38448830092170754`、run `59469831083523330`，实际输出经独立核验；最终暂存格式/范围检查与推送结果记于 changelog，不在各段重复。
- 体量：初始英文合计 31230 字节，最终各文件大小以 `verification.json` 为准；1242 个旧段尚未翻译、中文完整保留、按需补录，不宣称全量等价或实测 token 节省。
- 保留基线：五份快照存于 `_temp/qwen-bilingual-memory-20261023/baseline/`，共 1256 个二级标题；该数字证明历史保留范围，不代表 1256 段已翻译，历史全量翻译为按需进行。
- 本次散文/策略改动不要求 Unity/服务器构建或运行验证，不推断游戏验收；最终检查输出与未决项见 `项目修改日志/2026-10-23-QWEN-BILINGUAL-MEMORY-CHANGELOG.md` 与 `_temp/qwen-bilingual-memory-20261023/verification.json`。

## [VERIFY][协作][QWEN-LIGHT-20261023] qwen3:8b-light 精确模型约束验收

- `http://127.0.0.1:11434/api/tags` 已返回 `qwen3:8b-light`；当前 TOML 模型字段及轻量角色/流程引用均使用该完整标签。
- Python/tomli 断言通过：`model=qwen3:8b-light`、一次尝试回退、4096 上下文及 `think=false`；当前策略文件含完整标签并禁止静默替换。
- 这是配置与本机标签可用性检查，不运行 Unity/服务器或游戏功能验证；历史 `qwen3:8b` 冒烟证据仍按历史记录保留。
- 本次中文同步实际尝试：对 `qwen3:8b-light` 的一次限定翻译 API 调用 90 秒超时、无输出，属未成功的 Qwen 交付（非成功轮次）；按一次失败切换规则改由豆包完成中文同步，未反复重试。该事件不改变配置/标签可用性结论，仍不构成 Unity/服务器或游戏验收。

## [VERIFY][协作][DOCS-20261023] 协作入口分类与标签验收

- 验收范围：四份原文归档字节一致；历史段落正文完整覆盖；查询迁移与文档产物摘要分离；编号唯一、主题可检索；新路由引用、TOML和Git格式检查。
- 验收结果：四份原文归档与备份字节一致，1202段正文覆盖、76段查询迁移、标签编号和引用检查通过；主题检索与TOML解析通过，Git格式结果见本轮日志。
- 本轮没有修改业务代码、场景或正式资源，因此不运行Unity/服务端构建；不把文档验收当作NPC显示、双客户端同步或发布验证。
- 实际检查结果、文件清单及提交/推送回执集中在 `项目修改日志/2026-10-23-DOCS-AI-TAGGED-LEDGER-CHANGELOG.md`；原业务未验边界见下方对应主题。

## [VERIFY][盘点][TR-001-FILES] 查询任务的正式文档产物：2026-10-23 功能完成度盘点图文版：文档验证

- 该任务记录的正式文档路径：`项目分析/MMORPG功能完成度与开发优先级-图解.html`、`项目分析/MMORPG功能完成度与开发优先级.md`、`项目修改日志/2026-10-23-MMORPG-COMPLETION-INFOGRAPHIC-CHANGELOG.md`。
- 查询正文、当时状态及原文验证边界见 `Docs/AI/QUERY_TASKS.md` 的 `[TR-001]`；本条只保留文件产物追溯，不新增完成或检查结论。

## [VERIFY][协作][TR-002] 2026-10-22 当前生效：上下文过长自动新开对话

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 7—11 行；历史时点记录，不代表当前仍在执行。

- 本轮只验证TOML解析、三份生效策略一致、各7轮配置保持、精确文件范围、原用户文件哈希及Git空白；不重复运行未变化业务测试。未实际创建对话，不将规则检查写成迁移运行验收。
- 逐项文件、验证与Git回执：项目修改日志/2026-10-22-AUTO-NEW-CHAT-CHANGELOG.md。

## [VERIFY][协作][TR-003] 2026-10-22 当前生效：精简额度流程与各7轮回退

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 12—16 行；历史时点记录，不代表当前仍在执行。

- 本轮仅规则与TOML配置变更，检查配置解析、两模型各7轮/返工6轮及精简规则一致性、文件范围、Git空白和原用户文件哈希；具体结果集中修改日志。未重跑未变化业务编译或游戏测试，不产生新的Unity显示与部署结论。
- 逐项文件、验证与Git回执：项目修改日志/2026-10-22-QUOTA-SEVEN-ROUNDS-CHANGELOG.md。

## [VERIFY][NPC][TR-004] 2026-10-22 NPC开窗修复：提交推送回执

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 17—21 行；历史时点记录，不代表当前仍在执行。

- 本阶段5文件提交`8d47e15ce9eb041959fbc81a2a6735adeae6a3ae`已正常推送origin/codex/npc-dialog-fix，远端SHA核对一致；未合main。源码与隔离检查通过，真实Unity显示与热更部署仍未验；未把原用户修改混入暂存。
- 本条仅补Git回执，不代表新增运行验证。逐项文件、结果和证据见项目修改日志/2026-10-22-NPC-DIALOG-CHANGELOG.md。

## [VERIFY][NPC][协作][TR-005] 2026-10-22 NPC对话框反向引用：本阶段最终独立验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 22—31 行；历史时点记录，不代表当前仍在执行。

- 唯一业务修改MainCityMgr.BindMainRoleCompatibilityController增加sceneEntity.SetEntityCtrl(mainRoleController)与中文说明；原SetROObject和原导航/OpenTalk/UI流程保持。修复的是开窗参数为空，不改A*、移动或碰撞。
- Codex正式Network.dll插件路径289源Rebuild：exit0、0错误、38警告；未逐条归因警告。557编译输入SHA零差异，见arrival-followup/codex-validation/build/client-plugin-path及compile-inputs-check.json。
- Codex在独立输出目录重新抽取Git32a7fa7与当前真实绑定方法原文，配合真实MNavigationComponent/NpcCtrl复跑。修前24/0断言验证故障状态：近远到达均开窗0次、主角尚未创建完成；修后26/0验证已有/原流程新建控制器的双向同一引用、近远到达各调用TalkWindow一次且NpcEntity身份保持、停走、后续5帧不重复及过远提示。24与26不累计为功能通过数，修前通过代表成功复现故障。
- 薄边界使用Unity/实体容器/模型位置推进/UI调用记录替身，不含真实窗口显隐或渲染；后续帧无重复只证明该到达入口不重复调用，不能当整个UI系统排除双触发。9项测试输入哈希保持，详见codex-validation/behavior/result.json与before/after/runtime.log。
- 实际差异审计：业务仅一行绑定及注释、meta完整字节保持、无越界已跟踪修改、9用户文件SHA保持。没有改导航、NpcCtrl、TalkWindow或UIRoot，也没替用户热更字节。最终证据codex-validation/audit-final.json。
- WorkBuddy本项目desktop_queue平台round2已由Codexaccepted；只读阶段“直接OpenTalk代表完整到达”“程序集字符串证明现场同版”两处过强说法已纠正，不作为验收依据。无Unity现场/发布验收，现有Editor.log历史不可作为本轮现场证据。
- 逐项5文件与Git回执：项目修改日志/2026-10-22-NPC-DIALOG-CHANGELOG.md；完整临时证据根_temp/npc-dialog-20261022/arrival-followup。下一步加载同版代码及上一阶段UI资源，现场点击NPC观察对话框、文字和关闭/重开。

## [VERIFY][NPC][协作][TR-006] 2026-10-22 用户澄清：角色走过去但NPC对话不显示，继续定位

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 32—37 行；历史时点记录，不代表当前仍在执行。

- 用户确认点击NPC后角色会走过去，当前重点转为导航到达回调和窗口显隐。上一轮只修已确认文字绑定，不当作面板不弹的最终根因。
- 沿豆包原本项目local会话38448648846393602续查，run59396782753161730；分支codex/npc-dialog-fix、基线32a7fa7。当前仍无Unity现场进程/MCP和新日志，尚无新复现/修复通过结论。
- 同轮日志项目修改日志/2026-10-22-NPC-DIALOG-CHANGELOG.md；证据_temp/npc-dialog-20261022/arrival-followup；原9用户文件保持。

## [VERIFY][NPC][TR-007] 2026-10-22 NPC文字绑定修复：提交推送回执

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 38—42 行；历史时点记录，不代表当前仍在执行。

- 本轮6文件代码与记录提交`442fb623399a782a698fce3ae961e68211065e69`已推送origin/codex/npc-dialog-fix，远端SHA核对一致，未合main；9用户文件SHA保持。源码与隔离验收通过，完全不弹窗的现场现象仍待确认，同版热更程序集及UI资源尚未部署。
- 逐项文件和完整证据：项目修改日志/2026-10-22-NPC-DIALOG-CHANGELOG.md。本条为提交后的文档回执，不增加业务改动或声称新现场测试。

## [VERIFY][NPC][构建][TR-008] 2026-10-22 NPC文字绑定：编译与10项薄边界通过

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 43—50 行；历史时点记录，不代表当前仍在执行。

- 确认并修复TalkWindow.ReFreshUI未绑定真实NPC名称/对话；仅TalkWindow.cs与UIRoot.prefab两既有TMP引用，正文下划线显示换行。不改输入、NPC碰撞、导航或开窗生命周期。
- WorkBuddy桌面Hub hub-1771073d-0d79-4fd4-89c1-a7e677b75cba：两轮只读预检纠正错误假设，首次开发后一次审查精简，最终平台round4已由Codex验收；豆包先行桌面CLI受阻且确认停止，不计开发失败轮。
- Codex正式插件路径289源0错误，复跑真实源码薄边界10/0、编译输入557项保持，UIRoot逐块仅两引用变化、meta及9用户文件保持。
- 尚无本轮Unity真实点击/面板显隐验证。用户未明确点击后是否导航；“完全不弹窗”的现场现象仍待确认，不能将文字缺陷修复当作全部现场问题已解决。保留原GM_Game.dll.bytes，运行需同版热更代码和UI资源包；未启停游戏/服务或保存场景。
- 分支codex/npc-dialog-fix，基线14a0a88；本轮6文件逐项与Git回执见项目修改日志/2026-10-22-NPC-DIALOG-CHANGELOG.md，证据_temp/npc-dialog-20261022/codex-validation。

## [VERIFY][NPC][协作][TR-009] 2026-10-22 NPC点击无对话：验证准备

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 51—56 行；历史时点记录，不代表当前仍在执行。

- 用户反馈点击NPC无对话；先豆包官方桌面本项目只读定位（会话38448648846393602/run59391213363817218，第1轮），Codex沿路线核验后委派最小修复，最后复测。
- 当前无Unity MCP工具及可见Unity/LYF_MMO进程，Editor.log为10月21日历史输入；没有现场复现或本轮测试通过结果。保留原9用户文件，不启停用户游戏、不替热更字节、不保存场景。
- 本轮文件、事实与后续结果：项目修改日志/2026-10-22-NPC-DIALOG-CHANGELOG.md；证据_temp/npc-dialog-20261022。

## [VERIFY][网络][TR-010] 2026-10-22 RO网络接口：提交推送回执

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 57—61 行；历史时点记录，不代表当前仍在执行。

- 最终代码、正式Network.dll及逐项记录提交`28c21e8811e5f2d9c68d3cdcbc26b5123a6d9366`，已成功推送origin/codex/network-kcp-development，远端SHA核对一致；未合main。49项最终文件精确提交，原9用户文件推送后SHA保持。WorkBuddy最终轮review=accepted；验证为五服务/289客户端0错、Socket122/0、入场8/0、薄边界54/0，现场及发布边界保持。
- 文件和证据入口：项目修改日志/2026-10-22-RO-NETWORK-INTERFACE-CHANGELOG.md。本条与日志是提交后的文档回执补录。

## [VERIFY][网络][TR-011] 2026-10-22 RO公共接口：最终独立验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 62—69 行；历史时点记录，不代表当前仍在执行。

- 五服务工程Network/Gate/Game/Center/Login0错误；289客户端源默认临时新库及正式插件路径均0错误。实际警告计数服务14/14/28/14/16、客户端350，不作全部既有或本轮新增归因。
- 主Agent真实Socket/KCP122/0、入场8/0、客户端薄边界54/0；最终证据_temp/network-ro-interface-20261022/codex-validation/final-verification.json。薄边界Unity/资源前置用桩，KCP回调专项反射调用真实方法，不冒称Unity画面或真实UDP下行。
- 原测试53/1为主Agent过强预期，已只改测试为Disconnect旧代次失效，首日志保留；业务不加门禁。开发者自检126/52/8与主Agent数字分开记录，不能互相冒用。
- 最终正式插件与主Agent新构建SHA一致，原9用户文件、meta身份、33机械迁移语义保持；输入与验证记录一致。关闭Socket及UDP10054清理告警仍有，未宣称长稳或运行日志零错误。
- Unity双号、真实模型动画/MP、业务重连、实体早到、端到端弱网与IL2CPP/HybridCLR未验；未启停用户Unity/正式服务、保存场景或替用户热更字节。逐项文件及Git回执见项目修改日志/2026-10-22-RO-NETWORK-INTERFACE-CHANGELOG.md。

## [VERIFY][协作][TR-012] 2026-10-22 自动审批规则与实际原生回执

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 70—75 行；历史时点记录，不代表当前仍在执行。

- AGENTS、AIconfig总策略/流程及两执行者角色同步自动同意、允许本次、用户授权优先和原任务范围要求；TOML解析、文字覆盖、Git差异及原用户文件保护另见本轮检查。
- WorkBuddy当前原生待办真实选择allow_once，返回accepted=true、originalRequestStillPending=false、remainingPermissionCount=0、isProcessing=true。不把权限回执当作命令完成或业务自检通过。
- 本轮不修改客户端/服务端业务、场景、资源或插件，不重跑未变化业务编译。原网络任务中间测试与未验边界保留；对应日志项目修改日志/2026-10-22-AUTO-PERMISSION-APPROVAL-CHANGELOG.md。

## [VERIFY][网络][TR-013] 2026-10-22 RO公共接口：独立中间输入验证与阻塞

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 76—83 行；历史时点记录，不代表当前仍在执行。

- Network/Gate/Game/Center/Login五工程0错误；289客户端源引用本轮临时网络库0错误。警告日志计数分别14/14/28/14/16，客户端350条，不将全部警告归因本轮或宣称无警告。
- 真实Socket/KCP122/0、入场8/0、客户端薄边界54/0；输入与source-inputs.json一致。客户端包含真实MNetClient/NetClient/KcpClient，资源和Unity前置为桩；KCP接收专项反射调真实回调，不冒称真实UDP下行或Unity联机。
- 首版边界53/1为主Agent构造的ReleaseRealtimeClient同代次预期过强，基线不支持该要求；只改测试为真实Disconnect后旧包失效，复验54/0，保留首日志。关闭回调及UDP10054清理告警仍存在，入场部分中文解码有乱码，ASCII断言与退出码可核。
- 审计：33机械迁移语义保持、meta GUID及导入设置保持、9用户文件哈希保持；23个BOM噪声及最小代码审查意见待WorkBuddy原任务收尾。正式插件未集成，因此这些结果不是最终交付验收。
- WorkBuddy原生RPC确认waiting_input；原轮超时且取消未确认，未重复派工。证据与反馈在_temp/network-ro-interface-20261022；Unity双号、画面/MP、业务重连、端到端弱网、IL2CPP/HybridCLR打包未验，未启停用户Unity/服务或保存场景。

## [VERIFY][网络][TR-014] 2026-10-22 RO网络共用接口对齐：准备阶段

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 84—89 行；历史时点记录，不代表当前仍在执行。

- 已读取四入口、核对分支codex/network-kcp-development和基线85cda92，冻结原用户9份修改及未跟踪清单。豆包本项目local任务已受理，尚未交付。
- 本轮尚未开发、编译或运行验收，下一步按真实RO接口确定范围，再由WorkBuddy实现、Codex独立验证。历史108/8/39不作为本轮已通过结果。
- 本轮正式变更、后续文件清单及Git回执统一在项目修改日志/2026-10-22-RO-NETWORK-INTERFACE-CHANGELOG.md；临时证据_temp/network-ro-interface-20261022。

## [VERIFY][网络][协作][TR-015] 2026-10-22 固定 KCP：Codex独立源码与隔离验收通过，现场未验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 90—101 行；历史时点记录，不代表当前仍在执行。

- Git验收：18路径精确暂存、暂存空白检查通过；业务提交c9ee4d11237e8cffb983c8ff25abe5a8928a50a7推送origin/codex/network-kcp-development成功，ls-remote同SHA。未合main；补录回执仅改本文件、CURRENT_TASK和根修改日志，不重复声明运行测试。
- 最终compile_kcp.py重建Network/Gate/Game/Center，均0错误；警告日志计数14/14/28/14含重复输出，属于既有警告。compile_client_plugin_path.py以正式插件路径编译289源，0错误；未替用户热更DLL。
- run_loopback.py独立真实Socket回环PASSED=108 FAILED=0：Unity上行和Gate下行未绑定/绑定中/失败/就绪/释放/重连未绑定实时TCP零写入，普通业务TCP、服务间实时TCP、同通道保序及生命周期回归通过。WorkBuddy第二轮自检112/0单独归属开发自检，不冒作Codex复跑数字。
- run_enterworld.py独立PASSED=8 FAILED=0：真实protobuf与RoleManager处理，出生坐标过滤前应用，距出生8米/距原点约107米的观察者收到广播与附近补发，不发首移动；不是正式Unity联机。
- run_boundary.py独立PASSED=39 FAILED=0：链接真实MainCityMgr/NetSocketMgr，真实NetClient/KcpClient及TCP帧；Ready前不发起模型加载、Enabled后一次加载、拒绝/端口0启动失败/真实初次超时后清理与断开、Abandoned、实时TCP零写入。Unity、资源与实体前置为显式桩；Enabled由测试网关发送。旧代次只反射调用当前代次减1（本次为0）的释放入口，不当作真实旧连接回调与重连全链；只证明未发起加载，不宣称实际角色输入/画面已验。
- 正式Network.dll与最终独立build/network/bin/Network.dll SHA相同；生成RequestEntity.cs与正式protoc输出逐字节一致。静态新增语法候选空，git diff --check通过（仅行尾转换提示）；9份用户原文件SHA与基线一致。
- 日志中关闭Socket回调及UDP10054等清理告警保留，未宣称所有运行日志零错误或长稳通过。108/8脚本部分中文解码乱码，ASCII断言与退出码清晰，保留原证据。
- 未验：正式Unity双号、动画/模型真实加载、MP扣费、自动重连、实体早到、端到端弱网与HybridCLR/IL2CPP；原_enterWorldSent跨重连尚待现场确认。持续拒绝会继续无法进入可玩状态；本轮不扩退避/提示、大包或协议白名单问题。
- 证据_temp/network-kcp-only-20261022/codex-validation（build、三套result.log、final-verification.json）；逐项文件与Git状态见项目修改日志/2026-10-22-KCP-ONLY-REALTIME-CHANGELOG.md。未启停正式服务或Unity、未保存场景、未清_temp。

## [VERIFY][网络][构建][TR-016] 2026-10-22 网络整体复审：编译/回归通过，传输边界仍开放

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 102—110 行；历史时点记录，不代表当前仍在执行。

- Codex重新实际执行本轮_temp副本compile_kcp.py、compile_client_plugin_path.py：Network/Gate/Game/Center均0错误（警告日志计数14/14/28/14，含重复），客户端289源引用正式Network插件0错误。run_loopback.py退出0、PASSED=90 FAILED=0；review/run_codex_final.py三项FIXED结果True、退出0。
- 豆包两轮桌面只读检查后Codex复跑新探针：1186字节完整包Send=True、移动投递0；非实时登录11040走已认证UDP容器计数1。探针的无TCP Session使绑定回包空引用，是夹具边界，不作真实Gate登录故障。当前正常五实时协议都是小包，正常非实时业务走TCP。
- doubao-transition真实TCP/KCP探针：人为阻塞TCP回调时后发KCP先完成；无屏障豆包400组反序0、Codex独立400组反序2。直接发送四字节标记且绕过正式客户端分流/完整启用过程，只证明跨通道投递允许反序，未验证真实绑定时的游戏故障或发生率。
- Peer地址污染疑点未成立；本轮新多客户端探针没排空28字节绑定回执，不采用其“只到目标”判断。原90项目标路由回归另通过。未找到正式退出登录UI，Global.OnApplicationQuit明确Disconnect，不把LoginInfo清理判成退出泄漏。
- 正式插件SHA256保持950846B969F8FF43B229A3769473FD18CEBFAC2B41650E0D8E043FD9A2F8ACF2；本轮新编译DLL哈希与它不同，未证明等价原因。客户端引用正式插件编译不等于该插件运行覆盖。写正式文档前tracked差分与baseline-user.patch完全相同，用户9份修改保留。
- 证据_temp/network-kcp-audit-20261022/codex-validation.json、loopback-result.log、review/codex-final-result.log与三个新探针目录；正式专题项目分析/KCP_NETWORK_AUDIT_20261022.md。此次只增改6份记录，不改业务；三项发现未修复。无Unity双号、MP、正式切换/重连/实体早到、弱网端到端或打包运行验收，不能判全项目无Bug。

## [VERIFY][协作][TR-017] 2026-10-22 Git回执

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 111—114 行；历史时点记录，不代表当前仍在执行。

- 业务提交21c95f0已推送并由ls-remote核对一致；暂存34路径排除全部9个原用户文件。全暂存上游KCP原文2处空格保留为第三方例外，排除上游该单文件后业务差异检查通过。

## [VERIFY][网络][TR-018] 2026-10-22 最终KCP集成验收（源码/隔离范围）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 115—124 行；历史时点记录，不代表当前仍在执行。

- Codex最终实际执行compile_kcp.py：Network/GateServer/GameServer/CenterServer全部0错误，日志警告计数14/14/28/14，含重复输出。compile_client.py及compile_client_plugin_path.py均289源0错误，后者真实指向正式Plugins/Net/Network.dll。
- Codex运行run_loopback.py最终退出0，PASSED=90 FAILED=0；不是只采信开发自检。豆包此前两次连续90/0日志另存loopback-run1.log与loopback-run2.log。Codex早前89/1日志保留：AcceptSocket未等客户端OnConnSucceed是新增夹具时序缺口，退原任务三处补真实连接成功事件，未加业务重试/门禁，不改90个原断言。
- Codex最终独立同步探针review/run_codex_final.py编译、运行退出0：零发送释放技能TCP0字节；真实Disconnect等待ReleaseSession表锁期间先Attach再放行，最终Peer字典和Session引用均空；原Enabled日志屏障下真实TCP Abandoned也均空。三条件均True。断开项使用现有同步对象反射固定交错及实际CompleteBind，不冒称UDP认证入口测试；另两项走真实Socket/正式GateKcpCtrl。
- 最终正式Network.dll与本轮build字节一致，SHA256=950846B969F8FF43B229A3769473FD18CEBFAC2B41650E0D8E043FD9A2F8ACF2；主Agent仅集成已授权开发源码的编译产物，没有代改业务源码。旧插件备份_temp/network-kcp-20261022/plugin-backup。探针输入前后SHA相同。
- 17份本轮业务源去注释后token与冻结快照完全一致；中文局部候选105→0，新增语法范围无foreach/namespace/三目违规，上游KCP两源与固定提交ZIP逐字一致。上游网络直连核验曾被10054重置，改用已存在固定提交归档核字节，不虚报网络查询成功。
- 没有真实Unity双号、MP扣除、跨通道实体早到、Unity自动重连、弱网性能或HybridCLR/IL2CPP主包运行验收；无Unity MCP工具，未启停用户Unity/服务/保存场景。不能将隔离通过写成线上运行或性能收益。用户GM_Game.dll.bytes仍057685A6…基线，其余用户修改精确排除暂存。
- 本次34路径最终清单、Git提交推送结果见项目修改日志/2026-10-22-KCP-NETWORK-CHANGELOG.md。测试脚本与日志留本_temp，不混入正式业务/资源；WorkBuddy原Hub交付3轮后保留未验收记录，最终修复由豆包本项目local第1～3轮实现。

## [VERIFY][网络][TR-019] 2026-10-22 KCP第三轮复验：78项通过，但两项独立真实复现阻塞

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 125—132 行；历史时点记录，不代表当前仍在执行。

- Codex实际运行compile_kcp.py：Network/GateServer/GameServer/CenterServer均0错误；日志警告计数分别14/14/28/14（含重复输出）。compile_client.py引用_temp新Network.dll，289源0错误。正式插件未替换，不代表Unity现场编译/运行通过。
- run_loopback.py首次因Windows控制台GBK输出UnicodeEncodeError未进入测试；设置PYTHONIOENCODING=utf-8后重跑退出0，PASSED=78 FAILED=0（82条PASS含内部断言）。子进程UTF8被脚本按GBK读取导致中文乱码，已退豆包修脚本，不能将首次失败写成业务失败。
- 审查独立真实Socket探针：启用KCP后零实时发送即按正式顺序清引用关闭，技能11170实际收到TCP14字节；利用既有Enabled日志同步屏障暂停UDP绑定，真实客户端超时后经真实TCP/正式GateKcpCtrl Abandoned清理，再恢复绑定，Peer字典为空但Session仍挂Peer。探针退出0仅表示两故障均复现。DLL前后SHA256保持2C105772143D313B3805D8C93E47198DAE2C2F79C05618A11A270AC102B61266，未修改生产输入。
- 证据_temp/network-kcp-20261022/review/ROUND3_REVIEW.md、repro-result.log、build.log与独立探针工程；唯一消费者、绑定日志脱敏、旧代次真实接线、类型拆分定向核验通过；GateKcpCtrl新增三目违规待改。git diff --check通过，尚未业务提交推送。
- WorkBuddy3轮结束仍未通过，按规则豆包第1轮正在原本项目local会话38448538786202114/run59342789144677378修复。交付后Codex复验，不以自报替测试。真实Unity双号、MP扣除、跨通道时序与自动重连仍未运行；不保存场景、不替用户热更DLL、不启动停止用户服务。

## [VERIFY][网络][TR-020] 2026-10-22 KCP网络改造：首轮审查未通过

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 133—140 行；历史时点记录，不代表当前仍在执行。

- WorkBuddy首轮桌面completed已核实，但开发自报Network/Gate/Unity编译0错及19项回环不等于Codex最终验收。正式插件仍旧版，尚未运行修后Unity双号或部署。
- 只读审查核对KcpClient.Send/Input/Update/PeekSize/Recv与KcpPeer相同操作跨线程访问，UdpNetwork接收和更新线程还同时SnapshotDeliver；第三方KCP只保护段池。空闲清理30秒释放Peer与客户端持续Ready、单次UDP绑定Accept丢失后两端分流不同、UDP载荷含认证密钥均有源码证据。
- 首轮顺序测试先等移动到达才发技能，错误令牌测试使用已绑定conv而命中未知conv；没有真正覆盖连续发送保序及待绑定错误密钥。后续必须补足真实Socket并发、丢包/乱序/重复、阻断/空闲/重绑定及旧通道隔离证据。
- 已沿原Hub任务返工第2轮，未接手代改业务。审查误将一次构建输出至默认bin/obj，已记录且不作为_temp隔离构建证据，不暂存缓存、不擅自清理。
- 完整证据与后续实测以_temp/network-kcp-20261022和项目修改日志/2026-10-22-KCP-NETWORK-CHANGELOG.md为准；未通过，不报告网络改造完成。

## [VERIFY][网络][协作][TR-021] 2026-10-22 KCP网络改造：分支创建，等待开发交付（历史状态）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 141—146 行；历史时点记录，不代表当前仍在执行。

- 实际基线756ca35；git switch -c codex/network-kcp-development成功，git push -u origin codex/network-kcp-development成功；没有提交main或合并操作。
- 读取协作入口、既有双通道方案及已核验源码路线；WorkBuddy provider available，仅表示网关服务可达，不代表开发已完成。Hub任务hub-7e630e54-2ccc-469c-b3d4-e972acfc0d82首轮已派发，最终代码、编译、真实Socket弱网测试和Unity双号均未验收。
- 原有工作区修改以_temp/network-kcp-20261022/baseline-dirty-paths.txt和baseline-user-changes.patch保留，原Network/GM_Game插件摘要在baseline-plugin-hashes.json；后续精确暂存本任务清单，不混入原用户文件。

## [VERIFY][网络][TR-022-FILES] 查询任务的正式文档产物：2026-10-22 网络双通道方案：源码核验，未实施（历史方案轮）

- 该任务记录的正式文档路径：`项目修改日志/2026-10-22-NETWORK-TRANSPORT-DESIGN-CHANGELOG.md`。
- 查询正文、当时状态及原文验证边界见 `Docs/AI/QUERY_TASKS.md` 的 `[TR-022]`；本条只保留文件产物追溯，不新增完成或检查结论。

## [VERIFY][协作][TR-023] 2026-10-22 规则验证：两模型各3轮的接手门槛

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 154—159 行；历史时点记录，不代表当前仍在执行。

- 核对同一问题豆包3轮、WorkBuddy3轮，各为首次1轮加返工2轮；未满必须沿原任务附真实证据返工，双方各3轮仍未通过才切Codex。先满模型转另一模型补充，提前解决停止。
- 核对等待、状态查询、连接失败、自检和核验不计轮，新会话不清零、任务标识及失败证据留痕。用户明确提前接手优先，本条取代历史“不强求固定轮数”描述。
- 本轮只做配置与文档静态验证，不实际运行两模型返工或游戏测试；结果与Git回执见 项目修改日志/2026-10-22-AGENT-FALLBACK-ROUND-LIMIT-CHANGELOG.md。

## [VERIFY][协作][TR-024] 2026-10-22 规则验收：查找先豆包，Codex沿路线核验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 160—165 行；历史时点记录，不代表当前仍在执行。

- 本轮按用户要求把“代码查找、参考项目查找与Bug定位先豆包，Codex沿路线核验，路线错误或证据不足再独立查找”同步到AGENTS.md、AIconfig/config.toml、AIconfig/workflow.md、AIconfig/agent/doubao_executor.md，并更新PROJECT_CONTEXT.md、DECISIONS.md、CURRENT_TASK.md。
- 静态检查：上述规则文本均包含只读查找、路径/行号/调用链/证据交付、Codex最终核验、错误时独立查找、用户例外和超时恢复；配置键可由Python tomli解析，规则值为字符串。仅做配置与文档验证，没有派发本轮豆包业务查找任务，不能把本轮写规则报告成运行查找验收。
- `git diff --check`通过；无业务代码、资源、场景或Unity `.meta`变动。提交前仅精确暂存本轮9文件并核对清单，提交和推送回执写入 `项目修改日志/2026-10-22-DOUBAO-FIRST-SEARCH-RULES-CHANGELOG.md`。

## [VERIFY][协作][TR-025] 2026-10-22 规则验收：额度节省分工与失败回退

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 166—171 行；历史时点记录，不代表当前仍在执行。

- 已确认规则覆盖豆包查找、Codex定向核验、WorkBuddy实现、Codex最终验收，以及确认两者无法解决时由Codex接手业务实现及验证的路径；不强求固定返工轮数。
- 已确认回退条件包含“无可用证据、补丁或通过结果”、停止循环重派、保留失败证据和用户授权覆盖常规委派门槛；测试仅验证配置与文档，不代表本轮运行了豆包或WorkBuddy任务。
- TOML解析与git diff --check通过；本轮仅10份配置与文档。精简任务单/报告、报告复用、超时非失败与接手前结束原执行者均已覆盖，Git回执见 项目修改日志/2026-10-22-AGENT-QUOTA-FALLBACK-RULES-CHANGELOG.md。

## [VERIFY][协作][TR-026] 2026-10-22 续接验收：有效豆包初稿、配置、评分与静态证据完成

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 172—182 行；历史时点记录，不代表当前仍在执行。

- 双方三题15项核心依据逐项回读，均15/15，实际源码行记录于_temp/source-search-comparison-20261022/final-score-verification.json；不把覆盖得分写成全文零错误。发现Codex数据用途称呼问题和豆包DllImport/NavMeshSet/AI事件摘录问题及玩家寻路概括过宽，正式报告列明真实代码。
- 额外核对：MBaseDynamicObstacle.cpp:1144/1145/1169/1200确认NavMeshSet头、memcpy、header.params、tileHeader.tileRef；PINVOKE.cs:2466/2469/2479确认UNITY_IOS和完整EntryPoint；entity_common_tool.cpp:547/553/559确认构造事件和派发；world_map.cpp:692起queue/visited确为BFS。LYF服务源码未查到EntityWorld/Factory常规new入口，未做动态注入排除或运行调试。
- 有效桌面原生配置回读：conversation38448622087397122，run59302623239101442，completed；key5=豆包2.1 Pro、effort7=最高，agentMode1/runtime_type2，本项目ID38435310952198914/本地目录D:/unitypro/LYFMMORGP。证据doubao-clean-native-settings.jsonl与doubao-clean-final-status.json。其他可见会话设置不能代替本次配置。
- 首次多行.cmd被截断，初run取消；同会话完整补发后自报已读本次分析Git提交，整轮污染作废不评分。新会话完整首提示禁读Git/旧答案，声明遵守但无系统级文件访问审计。原生completed及实际文件已核实，中途CLI等待连接报错不视为模型失败。
- 冻结SHA256：Codex 84B3ADB8D062464879C24F4136EFDE0023D7201D185B3276F803437D6C763630；豆包7EC5B1A134B7096094FCD595DB073FE071D898B86EA55EE71180A493A3B6A200。final_verify.py重新计算一致；初稿36/624行。统一开始至文件落盘178.225/1520.731秒，原Codex178.604秒为落盘后结束记录，已校正口径。
- 非盲测、已有开发/预读优势、协作入口含线索、一轮三题、写作篇幅不同、Codex实际型号未独立核实，均已披露；不推导纯模型倍率。豆包自报旧Common DLL反射未独立复跑，不当作本轮重新编译或运行验收。
- 只改5份根正式文档，未改业务/资源/场景，无需重编译未变化代码；未启动Unity/服务器。源码链接、文档清单、初稿保护及Git检查结果见本轮根日志和final-document-check.json。正式分析为项目分析/RO_NAVIGATION_CLIENT_SERVER_SEARCH_COMPARISON.md。
- 以下“未运行/待连接”条目为历史快照，本轮有效任务与比较已完成；正式游戏运行和服务端导航开发未在本轮验收范围内。

## [VERIFY][导航][TR-027-FILES] 查询任务的正式文档产物：2026-10-22 RO两端寻路与源码查找实验：源码核查完成，对比未运行

- 该任务记录的正式文档路径：`项目分析/RO_NAVIGATION_CLIENT_SERVER_SEARCH_COMPARISON.md`、`项目修改日志/2026-10-22-NAVIGATION-SEARCH-COMPARISON-CHANGELOG.md`。
- 查询正文、当时状态及原文验证边界见 `Docs/AI/QUERY_TASKS.md` 的 `[TR-027]`；本条只保留文件产物追溯，不新增完成或检查结论。

## [VERIFY][综合][TR-028] 2026-10-21 RO配置曲线替代采集器：独立源码、资产与原生发布检查通过

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 192—206 行；历史时点记录，不代表当前仍在执行。

- 输入基线1e2e084，分支codex/feature-development；全部证据根_temp/ro-charge-only-20261021/review。WorkBuddy交付后由Codex独立运行；不把开发自报作为通过依据，源输入SHA256在提交前汇总核对。
- prepare_review.py：按作者包→客户端→Editor依赖重建59/289/8份源码，后级使用本轮程序集，全部0错误、既有警告2/38/2。正式热更DLL未替换。
- audit_assets.py：14资产48项全部通过；只允许位移字段及滑步描述变化，动画/音效/特效及其它制作内容逐项与基线保护。六作者Program合法默认字段已归一；3005001、3007001仍无位移。
- audit_trajectory.py：174/174通过；核六技能120Hz全部16/236/300/372/380/80步有符号X/Z、零起点、窗口、作者与Runtime曲线和尺度一致、峰值覆盖。按Hermite评价与样本对照的最大误差约9.17e-7米，小于0.001毫米；3001002实际每轴238键，不能用步数加一猜键数。滑步前向终点4.999852847米。
- verify_displacement.py：链接真实MSkillChargeData/MDisplacementComponent及事件类型，14断言通过；覆盖有符号X/Z、0/90度旋转、峰值后回拉、累计终点、同帧一次消费、结束、中断清理、迟到基线及旧仅Z。Unity数学/宿主/时钟是显式薄边界，非真实碰撞运行。
- prepare_native.py/run_native.py：专用_temp工程覆盖180份当前正式只读输入；Unity2023.2.20f1c1真实编译导入、两原预检拒旧Animation模式、八表现Validate/ReadCharge/ValidatePublished、六表现Export/SaveAssets后再次比较、作者字节保护均通过，共58断言，native-exit.json真实退出0、native-charge-only-result.json pass=true。正式项目不是启动目标。
- 审查子Agent ro_charge_review只读核对：Collector及旧窗口与GUID引用已移除，applyRootMotion=false；Charge/FX/Audio/中断收尾和MMove单次碰撞保持。末轮仅补两处失实中文说明和报告238键，未改变功能。最终输入冻结见final-input-verification.json，精确暂存清单stage-paths.json。
- 真实失败和修复保留：首轮291源15错与零采样被拒；第二轮漏零起点/E的FPS/滑步旧倍率导致174项中14失败；第三轮原生发布因3001001 alignFacing退出1，六旧Animation未执行的Program参数须默认归一；两原预检须承接ReadCharge且保动作时长上限。均退原开发任务修改后重测，未由主代改业务。失败快照round-2-failure、round-3-failure，集中反馈pre-delivery-findings.md及rework-successor-*.json。
- 未完成边界：正式Play、真实角色碰撞和双端同步未运行；未启停用户Play/服务、替正式DLL、改正式场景或预制体。时间轴Program移动预览未扩展，不宣称其支持两轴。后续正常加载后实测六技能正常完成/打断、贴墙及两端位移。
- 逐项正式文件、临时证据与Git结果见 项目修改日志/2026-10-21-RO-CHARGE-ONLY-CHANGELOG.md；设计与复盘见 项目分析/RO_SKILL_MOTION_IMPLEMENTATION.md、项目分析/项目难点.md。

- Git与验收回执：实现d45df25已推送origin/codex/feature-development，远端核SHA一致；Hub第五轮run-89e03f5a-bfed-49f9-a88d-c6094416fa59 completed/accepted/holdsCapacity=false。证据review/git-code-push.json、hub-accepted.json；随后仅三份收尾记录提交，无业务再改。

## [VERIFY][NPC][TR-029] 2026-10-21 Unity NPC待机误报：末稿代码检查通过，现场待复测

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 207—215 行；历史时点记录，不代表当前仍在执行。

- 冻结Editor.log中16:26:24和16:29:22每次进主城各一组：动作状态缺少真实动画配置Idle、表现表未配置待机。沿NpcEntity→PresentDataView→默认状态刷新及原生Controller绑定追踪，NPC无Role位故表现表为空；QuQu原生顶层Idle绑定真实Warrior@Idle.FBX。不是由堆栈附近的玩家绑定成功日志推定玩家出错。
- WorkBuddy桌面原任务两轮，第二轮仅补shortStateHash用途注释；正式只改MActionComponent.cs，SHA256=35a7cf4aa5c558b09ac48d026cdcf573c84504b65b031dda44f28a964813d700。两条原始报错、玩家准备流程、NpcManager和资源均保持。
- 主独立运行compile_client.py unity-idle-final：实际291源Rebuild，0错误38既有警告；无正式DLL替换。review/verify.py直接链接当前MActionComponent与既有接口/枚举，9项通过：NPC原生Idle、NPC缺Idle、玩家缺配置、玩家正常装配、NPC特殊状态失败回退、完整状态路径、缺Animator、怪物不绕过校验、玩家已安装剪辑。Unity及实体依赖为薄替身，非原生帧播放验收。
- 修前源码强制Rebuild后同套开发5例恰有2项失败，分别NPC原生Idle和特殊状态回退，均复现两条错误；其余3项保持。开发两个工程共享obj/bin曾被缓存误导，正式证据均强制重建，主另用独立输出工程复测；未为夹具改业务。输入清单与编译后SHA一致，diff仅55行插入，新增代码规范及diff --check通过。
- Unity UI实际看到3条错误，其中2条游戏Idle、1条MCP SkillSync网络错误。恢复连接只能注册实例，editor/state/read_console及对象查询因ping超时/断连失败；用户Escape停止电脑操作，之后未再发UI输入。保持Play暂停现场，未正式热更、重进主城或保存场景，整项运行验收尚未完成；不可把9项隔离通过写成现场0错误。
- 证据：_temp/unity-idle-bug-20261021/editor-before.log、workbuddy/development-report.md和bounded-cases-*-r2.log、review/inputs.json、runtime.log、baseline-runtime.log、verification.json；全量编译_temp/navigation-test-entry-20261021/review/unity-idle-final。提交前基线7cbe5dd已推送功能分支，修复及回执见 项目修改日志/2026-10-21-CODE-BASELINE-UNITY-IDLE-FIX-CHANGELOG.md。

## [VERIFY][协作][TR-030] 2026-10-21 当前生效：Codex主控、开发子Agent实现、失败退回复测

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 216—222 行；历史时点记录，不代表当前仍在执行。

- 主Agent固定为Codex，负责解读用户需求、设计开发逻辑、拆分和发布任务、最后测试与验收，并维护协作记录及统一集成；不直接代改客户端或服务端业务代码。
- WorkBuddy明确作为本项目外部开发子Agent，与豆包一样可承担开发、修复和授权范围内的本地代码、资源、配置及产物修改；沿已配置的桌面工作模式，项目固定LYFMMORGP／D:/unitypro/LYFMMORGP，不改网页或其他项目。
- 开发子Agent先自检交付；Codex最后核对实际文件并执行必要测试。未通过时将失败证据、复现步骤、预期结果退回原开发子Agent重新开发或修复，完成后Codex复测；不接手代写业务修复，未通过或受阻不标验收完成。
- 已同步AGENTS、总策略、协作流程及两个桌面开发角色；本次仅写入约束，不派发业务任务。检查、逐项文件与提交推送记录见 项目修改日志/2026-10-21-CODEX-DEVELOPMENT-ROLES-CHANGELOG.md。

## [VERIFY][协作][TR-031] 2026-10-21 当前生效：豆包与WorkBuddy采用本项目桌面工作任务

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 223—230 行；历史时点记录，不代表当前仍在执行。

- 用户明确替换旧豆包网页调用：豆包只经E:/doubao-cli/doubao-windows.cmd调用官方桌面，工作任务使用local运行环境、LYFMMORGP项目ID38435310952198914及D:/unitypro/LYFMMORGP目录；新建和返工均显式传项目、目录及AskOnRisk权限，不使用网页、headless或project none。
- WorkBuddy按workbuddy-executor Skill走现有Agent Hub／桌面网关，provider=workbuddy，workspace固定D:/unitypro/LYFMMORGP，创建或复用本项目原生工作会话；两者均使用工作模式，不派普通对话、其他项目或历史测试会话。
- 主Agent继续负责委派、测试审查和验收，外部执行者可在授权范围直接改本地文件；仅交付补丁时由本地开发子Agent落盘。保存各自任务／会话编号，返工沿原任务，已受理超时恢复原轮次。
- 已同步AGENTS、AIconfig总策略与流程、豆包角色和新增WorkBuddy角色，以及桌面CLI示例。旧网页云电脑和隔离实验段落保留为历史，本条为当前规则；本次不额外派发开发任务，不声称项目绑定任务已执行验收。
- 本轮验证与逐项文件、提交推送结果见 项目修改日志/2026-10-21-DESKTOP-EXECUTOR-RULES-CHANGELOG.md；不改游戏业务代码或执行器源码，保留已有用户改动。

## [VERIFY][协作][TR-032] 2026-10-21 豆包官方 Windows 桌面 CLI：真实两轮执行通过

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 231—239 行；历史时点记录，不代表当前仍在执行。

- 用户确认退出后，检查无豆包进程，再带回环调试参数启动原官方客户端；127.0.0.1:9225监听，cdp.available=true，本地runtime=READY，项目列表可读。未切换网页或headless。
- 隔离任务DOUBAO-DESKTOP-WINDOWS-SMOKE-20261021使用project none、_temp/doubao-cli-20261021/live-smoke、no-skills和AskOnRisk。会话38448284654642434，首轮59244078475941122实际创建result.json初稿/7；主读盘确认后，同会话第二轮59243790148828162实际改为修改稿/11/已核对布尔true，两轮completed。
- 主本机解析逐项核对通过、sessions read回读4条消息成功，未代写成果。SHA256=D5A29480EE9BC96FF90E3821B5CBEA17AAE1BA6973EDB51F8DBF9D3B3548112E；证据_temp/doubao-cli-20261021/final-verification.json及同目录两轮结果、原生回读。
- E:/doubao-cli新增私有.env.windows、doubao-windows.cmd和start-doubao-windows.cmd，.git/info/exclude排除机器配置与.local-config；上游源码git status为空。以后普通启动无端口时，保存并托盘退出后用start入口启动，调用用doubao-windows.cmd。
- 使用说明 项目分析/DOUBAO_CLI_WINDOWS.md，逐项变更 项目修改日志/2026-10-21-DOUBAO-CLI-WINDOWS-CHANGELOG.md。未改游戏业务代码、未注册豆包MCP或接入Agent Hub，无项目代码提交推送。
- 上游status/cdp launch含macOS专用命令，Windows改用本机启动入口与cdp status；Goal版本探测为null，不证明客户端过旧。显式Goal、长期运行、开机恢复、附件和完整游戏开发未验。以下待退出记录为历史过程，已由本次结果解除。

## [VERIFY][协作][TR-033] 2026-10-21 豆包桌面 CLI：Windows 配置完成，待退出重启实测

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 240—247 行；历史时点记录，不代表当前仍在执行。

- 用户要求配置并验证官方豆包桌面接入，最新要求优先于旧网页云电脑试验路径。本轮不走网页或 headless，不上传游戏仓库。
- 已克隆 Fullstop000/doubao-cli 到 E:/doubao-cli，基线6d7b1c66430db3bbc095a3d9c1cf15c7ce81f8e6、CLI0.20.0；npm ci成功。上游桌面只声明macOS支持，Windows核心协议仍待实际验证。
- 新增私有 .env.windows、doubao-windows.cmd，忽略机器配置和.local-config；固定官方exe、现有User Data与127.0.0.1:9225。--version与profiles读取通过，无源码改动。
- 当前豆包进程未带调试参数、9225/9226无监听，cdp status available=false。已请求用户保存后托盘退出，未强杀；收到退出确认后带回环参数重新启动，再测任务及同会话文件修改。不能把配置可读当执行通过。
- 逐项文件及证据见 项目修改日志/2026-10-21-DOUBAO-CLI-WINDOWS-CHANGELOG.md；本轮无游戏业务代码修改或提交推送，原有用户改动保持。

## [VERIFY][协作][TR-034] 2026-10-21 豆包需求纠正：查找官方桌面客户端程序接入项目

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 248—254 行；历史时点记录，不代表当前仍在执行。

- 用户纠正：要的是类似WorkBuddy GitHub网关、调用豆包桌面客户端的方案，不是网页云电脑实验。已停止网页实验；仅打开LYFMMORGP项目并查看云电脑菜单，没有发送任何任务或生成云端产物。
- GitHub检索并实际读取Fullstop000/doubao-cli的README与package.json：版本0.20.0，明确支持macOS原生Doubao/DoubaoWork桌面后端，提供会话创建/发送/等待/读取/停止、项目与local/cloud运行环境；桌面要求macOS，Linux/macOS另有headless，不可将headless当Windows桌面已接入。
- Windows检索尚未确认现成可用移植。5201213/doubao-free-api虽描述桌面封装，其当前README主路径是网页Cookie加HTTP反代；dorakoo/doubao-studio是Windows自建Electron工作台驱动网页，不等同官方豆包桌面原生会话。
- 本轮仅只读调查，未安装新项目、未改模型/MCP/业务代码，无提交推送。下一步如用户要求Windows接入，先核对官方桌面客户端和本机协议兼容性，再决定最小适配，不能宣称可直接安装跑通。

## [VERIFY][协作][TR-035] 2026-10-21 WorkBuddy 原生创建与同会话返工：真实通过

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 255—262 行；历史时点记录，不代表当前仍在执行。

- 用户确认已退出，网关启动原生WorkBuddy并开启仅回环18491。复核旧预留c6803bb8-cfc6-47f6-b5da-a423695645b7原生不存在后，使用原ID创建；调用使用正斜杠，返回规范化反斜杠导致严格路径比较报错，但只读workbuddy.db确认同ID/同目录已存在，随后ensure复用并标ready，未重复创建。
- 服务标准安装Set-Acl报SeSecurityPrivilege。已核实data原DACL保护开启且只有本用户/SYSTEM/Administrators FullControl，保留原权限，按上游同参数手动登记persistent-service.json及当前用户worker计划任务；未修改网关源码、未提权、未扩大权限。当前serviceRunning=true/desktopRegistered=true。
- 新任务hub-efab5b5e-f3cf-49d9-b8de-039e55980103，旧failed/DISPATCH_NOT_SENT保留不覆盖。第一轮run-a784aef1-91c3-4233-a49a-849d8879f793真实创建result.json（来源WorkBuddy桌面/阶段初稿/数值7），主读取符合后发同会话修改；第二轮run-9cdb1016-1135-422a-8468-6d3ff235773e完成（阶段修改稿/数值11/已核对布尔true）。两轮sessionId相同、runtimeSource均desktop_queue，最终completed/review=accepted/holdsCapacity=false。
- 主仅派工、读文件与验收，没有代写测试成果。最终SHA256=2E2B84FD95C3FDA125C049C0ADF6BB6EBDB8FA34321B59943F42D49F6FF5B291，证据_temp/workbuddy-gateway-20261021/smoke-final-verification.json。服务状态写入曾短暂EPERM、由现有监督脚本自动重启，末次no_task心跳正常；不声称长期稳定、重启登录恢复或复杂业务通过。
- 本轮修改协作两文件、安装日志及项目难点；新增本机服务注册/监督脚本/计划任务、两轮平台结果和_temp测试文件。完整清单见 项目修改日志/2026-10-21-WORKBUDDY-GATEWAY-SETUP-CHANGELOG.md；未修改业务源码，无代码提交推送。后续可在Codex经$workbuddy-executor指定真实业务目录派工，任务记录可在本机工作台查看。

## [VERIFY][网络][协作][TR-036] 2026-10-21 WorkBuddy 网关真实派工实验：未发送，用户暂不退出桌面

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 263—269 行；历史时点记录，不代表当前仍在执行。

- 用户要求实际实验。按 workbuddy-executor Skill，经 Agent Hub 提交隔离目录创建 result.json 的任务 hub-b765b208-e02b-4885-b75a-629724499592，稳定键 gateway-local-smoke-20261021-01；实际工作目录 _temp/workbuddy-gateway-20261021/live-smoke。
- 平台记录成功，随后 failed / DISPATCH_NOT_SENT，holdsCapacity=false，无执行轮次attemptId。错误明确为运行中的WorkBuddy没有网关回环端口，需要保存后从托盘退出一次。当前仅18492监听，18491未监听；未终止进程，未发送到原生模型，未生成成果，未执行同会话返工或验收。
- 用户明确选择“暂时不退出”，故停止依赖桌面退出的连接测试，保留既有桌面与Hub。后续用户方便退出后先检查原预留工作区映射及失败任务状态，不盲目重复创建未知会话；本次已确认任务未发送。
- 本次新增临时smoke-request.json与空测试目录；修改Docs/AI/CURRENT_TASK.md、Docs/AI/TEST_REPORT.md及 项目修改日志/2026-10-21-WORKBUDDY-GATEWAY-SETUP-CHANGELOG.md。未修改业务源码，无代码提交推送。

## [VERIFY][网络][协作][TR-037] 2026-10-21 WorkBuddy Windows 网关：本机配置与使用说明

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 270—278 行；历史时点记录，不代表当前仍在执行。

- 用户要求拉取 Windows 仓库到 E 盘并配置，随后询问使用方式。已克隆到 E:/codex-workbuddy-gateway-windows，来源提交 e68e3bce8c7fd577b93191add90beb74481dc136。
- 已 npm ci/build，安装 workbuddy-executor Skill，注册 agent-hub-windows/workbuddy-windows MCP，安装当前用户 Hub 计划任务，http://127.0.0.1:18492 健康检查通过。
- 本机 .env 指向 E:/Program Files/WorkBuddy/WorkBuddy.exe、C:/Users/lyf/.workbuddy 与固定 Node 路径。其他 MCP 保留；CLI 将既有 node_repl 的空 args=[] 规范化为省略，实际启动参数仍为空。
- Windows 专项95项中92通过，3项因当前用户无符号链接权限而 EPERM；未修改业务源码或开启系统开发者模式。临时目录改为项目 _temp 后首次出现的斜杠格式断言已通过使用标准 Windows 路径重跑消除。
- 当前仅18492工作台端口在线，18491桌面连接端口未开启，worker服务尚未注册，未派发模型任务、未验证真实领取/执行/返工。首次使用需用户保存工作并从 WorkBuddy 托盘退出一次，再由网关启动并准备原生项目会话。
- 文件清单与证据见 项目修改日志/2026-10-21-WORKBUDDY-GATEWAY-SETUP-CHANGELOG.md；未修改Unity客户端或服务端业务，本次无代码提交或推送。后续需区分平台在线与真实执行通过。

## [VERIFY][协作][TR-038] 2026-10-21 豆包外部子 Agent 调用约束验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 279—287 行；历史时点记录，不代表当前仍在执行。

- 已实际读取 LYFMMORGP 项目主页及其输入区域，项目 URL 为 https://www.doubao.com/chat/workspace/38435310952198914；执行环境菜单实际提供“工作任务 本地电脑”和“工作任务 云电脑”，已选择后者，页面显示项目名及“云电脑”。
- 规则覆盖每次派发/返工的项目与工作模式确认、任务单、外部子 Agent 身份、本地落盘职责、云端/本地不同步边界、结果回收、测试反馈和功能分支集成。
- 本次无业务源码或资源改动，不运行游戏编译；检查项目配置 TOML 可解析、八份交付文件引用和规则一致性、Git diff 空白错误与暂存文件范围，结果写本轮根日志。
- 未启动云电脑开发任务，未验证云端目录/版本/仓库及产物取回；不把此前普通对话 4628 例通过当云电脑验收，不声称云端自动改本地文件。页面证据位于 _temp/doubao-background-browser/subagent-rules-20261021。
- 修改清单及 Git 结果见项目修改日志/2026-10-21-DOUBAO-SUBAGENT-RULES-CHANGELOG.md。原有导航等测试待验事项保持。
- 末次静态检查 PASS：配置 TOML 可解析，八文件清单完整，三份协作记录历史正文逐字保留，暂存差异空白检查通过。规则提交 90d7701 已推送 origin/codex/feature-development，未合 main。

## [VERIFY][NPC][TR-039] 2026-10-21 点地专用测试/NPC初始化：独立PASS与暂存快照通过

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 288—297 行；历史时点记录，不代表当前仍在执行。

- 本次直接代码范围：MainCityMgr.Start调用已有NpcManager.Initialize；新增NavigationClickTest.cs/meta、NavigationClickTestMenu.cs/meta与Test.meta，共6路径。完整变更/累计导航用途见项目分析/导航代码文件清单与测试说明.md。
- 实际NPC二进制完整读取只有4001 QuQu，资源存在且YooAsset采集，出生(60.32,19.16,95.36)，XZ命中导航面Y18.7062；场景manager启用、旧test inactive，正式Start原先没有初始化。开发9静态检查PASS，独立确认表/世界先于主城Start。
- 双方工作区客户端291实际源编译0错误38旧警告，Editor4源0错误2环境警告；独立14审计和26薄边界通过，覆盖无NPC点地、UI/实体按下松开、拖动10像素边界和返回、方向兼容入口、事件池回收、菜单重复启用/DontSave/退出清理。3038冻结仅批准MainCityMgr变化、5新增路径完整，正式Touch/地图/场景/预制体保持，无测试GUID持久挂载。
- 主仅暂存导航hunk和本任务相关文件，将Git index中的290源码及受控引用导入_temp/index/snapshot后编译0错误37警告，Editor4源码0错误1环境警告；不依赖未提交跳跃同步、协议或设置UI变化。完整工作区291与暂存290因SettingPanel未提交而不同，不累加；实际结果见index/client、index/editor。
- 文档/约束补充AGENTS、AIconfig/config.toml及workflow、根日志README；最终Git路径清单写本轮日志。Git曾被08:00遗留0字节index.lock阻挡，确认无git/ssh进程后先备份至本轮_temp，再仅移除该锁完成暂存，没有改工作区内容。独立已完成PASS，后续暂存验证追加委派遇代理403余额不足，主执行原编译方式完成只读验收。
- 无Unity MCP；没有现场Game/NPC/动画/碰撞或原生退出Play验收，不主动启停Play/服务、保存场景或替正式DLL。证据_temp/navigation-test-entry-20261021/{client,npc,review,index}；Git提交推送结果及文件清单见项目修改日志/2026-10-21-NAVIGATION-TEST-GIT-RULES-CHANGELOG.md。用户实际测试需正常重载后菜单临时开启。
- Git代码提交7553c77已成功推送origin/codex/feature-development并建立跟踪；包含78个导航及协作文件，提交快照已独立于未提交内容编译通过。没有合main或推送main。本次仅追加上传结果的三文档收尾，不重复编译未变化代码。

## [VERIFY][导航][TR-040] 2026-10-21 导航业务与实测问题修正：源码及隔离回归通过，Unity现场待验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 298—309 行；历史时点记录，不代表当前仍在执行。

- 用户报告点地位移无动画和容易死路；当前正式导航需求改为NPC/未来任务，地面点击不得再派发移动。上一轮2090/32/26等隔离结果仅历史，不替代此次回归。
- 独立审查已冻结1720路径，含客户端源、场景、NPC预制体、地图两份数据及相关工程；证据_temp/navigation-business-fix-20261021/review/baseline-manifest.json。
- 最终6源：TouchObject去点地/接NPC，NpcCtrl共享原三维10米距离，MDefaultAction识别导航，MNav实际位置消费/止于转角/到达原对话，MNavigation沿RO门户入口位置计价，Triangle仅Center一行注释。MMove/MJump/输入及原技能OnAfterAction尾段逐字保持。
- 双方290实际源末稿0错38既有警告；真实Common73输入绝对化后的_temp副本0错4旧警告，未直接构建正式路径工程。主核1720冻结1714保持，仅6授权源变化，双方290及Common73编译SHA均与当前一致；两份地图、场景和预制体保持，root-final-validation通过。
- 独立49综合通过，覆盖走跑/持续/到达Idle、NPC三维10米与10.005边界/换目标/中断/断岛不对话、地面/UI/拖动、原角色选择、原无目标技能。重提7生产方法18移动项通过；修前冻结完整导航及逐字动画方法3项复现跨角弦/碰撞后丢角/Idle平移。真实主城离网弦样本修后止首角，共线多门户维持整帧预算。
- 截图同区静态地标+09:29:25历史位置定位候选（非用户精确点击）：X68/Z41→39的8面7同高邻接均在，2米直路重心费用20.0648米。按RO运行版门户位置计价后，候选68/33水平77.4351→41.8481米、68/39水平57.0793→35.9979米；三维路长分列见日志。独立宏观道路23项通过，冻结原查询对照证据保留，不用消费问题代替宏观归因。
- 正式末稿同查询夹具2380断言通过，修前2090随路点输出数变化，不累加。主城200路线45844段XYZ走廊覆盖0失败；361局部候选可达354保持，水平路程/直距>1.4者234→0。覆盖与局部比例不保证全图绝对最短或实际角色可过。
- 当前无Unity MCP，未启停Play、保存场景或替正式DLL；实际Unity动画/物理/NPC点击/联机仍待本轮代码正常重载验证。用户实测DLL已更新08:53:50、日志有09:30，本轮不沿用旧时间推断未加载。台阶0.3/烘焙0.75差异保持，未证为同高道路绕楼根因。
- 主要证据本轮_temp下review/final-review.json、final-audit.json、macro-before/final、client/final-with-query、query/final-query-report.json和local-comparison.json；完整文件/失败尝试/范围与后续见项目修改日志/2026-10-21-NAVIGATION-BUSINESS-FIX-CHANGELOG.md、项目分析/RO_NAVIGATION_IMPLEMENTATION.md及项目分析/项目难点.md。

## [VERIFY][场景实体][TR-041] 2026-10-21 SceneEntity目录整理

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 310—316 行；历史时点记录，不代表当前仍在执行。

- 49源移动到Base5/Entity15/Component24/Skill2/World3；不改源码。GameServer.csproj只替换49处编译路径。
- 主以迁移前274份服务端.cs/.csproj独立冻结验收：迁移完成时49源新路径SHA全部一致；末次48份仍一致，NavigationComponent仅随后构造函数换行，内存合回该处即匹配原SHA，保留当前文件，不推断写入者。旧路径全部消失，其余224输入保持；实际文件和49编译项一一对应，项目文件与逐字路径替换预期完全一致。
- 真实GameServer与Common/Network依赖最终编译通过：0错误、11条既有警告，最终输出在_temp。早期首次MSB3540、第二次导入顺序错误导致12份既有bin产物误写；已从原SHA匹配的obj副本备份并恢复。主逐份读当前字节核对12/12原SHA，范围外更宽冻结439/439保持；root-final-validation最终八项通过，errors为空，严格字节差异仍如实列出上述排版观察。
- 本轮不新增业务单测、不启动服务器。无需对源码无语义变化重复构建；无剩余目录整理阻断。证据_temp/sceneentity-organize-20261021（dev-output-incident、dev-output-restore-results、dev-navigation-external-change-evidence、root-final-validation），日志项目修改日志/2026-10-21-SCENEENTITY-DIRECTORY-CHANGELOG.md；事故复盘见项目分析/项目难点.md。

## [VERIFY][导航][TR-042] 2026-10-21 RO运行导航：末稿独立PASS，真实Unity运行待验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 317—330 行；历史时点记录，不代表当前仍在执行。

- 21正式路径完成：共享查询5类型/meta与Common同源链接；客户端导航组件/移动/事件/输入/场景加载、新Mgr/meta及StreamingAssets同份主城数据。主与独立核1941冻结1935保持，仅6授权旧文件变化，无删除或额外变化；root-final-validation五项全true，双方编译输入与末稿SHA一致。
- 开发/独立完整客户端290源0错误38既有警告；真实Common.csproj独立0错误4既有警告，输出全在_temp。末稿MNavigationComponent SHA8884c915…8807f；原OnAfterAction尾段逐字保持。
- 独立查询末稿2090断言：平面直线、L形/孔洞绕障、重复XYZ、长短边/浮点接缝、点接触/断岛、上下层、坡面高度、整段落面采样；真实主城12条非精确共边接缝及9条长路线通过。拓扑4304分量、最大40682面、1459无向接缝；保留原数据，不据分量数声称烘焙缺陷。
- 独立直接编译完整MNav/Mgr/真实事件池32断言：首末帧、即时无目标、跨帧clone/回收、到达动作、中断不施法、失败/切图、UseAfterAction=false及RO7米隔层回归。实体更新7个逐字生产方法派发链10项通过，实测Move先于普通Nav.Update，到达流程随后执行；非仅手动调用LateUpdate。
- Move/Touch26项为开发夹具的独立重编复跑（不称独立设计），7方法逐字核生产；覆盖预算/Shift/0毫秒、一次碰撞提交合并重力/根/程序位移、跳跃/技能移动限制、方向接管、UI/拖动/实体选择/射线失败。物理、动画、网络为薄边界替代，不作正式Player验收。所有最终项不与早阶段断言累加。
- 正式MainCityMgr出生配置(62.1493416,19.4139996,80.6689758)投影到Y18.80104，附近X+10/Z+10/X-8米三路完整可达（3/6/12路点）。这是配置样本，不是当前主角内存位置或真实碰撞结果。
- 失败及修正：首稿遗漏RO SELECT_TARGET_DISTANCE_HEIGHT=6.0f，按原常量恢复，修前7米隔层误施法已以开发原始前缀+冻结尾段重建复现（非完整修前快照），修后拒绝。现有MEntity不派发组件LateUpdate，最小在导航Update接现有顺序；未改实体生命周期。UseAfterAction末次修改后主首次哈希审计失败，两方重编并补false专项后末稿一致通过。首次夹具失败记录与provenance保留。
- 性能仅临时.NET观察：开发主城构建658毫秒，五长路线首26/热4～14毫秒；独立另一组构建约863毫秒、9长路线2～8毫秒，非Unity帧率收益。早稿64～129毫秒查询已被面AABB筛选替代，不当末稿结果。
- 原公共bytes和随包副本SHA4659a2c3…3984b84逐字一致。原场景、NavMesh、Editor预览、协议插件保持；不替换Unity Common DLL，未重启服务器/发布。
- Unity工具当前缺失；只读Editor.log更新08:27:14、Unity GM_Game.dll仍10月20日21:16:01，早于本轮末源，无法确认原生编译/加载。未清Console或用历史Console0代替新验收。真实主城点击/碰撞/坡地/动画及联机仍待正常重载后验证。
- 证据_temp/navigation-runtime-20261021/review/final-review.json、root-final-validation.json及client/query各专项；专题项目分析/RO_NAVIGATION_IMPLEMENTATION.md，日志项目修改日志/2026-10-21-RO-NAVIGATION-CHANGELOG.md。

## [VERIFY][综合][TR-043] 2026-10-21 草地正式视觉通过，最后运行专项及原生恢复未执行

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 331—340 行；历史时点记录，不代表当前仍在执行。

- 用户最新d332截图为原生覆盖而自绘漏显的真实回归。当前只委派冻结/诊断，不以旧数据一致或障碍图通过证明该位置正确。
- 验收需该位置成功原生/旧自绘/新自绘同镜头实图，核该面数据存在与深度偏差，再验上一轮柱树石遮挡保持；原生开关true但无蓝图无效。当前未确认根因或修复完成。
- 已取得成功grass-native（临时同资产123977面，finally卸载/Surface仍inactive）与grass-before；公共面52401存在且内部低地表0.771/0.324米，三个顶点反高地表，解释深度漏显。正式两文件Preview+原Shader修有限贴地深度，不改几何。
- 主已目检正式grass-final、grass-rotated-final、obstacle-final：草地缺口补齐、镜头轻转仍生效、柱树石洞口/遮挡保持。当前等待最后保真/资源恢复/Console证据；用户追加原生不显示，开发按授权恢复MainCityNavigation必要激活、隐藏自绘并留原生可见。
- 上条恢复属于计划，实际未执行：工具层随后失去Unity工具，127.0.0.1:8082/mcp连接失败。原生正式对象最后已知inactive，自绘仍是最后有效观察状态；临时注册原生参考的finally移除已执行，不能把参考图当正式组件恢复。
- 正式两文件视觉/静态已核，新增资源销毁与DepthTextureMode恢复有实现，但最后生产运行专项/最终Console未取得；后补两行注释没有新编译结果。不将上一轮清理/Console0或本轮实验图冒充当前末稿全面验收。恢复连接后继续必要专项。
- 独立现状报告部分通过：429冻结427保持，仅Preview/原Shader变化、公共文件保持，diff检查通过。最终SHA见本轮日志及final-source-hashes；861点max1.590706只为当时会话转录（未落原源码/JSON），正式双景原图和两点depth-samples为已保存证据。当前磁盘Editor.log另有用户导出非空导航及openupm网络错误，未称当前Console0或扩大修复范围。

## [VERIFY][综合][TR-044] 2026-10-20 行走网格可读性修正：前轮结果，不含新缺口

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 341—349 行；历史时点记录，不代表当前仍在执行。

- 用户最新参考图显示道路蓝面与障碍边界清楚，上版自绘却有遮挡层杂线和过淡面；其可见/数据/清理测试不能替代本轮可读性。
- 必须含树石柱场景的同镜头原生参考/自绘实图对照，检查道路连续、障碍遮挡与边界，不以无障碍空地或仅开关true验收。当前仅已下发开发/审查，未声明原因完全证实或修复完成。
- 同镜头native-reference/self-before/self-visible-depth复现旧Greater后线穿透柱树，去透画且蓝面写深度后道路连续和障碍洞清楚。末稿共面线Shader补淡内部边与深轮廓，主与独立实际目检首轮末图。用户随后移动镜头覆盖self-final，最终固定self-current/hidden-current为当前显隐证据；native-current无蓝不作为成功原生对照，不声称末稿逐像素一致。
- final-check-result：270735顶点及123977面逐项保持，UInt32；121039内部边/129853轮廓边与独立文件推导几何边集完全相同，无重复/跨子网格重复。重复单buffer、Hide/Close清理、原生false/true恢复均true，ShaderHasError false，final-console错误警告0。
- 新增边统计初版UInt64键导致真实Editor长时间占用；离线250892唯一边仅14001hash/最大链36200。改Vector2Int后实际重新加载，四次Show共1765毫秒、末次406毫秒；仅本机预览生成测量，不作运行帧率收益。未强杀或保存场景，旧调用自然恢复后编译新稿。
- 保护：427冻结425保持，仅两Editor源及新增专用Editor shader/meta，公共文件SHA4659…3984b84保持。最终主城非Play/dirty原true，自绘true/原生false，原场景/导航字段恢复，用户新镜头保持。证据walkable-readable下final-check、line-set-verification、final-state、self-current/hidden-current、root-final-validation及review；不改烘焙/角色可达/移动。

## [VERIFY][综合][TR-045] 2026-10-20 自绘公共文件网格：前轮技术结果

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 350—358 行；历史时点记录，不代表当前仍在执行。

- 用户已纠正必须自绘，只参考NavMeshSurface外观。上一轮原生显示、像素对照和开关测试仅证明原生接入技术流程，不证明完成用户要求。
- 本轮需核公共文件反读、32位大网格、透明填充/清晰边线、真实Scene可见、重复显示/隐藏/关闭清理、编译及原数据保持；不得用原生开关true或强制Gizmos离屏图替代真实用户显示验收。
- 本轮结果：正式ShowSelectedMap反读文件；270735顶点XYZ、123977面索引和743862线端点逐项true，两Mesh实际UInt32。重复Show单自绘buffer，旧Mesh销毁，原生false/true初值恢复、Hide/Close销毁Mesh/材质及buffer通过；窗口_navMeshData空时仍可预览，测试后原选择恢复。
- CaptureSceneView实际Scene GUI同镜头显隐图已主/独立目检，透明面/深线出现、地面纹理保留；不使用首m_SceneTargetTexture纯黑轴图。最终899×377图直接按真实窗口大小采集，不伪造显示内容，采集源码和原始图保留。
- 真实RequestScriptCompilation域重载后，原生恢复true、预览隐藏、旧Mesh0/旧自绘指令0；末稿正式入口重新显示，自绘true/nativefalse。最后Console错误警告0、非Play/dirty原true、镜头保持。427冻结仅两Editor源及asmdef变化，公共文件SHA4659a2c3…3984b84保持。
- 证据：_temp/navigation-pathmesh-view-20261020/self-draw/lifecycle-result.json、after-reload-result.json、final-console.json、scene-visible.png、scene-hidden.png、root-final-validation.json及review。边界：未改烘焙/数据/移动/协议，未算角色连通范围或测试服务器路径，树冠石顶仍是文件原有内容。

## [VERIFY][综合][TR-046] 2026-10-20 原生接入：历史技术结果，已按用户纠正返工

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 359—371 行；历史时点记录，不代表当前仍在执行。

- 用户明确全文件蓝面显示无法用来观察寻路网格。前轮真实Scene覆盖一致性仅证明显示了同一份数据，不证明网格可读性或角色可达范围符合需求。
- 本轮先确认连通区域/网格边界需求并只读现场、冻结输入，答案前不修改依赖方案的业务代码。证据_temp/navigation-pathmesh-view-20261020，最终验证按明确方案补齐。
- readonly-scene-state.json与readonly-local-role-final.json核当前主城非Play/dirty=true、预览false/原生true/123977面，Global.Instance为空、无运行LocalRole；选择Surface原点。失活角色不当实际玩家，活动Controller为怪物不作玩家种子。未修改源码/场景/镜头/开关，纯文档与只读核查不重复编译。
- 独立冻结427路径及两源快照后开发；当前结果按本轮原生接入验收，不沿用前轮自绘PASS替代。
- 用户已指定NavMeshSurface为参考。独立核包2.0.6：NavMeshSurfaceEditor选中通过FlagAsInSelectionHierarchy标记，NavigationOverlay控制showNavMesh/showOnlySelectedSurfaces；无当前位置连通查询。现实施直接复用原生显示，待真实Surface参考/窗口打开同镜头一致、生命周期和保护检查，不再按自绘全蓝mask作为验收。
- surface-window-compare.cs确实调用正式ShowSelectedSurface，固定同Scene相机和镜头，经完整原生绘制得到surface-compare-0/1。主/独立目检黑色网格线与障碍边界一致；1350×525像素全部通道差≤2，平均0.1652（surface-image-comparison.json），不宣称地图可达性正确率。
- native-lifecycle-result.json四开关组合的Show/重复Show/Hide12项全true、Close恢复true、oldCameraCallbacks=0。FrameSelected实测空Surface定位原点/size0.866，按sourceBounds正确变换后定位中心(47.26,272.43,-7.50)/size772.657837，测试后恢复当时镜头。
- 编译/域重载后新原生入口加载、compiled-console及final-console均0条。final-delivery-state.json记录实际重载恢复后重新Show，末态主城非Play/dirty原true/123977面/原生true/仅所选true/选中MainCityNavigation/旧相机回调0。用户期间更改镜头，末pivot(-19.11,-20.02,111.21)不硬恢复旧值。
- 开发13保护输入保持；仅两Editor源及asmdef现有程序集引用必要变化，独立与主末稿SHA/保护复核见review/final-review.json、root-final-validation.json。
- 只读采集样例证实RenderMeshes采第7层部分Elven_Tree及Rock面，自身可能无Collider；原生仍含树冠/石顶，不声称烘焙质量已修。本次画面来源为Surface资产，公共bytes导出保持、未把原资产画面冒充文件反读，未增加BFS/角色移动/服务器寻路。

## [VERIFY][综合][TR-047] 2026-10-20 真实主城范围漏显：前轮主城对照及原生专项通过

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 372—381 行；历史时点记录，不代表当前仍在执行。

- 用户截图反馈表明前轮隔离深度与数组一致性不足以验收真实Scene显示，本轮补同现场同镜头原生/预览对照，区分实际深度状态、渲染路径与烘焙/位姿问题；不提前宣称根因或修复。
- 开发独占Unity，独立审查冻结及只读证据；当前证据_temp/navigation-preview-scene-fix-20261020，真实主城结果与保护检查已补齐。独立与主复核427冻结425保持，仅两授权源变化，末稿SHA与交付相符，限定git diff --check通过。
- 用户明确关注范围。先以Internal_DrawCamera+Internal_FinishDrawingCamera完整Editor流程固定同Scene镜头取得四态；原生单开仍覆盖旧文件单开的中央露草区域，双开精确复现用户图，根因为LessEqual漏显和原生叠加。不能以普通Camera.Render空图作原生对照。
- 本轮当前NavMesh.CalculateTriangulation与文件三数组逐项true（270735顶点/123977面），Surface/asset位姿均0/identity，见current-geometry-comparison.json。修后单pass统一叠加；独立相对青蓝mask和2px线边容差：原生626252像素/文件625132像素、缺81/多146，仅作当前镜头覆盖核验，非导航正确率。修后真实图single-pass-mode-2.png已主/独立目检，中央缺口不再裸露底图。
- final-game-result.json可见/遮挡位置均显示区域、Game排除三项true。final-lifecycle-result.json原生原值false/true重复Show/Hide六项true及Close恢复true，实际域重载后原生恢复true后正式Show再次关闭。final-console.json为0条；首次直访内部showNavMesh产生CS0117后最小改Editor缓存PropertyInfo，末稿编译通过。
- delivery-state.json主城非Play/dirty原true/123977面/回调1/预览true/原生false并保留恢复true，镜头pivot、rotation、size60保持。本轮没有保存/切图/重烘焙或改公共文件。开发13保护输入保持，独立保护与主末稿SHA汇总root-final-validation.json。
- 颜色仍略深、未复制原生线框，不作逐像素视觉一致；未做当前位置可达计算、移动接入、Player发布或服务端寻路验收。用户已明确范围诉求，停止颜色深入排查，保留原生烘焙/角色规格边界。

## [VERIFY][导航][TR-048] 2026-10-20 可行走面预览：前轮原生隔离专项及独立审查通过

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 382—390 行；历史时点记录，不代表当前仍在执行。

- 用户追加要求像Unity导航显示可走位置，现开展公共文件/烘焙资产保真与Agent来源核查、Scene相机阶段绘制和深度遮挡验证、UInt32缓存与显隐/关闭清理。开发独占MCP，审查426路径冻结后只读核源码/证据。
- 真实Unity编译/域重载通过，开发报告Console0错误警告；文件与原NavMeshData的顶点/索引/区域逐项一致（270735顶点/123977面）。末稿UInt32 Mesh数据保真；重复Show释放旧Mesh/Material且仅1个回调，Hide后0回调、Close销毁通过。
- 专用PreviewScene用生产Show/Camera.onPostRender与Camera.Render验证实际深度：前方遮挡像素(1,0,0)，移到后方后透明面混色(0.651,0.247,0.380)，Game相机保持(1,0,0)，三项通过。独立读测试源码/原始JSON及PNG像素确认64×64实心填充，非仅中心像素或线框；此图不作主城最终效果证据。
- 独立426冻结路径424保持，仅两授权Editor源变化；开发13保护输入保持。主复核两源和证据，末稿SHA及公共文件SHA汇总_temp/navigation-walkable-preview-20261020/root-final-validation.json，独立结论review/final-review.json为PASS。
- 末态主城非Play/原dirty=true/全局导航0/预览visible=true，未保存或切换场景、重烘焙、改公共文件或插件。原资产真实参数Agent0/r0.29/h2.1/slope45/climb0.75；MJS台阶0.3米，现场无活动玩家，不声称角色移动严格匹配。文件仅含AgentTypeID。
- 主城最终合成截图未独立目检：工具拒绝项目根_temp输出目录，未在其他位置创建截图，不再扩展截图方案。未新增当前位置可达查询、动态阻挡、实际移动接入、独立烘焙、Player/服务端运行验收或性能收益结论。本轮无代码阻断，用户可直接在Scene查看已显示的填充面。

## [VERIFY][构建][TR-049] 2026-10-20 导出文件Scene预览：编译与原生数据/生命周期验证通过

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 391—399 行；历史时点记录，不代表当前仍在执行。

- 当前最小范围：Editor窗口读取公共文件绘制Scene网格，支持显隐/定位，既有烘焙/导出/服务器业务保持。开发独占MCP，独立测试只核磁盘和原生证据。
- 末稿真实Unity编译/域重载通过；两源静态约束/中文注释和直接Load文件通过。Handles原生线框缓存32位int索引，不再使用首稿Mesh/Material。270735顶点和743862边端点与正式文件逐项相符，123977面；重复显示监听仅1个，隐藏/窗口关闭清引用及回调通过。原始结果_temp/navigation-preview-20261020/native-final.json，首稿native-verification.json不冒充末稿。
- 末态正式窗口/预览重新打开visibleAtDelivery=true，Scene_MainCity非Play/dirty=true/1场景/全局导航0，Console错误0。dirty=true是本轮原有用户基线，不借上轮Bootfalse推断本轮改脏。未保存/切换场景、替换数据、改插件或启动Play。
- 开发冻结1307/独立423输入复核仅授权窗口源码改变；新增Preview.cs/meta，原场景/NavMesh/完整Plugins/Packages/三共享源/Common工程及公共文件保持。本轮用户重导文件SHA=4659a2c35d5fdbbdd0dbb084321328408f560d5fd239b084b3dfbc7cd3984b84，与首次导出61D6历史不同，不误记为本轮预览修改。
- 失败与验证边界：新类型未被资产库导入时曾CS0246，正常完整刷新后原生编译通过；首稿Graphics.DrawMeshNow只获相机中间缓冲图，无法证明最终合成，末稿简化为原生Handles。桌面像素scene-local.png/scene-hidden.png受其他窗口遮挡无效，scene-overview.png是首稿缓冲，不作末稿视觉证据；短临时窗口激活探针API不存在，仅探针未编译执行，没有进入正式源。未继续开发截图框架，不声称完成最终画面对齐目检或测得FPS收益。
- 主核两源、末稿原始结果和3路径SHA，汇总_temp/navigation-preview-20261020/root-final-validation.json；使用说明项目分析/UNITY_NAVIGATION_EXPORT.md，日志项目修改日志/2026-10-20-NAVIGATION-SCENE-PREVIEW-CHANGELOG.md。项目难点已补本轮渲染路径取舍；后续用户在Scene查看实际覆盖范围。

## [VERIFY][导航][TR-050] 2026-10-20 自有导航导出工具：验收通过

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 400—411 行；历史时点记录，不代表当前仍在执行。

- 已验证真实主城三角网导出、世界坐标/高度/区域/索引保真、同份正式文件Unity及.NET4.8读回、换地图可复用、已有导航网格拒绝混入及用户原资源保持。独立冻结1108输入，证据_temp/navigation-export-20261020/review。
- 本轮不重建Unity内部polygon/link和邻接，不实现完整导航查询/A*、协议或移动校验。测试临时夹具只放_temp或专用测试目录，不挂正式场景。
- Unity连接由客户端Agent独占，主和审查不并发操作；按unity-mcp-orchestrator先读state/resource，不主动启停Play/保存用户场景。
- Unity原生编译/域重载后Editor工具加载；正式主城270735顶点/123977三角形逐XYZ、索引、区域写出和反读一致。文件5,232,487字节，SHA256=61D6AB8FE38AB80DD59FCBC425E652AC637FB7DE30D45CD6BC4987853F3CBB4C；纯.NET读回重写逐字节一致。
- 两张原生专用地图各32顶点16三角，非零位置与yaw37/-21、双层高度、已有全局网格拒绝并保留原网格、真实写入失败finally清理共10项通过。独立实际.NET4.8再读主城及两原生小图，末稿107断言通过；共享专项含主城220断言通过，阶段95/99/218不与末稿累加。两方Common工程0错误、4条既有警告。
- 五源约束/中文注释、同源链接及1108冻结输入核查通过，仅授权Common.csproj变化；六客户端保护文件SHA保持。主复核16正式代码/配置/资源路径末稿及证据，见_temp/navigation-export-20261020/root-final-validation.json。主城资源、既有业务和Unity插件未改。
- 失败及边界：首次NavMeshData.agentTypeID未公开导致CS1061，最小改用编辑器SerializedObject读取真实字段后编译通过；隔离首夹具错误要求跨注册索引排序稳定，改成世界几何集合核对，生产代码未迎合测试。一次只读场景探针误用LoadAllAssetsAtPath(.unity)，遗留Do not use ReadObjectThreaded on scene objects；未清Console，不能声称最终全Console0。
- 最终Boot非Play/dirty=false/全局0三角，主城Surface与父根及资产位姿均0/identity。未做Player包、发布、服务器启动加载、寻路算法、邻接/接缝或正式两端移动验收。后续新Common进Unity须先明确同名类型归属，当前旧插件不含新增类型。
- 使用说明项目分析/UNITY_NAVIGATION_EXPORT.md；日志项目修改日志/2026-10-20-NAVIGATION-EXPORT-TOOL-CHANGELOG.md及项目修改日志/2026-10-20-NAVIGATION-EXPORT-INDEPENDENT-REVIEW.md；跨模块真实难点已加入项目分析/项目难点.md。

## [VERIFY][导航][TR-051] 2026-10-20 两端共用导航接入步骤：静态核查

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 412—418 行；历史时点记录，不代表当前仍在执行。

- 当前包清单AI Navigation 2.0.6、主城NavMeshSurface已绑定0241a054f78c9b4489b3f2b28088524c，对应新导航asset/meta（17,755,640字节），用户自行生成的资源未修改。旧121行全局引用0不能单独判断Surface未烘焙；本轮已纠正时点说明。
- 服务端工程及公共工程目标v4.8；已读ROMoveComponent、NavigationComponent、SceneManager与客户端导航入口，未发现Detour/Recast依赖。方案按兼容查询→原几何导出→公共烘焙→服务端查询→客户端移动分阶段，区分原几何和已收缩的Unity三角网，避免重复按角色半径缩边。
- 关键文件SHA、Surface/资源GUID、包及框架定位和本轮新增文档段编码核验见_temp/shared-navigation-plan-20261020/validation.json。一次猜测Scene目录失败后定位manger/SceneManager，只读无修改；DotRecast上游csproj请求20秒超时，兼容性未确认，不据此定库/升级或安装。
- 本轮仅4文档；未做业务编译、Unity运行/查询、库兼容试验或真实两端寻路，方案不能作实现验收。后续先在隔离测试中核候选后端和同份小网格加载，再委派正式开发；本轮没有已实现寻路收益或防作弊结论。

## [VERIFY][导航][TR-053-FILES] 查询任务的正式文档产物：2026-10-20 RO导航烘焙：静态源码与文档核查

- 该任务记录的正式文档路径：`项目分析/RO_PATHFINDING_ANALYSIS.md`。
- 查询正文、当时状态及原文验证边界见 `Docs/AI/QUERY_TASKS.md` 的 `[TR-053]`；本条只保留文件产物追溯，不新增完成或检查结论。

## [VERIFY][导航][TR-054-FILES] 查询任务的正式文档产物：2026-10-20 RO寻路整理：源码与文档核查通过

- 该任务记录的正式文档路径：`项目分析/RO_PATHFINDING_ANALYSIS.md`、`项目修改日志/2026-10-20-RO-PATHFINDING-ANALYSIS-CHANGELOG.md`。
- 查询正文、当时状态及原文验证边界见 `Docs/AI/QUERY_TASKS.md` 的 `[TR-054]`；本条只保留文件产物追溯，不新增完成或检查结论。

## [VERIFY][综合][TR-055] 2026-10-20 顶部关闭与菜单展开：源码及隔离验证通过

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 439—449 行；历史时点记录，不代表当前仍在执行。

- 检查正式界面已有顶部关闭按钮是否接入现有关闭流程，只补真实缺失；四个功能入口移除自动收起，保手动开关与初始状态。
- 开发/独立审查先调查冻结，再核源码编译、按钮引用/关闭行为、镜头恢复及菜单状态；不主动启停Play、保存场景或发布。证据_temp/ui-close-menu-stay-20261020。
- 末稿两方281源编译0错误、38既有警告，无新增签名；独立18项PASS，主11项汇总通过。162冻结路径只4正式文件改变，Root5530对象无增删/5527块逐字保持，仅背包/角色/技能3Button.m_OnClick补实际目标CloseWindow；其余持久事件和MainView/CameraMgr保持。四入口仅取消自动收起，五菜单事件增删成对，初始化收起/箭头手动切换保留。
- 新背包/角色CloseWindow先base隐藏，再分别调用原KnapsackWindowAngle(this)/RoleAttrWindowAngle(this)，沿B/I关闭分支更新背包状态、属性镜头及保存视角，不改CameraMgr；技能使用继承WindowBase关闭。原有设置/对话/商城/服务器列表/注册返回/购买确认弹窗保持，常驻/流程无顶部关闭入口者不新造按钮，遗弃背包仅记录。
- 原生PreviewScene失活父节点下克隆6主城窗口，临时将克隆监听设EditorAndRuntime后Invoke，6/6实际目标正确且activeSelf=false，正式源缺脚本0、初始菜单收起，主城前后dirty=false。CameraMgr单例临时隔离并finally恢复；当前Play中两个新overrideLoaded=false，因此该结果只证接线与基类隐藏，不能证明原生新镜头恢复。
- 逐字提取14段生产方法专项28项通过：四入口重复开关不收菜单、背包关闭更新镜头状态、角色/背包组合800/460偏移及保存视角恢复、B/I重开、箭头手动收起。Unity对象、ShowMainWindow刷新与补间为薄边界，不称正式Play画面。当前运行需正常编译重载后查看新逻辑，未主动启停/保存/构建发布。
- 独立夹具最初误要求技能监听声明SkillInfoWindow而非实际继承WindowBase，仅修临时审计，first报告保留；主误猜CameraMgr目录后定位Manger，只读失败无业务返工。保护输入、两方281编译输入和4正式末稿SHA保持，详见root-final-validation.json；日志项目修改日志/2026-10-20-UI-CLOSE-MENU-STAY-CHANGELOG.md。
- 交接末态主城Play/非编译/dirty=false、临时残留0、Console0；CameraMgr.Instance为正式FreeLook Camera(id242194、非Preview)。独立method-evidence-review.json确认14段真实方法SHA/原文全匹配、finally恢复及业务冻结保持；开发delivery-verification.json七项通过。方法初报13已纠正14，专题项目分析/MAIN_FUNCTION_MENU_UI.md含窗口关闭清单。

## [VERIFY][综合][TR-056] 2026-10-20 展开功能组背景透明：验收通过

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 450—457 行；历史时点记录，不代表当前仍在执行。

- 本轮只改用户截图中的展开组深色底板透明；冻结四个无关源码/场景输入，核正式Root精确差分及原生展开预览，不重复业务编译。
- 客户端正式仅改Root五个Image颜色alpha归零；主独立逐行差分核对5行、原RGB及全部其余字节保持，5530对象无增删、5525块保持，四冻结输入保持。主汇总4项通过，Root SHA459b275fb59c32946eff834aaa4df491e2b85edda6ac9f14b354e210a0fbf710。
- 原生一次隔离展开预览已主目检：组底板和四入口底板均alpha0，四按钮raycast=true、组底板raycast=false、原箭头/中文/图标/布局保持，源默认收起、缺脚本0。当前主城Play且新箭头字段已加载，检查前后dirty=false；本轮不主动启停或保存，未构建发布，纯资源不重复编译。
- 预览手动显示临时克隆，不作为正式点击/包端验收；重新加载正式UIRoot后查看。证据_temp/main-menu-content-transparent-20261020/client及root-final-validation.json；专题项目分析/MAIN_FUNCTION_MENU_UI.md，日志项目修改日志/2026-10-20-MAIN-MENU-CONTENT-TRANSPARENT-CHANGELOG.md。
- 开发交接末态原始响应已复核：主城Play、非编译、dirty=false、临时预览残留0、Console0；没有额外重复采样或切换运行模式。

## [VERIFY][构建][TR-057] 2026-10-20 透明箭头功能开关：编译与隔离预览通过

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 458—467 行；历史时点记录，不代表当前仍在执行。

- 只替换功能开关文字及背景，不改变四入口与组背景；默认收起箭头向下，展开向上，底板透明同时保射线点击区域。
- 开发及独立完整281源编译均0错误、38既有警告且无新增警告签名；独立21项PASS。正式仅MainView.cs/UIRoot.prefab变化：5530对象无增删、5525块逐字保持、5必要块为接线/Label改Image/箭头尺寸/开关透明；四入口与展开组底板、MainCtrl、设置2304对象保持。原CRLF形态保持，新增代码约束通过。
- 现有Common.png/meta与RO源逐字相同，箭头UI_Common_Shouzhanjiantou_03.png、GUID22781455b3685ea4bae5d7ef77d885ea、fileID21300460。Sprite原20×14，显示28×20并保持比例。原生正式源缺脚本0，开关alpha0/raycast=true、箭头不拦射线；两态GraphicRaycaster中心与近两角共6点命中，108×38点击区保持。主与审查目检两态裁图通过。
- 主13项汇总PASS，11保护输入、两正式末稿及双方281编译输入SHA保持；MainView末稿0ce28ce80803ab6e0c78a04e59524f0e444a53be8f72e024fc30ceac2b0ae4a4，Root末稿71c332e2f2fc6ab9d410926b78f4e05d0f0b5045ea30352860a821d7fc2f80f8。主首次猜测独立编译子目录compile-final读取失败后定位实际final目录，只是证据路径读取错误，无业务返工。
- 原生检查当前主城Play、新箭头字段False，前后dirty=false；未主动启停Play/保存场景。预览在PreviewScene克隆中手动切容器与箭头，六点命中是原生射线测试，不等于正式MainView新方法或MainCtrl点击验收。待正常编译并重新加载UIRoot后正式查看，包端未构建发布。
- 开发交接末次只读状态为主城Play、非编译、dirty=false、loadedArrowField=false、previewLeftovers=0，末Console0；主核原始final-state-result.json/console-final.json，与上述边界一致，未额外切换运行状态。
- 临时证据_temp/main-menu-arrow-20261020，独立review/final-audit.json、主root-final-validation.json；根日志项目修改日志/2026-10-20-MAIN-MENU-ARROW-CHANGELOG.md，专题项目分析/MAIN_FUNCTION_MENU_UI.md。

## [VERIFY][界面][TR-058] 2026-10-20 主菜单RO图标：资源验收完成

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 468—475 行；历史时点记录，不代表当前仍在执行。

- 用户只要求改用RO图标，沿用左上菜单与现有入口。本轮验证真实RO Sprite对应、必要引用与等比显示、原UI对象/窗口/业务源码保持；纯资源修改不重复编译。
- 独立11项PASS，Root末稿SHA acef4917b4a48e432f28c0a80ba75e3c668ecdf502908580a5dd2709ebd0dc4b；5530对象保留、4Image/8属性变化、5526块逐字保持。全部布局RectTransform、按钮监听/注册代码和设置2304对象保持；1094路径仅Root改，16审查保护输入及主12输入保持。
- 对照RO CommonIcon原PNG/meta、精灵名/内部fileID/GUID一致；四图实际为top_Bag/Role/Skil/Setting，文件与本项目副本逐字一致。原生导入读取对应4Sprite、白色Tint与preserveAspect=True，原生正式菜单16Transform/5Button/0缺脚本。两状态布局预览及400×190裁图已主/审查目检，末Console0错误警告。原裁图/meta未改或删除，没有新图集/Shader/源码，不重复编译。
- 开始读到Play，末态Boot非Play/非编译，新MainView/MainCtrl方法已True；本轮未主动启停/刷新脚本。Boot内存dirty=True，最初state资源不含脏状态，缺前值无法归因；保持原状，不保存或清dirty。原生预览仅克隆布局，不新增正式Play点击或包端验证。
- 证据_temp/ro-main-menu-icons-20261020，独立review/final-audit.json及主root-final-validation.json；专题项目分析/MAIN_FUNCTION_MENU_UI.md，根日志项目修改日志/2026-10-20-RO-MAIN-MENU-ICONS-CHANGELOG.md。下一步正常加载UIRoot查看，包端需原资源更新流程，本轮不发布。

## [VERIFY][界面][TR-059] 2026-10-20 左上可收缩菜单：源码与隔离布局验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 476—486 行；历史时点记录，不代表当前仍在执行。

- 用户明确左上位置，默认收起、展开四个已有窗口入口，必要裁图来自项目现成一梦江湖风格ExtractedMenuUi；原图与设置内容应保持。
- 主冻结10关键输入，审查冻结3目标及1085个Scripts/UISprites/UIPrefabs路径，独立核实际12路径；开发完整281源与独立281源Rebuild均0错38警告，新增警告签名0。review/final-audit.json共24项通过。
- 独立引用审计通过：5472旧对象无删除、5465原块逐字保持、7必要旧对象/33字段变化+58新块；原设置窗口2304对象逐字保持。10保护输入与4原图SHA保持；4裁图逐像素对照仅角色3/设置22像素alpha清零，原图没有改变。按钮Add/Remove成对、B/I/L生产方法未改，中文具体注释及本轮代码规则通过。
- 主独立Unity原生只读核正式源菜单：默认内容false、5按钮可交互、4匹配sprite、0缺脚本；当前主城dirty=false/Play=true/compiling=false、无本轮PreviewCanvas/Camera残留，末Console0错误警告。MCP旧会话404后在本轮_temp重建标准会话，再成功读取；未改服务配置。
- 两张1920×1080隔离预览同框包含菜单、底部HUD和右上小地图，已由主/审查目检展开/收起布局无覆盖。预览移除克隆业务组件，直接切容器与文字，不代表正式方法点击。首preview.cs里missing局部初值0不是缺脚本证据，已由client/native-audit-result.json及主root-native-result.json真实原生计数替代。
- 当前运行MainView/MainCtrl无新方法，不能正式点击验收；未主动启停Play/强制刷新脚本/保存场景/构建发布。下一步正常结束Play编译并加载新UIRoot后验证开关、四窗、镜头及B/I/L；包端需原热更/资源更新流程。
- 临时布局解析初稿未兼容stripped对象的两次KeyError仅修_temp夹具，没有因此修改业务。证据_temp/main-menu-integration-20261020，根汇总root-final-validation.json；专题项目分析/MAIN_FUNCTION_MENU_UI.md，根日志项目修改日志/2026-10-20-MAIN-MENU-CHANGELOG.md。
- 主末次汇总15项全PASS，12正式路径、两方281编译源SHA与10保护输入一致，限定差分空白检查通过；旧设置专题查看入口已同步为左上功能菜单，无业务返工。

## [VERIFY][界面][TR-060] 2026-10-20 RO设置界面融合：最终验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 487—497 行；历史时点记录，不代表当前仍在执行。

- 用户最终只要界面，画质/音量/帧率/FPS/挂机等业务不实现；原先系统设置功能计划在任何业务写入前已停止。
- 开始时主城Play基线quality=5、vSync=1、targetFrameRate=-1、AudioListener.volume=1；末态已为Boot非Play，场景dirty=false，未主动启停用户Play。Unity请求串行执行，MCP会话过期后重新initialize再读取。
- 独立完整客户端281源Rebuild通过：0错误、38既有警告；编译输入SHA末次复核，避免新增窗口漏编。前两次夹具错误（XML编码、误含独立测试asmdef）仅修_temp，失败日志保留，没有修改生产代码迎合测试。
- 最终reference-audit.json通过：正式Root新增2313对象引用闭包无缺失，原3159对象均保留、仅3字段变化；10冻结输入SHA保持。4278基线中只4既有源码/资源变化，无删除。转换5中间prefab/meta移动_temp留证，Root无对应GUID依赖；初审转换资源10悬空引用失败证据保留，正式Root没有这些问题。
- 原生PreviewScene克隆正式窗口，触发原持久按钮验证系统/个人/挂机/推送四页、系统页滚到底（normalized≈1.49E-7）、关闭active=False、重开默认系统页；缺脚本0，quality5/vsync1/fps-1/volume1保持。五张1920×1080末图已目检，页签白块、账号样例和假日期已处理。勾图/挂机槽数字仍为静态模板，不能当真实设置值。
- 测试边界：PreviewScene不运行Play生命周期，首轮按钮未触发；仅克隆调整原监听到EditorAndRuntime并显式调用原OnEnable，再执行原事件，正式事件/资源未改。一次普通运行场景尝试在非Play环境失败，未切换模式；失败证据保留。主城入口依静态接线/编译验证，未做正式Play主城点击或包端验收，未构建/发布。末Console0错误/警告。
- AIconfig以tomli解析通过，首次tomllib缺失仅为临时Python环境差异。ro_settings_review终审通过；源/资源23路径及规则、日志、专题完整清单见项目分析/RO_SETTINGS_UI_INTEGRATION.md和项目修改日志/2026-10-20-RO-SETTINGS-UI-CHANGELOG.md。证据_temp/ro-settings-integration-20261020；汇总root-final-validation.json。下一步正常加载UIRoot查看，设置功能等用户另行明确。
- 主末次汇总19项全PASS，281编译源SHA完全一致；no-index返回码/Windows CRLF与首次自引用缺文件仅为临时验收夹具问题，首结果保留。预览枚举仅余编辑器CustomLightsScene，无本轮窗口/相机克隆残留。

## [VERIFY][性能][TR-061-FILES] 查询任务的正式文档产物：2026-10-20 RO帧率显示：只读源码验证

- 该任务记录的正式文档路径：`项目分析/RO_FPS_DISPLAY.md`、`项目修改日志/2026-10-20-RO-FPS-DISPLAY-CHANGELOG.md`。
- 查询正文、当时状态及原文验证边界见 `Docs/AI/QUERY_TASKS.md` 的 `[TR-061]`；本条只保留文件产物追溯，不新增完成或检查结论。

## [VERIFY][性能][TR-062] 测试记录

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 503—506 行；历史时点记录，不代表当前仍在执行。

- 独立复核26项通过，主验收复算两段统计并核文档引用与恢复状态；证据_temp/fps-investigation-20261020/root-final-validation.json。仅诊断与记录，无业务编译或正式Player验收；下一步固定镜头独立客户端采样。

## [VERIFY][性能][TR-063] 2026-10-20 主城FPS：真实Editor CPU采样与可逆视图对照

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 507—515 行；历史时点记录，不代表当前仍在执行。

- 用户开MCP后连接本机8082，主城Play读取559batches/306setpass/941986tris、Ultra/vsync1/targetFPS-1/RTX4070，未修改游戏或资源。
- 真实原始Profiler两段各保300帧，基准按ProcessQueue>=1ms排1诊断帧后299：主线程mean19.9269/median19.9089ms，SceneView7.9493、GameView7.9917、Update脚本阶段0.4072（不含LateUpdate/FixedUpdate）、Global0.1169、Skin0.1866、Animator0.3279。第二段临时Game最大化300帧：main mean11.288/median9.8364、SceneView0、GameView6.4524ms。嵌套inclusive值不相加，原始全帧和阈值保留。
- 镜头期间移动74.52/19.67/89.66→84.89/25.10/83.33，虽分辨率1920×1080保持，不宣称严格A/B/百分比收益或用CPU倒数当实际稳定FPS。原图36.6FPS也非同一时刻。
- 活动54skin分23INQ/28CAL/3Demon，后快照46任意相机可见；1932staticbatch/675LODGroup/66共享材质。单次FrameTiming GPU6.093824ms，仅瞬时样本不推断持续瓶颈。CPU业务更新样本较小，无依据改技能/移动。
- 原窗口非最大化→临时最大化→恢复false；Profiler最终enabled/recording/CPU/GPU均false、log_file null且其余区域同原值。场景dirty=false，末Console0错误警告，未清Console/删对象/改质量资源/停Play/发布。两raw约911MB保留。
- 初次MCP并行同id请求错配/超时，改唯一id串行后成功；Python编码UTF8重取。性能诊断不写业务测试，不称正式Player/固定镜头GPU验收。证据_temp/fps-investigation-20261020/performance-summary.json与各原始响应，专题项目分析/MAIN_CITY_FPS_RUNTIME_20261020.md。

## [VERIFY][综合][TR-064] 2026-10-20 落地后再移动：修前复现与末稿独立通过

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 516—524 行；历史时点记录，不代表当前仍在执行。

- 本轮仅MJumpComponent.Update新增16行，修前源码已保存；原落地后新输入产生位移与刷新，但Default.PlayAnim被落地保护早退，实际StateMachine.LateUpdate清触发。修前32项复现，修后59项通过（断言数不等于独立场景数），覆盖本地/远端×走/跑×普通/战斗8组合，以及静止完整落地、本地无方向残留移动态、落地瞬间已移动、空中/再跳、技能优先、远端0段落地后收到移动包、一帧点按松开。
- 专项逐字提取25段生产方法，真实Protobuf及MainCity/MMove接线；动画Play/模型/向量/碰撞是薄边界。夹具默认阶段长度0.2秒，用于前后时序断言；正式索引Jump_End为0.83333343秒，未原生采样或测量用户实际等待，不将夹具等待称真实剪辑播放。
- 首夹具远端通知误填IsMoved=false，只修夹具按实际字段；初次同名运行日志已覆盖，失败事实如实保留fixture-first-attempt.txt，不宣称留存首日志或业务因此返工。
- 开发280源Rebuild0错38既有警告，独立280源0错39警告；独立环境多CS8032源生成器System.Runtime依赖警告，未修改依赖掩盖。final-audit.json14项PASS，无新类型/字段/方法/三目/命名空间/foreach或超过三项判断，保原其他源。
- 主核283去重输入SHA一致、业务差分检查通过，唯一修改MJump末稿SHA96609F87AED28D589D584B9928C5CDF79C11BDEFD526A5F964FDDDD9385302D5。证据_temp/landing-walk-slide-20261020/client、review及root-final-validation.json。
- 当前工具无Unity MCP，未做原生Animator/正式双端/打包端验收；未停Play、发布、替正式热更DLL或资源。本轮仅GM_Game内部改动不新增Network AOT接口，需原热更构建/资源更新后验证生效。专题项目分析/LANDING_WALK_ANIMATION_HANDOFF.md、日志项目修改日志/2026-10-20-LANDING-WALK-SLIDE-CHANGELOG.md及客户端/REVIEW分日志。

## [VERIFY][技能][构建][TR-065] 2026-10-19 打包主角消失：旧AOT主包与新跳跃热更闭环

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 525—535 行；历史时点记录，不代表当前仍在执行。

- 独立实际Player日志：本地RoleID2/UID1绑定失败1次、成功0次；MissingMethodException MoveOperationReq::set_JumpCount重复29896次，同一异常不是独立故障数。
- 实际895 Bundle内GM_Game568320字节SHA057685A6…与正式Dlls逐字一致，实际UpdateMove IL调用新setter、CreateRole IL证实catch销毁模型；旧Build真实global-metadata.dat没有JumpCount，11:29原主包早于11:46插件。模型资源已取得，不能归因shared漏包。
- 生产链核实UpdateMove抛错先于EnterWrold发送；中心服入场入口才回11080技能快照，故人物销毁与HUD空属于同一中断链。
- 当前Unity只读非Play/非编译/非构建，Windows64 IL2CPP、新协议setter存在；Boot HostPlayMode/原地址/未脏，MethodBridge DEVELOPMENT=0匹配、link保留两消息all。真实完整主包BuildReport Succeeded，158.8911883秒、0错4警告（任务墙钟160.416秒），报告client/build-report.txt。
- 独立新包实际Metadata类型表Req33/Ntf35确认JumpCount四存取，匹配原Build缓存895真实热更的两JumpCount方法引用；新GameAssembly52490752字节SHA8AB5E4A6…、Metadata11537512字节SHAC4653C6D…，review/final-audit.json PASS。895未HTTP查当前资源服，不能称远端当前版本。
- 4警告：Mogo/Skybox不再支持SetTexture combiner一条、UI/TMP/Common SDF.asset缺脚本一条、FriendChatPrefab.prefab的img缺脚本两条。无证据称全部旧警告，也不能归因本次缺方法；本轮未扩大修复这些资源。
- 7冻结输入SHA全保持，5核心输出存在；本轮新增段20项目根反引号引用无缺失、限定diff-check0，client/delivery-verification.json。初扫历史段旧路径的first结果保留，未批量改动历史。输出D:/unitypro/LYFMMORGP/_temp/packaged-character-missing-20261019/player/GM_MMO.exe，整个目录供手动测试。
- 不改业务兼容层、不覆盖原Build、不发布/重启/自动运行；正式登录人物/技能图标与双端跳跃未验。专题项目分析/PACKAGED_CHARACTER_MISSING_AOT_PROTOCOL.md、根日志项目修改日志/2026-10-19-PACKAGED-CHARACTER-MISSING-CHANGELOG.md，证据_temp/packaged-character-missing-20261019。

## [VERIFY][网络][技能][构建][TR-066] 2026-10-19 远端跳跃动画：编译、生产方法与协议回环

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 536—544 行；历史时点记录，不代表当前仍在执行。

- 修前独立提取真实生产方法，6项复现纯Y位置变化但默认Walk/远端未进入跳跃事件；修后37项通过。覆盖Player即时零耗时段变化包、原普通移动节拍、MainCity传段、事件克隆/回池、1→2→3→0、同段不重播、纯Y/水平跳、空中停止、空中目标到达保持跳跃、零段抵达后落地、Idle/Walk交接、技能根与程序位移单次提交、组件清理及缺字段默认零。
- 专项使用逐字生产方法、真实Protobuf代码；模型/Animator为播放记录器，向量、碰撞和事件宿主为薄边界。能证明分支时序，不能声称原生Animator、真实碰撞/正式双端延迟表现已验。
- 客户端280真实源开发Rebuild0错38既有警告，独立Rebuild0错39警告；差一为CS8032：独立夹具内Unity.Properties生成器缺System.Runtime6依赖，非新增业务编译错误。Network/GameServer/GateServer分别0错4/11/4旧警告。
- 服务端真实生产程序集、在线组件、逻辑队列和网关回环TCP80断言通过，覆盖三段/零段ProtoBuf、原坐标朝向秒耗时、上下行及原本人/远处/异图/离线过滤。两生成文件逐字匹配原protoc，51无关消息类保持；客户端Network.dll与Network构建相同，SHA4429175E2D87C4F8D00CC6E78D6B178955AA7BAE6EAEEAD7428EDED718E6DBBF。
- 主核11正式开发路径和527去重冻结输入SHA均匹配，限定git diff --check通过；结果_temp/jump-animation-sync-20261019/root-final-validation.json。当前只读Console0错误警告；先前反射确认当前Play仍没有JumpCount或ApplySyncJumpState，因此当前Console不作新程序集编译/运行证明。
- 未停止Play、未保存场景、未改正式动画资源、未部署重启或发布。两客户端和游戏服配套更新后，正式原地/移动/二三段/松键/落地画面仍待验收。证据_temp/jump-animation-sync-20261019/client、server、review；专题项目分析/JUMP_ANIMATION_SYNC.md，汇总日志项目修改日志/2026-10-19-JUMP-ANIMATION-SYNC-CHANGELOG.md。

## [VERIFY][综合][TR-067] 2026-10-19 编辑器Jobs IL后处理失败：本轮实际验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 545—553 行；历史时点记录，不代表当前仍在执行。

- 现场Unity2023.2.20f1c1、Boot、非Play/非编译。Console读取4异常，Editor.log确认Unity.Collections（123 defines/265 references）与Unity.VisualScripting.State.Editor（124/272）ILPP Trigger退出1，后续Error building Player、GenerateStripedAOTDlls failed。日志无error CSxxxx，归档before-summary.json。
- 同段ILPP服务PostProcessAssembly/ProgressMessageStreamer存在gRPC响应写入已结束请求异常。该事实是通信故障线索，不能据此证明首次退出1具体内部根因。HybridCLR CompileDllCommand忽略ScriptCompilationResult而打印finish的现有行为已核源码，本轮未改工具。
- 正常RequestScriptCompilation后域重载完成，两条ILPP异常消失，剩两条既有构建失败记录。随后内存诊断执行真实PlayerBuildInterface.CompilePlayerScripts（Windows x64/None），所有输出在_temp/editor-ilpp-20261019/player-scripts，返回typeDB=True/26程序集；日志Player Tundra success，后续Editor Tundra success。
- 末次实时Console查询0错误0警告，未调用Console.Clear。Editor.log仍有CS0436/CS0618等警告文本，所以‘当前控制台0警告’不等于‘构建日志无警告’。状态idle/非Play/无域重载等待。compile返回原文player-compilation-result.txt、前后Console与状态及日志摘录归档。
- 六冻结正式输入（Packages两文件、Boot场景、正式热更DLL、HybridCLR两工具源码）末稿哈希复核；难点问题十八同步复发事实。未删Library/杀进程/修改依赖/业务/保存场景/拷贝或发布DLL，没有执行完整AOT/安装包及正式游戏运行，首次内部触发条件仍未定。
- 日志项目修改日志/2026-10-19-EDITOR-ILPP-DIAGNOSIS-CHANGELOG.md；静态核对结果_temp/editor-ilpp-20261019/verification-result.json。

## [VERIFY][盘点][TR-069] 2026-10-15 面试精简版：文档验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 571—578 行；历史时点记录，不代表当前仍在执行。

- 范围仅新建项目分析/项目难点与亮点-面试精简版-2026-10-15.md（297 行）、更新项目分析/README.md、四入口与项目修改日志/2026-10-15-INTERVIEW-CONCISE-CHANGELOG.md；纯文档轮，不做业务编译/Unity 运行/打包/服务启动/数据库操作；详尽版保持不动。
- 主 Agent 通读全文并复跑校验：8 节结构齐全（30 秒介绍/技术栈与四服/三亮点/两难点/ID 协议对照/1 分钟与 3 分钟话术/12 条高频追问/未完成边界）；代码围栏 1 对成对、连续空行 0、UTF-8 无 BOM；无大目录锚点故无失效链接；引用 8 个项目根相对源码路径全部存在。
- 事实一致：18 米、SkillUuid 1~9999 循环、20Hz/50ms、根运动 4.999855/4.999854、499 文件/191 处→0、280 源 0 错、错误码 28/29、同图 100 米、54 灯均与详尽版及源码核对一致；追问首轮 17 条超过“10 个左右”要求，已压至 12 条核心+1 行其它速查。
- 未夸大：目标伤害/Buff/刷怪、移动物理权威/AOI、FPS 收益、真人联机与安装包、role_skill 联机回归、布尔重构运行时冒烟、Shader IPC/Burst 起因均明确标注未完成或仅静态/隔离；无新增未核实结论。
- 结果：文档静态验收 PASS；无业务返工。

## [VERIFY][盘点][TR-070] 2026-10-14 面试文档第二轮澄清整理：文档验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 579—587 行；历史时点记录，不代表当前仍在执行。

- 范围仅同一文件项目分析/项目难点与亮点-面试版-2026-10-14.md（1493→1602 行）、项目分析/README.md、四入口与项目修改日志/2026-10-14-INTERVIEW-ENHANCED-DOC-CLARIFY-CHANGELOG.md；纯文档轮，不做业务编译/Unity 运行/打包/服务启动/数据库操作。
- 事实先取证：两个只读子 Agent 对照源码/proto 核实四类 ID、11070/11090/11100/11170/11171/11173/11180/11181 职责与字段、UUID 生成规则（1~9999 循环）、MP/CD 权威校正与拒绝回滚、位移生产消费与服务器无速度/路径校验；新增内容均有源码或归档依据。
- 静态校验（主 Agent 独立复跑）：页内链接 79 个锚点失配 0；代码围栏 10 成对；连续空行 0（修复 13 个多行导航与 142 处空行）；## 17 个；UTF-8 无 BOM；不存在的标识符 IsWaitingForServer 计数 0（两处删除）；本章一句话 15 处；11090/UUID/MP-CD 校正等新增内容落位；本轮新增引用的 7 个源文件相对路径全部存在。
- 未夸大：MP/CD 权威校正确认存在但仅限本链路；服务器移动权威未实现、无速度字段、影子追赶直写 Transform 不碰撞、无 RTT 抖动缓冲均如实写明；11090/11100 消息体、SendData bool 封装标注待确认。
- 未验事项沿用：待机滑步正式双端停走、目标协议/伤害/刷怪、role_skill 真人联机、布尔重构 Unity 运行时冒烟、正式资源包发布、Shader IPC/Burst 首次内部起因均未完成，本轮不重跑。
- 结果：文档静态验收 PASS；无业务返工。

## [VERIFY][盘点][TR-071] 2026-10-14 项目难点文档增强版：文档验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 588—596 行；历史时点记录，不代表当前仍在执行。

- 范围仅新建项目分析/项目难点与亮点-面试版-2026-10-14.md、更新项目分析/README.md、四入口及项目修改日志/2026-10-14-INTERVIEW-ENHANCED-DOC-CHANGELOG.md；纯文档轮，不执行业务编译、Unity 运行、打包、服务启动或数据库操作。
- 事实核对：先由 3 个只读子 Agent 对照源码/临时产物/归档日志核实新增事实（11080 回包字段与 role_skill 修复、499/191/75 布尔重构、18 米选敌、协议号/端口/TCP 帧、Shader 变体、热更启动链、移动同步行号），区分“源码核实/临时产物/归档记载”，未联网或运行时验证项均标注。
- 静态校验（写作子 Agent + 主 Agent 两轮）：文档 1493 行/17 个大章节/59 小节；111 条项目内相对路径真实缺失 0（修正 _temp/skill-root-sync-20261013/ 与 test.cs 完整路径，通配/占位文本有意保留）；79 个页内导航链接锚点失配 0（补齐 11 个显式锚点）；代码围栏 10 个成对闭合；问题编号一~二十连续；UTF-8 无 BOM；新增数字与核对报告一致（18 米、191→0、12/8 行、错误码 28/29、77/165、4.999855 米、54 灯、9036ms）。
- 原稿十八问题/六难点/六亮点/六段操作讲述/模板与维护约定逐字保留；教学假设数字与归档实测区分保留；未把隔离验证写成联机/发布结果，未夸大 AOI/物理权威/目标伤害/性能收益。
- 未验事项：role_skill 数据修复未真人联机（需重登装载 skill_map_）、布尔重构未做 Unity 运行时冒烟、目标选择无目标协议/伤害/刷怪、正式资源包与完整主包未发布、Shader IPC/Burst 首次内部起因未证明——均按原边界在文档标注，本轮不重跑。
- 结果：文档静态验收 PASS；无业务返工。

## [VERIFY][盘点][TR-072] 2026-10-14 面试详细流程与18项真实问题：文档验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 597—604 行；历史时点记录，不代表当前仍在执行。

- 范围仅项目分析/项目难点.md、README、四入口及项目修改日志/2026-10-14-PROJECT-INTERVIEW-DETAIL-CHANGELOG.md；不执行Unity/业务编译，不重跑历史专项，不更新资源或运行服务。
- 核对当前提交后预表现/客户端空连接既有路径、Cease不清CD、InState请求语义、服务器段窗口、TCP帧格式、Boot编辑器/Player加载差异；故障结论以现有专题最终修订和正式日志为准。
- 静态校验覆盖六亮点/六详细链路、原六案例与维护模板保留、18问题索引/12新增案例编号和必需字段、根相对引用存在、Markdown围栏/表格、教学例子标识、归档数据出处及限定git diff --check。结果与输入哈希归档_temp/project-interview-detail-20261014/verification-result.json及reference-input-hashes.json。
- 未验事项仍保留：双端全技能/安装包、稳态CPU/GPU帧耗时、MP耗尽历史、HFS零响应根因和Shader/Burst首次内部触发原因。文档不把‘直接原因已定位’写成‘全部业务已修复’，不把引用非空或compile finish当作实际运行成功。
- 末次静态校验PASS：六详细流程、18索引及12新增案例齐全，73条完整根相对引用无缺失，16份归档来源关键事实匹配，原六案例与模板保留，围栏/表格及限定diff检查通过。首轮部分因果/验证标签不显式已补清，校验脚本两处词组与来源用词不同已校正；首失败保留verification-first-result.json，无业务返工。

## [VERIFY][盘点][TR-073] 2026-10-13 项目难点亮点面试版：文档验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 605—612 行；历史时点记录，不代表当前仍在执行。

- 文档范围：项目分析/项目难点.md与README、四入口、项目修改日志/2026-10-13-PROJECT-INTERVIEW-SUMMARY-CHANGELOG.md。无业务代码/资源/规则变更，不重新运行历史业务测试、不新增编译或发布。
- 事实核对：当前导航提交→预表现、服务端学习/MP/CD提交、技能核心/实例与自然尾段、真实Timer→GO操作回调、50ms/5帧余量、两级消息队列、Boot实际DLL加载及宿主导出入口；历史设计正文以最终修订为准。
- 六亮点与六案例结构齐全，原三案例正文保留；每项区分实现、历史隔离验证与未验正式边界，20Hz/约5米/77与165/0.1100968米等数据均来自已归档记录，本轮没有宣称新跑通过。
- 校验完整项目根相对路径引用存在、标题/代码围栏/表格结构、原条目及模板保留、限定git diff --check通过。证据_temp/project-interview-summary-20261013/verification-result.json。
- 本轮无文档阻塞；正式双端/安装包、Profiler及完整目标伤害/动态AOI等仍按原状态，未因面试总结改变实现范围。

## [VERIFY][协作][盘点][TR-074] 2026-10-13 项目难点文档与约束：文档验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 613—620 行；历史时点记录，不代表当前仍在执行。

- 范围仅AGENTS.md、AIconfig全局/流程/产品经理角色、项目分析/README.md与新建项目难点文档、四入口和根修改日志，无业务源码或资源变更，不运行无关编译/Unity测试。
- 校验项目难点维护要求在四份规则中一致；TOML可解析；三个案例与已有专题/实际验证边界一致，面试讲述没有虚构个人职责或全量上线结果。
- 检查难点正文的项目根相对路径存在、案例与模板章节齐全，既有面试点总结保留。限定git diff --check通过；验证产物位于_temp/project-difficulties-doc-20261013/。
- 未解决项：无本轮文档阻塞；业务案例未完成的正式双端/安装包验收按原边界保留。
- 日志：项目修改日志/2026-10-13-PROJECT-DIFFICULTIES-DOC-CHANGELOG.md；主维护入口：项目分析/项目难点.md。

## [VERIFY][技能][TR-075] 2026-10-13 技能瞬间下沉与无法再次施法：验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 621—630 行；历史时点记录，不代表当前仍在执行。

- 正式复现：root-live-height-skill10080.txt通过正常技能入口释放F，约0.4204秒真实clip变空Skill/ready=False、左脚Y18.32197，而实体/模型根Y18.73892；约0.4754秒Idle恢复。45秒纯待机及MP不足的10040请求均不计故障复现。
- 最小修复：仅MAnimator.TryReleasePendingSkillAnim改读原生状态，保InState最新请求去重。开发280源0错38旧警告、独立280源0错39旧警告/警告签名保持；两生产方法逐字抽取修前10项复现/回归、末稿10项通过，覆盖请求评估缝隙/过渡/退出归还/快速停走语义，薄边界不当原生。
- 独立final-audit PASS：280编译源无漂移、方法外逐字保持、InState未变、19保护输入保持。客户端20冻结输入仅授权MAnimator变化；主另核两方编译清单及前轮MMove/MainCity/MDefaultAction/Collector末稿。
- 用户自然退出Play，主未停运行。Boot非Play/非编译、域reload1791903962187；当前TryReleasePendingSkillAnim原生IL含GetCurrentAnimatorStateInfo/IsName且无InState调用，确认测试加载末稿。
- 原生client/native-exit-release-result.txt PASS：正式OtherPrefab/Humanoid/Controller及真实MAnimator Release→CrossFade→Update；gapLatest=FIdle/nativeSkill=True/transition=False仍保真实clip，之后12过渡帧全保留，第12帧实际退出才归还，20帧最低脚高0.1100968米；Console错误警告0。独立元数据引用与PreviewScene，未触正式资源缓存/实体/网络；不能称修后双客户端正式全技能验收。
- 无法继续施法：客户端root-live-skill-gates.txt显示MP0、默认状态/空CurrentSkill/CD0/CanSkill真，收费技能coreCanStart假，零费F可释放。服务端两个被动快照两角色MP0/500且无活动技能，LogicTick92020→97767约287秒，20Hz正常。只读DB均500/500，部署IL减表MpCost、Handle一次TryCast；当前无回蓝实现。未获取完整消耗请求序列，不证明一次耗尽或历史UUID重放；不增加假设修复。
- 未验边界：修后两端正式所有技能/包未验，未发布热更/重启/补MP/改表或保存场景。详见项目修改日志/2026-10-13-SKILL-HEIGHT-DIP-CHANGELOG.md，分析项目分析/SKILL_EXIT_CLIP_RELEASE_HEIGHT_DIP.md与项目分析/SKILL_STUCK_SERVER_MP_DIAGNOSIS.md。临时证据全部保留。

## [VERIFY][网络][技能][TR-076] 2026-10-13 技能根运动远端同步：末稿验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 631—641 行；历史时点记录，不代表当前仍在执行。

- 客户端仅修改MMoveComponent.Update远端分支：消费根与程序位移，一次碰撞提交；技能位移帧跳过普通影子追赶，重算现有同步残差，精确抵达收束移动态。最终SHA0DB2686CC687D8BDA7616E88646C75449286C5870CA8CB6ADD128E017E70F0EE，Update外逐字保持。
- 开发280源Rebuild0错38旧警告，独立280源0错39旧警告、警告签名保持；独立真实3移动方法及Collector.ConsumeDelta逐字抽取，修前15项复现/回归、修后19项通过。涵盖根漏消费、无程序组件、根与程序一次提交、碰撞停止/实际残差、技能后不重复追赶与移动态收束、允许施法移动、同步点落后及普通跟随。实体、归属、数学与碰撞为薄测试边界，不当原生证明。
- 独立final-audit PASS、4方法原文匹配、280源无漂移、7文件保护保持；主root-final-hash-check.json复核569条编译/测试/保护SHA全一致。源码diffcheck0，未修改前轮MAnimator、Default、MainCity、Collector、MArtSkill、PlayerEntity与施法配置。
- 正式主城最初只读30秒采样时两角色静止，未捕获施法；root-live-before-samples.txt不算原生修前复现。随后用户已退出Play回Boot，主未停止；普通脚本刷新后loaded Update IL379字节含Root消费与SetMoving，控制台0错误。
- 原生隔离正式OtherPrefab、正式Humanoid滑步剪辑、生产Collector/远端MMove.Update/CharacterController通过：40帧原生raw水平根约3.428米，经现有轨道倍率采集4.999855米，实际4.999854米，误差9.536743E-07米；pending清零、movingAtTarget=false，结束后20帧额外位移0/movingAfter=false。结果client/native-root-movement-result.txt。
- 边界：Edit PreviewScene显式反射调用真实Collector.OnAnimatorMove，共40帧；反射装配独立技能/剪辑信息及目标缓存，没有正式Skill请求/完整生命周期/onSync网络节拍/双客户端延迟。首测未自动采根、第二次Rebind后实体/原生起点不一致，分别保留first/second-attempt；只修_temp夹具起点和回调，末次统一坐标后通过。
- 末次控制台发现测试清理Error1：夹具world.Dispose间接调用MGameObject.Release中的Play模式Object.Destroy。只调整_temp夹具先DestroyImmediate模型再Dispose；独立真实MGameObject清理小验证错误计数1→1、新增0，证据root-preview-cleanup-error.txt与root-preview-cleanup-result.txt。旧消息保留，不清控制台，不把此条算正式移动故障；原生40帧位置断言仍有效，不称最终Console0。
- 专用对象与PreviewScene结束移除，未保存正式场景、发布热更或重启服务器。正式安装包及双客户端施法画面仍待新热更代码生效后验收。日志项目修改日志/2026-10-13-SKILL-ROOT-SYNC-CHANGELOG.md及两职责日志；分析项目分析/SKILL_ROOT_MOTION_SYNC.md；证据_temp/skill-root-sync-20261013。

## [VERIFY][构建][TR-077] 2026-10-13 Standard Surface编译器IPC专项：当前恢复验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 642—652 行；历史时点记录，不代表当前仍在执行。

- 用户明确0x80000004报错，控制台当前有1条Exception；原生ShaderUtil在内置Shader对象读得同Error。先保存旧错误，限定该Shader缓存/错误状态恢复后同步编译，再实际构建含该Shader的现有材质到_temp，不以清消息当PASS。
- 日志定位4个真实Compiler crash与协议断连，均为FORWARD Fragment；自动重启后同Shader编译ok=1。当前正式1118为用户新的ForceRebuild，不能继续以1071历史路径验证。独立核新IPC增量、6pass与实际Bundle读回、正式资产SHA保护。
- 开发/独立分工已建立，主统一MCP，未清控制台、未删除资源或缓存目录。修后结果待执行。
- 开发专用StandardSurfaceCompileProbe独立C#编译0错；主原生执行Compile，源before消息1 Error→限定缓存与对象错误状态复位后6个pass compiled=True，ShadowCaster/Selection/Picking/FORWARD可SetPass，源after0。空名pass与DEFERRED SetPass=False不作为当前Forward模式编译失败。
- 主真实Build以现正式Foggy材质＋集合ForceRebuild到_temp，manifest非空/读取材质消息0；独立日志审查发现Standard Surface D3D11 internal programs=0，包内目标无FORWARD，不能冒称临时包目标字节码重编通过。编译缓存有效并被复用，Compiler日志新增Surface请求0，不宣传全变体重编。
- 补查1118正式清单实际主包364/依赖575,576，三MD5相符/未借用已加载句柄，真包材质Shader实例683040区别源176；FORWARD pass2 compiled=True/SetPass=True，消息前后0。实际包Shader不在LoadAllAssets<Shader>()枚举结果中，先前空字符串读取不算验收，改从真实材质引用检查。结果formal1118-fog-forward-result.txt及原始响应。
- 本轮Editor增量无IPC/Crashed；最终原生查询源消息0与控制台0错误，未调用Console.Clear。已保留before完整错误、编译器日志与自动重生证据，不能把当前恢复说成引擎内部触发根因永久修复或全项目零错误。
- 五冻结输入（Foggy材质、三变体资源、GraphicsSettings）主SHA全保持，root-protected-input-recheck-final.json；首PowerShell属性计数夹具失败仅_temp留存、末稿改快照数组后通过。无正式业务/资源修改，未验安装包或重建正式资源新版本；已有1118为用户此前构建。分析与主日志、独立日志按根目录归档，四入口更新。

## [VERIFY][技能][TR-078] 2026-10-13 两截图对应技能：shared与修后画面验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 653—663 行；历史时点记录，不代表当前仍在执行。

- 正式1071清单与36依赖包大小/MD5核对无差异；HS_Blend_CG在unityshaders且两特效及Runtime显式依赖，材质/贴图不是这批漏包原因。此前66材质1527有效字段一致、43纹理RGBA逐像素相同及单Shader替换因果继续有效。
- 原生读取collection：1071=17 Shader/20变体，临时实际43包=77/165，源=77/165。开发三正式资源修复与两meta/根用户集合等7路径主SHA重核全匹配，root-resource-final-recheck.json及collection-after-unity-result.json留存。
- AllActualCollectionBoundary2独立C#6编译0错，主在Unity2023.2真实执行成功，按原生AssetBundleManifest加载43实际包。源/包同相机、粒子种子、0.15/0.4/0.8/1.2秒成对渲染，输出review/native-20261013102847。6FX/24对中19完全一致；Skill3/Skill04的8对全部逐像素一致，包含早期两侧同为空的Skill04-t0，不把空帧视作可见形态测试。两可见异常白框/黑轮廓消失，主与独立目检通过，48重复控制全部逐像素相同。
- 其余5对保留：Attack01-t0平均RGB差0.00352、超过5灰阶72像素，另4对差异≤5灰阶/轮廓IoU1。71元数据噪声为Color模式不参与的colorMin/colorMax读取，过滤这两字段后两侧2203有效记录差0，不据噪声改资源；不得将19/24说成全特效完全一致。
- 失败证据保留：先在Play同名Bundle冲突，后原after错误使用模拟清单DependIDs导致Particle为空，再共享音效包名映射失败；全为专用夹具边界。新版用实际43包名读取后成功，正式YooAsset依赖规则未改，不清控制台。结束Play回Boot，无正式场景保存。
- 临时43包构建真实记录YCOMBAT_BUNDLE_VERIFY_SUCCESS，但同时有Shader Compiler IPC/Particles Standard Surface通信错误，见review/shader-build-ipc-evidence.txt。不能用Success日志覆盖编译错误。后续原生诊断HS_Blend_CG实包消息0，源有3宏重复Warning；当前默认根Shader为Standard Unlit，Surface未在该包LoadAllAssets命中，故不宣称IPC对应Shader已验证修好。捕图期间无新增IPC。
- 未验证：新正式版本发布/安装包或主城联机施法、全构建零IPC；旧1071仍旧20变体。两目标修复限本轮实际Bundle/预览证据，不扩大为所有渲染/所有技能验收。正式资源/Shader源码/共享规则保持；四入口与根日志更新，临时全部保留。
- 独立审查末稿independent-shared-review-final.json及根日志 项目修改日志/2026-10-13-SKILL-FX-SHARED-INDEPENDENT-REVIEW.md已读，343冻结源342保持/唯一授权collection变化/缺0，24源图与before逐像素一致。主目检统一裁切三列对比named-skills-before-after-source.png通过。四入口diff空白检查退出0；Unity集合序列化空keywords行的原生尾空格提示保留，不格式化用户新集合。

## [VERIFY][技能][TR-079] 2026-10-13 技能特效颜色/形状：验证准备

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 664—668 行；历史时点记录，不代表当前仍在执行。

- 用户明确颜色或形状不对。前轮12粒子引用/材质Shader非空只证明资源能读取，未证明视觉与源一致；本轮以源/包粒子及材质属性比对、完整实例层级和确定性渲染为验证目标。
- 开发与独立测试已按职责分工，临时 _temp/skill-fx-appearance-20261013，不向正式场景挂测试、不发布资源。实际验证随后补充。

## [VERIFY][技能][构建][TR-080] 2026-10-13 技能特效打包与运行错误：本轮验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 669—687 行；历史时点记录，不代表当前仍在执行。

- 用户后续明确移动时报错，指出编辑器已有报错；主重取编辑器状态非Play/非编译、当前错误0。已知堆栈和修复前后行为足够定位编辑器移动故障，不再要求用户重复提供该堆栈。
- 补充只读包代码核对通过：真实995代码Bundle提取GM_Game.dll为567808字节/SHA9D399E048A3527D7B988103AE6DA22D748E44E68CD3F70C8743B1B67B6C0F320，与源Dlls完全一致。Mono.Cecil读取其OnUpdate IL证明null表仍流入ldelem.r4、最终才and，包含真实故障；Editor及独立末稿IL已修。Boot只在非Editor反射加载包中字节。证据review/code-version/含IL、MVID/SHA、提取副本和native-hotdll-result.json，不仅用文件时间推断。
- 此补充未修改业务或正式代码资源、未重新运行安装包；已定位旧995未同步修复，无需再索取同一移动报错。安装包修复生效须重新编译热更DLL并按原流程更新资源包，当前不声称已发布。

- 用户确认编辑器和安装包均报错；编辑器真实控制台保存了 MDefaultActionComponent.OnUpdate:296 空引用调用链。尚未将安装包错误归为同一原因，独立测试继续核 Player.log。
- 本轮验证要求：真实故障修复前后对照、当前客户端末稿编译、技能 Runtime 特效路径与实际 YooAsset 收集/清单覆盖核对；按实际结果记录，不以历史 PASS 代替本轮验收。
- 证据 _temp/skill-fx-package-runtime-20261013，开发与测试分目录；不清控制台、不向正式场景挂测试组件、不自动发布或重启。
- 客户端独立280份源 Rebuild 0错39旧警告；直接抽取本轮before的 OnUpdate/FootSteps 空表移动失败NullReference，末稿相同方法18行为项通过。薄边界仅实体与动画接口；主另在Unity当前真实程序集/Animator隔离对象验证静止、空表移动、有效脚步点及空数组4项通过，销毁临时对象、不保存场景。
- 编辑器工具首稿 expectedCount 新旧局部重名 CS0136，返工为 expectedFormalCount 后Unity原生编译0错误；未使用首稿编译失败时的旧DLL结果验收。真实ValidateAll从before固定数量8/1失败变末稿PASS，八正式/一参考与全部当前作者字段、粒子依赖一致。
- 专用BuildAndVerify真实正式YooAsset映射模拟清单＋43个完整技能依赖Bundle原生构建读取PASS：8份Runtime，FX分别1/1/2/2/2/2/2/0，总12。主初消息误写13已公开更正；F无粒子事件，仍原Timer残影。产物 package/bundle-verification-20261013091048/，证据 unity-final-evidence.json。
- 原生动作夹具首运行缺返回值，仅追加结果return后通过4项；不改业务迎合夹具。最终控制台0错误，不将旧日志清空或重新编译后零错误等同完整正式游戏回归。
- 当前995清单与报告独立核8Runtime、12Particle、8动画路径；Runtime依赖36包，动画独立字符串路径再加3包，合39文件大小/MD5一致。主真实AssetBundle读取39包，通过8Runtime/8动画/12FX及182次Renderer材质/Shader非空检查，证据 unity-995-evidence.json；未验证最终屏幕渲染或全部变体。清单原有覆盖不计作本轮补收集。
- 主root_verify.py核两方编译/真实方法before-final/三末稿/8Runtime共573条291去重文件SHA全一致，root-final-hash-check.json。源审查只3源码，独立冻结Collector/ResourceMgr/FxEvent不变，无用户资源写回；不夸称全工作区基线。
- 995原生夹具漏动画包发现后补独立动画依赖；Unity没有Roslyn，改测试局部函数为具名强类型Action委托适应CodeDom后PASS，未安装依赖或修改生产迁就工具。
- 未完成边界：本机旧Player.log没有用户所述当前包异常，已问具体报错/触发步骤；未重新发布/运行安装包、未正式联机或验证最终屏幕着色视觉。历史布尔重构全部等价结论对此 OnUpdate 已被本轮真实故障推翻，不继续据其宣布无回归。
- 正式日志 项目修改日志/2026-10-13-DEFAULT-ACTION-RUNTIME-FIX-CHANGELOG.md、项目修改日志/2026-10-13-SKILL-FX-PACKAGE-CHANGELOG.md；主负责完成记录，独立审查报告位于本轮review目录。

## [VERIFY][综合][TR-081] 2026-10-13 全项目布尔并排判断不超过 3 项重构：验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 688—698 行；历史时点记录，不代表当前仍在执行。

- 静态规则检查（Roslyn 全量复扫）：纳入第一方 499 个 C# 文件，3 个 auto-generated 跳过；重构前基线 191 处违规，重构后输出 `违规：0`（191→0）。结果文件 `_temp/boolcond-refactor/scan-after-all.json`。
- 客户端编译：MSBuild 2022 Professional 编译独立工程 `_temp/boolcond-refactor/compile-GM_Game/GM_Game.csproj`（280 Compile，含 DOTween/HighlightPlus 20 第三方源，目标 v4.7.1、LangVersion 9.0）。首编 1 错误（MDefaultActionComponent.cs stillMoving 重复声明 CS0128），经开发批次 B 修复后 Rebuild **0 错误 39 警告**；以 2026-10-12 同模板基线（38 警告）按“文件+警告码”去重对比，警告集合完全一致（25 去重签名），无新增。
- 服务端编译：MSBuild 2022 Professional 编译 `LYF_Server/LYF_Server.sln`，**0 错误 12 警告**；警告均为既有 CS0162/0649/0169/0114/0108/4014 代码质量提示，位于非本次改动位置，与布尔拆分无关。
- 独立复审：三名只读复审子 Agent 逐条核对 191 处（客户端核心 75、客户端控制器/功能/UI/编辑器 67、服务端 49），确认判断顺序、短路、空引用守卫、副作用调用次数/时机、混合括号与取反全部保持，结论全部等价。
- 边界确认：未修改第三方（UniRx/DOTween/HighlightPlus/LubanLib/protobuf）、生成代码（HybridCLRGenerate/Luban Codes/Proto）、Packages 与参考工程 F:\UnityPro\ro。
- 未执行项：未在 Unity 编辑器内做运行时冒烟（建议后续进场景选目标/施法/移动）；测试为静态规则、编译与代码级等价复审。
- 遗留：少量纯查询谓词提前求值经复审判定等价（无副作用、无空引用）；LubanMgr effect==null 时 throw 消息取 effect.Id 属重构前既有隐患，按范围原样保留。
- 日志：项目修改日志/2026-10-13-BOOLEAN-CONDITION-REFACTOR-CHANGELOG.md。

## [VERIFY][协作][TR-082] 2026-10-12 连续判断约束：仅规则修改

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 699—704 行；历史时点记录，不代表当前仍在执行。

- 最新用户指示仅加入约束；未修改客户端/服务端业务或资源，未执行全项目条件重写。
- AGENTS.md与AIconfig全局、流程及客户端/服务端/审查模板统一规定：单个布尔表达式的&&/||判断项最多三个，混合与括号合并统计；拆分保原求值顺序/短路和语义，不以换行规避、不新增多余判断，不擅自批量改既有代码。
- 验收采用六份规则文本一致性及config.toml解析；纯规则与记录修改无需编译/运行测试。无待修业务项，后续开发遵守；日志项目修改日志/2026-10-12-BOOLEAN-CONDITION-RULE-CHANGELOG.md。

## [VERIFY][综合][TR-083] 2026-10-12 RO 自动选敌距离：末稿验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 705—713 行；历史时点记录，不代表当前仍在执行。

- 已核RO普通技能按钮FindEnemyBySkillBtn使用MaxChooseDistance，实际表MaxChooseRange18米，并非UpRange倍数；UpRange及导航到达判定另走技能射程。
- 修改范围仅MSkillTargetMgr.cs选择半径30→18与两中文注释，三行替换。独立完整280源Rebuild零错38既有警告，审查PASS无业务返工。
- 本轮Unity隔离完整真实GM_Game.dll/world/工厂/实体/模型/InputSystem/相机/Collider原生28项通过：17.99/18米含边界可选、18.01米拒绝，技能无目标自动/真实点击/真实Tab与瞄准共用方法/超距清目标、当前目标优先/友敌死亡过滤、五正式攻击技能同18。仅隔离内存AtkDistance改1仍可选17米、改100仍拒18.01后恢复，正式表不改；F/轻功保持不选敌。
- 主核7清单（单末稿、两方280源/268引用、71原生输入、独立5RO参考）及开发7RO参考与2073保护共2440去重文件SHA一致；原生DLL=本轮独立编译且区别前轮30米DLL，真实NATIVE_TARGET_RANGE_PASS checks=28通过。保护仅授权单源变，其余保持；未累计旧93项、未重跑无影响Timer/根/后端/HUD。
- 原生Global禁用、世界初始状态反射设置/模型为专用GO，SkillCtrl查询用未初始化对象，正式bytes读取；无正式网络/资源包/目标伤害/安装包运行。本轮无失败业务或返工，未发布/重启/清临时。报告_temp/ro-target-range-20261012/review/review-report.md、final-audit.json及根root-manifest-check.json。
- 临时_temp/ro-target-range-20261012；日志项目修改日志/2026-10-12-RO-TARGET-RANGE-CHANGELOG.md。无正式资源/表/协议/服务器变更，主仅协调与记录。

## [VERIFY][综合][TR-084] 2026-10-12 RO 三种目标选择：末稿验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 714—725 行；历史时点记录，不代表当前仍在执行。

- target_client完成13客户端源/meta，target_protocol核无需后端/协议/表变更，target_review独立审查PASS，无业务返工。主已读review-report.md/final-audit.json及差分，不改业务。
- 独立完整280源Rebuild 0错38警告：35旧＋3个CS0436新引用，既有客户端MonsterEntity与共享库重名，编译采用客户端源类型，原生普通/精英/Boss敌判定正确。
- Unity 2023.2隔离PlayMode完整真实GM_Game.dll/world/工厂/实体/模型/InputSystem/Camera/Collider/EventSystem/GraphicRaycaster/窗口/SkillCtrl共57项：点击/子Collider/UI阻挡/Mouse和Tab边沿、前四循环、30米/实际HP/可见/加载/离场/回池、敌友/当前目标优先/自动选敌/Type3/轻功/无敌，以及Button→控制器→Target→UI和注销。
- 本轮72当前生产源技能/移动回归36项：原F特殊取消/500ms CD/667ms动作/残影/跳跃/普攻/根倍率与一次位移提交保持；发送异常不转向不预测、提交后转向再预测、Jump/CD拒不转、F有目标保原朝向。Unity/网络/角色/状态机/新目标查询为薄边界，选敌由另57原生证明。合计93，不累计历史。
- HUD原生1920×1080图主/测试目检中文可读/无重叠/F最右；正式UIRoot角色HUD子树/槽资产隔离副本，4控件方法逐字等价，正式Common SDF；视觉Global/实体仅未选中显示边界，图标按原预览布置，不称完整正式HUD联机。
- 主root_verify.py核11清单3792去重路径（13末稿、两方280源/268引用、72回归源、69原生/108回归/16原HUD资源/1227HUD输入、后端10只读）SHA一致，原生DLL=独立末稿编译、4方法/3运行标记一致。2067保护仅7授权既有源变，其余保持，不夸称全工作区保护。
- 失败留存：隔离Cinemachine依赖、TouchPhase歧义、Image同帧无Canvas depth、缺临时TouchObject、预览组件Editor目录/旧路径缓存、审计拼写，仅修_temp。主首核HUD因末尾补注释SHA不同，保root-first-manifest-check.json，独立末稿重编后PASS；未用旧PASS替代。
- 主城无正式怪物生产；测试世界工厂真实怪物不代表正式战斗。未启动正式Global网络/包初始化，未验正式联机/异步模型/服务器目标伤害/安装包，未部署/重启/发布/清临时。下一步正式运行看提示/选择入口，未来明确目标结算需求后另做。
- 分析项目分析/RO_TARGET_SELECTION_IMPLEMENTATION.md、项目分析/RO_TARGET_SELECTION_SERVER_BOUNDARY.md；日志项目修改日志/2026-10-12-RO-TARGET-SELECTION-CHANGELOG.md及两职责分日志；证据_temp/ro-target-selection-20261012/review/review-report.md、final-audit.json与根root-manifest-check.json。

## [VERIFY][综合][TR-085] 2026-10-12 RO 三种目标选择：开发范围与验证准备

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 726—732 行；历史时点记录，不代表当前仍在执行。

- 已顺序读取协作记录并委派客户端、后端边界与独立测试。拟新增RO同名TouchObject/MSkillTargetMgr/MSkillTargetDefine，接现有实体世界/输入/SkillCtrl/UI，主Agent不改业务。
- 当前正式技能TargetMode0；自动选择对Type1/2攻击作为客户端目标与朝向辅助，无敌仍保原无目标释放，Type3滑步/轻功不参与，不扩目标伤害/协议。阵营数据缺失下仅怪物为敌，普通玩家友方。
- 拟验点击射线UID/模型层级映射、UI点击不穿透、按钮/Tab循环、按技能优先当前目标与自动候选、失效/隐藏/死亡/超距排除，并回归F/跳跃/CD/root/移动同步。独立原生测试仅隔离或临时预览，不挂正式测试组件。
- 当前开发中，无本轮编译/运行通过结论；临时_temp/ro-target-selection-20261012，日志项目修改日志/2026-10-12-RO-TARGET-SELECTION-CHANGELOG.md。

## [VERIFY][综合][TR-086-FILES] 查询任务的正式文档产物：2026-10-13 RO 目标选择只读核对

- 该任务记录的正式文档路径：`项目分析/RO_TARGET_SELECTION_FLOW.md`、`项目修改日志/2026-10-13-RO-TARGET-SELECTION-ANALYSIS-CHANGELOG.md`。
- 查询正文、当时状态及原文验证边界见 `Docs/AI/QUERY_TASKS.md` 的 `[TR-086]`；本条只保留文件产物追溯，不新增完成或检查结论。

## [VERIFY][技能][TR-087-FILES] 查询任务的正式文档产物：2026-10-13 RO 气墙CheckWall只读核对

- 该任务记录的正式文档路径：`项目分析/RO_AIR_WALL_SKILL_CHECK.md`、`项目修改日志/2026-10-13-RO-AIR-WALL-ANALYSIS-CHANGELOG.md`。
- 查询正文、当时状态及原文验证边界见 `Docs/AI/QUERY_TASKS.md` 的 `[TR-087]`；本条只保留文件产物追溯，不新增完成或检查结论。

## [VERIFY][技能][TR-088-FILES] 查询任务的正式文档产物：2026-10-13 RO MSkillComboTableConfig 只读核对

- 该任务记录的正式文档路径：`项目分析/RO_SKILL_COMBO_TABLE_CONFIG.md`、`项目修改日志/2026-10-13-RO-SKILL-COMBO-ANALYSIS-CHANGELOG.md`。
- 查询正文、当时状态及原文验证边界见 `Docs/AI/QUERY_TASKS.md` 的 `[TR-088]`；本条只保留文件产物追溯，不新增完成或检查结论。

## [VERIFY][技能][TR-089-FILES] 查询任务的正式文档产物：2026-10-13 RO Send 返回值与施法预表现只读核对

- 该任务记录的正式文档路径：`项目修改日志/2026-10-13-RO-SEND-CONFIRM-SEMANTICS-CHANGELOG.md`。
- 查询正文、当时状态及原文验证边界见 `Docs/AI/QUERY_TASKS.md` 的 `[TR-089]`；本条只保留文件产物追溯，不新增完成或检查结论。

## [VERIFY][综合][TR-090] 2026-10-13 RO 原版 MNavigationComponent.OnAfterAction 中文注释验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 756—761 行；历史时点记录，不代表当前仍在执行。

- 范围：仅注释 `F:\UnityPro\ro\Ro_cs422nb\clientcode\CSProject\MoonClient\Skill\MNavigationComponent.cs` 的 `OnAfterAction()`，并补 `OnGetEvent` XML；不改任何代码/签名/命名空间/格式，D 盘 LYF 移植版未触碰。
- 验证：①写入前所有锚点整行唯一命中（不命中即中止，曾因外部编辑器折行中止重做，无脏写）；②用改前备份 `_temp/ro-nav-comment-20261013/MNavigationComponent.cs.orig` 与改后文件做“去除整行注释后逐行比对”，代码行均为 1139 行且完全一致，确认 0 行业务代码改动；③编码 UTF-8 无 BOM、全 CRLF(1378)、无裸 LF、严格 UTF-8 解码通过；④已重新 Read 方法全段与 OnGetEvent XML 目检正确。
- 结论：PASS（纯注释）。未做外部 RO 工程整体编译（单文件注释、代码逐行一致，编译结果不变）；未决项：服务器端 Common 位域拆包代码未在本次范围核对。

## [VERIFY][性能][TR-091] 2026-10-12 RO 移动同步与主城性能：末稿验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 762—773 行；历史时点记录，不代表当前仍在执行。

- move_client/move_server/city_performance 按职责开发；move_city_review 独立测试审查 final-audit PASS，无业务返工。当前20正式路径，客户端3源、服务端16路径、主城场景1；原正式 F/跳跃/技能与根运动资源保持。
- 本轮独立277完整客户端源 Rebuild 0错35警告，GameServer及引用0错11既有警告，GateServer及引用0错4既有警告。客户端旧预留影子字段实际使用减少未读警告，RO保留的 _predictedTime 赋值仍提示未读，未增加假计算。
- 独立80断言：移动30（真实事件/协议编解码、秒/米、0.1秒/厘米/角度阈值、只转向、纯Y、停止最终包、远端追赶/停止、本地不重复积分）；F/跳跃/CD/根28（原24当前重跑及Collector真实采集→MMove一次消费→IsMoved同步）；服务端与网关22（真实容器/逻辑队列/会话索引及回环TCP上下行、本人/远处/异图/离线排除）。开发24移动及22后端单列，不合并计数。
- 客户端运行72完整生产源，玩家Update/UpdateMove及主城接收3方法逐字提取核一致；原生/UI/CharacterController/传输为薄边界。服务端真实程序集/逻辑队列/Socket封包执行，使用独立临时连接，非正式多服务联机。
- 主城独立原生OpenPreviewScene核60组件保留，54实时点光仅disabled、1活动Baked/0Realtime/0Shadow；独立精差剔除9336字节覆盖后与before一致，deleted_objects=0/deleted_components=0。共享灯光prefab保持，Boot活动场景/dirty前后false，预览关闭且无正式测试挂载。
- 协议旧51消息类整段保持、protoc重生成字节一致；客户端Network.dll等于本轮后端生成产物。主 root_verify.py 核12类清单962去重文件、两端末稿/编译/引用/运行及场景SHA全部一致，root-manifest-check PASS；不夸称全工作区保护。
- 失败保留：首次临时预览RT销毁顺序有诊断，解除相机引用后重出图成功；独立临时XML编码/命名空间导致首次MSBuild读项目失败，仅修_temp后重编成功；根测试首次17ms不满足自动0.1秒阈值，改测原UpdateMove，另17ms混入旧Timer序列导致残影断言不成立，仅在_temp分离实例修夹具后28通过，业务无返工。首编译38警告不作为末稿35结论。
- 性能证据：54实时点光/13软影、全场景2020活动Renderer/835万总三角与历史加载9036ms；摄像机远裁剪/高画质/AO待Profiler排序。不把总网格当每帧提交、不把加载时间当稳态帧耗时、不宣称FPS增幅或唯一根因。
- 未验正式两客户端/多服务联机、原生墙体/实际远端动画、安装包/Bundle与稳态Profiler；未部署重启或清临时。分析 项目分析/RO_MOVE_SYNC_CLIENT.md、RO_MOVE_SYNC_SERVER.md、MAIN_CITY_PERFORMANCE.md；日志 项目修改日志/2026-10-12-RO-MOVE-CITY-CHANGELOG.md 与 2026-10-12-RO-MOVE-SYNC-SERVER-CHANGELOG.md；证据 _temp/ro-move-city-20261012/。

## [VERIFY][性能][TR-092] 2026-10-12 RO 移动同步与主城性能：范围核对

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 774—782 行；历史时点记录，不代表当前仍在执行。

- 已顺序读取协作入口、角色职责与当前用户修改；工作区已有大量未提交改动，各开发以当前文件备份为基线，不以 Git HEAD 覆盖用户稿。
- RO 客户端实际入口为 MPlayer.UpdateMove、MMoveComponent.onSyncMoveEvent/LagSyncUpdate；CostTime 为秒。当前正式链的帧累积毫秒/缺发送消费由客户端作最小适配，保持跳跃、技能根运动与 F 逻辑。
- 服务端实际参考为 MoveOperationReq.Process → RoleManager.FindBySession → ROMoveStartActionArgs → MoveComponent.OnMoveStart；本项目接入 OnlineRole 现组件与逻辑线程队列、附近广播、网关必要注册，不修改历史占位组件冒充正式链。
- 主只读确认 Unity 进程存在；city_performance 已通过新恢复的 MCP 确认编辑器非 Play、活动 Scene_Boot，并检查主城灯光引用。待灯光资产核对及性能调查，不提前给出 FPS 或低帧率唯一根因结论。
- 当前状态为开发中；编译、移动专项、独立审查与末稿一致性核对尚待执行。本轮不沿用历史技能断言作为通过证据。
- 临时证据 _temp/ro-move-city-20261012/，正式日志 项目修改日志/2026-10-12-RO-MOVE-CITY-CHANGELOG.md。

## [VERIFY][技能][TR-093] 2026-10-12 RO 技能组件重新对照：最终验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 783—795 行；历史时点记录，不代表当前仍在执行。

- ro_client_skill 两源、ro_server_skill 五路径开发完成，ro_skill_review 独立审查 PASS；主已读取 review-report.md、source-final-audit.json、test-final-audit.json 和保护/末稿清单，无业务返工项。
- 本轮客户端277完整生产源Rebuild零错误/37既有警告；服务器及Network/MMoRpgCommon隔离Rebuild零错误/11既有警告。独立72客户端生产源专项重新编译；服务器专项引用本轮真实程序集和正式配置，236编译输入当前SHA一致。
- 独立165断言：客户端25同名入口/真实11080/权威成功拒绝MP-CD/确认/自然打断/StopAll/BeenStop/Q两段/OnFinished/摘除，25真实经典发送链及24滑行回归；服务器28继承/核心/实例/毫秒CD/组独立/自然End/StopAll/Dispose/Q三段六秒/死亡，63权威F及准入回归。本轮重新执行，不累计历史通过数。
- 服务端开发另111专项（滑行63＋核心41＋真实GameGlobal时钟7）通过；1200ms忙循环真实推进到Tick25/活动UUID0，保持原亚毫秒余量修复，不与独立断言数合并。
- 主 root_verify.py 复核10类清单、2145去重文件：两端7末稿、277客户端编译/236服务器编译、72独立客户端源、236独立后端输入、35开发和102独立运行输入，以及1502关键资源/表/协议/场景保护，全部零漂移；root-manifest-check.json PASS。保护仅指已冻结关键路径，不夸称完整工作区保护。
- 首Q夹具假定0.6秒已结束失败，改按实际本段0.9秒验证后通过，业务未改。首次最终发送log实际为滑行24：Copy-Item保留旧mtime使dotnet增量沿用旧输出，主/独立证据检查FAIL阻断；核正确Program后显式Rebuild＋run --no-build，client-send-flow-final2.log真实发送25通过。旧失败日志及root-first-evidence-check.json均保留，未复制旧PASS日志替代。
- 新改六业务源中文声明/参数/局部用途、禁namespace/foreach/三目/弱业务载荷及一类一文件审查通过；工程仅3个Compile新增，未改协议/表/资产/正式测试挂载。已有并发源码改动保留，最终实际输入重新核一致。
- 边界：客户端Unity/动作状态机/UI/网络为薄替身，服务器真实角色/组件/配置但无网关，原发送失败提示不计交付。未验正式Unity画面/Animator/碰撞/声音、TCP/多服务联机/安装包/Bundle；未部署重启或清临时。外部无动画/QTE/Buff/目标/伤害/世界绑定缺项按用户确认范围明确列出。
- 正式分析 `项目分析/RO_SKILL_COMPONENT_PARITY.md`，日志 `项目修改日志/2026-10-12-RO-SKILL-COMPONENTS-CHANGELOG.md`；证据 `_temp/ro-skill-components-20261012/`。下一步原入口现场回归普通/组合/F/CD及自然结束，无已复现未解决业务故障。
- 六份正式记录收口检查通过（verify_records.py）。首检查误要求DECISIONS直写临时目录、第二次误要求专题正文引用自身，均为检查假设错误；按文件保存、四入口正式引用及实际结果核验后通过，无需修改正文迁就检查。

## [VERIFY][技能][TR-094] 2026-10-12 角色技能数据库数据补齐：查询验证与回包静态复现

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 796—804 行；历史时点记录，不代表当前仍在执行。

- 范围：仅改 `game_db.role_skill` 技能数据，不改代码/配置/表结构；对角色 ID=1、2 补齐。连接 localhost:3308/game_db（pymysql，只读探查 + 一次事务变更）。
- 变更前只读复核：两角色各 7 行、无 `(RoleID,SkillID)` 重复、SkillLevel 全 0；Bindkey 10010=Q/10080=F/10090=V、四个 Type2 技能为空；`rolebasearrt` MaxMP/CurrMP=500（满足 10070 耗蓝 180）。
- 备份校验：建 `role_skill_bak_20261012` 复制 14 行，与原表行数一致；另导出 `_temp/skill-db-fix-20261012/role_skill_original_20261012.csv/.sql` 与 `rollback.sql`。
- 事务结果：等级 UPDATE 实际变更 12 行、快捷键 UPDATE 实际变更 8 行；事务内 SELECT 权威复核两角色终态 10010/1/Q、10040/1/E、10050/1/R、10060/1/2、10070/1/1、10080/1/F，10090 保持 0/V，各角色仍 7 行无增删，全部符合才 COMMIT。
- 提交后新连接复核：备份表 14 行且全 0；`03_post_verify_packet.py` 严格复刻 `CentRoleModel.RoleSkillInfo`（含 10080 强制 F/兜底1级、10060 F→2）与 `RoleSkillComponent.TryCast`、客户端上栏规则，两角色 11080 回包 6 技能均判定“可施放且上栏”，10090 如预期不可用。
- 未做编译（无代码改动）；未启动服务/Unity 真人联机，回包为对真实库的服务端逻辑静态复现，最终以角色重新登录游戏内实测为准；在线角色需重登才重载内存已学等级。日志 `项目修改日志/2026-10-12-ROLE-SKILL-DB-DATA-FIX-CHANGELOG.md`，证据 `_temp/skill-db-fix-20261012/`。

## [VERIFY][技能][TR-095] 2026-10-12 RO 技能组件重新对照：验收准备

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 805—811 行；历史时点记录，不代表当前仍在执行。

- 本轮重新读取 RO 客户端 MSkillComponent 与 C++ ROSkillComponent/RoleSkillComponent 及本项目正式调用链。
- 已确认客户端基础逻辑已有实现，但缺部分 RO 同名管理入口；服务器正式施法在 OnlineRole 的 RoleSkillComponent，SceneEntity.SkillComponent 是未绑定世界的历史占位。
- 客户端开发、服务器开发及独立测试审查已经委派；待完成本轮真实编译、功能验证及逐方法对照。当前没有本轮通过结论，不沿用历史断言数代替验证。
- 范围分析 `项目分析/RO_SKILL_COMPONENT_PARITY.md`，日志 `项目修改日志/2026-10-12-RO-SKILL-COMPONENTS-CHANGELOG.md`；证据 `_temp/ro-skill-components-20261012/`，保留现有用户修改和临时文件，不部署或重启正式服务。

## [VERIFY][技能][TR-096] 2026-10-12 RO 技能发送模块：最终证据核对与文档收口

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 812—820 行；历史时点记录，不代表当前仍在执行。

- roll_movement_check开发编译，roll_local_move_review独立测试/审查PASS，无源码问题无需返工。skill_flow_records仅整理既有结果和重算SHA，不重跑或冒称新增独立测试；依据 `_temp/ro-skill-send-flow-20261012/review/final-audit.json` 与send-flow-runtime-final.log/previous-regression.log，无另行Markdown审查报告。
- 五新增类型及五meta、五既有源，共15正式路径；277生产源码Rebuild零错/37既有警告，证据在本轮develop目录。明细见 `项目修改日志/2026-10-12-RO-SKILL-SEND-MODULES-CHANGELOG.md`。
- 独立25发送专项＋24既有滑步回归通过：导航一次派发/发送、UpdateMove→发包→同UUID预测、UI刷新/事件回收、空目标即时提交、发送失败/组件卸载/池复用；原CD/动作/跳跃/Type3取消许可及F/2、500ms CD/667ms动作/21残影保持。
- 实核277编译源＋15正式路径＋72专项生产源＋49运行输入，共413项SHA零漂移，清单摘要与final-audit一致，root-manifest-check.json已扩充；不另计运行断言。
- 首缺组件Getter实际抛KeyNotFound，夹具误判null，仅改_temp断言Contains，业务未改、失败保留。实体/导航/事件分发器/组件池真实执行，空UpdateMove为记录器、Builder静态核对，Unity/UI/网络薄边界；未正式场景联机/安装包验收。
- 四入口及既有日志已收口，旧记录与临时保留；无已复现代码阻断，下一步游戏回归F及2/真实联机发送。分析 `项目分析/RO_SKILL_SEND_MODULE_FLOW.md`，临时待产品经理确认清理。

## [VERIFY][综合][TR-097] 2026-10-12 编辑器F仍被拒：现场原因及计时修复验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 821—829 行；历史时点记录，不代表当前仍在执行。

- 当前EditorSimulate日志20:57:47–49九次11170/10080+UUID1–9；编辑器实际Library/ScriptAssemblies/GM_Game.dll包含Type3/CanSlideCancel，未加载旧Dlls资产。运行GameServer部署TryCast的583条IL与已验末稿逐条相等。旧Player.log不用于解释编辑器现场。
- 初查只读角色1职业/HP/MP正常，数据库技能等级0与状态码32不能直接等同；没有据此删除技能规则或强行改数据库。日志`项目修改日志/2026-10-12-ROLL-STATEBLOCKED-FIX-CHANGELOG.md`，证据`_temp/roll-stateblocked-20261012/`。
- 后续实际进程只读堆锁定：角色/技能/保存数据loaded均true，在线、HP1000/职业1，活动首F UUID1截止Tick22；相隔16秒LogicTick均10。前述未锁定状态已更新，真实故障是主循环丢亚毫秒余量。独立未完成背包门闩会拒绝的10项实验仅说明另一分支，非本现场，不以其删除校验。
- 唯一GameGlobal.Update计时原点一行逻辑及四用途注释修复。实际GameServer/Network/MMoRpgCommon Rebuild零錯11既有警告，独立clock-review-report.md与clock-final-audit.json均PASS。主233编译源/工程、1末稿、58开发运行输入及38/36独立运行副本SHA一致，root-manifest-check.json保存证据。
- 开发真实主循环before1200ms/5038390次Update仅tick1/活动UUID1，正常时间契约失败；after1200ms/4673074次tick25/活动0、7断言通过。独立另编两个进程before5172976次tick1/活动1，after5068173次tick25/活动0、同7断言通过。执行完整GameGlobal/RoleManager/OnlineRole/技能/Timer生产逻辑，不手工改Tick、不Sleep、不手工结束动作，667ms动作及500ms CD自然到期后第二F成功。
- 专项没有真实网关，结束广播的无效会话提示保留为边界；非正式网络交付/场景表现验收。正式PID21016未重启或替换产物，仍旧计时实现；下一步重新编译重启GameServer后游戏回归。以前24/63组件专项不计主循环覆盖，没有因背包独立实验修改加载门闩。不发布或清临时。

## [VERIFY][技能][TR-098] 2026-10-12 F恢复特殊技能：末稿验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 830—837 行；历史时点记录，不代表当前仍在执行。

- 用户覆盖原本地动作选择，要求仍为特殊技能。7源本地接入已定向撤销，原技能请求/预测/槽位恢复，SceneMgr授权拼写修复保持。新逻辑仅客户端CanSkill和服务端TryCast按Type3/CanSlideCancel特殊互断及类别准入。
- 独立review-report.md/final-audit.json均PASS，无需返工。客户端272完整生产源Rebuild零错37旧，服务端GameServer及Network/MMoRpgCommon Rebuild零错11旧；主核272客户端编译、233后端编译源/工程、1后端末稿、29后端运行输入、63客户端专项源、42配置/夹具SHA一致，root-manifest-check.json保存证据。
- 独立客户端63实际生产源24项通过：三个实表CanSlideCancel1攻击可转F，不同ID的Type3同规则，Type2原名单；Q→F恢复真实SkillCastReq及预测/CurrentSkill，2旗标0拒F，自滑步不因500ms CD结束自断，完整667ms/21原残影后再次请求，F自身CD、原跳跃/未学习限制保持。
- 后端真实程序集63项通过：三个允许取消/两个禁止取消、同UUID或拒绝时旧活动保留、旧MP/CD不退、500ms CD/667ms后续动作、学习/职业/死亡/普通MP限制保持。旧End在全部校验后真实触发，handler结果→新Start只核源码顺序；测试角色无网关，相关会话无效提示是边界。首依赖绑定失败仅修_temp测试配置，正式配置不改、失败证据保留。
- 六源与前次本地方案before严格字节相等，新逻辑仅CanSkill/TryCast；SceneMgr授权单字符保持，无TrySlide/SlideAction。客户端Unity/网络/状态机/Ghost为薄边界，约5米资产未改不重测，未正式场景/联机/包验收。前次55项无网络断言不计当前成果。无剩余已复现阻断，下一步正式游戏体验；日志`项目修改日志/2026-10-12-ROLL-SPECIAL-SKILL-CHANGELOG.md`，证据`_temp/roll-special-skill-20261012/`，不发布或清临时。

## [VERIFY][综合][TR-099] 2026-10-12 F滑步翻滚本地动作：末稿验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 838—845 行；历史时点记录，不代表当前仍在执行。

- 用户确认Type3滑步走本地移动，不发施法请求；原500ms CD/667ms动画/约5米/白残影保持，客户端和后端现有普通技能限制不全局删除。
- 开发272完整生产源Rebuild零错37既有警告；独立review-report.md/final-audit.json均PASS。主核272编译输入、8末稿、64独立专项源与42配置/夹具输入SHA一致，报告`_temp/roll-local-move-20261012/root-manifest-check.json`。
- 当前64实际生产源55项断言通过：连续三次F零施法包/零预测/零MP，未学习仍可按原绑定启动，技能槽为空；每次0.5秒CD、667ms自动归池、21原计时残影。真实Collector采样→MMove→MModel提交一次，结束清根/取消未来Timer；默认动画不复播或套跑速，W/Shift不叠速度并在结束下一帧恢复方向移动。冷Runtime400ms不补旧影，Shutdown清理通过。普通2仍发送真实协议包并预测，跳跃互斥及不抢攻击保持。
- 用户确认后SceneMgr只修字段声明_loadingVie→_loadingView一字符，独立严格字节比对其余保持。首开发MEntity身份入口编译错已最小修为实际PlayerEntity；首专项夹具暖资产准备过早失败，仅修_temp夹具，原失败保留，末稿55项为验收依据。
- Unity/网络/状态机为薄边界；真实Collector方法的delta由测试提供，碰撞桥为记录器，Ghost记录原参数。约5米原资产未改，引用既有原生测距证据，不称本轮正式场景/原生墙体/联机/安装包通过。日志`项目修改日志/2026-10-12-ROLL-LOCAL-MOVE-CHANGELOG.md`，证据`_temp/roll-local-move-20261012/`；无剩余已复现阻断，下一步正式游戏重新运行验证，不发布或清临时。

## [VERIFY][综合][TR-100] 2026-10-12 进入主城后移动和按键：末稿验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 846—854 行；历史时点记录，不代表当前仍在执行。

- 最新Editor日志主城加载、角色模型绑定成功；旧Player.log只代表之前场景误卸载。待验证真实Unity Single销毁创角Camera后GetMainCamera与移动事件、主界面先建/输入晚到的快捷键注册。
- 仅PlayerEntity失效缓存时重取主城相机、MainCityMgr模型/输入就绪后原RegisterMainUIKeyHandler一行接入；其他业务保持。272完整GM_Game源Rebuild0错37既有警告，主核272当前输入SHA无漂移，两源精确before差异通过。
- 独立review/final-audit.json结论PASS，生产相机及三个注册方法提取逐字相等、完整PlayerInputCtr/PlayerInput与生产字节一致，主另核27原生输入SHA无漂移；没有要求额外防御机制。
- 独立同版本Unity原生Single及真实InputSystem27项通过，native-input-focus.log记录NATIVE_INPUT_PASS checks=27。旧相机销毁后before真实DispatchMovement零事件→after重新获取主城相机并派发；主界面先建/输入晚到before缺技能/UI订阅→after原注册入口恢复一次，重复原幂等入口不重复；真实QueueStateEvent的W移动、90度新相机方向、Shift/Space及四方向通过。
- 首隔离batchmode无GameView焦点导致键盘事件转编辑器，首18核心通过而真实W断言失败；仅_temp输入设置IgnoreFocus/AllDeviceInputAlwaysGoesToGameView后27通过，正式输入时序/配置不改，首日志保留。
- 运行真实完整PlayerInputCtr.cs/PlayerInput.cs和InputSystem DLL；相机/界面注册为生产方法逐字抽取，角色/移动桥/UI为薄记录，MainCityMgr新调用位置静态核。不称完整正式PlayerEntity/MMove位移、异步模型、技能权威或联机/包现场通过。证据`_temp/enter-game-move-20261012/`，日志`项目修改日志/2026-10-12-ENTER-GAME-MOVE-FIX-CHANGELOG.md`。不发布、关用户进程或清临时，正式重新运行后回归。

## [VERIFY][协作][TR-101] 2026-10-12 用户要求范围约束

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 855—858 行；历史时点记录，不代表当前仍在执行。

- 仅更新AGENTS.md、AIconfig/workflow.md、config.toml及本次记录；核对三处均覆盖主Agent/全部子Agent，明确禁止自行扩展功能、无关重构及未来需求。没有业务代码修改，不重复业务编译或运行测试；无未解决问题。

## [VERIFY][场景实体][TR-102] 2026-10-12 场景管理简单流程：末稿验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 859—867 行；历史时点记录，不代表当前仍在执行。

- 独立review/review-report.md与final-audit.json结论PASS，无需返工；主读取并审查最终报告/原生日志，与末稿SHA一致。
- 真实安装包Player.log显示Scene_MainCity因SceneBusinessRoot默认启用被误拒/卸载，随后MinMapWindow空引用；正式主城根active1、创角active0，不将默认状态作为成功条件。
- 客户端末稿759→80行、原三个字段/两个方法；272真实GM_Game源Rebuild零错37既有警告。主核272源码/268依赖及5原生输入SHA一致，五调用方before保持；末稿SHA7a2215d41625c7865137b3a85e2fec640c58d776da88fbc3347f295eaf7a89e1。
- 独立Unity2023.2原生Single末稿28项通过：创角禁根启用、已启主城不误拒、旧场景卸载、目标活动、provider提前SceneObject一致、OnEnable/Start及成功回调一次、进度/关闭、无Loading成功和失败结果处理。真实SceneMgr/UniRx，场景为原生层级/active副本；Yoo入口薄映射Unity，DDOL/禁UI为探针，失败为薄结果模拟，外围业务/Prefab移除，不称完整Global/UIRoot/正式网络登录或打包验收。
- 首原生夹具缺UnityEngine.UI依赖未执行，补真实DLL后native-final-ui.log记录SCENE_SIMPLE_NATIVE_PASS checks=28；仅修_temp依赖，保首失败。不计127行中间稿和旧编译。规则差异静态检查通过；证据`_temp/scene-simple-20261012/`，日志`项目修改日志/2026-10-12-SCENE-SIMPLE-FLOW-CHANGELOG.md`。
- 本轮无剩余已复现问题，未发布或替换运行包；正式包需生成并更新GM_Game热更DLL与资源后联机进入主城验收。不清理临时文件。

## [VERIFY][构建][TR-103] 2026-10-12 HFS启动长停：证据与编译

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 868—877 行；历史时点记录，不代表当前仍在执行。

- 最终：before/修改后Editor/非Editor Player分支三次真实Assembly-CSharp编译均0错1既有MSB3277；10源码+309依赖清单，主含2末稿共321SHA核对一致。独立review-report PASS、43真实Load.cs非Editor协程断言通过；测试Unity/Yoo/View为具名薄边界，并非真实HFS联机。覆盖版本失败不进入manifest、Host空拒/Editor空合法、各加载失败UI停止、真实字节50/100及正常零下载到登录，实际非法Assembly.Load异常路径亦验。
- 用户浏览器确认版本URL也一直等待或报错。网络服务器问题未解决，不能声称安装包联网启动通过；Load及启动场景属于主包需要重新Build。独立审查清单与SDK来源核通过，不清临时、不自动切离线或改HFS。

- 最新Player资源初始化成功，Curl42后manifest空版本错误，尚未下载；原Load版本检查对象错误、Scene_Boot初始TMP为固定50%。真实SDK确认版本/清单默认60秒无新增数据超时，编辑器模拟合法空版本需保留。
- 主探测HFS初查无监听，复查hfs.exe PID45908监听8080；版本GET配置IP/127各5秒零字节、根HEAD4秒零字节。不能宣称联网热更服务已正常，不自动换IP/离线/重启HFS。
- Boot实际Assembly-CSharp编译零错1引用版本冲突警告；开发专项与独立审查待追加。修改场景仅初始文字、Host/地址保持，Load主包AOT需要重新Build完整客户端才生效；未正式联网登录回归。
- 证据`_temp/boot-progress-fix-20261012/`，分析`项目分析/BOOT_HFS_VERSION_REQUEST.md`，日志`项目修改日志/2026-10-12-BOOT-HFS-PROGRESS-FIX-CHANGELOG.md`。

## [VERIFY][场景实体][TR-104] 2026-10-12 创角场景活动状态误判：验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 878—885 行；历史时点记录，不代表当前仍在执行。

- 独立Unity2023.2.20f1c1 PlayMode：Additive开始后最后Scene为有效未loaded的CreateRole，加载后与GetSceneByPath同handle；DDOL不进sceneCount。旧登录卸载后唯一CreateRole自动活动，重复SetActiveScene三次均false，定位原Commit误判；无需改provider。
- 唯一SceneMgr.Commit先检查GetActiveScene与LoadedSceneSnapshot是否相等，未活动再走原ActivateScene，真正失败仍原FailRequest；开发精准字节差异其余保持，独立最小差异审查通过。
- 开发272真实客户端源Rebuild零错/37既有警告，主独立核当前272编译输入SHA一致。末稿SHA408da564f5ac734edd70ba05ddf0a1accf64762b9eae0ace9a6ffc64ed44a0c9。首Edit实验未保存空场景拒Additive，仅修_temp夹具并保首失败，不改业务扩大适配。
- 原生实验与编译通过不等于正式登录服务器回归；未操作用户当前PlayMode或发布资源，现场需要重新运行登录到创角。证据`_temp/scene-active-fix-20261012/`，分析`项目分析/SCENE_ACTIVE_COMMIT_FIX.md`，日志`项目修改日志/2026-10-12-SCENE-ACTIVE-COMMIT-FIX-CHANGELOG.md`。
- 独立fixed原生实验提取同业务条件，已活动与尚未活动两分支均通过；未直接运行真实SceneMgr.CommitLatestRequest，不能将其计为完整场景调度联机验收。review-report.md审查PASS，无返工。

## [VERIFY][构建][TR-105] 2026-10-12 打包不进入登录：日志与产物核对

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 886—893 行；历史时点记录，不代表当前仍在执行。

- 子Agent按IL2CPP29元数据结构核真实旧global-metadata：无MMoRpgCommon镜像、无KnapsackType定义；今日临时生成metadata两者均有。Cecil核两份共享插件哈希/MVID相同且都有类型，当前裁剪DLL亦有，GM_Game明确引用该类型；证据`_temp/build-login-stuck-20261012/client/metadata-inspection.json`及`assembly-inspection.json`，主复核报告与Player-before.log一致。

- Player.log显示InitPackage Succeed、GM_Game557056字节和mscorlib/System/System.Core元数据OK，随后Assembly.Load抛TypeLoadException缺KnapsackType/MMoRpgCommon。更新提示停留为异常前最后UI状态，尚未调用登录场景加载。
- 运行路径为LYF_MMO/Build/GM_MMO.exe，exe/GameAssembly/global-metadata均8月26日；当前热更和裁剪AOT为10月12日。旧主包ScriptingAssemblies无MMoRpgCommon，当前link已保KnapsackType，当前裁剪C++已有该类型。定位主包与热更不配套，未修改业务。
- 未重新构建主程序或发布资源，Unity MCP没有Editor会话，不能声称运行通过。下一步为完整配套主包构建及登录界面验收；流程见`项目分析/BUILD_LOGIN_STUCK_AOT_MISMATCH.md`，日志`项目修改日志/2026-10-12-BUILD-LOGIN-STUCK-CHECK-CHANGELOG.md`。

## [VERIFY][综合][TR-106] 2026-10-12 滑行残影逐帧改造取消

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 894—899 行；历史时点记录，不代表当前仍在执行。

- 开发交接确认：本轮未写入任何业务文件，无需恢复，未构建或扩测。

- 用户在实现期间取消逐帧改造，要求使用此前计时器版本。只读确认类别Type==3、30Hz采样循环、EGhost回调/枚举与无载荷参数构造保持；本轮不宣称新增功能验收，不重复编译和旧专项。正式协作记录更新，业务改动停止；记录见`项目修改日志/2026-10-12-SLIDE-GHOST-FRAME-CANCEL-CHANGELOG.md`。

## [VERIFY][综合][TR-107] 2026-10-12 客户端生命周期函数排序：静态校验与独立校验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 900—907 行；历史时点记录，不代表当前仍在执行。

- 需求：把 8 个核心生命周期函数（Awake/OnEnable/Start/FixedUpdate/Update/LateUpdate/OnDisable/OnDestroy）整体上移到成员变量区之后、其他方法之前，嵌套类型移到类末尾；纯排版、零逻辑改动。按方法名识别，带参数同名方法（Update(int)/Update(float)/LateUpdate(float)）也算。
- 范围：`Scripts` 下 57 个文件（SceneEntity 22、UI 18、其余 17），不含 UI/test。使用 Roslyn 语法树工具（`_temp/lifecycle_reorder_tool`）重排，55 个写盘、2 个 NOCHANGE。
- 工具内置写盘前校验全部通过：代码 token 多重集合一致、注释/预处理多重集合一致、语法错误诊断不新增、成员数量守恒、排序符合约定；写盘前镜像备份到 `_temp/lifecycle_run/<模块>/backup`，BOM 保持。
- 独立第三方校验（独立 Python，`_temp/verify_independent/`）结论 PASS：55/55 代码本体一致、括号与 `#if/#endif`/`#region/#endregion` 配平、排序正确；目检 12 文件无注释脱离/截断/粘连；2 个 NOCHANGE 确认合规；git 只读核对仅方法移动、无逻辑改写。
- 遗留：零 SKIPPED/FAILED；客户端命令行无法跑 Unity 编译，需用户在 Unity Editor 做一次编译确认。日志 `项目修改日志/2026-10-12-客户端生命周期函数排序-CHANGELOG.md` 及三份分模块日志。

## [VERIFY][技能][TR-108] 2026-10-12 滑行残影按技能类别：验收准备

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 908—920 行；历史时点记录，不代表当前仍在执行。

- 核正式SkillInfo.Type=3滑行，替代MArtSkill残影注册的10080身份判断；不同ID的Type3应注册，10080若非Type3不注册，当前正式滑行保原行为。唯一源码最小适配，不改数据/业务框架。
- roll_client_develop对应编译及必要类别行为，主审差异和输入SHA；证据 `_temp/slide-category-20261012/`，日志 `项目修改日志/2026-10-12-SLIDE-CATEGORY-CHANGELOG.md`。当前未完成，不清临时。

### 滑行类别：最终验收

- 交接时发现并发整理使历史编译55源、专项16源的当前SHA改变，旧清单仅代表已测副本，不声称仍匹配当前文件。MArtSkill的35个成员声明、函数体、相邻注释逐字相等，仅成员顺序和五空行不同；保留整理，当前272源另行Rebuild零错/37既有警告，主独立核273项当前末稿及编译SHA一致。不重复语句未变的14项类别专项。追加证据为`current-ordered/`、`current-final-audit.json`与`member-order-evidence.json`，位于本轮证据目录。

- 唯一MArtSkill.FireEvents原ID条件替换为一次取cfg.SkillInfo并判断非null/Type==3；原类别语义由真实生成主表注释/bytes确认，不误用Art SkillType。不修改Core接口/数据/schema/协议或其他身份绑定。
- 客户端272源Rebuild0错37既有警告；复用63实际生产源和原薄Unity/网络边界，14项本轮类别专项通过：不同ID10081/Type3完整21残影，10080/Type2动作仍执行但0影，正式10080/Type3原影/白色四0.2/500ms CD/667ms动作/根1.458485无Charge保持。三次11080真实装配，样例行只在内存生成，正式数据不写。
- 首临时XML utf8编码不被MSBuild接受，只改_temp为utf-8，首失败保留；未为夹具改业务。主字节精准替换与MSkill/Core保持审计通过，272编译源SHA一致；开发末稿/专项输入清单保留。无已复现剩余问题，未正式PlayMode联机，不清临时。

## [VERIFY][综合][TR-109] 2026-10-12 翻滚位置纠正为最右：验收准备

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 921—932 行；历史时点记录，不代表当前仍在执行。

- 用户纠正最左为最右，仅UIRoot位置调整；验F位于整个栏最右、Q/E/1/2顺序、R/资源/HPMP保护、键绑定仍F和2及技能/源表/脚本保持。
- 客户端开发冻结本轮before并复用HUD原生预览，主目检/精准差异和SHA，不为纯布局重跑编译/旧技能测试。证据 `_temp/f-roll-right-20261012/`，日志 `项目修改日志/2026-10-12-F-ROLL-RIGHT-CHANGELOG.md`；当前尚未完成，不清临时。

### 翻滚最右：最终验收

- 唯一UIRoot七个X，F400、Q/E/1/2为-400/-328/-256/-184，空槽及同级Inset为-112；其他字节保持。首六值图漏Inset覆盖F，主/开发打回，补必要关联坐标后终稿通过，首图/日志保留。
- 同版本独立HUD原生1920全图及局部，日志HUD_PREVIEW_COMPLETE/退出0。主目检F最右且图标/键可见，左Q/E/1/2/暗空槽，R暗/双满资源/底HPMP保持；隔离预览不称正式现场。
- 主独立逐行比对仅七个位置行及其余字节保持，末稿/15原生输入/2图共18SHA一致；终稿UIRoot SHA637f8d7259fc2a4711a11460a600da6ebbbaaf0a7413bf89bb696114ddb10cbb。首次difflib自动junk匹配重复YAML的假计数失败只修临时审计，不返改资源。
- 纯布局，无源码/表/技能配置/场景/设置变化，不重复编译或旧技能专项。无已复现剩余问题，未正式PlayMode/Bundle发布，临时保留。

## [VERIFY][技能][TR-110] 2026-10-12 翻滚最左、位移5与特效：验收准备

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 933—955 行；历史时点记录，不代表当前仍在执行。

- 本轮以已完成F滑行末稿为before，只改所需HUD锚点/作者/绑定/Runtime、两源表/两bytes，原FX先查事实。验F最左且输入仍F、2原攻击、单根位移及原FX计时/挂点/回收；根5单位待用户可选澄清。
- 10080 CD改500ms/主表0.5秒，667ms完整动作结束可连续施放；验连续至少三次、CD内拒绝、动作限制/跳跃仍生效、不自循环/缓存。业务源码无改不重复历史全编译；必要导出/生成回读/原生预览和独立行为/保护待执行。
- 证据 `_temp/f-roll-refine-20261012/`，日志 `项目修改日志/2026-10-12-F-ROLL-REFINE-CHANGELOG.md`；当前没有本轮通过结论，原数据库只读、不清临时。

### 翻滚追加：开发验证与主目检

- 用户已确认每次约5米。独立原生单Skill Animator Controller完整水平累计(-0.038126,0,4.999854)米，作者根倍率1.458485，原完整窗口不变；无正式过渡/碰撞，不作为现场精确恒距保证。
- 原特效真实来源为旧MainRoleCtrl.Slider调用GhostEffect的白色角色网格残影，沿原四个0.2参数恢复。主已目检原Shader三姿态原生图，白色残影清楚、无粉材质；独立姿态渲染，不称正式技能全链现场。
- 显式执行原OnDisable/OnEnable复现顶点43749→0，CleanUp仅两缓存置null后43749→43749。EditMode最初SetActive不自动触发生命周期不能计为通过，后续显式原方法有Destroy编辑态提示，只证明缓存重收集，未据此改生产为DestroyImmediate或声称完整释放/淡出实测通过。
- 后端本轮17项真实程序集连续三次动作测试通过，500ms CD/667ms动作/MP0无伤害，每次动作结束可再放，CD完而动作未完仍拒重叠。两表全行真实生成读取、正常Luban两产物一致，精准三单元/其他ZIP成员保持；254编译输入未变复用原程序集，不重复历史编译/断言。本轮未访问数据库。
- 末稿真实导出、客户端编译、最左HUD和独立计时/连续输入专项仍在收尾；证据与首失败保留，不清临时。

### 翻滚追加：最终验收

- 客户端末稿272源Rebuild0错37旧，真实Unity作者Validate→Export→ValidatePublished七项通过，正式Runtime与原生导出字节完全一致。单Skill Controller约5米、白残影及最左F/Q/E/1/2原生图均主/独立目检通过，零攻击粒子冒用；本轮不重跑无关多比例/CD截图。
- 独立63完整生产源/实际五bytes/权限JSON，34项通过。连续三次F真实请求/预测，500ms冷却完但667ms动作未完不能叠放；动作结束可再按F，21个30Hz残影点沿原白色及四0.2参数，完整动作后不自动续放。400ms冷Runtime只出当前及未来影不补历史；中断杀未来Timer、未就绪/自然离槽不出新影，跳跃拒F和2。
- 首夹具遗留CanInterrupt=true与未接地导致假失败，替换为真实权限Catalog及显式实际接地；审计旧锚点假设错误仅修测试。原生EditMode生命周期显式原方法与Destroy提示边界保留，不改生产为DestroyImmediate，不把缓存/静态渲染通过称完整淡出或现场联机通过。失败证据均保留。
- 独立final-audit PASS：6554基线6544保持/10授权变化，GraphicsSettings在基线外单独与before精确比对；11正式路径，无新增资产/删除越界。UIRoot仅三锚点、Graphics仅一shader引用、作者/Runtime仅根Scale，源表精准三个单元/其他ZIP及旧样式保持；其余旧成果和本机设置保持。
- 主root-manifest-check PASS：4后端末稿＋7客户端末稿＋272编译＋327原生导出＋15HUD＋4残影＋3PNG＋63独立生产源＋11独立末稿＋42夹具输入，共748项SHA一致；后端254原编译输入由独立复核保持。目标源码/设置/文档diff检查通过；HEAD的UIRoot既有序列化空值尾空格报告保留，本轮before三锚点差异无新空白问题，未为检查清理用户原资源。
- 当前无已复现剩余阻断。未正式PlayMode/原生墙体/真实TCP联机或Bundle发布，重新运行或登录以重建配置，按F各动作末续放；日志/分析/四入口同步，临时保留。

## [VERIFY][综合][TR-111] 2026-10-12 F翻滚/滑步：验收准备

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 956—977 行；历史时点记录，不代表当前仍在执行。

- 验F使用Anim_JX_Slider_F真实动画和水平位移/碰撞唯一提交、既有图标/CD，原F技能在2按真实绑定施放；跳跃期间仍拒绝，不扩无敌/气力玩法或协议。
- 配置源表/生成数据/两端回读一致，现存角色11080与新角色默认/2占用处理需真实来源核对；动画根位移不得与程序Charge重复积分。既有HUD/场景/注释/技能时序保护，必要编译/专项行为和视觉证据待定。
- 证据 `_temp/f-roll-slide-20261012/`，日志 `项目修改日志/2026-10-12-F-ROLL-SLIDE-CHANGELOG.md`。当前仅找到资源，尚无本轮通过结果，活数据库只读，不清临时。

### F翻滚/滑步：开发验证与主初审

- 客户端272真实源Rebuild零错/37既有警告，Center/GameServer Rebuild零错；本轮不改正式技能/根运动/移动源码和HUD预制体。
- 原目录门禁拒绝指定滑行动画已复现，仅新增精确路径例外后真实Unity Validate→Export→ValidatePublished七项通过、退出0。正式新Runtime与真实导出字节一致；终稿327原生输入已冻结，草稿重排前证据单独保留。
- 真实现角色Avatar单剪辑独立采样80步/120Hz：0度水平约3.428米，90度方向跟随角色朝向；不当作正式施法/墙体/联机验收。唯一Animation根窗口、Scale1、无Charge/Fx/Audio，667ms时长与原80/120秒剪辑对应。
- HUD闲态及F/2的2.5秒CD外观样例共四原生PNG、两日志退出0，主已目检两局部：F现成位移图标、2原10060图标，资源双满/怒气暗，底HPMP保持。截图为隔离预制体渲染，不称正式现场。
- 主root-manifest-check初审PASS：参考8＋客户端末稿8＋272编译输入＋327原生终稿输入＋15HUD输入＋4图，共634项SHA一致。独立施放/CD/根碰撞专项与后端清单仍在终验；当前不把开发验证称为全部完成。

### F翻滚/滑步：最终验收

- 后端正式两服Rebuild零错，权威19项及真实ORM11080只读回包7项通过。覆盖零MP成功/667ms通知、动作结束仍保6秒CD、5.95秒拒绝/6秒允许、未学/错职业/死亡拒绝；10080零级回包一级/F、高等级保留、10060旧f改2且等级保留、其他键保持。测试只允许SELECT，活DB前后相同；无网关发送失败日志属于夹具边界，不算联机通过。
- 独立专项59完整生产源与实际Luban生成读取，24项通过：三bytes及身份/动画/CD/MP，11080→SkillCtrl→Factory装配，F发11170/10080、2发10060，冷Runtime不提前移动/就绪恢复根窗，Collector去Y后MMove唯一提交且根量不重复消费，667ms退槽停止根并保CD，重复F不发包，起跳/下降禁F与2、落地恢复、6秒到期续放。CharacterController墙体和Animator增量为薄边界，未原生现场碰撞。
- 三既有源xlsx全行与真实读取器一致、三bytes和正常Luban产物相同；原xlsx只必要sheet单元/追加行，其他ZIP成员和旧单元样式保持。没有schema/协议/生成类/DLL变更。首临时依赖重定向/配置、XML编码和资产数值解析失败只修测试目录，失败日志保留，没有以测试错误扩业务代码。
- 独立final-audit PASS：6545客户端/服务端基线6538保持、7授权变化，无删除越界；3既有源表另与backend before比对，新增仅6资产/meta。共16正式路径；所有旧技能/动画图标/HUD/场景/注释/跳跃门禁/本机设置保持。主目检HUD闲/CD及效果表通过，目标diff检查零问题。
- 主root-manifest-check PASS共1010项＝参考8＋客户端末稿8＋272编译＋327原生终稿＋15HUD输入＋4PNG＋后端末稿8＋254编译＋独立59生产源＋16末稿＋39夹具输入；零SHA差异。分析/日志/四入口同步；无剩余已复现阻断，未正式PlayMode联机或Bundle发布，临时保留。

## [VERIFY][技能][TR-112] 2026-10-12 跳跃禁止技能/普攻：验收准备

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 978—992 行；历史时点记录，不代表当前仍在执行。

- 目标：起跳后、多段跳及下降期间所有技能/普攻拒绝；落地后按原CD/MP及动作条件恢复。拒绝前不得发包、扣MP、开CD、推进普攻段数或切技能动画。
- 开发和独立审查已委派，拟复用现有IsJump与统一施法入口；核真实跳跃状态时序/所有入口/后端现有能力，做必要编译和真实源码行为验证/保护。不新增输入缓存/状态/协议，历史测试不算本轮新结果。
- 证据 `_temp/jump-cast-block-20261012/`，日志 `项目修改日志/2026-10-12-JUMP-CAST-BLOCK-CHANGELOG.md`。当前尚无通过结论，保所有现有修改，不清临时。

### 跳跃禁止技能/普攻：最终验收

- 唯一业务文件MSkillComponent.cs的CanStart空值条件后加入Entity.IsJump，summary及一条具体时序注释补齐，其余字节精确保持。全输入SkillCtrl.RequestAuthoritativeCast先校验再取UUID/发11170/预测，BeginPrediction也校验；Move.BeginJump派事件前true、实际下降接地EndJump清false。
- 本轮开发272真实GM_Game源Rebuild0错37既有警告，独立核272编译输入SHA与末稿一致而不重复同编译。开发27条、独立93条专项均链接50完整正式生产源（SkillCtrl/协议/Core/Component/Move/Entity等），Unity/状态机/资源/网络为专用薄边界，未正式PlayMode或联机。
- 覆盖起跳当帧、上升、二三段、下降：13既有输入绑定Q/E/R/F/1..9及直接预测被拒；无CD/MP/UUID/段号/技能动画/声音/特效副作用。真实Move接地EndJump后恢复各键发包/表现，原普攻CD继续空中递减，空中拒绝不刷新/清除旧CD，落地未到期仍拒、到期可普攻。
- 将唯一门禁源码换冻结before，同契约起跳Q产生CD/请求表现而失败；末稿复验通过。夹具首编译protected列表访问/编码问题仅修_temp，失败日志保留，不扩大生产流程。不把旧298测试计为本轮。
- 独立final-audit PASS：6545保护6544保持，唯一授权源码改动，无新增删除/越界，全部既有HUD/场景和另一任务注释保持；精准expected diff通过。主418项输入SHA一致＝1开发末稿＋272编译＋50开发方法源＋44测试夹具＋1独立末稿＋50独立方法源，root-manifest-check PASS。
- 正式分析 `项目分析/JUMP_SKILL_CAST_GATE.md`，日志与四入口同步。后端Move空壳，无真实跳跃源，本轮只客户端门禁、不造协议。无已复现剩余阻断；现场起跳/多段/下降禁Q及其他键、落地/CD仍待用户PlayMode验证，不清临时、不挂正式测试组件。

## [VERIFY][界面][TR-113] 2026-10-12 气力满条显示：验收准备

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 993—1007 行；历史时点记录，不代表当前仍在执行。

- 用户实反馈气力未满，前次静态初值核对不足；开发/独立重查真实Prefab原样初始化，不通过预览覆盖数值模拟默认满。
- 待定位遮挡/旋转/加载覆盖等事实根因并最小修复，默认双满且怒气暗需原生目检；核必要进度、引用、保护和SHA，不把历史满怒样例当本轮默认图。
- 证据 `_temp/skill-hud-stamina-full-fix-20261012/`，日志 `项目修改日志/2026-10-12-SKILL-HUD-STAMINA-FULL-FIX-CHANGELOG.md`。当前无新通过结论，保既有修改和新增注释，不保存正式场景、不清临时。

### 气力满条显示：最终验收

- 根因是New Scene.unity的UIRoot实例对StaminaFill组件9100000000000002033覆盖m_FillAmount=0，GUID与正式UIRoot一致。Prefab本身双满，业务初始化未清零。前次静态验收漏场景覆盖，本轮已纠正。
- 唯一业务资源修改为场景4行/176字节覆盖删除，94602→94426字节；独立重组完整before精确匹配after，其余字节保持。有效值左0.33333334/右0→双0.33333334；原弧方向、旋转、尺寸和Sprite一致，Ready默认失活。分析同步事实，不新增玩法/业务逻辑。
- 独立final-audit PASS：6482保护6481保持/唯一授权场景修改，无新增删除/越界，21合同/Prefab/所有源/用户设置和另一任务132文件注释保持。主读取精准差异及默认原生局部目检通过。
- 同版本隔离Unity加载当前正式Prefab的HUD子树/槽复制品，默认未经填充值覆盖，实际0.3333333/0.3333333、怒气Ready false，默认1920×1080/1280×720/3440×1440及两裁图共5PNG，正常原生日志退出0。11正式原生资产SHA与成功预览相符；不将后来失败场景变体夹具当成功输入。
- 场景变体临时实例化预览失败，主停止继续尝试并保留失败日志；没有场景现场原生通过结论。真实场景有效状态以覆盖解析/继承值/精确差异验收，正式Prefab默认图只证明原资源外观。未重编业务、无正式PlayMode/网络或Bundle验证，不Editor保存场景重序列化、不清临时。
- 主19项SHA一致＝2末稿（场景/分析）＋11正式原生资产＋5图＋1独立场景。报告/清单见review，root-manifest-check PASS。无剩余已复现缺陷；重新打开场景或重新运行使已加载旧实例使用修复。

## [VERIFY][综合][TR-114] 2026-10-12 全项目废代码分析与注释补充：验证通过

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1008—1014 行；历史时点记录，不代表当前仍在执行。

- 服务端编译：MSBuild（VS2022 Professional）构建 `LYF_Server.sln`（Debug，/m），退出码 0；MMoRpgCommon.dll、Network.dll、GateServer.exe、LoginServer.exe、GameServer.exe、CenterServer.exe 全部生成。剩余警告 CS0169/CS0649（未用字段）、CS0162（ShopCondition 不可达代码）、CS0114/CS0108（隐藏成员）、CS4014（LoginCtrl 未 await）均为已记录历史问题，无新增错误。日志 `_temp/server_build_20261012.log`。
- 客户端静态检查：`_temp/static_check_client_all.py` 对 256 cs 做大括号与 #if/#endif 配对检查，255 通过；唯一 `controller/InputController/PlayerInput.cs` 为 Input System 自动生成（逐字字符串转义导致脚本误报），该文件未被修改。
- 注释改动纯净性：`git diff --ignore-all-space` 核对，132 个改动文件（客户端45/服务端87）均为注释行；含单行类展开多行、两处 SqlSugar TableDescription 描述更正（表名未变）、个别 BOM 归一；关键字段 `_friends`/`_module_modify_time`/`space_still_need` 与 SugarTable 特性经回读确认均保留。
- 用户改动保护：RoleCurrInfoWindow.cs、SkillSlotWidget.cs 为 2026-10-11 技能HUD未提交工作，未触碰。未验 Unity Editor 实编译/PlayMode 与真机网络，建议 Editor 打开后对客户端做一次编译确认。

## [VERIFY][界面][TR-115] 2026-10-11 真气与气力初始满条：验收通过

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1015—1020 行；历史时点记录，不代表当前仍在执行。

- 用户明确双资源初始化满；开发从当前稿冻结before，主读取exact-diff和verification-result。UIRoot精确仅TrueQiFill/StaminaFill两处m_FillAmount由0改0.33333334，窗口精确仅一注释改变，执行逻辑保持；分析同步当前默认语义。
- 初始化链只读核对MainView.InitView→InitWindow→EnsureSkillSlots只建技能槽，WindowBase不清两弧，RoleInfoCtrl仅HPMP，两接口全Scripts仅声明无调用。Core ReadyVisual保持失活，初始化满资源不会点亮怒气；后续无效max仍空。
- 开发验证PASS，主3末稿与6初始化输入共9项SHA一致，证据 `_temp/skill-hud-resource-initial-full-20261011/`；无新增测试脚本、编译/历史行为或原生重跑，未验现场/Bundle。日志 `项目修改日志/2026-10-11-SKILL-HUD-RESOURCE-INITIAL-FULL-CHANGELOG.md`，无未解决项，临时保留。

## [VERIFY][界面][TR-116] 2026-10-11 中央圆盘嵌槽返工：验收准备

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1021—1035 行；历史时点记录，不代表当前仍在执行。

- 新局部参考要求硬质分层盘框、内部剑形浮雕及嵌入两侧的窄资源槽，前轮悬浮外弧需返工。开发和独立审核已委派，主先目检默认原生局部，失败继续修改。
- 本轮拟纯Prefab/素材修改；检查默认暗空、部分资源与满怒样例、比例/引用/点击穿透、侧槽和底HPMP保护、原业务源及用户本机设置保持。若业务源无改，不把此前272源编译和24条断言称为新测试。
- 本轮证据 `_temp/skill-hud-center-inset-20261011/`，日志 `项目修改日志/2026-10-11-SKILL-HUD-CENTER-INSET-CHANGELOG.md`；当前未产生通过结论，玩法/正式现场/Bundle不在此次视觉验收范围。

### 中央圆盘嵌槽返工：最终验收

- 两Prefab修改、三PNG/meta新增，共8资产路径；全部业务源码保持，本轮不重编译、不重跑历史24接口断言。独立source-protection核旧272编译输入与四方法源当前SHA一致，仅作为保持证明。
- 首稿等距同心亮线被主与独立打回；返工为宽灰阶倒角、少数硬轮廓、暗槽和五剑浮雕后，主/独立默认/非对称部分/满资源加满怒原生目检通过。两侧资源紧贴框内凹槽，满怒内圈独立，下R清楚，底HPMP/侧槽无变化。
- 同版本Unity隔离复制品真实渲染：默认1920×1080、1280×720、3440×1440，部分及满态1920×1080；5全图、3整条及3中央原像素裁图，共11PNG。121原生输入冻结，三最终日志HUD_PREVIEW_COMPLETE/退出0，无脚本或Shader错误。首失败图/日志保留，不覆盖前轮证据。
- 独立final-audit PASS：6476保护6474保持，两授权Prefab修改和六新增，无删除越界；14合同/所有源/工程/本机设置保持。HUD外、底条和侧槽原块保持，三新GUID唯一、单Sprite透明导入/引用有效，无新增未解析fileID；原三接口引用与默认Ready失活/资源填充0保持，Graphic不截获点击。
- 五比例几何纳入实际SkillCoreWidget挂R锚点及满态子Graphic，1920×1080、1280×720、1920×1200、2560×1080、3440×1440均无屏外图形。仅前三默认比例有本轮原生图，不把几何审计称为五比例实渲染。
- 主root-manifest-check PASS：8开发末稿＋121原生输入＋11图＋8独立末稿，共148项SHA一致。两报告及evidence-manifest-check留review，分析/四入口/正式日志同步；资源玩法/现场服务器施法/Bundle未验或发布，隔离样例不代表已接通资源数据。无剩余已复现结构阻断，后续由用户确认观感，临时保留。

## [VERIFY][技能][TR-117-FILES] 查询任务的正式文档产物：2026-10-10 技能编辑器图文解析文档（只读分析 + HTML 文档新增）

- 该任务记录的正式文档路径：`项目分析/SKILL_EDITOR_GRAPHIC_GUIDE.html`。
- 查询正文、当时状态及原文验证边界见 `Docs/AI/QUERY_TASKS.md` 的 `[TR-117]`；本条只保留文件产物追溯，不新增完成或检查结论。

## [VERIFY][技能][TR-118] 2026-09-27 Q 普攻接段节奏返工独立复验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1045—1053 行；历史时点记录，不代表当前仍在执行。

- RO 证据：`项目分析/RO_Q_COMBO_TEMPO_REFERENCE.md`；RO 剑士三段普通攻击 `SkillStateTime=0.7s`，与 `Time=0.9s`、`AnimationLength=0.6s` 分离。本项目 MJS 剪辑更长，700ms 是参照 RO 节奏的本项目配置值。
- 构建：客户端 `GM_Game`、服务端 `GameServer` 隔离构建均 0 错误；客户端完整开发构建 42 条既有警告，独立增量构建 38 条；服务端首次完整构建 11 条既有警告，注释补齐后增量构建 7 条。`Network` 开发构建 0 错误、0 警告；正式客户端插件与对应开发构建 DLL SHA256 一致为 `33F5B3359A26C56910BDD5D8C8A00A43AA298DEEB67452F9D1BB5A3AA2617E3C`。独立不同输出目录重编的 DLL 哈希不用于字节一致性判断。
- 客户端源码断言：`MSkill` 在 0.699s 仍处技能状态、0.701s 状态结束，完整动画时钟独立保留；单动作技能原完整时长行为保持。旧服务器结束通知按施法 ID 清旧实例，不清理新段。
- 服务端正式组件断言：650ms 下一段被活动施法拒绝，700ms 接受第二段；段序 0→1→2→0，失败/重传不推进，六秒重置和其他单动作时长保持。11170/11171 的 `CastDurationMs` 对 Q 返回 700ms。
- 配置：`#SkillPresentationInfo.xlsx` 增加 `SkillStateTimeMs`，Q 三行均为 700ms，原 2167/1967/2500ms `DurationMs` 保留；Luban 生成类、bytes 和客户端插件已同步。业务代码目标范围静态规范与差异检查通过，未添加输入缓存、动画倍率或自动三连。
- 未运行：未注入 Unity 键盘、未创建角色探针，未运行 Game View 画面及四服联机。长 MJS 剪辑在 700ms 后转 Idle 或下一段的实际关键姿态、过渡流畅度和移动阻断需要用户手动确认。

## [VERIFY][技能][TR-119] 2026-09-27 Q 普攻三段轮换最终验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1054—1063 行；历史时点记录，不代表当前仍在执行。

- 参与：架构子 Agent 提供 RO 对照，客户端与后端子 Agent 开发，独立测试审查子 Agent 复验并打回租约及注释问题；返工后终审通过，无剩余阻断项。范围见 `项目修改日志/2026-09-27-RO-NORMAL-ATTACK-COMBO-CHANGELOG.md`，设计见 `项目分析/RO_NORMAL_ATTACK_USING_TIMES_DESIGN.md`。
- 独立构建：最终 `GM_Game` 隔离构建 0 错误、42 条既有警告；`GameServer`/`Network` 0 错误、11 条警告。证据保留 `_temp/skill-combo-review/client-build.log` 和 `server-build.log`；正式客户端 Network.dll 与后端开发输出哈希一致，未用独立不同构建目录的 DLL 覆盖正式插件。
- 核心断言：直接编译正式 Core/Data/Prediction 源码，验证 0→1→2→0、超过十秒重置、MP 不足不推进、技能重装继承、旧确认/取消/超时不覆盖新 CD、重复结果拒绝、最新权威 CD 和单动作待确认行为；`_temp/skill-combo-review/core-check.log` 为 PASS。
- 服务端断言：使用正式组件源码的隔离构建结果，验证三段轮换、六秒等号续段/超过六秒重置、拒绝/缓存重传不推进、2167/1967/2500ms 段时长及结束帧、11170 字段 11/11171 字段 7 段数 protobuf 往返、其他单动作时长不变；`server-check.log` 为 PASS。内存夹具没有真实网关，结束发送的无效会话日志属于预期，不能用作网络通过证据。
- 配置：正式表现表新增两行，重新生成 bytes 为 759 字节，SHA256 `502F5FC7D19DD28924ECF0B689105F1356FD66C781709AA043D5AE8D98C1E3CF`；原五行业务值一致。空音频单元格在归档检查中为空字符串、正式回读为空白 null，Luban 语义同为空音频，不声称 JSON 字节完全一致。原 SkillInfo/SkillEffectInfo 表及 bytes 保持不变；新增源码 `.meta` 与 csproj 索引已登记。
- 静态审查：目标业务范围无新增命名空间、foreach、三目或弱类型业务载荷；中文注释复审通过。目标 `git -c core.whitespace=cr-at-eol diff --check` 通过；CRLF 按正常换行处理，未为此全文件格式化。
- 未运行：未注入键盘、未生成 Unity 测试角色/探针，也未做 Game View 物理按键或真实四服联调。手动验收按当前测试角色逐次 Q 检查三段顺序、动画结束续段、第三段完整播放、施法移动阻断；联机检查本人确认不重播、他人段数及 MP/CD。

## [VERIFY][技能][TR-120] 2026-09-27 RO 技能施放链最终独立复验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1064—1070 行；历史时点记录，不代表当前仍在执行。

- 独立构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/ro-cast-review/final6 --verbosity quiet -clp:ErrorsOnly`，0 错误、38 条既有警告；临时产物保留在 `_temp/ro-cast-review/final6`。
- 静态检查：`test.cs` 仅构造并经正式解析入口装载 11080，全项目技能键只有正式 `SkillCtrl` 订阅；无 Client 时可进入 `BeginPrediction`，有 Client 时先尝试 11170；11170 序号确认、11171 服务器施法 ID 去重、未确认时长加 2 秒回滚、已确认时长加 10 秒兜底、技能槽逐帧读取核心 CD 和单技能优先的组 CD 展示均通过源码核对。
- 规范与返工：目标文件未发现新增 `namespace`、`foreach`、三目、`dynamic` 或 `object[]`；目标 `git diff --check` 通过。已移除来源标记、业务离线施法分支、单请求重试和过期状态、本地组 CD 预测、测试侧第二按键订阅及无调用的快照包装入口；独立审查通过。
- 未执行：Unity Game View 的用户物理键盘与动画画面验收、真实四服联机回包、MP/伤害结算。11171 缺少 RO 请求 UUID，当前依赖服务端先发送 11170 再发送 11171 的现有顺序；极端乱序为剩余风险。完整修改见 `项目修改日志/2026-09-27-RO-SKILL-CAST-ALIGNMENT-CHANGELOG.md`。

## [VERIFY][综合][TR-121] 2026-09-24 项目文档归档整理验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1071—1079 行；历史时点记录，不代表当前仍在执行。

- 归档结果：项目修改日志目录包含 45 份历史修改/测试流程记录，另有 README 与本次变更说明；项目分析目录包含 9 份历史项目解析、架构设计和方案文档，另有 README。两个目标目录原已存在；归档保留原内容。
- 约束更新：`AGENTS.md`、`AIconfig/workflow.md`、产品经理/策划架构师/测试角色说明明确归档边界；`Docs/AI/PROJECT_CONTEXT.md` 与 `DECISIONS.md` 已记录长期规则。
- 路径核验：`Docs/AI` 仅包含四份协作入口记录；根目录未发现散落的 Markdown 修改日志或专题设计正文。
- 验证：检查两个归档目录清单、角色模板、协作记录引用；本次修改的文档和配置未发现行尾空白。没有运行代码构建，因为本轮不涉及业务代码。
- 全仓差异检查：`git diff --check` 受既有用户改动影响失败，报告涉及 `AssetBundleCollectorSetting.asset`、Unity `.meta`/场景、`NpcManager.cs`、`MainRoleCtrl.cs`、`OnlineRole.cs` 等文件的空白。上述文件不属于本次范围，未作修改。
- 未解决问题：历史协作记录中少数描述性文字仍称“根目录变更说明”；本次修正了可定位的旧文件路径，保留纯叙述性历史文字。

## [VERIFY][场景实体][TR-122] 2026-09-15 SceneEntity 中文注释补充（Action + 3DModel 批次）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1080—1090 行；历史时点记录，不代表当前仍在执行。

- 变更范围：`SceneEntity/Component/Action/` 15 个文件 + `SceneEntity/3DModel/` 14 个文件，共 29 个 C# 文件。
- 修改内容：仅新增中文 XML 注释和行内注释，业务逻辑零变更；已有注释保留。
- 覆盖内容：
  - Action 目录（15 个）：MActionComponent 动作状态抽象基类完整生命周期（许可/开始/更新/结束/强制停止/清理）、事件注册与转移判定钩子、PlayAnim 动画播放逻辑；MBasicActionComponent 通用基类默认转移判定；MDefaultActionComponent 默认状态 idle/move 自动切换与移动战斗事件刷新；MSkillComponent 技能组件快捷组件与动作状态双重身份、技能集合维护；MSingingComponent/MSpecialComponent/MBeHitComponent/MDeadComponent/MStunComponent/MBornComponent/MClimbComponent/MCollectComponent/MDanceComponent/MFishingComponent/MFearRunComponent 各具体动作状态类级职责、StateName 动画映射、事件注册说明。
  - 3DModel 目录（14 个）：MModel 模型加载会话版本号校验/异步回调防串台/Animator 生命周期；MModelMgr 模型对象池单例；MModelOriginData 模型原始配置数据；MObjectAnimator 简化版动画器；MAnimator Animator 封装全部字段/属性/方法注释（状态常量、参数缓存字典、Play/CrossFade 原生状态与兼容参数双路径、OverrideAnim 片段替换、Reset 资源释放）；MAnimatorPool 双类型对象池；MAnimatorStateCompatibility 旧 Controller 参数兼容协议（Movement/Action 参数映射）；MAnimatorControllerAssetProvider YooAsset 异步加载与 Location 校验；MAnimatorControllerAssetLease 资源租约幂等释放；MAnimationStateNames 状态名常量逐项说明；MAnimationClipInfo 引用计数与片段元数据；MAnimationAssetProvider 动画资源适配器；MAnimInfo 逻辑 key 与片段绑定信息；MGameObject 变换缓存机制（未加载时缓存属性，加载后自动同步）。
- 验证方式：人工抽查 MDefaultActionComponent.cs、MFearRunComponent.cs、MAnimationClipInfo.cs、MAnimator.cs（前 80 行）、MGameObject.cs（前 60 行）五个代表性文件，确认类级 summary、字段注释、方法 XML 注释（含 param/returns）、关键逻辑行内注释齐全，业务代码行未改动。
- 未执行：Unity 编译（按任务要求，仅注释变更，无需编译验证）。
- 未解决问题：无。

## [VERIFY][场景实体][TR-123] 2026-09-15 SceneEntity 中文注释补充（Component 批次 16 个文件）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1091—1103 行；历史时点记录，不代表当前仍在执行。

- 变更范围：`SceneEntity/Component/` 根目录 11 个文件 + `Component/State/` 5 个文件，共 16 个 C# 文件。
- 修改内容：仅新增中文 XML 注释和行内注释，业务逻辑零变更；已有注释保留。
- 覆盖内容：
  - 组件基类与容器：EntityComponentBase 事件订阅记录/批量解绑/RebindOwner 宿主重绑/ResetForPool 池回收；EntityComponentContainer 按类型字典与顺序列表双结构、Initialize/Update/LateUpdate 顺序驱动、Shutdown 逆序回收、Invoke 异常隔离。
  - 组件管理器：MComponentMgr 普通组件池与动作状态池双池结构、CreateComponent 生命周期校验、CreateSpecialComponents 四个专用组件创建、ReleaseSpecialComponents 状态机唯一回收 Skill 约定、AcquireActionState/ReleaseActionState 对象池复用、RegisterExtraComponents/AppendExtraComponents 按实体类型追加组件。
  - 专用组件：MMoveComponent 目标快照/插值系数/移动开始结束事件；MBuffComponent Buff ID-等级表；EntityTransformComponent 快照直写；EntityRoleComponent RoleId/UserName；EntityProjectileComponent 超时 MarkPendingDestroy；EntityMonsterComponent MonsterId/Rank；EntityAttributesComponent 范围钳制与 Hp/Mp/综合属性事件派发；EntityComponentMarker 占位不可复用语义。
  - 状态机：MStateMachine 按实体类型注册动作状态清单（集合/NPC/怪物/玩家分支）、CanSkill/CanTransfer 权限校验、TransferToState 状态切换与 StateChanged 事件、UpdateSeconds 完成自动回默认、LateUpdate 延迟动画播放、Shutdown 容器统一回收；MActionStateContainer 三套索引（StateDefine/复合键/顺序列表）；IMStateTransform 接口契约；CommonStateTypeTool 许可矩阵越界保护；ActionStateKey 复合键相等比较与哈希。
- 验证方式：人工抽查 MStateMachine.cs（状态机核心）、EntityAttributesComponent.cs（含范围钳制与事件分支）、EntityComponentContainer.cs（生命周期包装）三个代表性文件，确认类级 summary、字段注释、方法 XML 注释（含 param/returns）、关键逻辑行内注释齐全，业务代码行未改动。
- 未执行：Unity 编译（按任务要求，仅注释变更，无需编译验证）。
- 未解决问题：无。

## [VERIFY][场景实体][TR-124] 2026-09-15 SceneEntity 中文注释补充（根目录 + Entity 批次）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1104—1114 行；历史时点记录，不代表当前仍在执行。

- 变更范围：`SceneEntity/` 根目录 9 个文件、`SceneEntity/Entity/` 18 个文件，共 27 个 C# 文件。
- 修改内容：仅新增中文 XML 注释和行内注释，业务逻辑零变更；保留已有注释。
- 覆盖内容：
  - 根目录：MEntityPool 对象池单例职责/Acquire/Release 方法参数与副作用、SceneEntityWorld 实体世界 UID 分配/Add/Remove/Update/LateUpdate/Clear 完整生命周期、ROObject 接口全部成员逐一注释、SceneEntityFactory 工厂方法与 Builder 映射、MSceneObject 变换快照与表现控制器关联、MObject 通用对象层组件容器与事件分发器生命周期、MEntity 实体层四个专用组件与模型持有关系、ProjectileType/MonsterRank 枚举逐项说明。
  - Entity 目录：RoleEntity 其他角色同步信息、PlayerEntity 本地玩家业务组件容器与背包/金币操作 API、MonsterEntity 怪物等级档位、NpcEntity NPC 全部配置字段、BossEntity/EliteEntity/MercenaryEntity 继承关系、DummyEntity/EagleEntity 训练假人与鹰类召唤物、CollectionEntity 采集物、TrapEntity 陷阱、ProjectileEntity 及四类派生投射物（Target/Rect/Ring/Anim）继承关系说明。
- 验证方式：人工抽查 MEntity.cs、PlayerEntity.cs、NpcEntity.cs、ProjectileType.cs 四个代表性文件，确认类级 summary、字段注释、方法 XML 注释（含 param/returns）、关键逻辑行内注释齐全。
- 未执行：Unity 编译（按任务要求，仅注释变更，无需编译验证）。
- 未解决问题：无。

## [VERIFY][界面][场景实体][TR-125] 2026-09-15 SceneEntity 中文注释补充（Builder + event 批次）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1115—1125 行；历史时点记录，不代表当前仍在执行。

- 变更范围：`SceneEntity/Builder/` 15 个文件、`SceneEntity/event/` 13 个文件（含 eventclassArg 子目录 7 个），共 28 个 C# 文件。
- 修改内容：仅新增中文 XML 注释和行内注释，业务逻辑零变更；保留已有注释。
- 覆盖内容：
  - Builder 目录：类级职责说明、Build 方法 XML 注释（uid/configId 参数、返回值）、构造函数注释、字段说明、组件挂载流程行内注释、子类继承关系说明。
  - event 目录：事件分发器类级职责、字段说明、Add/Remove/Dispatch/Clear 方法 XML 注释（参数、返回值、副作用）、监听器快照复制和 Clone 副本机制行内注释、输入桥接方法完整参数说明、事件参数类字段/属性/构造/Clone/Recycle 注释、MEventType 枚举逐项分类说明。
- 验证方式：人工抽查 ProjectileEntityBuilder.cs、MEventDispatcher.cs、EntityInputEventBridge.cs 三个代表性文件，确认类/字段/方法/参数/返回值/关键逻辑注释齐全。
- 未执行：Unity 编译（按任务要求，仅注释变更，无需编译验证）。
- 未解决问题：无。

## [VERIFY][协作][TR-126] 2026-09-15 代码注释完整性约束登记

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1126—1131 行；历史时点记录，不代表当前仍在执行。

- 变更范围：`AGENTS.md`、`Docs/AI/PROJECT_CONTEXT.md`、`Docs/AI/DECISIONS.md`、`Docs/AI/CURRENT_TASK.md`。
- 验证结果：已核对四个文件均包含“类型/接口、函数、字段/属性/常量/事件、参数及局部变量必须有具体中文注释”的约束，并明确注释需描述实际语义。
- 本轮未修改业务代码，未执行编译或运行测试；后续代码审核和静态检查需增加注释完整性检查。

## [VERIFY][界面][TR-127] 2026-09-15 EntityId 删除与 UID 迁移验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1132—1139 行；历史时点记录，不代表当前仍在执行。

- 公共库构建：`dotnet build LYF_Server/MMoRpgCommon/MMoRpgCommon.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/common-uid-build`，成功。
- 客户端构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/uid-build-final4`，成功，0 个错误。
- 服务端 GameServer 构建：`dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/server-uid-build3`，成功，0 个错误。
- 静态扫描：实体相关源码不再包含 `EntityId` 类型、`EntityIdAllocator`、`new UID` 或损坏的 `ulong` 构造调用；`ConfigId`、`RoleId` 语义保留。
- 未执行：Unity Play Mode、实体池多轮复用和网络联调。

## [VERIFY][界面][协作][TR-128] 2026-09-15 UID 迁移主 Agent 最终复核

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1140—1148 行；历史时点记录，不代表当前仍在执行。

- `MMoRpgCommon` 构建：成功，0 错误；输出 `_temp/common-uid-final2`。
- Unity 客户端 `GM_Game` 构建：成功，0 错误；输出 `_temp/uid-final-build`，仅有 31 个既有警告。
- GameServer 构建：成功，0 错误；输出 `_temp/server-uid-final`，仅有既有警告。
- 静态扫描：业务源码未发现 `EntityId`、`EntityIdAllocator`、`OwnerEntityId`、`SceneEntityId`、`TargetEntityId` 等旧身份命名；实体世界字典、快照字段和池接口统一使用 `ulong UID`。
- `git diff --check` 仅报告既有换行格式转换及 3 个文件末尾空行，不影响编译。
- 未执行：Unity Play Mode、对象池运行态多轮复用和网络联调。

## [VERIFY][技能][TR-129] 2026-09-14 RO 状态许可与 Animator 兼容返工

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1149—1156 行；历史时点记录，不代表当前仍在执行。

- 构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/animator-state-fix-build`，0 错误，31 个既有警告。
- 静态规则：状态与动画目标文件 `namespace=0`、`foreach=0`、三目运算符=0、`object`/`object[]`/`dynamic`=0。
- 状态许可矩阵 15×15 与 RO `state_machine_define.cpp` 逐项一致。
- `controllerLocation` 已通过 YooAsset 异步租约绑定 Controller，回收释放并以会话版本忽略过期回调；Movement/Action 兼容映射已落地。
- 未执行：Unity Play Mode、真实 YooAsset Controller 地址和动作参数运行态回归。

## [VERIFY][技能][TR-130] 2026-09-14 MAnimator 类结构说明

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1157—1161 行；历史时点记录，不代表当前仍在执行。

- 已根据当前实体代码和已审核设计核对继承、组合及管理器关系。
- 本轮仅输出设计结构，不涉及源码修改，因此未执行编译或 PlayMode；后续实现前仍需按文档验收清单验证。

## [VERIFY][场景实体][TR-131] 2026-09-14 实体事件参数强类型重构基线

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1162—1168 行；历史时点记录，不代表当前仍在执行。

- 本轮开始前检查：`SceneEntity/event/MEventArgs.cs` 使用 `Data: object`、`IntValue`、`FloatValue`、`BoolValue`、`StringValue` 承载多个不相关业务事件；`IEventArg.Init(params object[])` 也存在无类型载荷。
- 调用基线：`ROObject`、`MObject`、`MEventDispatcher`、`EntityInputEventBridge`、`EntityMoveComponent`、`EntityTransformComponent` 和 `EntityComponentBase` 直接依赖 `MEventArgs`。
- 参考范围：已读取 `F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Event/IEventArg.cs`、`MEventDispatcher.cs`、`MEventType.cs`；参考项目采用具体事件参数类配合 `IEventArg`，而不是单一万能数据对象。
- 本条为重构前基线记录，尚未修改业务代码、尚未构建。

## [VERIFY][场景实体][TR-132] 2026-09-14 实体事件参数强类型重构最终审核

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1169—1176 行；历史时点记录，不代表当前仍在执行。

- 独立代码审核结论：通过。`InputKeyDown`、`InputKeyUp`、`InputKeyHeld` 只传递 `KeyInputEventArgs`；`InputAxisChanged` 只传递 `InputAxisChangedEventArgs`；新增 `InputActionStarted`、`InputActionPerformed`、`InputActionCanceled` 三个动作事件且只传递 `InputActionEventArgs`。
- 输入桥接对动作事件执行白名单校验，错误事件类型会记录错误并拒绝派发，避免后续重新混用事件参数类型。
- 最终复构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/entity-event-review-final-build`，0 个错误、31 个既有警告。
- 静态检查：客户端业务代码中 `MEventArgs` 与工程编译项为 0；本次事件范围未发现 `object`、`object[]`、`dynamic`、`Init(params object[])`、`foreach`、三目运算符或新增命名空间。
- 未执行 Unity Play Mode 的真实键鼠、触摸和 Input System 动作监听回归；该项不阻塞本次静态重构验收。

## [VERIFY][技能][TR-133] 2026-09-14 MAnimator 与角色/怪物状态机设计文档检查

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1177—1185 行；历史时点记录，不代表当前仍在执行。

- 参考源码：已读取 `F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/AsyncObj/MAnimator.cs`，并核对 `AsyncObj/MAnimationMgr.cs`、`Components/State/MStateMachine.cs`、`Components/Action/MActionComponent.cs` 的职责和调用时序。
- 产物：新增 `项目分析/ANIMATOR_STATE_MACHINE_DESIGN.md`，覆盖 MAnimator API、AnimatorOverrideController、Clip 引用计数/YooAsset 适配、对象池回收、异步回调过期保护、EntityViewComponent 绑定、Player/Role/Monster 状态机和旧 RoleFSM 迁移方案。
- 代码变更：本轮未修改客户端、服务端、协议、配置表、场景或预制体业务代码，因此未执行编译和 PlayMode。
- 静态文档核对：文档明确记录无命名空间、无 `foreach`、无三目运算符、顶级类型一文件一类型等后续实现约束；未发现需要编译的源码变更。
- 审核状态：独立子 Agent 已完成最新版本复审并通过；确认 LateUpdate/PendingDestroy 时序、精确毫秒/秒接口、Global.Awake 注册顺序、ActionStateKey 独立容器、MActionComponent 的 MEventArgs/Attached/ResetForPool 契约，以及通过 EntityViewComponent.AnimatorFacade 的安全访问均已明确。
- 未解决：动画配置表字段、具体 YooAsset location 约定、状态同步协议字段和各实体动作组件清单需在实现前由客户端/策划确认。

## [VERIFY][技能][TR-134] 2026-09-14 MAnimator 设计文档第一轮返工

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1186—1191 行；历史时点记录，不代表当前仍在执行。

- 审核反馈：初审不通过，缺少动作状态注册/回收、时间单位统一、RuntimeAnimatorController 生命周期、Clip 异步租约配对、状态事件仲裁和旧 RoleFSM 全量映射。
- 文档修订：补充 `MActionStateRegistry/MActionStateFactory` 装配顺序与回收规则；明确毫秒仅在 `MEntity.Update` 入口转换为秒，`MAnimator.Update` 每帧唯一调用；补充控制器租约和 OverrideController 清理；定义 ClipLease 的 Requested/Loading/Ready/Failed/Stale 状态及 Retain/Release 配对；补充 RO 动作契约和事件解绑；定义状态队列来源优先级、同优先级排序、服务器终态覆盖预测 token；补齐 Idle/Run/FastRun/Jump/Slider/Attack/Hit 映射及相机副作用迁移。
- 本轮仍未修改业务代码，未执行编译或 PlayMode；等待独立 Agent 复审。

## [VERIFY][技能][TR-135] 2026-09-14 MAnimator 设计文档第二轮返工

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1192—1197 行；历史时点记录，不代表当前仍在执行。

- 复审反馈：仍缺少世界层实际 LateUpdate 调度、动作状态 Attached/事件订阅时机、BlendTree 组共享时间、远程 RoleEntity 状态字段缺失时的降级策略，以及更明确的仲裁伪流程。
- 文档修订：增加 `Global.LateUpdate → SceneEntityWorld.LateUpdate → MEntity.LateUpdate` 链路，规定 `EntityViewComponent.UpdateAnimator` 为 MAnimator 唯一更新点；补充 `MActionStateFactory` 的 Attached/注册/解绑/回收顺序；补充 BlendTree 全组 normalizedTime 和失败回退；补充状态队列仲裁伪代码与服务器序列覆盖规则；补充 `RemoteActionSyncState` 适配层和无序列字段降级；补齐 RO 动画元数据预加载与 LRU 参数。
- 本轮仍未修改业务代码，未执行编译或 PlayMode；等待独立 Agent 再次复审。

## [VERIFY][技能][TR-136] 2026-09-14 MAnimator 设计文档第三轮返工

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1198—1203 行；历史时点记录，不代表当前仍在执行。

- 复审反馈：需要明确 LateUpdate 对 `ROObject`/世界接口的编译接入，以及动作状态不能复用普通组件类型键。
- 文档修订：增加 `ROObject.LateUpdate(int)`、`MObject/MEntity` 默认与转发责任、`SceneEntityWorld.LateUpdate` 调用约定；定义 `MActionStateContainer` 独立保存动作状态，避免与 `EntityComponentContainer` 和四个专用组件键冲突。
- 本轮仍未修改业务代码，未执行编译或 PlayMode；等待独立 Agent 再次复审。

## [VERIFY][技能][TR-137] 2026-09-14 MAnimator 设计文档第四轮返工

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1204—1209 行；历史时点记录，不代表当前仍在执行。

- 复审反馈：LateUpdate 销毁时机和精确接口仍需统一；动作状态容器不能以 `StateDefine` 单键；动作组件需与当前 `EntityComponentBase` 构造、事件和回收契约一致；状态注册必须早于实体 Build。
- 文档修订：明确 Update 末移除 PendingDestroy、LateUpdate 仅处理 Active/Sleeping；给出 `ROObject.LateUpdate(int)`、`MEntity.Update(int)`、`MComponentMgr.UpdateSpecialComponents(entity,int,float)`、`MStateMachine.UpdateSeconds(float)`、`MAnimator.Update(float)` 精确签名和无 Update 行为；定义 `ActionStateKey → MActionComponent`、`StateDefine → List` 容器 API；明确 Global.Awake 注册与实体 Initialize Attach/Attached/SetDefault 顺序；修正 MEventArgs、AnimatorFacade、ResetForPool 及旧字段移除规则；补充 RoleState.Attck 拼写和旧表现入口迁移。
- 本轮仍未修改业务代码，未执行编译或 PlayMode；等待独立 Agent 最终复审。

## [VERIFY][技能][TR-138] 2026-09-14 MAnimator 设计文档最终复审

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1210—1216 行；历史时点记录，不代表当前仍在执行。

- 审核对象：`项目分析/ANIMATOR_STATE_MACHINE_DESIGN.md` 最新版本。
- 审核结论：通过，无阻塞项；独立审核 Agent 逐项确认六项返工要求已满足。
- 验证范围：仅文档和协作记录，未修改客户端、服务端、协议、配置表、场景或预制体业务代码，因此未执行编译和 PlayMode。
- 后续交接：进入实现阶段时，按文档第 11 节先实现动画资源适配和 `MAnimator`，再接入视图、状态机及 Player/Role/Monster 状态；实现任务必须委派客户端子 Agent 并执行静态检查、构建和 PlayMode 验收。

## [VERIFY][背包商城][TR-139] 2026-09-11 移除商城购买回包重复限购字段验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1217—1223 行；历史时点记录，不代表当前仍在执行。

- 修改：`RequestBuyShopItem` 成功回包不再读取或填充剩余限购次数；`BuyShopItemRes` 删除剩余次数和虚拟背包快照字段，仅保留 `ErrorInfo`。
- 静态检查：`RequestBuyShopItem` 无 `remaining_purchase_count`、`RemainingPurchaseCount` 或成功回包快照写入；`RoleShopRecord.UseBuyTimes` 仍由 `ShopStatic.BuyItems` 调用。
- 构建：`dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/remove-purchase-echo-build` 成功，0 个错误，8 个既有警告。
- 未执行：Center/Game/Gate/Unity 真实商城购买联调；客户端和 Gate 使用的新协议程序集需要由主 Agent 按现有发布流程同步。

## [VERIFY][背包商城][TR-140] 2026-09-11 商城购买客户端前置校验验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1224—1234 行；历史时点记录，不代表当前仍在执行。

- 构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/shop-purchase-client-build` 成功，0 个错误，17 个既有警告。
- 静态检查：商城购买目标文件无新增 `foreach` 和三目运算符；购买回包只访问 `BuyShopItemRes.Error`，未访问购买物品、虚拟背包或剩余次数字段。
- 验证内容：购买前读取 `RoleCountComponent.GetLeftCount` 和 `RoItemComponent.GetVirtualItemCount`；校验失败直接调用 `TipsMgr.ShowSystemTips` 并在设置在途状态前返回。
- 未执行：Unity Play Mode、真实服务器商城购买回包、角色快照刷新和真实货币扣除联调。
- 补充构建：在加入成功回包后的本地次数和货币更新后，`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/shop-purchase-client-build2` 仍成功，0 个错误，17 个既有警告。
- 需求修正：按用户要求删除成功回包后的本地货币/次数修改及其临时字段，购买结果只通知成功或失败；发包前校验逻辑保留。
- 最终构建：移除 `ApplyLocalPurchaseResult` 后，`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/shop-purchase-client-build3` 成功，0 个错误，17 个既有警告。
- 临时产物：`D:/unitypro/LYFMMORGP/_temp/shop-purchase-client-build/`、`D:/unitypro/LYFMMORGP/_temp/shop-purchase-client-build2/`，用途为本次客户端构建，待产品经理确认后清理。

## [VERIFY][网络][背包商城][TR-141] 2026-09-11 商城购买结果同步验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1235—1243 行；历史时点记录，不代表当前仍在执行。

- 静态检查：购买成功回包只在 `ShopMgr` 读取服务器返回的虚拟物品和限购剩余字段；`MainCtrl` 负责注册 `CMD_SC_UpdateItemInfoCode` 并交给背包组件，UI 不直接解析协议。
- 增量处理：`RoItemComponent.ApplyServerItemDelta` 支持服务器物品新增、删除、数量变化和跨容器移动，并复用已有背包细粒度事件；虚拟物品快照只清理并重建虚拟物品容器。
- 规则检查：修改文件无新增 `foreach` 和三目运算符，新增接口和关键状态均有中文注释。
- 构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/shop-sync-client-build` 成功，0 个错误，17 个既有警告。
- 未执行：Unity Play Mode、真实服务器购买回包字段填充、UpdateItemRet 运行态推送和四端联调。
- 临时产物：`D:/unitypro/LYFMMORGP/_temp/shop-sync-client-build/`，用途为本次客户端构建，待产品经理确认后清理。

## [VERIFY][综合][TR-142] 2026-09-11 RoleKanpsackInfo 结构复核

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1244—1250 行；历史时点记录，不代表当前仍在执行。

- 静态检查：协议源文件中 `RoleKanpsackInfoRet` 只包含结果码和嵌套信息；`RoleKanpsackInfo` 包含各背包、容量、装备栏、虚拟物品背包和保存版本；未发现旧的直接字段或 `MainRoleInfo.Money` 引用。
- 构建：Network、GameServer、CenterServer、GateServer Debug 隔离构建均成功，0 个错误；保留项目已有警告。
- 规则检查：本次复核涉及的服务端业务文件无新增 `foreach` 或三目运算符；临时构建产物位于 `D:/unitypro/LYFMMORGP/_temp/proto-final-check/`，待产品经理确认后清理。
- 未执行：未连接真实数据库，未进行 Center/Game/Gate/Unity 四端登录联调。

## [VERIFY][综合][TR-143] 2026-09-10 YooAsset 图集子 Sprite 地址诊断

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1251—1258 行；历史时点记录，不代表当前仍在执行。

- 检查最新资源清单：`Bundles/StandaloneWindows64/DefaultPackage/2026-09-10-680/PackageManifest_DefaultPackage_2026-09-10-680.json` 包含 `Assets/GM_Game/UISprites/Icon/Item/Icon_ItemConsumables01.png`，不包含 `UI_icon_item_Zeng.png` 子资源定位项。
- 检查纹理导入信息：`Icon_ItemConsumables01.png.meta` 的 `spriteMode=2`，并包含名为 `UI_icon_item_Zeng.png` 的切片，确认它是子 Sprite 而非独立文件。
- 检查收集配置：`AssetBundleCollectorSetting.asset` 中 `EnableAddressable=0`；`UISprites` 收集器为 `AddressByFileName + PackDirectory + CollectAll`，地址规则在关闭 Addressable 时不会写入清单。
- 检查客户端调用：`ResourceMgr.LoadSpriteAsync` 将表内路径拼接到 `Assets/GM_Game/UISprites/` 后调用 `LoadAssetAsync<Sprite>`，没有图集子资源选择步骤。
- 结论：构建已收录图集主资源，失败发生在 location 映射阶段；重新构建相同配置不会自动产生子 Sprite 路径。当前未执行代码修复或 Unity 运行验证。

## [VERIFY][协作][TR-144] 2026-09-06 禁止三目运算符规则

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1259—1319 行；历史时点记录，不代表当前仍在执行。

- 验证内容：确认项目协作入口和决策记录已加入“禁止使用三目运算符”规则。
- 验证结果：`AGENTS.md` 与 `Docs/AI/DECISIONS.md` 均已写入；本次仅修改文档，未涉及业务代码，因此无需编译。

### RoItemComponent 延迟物品持久化代码审查（2026-09-06）

- 审查结论：不通过；发现 4 个 P1、2 个 P2，已反馈后端返工。
- P1 版本语义：`CenterServer/DB/CRoleWriteTask.cs:44-50` 使用 `role.ItemSaveVersion >= request.Version` 统一返回成功，不能区分重复请求与落后请求；GameServer 会把同版本成功 ACK 当作本地快照已提交并清脏，存在旧实例数据丢失风险。
- P1 生命周期：`GameServer/manger/RoleManager.cs:55-65` 在旧角色等待保存时直接退出 `AddOnlineRole`，新重连角色没有排队、回调或接管路径；全仓只发现 `RemoveOnlineRole` 定义，没有断线/退出调用方。
- P1 网络承载：`Network/Socket/com/ServerBase.cs:21,35,48-81` 仅使用 4KB 收包缓冲，包不完整时没有保存已收片段。用当前协议构造四类背包各 81 条随机物品快照，原始请求 18755 字节、GZip 后 13919 字节，超过 4096 字节，完整快照无法可靠到达 Center。
- P1 客户端协议：`LYF_MMO/GM_Game.csproj:614` 引用的 `Assets/GM_Game/Plugins/Net/Network.dll` SHA256 与本次 Network 构建产物不同；反射确认旧 DLL 的 `RoleItemInfo` 仍只有 `ItemId:System.Int32`，并缺少 `RoleKanpsackInfoRet.SaveVersion` 和 `SaveRoleDataReq`。
- P2 脏标记边界：`GameServer/Service/item/RoItemContainer.cs:22,50,98,122,155,170,234` 暴露可变字典和裸容器修改入口，`ContainerOnItemChange` 为空，`SortOutBag` 也只改位置不标脏；只能证明当前组件四个入口主动标脏，不能保证所有背包变化都标脏。
- P2 注释：`RoleNetSender.cs:10-13`、`ItemDataListDB.cs:9-12`、`CRoleWriteTask.cs:13-15` 的关键状态字段没有逐项中文注释。
- 通过项：`RoItemComponent` 当前 Add/Delete/Modify/Swap 主路径均设置脏版本；发送前不清脏；旧 ACK 不会清除更高的本地脏版本；单角色单在途和 5 秒失败/超时重发逻辑存在；登录恢复覆盖普通物品、装备、词条、位置和保存版本；服务器 proto、运行时容器和三张数据库实体均使用 `long/int64`；Center 任务在单消费者队列执行且事务提交后才返回成功；新增六个业务类型均一类型一文件，项目文件和协议注册完整。
- 独立构建：Network、GameServer、CenterServer 使用 `_temp/item-persistence-review-20260906/` 隔离输出，均为 0 错误；分别保留 4、7、4 条既有警告。相关差异 `git diff --check` 通过。
- 未执行：真实 MySQL 事务提交/回滚、Center 连接中断、四端联网和大包实发未验证；大包问题依据协议序列化长度和接收状态机静态行为判定。

### RoItemComponent 延迟物品持久化验证（2026-09-06）

#### 代码审查返工复测

- Network 构建：`dotnet build LYF_Server/Network/Network.csproj --no-restore --configuration Debug`，0 错误、0 警告。
- GameServer 构建：同配置，0 错误，保留 3 条既有 `ShopStatic`/`OnlineRole` 警告。
- CenterServer 构建：同配置，0 错误、0 警告。
- BehaviorTest：使用返工后的三端隔离产物编译运行，输出 `PASS: login-64bit, add/delete/modify/swap-dirty, ack-version, timer-60s`，退出码 0。
- 版本审查：`CRoleWriteTask` 对版本相等返回幂等成功，版本更旧返回错误和权威 `persisted_version`，不执行快照删除/插入和版本更新；GameServer 仅对 `CmdCode.Succeed` 清脏。
- 收包审查：`ServerBase` 的 4KB 接收边界测试路径改为完整帧累计解析，`NetUtils.ParseData` 支持偏移；未改变现有 uint16 帧协议。
- 会话审查：重连和会话冲突的旧角色未完成保存时，新角色进入待激活表；`Update` 在旧角色可释放后激活，`RemoveOnlineRole` 会清理尚未激活的会话。
- 容器审查：`GetItemMap` 和 `Items` 均为只读视图，未引入新的裸字典写入口。
- 未执行：未连接真实 MySQL，未进行四端联调和真实 Socket 长包压测；客户端文件未修改。

- 构建：Network、GameServer、CenterServer 分别使用隔离的 `OutputPath` 和 `BaseIntermediateOutputPath` 构建到 `D:/unitypro/LYFMMORGP/_temp/item-persistence-20260906/`，三者均为 0 错误。Network/Center 保留 4 条既有公共库警告，GameServer 保留上述 4 条及 `ShopStatic`、`OnlineRole` 共 7 条既有警告。
- 行为测试：`_temp/item-persistence-20260906/BehaviorTest.cs` 编译并运行成功，输出 `PASS: login-64bit, add/delete/modify/swap-dirty, ack-version, timer-60s`，退出码为 0。
- 恢复路径：在未配置 `UIDHelper` 逻辑服实例 ID 的情况下，`ItemFactory`、普通物品、装备和 `RoItemComponent.LoadKnapsack` 均能恢复已有 64 位 UID；恢复过程不生成临时 UID。
- 新建路径：配置逻辑服实例 ID 后连续构造两个新物品，UID 序列差为 1，确认 `ItemBase(int...)` 不再重复消耗 UID 序列。
- 启动配置：静态确认 `GameServerApp.Main` 在建立网络连接和初始化游戏模块前配置 `UIDHelper`；支持 `GAME_SERVER_INSTANCE_ID`，非法或超出 0-255 时启动失败，避免静默产生跨服重复 UID。
- 状态验证：登录装载不标脏；Add/Delete/Modify/Swap 均进入脏状态；旧 ACK 不会清除更高版本；匹配 ACK 清脏；跨包移动同时保存源与目标快照；删除后的快照不包含旧实例；60 秒前不触发、边界时触发。
- 静态检查：6 个新增业务类型各自仅出现于一个同名文件；ItemUID 在协议、GameServer 容器和 Center 三张表中均为 64 位；相关文件 `git diff --check` 清理后无新增空白错误。
- 未执行：未连接真实 MySQL，未验证 `CRoleWriteTask` 的实际事务提交、回滚、重复版本和连接中断；未启动 Center/Game/Gate/Unity 四端联调；客户端协议程序集未同步。

### #EquipInfo 列重排回读验证（2026-09-06）

- 测试目标：确认 `Luaban/MiniTemplate/Datas/#EquipInfo.xlsx` 12 个装备 sheet 完成列重排后数据无损、顺序正确。
- 验证方式：openpyxl 以 `data_only=True` 独立回读，逐 sheet 断言 `max_column==32`，表头顺序与预期列表完全一致（基本信息→NeedLevel等→CanStrengthen/MaxStrengthenLevel/MaxGemLevel/GemSlotCount/Des/PackType→GeneId0-5/GeneValue0-5）。
- 验证结果：12 个 sheet 全部通过；`PackType` 数据行均为 int `1`，`Des`（装备简介）内容完整；原 `Desc`（物品简介）列已删除；`Sheet1` 空表未改动。
- 数据完整性：重排采用"读取全部单元格值+样式→按新索引写回→删除多余列"方式，逐格迁移，未丢失任何数据或样式。
- 未执行项：Luban 配置导出与客户端读取验证待配置导出联调时确认。

### 背包延迟持久化设计记录（2026-09-06）

- 分析范围：现有 `BagTransition`、`RoItemComponent`、`GameGlobal.Update`、CenterServer 背包容量持久化链路及参考 roserver 的延迟处理思路。
- 本次未修改业务代码、协议、数据库或客户端，未执行构建。
- 设计验收点：内存变更与持久化解耦、同角色顺序提交、失败重试、角色下线强制刷盘、服务器停机优雅刷盘，以及重启后的快照校验。

### 背包系统总体设计方案记录（2026-09-06）

- 分析范围：`PROJECT_CONTEXT.md`、`DECISIONS.md`、`GameServer/Service/item`、角色组件、`RoleKnapsackTable` 及背包 protobuf。
- 本次未修改业务代码，未执行构建；记录方案用于后续产品、策划、后端、客户端和测试 Agent 交接。
- 关键风险：当前物品运行时 UID 为 `long`，数据库/协议仍存在 `int` 兼容边界；正式接入前需单独完成迁移和四端联调。

## [VERIFY][界面][TR-145] 2026-09-06 #EquipInfo 包类别默认值 1 回读验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1320—1327 行；历史时点记录，不代表当前仍在执行。

- 测试目标：确认 `Luaban/MiniTemplate/Datas/#EquipInfo.xlsx` 12 个装备 sheet 的包类别（PackType）数据行已由 0 改为 1，且字段结构无损。
- 验证方式：openpyxl 以 `data_only=True` 独立回读原文件，逐 sheet 断言 `AG4==1` 且为 int 类型、`AG1=='PackType'`、`max_column==33`。
- 验证结果：12 个 sheet 全部通过；字段名/类型/注释行与其余 32 列未改动。
- 异常记录：首次保存报 `PermissionError: [Errno 13]`（文件瞬时占用，检查无 EXCEL/WPS 进程、无只读属性、文件可写），重试后保存成功；未造成数据损坏。
- 未执行项：Luban 配置导出与客户端读取验证待配置导出联调时确认。

## [VERIFY][界面][TR-146] 2026-09-06 #EquipInfo 配置表包类别字段回读验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1328—1335 行；历史时点记录，不代表当前仍在执行。

- 测试目标：确认 `Luaban/MiniTemplate/Datas/#EquipInfo.xlsx` 12 个装备 sheet 均已追加 `PackType`（包类别）字段列且原数据无损。
- 验证方式：用 openpyxl 以 `data_only=True` 独立回读原文件（与生成路径不同），逐 sheet 断言 `max_column==33`、`AG1=='PackType'`、`AG2=='int'`、`AG3=='包类别'`、`AG4==0` 且为 int 类型。
- 验证结果：12 个 sheet 全部通过；抽查 MainWeapon 第 4 行 B..AF 原数据与修改前逐格一致；`Sheet1` 仍为空表未改动；新列字体与相邻列一致（宋体 11，无边框填充）。
- 文件安全：修改前原文件 MD5 `c260a90e75e27f7a56b2ff59646737b0`，确认用户无新增改动后才写入；配置表修改不涉及代码编译。
- 未执行项：未进行 Luban 配置导出与客户端读取验证，需策划/客户端在后续配置导出联调中确认新字段正常进入生成代码。

## [VERIFY][背包商城][TR-147] 2026-09-04 商城购买客户端链路接入

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1336—1343 行；历史时点记录，不代表当前仍在执行。

- 静态检查：客户端新增 `ShopPurchaseClient` 独立管理请求号和待处理回调；商品组件携带 NPC/商城上下文；确认弹窗通过回调提交请求；`MainCtrl` 注册 `CMD_BuyShopItemCode` 并按 `request_id` 分发结果；未修改测试目录或新增 Mock 入口。
- 协议程序集：已将 `_temp/shop-buy-protocol-20260904/bin/Network.dll` 同步到 `LYF_MMO/Assets/GM_Game/Plugins/Net/Network.dll`，旧插件备份在 `_temp/shop-client-20260904/Network.dll.backup`。
- 构建命令：`dotnet build GM_Game.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/shop-client-20260904/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/shop-client-20260904/obj/"`。
- 构建结果：成功，0 个错误；保留既有 Unity 引用冲突、Singleton 类型冲突、弃用 API 和异步调用等警告。首次构建因旧 Network.dll 缺少购买协议类型产生 3 个错误，已通过同步新版 DLL 修复。
- 未执行项：未进行真实四服务器与 Unity 联网联调、购买成功后的库存/货币刷新验证；需测试 Agent 继续验证。

## [VERIFY][背包商城][TR-148] 2026-09-04 RoleKnapsackComponent 背包事务融合

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1344—1352 行；历史时点记录，不代表当前仍在执行。

- 测试目标：确认 C++ `Bag` 的主要事务职责已映射到 C# 类型，且不引入当前项目不存在的依赖。
- 静态检查：`GameServer/Service/item` 共 15 个 C# 文件；每个顶级类型独立同名文件；`GameServer.csproj` 显式包含全部 15 个编译项；事务骨架仅引用当前 `OnlineRole`、`RoleKnapsackComponent`、`ItemBase`、`EquipBase` 和 `KnapsackType`。
- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/bag-transition-review-20260904/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/bag-transition-review-20260904/obj/"`。
- 构建结果：成功，0 个错误；保留 2 条已有警告：`OnlineRole._roleBaseAttrEntity` 未使用、`LoginCtrl.cs` 异步调用未等待。
- 未执行项：未进行真实物品扣除、发放、穿戴、货币、邮件和任务通知测试；等待权威数据模型和协议接入。
- 临时产物：`D:/unitypro/LYFMMORGP/_temp/bag-transition-review-20260904/`，待产品经理确认后清理。

## [VERIFY][网络][背包商城][TR-149] 2026-09-04 商城购买商品协议与链路需求拆解

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1353—1359 行；历史时点记录，不代表当前仍在执行。

- 本记录为产品经理范围拆解，未修改客户端、服务端业务代码，未执行构建。
- 当前可验证边界：项目已有 `ShopInfo` 配置字段（商品配置 ID、物品 ID、价格、货币类型、绑定标记）和 `RoleNpcShopPurchaseTable`，但尚未发现可用于 GameServer 权威购买的货币余额、物品 UID/库存、背包空间、幂等请求和原子事务实现。
- 阻塞项：用户协议中的 `BuyShopItem`、`ErrorInfo` 未在 proto 定义，命令号和错误码也未确定；在这些内容明确前不能验证协议生成、Gate 转发、GameServer 购买处理或真实扣费发货。
- 下一步：策划与系统架构师先提交协议字段、命令号、购买数量/价格校验、货币与物品事务、限购和幂等规则；之后再由后端、客户端、测试和代码审查角色按流程实施。

## [VERIFY][背包商城][TR-150] 2026-09-04 背包事务类型骨架

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1360—1368 行；历史时点记录，不代表当前仍在执行。

- 测试目标：确认 C++ 背包事务概念已映射为当前 C# 项目的独立类型，并且不引入不存在的业务依赖。
- 静态检查：`GameServer/Service/item` 共 15 个 `.cs` 文件；每个顶级类型独立同名文件；`GameServer.csproj` 显式包含 15 个编译项；事务基类和派生类仅引用现有 `OnlineRole`、`RoleKnapsackComponent`、`KnapsackType`。
- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/bag-transition-review-20260904/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/bag-transition-review-20260904/obj/"`。
- 构建结果：成功，0 个错误；保留 2 条已有警告：`OnlineRole._roleBaseAttrEntity` 未使用、`LoginCtrl.cs` 异步调用未等待。
- 未执行项：未进行真实物品扣除、发放、购买、穿戴、邮件和任务通知测试；这些功能等待权威数据模型和协议接入。
- 临时产物：`D:/unitypro/LYFMMORGP/_temp/bag-transition-review-20260904/`，待产品经理确认后清理。

## [VERIFY][背包商城][TR-151] 2026-09-04 Luban 背包配置表初始化补全

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1369—1376 行；历史时点记录，不代表当前仍在执行。

- 测试目标：确认新增 `TbKnapsackInfo` 已被 `LubanMgr` 正确缓存、初始化、打印并可查询。
- 静态检查：确认 `KnapsackInfo` 字典、`tables.TbKnapsackInfo.DataMap`、启动打印和 `GetKnapsackInfoByType` 均已写入。
- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/luban-init-20260904/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/luban-init-20260904/obj/"`。
- 构建结果：成功，0 个错误；保留 2 条既有警告：`OnlineRole._roleBaseAttrEntity` 未使用、`LoginCtrl.cs` 异步调用未等待。
- 未执行项：未启动多服务和客户端进行真实联网联调；本次仅更新 Luban 表初始化。

## [VERIFY][综合][TR-152] 2026-09-04 RoleManager 会话冲突与组件异常隔离返工

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1377—1384 行；历史时点记录，不代表当前仍在执行。

- 测试目标：验证同一 GateSessionId + UnitySessionId 绑定新角色时，旧角色完整释放并清理双索引；验证单组件生命周期异常不会阻断后续组件和角色索引清理。
- 静态检查：`RoleManager.AddOnlineRole` 冲突分支设置旧角色离线、删除旧角色会话键、调用 `Dispose` 并移除旧角色主索引；`RoleComponentContainer` 各生命周期逐组件调用 `InvokeSafely`，异常仅记录日志后继续。
- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/role-component-isolation-20260904/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/role-component-isolation-20260904/obj/"`。
- 构建结果：成功，0 个错误；保留既有 2 条警告：`LoginCtrl.cs` 验证码异步调用未等待、`OnlineRole._roleBaseAttrEntity` 未使用。
- 未执行项：未启动多进程服务器和 Unity 客户端进行真实联网联调。

## [VERIFY][综合][TR-153] 2026-09-04 RoleManager 同角色替换状态补充

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1385—1391 行；历史时点记录，不代表当前仍在执行。

- 测试目标：确认按 `roleID` 替换旧在线角色时，旧实例在索引清理和组件释放前被标记为离线。
- 静态检查：`RoleManager.AddOnlineRole` 已在 `roleID` 替换分支设置 `oldRole.State = OnlineRoleState.Offline`，随后删除旧会话索引并调用 `Dispose`。
- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/role-replace-state-final-20260904/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/role-replace-state-final-20260904/obj/"`。
- 构建结果：成功，0 个错误；保留既有 2 条警告。

## [VERIFY][场景实体][TR-154] 2026-09-04 OnlineRole 组件生命周期融合

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1392—1401 行；历史时点记录，不代表当前仍在执行。

- 测试目标：确认角色组件容器按固定顺序执行生命周期，组件更新仅在逻辑线程进行，且背包容量不产生第二份状态真相。
- 静态检查：`OnlineRole` 提供 `InitSerializes`、`AfterLoad`、`AfterAddedToRole`、`Update`、`Dispose` 和泛型 `SetComponent/GetComponent/TryGetComponent`；`RoleManager` 在新增、替换、移除角色时调用对应生命周期；`RoleKnapsackComponent` 直接代理 `OpenedKnapsackGridCount`。
- 结构检查：`LYF_Server/GameServer` 一类一文件扫描通过；未新增技能空组件。
- 空白检查：`git diff --check -- LYF_Server/GameServer Docs/AI` 无新增空白错误（仅提示既有换行符转换）。
- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/role-components-20260904/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/role-components-20260904/obj/"`。
- 构建结果：成功，0 个错误；保留既有 2 条警告：`OnlineRole._roleBaseAttrEntity` 未使用、`LoginCtrl.cs` 验证码异步调用未等待。
- 未执行项：未启动完整多进程服务器和 Unity 客户端进行真实联网联调。

## [VERIFY][网络][TR-155] 2026-09-04 GameServer 命令层精简与客户端消息管理器

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1402—1410 行；历史时点记录，不代表当前仍在执行。

- 测试目标：确认客户端命令由独立 `ClientMessageManager` 统一接收和分发，`RoleManager` 不再承担网络协议路由，且线程边界、协议号和会话语义保持不变。
- 静态检查：`ClientMessageManager` 实现 `IContainer`，Gate/Center 回调仅投递 `GameCommand`；按来源和协议调用 `Game_LoginCtrl`、`RoleManager` 的公开业务入口；GameServerApp 的网络注册统一指向该管理器；`RoleManager` 仅保留角色索引、角色业务、中心结果处理和固定帧更新。
- 层级评估：保留 `GameModuleManager` 与 `GameModuleBase`，前者负责模块注册/协议路由，后者负责每模块并发队列和 Update 消费，职责不同，合并会损失线程边界与扩展点。
- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/client-message-manager-20260904/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/client-message-manager-20260904/obj/"`。
- 构建结果：成功，0 个错误；保留既有 2 条警告：`OnlineRole._roleBaseAttrEntity` 未使用、`LoginCtrl.cs` 验证码异步调用未等待。
- 未执行项：未启动完整多进程服务器和 Unity 客户端进行真实联网联调。

## [VERIFY][场景实体][TR-156] 2026-09-04 业务管理器直接固定帧更新

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1411—1420 行；历史时点记录，不代表当前仍在执行。

- 测试目标：确认业务管理器移除 `GameModuleBase` 继承后，由 `GameGlobal` 直接按顺序驱动 `Update`，客户端消息队列仍正常消费。
- 静态检查：`RoleManager`、Redis、小游戏、活动、场景等管理器均为普通单例类并提供 `Update(int)`；`GameGlobal.UpdateLogicFrame` 直接调用全部管理器；项目中已无 `GameModuleManager`、`GameManagerModule`、`IGameModule` 引用；`ClientMessageManager` 独立维护命令队列并实现 `IContainer`。
- 一类一文件检查：GameServer（排除 `obj/bin`）每个源文件至多一个顶级类型，结果通过。
- 构建命令：`dotnet build LYF_Server\\LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:\\unitypro\\LYFMMORGP\\_temp\\direct-manager-update-final\\bin\\" -p:BaseIntermediateOutputPath="D:\\unitypro\\LYFMMORGP\\_temp\\direct-manager-update-final\\obj\\"`。
- 构建结果：成功，0 个错误；保留既有 2 条警告（`LoginCtrl.cs` 异步调用未等待、`OnlineRole._roleBaseAttrEntity` 未使用）。
- 代码审查结论：通过；确认网络线程入队、客户端消息消费、业务管理器固定帧更新和角色职责边界符合本次要求。
- 未执行项：未启动完整多进程服务器和 Unity 客户端进行真实联网联调。

## [VERIFY][综合][TR-157] 2026-09-04 GameServer 管理器继承层精简

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1421—1430 行；历史时点记录，不代表当前仍在执行。

- 测试目标：确认移除无实际价值的 `GameModuleManager`、`GameManagerModule`、`IGameModule` 继承层后，客户端命令队列和固定帧更新仍正常。
- 静态检查：确认所有业务管理器均为普通单例并公开 `Update(int)`；`ClientMessageManager` 独立维护 `ConcurrentQueue<GameCommand>`，`GameCommandDispatcher` 直接投递该队列；`GameGlobal` 直接按既定顺序调用各管理器；旧层级和 `GameServerModuleBootstrap` 无项目引用。
- 空白检查：`git diff --check -- LYF_Server/GameServer Docs/AI` 无新增空白错误（仅提示既有换行符转换）。
- 首次构建：因 `ClientMessageManager` 缺少 `System.Threading` 引用出现 3 个错误，已补充 using 并重新构建。
- 最终构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/manager-layer-simplify-final-20260904/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/manager-layer-simplify-final-20260904/obj/"`。
- 最终构建结果：成功，0 个错误；保留既有 2 条警告：`LoginCtrl.cs` 异步调用未等待、`OnlineRole._roleBaseAttrEntity` 未使用。
- 未执行项：未启动完整多进程服务器和 Unity 客户端进行真实联网联调。

## [VERIFY][网络][TR-158] 2026-09-04 客户端消息管理器最终复验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1431—1440 行；历史时点记录，不代表当前仍在执行。

- 测试目标：确认新增消息管理器已接入实际网络入口，角色管理器不再重复承担消息分发，且精简后未破坏线程边界。
- 静态检查：`GameServerApp.cs` 的 Gate/Center 协议注册全部指向 `ClientMessageManager.Instance`；`ClientMessageManager` 实现 `IContainer` 并继承模块队列基类；`RoleManager` 不再包含 `OnServerCommand`、`OnClientCommand` 或 `ProcessNetMessage`；角色业务入口由消息管理器调用。
- 结构检查：`LYF_Server/GameServer`（排除 `obj/bin`）每个源文件至多一个顶级类型；命令层保留 `GameCommandDispatcher`、`GameModuleManager`、`GameModuleBase` 的必要职责，移除无用的角色网络入口。
- 构建命令：`dotnet build LYF_Server\\LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:\\unitypro\\LYFMMORGP\\_temp\\client-message-manager-final\\bin\\" -p:BaseIntermediateOutputPath="D:\\unitypro\\LYFMMORGP\\_temp\\client-message-manager-final\\obj\\"`。
- 构建结果：成功，0 个错误；保留既有 2 条警告（`LoginCtrl.cs` 异步调用未等待、`OnlineRole._roleBaseAttrEntity` 未使用）。
- 代码审查结论：通过；客户端消息入口、角色职责边界和必要的队列层级均符合当前架构。
- 未执行项：未启动完整多进程服务器和 Unity 客户端进行真实联网联调。

## [VERIFY][综合][TR-159] 2026-09-04 附近角色补发会话路由修正

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1441—1448 行；历史时点记录，不代表当前仍在执行。

- 测试目标：验证进入世界后附近角色补发只发送到当前玩家的 Gate/Unity 会话，不复用进入世界请求包。
- 静态检查：`RoleManager.SendNearbyRoles` 使用 `GetOnlineRoleBySession` 校验当前角色双索引，通过 `SessionMgr.GetSession` 获取目标会话，并调用 `Session.SendData(unitySessionId, ...)` 为每个附近角色独立构造包；`BroadcastRoleEnterWorld` 保持不变。
- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/nearby-session-routing-20260904/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/nearby-session-routing-20260904/obj/"`。
- 构建结果：成功，0 个错误；保留既有 2 条警告：`OnlineRole._roleBaseAttrEntity` 未使用、`LoginCtrl.cs` 验证码异步调用未等待。
- 未执行项：未启动多 Gate、多客户端进行真实联网联调。

## [VERIFY][综合][TR-160] 2026-09-04 附近角色补发会话路由复验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1449—1457 行；历史时点记录，不代表当前仍在执行。

- 测试目标：验证 `SendNearbyRoles` 修复后按当前玩家的 Gate/Unity 会话发送，且不污染进入世界请求包。
- 静态检查：方法签名仅接收当前 `OnlineRole`；通过 `GetOnlineRoleBySession` 校验双索引，使用 `SessionMgr.GetSession(sessionRole.GateSessionId)` 获取目标会话，并调用 `targetSession.SendData(sessionRole.UnitySessionId, ...)` 为每个附近角色独立构造包。未发现复用 `serverBase` 或 `package` 发送附近角色的路径。
- 构建命令：`dotnet build LYF_Server\\LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:\\unitypro\\LYFMMORGP\\_temp\\nearby-session-final\\bin\\" -p:BaseIntermediateOutputPath="D:\\unitypro\\LYFMMORGP\\_temp\\nearby-session-final\\obj\\"`。
- 构建结果：成功，0 个错误；保留既有 2 条警告（`LoginCtrl.cs` 异步调用未等待、`OnlineRole._roleBaseAttrEntity` 未使用）。
- 代码审查复审：通过；确认当前玩家补发路由、目标玩家广播路由和包独立性均符合会话模型。
- 未执行项：未启动多 Gate、多客户端进行真实联网联调。

## [VERIFY][协作][TR-161] 2026-09-04 一类一文件约束配置

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1458—1464 行；历史时点记录，不代表当前仍在执行。

- 测试目标：确认项目配置、工作流和持久决策记录纳入“一类一文件”及“主 Agent 只调度不改业务代码”约束。
- 检查范围：`AGENTS.md`、`AIconfig/config.toml`、`AIconfig/workflow.md`、`Docs/AI/DECISIONS.md`、`Docs/AI/CURRENT_TASK.md`。
- 结果：已完成配置和记录更新；业务源代码尚未拆分，等待后端开发 Agent 执行。
- 未执行项：GameServer 静态类型扫描、编译和代码审查将在拆分完成后执行。

## [VERIFY][综合][TR-162] 2026-09-04 GameGlobal 中文注释补充

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1465—1472 行；历史时点记录，不代表当前仍在执行。

- 测试目标：确认 `GameGlobal` 注释补充不改变固定帧逻辑且项目可编译。
- 修改范围：`LYF_Server/GameServer/GameGlobal.cs`，仅新增中文注释。
- 构建命令：`dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/gameglobal-comments-20260904/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/gameglobal-comments-20260904/obj/"`。
- 构建结果：成功，0 个错误；保留已有 `OnlineRole._roleBaseAttrEntity` 未使用警告。
- 未执行项：未启动完整多服务和客户端进行联网联调；本次未修改运行时代码逻辑。

## [VERIFY][综合][TR-163] 2026-09-04 GameServer 管理器队列与预注册

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1473—1481 行；历史时点记录，不代表当前仍在执行。

- 测试目标：确认 GameGlobal 角色接口已移除，总队列只路由到管理器队列，各管理器在自身 Update 中消费，并完成参考管理器骨架预注册。
- 静态检查：扫描 `LYF_Server/GameServer`，未发现 `GameGlobal.Instance.AddOlineRole`、`RemoveOlineRole`、`GetOlineRole`、`GetAllOlineRole` 等旧转发调用；确认 `GameModuleBase` 实现独立命令队列，`GameModuleManager` 只调用 `EnqueueNetMessage`；确认骨架均以 `XXXManager.Instance` 注册。
- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/module-queues-bootstrap-20260904/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/module-queues-bootstrap-20260904/obj/"`。
- 首次静态检查：发现 `GameServerApp.cs` 一处新增空白行，已修复。
- 构建结果：成功，0 个错误；保留 2 条既有警告：`OnlineRole._roleBaseAttrEntity` 未使用、`LoginCtrl.cs` 异步调用未等待。
- 未执行项：未启动完整服务器和 Unity 客户端进行真实联网联调；空模块仅提供 Update 骨架，尚未承载具体玩法。

## [VERIFY][综合][TR-164] 2026-09-04 管理器单例结构修正

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1482—1487 行；历史时点记录，不代表当前仍在执行。

- 测试目标：确认参考项目中的业务模块按管理器单例方式预创建，而不是启动时反复 `new` 临时模块。
- 静态检查：确认 `GameServerModules.cs` 已移除 `EmptyGameModule` 和 `new XXXModule()`，所有预置业务对象均提供 `XXXManager.Instance`，并继承带独立命令队列的 `GameManagerModule<T>`。
- 验证结果：通过；已完成最终方案构建复验。

## [VERIFY][构建][TR-165] 2026-09-04 GameServer 模块队列最终构建复验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1488—1494 行；历史时点记录，不代表当前仍在执行。

- 复验目标：使用规范 `_temp` 输出路径重新确认模块队列改造可编译。
- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/module-queues-bootstrap-final-20260904/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/module-queues-bootstrap-final-20260904/obj/"`。
- 构建结果：成功，0 个错误；保留 2 条既有警告：`LoginCtrl.cs` 异步调用未等待、`OnlineRole._roleBaseAttrEntity` 未使用。
- 输出位置：`D:/unitypro/LYFMMORGP/_temp/module-queues-bootstrap-final-20260904/`。

## [VERIFY][协作][TR-166] 2026-09-04 AIconfig 项目化配置

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1495—1502 行；历史时点记录，不代表当前仍在执行。

- 测试目标：确认 Agent 模板已按 LYFMMORGP 的实际 Unity、.NET、协议目录、依赖和角色分工完成修订。
- 检查范围：`AIconfig/config.toml`、`workflow.md`、`agent/*.md`、`AGENTS.md`、`Docs/AI/PROJECT_CONTEXT.md`。
- 静态检查：确认模板不再引用 `Shared/Protocol`、Unity 2022.3、.NET 8、MemoryPack、DOTS-ECS 等与当前项目不符的强制路径或技术栈；确认新增产品经理和测试工程师定义。
- 验证结果：通过；配置文件和文档已写入项目，未修改业务代码，未执行 Unity 或 .NET 构建。
- 未执行项：AIconfig 不会自动注入 Codex 平台运行时；实际执行仍需遵守 `AGENTS.md` 和 `Docs/AI`。

## [VERIFY][协作][TR-169] 2026-09-04 项目结构与持久记录规范

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1518—1525 行；历史时点记录，不代表当前仍在执行。

- 测试目标：确认项目结构、模块职责、GameServer 消息流程及后续记录要求已经写入持久协作文档。
- 检查范围：`PROJECT_CONTEXT.md`、`CURRENT_TASK.md`、`DECISIONS.md`、`TEST_REPORT.md`。
- 验证方式：逐份读取并检查新增章节，确认包含客户端分层、服务端职责、网络队列流程、测试隔离、临时目录和任务交接规则。
- 验证结果：通过；本次仅修改协作文档，不涉及源码编译，未产生新的构建产物。
- 未执行项：未执行 Unity 或 .NET 编译；本次变更不影响运行时代码。

## [VERIFY][背包商城][TR-170] 2026-09-04 背包开格子服务端处理

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1526—1534 行；历史时点记录，不代表当前仍在执行。

- 协议生成：使用 `LYF_Server/ProtoBuf/bin/protoc.exe` 重生成协议 C# 代码，临时生成目录为 `_temp/protobuf-open-knapsack-grid-20260904`。
- 队列校验：现已升级为 `GameCommandDispatcher`；`GameRoleCtrl.OnServerCommand` 对开格子命令只投递不可变 `GameCommand`，`GameServerApp` 主循环通过 `GameGlobal.Update` 消费队列，业务状态不在网络接收线程修改。
- 容量校验：`CentRoleModel.RoleKanpaskInfo` 的 `KanpsackTypeCountLst.Count` 已改为 `RoleKnapsackTable.roleKnapsackcount`，不再读取物品数量；新角色默认和旧角色兼容下限均为 81 格。
- 持久化校验：开格子成功后 GameServer 以内部命令 11121 通知 CenterServer 写入 `roleKnapsackcount`；登录回包携带 `opened_knapsack_grid_count` 初始化在线角色数据。
- 构建：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/build-open-knapsack-grid-20260904/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/build-open-knapsack-grid-20260904/obj/"`，0 错误。最终仍有 2 条既有警告：`OnlineRole._roleBaseAttrEntity` 未使用、`LoginCtrl.cs` 验证码异步调用未等待。
- 未进行运行联调：MySQL、Redis、Center/Login/Gate/GameServer 与客户端未同时启动；客户端尚未接入 11120 协议，需后续联调验证。

## [VERIFY][背包商城][TR-171] 2026-09-03 背包 Mock 测试按钮清理

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1535—1542 行；历史时点记录，不代表当前仍在执行。

- 根因：`UIRoot.prefab` 上的 `KnapsackWindowMockServer` 在启动时动态创建 `MockAddItemButton`。
- 处理：删除动态按钮代码，并从正式预制体移除该测试组件。
- 静态校验：`KnapsackWindowMockServer.cs` 为 0 错误、0 警告。
- 引用扫描：未发现 `MockAddItemButton`、`CreateRuntimeAddButton` 或测试组件 GUID 的正式引用。
- Unity 控制台：0 条错误/警告。

## [VERIFY][NPC][背包商城][界面][TR-172] 2026-09-03 NPC 商城窗口类型重命名

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1543—1549 行；历史时点记录，不代表当前仍在执行。

- 重命名：`ShopWindow.cs` -> `NpcShopWindow.cs`，`ShopWindow` -> `NpcShopWindow`。
- 引用检查：`MainView` 的序列化字段已更新；窗口枚举 `WindowType.ShopWindow` 保持不变。
- 资源安全：`.meta` 随脚本移动，GUID 为 `c31991ec038b4f8408fd9066c521a62a`，预制体脚本引用应保持不变。
- 构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug`，0 错误、16 条既有警告。

## [VERIFY][技能][TR-173] 2026-09-03 Animator 编辑器异常诊断

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1550—1556 行；历史时点记录，不代表当前仍在执行。

- 异常位置：`UnityEditor.Graphs.AnimationStateMachine.Graph.GenerateConnectionKey`，发生在 Animator 窗口重绘链路。
- 只读扫描：11 个 `Animator Controller`，未发现空状态、空子状态机或目标为空且非 Exit 的 Transition。
- Override Controller 扫描：0 个。
- 结论：未发现可安全修改的损坏动画资源；建议先重开 Animator 窗口，确认具体 Controller 后再定点处理。

## [VERIFY][综合][TR-174] 2026-09-03 目录清理

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1557—1563 行；历史时点记录，不代表当前仍在执行。

- 排除目录：`LYF_MMO`、`LYF_Server`、`navicat`，未修改内容。
- 已处理：根目录 `.codex-tmp`（约 70 MB，构建/验证输出）移动至 `_temp/codex-tmp-20260903`。
- 清理方式：可恢复移动，没有执行永久删除。
- 保留项目：备份目录、`.codex`、场景备份、脚本、JSON/Excel 和 `.git` 均未处理。

## [VERIFY][综合][TR-175] 2026-09-03

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1564—1575 行；历史时点记录，不代表当前仍在执行。

### NPC 商城购买弹窗防重复

- 静态校验：
  - `TipsMgr.cs`：0 错误，0 警告
  - `BuyDialog.cs`：0 错误，0 警告
- Unity 编辑器：编译未进行中，编辑器可操作。
- Unity 控制台：本次最终检查返回 0 条错误/警告。
- 代码检查：确认 `git diff --check` 无空白错误。
- 验收结论：代码逻辑满足“加载中和已打开期间忽略其他商品点击”；尚未完成真实运行时人工双击操作录屏验收。

## [VERIFY][协作][TR-176] 测试 Agent 记录规则

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1576—1581 行；历史时点记录，不代表当前仍在执行。

- 记录执行时间、测试目标、命令或操作、结果和失败堆栈。
- 失败时标记“需返工”，并在 `CURRENT_TASK.md` 写出复现步骤。
- 修复后新增一条记录，不删除历史失败记录。

## [VERIFY][网络][背包商城][TR-177] 2026-09-04 背包已开格子同步与 GameServer 消息队列

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1582—1592 行；历史时点记录，不代表当前仍在执行。

- 客户端编译：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug`，0 错误；同步新 `Network.dll` 后为 20 条警告，其中新增 4 条为引用程序集版本冲突警告，无代码编译错误。
- 服务端编译：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug`，0 错误，2 条既有警告。
- 静态检查：测试脚本不存在 `CreateRuntimeAddButton` 或 `AddMockItems`；正式 `UIRoot.prefab` 未引用 `KnapsackWindowMockServer`。
- 行为检查：客户端最少显示 81 格，`KanpsackTypeCountLst.Count` 增加 9 时扩展一整行；物品根据 `BagIndex` 对应格子显示，空位不依赖物品列表长度。
- 服务端流程：`CMD_OpenKnapsackGridCode` 由 Gate 转发至 GameServer；GameServer 网络线程入 `ConcurrentQueue`，主循环每次最多消费 100 条并执行开格子，再向客户端回包与通知 CenterServer 持久化。
- 未执行项：未启动完整四服务器和 Unity 客户端进行联网人工操作验证。
- 协议接入：已将 `LYF_Server/Network/bin/Debug/Network.dll` 同步至 `LYF_MMO/Assets/GM_Game/Plugins/Net/Network.dll`，并由 `MainCtrl` 注册 `CMD_OpenKnapsackGridCode` 回包，使用服务器返回的容量刷新背包窗口。
- Unity MCP 校验：`KnapsackWindow.cs`、`KnapsackWindowMockServer.cs`、`MainCtrl.cs` 均为 0 错误、0 警告；Unity Console 当前 0 条错误/警告。

## [VERIFY][综合][TR-178] 2026-09-04 GameServer 命令分发重构

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1593—1602 行；历史时点记录，不代表当前仍在执行。

- 范围：仅修改 `LYF_Server/GameServer`，未改动 Network 底层、GateServer、CenterServer、客户端或参考 C++ 项目。
- 线程模型校验：`Game_LoginCtrl` 与 `GameRoleCtrl` 的 Gate/Center 网络回调均只投递 `GameCommand`；`GameCommandDispatcher` 在游戏主循环按命令号和来源分发，单条命令异常不会中断后续处理。
- Tick 校验：`GameServerApp` 每轮调用 `GameGlobal.Update` 并短暂让出 CPU；`GameGlobal.Update` 每轮先执行 `ProcessNetMessage`，累计时间达到 50ms 后才调用 `GameModuleManager.Update` 和 `RoleManager.Update`；单轮最多消费 3,000 条网络消息，掉帧时不进行无界补帧。
- 角色索引校验：`RoleManager` 维护角色 ID 与双会话组合索引，上线、下线、重复登录替换和状态切换均由逻辑线程处理；对外返回角色字典快照，不暴露内部集合。
- GameServer 构建：`dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/build-game-command-refactor-20260904/game/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/build-game-command-refactor-20260904/game-obj/"`，0 错误，1 条既有警告：`OnlineRole._roleBaseAttrEntity` 未使用。
- 解决方案构建：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/build-game-command-refactor-20260904/solution/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/build-game-command-refactor-20260904/solution-obj/"`，0 错误，2 条既有警告：`OnlineRole._roleBaseAttrEntity` 未使用、`LoginCtrl.cs` 验证码异步调用未等待。
- 未执行项：未启动 MySQL、Redis、Center/Login/Gate/GameServer 与 Unity 客户端进行真实多进程联网联调；该项需由测试角色在服务依赖可用时执行。

## [VERIFY][构建][TR-179] 2026-09-04 GameServer 参考项目复核与最终构建

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1603—1611 行；历史时点记录，不代表当前仍在执行。

- 参考范围：只读分析 `F:/UnityPro/ro/Ro_cs422nb/roserver/gameserver`。确认其采用“网络收包队列 + 单逻辑线程固定 50ms Tick”的模型；没有读取或改动 Login、Gate、Center 等其他参考服务器。
- 架构核验：Gate 与 Center 的 `OnServerCommand`/`OnClientCommand` 均只投递不可变 `GameCommand`；角色上线、会话索引、进入世界、背包开格子和中心服回包均由 Dispatcher 消费线程执行；背包容量校验和修改已下沉到 `Service/KnapsackService.cs`，控制器只做协议路由。
- 队列核验：容量上限 20,000，单轮处理上限 3,000；积压、未注册命令和单条处理异常均会记录日志，不会中断后续命令。
- 构建：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/gameserver-reference-architecture/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/gameserver-reference-architecture/obj/"`，0 错误，1 条既有警告：`OnlineRole._roleBaseAttrEntity` 未使用。
- 最终构建：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/gameserver-reference-architecture-final/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/gameserver-reference-architecture-final/obj/"`，0 错误，2 条既有警告：`OnlineRole._roleBaseAttrEntity` 未使用、`LoginCtrl.cs` 验证码异步调用未等待。
- 静态检查：`git diff --check` 在本次 GameServer 和协作文档范围内无新增错误；根目录其他 Unity 用户修改的尾随空格未处理。

## [VERIFY][综合][TR-180] 2026-09-04 Process_Update 两阶段模型修正

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1612—1618 行；历史时点记录，不代表当前仍在执行。

- 结构核验：`GameGlobal.Update` 现在每轮先调用 `ProcessNetMessage`，由 `GameCommandDispatcher` 出队，再按协议交给 `GameModuleManager.ProcessNetMessage`；累计时间未达到 50ms 时不执行模块 `Update`。
- 固定帧核验：累计达到 50ms 后，按注册顺序调用 `GameModuleManager.Update(delta)`，随后调用 `RoleManager.Update(delta)`；单个模块异常会被隔离，不影响同帧其他模块。
- 参考一致性：该流程对应参考 C++ 的 `GSConfig::ProcessNetMessage()` 与 `deltaTime >= FRAME_TIME` 两个阶段；消息处理上限为 3,000 条，掉帧不进行无界补帧。
- 最终构建：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/gameserver-process-update-final/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/gameserver-process-update-final/obj/"`，0 错误，2 条既有警告。

## [VERIFY][综合][TR-181] 2026-09-04 RoleManager 角色模块合并

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1619—1624 行；历史时点记录，不代表当前仍在执行。

- 静态检查：GameServer 中已无 `GameRoleCtrl`、`GameWorldBC`、`GameWorldBCd` 引用；`GameServer.csproj` 已移除两个旧控制器文件，`GameServerApp` 直接注册 `RoleManager.Instance`。
- 模块检查：`RoleManager` 统一承载角色协议入口、角色/会话双索引、进入世界和附近角色广播；`GameGlobal` 只通过 `GameModuleManager.Update` 调用角色固定帧，未重复调用 `RoleManager.Update`。
- 构建：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/rolemanager-merge/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/rolemanager-merge/obj/"`，0 错误，2 条既有警告。

## [VERIFY][综合][TR-182] 2026-09-04 GameServer 关键逻辑中文注释补全

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1625—1633 行；历史时点记录，不代表当前仍在执行。

- 测试目标：确认本轮 GameServer 架构代码的关键字段、协议来源、线程边界、角色索引和固定帧逻辑均有中文注释，且补注释未引入编译问题。
- 修改范围：`LYF_Server/GameServer/manger/GameCommand.cs`、`GameCommandDispatcher.cs`、`GameModuleManager.cs`、`RoleManager.cs`、`GameGlobal.cs`、`Ctrl/Game_LoginCtrl.cs`、`Entity/OnlineRole.cs`、`Service/KnapsackService.cs`、`GameServerApp.cs`。
- 静态检查：执行 `git diff --check -- LYF_Server/GameServer Docs/AI`，无新增空白错误；Git 仅提示既有换行符转换。
- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/comments-completion-20260904/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/comments-completion-20260904/obj/"`。
- 构建结果：成功，0 个错误；保留 2 条既有警告：`OnlineRole._roleBaseAttrEntity` 未使用、`LoginCtrl.cs` 异步调用未等待。
- 未执行项：未启动 MySQL、Redis、Center/Login/Gate/GameServer 与 Unity 客户端进行真实联网联调；该项不影响本次注释和编译验收。

## [VERIFY][综合][TR-183] 2026-09-04 GameServer 管理器一类一文件拆分

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1634—1642 行；历史时点记录，不代表当前仍在执行。

- 测试目标：确认 GameServer 全量顶级类型均按“一类一文件”拆分，且不改变模块注册、独立队列和固定帧行为。
- 静态检查：扫描 `LYF_Server/GameServer`，新增接口、基类、管理器、启动注册表、命令来源/会话类型和 `OnlineRoleState` 均为独立同名文件；已删除 `GameServerModules.cs`，项目中无其引用。
- 空白检查：`git diff --check -- LYF_Server/GameServer` 无新增空白错误（仅提示既有换行符转换）。
- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/split-module-types-final-20260904/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/split-module-types-final-20260904/obj/"`。
- 构建结果：成功，0 个错误；保留既有 2 条警告：`OnlineRole._roleBaseAttrEntity` 未使用、`LoginCtrl.cs` 验证码异步调用未等待。
- 未执行项：未启动完整四服务器和 Unity 客户端进行联网联调；代码审查由审查 Agent 执行。

## [VERIFY][综合][TR-184] 2026-09-04 一类一文件最终验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1643—1651 行；历史时点记录，不代表当前仍在执行。

- 测试目标：验证 GameServer 管理器拆分后每个顶级类型独立文件，并确认项目引用和完整解决方案构建正常。
- 静态扫描：扫描 `LYF_Server/GameServer` 下排除 `obj/bin` 的 32 个 `.cs` 文件；每个文件恰好一个顶级类、接口、枚举、委托或结构体，且类型名与文件名一致，结果通过。
- 项目文件检查：`GameServer.csproj` 包含全部 32 个 GameServer 源文件；已删除的 `GameServerModules.cs` 未被引用，结果通过。
- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/one-type-per-file-final/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/one-type-per-file-final/obj/"`。
- 构建结果：成功，0 个错误；保留 2 条既有警告：`LoginCtrl.cs` 异步调用未等待、`OnlineRole._roleBaseAttrEntity` 未使用。
- 未执行项：未启动完整多进程服务器和 Unity 客户端进行联网联调；本次结构拆分未改变协议和运行时行为。

## [VERIFY][场景实体][TR-185] 2026-09-04 OnlineRole 组件架构最终复审

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1652—1662 行；历史时点记录，不代表当前仍在执行。

- 复审范围：`RoleComponentContainer.cs`、`OnlineRole.cs`、`RoleManager.cs`。
- 会话冲突检查：同角色 ID 和同复合会话两类替换路径均设置旧角色为 `Offline`，清理角色/会话双索引并调用 `Dispose`；通过。
- 生命周期检查：五个生命周期钩子均逐组件异常隔离并继续处理；通过。
- 一类一文件检查：GameServer 31 个源文件扫描通过。
- 构建命令：`dotnet build LYF_Server\\LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:\\unitypro\\LYFMMORGP\\_temp\\role-component-review2-final\\bin\\" -p:BaseIntermediateOutputPath="D:\\unitypro\\LYFMMORGP\\_temp\\role-component-review2-final\\obj\\"`。
- 构建结果：0 个错误；保留既有 2 条警告。
- 审查结论：通过，建议主 Agent 完成最终验收。
- 未执行项：未进行真实多进程联网联调。

## [VERIFY][协作][场景实体][TR-186] 2026-09-04 OnlineRole 组件架构最终复审（主 Agent 指定复核）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1663—1671 行；历史时点记录，不代表当前仍在执行。

- 检查项：RoleManager 两类角色替换、组件生命周期异常隔离、中文注释、线程边界、一类一文件和构建。
- 结果：全部通过。两类替换均设置旧角色 `Offline`，清理双索引并调用 `Dispose`；五个组件生命周期均通过 `InvokeSafely` 隔离异常。
- 静态检查：GameServer 排除 `obj/bin` 后 31 个源文件，一类一文件通过；新增关键逻辑具备中文注释；网络回调只入 `GameCommandDispatcher` 队列。
- 构建：`dotnet build LYF_Server\\LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:\\unitypro\\LYFMMORGP\\_temp\\role-component-review2-final\\bin\\" -p:BaseIntermediateOutputPath="D:\\unitypro\\LYFMMORGP\\_temp\\role-component-review2-final\\obj\\"`，0 错误，2 条既有警告。
- 审查结论：通过，可交主 Agent 最终验收。
- 未执行项：未进行真实多进程服务与 Unity 客户端联网联调。

## [VERIFY][场景实体][TR-187] 2026-09-04 角色组件目录代码审查

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1672—1682 行；历史时点记录，不代表当前仍在执行。

- 检查范围：`Entity/Components` 目录、`OnlineRole.cs`、`GameServer.csproj`、一类一文件、中文注释和线程边界。
- 目录与引用：三个组件均位于 `LYF_Server/GameServer/Entity/Components`，旧根目录路径无残留；项目编译清单和 `OnlineRole` 引用正常，通过。
- 行为检查：组件注册、生命周期顺序、异常隔离、背包容量代理和角色更新行为未改变，通过。
- 规范检查：GameServer 排除 `obj/bin` 后 31 个源文件一类一文件通过；新增关键逻辑具备中文注释；网络回调只进入命令队列，通过。
- 构建命令：`dotnet build LYF_Server\\LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:\\unitypro\\LYFMMORGP\\_temp\\role-components-directory-review-final\\bin\\" -p:BaseIntermediateOutputPath="D:\\unitypro\\LYFMMORGP\\_temp\\role-components-directory-review-final\\obj\\"`。
- 构建结果：0 个错误，保留既有 2 条警告（`OnlineRole._roleBaseAttrEntity` 未使用、`LoginCtrl.cs` 异步调用未等待）。
- 审查结论：通过；目录整理未发现行为回归。
- 未执行项：未进行真实多进程服务与 Unity 客户端联网联调。

## [VERIFY][协作][场景实体][TR-188] 2026-09-04 OnlineRole 组件架构主 Agent 最终验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1683—1692 行；历史时点记录，不代表当前仍在执行。

- 一类一文件扫描：GameServer 排除 `obj/bin` 后 31 个源文件通过，未发现多顶级类型文件。
- 项目引用：`GameServer.csproj` 已包含 `IRoleComponent`、`RoleComponentContainer`、`RoleKnapsackComponent`。
- 空白检查：`git diff --check -- LYF_Server/GameServer Docs/AI` 无新增空白错误。
- 构建命令：`dotnet build LYF_Server\\LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:\\unitypro\\LYFMMORGP\\_temp\\role-component-final-integration-20260904\\bin\\" -p:BaseIntermediateOutputPath="D:\\unitypro\\LYFMMORGP\\_temp\\role-component-final-integration-20260904\\obj\\"`。
- 构建结果：完整解决方案生成成功，0 个错误；保留既有 2 条警告：`OnlineRole._roleBaseAttrEntity` 未使用、`LoginCtrl.cs` 异步调用未等待。
- 代码审查：审查 Agent 最终复审通过，并确认中文注释、生命周期异常隔离、角色替换清理和网络线程边界。
- 未执行项：未启动 Center/Login/Gate/GameServer 与 Unity 客户端进行真实联网联调。

## [VERIFY][场景实体][TR-189] 2026-09-04 角色组件目录整理验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1693—1701 行；历史时点记录，不代表当前仍在执行。

- 验证目标：确认三个角色组件迁移到 `Entity/Components` 后项目引用、源码扫描和完整服务端构建正常。
- 静态检查：`GameServer.csproj` 已引用 `Entity\\Components\\IRoleComponent.cs`、`RoleComponentContainer.cs`、`RoleKnapsackComponent.cs`；旧 `Entity\\*.cs` 组件路径无残留；`git diff --check -- LYF_Server/GameServer Docs/AI` 通过。
- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/role-components-directory-20260904/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/role-components-directory-20260904/obj/"`。
- 构建结果：成功，0 个错误；保留既有 2 条警告（`LoginCtrl.cs` 异步调用未等待、`OnlineRole._roleBaseAttrEntity` 未使用）。
- 结论：目录迁移不改变组件生命周期、角色注册获取和服务端行为。
- 未执行项：未进行真实多进程服务与 Unity 客户端联网联调。

## [VERIFY][背包商城][TR-190] 2026-09-04 按 KnapsackType 拆分背包容量验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1702—1708 行；历史时点记录，不代表当前仍在执行。

- 静态检查：协议请求/回包新增 `knapsack_type` 和四类容量列表字段，旧字段号未复用；GameServer 容量状态改为 `Dictionary<KnapsackType,int>`；CenterServer 查询、创建、同步均按类型过滤，穿戴栏不写入容量表；未修改测试脚本。
- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/knapsack-type-build-20260904/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/knapsack-type-build-20260904/obj/"`。
- 构建结果：完整服务端方案成功，0 个警告，0 个错误；Network 协议项目单独构建同样通过。
- 结论：四类容量链路已完成编译验证；未执行真实数据库数据迁移和多进程联网联调。

## [VERIFY][背包商城][构建][TR-191] 2026-09-04 按 KnapsackType 最终构建复验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1709—1716 行；历史时点记录，不代表当前仍在执行。

- 复验内容：补充旧协议单值容量兼容返回，并重新构建完整服务端方案。
- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/knapsack-type-build-final-20260904/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/knapsack-type-build-final-20260904/obj/"`。
- 构建结果：成功，0 个错误；保留 1 条既有警告（`OnlineRole._roleBaseAttrEntity` 未使用）。
- 静态检查：`git diff --check -- LYF_Server Docs/AI` 无新增空白错误；未发现服务端业务仍引用已删除的单容量字段。
- 未执行项：未进行真实数据库迁移、四服务器进程和 Unity 客户端联网联调。

## [VERIFY][网络][TR-192] 2026-09-04 客户端 Network.dll 协议同步验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1717—1727 行；历史时点记录，不代表当前仍在执行。

- 同步来源：`D:/unitypro/LYFMMORGP/_temp/client-knapsack-type-20260904/network-bin/Network.dll`。
- 同步目标：`LYF_MMO/Assets/GM_Game/Plugins/Net/Network.dll`。
- 备份位置：`D:/unitypro/LYFMMORGP/_temp/knapsack-protocol-dll-backup-20260904/Network.dll`；备份在覆盖前完成，且与新版 DLL 哈希不同。
- 哈希校验：同步后的插件 DLL 与来源 DLL SHA-256 均为 `D9F9E1C009E4CA5ECC7FBC12DA308DF2AACF` 前缀，确认字节一致。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/knapsack-protocol-client-build-20260904/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/knapsack-protocol-client-build-20260904/obj/" --nologo`。
- 构建结果：成功，0 个错误；保留 20 条项目既有警告，包括 Unity 引用的 `System.Numerics.Vectors` 版本冲突、`Singleton<T>` 重名、过时 API 和未使用字段。
- 结论：客户端已能引用新协议 DLL，之前 `OpenKnapsackGridRet.KnapsackType` 缺失导致的编译阻塞已解除。
- 未执行项：未在 Unity Editor 中执行真实网络开格子流程。

## [VERIFY][背包商城][TR-193] 2026-09-04 旧总背包容量迁移返工验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1728—1738 行；历史时点记录，不代表当前仍在执行。

- 验证范围：`LoginModle.StartGame` 与 `CentRoleModel.RoleKanpaskInfo` 的旧总背包回退、分类容量保护和缺失记录补齐；`KnapsackService` 的穿戴栏类型拒绝白名单。
- 静态/单元式场景 1：`RolePackAll=120`、其余三类缺失，解析结果为总包/装备/消耗品/材料均为 120，通过。
- 静态/单元式场景 2：`RolePackAll=120`、`RolePackEquip=150`、其余缺失，解析结果为总包 120、装备 150、消耗品/材料 120，通过。
- 静态/单元式场景 3：`RoleCurrtEquipPack` 不在 `KnapsackService.IsPersistedType` 白名单中，开格子请求会返回参数错误，通过。
- 代码检查：两个读取入口均包含 `GetLegacyFallback`、`GetMigratedGridCount` 和 `EnsureMissingKnapsackRows`；`git diff --check -- LYF_Server/CenterServer/DB/Modle/LoginModle.cs LYF_Server/CenterServer/DB/Modle/CentRoleModel.cs Docs/AI` 无新增空白错误。
- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/knapsack-legacy-migration-fix-20260904/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/knapsack-legacy-migration-fix-20260904/obj/" --nologo`。
- 构建结果：成功，0 个错误；保留既有 2 条警告（`OnlineRole._roleBaseAttrEntity` 未使用、`LoginCtrl.cs` 异步调用未等待）。
- 未执行项：未连接真实数据库验证补齐写入，也未执行多进程服务与 Unity 客户端联网联调。

## [VERIFY][协作][场景实体][TR-194] 2026-09-04 角色组件目录主 Agent 最终验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1739—1747 行；历史时点记录，不代表当前仍在执行。

- 目录结构：`GameServer/Entity/Components` 包含 `IRoleComponent.cs`、`RoleComponentContainer.cs`、`RoleKnapsackComponent.cs`；`Entity` 根目录未保留组件源码。
- 静态检查：组件目录三个文件均为单一同名顶级类型；`GameServer.csproj` 编译路径与实际目录一致；`git diff --check -- LYF_Server/GameServer Docs/AI` 无新增空白错误。
- 构建命令：`dotnet build LYF_Server\\LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:\\unitypro\\LYFMMORGP\\_temp\\component-directory-final-20260904\\bin\\" -p:BaseIntermediateOutputPath="D:\\unitypro\\LYFMMORGP\\_temp\\component-directory-final-20260904\\obj\\"`。
- 构建结果：完整解决方案生成成功，0 个错误；保留既有 2 条警告。
- 设计记录：已在 `PROJECT_CONTEXT.md` 和 `DECISIONS.md` 增加按职责建立目录的约束。
- 未执行项：未进行真实多进程服务与 Unity 客户端联网联调。

## [VERIFY][协作][TR-195] 2026-09-04 协作规则配置核验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1748—1756 行；历史时点记录，不代表当前仍在执行。

- 核验范围：`AGENTS.md`、`AIconfig/config.toml`、`AIconfig/workflow.md`、`AIconfig/agent/*.md`、`Docs/AI/DECISIONS.md`。
- 核验结果：已明确每个顶级类、接口、枚举和委托独立同名文件；已明确主 Agent 不直接修改客户端或服务端业务代码，业务代码必须由对应开发子 Agent 执行。
- 流程结果：已明确产品拆解、架构设计、开发、测试、审查、复审和主 Agent 验收顺序；测试与审查结果必须回写 `Docs/AI`。
- 强制委派核验：配置已要求涉及客户端或服务端业务代码时，主 Agent 必须先创建并委派对应开发子 Agent。
- 业务源码变更：无。
- 未执行项：本次为配置静态核验，不涉及编译或运行测试。

## [VERIFY][场景实体][TR-196] 2026-09-04 OnlineRole 角色组件架构复验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1757—1767 行；历史时点记录，不代表当前仍在执行。

- 测试目标：验证角色组件注册、获取、生命周期、重复注册保护，以及 RoleManager 的调用顺序和一类一文件约束。
- 组件静态检查：`IRoleComponent` 定义 `Initialize`、`AfterLoad`、`AfterAddedToRole`、`Update`、`Dispose` 生命周期；`RoleComponentContainer` 按注册顺序初始化/更新、按逆序释放；`GetComponent` 缺失时抛出明确异常，`TryGetComponent` 返回 false；重复类型注册和组件所属角色不匹配均被拒绝。
- OnlineRole 检查：`InitSerializes` 使用初始化标记保证可重复调用不会重复创建 `RoleKnapsackComponent`；组件通过 `SetComponent/GetComponent/TryGetComponent` 暴露；`Dispose` 可触发组件释放；背包容量组件直接代理角色唯一容量字段。
- RoleManager 顺序检查：新增角色依次执行 `InitSerializes -> AfterLoad -> 状态置为 Online -> 索引登记 -> AfterAddedToRole`；替换/移除角色先调用旧角色 `Dispose`；固定帧通过 `role.Update(delta)` 驱动组件更新。
- 一类一文件扫描：排除 `obj/bin` 后扫描 GameServer 31 个 `.cs` 文件，检测到的 29 个顶级类型均独立同名文件，无多顶级类型文件；`AssemblyInfo.cs` 为仅程序集属性文件，符合例外。
- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/role-component-final/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/role-component-final/obj/"`。
- 构建结果：成功，0 个错误；保留 2 条既有警告：`LoginCtrl.cs` 异步调用未等待、`OnlineRole._roleBaseAttrEntity` 未使用。
- 未执行项：未启动多进程服务和 Unity 客户端进行联网联调；本次为静态架构复验和编译验证。

## [VERIFY][场景实体][TR-197] 2026-09-04 GameServer 固定帧注册顺序返工

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1768—1775 行；历史时点记录，不代表当前仍在执行。

- 返工原因：代码审查发现登录模块和角色模块先于 Redis、RoGames 注册，导致 `GameModuleManager` 固定帧更新顺序与决策不一致。
- 修改文件：`LYF_Server/GameServer/manger/GameServerModuleBootstrap.cs`。
- 静态检查：确认固定帧顺序为 Redis、RoGames、RoleManager、Decouple、HttpTask、MonsterWave、LimitedTimeOffer、MerchantEvent、MallRefresh、OpenSystem、BackstageActivity、TimeLimit、Frame、Scene、WorldEvent、Gvg；登录模块注册位于末尾且仍保留登录、创建角色、开始游戏三个协议映射。
- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/bootstrap-order-fix-20260904/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/bootstrap-order-fix-20260904/obj/"`。
- 构建结果：成功，0 个错误；保留既有 2 条警告：`LoginCtrl.cs` 异步调用未等待、`OnlineRole._roleBaseAttrEntity` 未使用。

## [VERIFY][综合][TR-198] 2026-09-04 一类一文件代码审查

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1776—1784 行；历史时点记录，不代表当前仍在执行。

- 审查范围：`LYF_Server/GameServer` 顶级类型拆分、`GameServer.csproj` 编译清单、模块单例/队列/Update 结构及注册顺序。
- 静态结果：排除 `obj/bin` 后扫描 32 个 `.cs` 文件，每个文件恰好包含一个顶级类、接口、枚举、委托或结构体，且类型名与文件名一致；`GameServerModules.cs` 不存在；项目清单无缺失或多余源文件。
- 构建结果：`dotnet build LYF_Server\\LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:\\unitypro\\LYFMMORGP\\_temp\\one-type-per-file-review\\bin\\" -p:BaseIntermediateOutputPath="D:\\unitypro\\LYFMMORGP\\_temp\\one-type-per-file-review\\obj\\"`，0 个错误；保留既有 2 条警告（`OnlineRole._roleBaseAttrEntity` 未使用、`LoginCtrl.cs` 异步调用未等待）。
- 队列与单例结果：`GameModuleBase` 为各模块提供独立并发队列和 `Update` 消费入口；所有预注册玩法管理器继承 `GameManagerModule<T>` 并通过 `XXXManager.Instance` 使用；总队列仅经 `GameModuleManager` 路由。
- 问题清单（需返工）：`GameServerModuleBootstrap.RegisterAll` 当前先注册登录、角色，再注册 Redis/小游戏等管理器，导致实际固定帧顺序为“登录、角色、Redis、小游戏……”。这与 `Docs/AI/DECISIONS.md` 约定的 roserver 顺序“Redis、小游戏、角色、解耦、HTTP、怪物波次、限时特惠、商人、商城刷新、系统开放、后台活动、限时系统、相框、场景、世界事件、GVG”不一致；后端 Agent 需调整注册顺序，或由架构负责人明确豁免并更新决策记录。
- 审查结论：一类一文件拆分和构建门禁通过；因固定帧注册顺序不符合已确认架构决策，本轮代码审查暂不通过，待修正后复审。

## [VERIFY][场景实体][TR-199] 2026-09-04 固定帧注册顺序复验与代码审查复审

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1785—1793 行；历史时点记录，不代表当前仍在执行。

- 测试目标：验证返工后的管理器注册顺序、协议路由和一类一文件结构未回归。
- 静态检查：确认 `GameServerModuleBootstrap.RegisterAll` 按 Redis、RoGames、RoleManager、Decouple、HttpTask、MonsterWave、LimitedTimeOffer、MerchantEvent、MallRefresh、OpenSystem、BackstageActivity、TimeLimit、Frame、Scene、WorldEvent、Gvg 顺序注册；登录模块最后注册且保留登录、创建角色、开始游戏三个协议映射。32 个 GameServer 源文件仍为一类一文件，`GameServerModules.cs` 无引用。
- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/bootstrap-order-fix-final-20260904/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/bootstrap-order-fix-final-20260904/obj/"`。
- 构建结果：成功，0 个错误；保留既有 2 条警告（`LoginCtrl.cs` 异步调用未等待、`OnlineRole._roleBaseAttrEntity` 未使用）。
- 代码审查复审：通过；此前固定帧顺序问题已修正，队列、单例、Update 和项目文件清单均无回归。
- 未执行项：未启动完整四服务器与 Unity 客户端进行真实联网联调。

## [VERIFY][协作][TR-200] 2026-09-04 主 Agent 最终集成验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1794—1800 行；历史时点记录，不代表当前仍在执行。

- 验证目标：在返工后的最终工作区重新执行完整服务端方案构建，并确认协作约束和注册顺序记录一致。
- 构建命令：`dotnet build LYF_Server\\LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:\\unitypro\\LYFMMORGP\\_temp\\bootstrap-order-fix-final-20260904\\bin\\" -p:BaseIntermediateOutputPath="D:\\unitypro\\LYFMMORGP\\_temp\\bootstrap-order-fix-final-20260904\\obj\\"`。
- 构建结果：成功，0 个错误；保留既有 2 条警告（`OnlineRole._roleBaseAttrEntity` 未使用、`LoginCtrl.cs` 异步调用未等待）。
- 集成结论：一类一文件约束、主 Agent 调度边界、模块独立队列、单例管理器和 roserver 固定帧注册顺序均已落实；未发现新的静态问题。

## [VERIFY][场景实体][TR-201] 2026-09-04 OnlineRole 组件化代码审查

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1801—1812 行；历史时点记录，不代表当前仍在执行。

- 审查范围：`Entity/IRoleComponent.cs`、`RoleComponentContainer.cs`、`RoleKnapsackComponent.cs`、`OnlineRole.cs`、`manger/RoleManager.cs` 及 `GameServer.csproj`。
- 一类一文件：通过。排除 `obj/bin` 后 GameServer 共 30 个含类型源文件，每个文件恰好一个顶级类型，且类型名与文件名一致；项目文件包含 31 个编译项（另含无顶级类型的 `AssemblyInfo.cs`），组件文件均已纳入。
- 架构与线程边界：组件容器按注册顺序执行 `Initialize`、`AfterLoad`、`AfterAddedToRole`、`Update`，销毁按逆序执行；`RoleManager` 仅在逻辑线程触发生命周期，组件持有 `OnlineRole` 上下文；背包组件代理角色唯一容量字段，未引入重复状态。网络回调仍经 `ClientMessageManager` 队列，未发现跨线程直接修改组件状态。
- 注释检查：接口生命周期、容器顺序、角色初始化/销毁、背包容量代理和角色管理边界均有中文注释；未发现新增关键逻辑缺少基本说明。
- 构建结果：`dotnet build LYF_Server\\LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:\\unitypro\\LYFMMORGP\\_temp\\role-component-review\\bin\\" -p:BaseIntermediateOutputPath="D:\\unitypro\\LYFMMORGP\\_temp\\role-component-review\\obj\\"`，0 个错误；保留既有 2 条警告（`LoginCtrl.cs` 异步调用未等待、`OnlineRole._roleBaseAttrEntity` 未使用）。
- 问题清单：
  - [P1] `LYF_Server/GameServer/manger/RoleManager.cs:57-61`：同一 `(GateSessionId, UnitySessionId)` 绑定到新角色时，仅执行 `m_onlineRoleMap.Remove(mappedRoleID)`，未将被替换的旧 `OnlineRole` 标记离线、调用 `Dispose` 或清理其组件资源。组件化后会造成旧角色组件泄漏，并可能保留在线状态；应复用统一替换/下线清理路径。
  - [P1] `LYF_Server/GameServer/Entity/RoleComponentContainer.cs:68-103`、`RoleManager.cs:34-37,48-68,96-99`：组件 `Initialize`/`AfterLoad`/`AfterAddedToRole`/`Update`/`Dispose` 均未做异常隔离或回滚。任一组件抛异常时，新增角色可能半初始化且未释放；更新会停止当前角色及后续角色；销毁异常会阻止索引移除，留下离线角色和会话映射。应明确失败策略，至少保证单组件异常不阻断同角色其他组件/其他角色，并在替换和移除路径最终清理索引。
- 审查结论：一类一文件、构建、组件注册入口和线程边界通过；因上述两个生命周期问题，组件化改造本轮打回后端 Agent 修复，修复后需重跑静态扫描、构建并复审。

## [VERIFY][场景实体][TR-202] 2026-09-04 OnlineRole 角色组件架构第二轮代码审查

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1813—1823 行；历史时点记录，不代表当前仍在执行。

- 审查范围：`LYF_Server/GameServer/Entity/RoleComponentContainer.cs`、`OnlineRole.cs`、`manger/RoleManager.cs` 及项目引用。
- P1 复核：组件容器已对五个生命周期阶段逐组件捕获异常并记录日志，单个组件失败不会阻断同角色后续组件、其他角色更新或索引清理；`RoleManager` 同一复合会话冲突分支已标记旧角色离线、清理双索引并调用 `Dispose`。
- 注释检查：角色组件接口、生命周期顺序、异常隔离、会话索引和线程边界均有简洁中文注释；未发现新增关键逻辑缺少注释。
- 一类一文件：排除 `obj/bin` 后扫描 GameServer 31 个 `.cs` 文件，通过；每个顶级类型均独立同名文件，`AssemblyInfo.cs` 为无类型例外。
- 构建命令：`dotnet build LYF_Server\LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:\unitypro\LYFMMORGP\_temp\role-component-review2\bin\" -p:BaseIntermediateOutputPath="D:\unitypro\LYFMMORGP\_temp\role-component-review2\obj\"`。
- 构建结果：成功，0 个错误；保留既有 2 条警告（`OnlineRole._roleBaseAttrEntity` 未使用、`LoginCtrl.cs` 异步调用未等待）。
- 残留问题：[P2] `RoleManager.AddOnlineRole` 按相同角色 ID 替换旧实例时未设置 `oldRole.State = Offline`；建议后端统一在 Dispose 前标记离线，避免外部持有旧引用时状态不一致。
- 审查结论：两个原 P1 已修复；因上述 P2 状态一致性问题，本轮暂不完全通过，补充后需再次复审。未进行真实多进程联网联调。

## [VERIFY][背包商城][TR-203] 2026-09-04 客户端按 KnapsackType 容量适配

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1824—1833 行；历史时点记录，不代表当前仍在执行。

- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/UI/Main/Window/KnapsackWindow.cs`、`LYF_MMO/Assets/GM_Game/Scripts/controller/UIController/MainCtrl.cs`。
- 实现结果：`UpdateOpenedGridCount(KnapsackType type, int openedGridCount)` 只更新匹配分类；不存在时新增该类型记录；只有当前分类立即刷新。`MainCtrl` 将 `OpenKnapsackGridRet.KnapsackType` 转为 `KnapsackType` 传入。正式客户端未发现扩容请求入口，未新增请求，也未修改 `UI/test` Mock。
- Unity：`ready_for_tools=true`，未编译，控制台错误/警告 0 条。
- 静态检查：`git diff --check` 通过。
- Network 构建：`LYF_Server/Network/Network.csproj` 使用 `_temp/client-knapsack-type-20260904/` 输出，0 警告、0 错误。
- GM_Game 构建：使用现有正式 `Network.dll` 失败 1 个 `CS1061`，原因是 DLL 无 `KnapsackType` 属性；该 DLL 需替换为新版协议产物后复验。
- 当前结论：源码改动完成，测试暂不通过，阻塞为客户端协议 DLL 未同步；未进行真实联网联调。

## [VERIFY][背包商城][TR-204] 2026-09-04 Luban 背包配置表初始化验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1834—1839 行；历史时点记录，不代表当前仍在执行。

- 验证命令：`dotnet build Network/Network.csproj --no-restore -p:BaseIntermediateOutputPath=D:/unitypro/LYFMMORGP/_temp/luban-init/obj/ -p:OutputPath=D:/unitypro/LYFMMORGP/_temp/luban-init/bin/`。
- 验证结果：Network 构建成功，0 个警告，0 个错误；`TbKnapsackInfo` 已完成加载、启动打印和按 `KnapsackType` 查询。
- 临时产物：`D:/unitypro/LYFMMORGP/_temp/luban-init/`，待产品经理确认后清理。

## [VERIFY][背包商城][TR-205] 2026-09-04 角色背包默认容量配置表验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1840—1850 行；历史时点记录，不代表当前仍在执行。

- 测试对象：`Luaban/MiniTemplate/Datas/#KnapsackInfo.xlsx`；本次测试未修改该配置表、`__tables__.xlsx` 或任何客户端、服务端业务文件。
- 表格静态校验：使用 Artifact Tool 读取 `Sheet1!A1:C7`，字段为 `KnapsackType`、`DefaultGridCount`；有效数据恰为四行 `(0,81)`、`(1,81)`、`(2,81)`、`(3,81)`，不存在类型 `4`。字段类型均为 `int`，与 `KnapsackType` 的四类可扩容背包一致。
- 公式和可读性校验：公式错误扫描（`#REF!`、`#DIV/0!`、`#VALUE!`、`#NAME?`、`#N/A`）匹配 0 项；已渲染表格，标题、类型说明与四条容量数据均可见。
- Luban 生成验证：在 `Luaban/MiniTemplate` 执行 `dotnet .\\Luban\\Luban.dll -t all -c cs-bin -d bin --conf .\\luban.conf`，并通过 `outputCodeDir`、`outputDataDir` 将全部输出定向至 `D:/unitypro/LYFMMORGP/_temp/knapsack-config-test-20260904/luban-output/`。命令成功完成，生成 `Code/KnapsackInfo.cs`、`Code/TbKnapsackInfo.cs` 与 `Data/tbknapsackinfo.bytes`。
- 产物复核：`TbKnapsackInfo` 以 `KnapsackType` 建立 `Dictionary<int, KnapsackInfo>`；生成二进制为 9 字节 `04 00 51 01 51 02 51 03 51`，其中记录数为 4，四组类型/容量与源表一致。
- 结论：通过。本轮配置表满足四类角色背包默认容量均为 81、排除穿戴栏类型 `4` 的验收标准。
- 未执行项：本轮按任务边界未修改服务端新角色初始化逻辑，也未将临时 Luban 产物复制到正式运行目录；未执行运行时读取配置、数据库创建角色或多服务器联调。
- 临时产物：`D:/unitypro/LYFMMORGP/_temp/knapsack-config-test-20260904/`，包含只读检查脚本、渲染图、Artifact 检查结果和定向 Luban 生成产物，待产品经理确认后清理。

## [VERIFY][背包商城][TR-206] 2026-09-04 按 KnapsackType 分类型容量代码审查

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1851—1859 行；历史时点记录，不代表当前仍在执行。

- 检查范围：数据库表与模型、GameServer 背包链路、协议源/生成代码、Unity 背包窗口/MainCtrl、Network.dll、中文注释、一类一文件和线程边界。
- 通过项：四类容量独立存储且新角色各 81；扩容只修改指定类型；`RoleCurrtEquipPack` 被拒绝扩容；协议字段双端存在；客户端 DLL 与工程构建可用；网络消息经队列在逻辑线程处理；关键逻辑有中文注释。
- [P1] 失败项：`LoginModle.cs:426-431`、`CentRoleModel.cs:179-186` 对旧单条总背包记录只继承总背包容量，缺失三类在旧容量大于 81 时回退为 81，违反“旧单条记录迁移保留容量”。
- 构建：完整 `LYF_Server.sln` 和 `LYF_MMO/GM_Game.csproj` 均为 0 错误，保留既有编译警告；产物位于 `_temp/knapsack-type-review-final/` 与 `_temp/knapsack-type-client-review/`。
- 审查结论：不通过，退回后端 Agent；修复后需增加旧容量大于 81、已有分类更大容量和穿戴栏请求拒绝测试。
- 未执行项：未进行真实数据库迁移和多进程联网联调。

## [VERIFY][背包商城][TR-207] 2026-09-04 旧背包单条记录迁移修复复审

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1860—1871 行；历史时点记录，不代表当前仍在执行。

- 审查范围：`LYF_Server/CenterServer/DB/Modle/LoginModle.cs`、`LYF_Server/CenterServer/DB/Modle/CentRoleModel.cs`、`LYF_Server/GameServer/Service/KnapsackService.cs`，以及背包类型枚举和容量表模型。
- 静态推演：旧总包 `120` 时得到总包、装备、消耗品、材料均为 `120`；旧总包 `120` 且装备包 `150` 时得到 `120/150/120/120`；仅装备包 `150` 时得到 `81/150/81/81`，已有较大容量未降低。
- 迁移行为：`GetPersistedKnapsackTypes` 仅包含四类可扩容背包；`EnsureMissingKnapsackRows` 仅插入缺失记录，既有记录不更新；`GetMigratedGridCount` 以旧总包与已有分类记录中的较大容量回包。两处新增迁移注释均为中文。
- 穿戴栏检查：`RoleCurrtEquipPack` 未参与补齐循环；`KnapsackService.IsPersistedType` 不接受该类型，扩容请求会返回参数错误。
- 一类一文件：`LoginModle.cs` 与 `CentRoleModel.cs` 均仅含一个同名顶级类，通过。
- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/knapsack-legacy-migration-fix-20260904/review-bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/knapsack-legacy-migration-fix-20260904/review-obj/"`。
- 构建结果：成功，0 个错误；保留 2 条既有警告：`OnlineRole._roleBaseAttrEntity` 未使用、`LoginCtrl.cs` 异步调用未等待。
- 差异检查：`git diff --check -- LYF_Server Docs/AI AGENTS.md AIconfig` 无空白错误；仅输出既有行尾转换提示。
- 审查结论：通过。此前旧总包容量大于 81 时缺失分类回退为 81 的 P1 已修复；未执行真实数据库迁移和多进程联网联调。

## [VERIFY][网络][TR-208-FILES] 查询任务的正式文档产物：2026-09-04 GameServer 协议处理器架构只读分析

- 该任务记录的正式文档路径：`项目分析/MESSAGE_HANDLER_ARCHITECTURE.md`。
- 查询正文、当时状态及原文验证边界见 `Docs/AI/QUERY_TASKS.md` 的 `[TR-208]`；本条只保留文件产物追溯，不新增完成或检查结论。

## [VERIFY][网络][TR-209] 2026-09-04 GameServer 协议处理器按领域拆分开发自检

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1881—1892 行；历史时点记录，不代表当前仍在执行。

- 检查范围：`LYF_Server/GameServer/MessageHandler/`、`manger/ClientMessageManager.cs`、`GameServerApp.cs`、`GameServer.csproj`。
- 目录和处理器：新建 `Login`、`Role`、`Knapsack`、`Item`、`Shop`、`Equip`、`Task` 子目录；存在 15 个新增 C# 源文件，其中 4 个注册基础设施类型和 11 个真实入站 Handler。`Item`、`Shop`、`Equip`、`Task` 仅保留目录，不含空 Handler。
- 注册覆盖：`GameMessageProtocolRegister` 有 11 次 `RegisterHandler` 调用，分别覆盖 Gate 的登录、创建角色、开始游戏、进入世界、开背包格子，以及 Center 的登录、创建角色、开始游戏、角色技能、背包信息、背包容量持久化结果。网络入口同样为 Gate 5 条、Center 6 条。
- 静态检查：`ClientMessageManager` 中搜索 `Game_LoginCtrl`、`RoleManager` 和 `switch`，匹配 0 项；注册表键包含来源与协议号；代码包含重复注册、未知来源、非法协议号和未注册消息的日志拒绝路径；每个新增顶级类型位于独立同名文件，新增 C# 文件均显式写入旧式 `GameServer.csproj`。
- 首次构建：完整方案构建发现 `SyncKnapsackGridCountFromCenterHandler.Key` 缺少 `get` 访问器，产生 8 个语法错误；已修复，失败记录保留。
- 修复后构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/message-handler-refactor-20260904/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/message-handler-refactor-20260904/obj/"`。
- 修复后构建结果：成功，0 个错误，保留 1 条既有警告：`OnlineRole._roleBaseAttrEntity` 未使用。所有构建产物位于 `D:/unitypro/LYFMMORGP/_temp/message-handler-refactor-20260904/`。
- 差异检查：`git diff --check -- LYF_Server/GameServer Docs/AI` 无空白错误；仅输出工作区既有文件的 LF/CRLF 转换提示。
- 未执行项：未启动真实四服务器进程，未进行 Unity 联网联调；未知消息和重复注册拒绝路径为静态审阅，待测试 Agent 进行独立复验。

## [VERIFY][协作][TR-210] 2026-09-04 GameServer 处理器业务命名与来源分支开发验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1893—1902 行；历史时点记录，不代表当前仍在执行。

- 修改范围：`LYF_Server/GameServer/MessageHandler/`、`manger/ClientMessageManager.cs`、`GameServer.csproj` 及协作架构记录。
- 文件结构：删除全部 `*FromGateHandler.cs`、`*FromCenterHandler.cs`；保留 `LoginGameServerHandler.cs`、`CreateRoleHandler.cs`、`StartGameHandler.cs`、`EnterWorldHandler.cs`、`RoleSkillInfoHandler.cs`、`RoleKnapsackInfoHandler.cs`、`OpenKnapsackGridHandler.cs`、`SyncKnapsackGridCountHandler.cs` 八个业务命名处理器。
- 注册验证：登录、建角、开始游戏各创建一个处理器实例，并以 Gate/Center 两个 `(来源, 协议号)` 键登记；`CanHandle` 接受两个来源，`Handle` 根据 `GameMessageHandlerContext.Source` 分别处理请求或回包。其余五条单向协议仍使用来源键注册，并在处理器内校验来源。
- 静态检查：搜索 `FromGate`、`FromCenter`、旧 `IGameMessageHandler.Key` 与旧 `Register(IGameMessageHandler)` 均无匹配；`ClientMessageManager` 不引用 `Game_LoginCtrl`、`RoleManager`，不含业务 `switch`；`GameServer.csproj` 显式编译项与八个处理器文件一致。
- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/handler-name-cleanup-20260904/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/handler-name-cleanup-20260904/obj/"`。
- 构建结果：成功，0 个错误；保留既有 2 条警告：`OnlineRole._roleBaseAttrEntity` 未使用、`LoginCtrl.cs` 异步调用未等待。
- 未执行项：未启动真实四服务器与 Unity 客户端进行联网联调；本记录为后端开发构建验证，仍需测试与代码审查独立复验。

## [VERIFY][背包商城][TR-211] 2026-09-04 RoleKnapsackComponent 背包事务融合复验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1903—1909 行；历史时点记录，不代表当前仍在执行。

- 构建命令：`dotnet build LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/bag-transition-final-20260904/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/bag-transition-final-20260904/obj/"`。
- 构建结果：完整服务端方案构建成功，0 个错误；保留既有 2 条警告：`OnlineRole._roleBaseAttrEntity` 未使用、`LoginCtrl.cs` 异步调用未等待。
- 静态检查：`git diff --check -- GameServer Docs/AI` 通过；事务类型均按一类一文件组织，`GameServer.csproj` 已显式包含新增文件。
- 未执行项：未启动真实四服务器和 Unity 联网联调；事务实际扣除、发放和装备操作需待权威数据模型接入后验证。

## [VERIFY][网络][背包商城][TR-212] 2026-09-04 商城购买协议策划与系统架构设计

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1910—1916 行；历史时点记录，不代表当前仍在执行。

- 只读分析范围：`RequestEntity.proto`、`ResultEntity.proto`、`NetDefine.cs`、GateServer/GameServer 网络注册与转发、`ShopInfo`、NPC 商店配置、`RoleNpcShopPurchaseTable`。
- 文档产出：新增 `项目分析/SHOP_BUY_PROTOCOL_DESIGN.md`，未修改客户端或服务端业务源码，未生成构建产物。
- 设计验证：确认现有商品配置可支持服务端反查价格/货币/物品；确认限购表主键包含 `RoleID + NpcID + ShopID`，因此请求必须带 `npc_id`；确认现有表仅有每日和永久计数，周/月/年不能直接开放。
- 交接结论：建议命令号 `11130`，请求幂等字段 `request_id`，响应 `ErrorInfo` 回显请求号；Gate 只透传，GameServer 逻辑线程校验并在权威事务未接入时返回 `FeatureNotReady` 或 `ServerError`。
- 未执行项：未运行协议生成、Gate/GameServer/Unity 构建和联网联调；等待后端与客户端依据设计文档实施后由测试 Agent 验证。

## [VERIFY][网络][背包商城][TR-213] 2026-09-04 商城购买协议后端链路开发自检

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1917—1928 行；历史时点记录，不代表当前仍在执行。

- 协议生成：使用 `D:/unitypro/LYFMMORGP/LYF_Server/ProtoBuf/bin/protoc.exe`，将两个协议源生成到 `_temp/shop-buy-protocol-20260904/proto-out/`，并同步至 `LYF_Server/Network/Proto`；生成代码包含新增消息和 `CmdCode` 14-23。
- 网络链路：`NetDefine.CMD_BuyShopItemCode` 为 11130；GateServer `GateRoleCtrl` 与 `GateServerApp` 新增双向透传/注册；GameServer 注册 Gate 侧命令与 `RequestBuyShopItem`。
- 处理器静态校验：检查会话、角色在线、非零请求号、条目数量、单项数量、重复商品、NPC 商店类型、ShopInfo/NPC 归属和重复请求号；无权威事务时固定返回 `FeatureNotReady`。
- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/shop-buy-protocol-20260904/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/shop-buy-protocol-20260904/obj/"`。
- 构建结果：成功，0 个错误；保留 2 条既有警告：登录服异步调用未等待、`OnlineRole._roleBaseAttrEntity` 未使用。
- 差异检查：`git diff --check` 通过，仅有既有 LF/CRLF 转换提示。
- 未执行项：未执行真实购买事务、数据库幂等恢复、四服务器进程和 Unity 联网联调，待测试 Agent 独立复验。
- 补充复验：`RequestBuyShopItem` 同一 `(RoleID, request_id)` 且载荷一致时复用 `FeatureNotReady`；载荷不一致时返回 `DuplicateRequest`；临时缓存上限为 10000，避免进程内存无界增长。
- 补充构建：增加 GameServer 显式 `System` 引用后重建成功，0 个错误，保留 `OnlineRole._roleBaseAttrEntity` 既有警告。

## [VERIFY][背包商城][TR-214] 2026-09-05 商城购买请求 role_id 字段补充

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1929—1946 行；历史时点记录，不代表当前仍在执行。

- 协议变更：`BuyShopItemReq` 保留 `shop_type=1`、`items=2`、`npc_id=3`、`request_id=4`，新增 `role_id=5`；未调整既有字段编号或错误码。
- 生成命令：使用 `D:/unitypro/LYFMMORGP/LYF_Server/ProtoBuf/bin/protoc.exe` 生成到 `D:/unitypro/LYFMMORGP/_temp/shop-buy-protocol-roleid-20260905/proto-out/`，并同步 `Network/Proto/RequestEntity.cs`、`ResultEntity.cs`。
- 处理器校验：`RequestBuyShopItem` 从会话双索引取得在线角色，拒绝 `role_id=0` 或与会话角色 ID 不一致的请求，返回 `InvalidSession`；客户端字段不作为身份来源。
- 待执行：完整服务端方案构建、差异检查和测试/代码审查 Agent 独立复验。
- 复验结果（2026-09-05）：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/shop-buy-protocol-roleid-20260905/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/shop-buy-protocol-roleid-20260905/obj/"` 成功，0 个错误，保留 2 条既有警告（登录服异步调用未等待、`OnlineRole._roleBaseAttrEntity` 未使用）。
- 差异检查：`git diff --check -- LYF_Server/ProtoBuf LYF_Server/Network LYF_Server/GameServer Docs/AI` 通过；生成和构建产物均位于 `_temp/shop-buy-protocol-roleid-20260905/`。
- 兼容性复核：协议源静态确认 `shop_type=1`、`items=2`、`npc_id=3`、`request_id=4`、`role_id=5` 均存在；`BuyShopItemRes` 与 `ErrorInfo` 均已生成。GateServer 对 11130 请求/结果各注册一次，GameServer 注册器包含一个 `RequestBuyShopItem` 实例；网络回调继续通过 `ClientMessageManager` 入队，处理器只在逻辑线程执行。

### 客户端同步验证（2026-09-05）

- DLL 校验：`LYF_MMO/Assets/GM_Game/Plugins/Net/Network.dll` 已同步自 `_temp/shop-buy-protocol-roleid-20260905/bin/Network.dll`，SHA256 均为 `52F8EDFD7229E3EE07912D4B3659238F720EFF09E1B4112E565A1512E4BEC8CA`；更新前 DLL 已备份到 `_temp/shop-buy-client-roleid-20260905/original/Network.dll`。
- 协议反射检查：`BuyShopItemReq.RoleId` 存在，类型为 `System.UInt32`。
- 逻辑检查：`ShopPurchaseClient` 在发送前校验 `Global.Instance.mainRoleInfo.BaseInfo.RoleId > 0`，将该值写入请求；该字段只用于服务端会话角色的一致性校验，客户端不能以此建立身份。
- Unity 编译：执行 Unity 资产刷新和脚本编译后，编辑器控制台为 0 错误、0 警告。
- 未执行项：未进行真实 GateServer/GameServer/Unity 联网购买和权威事务验证。

## [VERIFY][网络][背包商城][TR-215] 2026-09-04 商城购买协议客户端接入验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1947—1954 行；历史时点记录，不代表当前仍在执行。

- DLL 校验：客户端 `Network.dll` 已同步自 `_temp/shop-buy-protocol-20260904/bin/Network.dll`，SHA256 为 `C5F29603F15EBFBA747D5113163B48D1AAA36614F3108F1F06E86A660438B573`；原 DLL 已备份到 `_temp/shop-buy-client-20260904/original/`。
- 协议静态检查：新 DLL 包含 `BuyShopItemReq`、`BuyShopItem`、`BuyShopItemRes`、`ErrorInfo`、`NetDefine.CMD_BuyShopItemCode` 和购买错误码，字段与设计文档一致。
- Unity MCP 脚本校验：`ShopPurchaseClient.cs`、`BuyItemWidget.cs`、`NpcShopWindow.cs`、`MainCtrl.cs` 均为 0 错误、0 警告。
- 行为检查：请求生成非零 `request_id`，商品卡片发送 `shop_type/npc_id/shop_id/buy_count`；响应仅按本地待处理请求号回调，错误码只用于提示和结束等待状态，未实现客户端本地成功逻辑。
- 未执行项：Unity 真机运行、GateServer -> GameServer 多进程联调和真实购买事务未执行，交由测试 Agent 继续验证。
- 阻塞发现：静态检查发现 GameServer `RequestBuyShopItem` 引用了生成协议中不存在的 `BuyShopItemReq.RoleId`，与会话确定角色身份的设计不一致；未修改服务端，已交后端修正后再联调。

## [VERIFY][网络][背包商城][TR-216] 2026-09-05 商城购买协议 role_id 最终复验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1955—1961 行；历史时点记录，不代表当前仍在执行。

- 协议字段：`shop_type=1`、`items=2`、`npc_id=3`、`request_id=4`、`role_id=5`；生成 C# 与客户端 DLL 均包含 `RoleId`。
- 服务端构建 0 错误，GM_Game 构建 0 错误；保留项目既有警告。
- 静态检查：处理器校验会话角色与 `role_id` 一致性；Gate 仅透传；客户端填充当前角色 ID；`git diff --check` 通过。
- 结论：协议和 Unity -> GateServer -> GameServer 链路骨架通过；未进行真实多服务器联网、扣费发货、限购事务和重启幂等验证。

## [VERIFY][综合][TR-217] 2026-09-05 GameMessageHandlerContext 注释验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1962—1967 行；历史时点记录，不代表当前仍在执行。

- 修改文件：`LYF_Server/GameServer/MessageHandler/GameMessageHandlerContext.cs`。
- 验证结果：`dotnet build LYF_Server.sln --no-restore --configuration Debug` 成功，0 个错误；保留既有 2 条警告。
- 差异检查：`git diff --check -- GameServer/MessageHandler/GameMessageHandlerContext.cs` 通过。

## [VERIFY][综合][TR-218] 2026-09-05 BagTransition 单文件合并验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1968—1975 行；历史时点记录，不代表当前仍在执行。

- 验证范围：`LYF_Server/GameServer/Service/BagTransition/BagTransition.cs` 与 `GameServer.csproj`。
- 静态检查：事务目录 C# 文件数为 1；`BagTransition.cs` 包含 15 个类型定义；项目清单仅包含 `Service\\BagTransition\\BagTransition.cs`，无旧 `Service\\item` 引用。
- 构建命令：`dotnet build LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/bag-transition-single-file-20260905/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/bag-transition-single-file-20260905/obj/"`。
- 构建结果：成功，0 个错误；保留既有 2 条警告：`OnlineRole._roleBaseAttrEntity` 未使用、`LoginCtrl.cs` 异步调用未等待。
- 差异检查：目标文件无行尾空白；全仓 `git diff --check` 报告的行尾空白来自本次范围外的既有文件。

## [VERIFY][背包商城][TR-219] 2026-09-05 按商店 ID 查询商品配置验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1976—1981 行；历史时点记录，不代表当前仍在执行。

- 修改文件：`LYF_Server/Network/LubanManage/LubanMgr.cs`。
- 验证命令：`dotnet build Network/Network.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/shop-item-by-shopid-20260905/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/shop-item-by-shopid-20260905/obj/"`。
- 验证结果：构建成功，0 个警告、0 个错误。

## [VERIFY][背包商城][TR-220] 2026-09-05 ShopItemInfo 运行时解析验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1982—1988 行；历史时点记录，不代表当前仍在执行。

- 修改范围：`Network/LubanManage/Parse/ShopItemParseInfo.cs`、`ShopItemPurchaseCondition.cs`、`ShopItemEnvironmentCondition.cs`、`LubanMgr.cs`、`Network.csproj`。
- 字段映射：`Id/ItemId/BindAfterPurchase` 映射商品、道具和绑定状态；`PurchaseLimitType/PurchaseLimitCount` 映射限购；解锁、职业、等级、任务与时间字段映射环境限制；`ItemPerMount/Discount/PriceChange/Price/CurrencyType` 映射购买消耗。
- 验证命令：`dotnet build Network/Network.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/shop-item-parse-20260905/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/shop-item-parse-20260905/obj/"`。
- 验证结果：构建成功，0 个警告、0 个错误。

## [VERIFY][背包商城][TR-221] 2026-09-05 解析商品按商店 ID 分组验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1989—1994 行；历史时点记录，不代表当前仍在执行。

- 结构检查：`_shopItemParseInfos` 外层键为 `ShopId`，内层为 `ShopItemID -> ShopItemParseInfo`；按商店查询不再扫描完整商品表。
- 验证命令：`dotnet build Network/Network.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/shop-item-parse-grouped-20260905/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/shop-item-parse-grouped-20260905/obj/"`。
- 验证结果：构建成功，0 个警告、0 个错误。

## [VERIFY][背包商城][TR-222] 2026-09-05 ItemDesc 公共物品描述验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1995—2001 行；历史时点记录，不代表当前仍在执行。

- 修改范围：`MMoRpgCommon/Item/ItemDesc.cs`、`ItemExtraData.cs`、`enum/Item.cs`、`MMoRpgCommon.csproj`。
- 静态检查：`ItemDesc` 字段与参考结构对应；默认绑定策略为发放不绑定、扣除匹配已绑定；扩展数据默认非空；中文 `ToString` 输出包含物品 ID、数量、UID 和变化原因。
- 验证命令：`dotnet build MMoRpgCommon/MMoRpgCommon.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/item-desc-20260905/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/item-desc-20260905/obj/"`。
- 验证结果：构建成功，0 个警告、0 个错误。

## [VERIFY][NPC][背包商城][协作][TR-223] 2026-09-05 商城类型枚举与 NPC 查询分支验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2002—2017 行；历史时点记录，不代表当前仍在执行。

- 静态检查：`ShopType` 独立放置于 `MMoRpgCommon/enum/ShopType.cs`，包含中文注释并已加入 `MMoRpgCommon.csproj`；`RequestBuyShopItem` 先校验枚举，再按 `Npc/Mall/Activity` 分支，非 NPC 分支不会调用 `GetNpcInfoById`。
- NPC 分支：校验 `npc_id`、NPC 配置存在且类型为 `ShopType.Npc`，每个商品同时校验 `ShopInfo` 存在和归属 NPC。
- 非 NPC 分支：`Mall`、`Activity` 返回 `FeatureNotReady`；未知数值返回 `InvalidShop`，不会继续执行商品列表查询。
- 构建命令：`dotnet build LYF_Server/MMoRpgCommon/MMoRpgCommon.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/shop-type-enum-20260905/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/shop-type-enum-20260905/obj/"`；`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/shop-type-enum-20260905/server-bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/shop-type-enum-20260905/server-obj/"`。
- 构建结果：公共库和完整服务端均成功，0 个错误；保留既有登录服异步调用未等待、`OnlineRole._roleBaseAttrEntity` 未使用两条警告。
- 差异检查：`git diff --check -- LYF_Server/MMoRpgCommon LYF_Server/GameServer/MessageHandler/Shop/RequestBuyShopItem.cs` 通过；仅有既有行尾转换提示。
- 未执行项：未进行真实多服务器购买联调；客户端强类型枚举参数和 `MMoRpgCommon.dll` 同步由客户端 Agent 后续复验。

### 客户端强类型枚举复验（2026-09-05）

- `ShopPurchaseClient`、`BuyItemWidget`、`NpcShopWindow` 已改用 `MMoRpgCommon.ShopType`，协议发送仍使用对应稳定数值。
- NPC 窗口拒绝非 `ShopType.Npc` 类型，并继续传递 `npc_id`；`MMoRpgCommon.dll` 已更新到 `LYF_MMO/Assets/GM_Game/Plugins/Common/MMoRpgCommon.dll`，原文件备份位于 `_temp/shop-type-enum-20260905/client-common/`。
- 客户端构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/shop-type-enum-20260905/client-bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/shop-type-enum-20260905/client-obj/"`。
- 构建结果：0 个错误，保留项目既有 21 条警告；未发现因枚举参数改造产生的新错误。

## [VERIFY][NPC][背包商城][TR-224] 2026-09-05 商城类型枚举与 NPC 商品查询复验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2018—2023 行；历史时点记录，不代表当前仍在执行。

- `ShopType` 位于共享项目 `MMoRpgCommon/enum/ShopType.cs`，保持一类一文件并已加入工程清单。
- `RequestBuyShopItem` 对未知类型返回 `InvalidShop`；`Npc` 类型要求有效 `npc_id`、NPC 类型匹配并在该 NPC 商品列表中查找 `shop_id`；`Mall`/`Activity` 不读取 NPC 配置，返回 `FeatureNotReady`。
- 完整 `LYF_Server.sln` 构建：0 错误、2 条既有警告；`git diff --check` 通过。
- 未执行真实联网及商城扣费发货事务验证。

## [VERIFY][背包商城][TR-225] 2026-09-05 商城表与商品表配置验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2024—2031 行；历史时点记录，不代表当前仍在执行。

- 使用 artifact-tool 读取参考 `ShopTable.xlsm`、`ShopCommoditTable.xlsm`，确认主表与明细表通过 `ShopId` 关联。
- 已渲染并检查项目 `#ShopItemInfo.xlsx` 原格式，扩展后商品表为 19 列、16 条商品记录；新建 `#ShopTable.xlsx` 为 9 列、4 条商店记录，标题/类型/说明行均可读。
- Luban 命令验证：`dotnet Luban/Luban.dll -t all -c cs-bin -d bin --conf luban.conf`，校验成功并生成 `tbshopiteminfo.bytes`、`tbshoptable.bytes` 及对应 C# 类型。
- 生成结果确认逻辑类型为 `ShopItemInfo/TbShopItemInfo` 和 `ShopTable/TbShopTable`；现有代码仍使用旧 `ShopInfo/TbShopInfo`，已记录为后端后续迁移事项，未覆盖旧正式生成代码。
- 临时产物：`_temp/shop-table-final-20260905/`、`_temp/shop-table-luban-auto-20260905/`，待产品经理确认后清理。

## [VERIFY][NPC][背包商城][TR-226] 2026-09-05 NPC 商店配置迁移与审查修复验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2032—2042 行；历史时点记录，不代表当前仍在执行。

- Luban：执行 `dotnet Luban/Luban.dll -t server -c cs-bin -d bin --conf luban.conf -x outputCodeDir="D:/unitypro/LYFMMORGP/_temp/npc-info-20260905/Code" -x outputDataDir="D:/unitypro/LYFMMORGP/_temp/npc-info-20260905/Data"`，成功生成新 `NpcInfo`、`Tables`、`ShopTable`、`ShopItemInfo` 与 13 个二进制表。
- 同步校验：正式 `NpcInfo.cs` 和 `tbnpcinfo.bytes` 的 SHA256 与 `_temp/npc-info-20260905` 对应生成产物一致。
- 结构检查：服务端搜索 `ShopInfo`、`TbShopInfo`、`ItemList`、`ShopItemList`、`NpcShopItem`、`ShopLimitType`、`GetShopInfoById`、`GetShopInfos` 为 0；`NpcParseInfo` 和 `NpcEntity` 各仅保留一个顶级类型。
- 审查修复：购买校验不再比较 `npcInfo.Type` 与 `ShopType.Npc`；改为校验 `NpcParseInfo.ShopId`、`ShopTable` 存在且 `ShopTable.ShopType == ShopType.Npc`。`NpcParseInfo.ParsePosition` 现在接受至少三段坐标，并将第 4 段朝向解析到 `RotationY`。
- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/npc-info-fix-20260905/build-bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/npc-info-fix-20260905/build-obj/"`。
- 构建结果：0 错误；保留 2 条既有警告，分别为 `LoginCtrl.cs` 的未等待异步调用与 `OnlineRole._roleBaseAttrEntity` 未使用。
- 未执行项：未启动四服务器和 Unity 客户端进行真实购买联调；客户端仍需同步本次构建的 `Network.dll`、`MMoRpgCommon.dll` 并应用 `RotationY`。
- 旧记录更正：此前关于 `ShopInfo/TbShopInfo`、NPC `ItemList` 和 “NPC 类型等于商店类型” 的描述已过期，以本记录为准。

## [VERIFY][网络][NPC][TR-227] 2026-09-05 NPC 客户端同步与最终复验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2043—2050 行；历史时点记录，不代表当前仍在执行。

- DLL 同步：将 `_temp/npc-info-fix-20260905/build-bin/Network.dll`、`MMoRpgCommon.dll` 同步至 `LYF_MMO/Assets/GM_Game/Plugins/Net/` 和 `Plugins/Common/`；同步前文件备份在 `_temp/npc-info-fix-20260905/client-backup/`，SHA256 与构建产物一致。
- 客户端修复：`NpcManager` 使用 `NpcParseInfo.RotationY` 应用配置的 Y 轴朝向；NPC 商城按 `NpcEntity.ShopId -> ShopTable -> ShopItemInfo` 生成商品；正式脚本无旧 `ShopInfo`、`ShopItemList`、`NpcShopData` 等引用。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/npc-info-fix-20260905/client-bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/npc-info-fix-20260905/client-obj/"`。
- 构建结果：0 错误，21 条既有 Unity/引用警告；`git diff --check` 通过。
- 审查结论：NPC 新字段、商店关联、坐标朝向解析、网络线程边界和中文注释通过；未执行真实四服务器联网联调。`ItemConfigBase.cs` 多类型文件为既有全局约束问题，未在本轮扩大范围。

## [VERIFY][网络][背包商城][TR-228] 2026-09-05 商城购买协议基线兼容性架构复核

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2051—2057 行；历史时点记录，不代表当前仍在执行。

- 类型：只读协议与链路审查，无业务代码修改、无构建产物。
- 检查：确认 `BuyShopItemReq` 保留用户字段 `shop_type=1`、`items=2`；当前追加 `npc_id=3`、`request_id=4`、`role_id=5`，响应追加 `request_id=2`，未重排字段。
- 发现：字段追加对 Protobuf wire 格式兼容，但 GameServer 当前把 `request_id`、`role_id` 校验为非零，只含基线字段的旧客户端会收到错误，不满足最小协议行为兼容。
- 结论：后端需明确采用扩展正式契约或缺省字段兼容方案；`npc_id` 是否可省略取决于商品能否唯一反查 NPC，不能绕过现有按 NPC 的限购键。验收用例和差异清单已写入 `项目分析/SHOP_BUY_PROTOCOL_DESIGN.md` 第 7 节。

## [VERIFY][NPC][TR-229] 2026-09-05 NPC 配置迁移客户端验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2058—2065 行；历史时点记录，不代表当前仍在执行。

- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/Manger/NpcManager.cs`。
- 静态检查：客户端商城相关脚本未引用旧 `ShopInfo`、`NpcShopItem`、`ShopItemList` 或 NPC `ItemList`；`NpcManager` 使用 `NpcParseInfo.ShopId`，并将 `RotationY` 应用于实例化 NPC 的 Y 轴旋转。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/npc-info-client-final-20260905/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/npc-info-client-final-20260905/obj/"`。
- 构建结果：成功，0 个错误，保留项目既有 21 条警告；构建输出均位于 `_temp/npc-info-client-final-20260905/`。
- 未执行项：未启动 Unity 场景验证 NPC 实例朝向，未进行 GateServer/GameServer/Unity 真实联网联调。

## [VERIFY][网络][TR-230] 2026-09-05 Center 回包网关错误码客户端提示验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2066—2074 行；历史时点记录，不代表当前仍在执行。

- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/Manger/NetErrorMsgMgr.cs`。
- 静态检查：`CMD_ErrCode` 对空数据和 protobuf 解析异常显示统一中文提示；`switch` 的 `default` 分支覆盖所有未显式映射的 `CmdCode`；商城 `BuyShopItemRes.Error` 保持由 `BuyItemWidget.OnBuyCompleted` 独立处理，未接入通用错误码路径。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/center-gate-forward-20260905/client-bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/center-gate-forward-20260905/client-obj/"`。
- 构建结果：成功，0 个错误，保留项目既有 21 条警告（Unity 引用冲突、弃用 API、未等待异步调用等）；构建输出位于 `_temp/center-gate-forward-20260905/`。
- 差异检查：`git diff --check -- LYF_MMO/Assets/GM_Game/Scripts/Manger/NetErrorMsgMgr.cs` 通过，仅报告项目既有 LF/CRLF 行尾提示。
- 未执行项：尚未启动 CenterServer、GameServer、GateServer 与 Unity 进行端到端错误码转发验证。

## [VERIFY][网络][TR-231] 2026-09-05 Center 回包网关转发后端验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2075—2108 行；历史时点记录，不代表当前仍在执行。

- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -t:Rebuild -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/center-gate-forward-20260905/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/center-gate-forward-20260905/obj/"`。
- 构建结果：成功，0 个错误；保留既有 `OnlineRole._roleBaseAttrEntity` 未使用警告。首次全量构建还显示 LoginServer 中未等待异步调用的既有警告，后续增量构建未重复输出该警告。
- 静态检查：`LoginGameServerHandler`、`CreateRoleHandler`、`StartGameHandler`、`RoleSkillInfoHandler`、`RoleKnapsackInfoHandler` 均调用 `GameCenterResponseForwarder.TryForwardToGate`；开始游戏在转发后才进行本服在线角色初始化。旧的 `RoleManager` 技能/背包中心回包直发方法已删除，避免绕开统一入口。
- 内部消息检查：`SyncKnapsackGridCountHandler` 只调用 `RoleManager.HandleCenterSyncKnapsackGridCount`，并有中文注释标记为持久化确认，未进入 `GameCenterResponseForwarder` 客户端回包白名单。
- 网关检查：`GateLoginCtrl`、`GateRoleCtrl` 的客户端回包均委派给 `GateClientResponseForwarder`；`SendData(package)` 和 `SendError(package, ...)` 均位于该统一入口。解析失败、未知回包和普通非成功 `CmdCode` 走 `CMD_ErrCode`；购买和扩容失败保留原业务回包，确保购买 `request_id` 和权威格子数不会丢失。
- 格式检查：`git diff --check -- LYF_Server/GameServer LYF_Server/GateServer` 通过，无空白错误；仅有工作区既有 LF/CRLF 行尾提示。
- 未执行项：未启动四服务器和 Unity 做真实网络收发验证；无法在静态构建中覆盖网关会话已断开、CenterServer 返回畸形 protobuf、CenterServer 返回业务失败和开始游戏本服初始化异常等运行时路径。

### 代码审查返工验证（2026-09-05）

- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -t:Rebuild -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/center-gate-forward-20260905/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/center-gate-forward-20260905/obj/"`。
- 构建结果：成功，0 个错误；保留既有 2 条警告：LoginServer 未等待异步调用、`OnlineRole._roleBaseAttrEntity` 未使用。构建产物仍位于 `_temp/center-gate-forward-20260905/`，未经产品经理确认不得清理。
- 路由元数据检查：`GameCenterResponseForwarder.cs` 不含 `NetDefine.CMD_*` 或协议 `switch`；全部 6 条 CenterServer 注册均在 `GameMessageProtocolRegister` 同一位置声明路由，5 条为 `ForwardToGate`，`CMD_SyncKnapsackGridCountCode` 明确为 `Internal`。
- 网关错误顺序检查：失败扩容分支调用 `session.SendData(package)` 后调用 `session.SendError(CreateErrorPackage(package), resultCode)`；错误包为独立 `BasePackage`，不会篡改原扩容回包。购买失败只保留 `BuyShopItemRes`，不触发第二条通用错误消息。
- 格式检查：`git diff --check -- LYF_Server/GameServer LYF_Server/GateServer Docs/AI` 通过，无空白错误；仅输出既有 LF/CRLF 行尾提示。
- 未执行项：未启动真实四服务器/Unity 网络环境，尚未观测 Unity 实际接收“失败扩容原包后再接收 CMD_ErrCode”的消息顺序。

### Center 回包前置转发验证（2026-09-05）

- 静态流程检查：`ClientMessageManager.ProcessCommand` 在 `registration.Handler.Handle(context)` 前，对 `GameCommandSource.CenterServer + CenterMessageRoute.ForwardToGate` 唯一调用 `GameCenterResponseForwarder.TryForwardToGate(context)`；所有登录、创建角色、开始游戏、技能、背包和商城 Center Handler 均不再直接调用转发器。
- 注册检查：`GameMessageProtocolRegister` 的 Center 回包继续以元数据声明 6 条路由，其中登录、创建角色、开始游戏、技能、背包、商城购买为 `ForwardToGate`，`CMD_SyncKnapsackGridCountCode` 明确为 `Internal`，不会进入前置转发分支。
- 会话安全检查：`GameCenterResponseForwarder` 找不到 `GateSession` 时记录错误；`StartGameHandler` 在该会话不存在时拒绝创建在线角色，但前置转发已经先尝试执行，任何后续本地解析异常不会影响有效会话的回包。
- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -t:Rebuild -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/center-gate-forward-20260905/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/center-gate-forward-20260905/obj/"`。
- 构建结果：失败，11 个错误均来自未纳入本任务范围的 `LYF_Server/GameServer/Service/shop/ShopStatic.cs:25`，其中 `const ShopType GetType() const` 是 C++ 语法，当前 C# 编译器无法解析；另有既有 LoginServer 异步未等待警告。未修改该工作区文件，待其负责人修复后重建。
- 格式检查：`git diff --check -- LYF_Server/GameServer LYF_Server/GateServer Docs/AI` 通过，无新增空白错误；仅有工作区既有 LF/CRLF 行尾提示。

### 代码审查复验（2026-09-05）

- 审查结论：通过，未发现阻塞问题。`GameCenterResponseForwarder` 已只依赖 `GameMessageProtocolRegister` 中的 `CenterMessageRoute` 元数据，不再维护固定协议白名单；内部持久化确认明确为 `Internal`。
- 会话和错误路径：GameServer 使用 `GateSessionId` 定位目标 Gate，会保留原 `UnitySessionId` 供 Gate 定位客户端；扩容失败先发送 `OpenKnapsackGridRet` 再以独立包发送 `CMD_ErrCode`，商城失败保持 `BuyShopItemRes` 的 `request_id` 与 `ErrorInfo` 专用提示，避免重复提示。
- 残余风险：服务端返工后的完整重建成功；客户端构建成功。未执行真实四服务器与 Unity 的联网时序验证。

## [VERIFY][综合][TR-232] 2026-09-05 Center -> Game -> Gate -> Unity 独立测试复验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2109—2118 行；历史时点记录，不代表当前仍在执行。

- 测试范围：只读核查 `GameCenterResponseForwarder`、五个 Center 回包处理器、`GateClientResponseForwarder`、网关注册和 `NetErrorMsgMgr`；未修改业务代码。
- 服务端构建：执行 `dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/center-gate-forward-20260905/test-server-bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/center-gate-forward-20260905/test-server-obj/"`，0 错误，保留既有 2 条警告：`LoginCtrl.cs` 未等待异步调用、`OnlineRole._roleBaseAttrEntity` 未使用。
- 客户端构建：执行 `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/center-gate-forward-20260905/test-client-bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/center-gate-forward-20260905/test-client-obj/"`，0 错误，保留项目既有 21 条 Unity 引用冲突、弃用 API 和未等待异步调用警告。
- 路由核查：登录游戏服、创建角色、开始游戏、角色技能、角色背包五个 Center 客户端回包均同时存在于 GameServer 客户端可见白名单、Center 网络注册和 Gate 客户端注册；处理器在 GameServer 解析或开始游戏初始化前调用统一转发入口。背包持久化确认 `CMD_SyncKnapsackGridCountCode` 未误转发给 Unity。
- 错误与业务回包核查：Gate 的九类已注册客户端回包均有协议解析分支；解析失败、未知回包和普通非成功 `CmdCode` 使用 `CMD_ErrCode`。商城失败保留 `BuyShopItemRes.request_id`，扩容失败保留 `OpenKnapsackGridRet.CurrentOpenedGridCount`，不会被通用错误包替换；客户端 `NetErrorMsgMgr` 对空包、解析异常和未知错误码均显示中文 Tips。
- 格式检查：`git diff --check` 针对本轮相关文件无空白错误，仅显示工作区既有 LF/CRLF 行尾提示。
- 未联调风险：尚未实际启动 CenterServer、GameServer、GateServer 和 Unity；未覆盖会话已断开、Center 回包畸形、普通失败错误码和开始游戏本服初始化异常时的端到端发送行为。

## [VERIFY][综合][TR-233] 2026-09-05 BuyDialog 数量购买弹窗验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2119—2133 行；历史时点记录，不代表当前仍在执行。

- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/UI/Tips/BuyDialog.cs`、`Manger/TipsMgr.cs`、`UI/Main/Window/Widget/BuyItemWidget.cs`、`GoodsItemWidget.cs`、`Prefabs/UIPrefabs/TipsDiialog/BuyDialog.prefab`。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/buy-dialog-20260905/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/buy-dialog-20260905/obj/"`。
- 构建结果：0 个错误，保留项目既有 Unity 引用冲突、弃用 API、异步调用等警告；构建产物均位于 `_temp/buy-dialog-20260905/`。
- 静态检查：`git diff --check` 通过；预制体字段已绑定商品图标、名称、货币图标、货币总价、数量输入框和加减按钮。
- 未执行项：未在 Unity 运行场景中实际点击加减/输入数量，也未进行真实商城联网购买和限购边界联调。

### 货币类别显示复核（2026-09-05）

- `BuyItemWidget` 和 `GoodsItemWidget` 传入的货币文本现在包含货币类型与货币名称；弹窗显示格式为“货币类型 X · 名称 总价”。
- 复核构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/buy-dialog-final-20260905/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/buy-dialog-final-20260905/obj/"`，0 错误，21 条既有警告。
- 未执行：Unity 场景实际点击、限购边界和真实商城联网验证。
- 兼容性备注：当前 NPC 商城窗口实际使用 `BuyItemWidget` 提交购买；旧 `GoodsItemWidget` 预制体仍保留原入口，未接入角色/NPC购买上下文，后续复用该预制体前需要补齐购买请求回调。

## [VERIFY][综合][TR-234] 2026-09-05 BuyDialog 代码审查返工验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2134—2142 行；历史时点记录，不代表当前仍在执行。

- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/UI/Tips/BuyDialog.cs`、`Manger/TipsMgr.cs`、`UI/Main/Window/Widget/BuyItemWidget.cs`、`GoodsItemWidget.cs`。
- 静态证据：`GoodsItemWidget` 仅被 `Assets/GM_Game/Prefabs/UIPrefabs/GoodsItemWidget.prefab` 的按钮事件引用，当前 NPC 商城窗口实际实例化 `BuyItemWidget`；旧组件已提供带 `npcId/shopType` 的刷新重载，缺少上下文会安全禁用购买。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/buy-dialog-review-20260905/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/buy-dialog-review-20260905/obj/"`。
- 构建结果：0 个错误，保留项目既有 17 条 Unity 引用冲突、弃用 API、异步调用和字段未使用警告；产物均位于 `_temp/buy-dialog-review-20260905/`。
- Unity MCP 校验：四个客户端脚本均 0 错误、0 警告；Unity 控制台读取到 0 条错误。
- 未执行项：未在 Unity 场景实际点击加减/确认，未进行限购边界和真实商城联网验证。

## [VERIFY][背包商城][TR-235] 2026-09-05 ShopMgr 商城购买回包职责集中

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2143—2151 行；历史时点记录，不代表当前仍在执行。

- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/Manger/ShopMgr.cs`、`NetSocketMgr.cs`、`UI/Main/Window/Widget/BuyItemWidget.cs`、`GoodsItemWidget.cs`、`LYF_MMO/Assets/GM_Game/Plugins/Net/Network.dll`。
- 协议同步：客户端 DLL 使用 `_temp/shop-purchase-limit-20260905/server-bin/Network.dll`，包含 `BuyShopItemRes.RemainingPurchaseCount`；旧 DLL 备份于 `_temp/shop-manager-client-20260905/original/Network.dll`。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/shop-manager-client-20260905/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/shop-manager-client-20260905/obj/"`。
- 构建结果：0 个错误，保留项目既有 17 条 Unity 引用冲突、弃用 API、异步调用和字段未使用警告。
- 格式检查：针对本轮客户端脚本执行 `git diff --check`，通过；仅有既有 LF/CRLF 行尾转换提示。
- 未执行项：未进行 Unity 场景实际点击、超时/断线运行验证和真实商城联网联调。

## [VERIFY][综合][TR-236] 2026-09-05 BuyDialog 代码审查局部返工

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2152—2159 行；历史时点记录，不代表当前仍在执行。

- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/UI/Main/Window/Widget/BuyItemWidget.cs`、`GoodsItemWidget.cs`。
- 修改内容：商品卡片显示货币类别与名称；空商品图标路径不再触发异步加载；购买确认、等待状态和回包处理补充中文注释。
- 构建验证：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/buy-dialog-review-fix-20260905/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/buy-dialog-review-fix-20260905/obj/"`，0 个错误，保留 21 条项目既有警告。
- 格式验证：针对本轮文件执行 `git diff --check`，通过；仅有既有行尾转换提示。
- 未执行项：未进行 Unity 场景真实点击、限购边界和商城联网验证。

## [VERIFY][综合][TR-237] 2026-09-05 BuyDialog 最终复验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2160—2167 行；历史时点记录，不代表当前仍在执行。

- 最终构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/buy-dialog-final-review-20260905/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/buy-dialog-final-review-20260905/obj/"`，0 个错误，21 条项目既有警告。
- Unity MCP 脚本校验：`BuyDialog.cs`、`TipsMgr.cs`、`BuyItemWidget.cs`、`GoodsItemWidget.cs` 均为 0 错误、0 警告；Unity 控制台读取到 0 条错误/警告。
- 静态检查：`git diff --check` 通过；BuyDialog 预制体商品图标、商品名称、货币图标、货币总价、数量输入框、加减按钮引用均非空。
- 复验结论：数量变化会实时刷新单价乘数量的总价，确认时提交实际数量；货币类别和名称独立传入并显示；异步图标加载受绑定版本和路径保护。
- 未执行项：未进行 Unity 场景真实点击、限购边界和四端真实商城联网验证。

## [VERIFY][网络][背包商城][TR-238] 2026-09-05 商城购买响应剩余限购数量协议

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2168—2177 行；历史时点记录，不代表当前仍在执行。

- 修改文件：`LYF_Server/ProtoBuf/proto/ResultEntity.proto`、`LYF_Server/Network/Proto/ResultEntity.cs`。
- 协议检查：`BuyShopItemRes` 原有字段编号未改变，新增 `uint32 remaining_purchase_count = 3`；protoc 生成代码包含属性、序列化、反序列化、复制、比较和反射描述信息。
- 生成命令：使用 `LYF_Server/ProtoBuf/bin/protoc.exe`，输出目录为 `D:/unitypro/LYFMMORGP/_temp/shop-purchase-limit-20260905/proto/`；正式生成代码同步至 `LYF_Server/Network/Proto/ResultEntity.cs`。
- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/shop-purchase-limit-20260905/server-bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/shop-purchase-limit-20260905/server-obj/"`。
- 构建结果：0 个错误，保留 2 条既有警告：`OnlineRole._roleBaseAttrEntity` 未使用、`LoginCtrl.cs` 异步调用未等待。
- 业务检查：`RequestBuyShopItem` 当前所有合法购买仍返回 `FeatureNotReady`，未填写剩余限购数量，未伪造购买成功或扣费；待权威事务接入后再填充该字段。
- 未执行项：未进行 CenterServer/GameServer/GateServer/Unity 真实购买联调；客户端 `Network.dll` 需由客户端 Agent 同步并验证。

## [VERIFY][背包商城][TR-239] 2026-09-05 商城购买客户端回调职责收窄验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2178—2191 行；历史时点记录，不代表当前仍在执行。

- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/Manger/ShopMgr.cs`、`UI/Main/Window/Widget/BuyItemWidget.cs`、`GoodsItemWidget.cs`。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/shop-manager-ui-refactor-20260905/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/shop-manager-ui-refactor-20260905/obj/"`。
- 构建结果：0 个错误，保留项目既有 21 条 Unity 引用冲突、弃用 API、异步调用和字段未使用警告；产物均位于 `_temp/shop-manager-ui-refactor-20260905/`。
- 静态检查：`git diff --check` 通过；客户端商品组件未再声明 `BuyShopItemRes`、`CmdCode` 或成功/失败回调参数，`ShopMgr` 保留超时、解析异常和断线清理。
- 未执行项：未进行 Unity 场景点击、真实商城联网和服务端购买记录/DB 持久化联调。

### 限购为零静态复验

- 有限购商品的初始数量或服务端刷新数量为 `0` 时，购买按钮不可交互，旧 `GoodsItemWidget` 入口也会被拦截。
- 不限购商品通过 `PurchaseLimitType == 0` 分支保持可购买，不会因 `RemainingPurchaseCount` 默认值为 `0` 被误禁用。
- 最新构建结果：0 个错误，17 个项目既有警告；`git diff --check` 通过（仅有既有行尾转换提示）。

## [VERIFY][背包商城][TR-240] 2026-09-05 商城购买请求 ShopId 客户端验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2192—2209 行；历史时点记录，不代表当前仍在执行。

- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/Manger/ShopMgr.cs`、`UI/Main/Window/NpcShopWindow.cs`、`UI/Main/Window/Widget/BuyItemWidget.cs`、`GoodsItemWidget.cs`、`Plugins/Net/Network.dll`。
- 协议 DLL：客户端插件已备份至 `_temp/shop-id-client-20260905/original/Network.dll`，替换为 `_temp/shop-id-protocol-20260905/final-bin/Network.dll`，两者 SHA256 不同；新 DLL 提供 `BuyShopItemReq.ShopId`。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/shop-id-client-20260905/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/shop-id-client-20260905/obj/"`。
- 构建结果：0 个错误；保留项目既有 Unity 引用冲突、弃用 API、异步调用和字段未使用警告；产物与构建日志位于 `_temp/shop-id-client-20260905/`。
- 静态检查：`NpcShopWindow` 只将 `ShopTable.Id` 传给 `BuyItemWidget`；两个商品组件和 `ShopMgr` 不再引用 `shop_type`/`ShopType` 作为购买请求字段；`git diff --check` 通过，仅有既有行尾转换提示。
- 字段映射复核：`ShopMgr` 顶层 `BuyShopItemReq.ShopId = ShopTable.Id`，明细 `BuyShopItem.ShopId = ShopItemInfo.Id`；商品 ID 未被误写为顶层商店 ID。
- 未执行项：未进行 Unity 场景点击和四端真实商城联网验证。

### 2026-09-05 Network.dll 最终同步验证

- 源文件：`_temp/shop-id-final/server-bin/Network.dll`。
- 目标文件：`LYF_MMO/Assets/GM_Game/Plugins/Net/Network.dll`。
- 备份文件：`_temp/shop-id-final/client-original/Network.dll`。
- SHA256：服务端源文件与客户端目标文件均为 `740018052BEF5B2337744622C0D531E1E9F8A7084E48A5451E7B47EE76F708AC`；备份旧 DLL 的 SHA256 为 `BE994675F4C7E35F4DADE6AE719A4242589CCF3C286FF9E3D06ABD1559F5D0CC`。
- 结论：协议插件同步成功；本轮未修改业务代码，未重复执行客户端构建。

## [VERIFY][背包商城][TR-241] 2026-09-05 商城购买中心服落库骨架验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2210—2218 行；历史时点记录，不代表当前仍在执行。

- `DBMgr.InitTables` 已包含 `RoleNpcShopPurchaseTable`、`RoleShopPurchaseRequestTable`；CenterServer 与 GameServer 工程清单已同步。
- `RequestBuyShopItem` 校验后转发 CenterServer；CenterServer 注册和处理 `CMD_BuyShopItemCode`；GameServer 使用独立结果处理器向 GateServer 转发 `BuyShopItemRes`。
- CenterServer 当前固定返回 `FeatureNotReady`，未调用限购写入，未实现货币扣除和物品发放前不会构造 `Succeed`。
- `RecordCommittedNpcShopPurchase` 使用 `BeginTran(IsolationLevel.Serializable)`；幂等键为 `(RoleID, RequestID)`，同指纹复用结果、不同指纹返回 `DuplicateRequest`；每日计数跨日重置，永久计数不重置，成功响应计算剩余限购数量。
- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/shop-purchase-persistence-20260905/server-bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/shop-purchase-persistence-20260905/server-obj/"`。
- 构建结果：0 个错误、0 个新增警告；未启动数据库和四端进行真实购买联调，货币/物品权威事务仍是阻塞项。

## [VERIFY][背包商城][构建][TR-242] 2026-09-05 商城购买最终构建与静态验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2219—2227 行；历史时点记录，不代表当前仍在执行。

- 客户端命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/shop-manager-final-20260905/client-bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/shop-manager-final-20260905/client-obj/"`。
- 客户端结果：0 个错误，保留 21 条项目既有 Unity 引用冲突、弃用 API 和异步调用警告。
- 服务端命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/shop-manager-final-20260905/server-bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/shop-manager-final-20260905/server-obj/"`。
- 服务端结果：0 个错误，保留 2 条既有警告；数据库初始化、Center/Game 回包路由和商城幂等落库骨架均已编译。
- 静态结果：商品 UI 未引用 `BuyShopItemRes` 或 `CmdCode`；`ShopMgr` 是购买结果唯一处理入口；相关文件 `git diff --check` 无空白错误（仅有既有行尾转换提示）。
- 未执行：未启动 MySQL 和四端服务进行真实购买；无法验证真实扣费、发货、事务回滚、并发限购及断线重试。

## [VERIFY][网络][背包商城][TR-243] 2026-09-05 商城协议字段精简验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2228—2236 行；历史时点记录，不代表当前仍在执行。

- 协议源文件删除 `BuyShopItemReq.npc_id`、`BuyShopItemReq.request_id` 和 `BuyShopItemRes.request_id`，使用项目 protoc 重新生成 `Network/Proto/RequestEntity.cs`、`ResultEntity.cs`。
- 删除 `RoleShopPurchaseRequestTable`、数据库初始化引用及服务端幂等请求缓存；`ShopMgr` 不再生成或匹配请求号。
- 服务端构建：`LYF_Server.sln` 0 个错误，保留 2 条既有警告。
- 客户端构建：`GM_Game.csproj` 0 个错误，保留 21 条项目既有警告。
- 静态检查：正式商城协议和处理器中已无 `request_id`、`npc_id` 引用；相关文件 `git diff --check` 无空白错误，仅有既有行尾转换提示。
- 未执行：未进行 Unity 场景点击、MySQL 实际写入和四端联网购买验证。

## [VERIFY][背包商城][TR-244] 2026-09-05 BuyShopItemReq 顶层 shop_id 验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2237—2243 行；历史时点记录，不代表当前仍在执行。

- `RequestEntity.proto` 顶层请求字段已改为 `shop_id = 1`，生成属性为 `BuyShopItemReq.ShopId`；商品明细 `BuyShopItem.ShopId` 未修改。
- 正式服务端源码已无 `request.ShopType` 或 `shop_type` 的商城请求引用；GameServer 按 `ShopTable.Id` 定位配置并从配置读取商店类型。CenterServer 和 GateServer 链路未发现失效字段引用。
- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -t:Rebuild -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/shop-id-protocol-20260905/server-bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/shop-id-protocol-20260905/server-obj/"`。
- 构建结果：0 个错误，保留 2 条既有警告；未进行 Unity 客户端协议 DLL 同步和四端真实购买联调。

## [VERIFY][背包商城][TR-245] 2026-09-05 BuyShopItemReq shop_id 服务端复验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2244—2253 行；历史时点记录，不代表当前仍在执行。

- 协议源和生成代码：`BuyShopItemReq.shop_id = 1` 与正式 `RequestEntity.cs` 的 `ShopId` 属性一致；使用 protoc 重新生成到 `_temp/shop-id-protocol-20260905/proto/` 后，与正式 `RequestEntity.cs`、`ResultEntity.cs` 内容一致。
- 服务端清理：`ShopType` 注释改为说明配置类型；`ShopManager` 局部参数改为 `shopType`；服务端无 `shop_type` 残留，未恢复 `npc_id` 或 `request_id`。
- 静态检查：GameServer 使用 `request.ShopId` 查询 `ShopTable.Id`；商品明细使用 `BuyShopItem.ShopId` 校验归属；CenterServer/GateServer 未发现旧字段引用。
- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/shop-id-protocol-20260905/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/shop-id-protocol-20260905/obj/"`。
- 构建结果：0 个错误，保留项目既有 2 条警告：`LoginCtrl.cs` 异步调用未等待、`OnlineRole._roleBaseAttrEntity` 未使用。
- 未执行：客户端 `Network.dll` 同步、Unity 实际购买操作和四端真实商城联网联调。
- 最终复核：`BuyShopItemReq` 使用 `ShopId`；正式源码未发现 `request.ShopType`、顶层 `shop_type`、`request_id` 或 `npc_id` 的商城协议依赖。最终 `LYF_Server.sln` 重建 0 错误、2 条既有警告；未同步 Unity 客户端 DLL，未进行四端真实联调。

## [VERIFY][网络][背包商城][TR-246] 2026-09-05 shop_id 客户端同步最终验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2254—2262 行；历史时点记录，不代表当前仍在执行。

- 服务端构建：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -t:Rebuild -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/shop-id-final/server-bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/shop-id-final/server-obj/"`，0 个错误，保留 2 条既有警告。
- 客户端构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/shop-id-final/client-bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/shop-id-final/client-obj/"`，0 个错误，保留 21 条既有警告。
- 协议映射检查：`BuyShopItemReq.ShopId` 使用 `ShopTable.Id`，`BuyShopItem.ShopId` 使用 `ShopItemInfo.Id`；GameServer 按顶层商店 ID 校验商品归属，客户端 DLL 已同步。
- 字段残留检查：正式协议、商城处理器和购买 UI 中无 `shop_type`、`request_id`、`npc_id`；`ShopType` 只作为配置读取后的内部枚举。
- `git diff --check`：本轮相关文件无新增空白错误；工作区另有既有 `LubanMgr.cs` 行尾/空白提示，未改动。
- 未执行：Unity 场景实际点击、数据库写入、四端真实网络购买和并发限购验证。

## [VERIFY][综合][TR-247] 2026-09-05 Center -> Game -> Gate 前置转发测试复验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2263—2272 行；历史时点记录，不代表当前仍在执行。

- 测试类型：只读静态链路与构建验证；未修改任何客户端或服务端业务代码。
- 逻辑线程与顺序：`ClientMessageManager.ProcessCommand` 在从注册表取到 Center 消息且路由为 `ForwardToGate` 后，先调用 `GameCenterResponseForwarder.TryForwardToGate(context)`，再调用 `registration.Handler.Handle(context)`。转发器使用 `context.GateSession.SendData(context.Package)`，保留原 `UnitySessionId`、`GateSessionId`、协议号和原始载荷。
- 注册矩阵：`CMD_LoginGameServerCode`、`CMD_CreateRoleCode`、`CMD_StartGameCode`、`CMD_RoleSkillInfoCode`、`CMD_RoleKnapsackInfoCode`、`CMD_BuyShopItemCode` 均为 `ForwardToGate`；`CMD_SyncKnapsackGridCountCode` 为 `Internal`。所有可见项均已在 Gate `client.RegistCommand` 登记。
- 重复与拦截检查：`GameCenterResponseForwarder` 是 MessageHandler 目录中唯一的 Center 回包统一发送点；登录、角色、背包和商城的 Center Handler 均不再发送原包。`SyncKnapsackGridCountHandler` 仅解析并交给 `RoleManager.HandleCenterSyncKnapsackGridCount`，没有转发调用。
- 错误码检查：`GateClientResponseForwarder` 对 protobuf 解析失败、未知协议和普通非成功结果调用 `Session.SendError(..., CmdCode)`；`NetErrorMsgMgr` 已注册 `CMD_ErrCode`，空包、解析异常和未映射错误码都会调用中文 Tips。商城和扩容失败保留专用回包；扩容额外发送通用错误码。
- 中文注释与类型文件：本轮新增/关键入口均含中文注释；`GameCenterResponseForwarder`、`GateClientResponseForwarder`、`CenterMessageRoute`、`GameMessageRegistration` 各自独立文件；`git diff --check` 未报告空白错误，仅有既有 CRLF 提示。
- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug -t:Rebuild -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/center-gate-forwarding-test-20260905/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/center-gate-forwarding-test-20260905/obj/"`。
- 构建结果：失败，11 个错误均由 `LYF_Server/GameServer/Service/shop/ShopStatic.cs:34` 的 C++ 风格 `const ShopType GetType() const` 引起；该文件不在本测试范围，未修改。`MMoRpgCommon`、`Network`、`GateServer`、`CenterServer`、`LoginServer` 已成功构建；保留既有 `LoginCtrl.cs:129` 未等待异步调用警告。未执行真实四端联网验证。

## [VERIFY][网络][背包商城][TR-248] 2026-09-05 角色商城购买记录登录同步

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2273—2320 行；历史时点记录，不代表当前仍在执行。

- 静态检查：`ResultEntity.proto` 仅保留独立 `RoleShopPurchaseInfoRet`；`RoleShopPurchaseInfo` 不含 `npc_id`，字段号 2 和旧名称均 reserved；`LoginModle` 和 `StartGameRet` 无 `RoleShopPurchaseInfo` 残留。
- Center→Game→Gate：Center 在 `CMD_EnterWroldCode` 处理中查询 `RoleShopPurchaseTable`，发送 `CMD_RoleShopPurchaseInfoCode`；GameServer 注册 `ForwardToGate` 和 `RoleShopPurchaseInfoHandler`；Gate 注册协议并由 `GateClientResponseForwarder` 验证后透传。
- 组件：`RoleShopPurchaseComponent` 为在线角色保存深拷贝后的购买记录；组件独立文件并在 `OnlineRole.InitSerializes` 注册。
- 协议生成：使用 `LYF_Server/ProtoBuf/bin/protoc.exe` 生成 `Network/Proto/ResultEntity.cs`，产物暂存于 `_temp/role-shop-login-proto-20260905/`。
- 构建：`Network.csproj`、`CenterServer.csproj`、`GateServer.csproj` 均 0 警告、0 错误，产物位于 `_temp/role-shop-login-build-20260905/`。
- 完整方案：`LYF_Server.sln` 失败于无关 `GameServer/Service/shop/ShopStatic.cs:80` 的未定义 `ItemDesc`、`item_info`、`item` 符号（4 个错误）；另有既有警告，未修改该文件。
- 未执行：客户端 DLL 同步、Unity 实际登录和四端联网验证。

### 审查返工复验

- 静态检查：商城记录 Handler 对 `Package.Data == null` 或 `Length == 0` 直接返回；只有成功解析且 `CmdCode.Succeed` 时才替换 `RoleShopPurchaseComponent` 数据。
- Gate 空包检查：`TryReadResultCode` 统一拒绝 `null`/零长度载荷，进入错误包分支，不向 Unity 发送空业务包。
- Center 异常隔离：`GetRoleShopPurchaseInfo` 捕获数据库查询和映射异常，清空部分结果并返回 `ServerError`；技能、背包回包已在此前发送，不受影响。
- 本轮未修改 `ShopStatic.cs`，完整方案构建阻塞原因保持不变；需在其修复后重跑完整构建。
- 独立构建：Network、CenterServer、GateServer 均 0 警告、0 错误；完整方案仍只受无关 `ShopStatic.cs` 未定义符号阻断。

### 客户端接入验证（2026-09-05）

- 协议 DLL：`dotnet build LYF_Server/Network/Network.csproj --no-restore --configuration Debug`，0 个警告、0 个错误；确认包含 `RoleShopPurchaseInfoRet`，记录无 `npc_id`。
- DLL 同步：服务端产物 `_temp/role-shop-client-protocol-20260905/bin/Network.dll` 已复制到 `LYF_MMO/Assets/GM_Game/Plugins/Net/Network.dll`，旧 DLL 备份于同目录 `original/`。
- 客户端构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/role-shop-client-build-20260905/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/role-shop-client-build-20260905/obj/"`，0 个错误，保留 21 条项目既有警告。
- 静态检查：`ShopMgr` 随网络模块提前注册登录回包，保存记录并为两个商品组件提供剩余限购查询；`MainCtrl` 不承载购买记录业务；`git diff --check` 无新增空白错误。
- 未执行：Unity 实际登录场景及 Center/Game/Gate/Unity 四端联网联调。

### 空商城回包防护复验（2026-09-05）

- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/Manger/ShopMgr.cs`。
- 静态检查：解析前检查 `ByteString == null` 或 `Length == 0`，直接返回并保留已有商城购买快照；`MainCtrl` 未改动。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/role-shop-client-empty-packet-20260905/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/role-shop-client-empty-packet-20260905/obj/"`。
- 构建结果：0 个错误，保留 21 条项目既有 Unity 引用冲突、弃用 API 和未使用字段警告。
- `git diff --check`：本轮相关文件无新增空白错误；未执行 Unity 实际登录和四端联网验证。

### GameServer 单项目构建复验（2026-09-05）

- 构建命令：`dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/role-shop-final-20260905/gameserver-bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/role-shop-final-20260905/gameserver-obj/"`。
- 结果：被既有 `LYF_Server/GameServer/Service/shop/ShopStatic.cs:80` 的 `ItemDesc` 调用错误阻断（1 个错误，另有该文件既有警告和 `OnlineRole` 未使用字段警告）；本任务未修改该文件。
- 本轮商城协议相关的 Network、CenterServer、GateServer 和客户端构建均已单独通过；GameServer 完整编译需先修复上述无关文件。
### 角色商城购买记录登录同步最终复验（2026-09-05）

- 协议静态检查：`RoleShopPurchaseInfoRet` 为独立登录回包；记录字段包含 `role_id`、`shop_id`、每日/累计购买次数及时间，`npc_id` 与 `request_id` 均无正式引用。
- 链路静态检查：Center 查询 `RoleShopPurchaseTable`，GameServer `RoleShopPurchaseInfoHandler` 写入 `RoleShopPurchaseComponent`，统一转发器在 Handler 前发送 Gate，Gate 校验后交给 Unity；客户端 `ShopMgr` 在网络初始化阶段注册并保存快照。
- 独立构建：`Network.csproj`、`CenterServer.csproj`、`GateServer.csproj` 和 `GM_Game.csproj` 均构建通过；服务端 0 警告/0 错误，客户端 0 错误并保留 21 条既有警告。
- 协议 DLL：客户端插件与 `_temp/role-shop-client-protocol-20260905/bin/Network.dll` SHA256 一致；最终客户端构建产物位于 `_temp/role-shop-client-empty-packet-20260905/`。
- 返工验证：GameServer Handler、Gate 回包校验和客户端 `ShopMgr` 均拒绝空数据；Center 商城查询捕获数据库/映射异常并返回 `ServerError`。
- 阻塞与风险：完整 `LYF_Server.sln` 仍被既有 `GameServer/Service/shop/ShopStatic.cs` 未定义符号阻断；未进行真实四端联网、数据库实写、断线和多 Gate 会话路由验证。
- 审查遗留：`RoleShopPurchaseInfoHandler` 目前通过 Center 查询条件和 Gate/Unity 会话双索引确定角色，尚未逐条校验记录内 `RoleId` 与在线角色 ID；该项需在多 Gate 联调前补充或由会话层统一保证。

## [VERIFY][背包商城][TR-249] 2026-09-05 RoItemContainer 容器子类转换验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2321—2326 行；历史时点记录，不代表当前仍在执行。

- 静态检查：`git diff --check -- GameServer/Service/item/RoItemContainer.cs` 通过。
- 构建命令：`dotnet build GameServer/GameServer.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/ro-item-container-20260905/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/ro-item-container-20260905/obj/"`。
- 构建结果：MMoRpgCommon、Network 编译成功；GameServer 失败 1 个既有错误：`BagTransition.cs` 引用不存在的 `RoleKnapsackComponent`。本次 `RoItemContainer.cs` 与 `RoItemComponent.cs` 未报告编译错误；保留既有 `ShopStatic.GetType()` 警告。
- 未执行：真实角色物品加载、UID 唯一性、负重规则和四端联调，等待权威物品模型接入。

## [VERIFY][背包商城][TR-250] 2026-09-05 背包子类语义重命名验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2327—2332 行；历史时点记录，不代表当前仍在执行。

- 静态检查：`git diff --check -- GameServer/Service/item/RoItemContainer.cs` 通过。
- 构建命令：`dotnet build GameServer/GameServer.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/ro-item-container-rename-20260905/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/ro-item-container-rename-20260905/obj/"`。
- 构建结果：公共库和 Network 编译成功；GameServer 仍有 1 个既有错误：`BagTransition.cs(12)` 找不到 `RoleKnapsackComponent`。新增类名未产生编译错误；保留既有 `ItemDesc.ToString()`、`ShopStatic.GetType()` 警告。

## [VERIFY][背包商城][界面][TR-251] 2026-09-05 数据库 ItemID 更名为 ItemUID 后端修复验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2333—2341 行；历史时点记录，不代表当前仍在执行。

- 根因：`ItemTable`、`EquipTable` 已改为 `uint ItemUID`，但 `EquipXLGeneTable` 仍保留 `int ItemID`，`CentRoleModel` 的装备/词条索引仍声明为 `Dictionary<int, ...>` 并向已重命名的协议属性 `ItemId` 写值，导致 CenterServer 出现 UID 类型错误和协议属性错误。
- 修改范围：`LYF_Server/CenterServer/DB/Table/ItemTable.cs`、`EquipTable.cs`、`EquipXLGeneTable.cs`、`DB/Modle/CentRoleModel.cs`。
- 静态检查：上述三张表与 CenterServer 查询映射不再有旧 `ItemID` 实例字段；物品、装备、词条均按 `ItemUID` 关联；`ItemTypeID` 与 Luban/商城配置 ID 未修改；`git diff --check` 通过，仅输出既有 CRLF 转换提示。
- 构建：`MMoRpgCommon.csproj` 0 错误、1 条既有 `ItemDesc.ToString` 警告；`Network.csproj` 0 错误、1 条同源既有警告；`CenterServer.csproj` 0 错误、1 条同源既有警告；`GameServer.csproj` 0 错误、4 条既有警告（`ItemDesc.ToString`、`ShopStatic.GetType`、未赋值字段、未使用字段）。
- 产物：`D:/unitypro/LYFMMORGP/_temp/item-uid-fix-20260905/`，未经产品经理确认不得清理。
- 未执行：未连接实际数据库验证列迁移后的历史数据读取，也未执行 Unity 客户端登录联调。

## [VERIFY][背包商城][界面][TR-252] 2026-09-05 ItemUID 数据库类型返工验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2342—2351 行；历史时点记录，不代表当前仍在执行。

- 审查结论：用户仅将数据库列名 `ItemID` 改为 `ItemUID`，未授权把 SQL 映射类型由 `int` 改为 `uint`；此前 `uint`、范围拒绝和强制转换均属于不必要的行为变更。
- 修复：`ItemTable`、`EquipTable`、`EquipXLGeneTable` 的 `ItemUID` 均为 `int`；`CentRoleModel` 的装备与词条索引恢复为 `Dictionary<int, ...>`，回包直接写入 `ItemId = ItemUID`。
- 保留：三表继续以 `ItemUID` 关联，背包 protobuf 保持 `item_id`，未修改 `ItemTypeID`、Luban 或商城配置 `ItemID`。
- 静态检查：CenterServer 中未发现 `uint ItemUID`、`Dictionary<uint, ...>`、`Int32.MaxValue` UID 拒绝或 `(int)ItemUID` 强制转换；`git diff --check` 通过，仅输出既有 CRLF 转换提示。
- 构建：`CenterServer.csproj` 0 错误、1 条既有 `ItemDesc.ToString` 警告；`GameServer.csproj` 0 错误、4 条既有警告（`ItemDesc.ToString`、`ShopStatic.GetType`、未赋值字段、未使用字段）。
- 产物：`D:/unitypro/LYFMMORGP/_temp/item-uid-int-revert-20260905/`，未经产品经理确认不得清理。
- 未执行：未连接实际数据库验证改名后的查询映射，也未执行 Unity 客户端登录联调。

## [VERIFY][网络][背包商城][界面][TR-253] 2026-09-05 ItemUID 数据库映射协议兼容返工

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2352—2360 行；历史时点记录，不代表当前仍在执行。

- 审查结论：用户只要求数据库持久化列从 `ItemID` 更名为 `ItemUID`，背包 protobuf 第 1 字段必须保持兼容名称 `item_id`，不能改为 `item_Uid`。
- 协议修复：`ResultEntity.proto` 的 `RoleItemInfo`、`RoleEquipItemInfo`、`RoleEquipGeneInfo` 均恢复 `int32 item_id = 1`；使用 `ProtoBuf/bin/protoc.exe` 重新生成 `Network/Proto/ResultEntity.cs`，生成属性均为 `ItemId`。
- 映射边界：CenterServer 保持 `ItemTable`、`EquipTable`、`EquipXLGeneTable` 的 `uint ItemUID` 和 `Dictionary<uint, ...>` 关联；仅在发出兼容协议时赋值 `ItemId = (int)ItemUID`。`ItemUID > Int32.MaxValue` 时返回 `ServerError`，避免截断；未修改 `ItemTypeID`、Luban 或商城配置 `ItemID`。
- 构建：`Network.csproj`、`CenterServer.csproj`、`GameServer.csproj` 均为 0 错误。Network/CenterServer 各保留 1 条既有 `ItemDesc.ToString` 警告；GameServer 保留 4 条既有警告（`ItemDesc.ToString`、`ShopStatic.GetType`、未赋值字段、未使用字段）。
- 产物：`D:/unitypro/LYFMMORGP/_temp/item-uid-proto-revert-20260905/`，未经产品经理确认不得清理。
- 未执行：未连接实际数据库验证列迁移后的历史数据读取，也未执行 Unity 客户端登录联调。

## [VERIFY][背包商城][界面][TR-254] 2026-09-05 ItemUID 数据库列重命名最终独立测试

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2361—2375 行；历史时点记录，不代表当前仍在执行。

- 测试范围：仅验证数据库实体 `ItemID` 改名为 `ItemUID` 的兼容返工；未修改客户端或服务端业务代码。
- 字段与关联检查：`CenterServer/DB/Table/ItemTable.cs:14`、`EquipTable.cs:13`、`EquipXLGeneTable.cs:13` 均为 `int ItemUID`。`CentRoleModel.cs:197` 和 `201` 分别以 `Dictionary<int, EquipTable>`、`Dictionary<int, EquipXLGeneTable>` 按 `ItemUID` 建索引；`CentRoleModel.cs:211`、`224`、`236` 保持写入协议属性 `ItemId`。
- 协议兼容检查：`ProtoBuf/proto/ResultEntity.proto:210`、`222`、`230` 均保持 `int32 item_id = 1`；生成代码 `Network/Proto/ResultEntity.cs:6944`、`7439`、`7764` 均保留 `ItemIdFieldNumber` 和 `ItemId` API。未发现 `item_uid`、`item_Uid` 或 `ItemUid` 残留。
- 配置 ID 边界检查：`ItemTable.ItemTypeID` 仍为配置 ID；商城解析 `Network/LubanManage/Parse/ShopItemParseInfo.cs:24` 仍使用配置 `ItemID`，未将 Luban 或商城商品配置 ID 改成实例 UID。
- 空白检查：`git diff --check -- CenterServer/DB/Table/ItemTable.cs CenterServer/DB/Table/EquipTable.cs CenterServer/DB/Table/EquipXLGeneTable.cs CenterServer/DB/Modle/CentRoleModel.cs ProtoBuf/proto/ResultEntity.proto Network/Proto/ResultEntity.cs` 无空白错误；仅有既有 LF/CRLF 提示。
- 构建命令：`dotnet build LYF_Server/CenterServer/CenterServer.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/item-uid-test-20260905/CenterServer/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/item-uid-test-20260905/CenterServer/obj/"`。
- CenterServer 结果：0 错误，1 条既有警告：`MMoRpgCommon/Item/ItemDesc.cs:80` 的 `ToString()` 隐藏基类成员。
- 构建命令：`dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/item-uid-test-20260905/GameServer/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/item-uid-test-20260905/GameServer/obj/"`。
- GameServer 结果：0 错误，4 条既有警告：`ItemDesc.cs:80`、`ShopStatic.cs:36`、`OnlineRole.cs:88`、`ShopStatic.cs:8`。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/item-uid-test-20260905/GM_Game/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/item-uid-test-20260905/GM_Game/obj/"`。
- GM_Game 结果：0 错误，21 条既有警告，主要是 `System.Numerics.Vectors` 程序集版本冲突、`Singleton<T>` 与 Network.dll 重名、过时 Unity API 和未使用字段。
- 测试结论：通过。构建产物位于 `D:/unitypro/LYFMMORGP/_temp/item-uid-test-20260905/`，未经产品经理确认不得清理。
- 未验证：没有连接实际数据库，无法验证既有表列已迁移、历史行读取和复合键约束；未进行 Center/Game/Gate/Unity 四端登录联调。

## [VERIFY][界面][TR-255] 2026-09-05 服务端分类 UID 生成器验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2376—2385 行；历史时点记录，不代表当前仍在执行。

- 独立行为测试：六种业务类型均正确写入高 3 位；`UID_Max` 和保留值 `6` 均抛出 `ArgumentOutOfRangeException`；10 万次并发生成的 UID 均为正数且无重复；时钟回拨和序列耗尽路径通过。
- 静态检查：`UIDType` 只含用户指定的七个成员；`UIDHelper` 仅公开 `ConfigureServerInstanceId(int)`、`CreateUID(UIDType)` 和 `EpochUtc`，未包含解析 API；未修改数据库、协议、物品或其他业务文件。
- 构建命令：`dotnet build LYF_Server/MMoRpgCommon/MMoRpgCommon.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/uid-generator-typed-test/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/uid-generator-typed-test/obj/"`。
- 构建结果：失败，既有 `LYF_Server/MMoRpgCommon/Item/ItemBase.cs:44` 的未完成 C# 语法触发 `CS1585`，未报告 `UIDHelper` 或 `UIDType` 错误。该文件第 14、34 行保留旧 C++ 调用 `UIDHelper::CreateUID(UID_Item)`，待该物品文件被单独修复时改为 `UIDHelper.CreateUID(UIDType.UID_Item)`。
- 空白检查：`git diff --check -- LYF_Server/MMoRpgCommon/UID/UIDHelper.cs LYF_Server/MMoRpgCommon/UID/UIDType.cs LYF_Server/MMoRpgCommon/MMoRpgCommon.csproj` 通过，仅有既有 LF/CRLF 提示。
- 临时产物：`D:/unitypro/LYFMMORGP/_temp/uid-generator-simple/`、`D:/unitypro/LYFMMORGP/_temp/uid-generator-typed/` 和 `D:/unitypro/LYFMMORGP/_temp/uid-generator-typed-test/`，未经产品经理确认不得清理。
- 未执行：未做跨进程重启、真实多逻辑服部署或物品 UID 持久化联调；同一实例 ID 的并发进程必须由部署层避免。

## [VERIFY][背包商城][TR-257] 2026-09-05 RItem 风格 ItemBase 实现验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2394—2422 行；历史时点记录，不代表当前仍在执行。

- 修复：`ItemBase.cs` 已移除未完成的 C++ 占位语法，建立 `long ItemUID`（实例）与 `int ItemID`（配置）的明确边界；默认构造通过 `UIDHelper.CreateUID(UIDType.UID_Item)` 生成 UID，工厂加载已有实例时覆盖为持久化 UID。
- 容器验证：`RoItemContainer` 主索引为 `Dictionary<long, ItemBase>`；按格位查询 UID 返回 `ItemUID`，按格位查询配置返回 `ItemID`；放入、取出、按 UID 查询和数量归零移除均按实例 UID 操作；排序和数量统计仍使用配置 ID。
- 静态检查：未发现 `RoItemContainer` 继续以 `ItemID` 作为字典键、UID 查找键或按位置移出参数；`git diff --check` 未发现空白错误，仅输出既有 LF/CRLF 提示。
- 构建命令：`dotnet build LYF_Server/MMoRpgCommon/MMoRpgCommon.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/ritem-fix-20260905/common-bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/ritem-fix-20260905/common-obj/"`。
- Common 结果：0 错误，保留既有 `ItemDesc.ToString()` 隐藏 `object.ToString()` 的 1 条警告。
- 构建命令：`dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/ritem-fix-20260905/game-bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/ritem-fix-20260905/game-obj/"`。
- GameServer 结果：0 错误，保留既有 4 条警告：`ItemDesc.ToString()`、`ShopStatic.GetType()`、`ShopStatic.type` 未赋值和 `OnlineRole._roleBaseAttrEntity` 未使用。
- 未验证：未连接数据库加载 64 位 UID，也未修改客户端协议；同配置多实例、绑定差异堆叠和真实角色背包持久化加载待后续专项测试。

### 成员封装复验

- 静态检查：`ItemBase` 的实例 UID、配置 ID、归属、位置、数量、状态、价格和时间均为私有字段；`ItemFactory` 与 `RoItemContainer` 通过 `Get/Set` 方法访问，不存在对这些成员的公开属性读写。
- 构建：`dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/itembase-encapsulation-20260905/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/itembase-encapsulation-20260905/obj/"`，0 错误，保留既有 4 条警告。

### ChangeReason 类型拆分验证（2026-09-06）

- 静态检查：`Item.cs` 已不再定义 `ChangeReason` 和 `RExtraParamType`；两类分别位于同名文件，`ChangeReason` 的 7 个字段均为私有并带中文注释。
- 构造与接口：无参构造默认主/次原因 NONE、怪物类型 `-1`、附加参数 `0`、流水号空串、经验倍率 `1.0`；单参和双参构造及 Getter/Setter、三种隐式转换均已实现。
- 构建命令：`dotnet build LYF_Server/MMoRpgCommon/MMoRpgCommon.csproj --configuration Debug --no-restore -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/change-reason-build/"`。
- 结果：0 个错误，保留既有 `ItemDesc.ToString()` 和 `IdipParam` 未使用字段警告；未执行真实商城/背包联调。
- GameServer 联编尝试：被工作区现有 `GameServer/Entity/OnlineRole.cs` 中未完成的 C++ 风格代码阻断（`item_count`、`item_id` 等符号及指针运算错误），这些错误与本次 `ChangeReason` 类型拆分无关，未修改该文件。

### 私有字段注释复验

- 静态检查：`ItemBase` 的 `_itemUid`、`_itemId`、`_roleId`、`_bagType`、`_bagIndex`、`_count`、`_itemSign`、`_moneyType`、`_totalPrice`、`_createTime`、`_expireTime` 与 `_extraData` 均有独立中文语义注释。
- 构建：`dotnet build LYF_Server/MMoRpgCommon/MMoRpgCommon.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/itembase-field-comments-20260905/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/itembase-field-comments-20260905/obj/"`，0 错误，保留既有 `ItemDesc.ToString()` 警告。

## [VERIFY][网络][背包商城][构建][TR-258] 2026-09-06 角色商城购买记录登录同步代码审查与构建

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2423—2439 行；历史时点记录，不代表当前仍在执行。

- 审查类型：只读代码审查，未修改客户端、服务端、协议或数据库业务文件。
- 协议生成校验：使用 `LYF_Server/ProtoBuf/bin/protoc.exe` 将 `ProtoBuf/proto/RequestEntity.proto`、`ResultEntity.proto` 生成到 `D:/unitypro/LYFMMORGP/_temp/role-shop-review-20260906/proto-generated/`；生成的两个 C# 文件与 `LYF_Server/Network/Proto/` 对应文件 SHA256 完全一致。确认 `RoleShopPurchaseInfoRet`、`RoleShopPurchaseInfo`、`CMD_RoleShopPurchaseInfoCode` 及字段 `PurchaseInfoList` 均存在。
- 静态链路检查：Center `CenterRoleCtrl.cs:54-72` 在进入世界后发送商城回包；`CentRoleModel.cs:112-158` 查询并映射 `RoleShopPurchaseTable`；GameServer `GameMessageProtocolRegister.cs:57-59` 注册 `ForwardToGate`，`RoleShopPurchaseInfoHandler.cs:41-72` 按复合会话查找角色并缓存；Gate `GateServerApp.cs:32` 注册协议，`GateClientResponseForwarder.cs:87-89` 解析回包；客户端 `NetSocketMgr.cs:30-31` 提前注册，`ShopMgr.cs:98-127` 解析并保存快照。相关业务文件每个顶级类型独立，新增关键逻辑含中文注释。
- P1 未通过项：`RoleManager.cs:120-132` 发送 `EnterWroldReq` 到 Center 前未校验请求角色与当前复合会话；由于 `ClientMessageManager.cs:180-188` 在 Handler 前执行 `ForwardToGate`，仅依赖 `RoleShopPurchaseInfoHandler` 的角色 ID 检查不能阻止其他角色商城记录泄露给 Unity。未进行攻击性联网测试，依据代码路径判定为阻塞项。
- P2 未通过项：Gate 对 `CMD_RoleShopPurchaseInfoCode` 的失败结果只透传专用回包，不发送 `CMD_ErrCode`；客户端 `ShopMgr` 仅日志处理失败，不展示 Tips。该行为与“网关校验失败统一错误码提示”的既有约束不一致。
- P2 风险项：客户端 `ShopMgr.ReplacePurchaseRecords` 未校验 `RoleShopPurchaseInfo.RoleId`；数据库实体注释仍引用不存在的 `NpcID`，表名仍是 `roleNpcShopPurchase`。需要确认数据库是否已完成从 NPC 维度到 Shop 维度的迁移，当前不能据此判定数据库实测通过。
- 构建命令与结果：
  - `dotnet build LYF_Server/Network/Network.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/role-shop-review-20260906/Network/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/role-shop-review-20260906/Network/obj/"`：0 错误，保留 4 条既有 MMoRpgCommon 警告。
  - `dotnet build LYF_Server/CenterServer/CenterServer.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/role-shop-review-20260906/CenterServer/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/role-shop-review-20260906/CenterServer/obj/"`：0 错误，保留 4 条既有警告。
  - `dotnet build LYF_Server/GateServer/GateServer.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/role-shop-review-20260906/GateServer/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/role-shop-review-20260906/GateServer/obj/"`：0 错误，保留 4 条既有警告。
  - `dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/role-shop-review-20260906/GameServer/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/role-shop-review-20260906/GameServer/obj/"`：失败 2 个错误，均为既有 `GameServer/Entity/Components/RoItemComponent.cs:103` 的 C++ 风格 `ItemDesc(item_id, item_count)` 残留，不属于本次商城同步改动。
  - `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/role-shop-review-20260906/GM_Game/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/role-shop-review-20260906/GM_Game/obj/"`：0 错误，保留 21 条项目既有 Unity/引用警告。
- 产物与限制：构建、protoc 和反射辅助文件均位于 `D:/unitypro/LYFMMORGP/_temp/role-shop-review-20260906/`；未连接真实数据库，未启动 Center→Game→Gate→Unity 四端联调，未验证断线、重连、空记录和多 Gate 会话隔离。客户端插件 `LYF_MMO/Assets/GM_Game/Plugins/Net/Network.dll` 与本次 Network 构建 DLL SHA256 不同（目标商城消息 API 可反射到，但插件整体不是当前构建产物），后续应由客户端开发同步并记录备份。
- 测试结论：协议生成和三端/客户端独立编译通过；商城登录同步审查不通过，必须先处理 P1 会话绑定问题，再复测 Gate 错误码、客户端角色校验和真实四端链路。

## [VERIFY][背包商城][TR-259] 2026-09-06 BagTransition 背包扩容链路验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2440—2446 行；历史时点记录，不代表当前仍在执行。

- 静态检查：确认 `KnapsackService.cs` 已删除，`GameServer.csproj` 不再包含其编译项；扩容代码仅使用 `RolePackPlain`、`RolePackEquip`、`RolePackConsume`、`RolePackMaterial`，未修改协议。
- 构建命令：`dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/knapsack-transition-20260906/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/knapsack-transition-20260906/obj/"`。
- 构建结果：GameServer、Network、MMoRpgCommon 均成功，0 个错误；保留 7 条既有警告（ItemDesc.ToString、IdipParam 未使用字段、ShopStatic、OnlineRole 字段）。
- 未执行：未连接真实数据库，未进行扩容请求的四端联调、容量并发和 Center 持久化失败重试验证。

## [VERIFY][背包商城][TR-260] 2026-09-06 背包容器拆分与容量封装验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2447—2454 行；历史时点记录，不代表当前仍在执行。

- 静态检查：`GameServer/Service/item` 下事务与容器顶级类型均已按同名文件拆分；`ShortCutBag` 唯一私有字段有中文注释；无 `OpenedKnapsackGridCounts` 外部写入。
- 构建命令：`dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/knapsack-transition-container-review-20260906/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/knapsack-transition-container-review-20260906/obj/"`。
- 构建结果：GameServer、Network、MMoRpgCommon 成功，0 错误；保留 7 条既有警告（ItemDesc、IdipParam、ShopStatic、OnlineRole）。
- 空白检查：本次拆分文件未发现空白错误；全量 `GameServer` 差异仍报告 `Entity/OnlineRole.cs:100-101` 两处既有行尾空白，本次未修改对应行。
- 未执行：未连接数据库，未验证真实扩容请求及 Center 持久化失败重试。

## [VERIFY][背包商城][TR-261] 2026-09-06 Luban 物品包类别读取验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2455—2467 行；历史时点记录，不代表当前仍在执行。

- 静态映射：`ItemConfigBase` 暴露只读 `KnapsackType PackType`；`NormalItemConfig` 读取 `ItemInfo.PackType`，`WeaponItemConfig` 读取 `EquipInfo.PackType`。`NormalizePackType` 仅接受 `RolePackPlain`、`RolePackEquip`、`RolePackConsume`、`RolePackMaterial`；非法配置会记录中文警告，并按配置来源回退到总览包或装备包。
- 查询接口：`LubanMgr.GetItemPackTypeById(int itemId)` 返回 `KnapsackType?`；未找到统一物品配置时返回 `null`，不会把未找到误判为零值 `RolePackPlain`。
- 结构检查：`ItemConfigBase.cs`、`NormalItemConfig.cs`、`WeaponItemConfig.cs` 各只包含一个顶级类型；`Network.csproj` 已显式包含三个文件。
- Network 构建命令：`dotnet build LYF_Server/Network/Network.csproj --no-restore --configuration Debug -o D:/unitypro/LYFMMORGP/_temp/luban-packtype-20260906/Network/bin -p:BaseIntermediateOutputPath=D:/unitypro/LYFMMORGP/_temp/luban-packtype-20260906/Network/obj/`。
- Network 构建结果：0 错误，4 条既有 `MMoRpgCommon` 警告（`ItemDesc.ToString` 隐藏、`IdipParam` 三个未使用字段）；产物位于 `D:/unitypro/LYFMMORGP/_temp/luban-packtype-20260906/Network/`。
- GameServer 构建命令：`dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug -o D:/unitypro/LYFMMORGP/_temp/luban-packtype-20260906/GameServer/bin -p:BaseIntermediateOutputPath=D:/unitypro/LYFMMORGP/_temp/luban-packtype-20260906/GameServer/obj/`。
- GameServer 构建结果：0 错误，保留上述 4 条公共库警告及 `ShopStatic.GetType` 隐藏、`ShopStatic.type` 未赋值、`OnlineRole._roleBaseAttrEntity` 未使用 3 条既有警告。
- 空白检查：新增解析文件和项目清单无空白错误；全范围检查仍报告 `LubanMgr.cs:29` 已存在的行尾空白，本次未修改该行。
- 运行时配置读取：通过构建产物加载现有 Luban bytes 并执行 `LubanMgr.Init()`；普通物品 `2001` 的原始 `PackType=0` 查询为 `RolePackPlain`，装备 `21001` 的原始 `PackType=1` 查询为 `RolePackEquip`，两类映射均正确。
- 未执行：未启动完整服务端，也未验证物品按包类别进入对应容器；构建产物由产品经理确认后再清理。

## [VERIFY][背包商城][TR-262] 2026-09-06 Luban 装备表生成物返工验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2468—2479 行；历史时点记录，不代表当前仍在执行。

- 正式生成命令：在 `D:/unitypro/LYFMMORGP/Luaban/MiniTemplate` 执行 `dotnet Luban/Luban.dll -t all -c cs-bin -d bin --conf .\\luban.conf -x outputCodeDir="D:/unitypro/LYFMMORGP/_temp/luban-packtype-regeneration-20260906/output/Code" -x outputDataDir="D:/unitypro/LYFMMORGP/_temp/luban-packtype-regeneration-20260906/output/Data"`；生成成功。
- 字段顺序：`EquipInfo` 依次读取 `ItemTypeId`、`ItemMainType`、`ItemSubType`、`EquipType`、`EquipName`、`Icon`、`Attribute`、`NeedLevel`、`Source`、`SellPrice`、`SellCurrencyType`、`Job`、`BCanBeStacked`、`CanStrengthen`、`MaxStrengthenLevel`、`MaxGemLevel`、`GemSlotCount`、`Des`、`PackType`、`GeneId0-5`、`GeneValue0-5`；不再包含旧 `Desc`。
- 同步校验：重新生成的 `EquipInfo.cs`、`TbEquipInfo.cs`、`Tables.cs` 与正式目录同名文件 SHA256 一致。生成的 `tbequipinfo.bytes` 与原正式 bytes 不同，已安全同步；同步后的二者 SHA256 均为 `26C9F4624900A1CCA34A2721968CA5498908CA9733246EA8EFCA508F2C71A25A`。
- 占用检查：系统中存在 Unity、Unity Hub 和 WPS 进程；对正式 `tbequipinfo.bytes` 的独占读写打开成功，因此无文件锁。未关闭、终止或重启任何进程。
- 适配与空白检查：`WeaponItemConfig.cs` 未发现 `config.Desc` 或其他已删除 `EquipInfo.Desc` 引用，仅使用 `EquipName`、`Des`、`PackType` 等现存字段；`LubanMgr.cs`、适配文件与生成代码的 `git diff --check` 通过。
- Network 构建：`dotnet build LYF_Server/Network/Network.csproj --no-restore --configuration Debug -o D:/unitypro/LYFMMORGP/_temp/luban-packtype-regeneration-20260906/Network/bin -p:BaseIntermediateOutputPath=D:/unitypro/LYFMMORGP/_temp/luban-packtype-regeneration-20260906/Network/obj/`，0 错误，保留 `ItemDesc.ToString` 隐藏和 `IdipParam` 三个未使用字段共 4 条既有警告。
- GameServer 复测：`dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug -o D:/unitypro/LYFMMORGP/_temp/luban-packtype-regeneration-20260906/GameServer-retest/bin -p:BaseIntermediateOutputPath=D:/unitypro/LYFMMORGP/_temp/luban-packtype-regeneration-20260906/GameServer-retest/obj/`，0 错误，保留上述 4 条公共库警告及 `ShopStatic.GetType` 隐藏、`ShopStatic.type` 未赋值、`OnlineRole._roleBaseAttrEntity` 未使用共 7 条既有警告。
- 运行时读取：通过构建的 Network 程序集执行 `LubanMgr.Init()` 加载正式 bytes；装备 `21001` 读取到 `EquipName=清池梦查·湖字主武器`、`CanStrengthen=1`、`MaxStrengthenLevel=80`、`MaxGemLevel=6`、`GemSlotCount=2`、非空 `Des`、原始 `PackType=1`、`GeneId0=3`、`GeneValue0=1236`，`GetItemPackTypeById` 返回 `RolePackEquip`。
- 未执行：未启动完整 Center/Gate/Game/Unity 链路，未验证物品按包类别进入对应容器；本任务的生成、构建及备份文件保留在 `_temp/luban-packtype-regeneration-20260906/`，等待产品经理确认后清理。

## [VERIFY][综合][TR-263] 2026-09-06 Luban TimeType 生命周期字段验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2480—2490 行；历史时点记录，不代表当前仍在执行。

- 表格验证：使用 artifact-tool 从临时只读副本检查工作簿结构和渲染，确认 `#ItemInfo.xlsx` 的 `TimeType` 位于 `PackType` 后；`#EquipInfo.xlsx` 的 12 个非空装备 sheet 均位于 `PackType` 后，空 `Sheet1` 未改动。字段三行依次为 `TimeType`、`int`、`物品生命周期`，所有数据行为数值 `0`。写入正式表前以 SHA256 对比确认正式工作簿未被用户改动；原始工作簿备份在 `D:/unitypro/LYFMMORGP/_temp/time-type-20260906/`。
- Luban 生成：在隔离的 `MiniTemplate` 副本执行 `dotnet Luban/Luban.dll -t all -c cs-bin -d bin --conf .\\luban.conf -x outputCodeDir="D:/unitypro/LYFMMORGP/_temp/time-type-20260906/output/Code" -x outputDataDir="D:/unitypro/LYFMMORGP/_temp/time-type-20260906/output/Data"`，成功。生成的 `ItemInfo.cs`、`EquipInfo.cs` 均按 `TimeType -> PackType -> GeneId0` 读取；仅同步这两个生成 C# 文件及 `tbiteminfo.bytes`、`tbequipinfo.bytes` 到正式 Network 目录，SHA256 一致。
- 静态映射：`ItemConfigBase` 新增只读 `int TimeType` 与中文注释、构造参数；`NormalItemConfig` 映射 `ItemInfo.TimeType`，`WeaponItemConfig` 映射 `EquipInfo.TimeType`；三个文件保持一类型一文件，项目文件已存在其编译登记，无需修改。
- Network 构建：`dotnet build LYF_Server/Network/Network.csproj --no-restore --configuration Debug -o D:/unitypro/LYFMMORGP/_temp/time-type-20260906/Network/bin -p:BaseIntermediateOutputPath=D:/unitypro/LYFMMORGP/_temp/time-type-20260906/Network/obj/`，0 错误，保留 `ItemDesc.ToString` 和 `IdipParam` 的 4 条既有警告。
- 运行时读取：使用构建出的 `Network.dll` 执行 `LubanMgr.Init()` 加载正式 bytes；普通物品 `2001` 与装备 `21001` 的原始 `TimeType` 均为 `0`，对应统一 `ItemConfigBase.TimeType` 也均为 `0`。
- GameServer 构建：`dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug -o D:/unitypro/LYFMMORGP/_temp/time-type-20260906/GameServer/bin -p:BaseIntermediateOutputPath=D:/unitypro/LYFMMORGP/_temp/time-type-20260906/GameServer/obj/` 被现有 `GameServer/Service/item/PackageLogic/BagGiveItemTransition.cs:46` 的 `CS1503`（`long` 无法转换为 `int`）阻断；未报告本次 TimeType 改动相关错误。
- 代码审查复核：从正式工作簿再次运行 Luban 到 `_temp/time-type-review-regenerate-20260906/`，`ItemInfo.cs`、`EquipInfo.cs`、`TbItemInfo.cs`、`TbEquipInfo.cs`、`Tables.cs` 与两个 bytes 的 SHA256 均和正式文件一致；`ItemConfigBase.TimeType` 为带中文注释的只读 `int`，普通物品和装备适配器均映射该字段；一类型一文件和 `git diff --check` 通过。
- 测试 Agent 独立复验：使用 artifact-tool 严格校验正式工作簿，普通物品表 `TimeType` 位于第 5 列，装备 12 个业务 Sheet 位于第 20 列，均为唯一字段且类型为 `int`、注释为“物品生命周期”、所有数据行为整数 `0`；空 `Sheet1` 未修改。独立加载正式 bytes 的运行时检查输出 `item=0,equip=0,itemConfig=0,equipConfig=0`（配置 ID `2001/21001`）。Network 构建 0 错误、4 条既有警告；GameServer 首错仍为既有 `BagGiveItemTransition.cs:46` 的 `CS1503 long->int`，未引入新的 TimeType 相关错误。严格脚本和日志保留在 `_temp/time-type-test-20260906/`。

## [VERIFY][背包商城][TR-265] 2026-09-06 延迟物品持久化审查返工验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2496—2502 行；历史时点记录，不代表当前仍在执行。

- Network、GameServer、CenterServer 均隔离构建通过，0 错误；警告均为项目既有警告，产物位于 `_temp/item-persistence-rework-20260906/`。
- 静态校验确认负重接口及 `BagMaxLoadChangeTransition` 已移除；容器容量和格子校验保留；`git diff --check` 未发现本次修改空白错误。
- 断开流程已接入网关会话事件队列，角色在 ACK 前不会释放；同版本快照会比较内容后再幂等确认。
- 未执行真实 MySQL、四端联调及客户端构建；本轮未修改客户端文件。

## [VERIFY][背包商城][TR-266] 2026-09-06 物品最大堆叠数量改造验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2503—2510 行；历史时点记录，不代表当前仍在执行。

- 表格：`#ItemInfo.xlsx`、`#EquipInfo.xlsx` 已将 `CanStack`/`bCanBeStacked` 改为 `MaxStackCount`，字段类型保持 `int`，不可堆叠值为 1，普通可堆叠物品默认值为 99；原始文件备份和生成过程均在 `_temp/stack-count-20260906/`。
- Luban：重新生成并同步 `ItemInfo.cs`、`EquipInfo.cs`、`TbItemInfo.cs`、`TbEquipInfo.cs`、`Tables.cs` 和两个 bytes；运行时字段统一为 `MaxStackCount`，未发现旧字段引用。
- Network、GameServer、CenterServer 隔离构建均 0 错误；Network 4 条、GameServer 7 条警告均为既有警告。
- 静态行为检查：`AddItem` 计算已有堆叠可用容量，预检新格子数，按 `MaxStackCount` 循环创建实例，并在每次实例变更后设置脏状态；`MaxStackCount=1` 时数量 N 会创建 N 个实例。
- 未执行真实 MySQL 和四端联调；未修改客户端文件。构建产物保留在 `_temp/stack-count-20260906/build/`，待产品经理确认后清理。

## [VERIFY][背包商城][TR-267] 2026-09-06 AddItem 堆叠计算溢出复验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2511—2516 行；历史时点记录，不代表当前仍在执行。

- `RoItemComponent.AddItem` 的已有堆叠容量累计、剩余数量和所需格子计算已改用 `long`；转换为 `int` 前增加最大值检查。
- Network、GameServer、CenterServer 隔离构建均 0 错误；警告数量与项目既有警告一致。产物位于 `_temp/stack-count-overflow-20260906/`。
- 未执行真实数据库和四端联调；未修改客户端文件。

## [VERIFY][背包商城][TR-268] 2026-09-06 最大堆叠数量与负重移除审查复验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2517—2524 行；历史时点记录，不代表当前仍在执行。

- 表格静态检查：`#ItemInfo.xlsx` 的 `MaxStackCount` 数据均为 99；`#EquipInfo.xlsx` 的 12 个业务 Sheet 数据均为 1，字段类型均为 `int`。
- Luban 重生成：在 `_temp/stack-count-review-20260906/luban/` 隔离生成成功；5 个生成 C# 文件及两个 bytes 与正式文件 SHA256 一致。
- 负重清理：正式源码范围未发现 `CanStack`、`bCanBeStacked`、`BCanBeStacked`、`KnapsackService`、`BagMaxLoad`、`MaxLoad`、`LoadWeight`、`负重` 或 `重量` 残留。
- 构建：`dotnet build LYF_Server/Network/Network.csproj`、`GameServer/GameServer.csproj`、`CenterServer/CenterServer.csproj` 均成功，0 错误；构建输出位于 `_temp/stack-count-review-20260906/`。
- 审查阻塞：`RoItemComponent.ModifyItem` 正增量没有校验 `MaxStackCount`，可突破单实例堆叠上限；当前结论为不通过，详见 `_temp/stack-count-review-20260906/review.txt`。
- 未执行：真实数据库和四端联调；待后端修复后补做 `MaxStackCount=1/99`、已有堆叠接近上限、超过上限、拆分及容量不足原子性测试。

## [VERIFY][背包商城][TR-269] 2026-09-06 ItemBase 物品状态位接口验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2525—2534 行；历史时点记录，不代表当前仍在执行。

- 修改文件：`LYF_Server/MMoRpgCommon/Item/ItemBase.cs`。新增 `SetSign(SignBit bit, bool sign)` 与 `GetSign(SignBit bit)`，分别对应 C++ 的状态位置位/清位和状态位读取。
- 构建命令：`dotnet build MMoRpgCommon/MMoRpgCommon.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/item-sign-methods-20260906/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/item-sign-methods-20260906/obj/"`。
- 构建结果：通过，0 错误。保留 4 条既有警告：`ItemDesc.ToString()` 隐藏成员，以及 `IdipParam` 的 3 个未使用字段。
- 静态验证：`SetSign` 使用 `1 << (int)bit` 建立掩码，设位使用 `|`，清位使用 `& ~`，仅影响目标状态位；`GetSign` 通过右移及 `& 1` 返回目标位。现有 `SignBit` 为 `0..10`，位移合法。
- 格式验证：`git diff --check -- MMoRpgCommon/Item/ItemBase.cs` 通过；仅报告既有 LF/CRLF 转换提示。
- 临时产物：`D:/unitypro/LYFMMORGP/_temp/item-sign-methods-20260906/`，待产品经理确认后清理。
- 代码审查：通过，未发现问题。`SetSign` 的设位和清位仅影响目标位，`GetSign` 与 C++ 的右移掩码判断等价；`BindBit` 与既有 `SetBound` 语义兼容。

## [VERIFY][背包商城][TR-270] 2026-09-06 RoItemComponent 代码注释补充验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2535—2546 行；历史时点记录，不代表当前仍在执行。

- 测试目标：确认 RoItemComponent 类、字段、属性、方法及关键逻辑均已补充简洁中文注释，且仅新增注释未改变任何业务逻辑。
- 修改范围：仅 LYF_Server/GameServer/Entity/Components/RoItemComponent.cs；ItemDataListDB.cs 已有较完整注释，本轮未修改。
- 注释覆盖：类级 <summary> 职责说明；构造函数、Owner 属性、Initialize、GetContainer、容量管理（Set/Load/ApplyOpenedGridCount）、LoadKnapsack 登录装载、GetItemNum、AddItem 堆叠填充、DeleteItem/ModifyItem/SwapItem 变更入口、OnItemChange 脏标记统一入口、NeedSave/Save/OnSaveAcknowledged 持久化链路、GetDefineContiner/LoadItems/CreateLoadedItem/FindContainer 内部工具、NextChangeVersion/MarkDataListChanged/SafeAddPrice 版本与安全计算，均补充 XML 注释或行内注释。
- 关键逻辑行内注释：AddItem 的 long 溢出保护与空格预检、LoadKnapsack 的 UID 去重与版本恢复、SwapItem 的跨容器回滚、MarkDataListChanged 的 swap 双容器脏标记、SafeAddPrice 的溢出判断。
- 构建命令：`dotnet build D:\unitypro\LYFMMORGP\LYF_Server\LYF_Server.sln --no-restore --configuration Debug`，输出重定向至 `D:/unitypro/LYFMMORGP/_temp/build_roitem_comment_20260906.log`。
- 构建结果：成功，0 个错误；保留 8 条既有警告（MMoRpgCommon 4 条：ItemDesc.ToString 隐藏、IdipParam 三个未使用字段；LoginServer 1 条：异步调用未等待；GameServer 3 条：ShopStatic.GetType 隐藏、ShopStatic.type 未赋值、OnlineRole._roleBaseAttrEntity 未使用），均与本次注释修改无关。
- 静态检查：仅新增注释行，未修改任何可执行代码、方法签名、字段类型或控制流；已有注释全部保留。
- 未执行项：未启动 MySQL、Redis、四端服务器和 Unity 客户端进行联网联调；本次为纯注释补充，不影响运行时行为。
- 临时产物：构建日志保留于 `D:/unitypro/LYFMMORGP/_temp/build_roitem_comment_20260906.log`，待产品经理确认后清理。

## [VERIFY][综合][TR-271] 2026-09-06 CRoleWriteTask 代码注释补充验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2547—2558 行；历史时点记录，不代表当前仍在执行。

- 测试目标：确认 `CRoleWriteTask` 类、字段、方法及关键逻辑均已补充简洁中文注释，且仅新增注释未改变任何业务逻辑。
- 修改范围：仅 `LYF_Server/CenterServer/DB/Task/CRoleWriteTask.cs`。
- 注释覆盖：类级 `<summary>` 职责说明（CenterServer 数据库写任务、事务保存角色物品/装备/基因数据、版本幂等与过期拒绝）；构造函数、`_request`/`_serverBase`/`_basePackage` 字段；Execute 主流程、ValidateRequest 参数校验、SaveItemDataLists 删旧写新、SnapshotMatches 幂等比对、Reply 回包、IsValidBagType、FromUtcTicks 时间转换，均补充 XML 注释或行内注释。
- 关键逻辑行内注释：Execute 的事务开启、版本三分支校验（同版本幂等/过期拒绝/新版本写入）、异常回滚不覆盖原异常；SaveItemDataLists 的删除顺序（词条→装备→物品）与插入顺序（物品→装备→词条）、过期时间 null 语义；SnapshotMatches 的数量校验、UID 字典逐字段比对、扩展记录双向存在性校验、最终记录数一致性。
- 构建命令：`dotnet build D:\unitypro\LYFMMORGP\LYF_Server\LYF_Server.sln --no-restore --configuration Debug`，输出重定向至 `D:/unitypro/LYFMMORGP/_temp/build_crolewrite_comment_20260906.log`。
- 构建结果：成功，0 个错误，0 个警告。
- 静态检查：仅新增注释行，未修改任何可执行代码、方法签名、字段类型或控制流；已有注释全部保留。
- 未执行项：未启动 MySQL、Redis、四端服务器和 Unity 客户端进行联网联调；本次为纯注释补充，不影响运行时行为。
- 临时产物：构建日志保留于 `D:/unitypro/LYFMMORGP/_temp/build_crolewrite_comment_20260906.log`，待产品经理确认后清理。

## [VERIFY][背包商城][TR-272] 2026-09-06 虚拟货币背包验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2559—2567 行；历史时点记录，不代表当前仍在执行。

- 新增 `CurrencyType`、`RoleCurrencyTable`、`RoleCurrencyComponent`；货币按角色和类型独立保存，余额使用 `long`，不占用物品格子。
- 登录回包和保存协议增加 `CurrencyInfo` 列表；CenterServer 负责读取/初始化/持久化，GameServer 装载并纳入现有延迟保存与 ACK 清脏链路。
- 商城购买按货币类型聚合总价，先原子扣款，再发放物品；发放失败返还本次扣款。
- 版本边界：货币变更会读取物品组件当前版本并生成更高版本，货币单独修改时不会因角色已有较高物品版本而被拒绝。
- Network、GameServer、CenterServer 均构建成功，0 错误；产物位于 `D:/unitypro/LYFMMORGP/_temp/currency-build-20260906/final2/`。
- 未执行真实 MySQL 和四端联调；未修改客户端，Unity 网络程序集和货币 UI 待后续同步。

## [VERIFY][背包商城][TR-273] 2026-09-06 虚拟货币背包代码审查返工验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2568—2569 行；历史时点记录，不代表当前仍在执行。


## [VERIFY][背包商城][TR-274] 2026-09-06 虚拟货币并入 BagTransition 验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2570—2584 行；历史时点记录，不代表当前仍在执行。

- 修改 `RoItemComponent` 持有货币虚拟背包状态，删除 `RoleCurrencyComponent` 编译项和实现文件。
- 修改 `BagTakeItemTransition`/`BagGiveItemTransition`，按 `ItemDesc + KnapsackType` 统一处理货币扣除与发放；商城改为通过事务入口扣款。
- 登录、保存快照、ACK 清脏和释放条件均已切换到 `RoItemComponent`。
- Network、GameServer、CenterServer 隔离构建均通过，0 错误；保留项目既有警告。
- 未执行真实 MySQL 和四端联调；货币 ItemDesc 使用 `CurrencyType` 数值作为虚拟物品标识，后续应与完整虚拟物品枚举统一。

- 静态验证：`RoleNetSender` 的空请求判断同时检查物品和货币列表；`OnlineRole.SaveAllMoudle()` 不再使用会跳过货币组件的短路调用；`CanDispose()` 同时检查物品、货币脏状态。
- 商城边界：每项购买数量限制为 `1..999`；价格乘法、同货币总价累加和奖励数量均在扣款前执行上限校验，奖励数量严格限制在现有 `int` 物品数量模型内。
- 构建：Network、GameServer、CenterServer 分别执行 Debug 隔离构建，全部成功且 0 错误。Network/CenterServer 各 4 条、GameServer 7 条既有警告，与本次修改无关。
- 格式检查：`git diff --check` 通过，仅提示 `OnlineRole.cs` 后续可能进行 LF/CRLF 行尾转换，不属于空白错误。
- 未执行：真实 MySQL 货币读写事务、商城购买运行测试及 Center/Game/Gate/Unity 四端联调；本轮未修改客户端。
- 临时产物：`D:/unitypro/LYFMMORGP/_temp/currency-review-fix-20260906/final/`，待产品经理确认后清理。

## [VERIFY][网络][TR-275] 2026-09-06 RoleKanpsackInfo 协议结构重构验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2585—2594 行；历史时点记录，不代表当前仍在执行。

- 协议：`RoleKanpsackInfoRet` 现在包含 `CmdCode` 与 `RoleKanpsackInfo`；后者包含背包容量、四类物品列表、穿戴栏、`SaveVersion` 和 `CurrencyInfoList`。`MainRoleInfo.money` 已删除，字段 3 保留为 reserved。
- 生成：使用 `LYF_Server/ProtoBuf/bin/protoc.exe` 生成 `ResultEntity.cs`，输出保留在 `D:/unitypro/LYFMMORGP/_temp/role-knapsack-proto-20260906/`，并同步正式 Network 协议文件。
- 引用：CenterServer `CentRoleModel` 组装嵌套背包信息和货币；GameServer `RoItemComponent.LoadKnapsack` 从嵌套对象装载容量、物品和货币；Unity 背包窗口、装备窗口和测试 Mock 已切换到嵌套对象。
- 构建：Network、GameServer、CenterServer、GateServer Debug 隔离构建全部成功，0 个错误。保留项目既有警告：Network/CenterServer/GateServer 4 条、GameServer 7 条，均与本次协议重构无关。
- 静态检查：旧的 `RoleKanpsackInfoRet` 直接背包字段仅剩生成代码历史文本和待清理的客户端引用检查目标；服务端无 `MainRoleInfo.Money` 使用。`git diff --check` 未发现本次修改空白错误。
- 未执行：真实 MySQL、Center/Game/Gate/Unity 四端联调及 Unity 编辑器导入后的完整编译。
- 审核结论：协议结构和引用迁移通过；生成代码内的三目表达式属于 protoc 自动生成内容，未手改。业务文件中存在历史遗留三目表达式（例如 `CentRoleModel.cs`、`RoItemComponent.cs`、客户端 UI），不由本次协议字段重构新增，后续可按全项目规则单独清理。
- 临时产物：`D:/unitypro/LYFMMORGP/_temp/role-knapsack-proto-20260906/`，待产品经理确认后清理。

## [VERIFY][协作][TR-276] 2026-09-07 类型文件拆分规则调整

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2595—2601 行；历史时点记录，不代表当前仍在执行。

- 校验内容：确认 `AGENTS.md` 明确已有多类型文件不强制拆分，新设计的多个顶级类型必须按同名文件分别创建；`DECISIONS.md` 已同步记录该决策。
- 验收结果：规范文件内容已写入，未修改业务代码；通过文本静态检查。
- 未执行：本次仅调整协作规则，不需要编译、运行测试或 Unity 验证。

## [VERIFY][综合][TR-277] 2026-09-07 PackageLogic 合并到 BagTransition.cs 验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2602—2616 行；历史时点记录，不代表当前仍在执行。

- 验证内容：21 个 PackageLogic 类型是否完整并入 `BagTransition.cs`、using 去重、注释骨架剥离、csproj 引用清理、源文件删除后全解决方案编译。
- 类型完整性：对合并后文件执行 `^(public|...)(class|enum|struct|interface)\s+\w+` 正则，命中 22 条，与"原 BagTransition 基类 + 21 个源文件类型"一一对应，无遗漏。
- using 校验：合并文件头部仅 `using System;` 与 `using System.Collections.Generic;` 两行，无重复 using。
- 注释骨架校验：逐文件比对，原 14 处文件末尾孤立前向 `/// <summary>`（含 `BagTransition.cs` 原末尾"修改物品属性的事务骨架"）均已剥离；各类型自身文档注释全部保留。
- csproj 校验：`GameServer.csproj` 中 `PackageLogic` 关键字 0 命中；`Service\item\BagTransition.cs` 引用保留。
- 源文件夹：`PackageLogic` 目录下 .cs 文件计数为 0，文件夹本身保留。
- 构建：`dotnet build D:\unitypro\LYFMMORGP\LYF_Server\LYF_Server.sln --no-restore --configuration Debug`，输出重定向 `_temp/build.log`，退出码 0；官方汇总"0 个错误，8 个警告"；`GameServer.exe` 正常产出。
- 警告归属：8 条警告均为历史既有（`IdipParam` 未使用字段、`LoginCtrl` async 未 await、`ShopStatic.GetType` 隐藏基类成员、`OnlineRole._roleBaseAttrEntity` 未使用、`ShopStatic.type` 未赋值、`ItemDesc.ToString` 隐藏基类成员），分布于 MMoRpgCommon/LoginServer/GameServer 既有文件，本次未引入新警告。
- 业务逻辑零变更：合并前后类型成员与方法体逐字一致，仅调整文件归属与排列顺序；C# 同文件内前向引用合法，编译通过证明类型解析无缺失。
- 未执行：未启动服务器运行时联调；本次为纯文件合并重构，不改变运行行为。
- 回滚路径：`_temp/PackageLogic_backup/` 含 21 个原始 .cs；`_temp/BagTransition.cs.bak` 与 `_temp/GameServer.csproj.bak` 为合并前快照。

## [VERIFY][综合][TR-278] 2026-09-07 MessageHandler 合并到 ClientMessageManager.cs 验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2617—2628 行；历史时点记录，不代表当前仍在执行。

- 验证内容：7 个 MessageHandler 类型是否完整并入 `ClientMessageManager.cs`、using 去重、csproj 引用清理、子目录 handler 未误伤、源文件备份后全解决方案编译。
- 类型完整性：对合并后文件执行 `^(public|...)(class|enum|struct|interface)\s+\w+` 正则，命中 8 条（`ClientMessageManager`/`CenterMessageRoute`/`IGameMessageHandler`/`GameMessageHandlerKey`/`GameMessageRegistration`/`GameMessageHandlerContext`/`GameCenterResponseForwarder`/`GameMessageProtocolRegister`），与"原 ClientMessageManager + 7 个源文件类型"一一对应，无遗漏。
- using 校验：合并文件头部仍为 `using System;`/`using System.Collections.Concurrent;`/`using System.Collections.Generic;`/`using System.Threading;` 四行，无重复 using；`GameMessageHandlerKey.cs` 的 `using System;` 已被覆盖。
- csproj 校验：`GameServer.csproj` 中 MessageHandler 根目录仅剩 `ClientMessageManager.cs` 一条；7 个被合并文件 0 命中；Login/Role/Knapsack/Shop 等子目录 12 个 handler 引用全部保留。
- 目录校验：`MessageHandler` 根目录 .cs 文件计数为 1（仅 ClientMessageManager.cs）；Equip/Item/Knapsack/Login/Role/Shop/Task 子目录完整保留；备份目录 `_temp/MessageHandler_backup/` 含 7 个原始 .cs。
- 构建：`dotnet build D:\unitypro\LYFMMORGP\LYF_Server\LYF_Server.sln --no-restore --configuration Debug`，输出重定向 `_temp/build2.log`，退出码 0；官方汇总"0 个错误，8 个警告"。
- 警告归属：8 条警告与上一轮 PackageLogic 合并后完全一致（`IdipParam` 未使用字段、`LoginCtrl` async 未 await、`ShopStatic.GetType` 隐藏、`OnlineRole._roleBaseAttrEntity` 未使用、`ShopStatic.type` 未赋值、`ItemDesc.ToString` 隐藏），均为历史既有，本次未引入新警告。
- 业务逻辑零变更：合并前后类型成员与方法体逐字一致，仅调整文件归属；C# 同文件内前向引用合法，编译通过证明类型解析无缺失。
- 未执行：未启动服务器运行时联调；本次为纯文件合并重构，不改变运行行为。
- 回滚路径：`_temp/MessageHandler_backup/` 含 7 个原始 .cs；`_temp/ClientMessageManager.cs.bak` 为合并前快照；`_temp/GameServer.csproj.bak2` 为本轮 csproj 修改前快照。

## [VERIFY][背包商城][TR-279] 2026-09-07 商城商品明细字段消歧验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2629—2635 行；历史时点记录，不代表当前仍在执行。

- 协议修改：`BuyShopItem.shop_id` 改为 `shop_item_id`，注释明确对应 `ShopItemInfo.Id`；顶层请求 `shop_id` 继续对应 `ShopTable.Id`。
- 代码同步：更新 `ShopStatic`、`RequestBuyShopItem`、`ShopCondition` 和 Unity `ShopMgr` 的商品明细访问；重新生成 `Network/Proto/RequestEntity.cs` 并同步 Unity 网络程序集。
- 静态检查：服务端和客户端脚本范围内未发现商品明细继续访问旧 `ShopId` 的引用；顶层 `BuyShopItemReq.ShopId` 保留。
- 构建结果：Network、GameServer Debug 构建均成功，0 个错误；共 8 个既有警告，与本次字段重命名无关。
- 未执行：真实 Gate/Game/Center/Unity 四端商城购买联调。

## [VERIFY][背包商城][TR-280] 2026-09-07 商城商品字段改为 GoodsId 验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2636—2642 行；历史时点记录，不代表当前仍在执行。

- 协议：`BuyShopItem` 字段已改为 `goods_id`，生成属性为 `GoodsId`；顶层 `BuyShopItemReq.ShopId` 未改变。
- 引用：`ShopStatic`、`RequestBuyShopItem`、`ShopCondition`、Unity `ShopMgr` 已完成明细字段迁移。
- 协议生成：已使用项目 `protoc.exe` 重新生成 `Network/Proto/RequestEntity.cs`。
- 构建：GameServer 暂未通过，首个错误为 `LYF_Server/GameServer/Service/shop/ShopCondition.cs:49` 的不完整 `GetBuyLeftTimes` 调用；该错误与本次字段改名无关，未擅自修改用户已有逻辑。
- 静态结果：旧商品明细 `ShopId` 引用已清理；临时生成文件保存在 `_temp/shop-goods-id-20260907/`。

## [VERIFY][背包商城][TR-281] 2026-09-07 RoleShopPurchaseInfo 商品字段消歧验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2643—2648 行；历史时点记录，不代表当前仍在执行。

- 协议：`RoleShopPurchaseInfo.shop_id` 已改为 `goods_id`，生成 C# 属性为 `GoodsId`，对应 `ShopItemInfo.Id`。
- 代码：CenterServer 使用 `GoodsId = row.ShopID`，Unity `ShopMgr` 按 `GoodsId` 建立限购记录索引；数据库实体暂保留 `ShopID`，因为本次未执行数据库列迁移。
- 生成与构建：`ResultEntity.cs` 已重新生成；Network 构建成功，0 错误。GameServer 当前仍被 `RoleShopRecord.cs` 既有未完成代码（`role_`、`RoleCountManager`、`COUNT_TABLE_SHOP_TYPE`、`commodit_id`）阻断，与本次字段改名无关。
- 插件：重新生成的 Network 程序集已同步到 Unity `Assets/GM_Game/Plugins/Net/Network.dll`。

## [VERIFY][综合][TR-282] 2026-09-07 RoleCountManager 验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2649—2655 行；历史时点记录，不代表当前仍在执行。

- 参考解析：完成对 `roserver/gameserver/unit/role_count_manager.h/.cpp` 的职责和接口对照，保留 `(type,id)` 计数模型及上限/增量接口命名语义。
- 代码验证：新增 `RoleCountType` 和 `RoleCountManager`，`OnlineRole` 初始化时注册组件；`RoleShopRecord` 登录装载时同步每日/永久购买次数，并通过管理器查询剩余次数。
- 构建：`dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug` 成功，0 个错误，8 个既有警告。
- 未完成：尚未新增通用计数数据库表和登录/保存协议；当前周限购记录仍缺少独立周计数持久化字段，后续扩展表结构时再接入 `RoleCountManager` 的持久化链路。
- 临时产物：构建输出位于 `D:/unitypro/LYFMMORGP/_temp/role-count-manager-20260907/`。

## [VERIFY][综合][TR-283] 2026-09-07 RoleCountType 简化验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2656—2661 行；历史时点记录，不代表当前仍在执行。

- 静态检查：`RoleCountType` 仅保留 `Shop` 和 `Dungeon`；商城每日/每周/永久周期通过 `PurchaseLimitType` scope 区分，不再重复定义商城枚举值。
- 行为接入：`RoleShopRecord` 装载和查询均使用 `RoleCountType.Shop`，每日与永久记录分别使用不同 scope。
- 构建结果：GameServer Debug 构建成功，0 个错误，8 个既有警告。
- 临时产物：`D:/unitypro/LYFMMORGP/_temp/role-count-shop-scope-20260907/`。

## [VERIFY][综合][TR-284] 2026-09-07 CountTable 角色次数系统设计记录

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2662—2666 行；历史时点记录，不代表当前仍在执行。

- 分析对象：参考项目 `CountTable.xlsm` 的 `Id/Action/Key/Name/RefreshType/RefreshNum/Limit/Active/TellClient` 九列及当前项目 `RoleCountManager`、商城限购记录和延迟保存链路。
- 分析结果：确认参考项目按 `Action + Key` 管理次数，刷新配置与角色运行时计数分离；本项目拟采用相同模型。
- 本轮未修改：未修改 Excel、proto、数据库实体或业务代码，未执行编译；设计方案已在本轮回复中说明。

## [VERIFY][综合][TR-285] 2026-09-07 CountInfo 配置表验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2667—2674 行；历史时点记录，不代表当前仍在执行。

- 参考读取：使用表格工具读取 `F:/UnityPro/ro/Ro_cs422nb/config/Table/Excels/CountTable.xlsm`，确认参考表为 9 列 `Id/Action/Key/Name/RefreshType/RefreshNum/Limit/Active/TellClient`。
- 新表：创建 `Luaban/MiniTemplate/Datas/#CountInfo.xlsx`，包含 16 条现有商城商品次数配置；`Key` 已修正为商品配置 `ItemId`（2001、2002、2003、2005、21001 等），不是商城行号。
- 旧表：`#ShopItemInfo.xlsx` 已删除 `PurchaseLimitType`、`PurchaseLimitCount` 两列，后续列已左移，原商品、价格和商店关联字段保留。
- 视觉检查：新表标题、类型行、注释行和数据区域渲染可读；临时预览与检查文件位于 `_temp/count-table-20260907/`。
- 占用检查：修改前对 `#ShopItemInfo.xlsx` 执行独占读写检查成功；未关闭 WPS 进程。
- 未执行：本轮未生成 Luban 代码，未修改服务端/客户端业务逻辑，未进行编译；等待策划确认 `PurchaseLimitType=2/3` 到 `RefreshType` 的最终映射。

## [VERIFY][综合][TR-286] 2026-09-07 角色次数系统后端实现

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2675—2680 行；历史时点记录，不代表当前仍在执行。

- Luban 生成命令成功，产物暂存 `_temp/count-gen-20260907`，并同步 Network/LubanManage。
- `dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug`：0 错误，5 个既有警告。
- 未完成：次数快照协议、CenterServer RoleCountTable 写入链路和真实商城限购联调。

## [VERIFY][综合][TR-287] 2026-09-07 RoleCountManager 脏状态审查

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2681—2685 行；历史时点记录，不代表当前仍在执行。

- 修复：RoleCountManager 使用变更代数，Save 不再提前清脏，Center ACK 且期间无新修改时才清理；OnlineRole.OnRoleDataSaved 接入次数 ACK。
- 验证：dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug，0 错误，4 个历史警告。
- 风险：次数登录读取仍未接入 RoleKanpsackInfo；RoleCountManager 刷新语义和旧 RoleShopRecord 仍需后端 Agent 完成统一。

## [VERIFY][综合][TR-288] 2026-09-07 GoodsId 字段消歧验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2686—2687 行；历史时点记录，不代表当前仍在执行。


## [VERIFY][场景实体][TR-289] 2026-09-07 角色组件基类与次数登录闭环验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2688—2689 行；历史时点记录，不代表当前仍在执行。


## [VERIFY][场景实体][TR-290] 审核返工验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2690—2707 行；历史时点记录，不代表当前仍在执行。
- 组件基类已重命名为 `RoleComponentBase`，旧 `IRoleComponent` 不再参与工程。
- `RoleComponentContainer` 统一保存、ACK 和角色级版本；次数快照支持空列表清库与同版本比较。
- CenterServer 不再额外下发 `RoleShopPurchaseInfo`，商城次数统一随背包数据返回。
- Debug 全解决方案构建：0 错误，4 条既有警告。

- 协议：`RoleCountInfo` 移入 `ResultEntity.proto`，`RoleKanpsackInfo` 增加次数列表；已重新生成 Network 协议代码。
- 组件：组件容器统一收集脏数据、保存快照和 ACK，OnlineRole 不再按物品/次数组件分别拼包。
- Center/Game：CenterServer 查询 `RoleCountTable` 并在背包登录回包返回；GameServer 将列表装载到 `RoleCountManager`。
- 结构：次数运行时使用 `Dictionary<Tuple<long,long>, RoleCountItemInfo>`，对应参考项目的 `map<pair<Action,Key>,...>`。
- 构建：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug` 成功，0 错误，4 条既有警告。
- 未执行：真实 MySQL 登录读取、保存 ACK 和 Gate/Game/Unity 四端联调。

- Luban 生成成功，产物位于 `_temp/count-gen-goods-20260907`。
- 全解决方案 Debug 构建成功：0 错误，5 个既有警告。
- 静态检查确认 ShopItemParseInfo/LubanMgr 已使用 `GoodsId`；未执行客户端业务修改。
- 追加检查：生成的 `ShopItemInfo`、`CountInfo` 均已使用 `GoodsId`，全解决方案复核构建 0 错误、0 警告，日志 `_temp/count-goods-verify.log`。

## [VERIFY][构建][场景实体][TR-291] 2026-09-07 OnlineRole.SendToClient 构建验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2708—2716 行；历史时点记录，不代表当前仍在执行。

- 验证范围：`LYF_Server/GameServer/Entity/OnlineRole.cs` 新增的本端客户端定向发送方法。
- 静态检查：方法按顺序校验 `protoCode`、`GateSessionId`、`UnitySessionId` 和网关 `Session`；失败写入中文错误日志并返回 `false`，成功调用 `Session.SendData` 后返回 `true`；新增代码未使用三目运算符。
- 构建命令：`dotnet build D:/unitypro/LYFMMORGP/LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug`。
- 构建结果：成功，0 个错误、12 个既有警告；未新增编译警告。
- 构建日志：`D:/unitypro/LYFMMORGP/_temp/build-online-role-send-to-client-20260907.log`，等待产品经理确认后清理。
- 未执行：未启动 GateServer、GameServer 和 Unity 客户端进行真实发包联调。

## [VERIFY][场景实体][TR-292] 2026-09-07 OnlineRole.SendToClient 代码审核

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2717—2727 行；历史时点记录，不代表当前仍在执行。

- 审核结论：通过，未发现阻塞问题。
- 路由核查：通过 `SessionMgr.Instance.GetSession(GateSessionId)` 定位本角色所在网关连接，再调用 `gateSession.SendData(UnitySessionId, protoCode, data)` 指定目标 Unity 客户端；与现有 `RoleManager` 定向发送方式一致。
- 防御检查：无效协议号、无效网关会话 ID、无效 Unity 会话 ID、网关会话不存在时均记录中文错误并返回 `false`；空协议体沿用 `ServerBase.SendData` 的既有允许语义。
- 规范检查：方法、参数和返回值均有中文 XML 注释；新增方法未使用三目运算符；除引入 `Google.Protobuf` 和新增该方法外，未发现本任务引入的范围扩展。
- 剩余边界：`ServerBase.SendData` 返回 `void` 并在内部捕获 Socket 异常，所以 `true` 只表示底层发送入口已被调用，不能证明数据已送达客户端；真实送达仍需 GateServer 与 Unity 联调验证。
- 本轮仅做静态代码审核并更新协作记录，未修改业务代码，未重复执行构建。
- 虚拟物品配置表验证：`#ItemInfo.xlsx` 新增 26 条 ItemMainType=7、PackType=5、TimeType=0 的物品；`#ShopItemInfo.xlsx` 货币字段改为 `CurrencyItemId` 并统一引用 101；Luban 全量生成退出码 0。
- 使用 `@oai/artifact-tool` 检查并渲染两个工作簿；公式错误扫描为 0，预览保存在 `_temp/currency-item-edit-20260907/`。
- Luban 生成输出已同步到 `LYF_Server/Network/LubanManage`，其中 `ShopItemInfo.cs` 已生成 `CurrencyItemId`；生成日志为 `_temp/currency-item-edit-20260907/luban-generate.log`。

## [VERIFY][背包商城][TR-293] 2026-09-07 客户端虚拟物品货币适配验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2728—2737 行；历史时点记录，不代表当前仍在执行。

- 编译命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/client-virtual-item-build-final`。
- 编译结果：成功，0 个错误、21 个警告；警告为 Unity 引用程序集版本冲突、既有 Singleton 重复类型和既有脚本告警，本次修改未新增编译错误。
- 静态检查：客户端业务脚本中 `WalletInfo`、`GetWalletInfoById`、`.CurrencyType`、`PurchaseLimitType`、`PurchaseLimitCount`、`CurrencyInfoList` 均为 0 命中。
- 规则检查：本次修改的 `ShopMgr`、`GoodsItemWidget`、`BuyItemWidget` 未使用三目运算符；`git diff --check` 无空白错误，仅有行尾转换提示。
- 程序集检查：Unity 客户端 `Network.dll` 与服务端构建产物 SHA256 一致；两处客户端 `MMoRpgCommon.dll` 已同步最新枚举定义。
- 构建日志：`D:/unitypro/LYFMMORGP/_temp/client-virtual-item-build.log`。
- 未执行：Unity MCP 未提供可用工具，未执行编辑器 Console 检查和四端商城购买联调。

## [VERIFY][背包商城][TR-294] 2026-09-08 虚拟物品货币迁移后端收尾验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2738—2748 行；历史时点记录，不代表当前仍在执行。

- 静态检查：`RoItemComponent.LoadKnapsack` 中 `LoadItems(info.RoleVirtualItemPack, ...)` 仅剩 1 处；服务端源码、协议、项目文件和文件名中 `CurrencyInfo`、`RoleCurrencyTable`、`WalletInfo`、`tbwalletinfo` 均为 0 命中。全项目扫描另发现 Unity `Assets/HybridCLRGenerate/link.xml` 残留 `cfg.WalletInfo`，已交接客户端 Agent 清理。
- 配置链路：生成的 `ShopItemInfo.CurrencyItemId` 注释明确关联 `#ItemInfo.ItemTypeId`；`ShopItemParseInfo` 将该字段传入购买消耗；`RequestBuyShopItem` 在扣款前校验货币物品配置存在且 `PackType=RoleVirtualItemPack`。
- 边界验证：通过反射调用构建产物中的 `ItemDesc.IsLegalVirtualItem`，`101、603、604、607、701` 返回 true，`0、1000、-1` 返回 false。
- 规则检查：本次新增和修改的关键逻辑包含中文注释；未新增三目运算符。
- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/virtual-item-backend-fix-build`。
- 构建结果：成功，0 个错误、13 个警告；警告为解决方案级 `--output` 提示及项目既有代码警告，本次未新增编译错误。
- 构建日志：`D:/unitypro/LYFMMORGP/_temp/virtual-item-backend-fix-build.log`；编译输出位于 `D:/unitypro/LYFMMORGP/_temp/virtual-item-backend-fix-build/`。
- 未执行：真实 MySQL 登录/保存、商城扣款运行验证，以及 CenterServer、GameServer、GateServer、Unity 四端联调。

## [VERIFY][背包商城][TR-295] 2026-09-08 虚拟物品货币迁移客户端收尾验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2749—2758 行；历史时点记录，不代表当前仍在执行。

- HybridCLR 检查：已删除 `LYF_MMO/Assets/HybridCLRGenerate/link.xml` 中失效的 `cfg.WalletInfo` 保留项。
- 静态检查：`LYF_MMO/Assets/GM_Game/Scripts`、`Assets/Boot`、`Assets/HybridCLRGenerate/link.xml` 和 `GM_Game.csproj` 中 `WalletInfo`、`CurrencyInfo`、`RoleCurrencyTable`、`RoleCurrencyComponent`、旧钱包 `CurrencyType` 均为 0 命中；没有误删 `SellCurrencyType` 或 `CurrencyItemId`。
- 程序集校验：客户端 `Plugins/Net/Network.dll` 的 SHA256 为 `B6A22833952FE06DE8F40E9F619B715E89EDE06A9EF8B536C021B67B84E740B5`；两处 `MMoRpgCommon.dll` 的 SHA256 均为 `C3A0E32F18C60FF632C021E30473B1FC9BCC5C6C01ECE976985DFC90775F903F`，与 `_temp/virtual-item-backend-fix-build` 最新产物一致。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/client-virtual-item-finalize-build`。
- 构建结果：成功，0 个错误、21 个既有警告；警告仍为 Unity 引用程序集版本冲突、`Singleton<T>` 重复类型和已有脚本告警。
- 构建日志：`D:/unitypro/LYFMMORGP/_temp/client-virtual-item-finalize-build.log`；编译输出位于 `D:/unitypro/LYFMMORGP/_temp/client-virtual-item-finalize-build/`，等待产品经理确认后清理。
- 未执行：Unity 编辑器 Console/HybridCLR 裁剪验证、真实商城购买、真实 MySQL 读写以及 CenterServer、GameServer、GateServer、Unity 四端联调。

## [VERIFY][背包商城][TR-296] 2026-09-08 虚拟物品货币迁移代码审核复核

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2759—2770 行；历史时点记录，不代表当前仍在执行。

- 审核方式：只读检查服务端虚拟物品组件、BagTransition、CenterServer 保存任务、Luban 配置输出、Unity 程序集哈希和旧钱包引用；检查产物与脚本均位于 `D:/unitypro/LYFMMORGP/_temp/virtual-item-code-review-20260908`。
- 配置读取：使用 `@oai/artifact-tool` 只读解析 `Luaban/MiniTemplate/Datas/#ItemInfo.xlsx` 与 `#ShopItemInfo.xlsx`；确认 26 条虚拟物品均为 `ItemMainType=7、PackType=5、TimeType=0`，商城第五列为 `CurrencyItemId` 且当前引用 101；未写回工作簿。
- 通过项：`RoItemComponent.LoadKnapsack` 虚拟背包装载调用仅 1 处；`ItemDesc.IsLegalVirtualItem` 对 `101、603、604、607、701` 返回 true，对 `0、1000、-1` 返回 false；GameServer 业务层继续检查 Luban 配置主类型和虚拟背包类型；客户端 `Network.dll` SHA256 为 `B6A22833952FE06DE8F40E9F619B715E89EDE06A9EF8B536C021B67B84E740B5`，两处 `MMoRpgCommon.dll` SHA256 为 `C3A0E32F18C60FF632C021E30473B1FC9BCC5C6C01ECE976985DFC90775F903F`，与 `_temp/virtual-item-backend-fix-build` 产物一致。
- P1 失败：`LYF_Server/GameServer/Entity/Components/RoleCountManager.cs:45-55` 在未配置计数项分支解引用空 `conf`，可直接触发 NullReferenceException，符合本次程序崩溃风险；需修复后复测。
- P1 失败：空次数快照清库无法发送/通过 Center 校验。`RoleNetSender` 以两个列表均为空判定无保存数据，`CRoleWriteTask.ValidateRequest` 又拒绝 `RoleCountSnapshot=true` 且空列表；需允许显式清空并验证 ACK。
- P2 失败：`Luaban/MiniTemplate/output/Code/Tables.cs:29,46,65` 残留 `TbWalletInfo` 声明、加载和解析，模板生成输出与已删除钱包表不一致。
- 规范失败：迁移相关 `ItemDataListDB.cs`、`RoItemContainer.cs`、`RoleCountManager.cs` 仍存在 `?:` 三目运算符；项目规则要求改为 `if/else`。本次未直接修改业务代码，待后端 Agent 返工。
- 构建参考：沿用 2026-09-08 收尾构建记录，服务端 0 错误、客户端 0 错误；本轮审核未重复编译，因已发现阻塞项，不能视为最终验收。
- 未执行：真实 MySQL 事务、Unity 编辑器 Console/HybridCLR 导入、商城购买和四端联调。

## [VERIFY][背包商城][TR-297] 2026-09-08 虚拟物品与角色次数系统第二次审核返工验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2771—2781 行；历史时点记录，不代表当前仍在执行。

- 空配置检查：`RoleCountManager.GetAndCreateItemInfo` 对不存在的 `CountInfo` 返回 null；构建产物验证无空引用编译风险。
- 空次数快照检查：`RoleNetSender` 的无数据判断保留 `RoleCountSnapshot` 标记；`CRoleWriteTask.ValidateRequest` 接受显式空快照；`SaveRoleCountData` 删除旧行后跳过空列表插入。
- Luban 检查：`Luaban/MiniTemplate/output/Code/Tables.cs` 和服务端 `Tables.cs` 均无 `TbWalletInfo`；`ShopItemInfo.cs` 均使用 `CurrencyItemId`；Luban/服务端生成目录旧钱包名称命中为 0；服务端 `tbshopinfo.bytes` 已删除。
- 三目检查：迁移相关手写文件集合（RoleCount、背包持久化、商城购买、角色保存、Luban 管理）中 `?:` 命中为 0；自动生成协议代码未手工改动。
- 构建命令：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/virtual-item-review-fix2-build`。
- 构建结果：成功，0 个错误、13 个既有警告；日志：`D:/unitypro/LYFMMORGP/_temp/virtual-item-review-fix2-build.log`。
- `git diff --check`：通过，仅提示 `LubanMgr.cs` 的行尾转换信息。
- 未执行：真实数据库事务、空快照运行时清库、商城购买和四端联调。

## [VERIFY][网络][构建][TR-298] 2026-09-08 后端返工程序集同步与客户端最终构建

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2782—2790 行；历史时点记录，不代表当前仍在执行。

- 同步来源：`D:/unitypro/LYFMMORGP/_temp/virtual-item-review-fix2-build/Network.dll`、`MMoRpgCommon.dll`。
- 同步目标：`LYF_MMO/Assets/GM_Game/Plugins/Net/Network.dll`、`LYF_MMO/Assets/GM_Game/Plugins/Net/MMoRpgCommon.dll`、`LYF_MMO/Assets/GM_Game/Plugins/Common/MMoRpgCommon.dll`。
- SHA256 校验：`Network.dll` 来源与目标均为 `5C742533B15CC3014E8BC11D7B9830EE7E37B0AABEA67E333CC93BE56443BD8F`；`MMoRpgCommon.dll` 来源与三个目标均为 `DC57C9336AB6D118AA54D9A30AA1E1E90FEBC35E1BC9F6B8C5FBD5540B7B803B`。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/client-final-build`。
- 构建结果：成功，0 个错误、21 个既有警告；日志位于 `D:/unitypro/LYFMMORGP/_temp/client-final-build.log`。
- 本轮未修改客户端业务脚本；未执行 Unity 编辑器导入、真实商城购买及四端网络联调。

## [VERIFY][协作][TR-299] 2026-09-08 Codex 崩溃后最终恢复核对

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2791—2798 行；历史时点记录，不代表当前仍在执行。

- 结论：确认异常来自 Codex 进程崩溃，项目修改没有回退或丢失。
- 服务端：`dotnet build LYF_Server/LYF_Server.sln --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/virtual-item-review-fix2-build` 成功，0 个错误、13 个既有警告。
- 客户端程序集：Unity `Network.dll` 与 `_temp/virtual-item-review-fix2-build/Network.dll` SHA256 一致；Unity `MMoRpgCommon.dll` 与对应服务端产物 SHA256 一致。
- 客户端：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/client-final-build` 成功，0 个错误、21 个既有警告。
- 静态核对：迁移相关手写代码三目表达式命中 0；Luban 正式输出无 `TbWalletInfo`；虚拟背包登录加载仅 1 次；空配置计数查询不会解引用空对象。
- 未执行：真实 MySQL、商城购买、空快照运行时清库和 Center/Game/Gate/Unity 四端联调。

## [VERIFY][背包商城][TR-300] 2026-09-08 FindItemForGive 验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2799—2807 行；历史时点记录，不代表当前仍在执行。

- 修改文件：`LYF_Server/GameServer/Service/item/RoItemContainer.cs`。
- 静态检查：`git diff --check -- GameServer/Service/item/RoItemContainer.cs` 通过；本次新增实现未发现 `?:`。
- 语义检查：函数对 `desc == null` 或无效 `ItemID` 返回空列表；按配置 `MaxStackCount` 过滤同 ID、数量大于零且未满的物品，并复制 `ItemUID`、`ItemSign`。
- 构建命令：`dotnet build GameServer/GameServer.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/find-item-for-give-20260908/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/find-item-for-give-20260908/obj/"`。
- 构建结果：未通过。失败来自既有 `GameServer/Service/item/BagTransition.cs:672` C++ 风格 `for (const auto* item : same_item_list)`，共 8 个语法错误；`MMoRpgCommon` 和 `Network` 在本次构建中成功生成。
- 未执行：真实背包发放和四端联调；待清理前述 `_temp` 临时产物。

## [VERIFY][背包商城][TR-301] 2026-09-08 ItemTransition 按背包类型分类验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2808—2816 行；历史时点记录，不代表当前仍在执行。

- 静态检查：`git diff --check -- LYF_Server/GameServer/Service/item/ItemTransition.cs` 通过；新增函数未使用三目运算符。
- 语义检查：`ClassifyItemsByBagType` 对空列表、空管理器、空物品、非法 `ItemID` 和未配置物品返回或跳过；已配置物品按 `GetItemPackTypeById` 返回的 `KnapsackType` 聚合到同一列表。
- 构建命令：`dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/item-transition-classify-20260908`。
- 构建结果：成功，0 个错误、13 条既有警告。
- 临时产物：`D:/unitypro/LYFMMORGP/_temp/item-transition-classify-20260908/`，待产品经理确认后清理。
- 未执行：真实背包发放和四端联调。

## [VERIFY][场景实体][TR-302] 2026-09-08 公共定时器基础设施验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2817—2828 行；历史时点记录，不代表当前仍在执行。

- 参考解析：确认 `Ro_cs422nb` 中 `ITimer` 只定义 `OnTimer`，`CTimerMgr` 负责注册、倒计时、回调和回收，`SaveRoleDataTimer` 只在回调中设置角色保存标记。
- 修改范围：新增 `LYF_Server/MMoRpgCommon/Time/ITimer.cs` 与 `CTimerMgr.cs`；GameServer 的 `SaveRoleDataTimer` 改为注册公共定时器；`GameGlobal` 统一推进定时器；补充 `项目分析/TIMER_USAGE.md`。
- 静态检查：新增及适配代码中文注释完整；`rg "\\?:" LYF_Server/MMoRpgCommon/Time LYF_Server/GameServer/Entity/SaveRoleDataTimer.cs` 无命中。
- 构建命令：
  - `dotnet build LYF_Server/MMoRpgCommon/MMoRpgCommon.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/timer-build/common/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/timer-build/common/obj/"`
  - `dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug -p:OutputPath="D:/unitypro/LYFMMORGP/_temp/timer-build/gameserver/bin/" -p:BaseIntermediateOutputPath="D:/unitypro/LYFMMORGP/_temp/timer-build/gameserver/obj/"`
- 构建结果：MMoRpgCommon 成功，0 个错误、4 个既有警告；GameServer 成功，0 个错误、13 个既有警告。日志分别为 `_temp/timer-build-common.log` 与 `_temp/timer-build-gameserver.log`。
- 行为测试：使用 `_temp/timer-smoke/TimerSmoke.cs` 验证单次触发、有限重复、掉帧时最多触发一次、剩余时间查询和取消，输出为“定时器行为测试通过”。
- 未执行：未启动 GameServer 等待完整保存周期，未执行真实多服定时器联调；`_temp/timer-build` 临时产物待产品经理确认后清理。

## [VERIFY][场景实体][TR-303] 2026-09-08 公共定时器代码审查

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2829—2841 行；历史时点记录，不代表当前仍在执行。

- 审查范围：`LYF_Server/MMoRpgCommon/Time/ITimer.cs`、`CTimerMgr.cs`、`LYF_Server/GameServer/Entity/SaveRoleDataTimer.cs`、`GameGlobal.cs`、`OnlineRole.cs`、`MMoRpgCommon.csproj` 和 `项目分析/TIMER_USAGE.md`。
- Block：`LYF_Server/GameServer/GameGlobal.cs:76-78` 使用三目运算符，违反项目全局禁止 `?:` 规则；必须改为 `if/else`。
- Block：`LYF_Server/GameServer/GameGlobal.cs:69-79` 在累计时间为 50 到 99 毫秒时固定传入 50 并将累计值清零，丢弃余量，导致定时器和所有固定帧逻辑长期漂移；应按参考项目保留余量，或明确传入实际经过时间。
- Critical：`LYF_Server/GameServer/GameGlobal.cs:61-63` 使用 `DateTime.UtcNow.Ticks` 计算间隔，系统校时或虚拟机时钟跳变会让定时器提前触发或长时间延迟；应使用单调时钟（例如 `Stopwatch` 或项目统一的单调时间工具）。
- Warning：`LYF_Server/MMoRpgCommon/Time/CTimerMgr.cs:136` 每次 `Update` 都创建 `List<TimerEntry>`，即使没有到期回调也会在 20Hz 热路径产生分配；应复用缓存列表或采用无分配的待处理结构。
- Warning：`LYF_Server/MMoRpgCommon/Time/CTimerMgr.cs:7` 的线程说明写成“业务线程只负责注册和取消”，但 `SetTimer`、`KillTimer`、`Clear` 和 `Update` 均无同步保护；应明确所有接口只能由逻辑线程调用，或实现跨线程投递/同步，否则并发修改字典可能抛异常。
- Warning：`LYF_Server/MMoRpgCommon/Time/CTimerMgr.cs:20-34` 使用 `public` 可变字段承载定时器条目，且 `CTimerMgr` 的 `_instance`、`_timerEntries`、`_nextHandle` 缺少逐项中文字段注释；与“成员变量不使用 public”和关键状态注释约束不符，应改为私有字段并补齐注释。
- 通过项：`ITimer` 为最小回调接口；`SaveRoleDataTimer` 位于 GameServer 且仅注册/取消公共定时器、回调只设置保存标记；`GameGlobal` 在固定逻辑帧调用一次 `CTimerMgr.Update`；公共库和 GameServer 构建均为 0 错误，已有定时器烟测通过。
- 审查结论：打回修复，不得进入最终验收。以上问题属于服务端后端；修复后需重新构建、静态检查和定时器边界测试。真实 GameServer 60 秒保存周期和多服联调仍未执行。
- 本轮未修改业务代码；构建产物和日志继续保留在 `D:/unitypro/LYFMMORGP/_temp/timer-build`，待产品经理确认后清理。

## [VERIFY][场景实体][TR-304] 2026-09-08 公共定时器审查返工验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2842—2848 行；历史时点记录，不代表当前仍在执行。

- 修复内容：`GameGlobal` 使用 `Stopwatch.GetTimestamp` 计算单调时间；按 50 毫秒固定步长消费累计时间，保留不足一帧和超出追赶上限的余量；删除本任务范围内的三目运算符。
- `CTimerMgr`：定时器条目字段全部改为私有；新增复用的到期条目缓存，`Update` 不再每帧分配 `List<TimerEntry>`；所有公开接口文档明确仅限逻辑线程调用。
- 静态检查：`rg "\\?:" LYF_Server/GameServer/GameGlobal.cs LYF_Server/MMoRpgCommon/Time LYF_Server/GameServer/Entity/SaveRoleDataTimer.cs` 无命中；未发现 `DateTime.UtcNow` 定时计算或 `new List<TimerEntry>` 热路径分配。
- 待验证：已完成代码修改，下一步执行 MMoRpgCommon/GameServer 构建和 `_temp/timer-smoke` 行为测试。

## [VERIFY][构建][场景实体][TR-305] 2026-09-08 公共定时器审查返工构建验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2849—2856 行；历史时点记录，不代表当前仍在执行。

- 静态检查：`GameGlobal.cs`、`CTimerMgr.cs` 和 `SaveRoleDataTimer.cs` 本任务范围内无 `?:`；未使用 `DateTime.UtcNow` 进行定时；`TimerEntry` 数据字段均为私有；`CTimerMgr` 只在初始化时创建一次到期缓存列表。
- 构建命令：`dotnet build LYF_Server/MMoRpgCommon/MMoRpgCommon.csproj --no-restore --configuration Debug` 与 `dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug`，输出目录及中间文件位于 `_temp/timer-review-fix-build`。
- 构建结果：MMoRpgCommon 成功，0 个错误、4 个既有警告；GameServer 成功，0 个错误、13 个既有警告。日志为 `_temp/timer-review-fix-build/common.log` 和 `_temp/timer-review-fix-build/gameserver.log`。
- 行为测试：重新编译并运行 `_temp/timer-smoke/TimerSmoke.cs`，单次、有限重复、掉帧单次触发、剩余时间和取消测试均通过，输出“定时器行为测试通过”。
- 未执行：未启动真实 GameServer 等待 60 秒保存周期，也未进行多服时钟和四端联调；临时产物待产品经理确认后清理。

## [VERIFY][场景实体][TR-306] 2026-09-08 公共定时器最终验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2857—2867 行；历史时点记录，不代表当前仍在执行。

- 返工复核：`GameGlobal` 已使用 `Stopwatch.GetTimestamp` 单调时钟；累计时间按 50 毫秒固定步长消费，未消费余量继续保留；本任务范围内无 `?:`。
- 管理器复核：`CTimerMgr.TimerEntry` 状态字段均为私有；通过内部构造、属性和推进方法访问；`_dueEntries` 只初始化一次并在每轮复用；公共接口注释明确只能由 GameServer 逻辑线程调用。
- 目录边界：`SaveRoleDataTimer` 仅位于 `LYF_Server/GameServer/Entity`，`MMoRpgCommon/Time` 仅包含通用 `ITimer` 和 `CTimerMgr`；未把角色保存业务放入公共库。
- 静态检查：`rg "\\?:|DateTime\\.UtcNow" LYF_Server/GameServer/GameGlobal.cs LYF_Server/MMoRpgCommon/Time LYF_Server/GameServer/Entity/SaveRoleDataTimer.cs` 无命中；`CTimerMgr.Update` 方法体内未创建 `List<TimerEntry>`，到期列表只在管理器初始化时创建一次。
- 构建：MMoRpgCommon 0 个错误、4 个既有警告；GameServer 0 个错误、13 个既有警告。构建输出和日志位于 `D:/unitypro/LYFMMORGP/_temp/timer-review-fix-build`。
- 行为测试：重新编译并运行 `D:/unitypro/LYFMMORGP/_temp/timer-review-fix-smoke/TimerSmoke.exe`，输出“定时器行为测试通过”，覆盖单次、有限重复、掉帧、剩余时间和取消。
- 审查结论：原代码审查阻塞项已完成返工并通过静态复核；没有新增客户端或协议修改。
- 未执行：真实 GameServer 运行 60 秒保存周期、多服务器时钟压力和 Center/Game/Gate/Unity 四端联调；`_temp` 临时产物待产品经理确认后清理。

## [VERIFY][综合][TR-307] 2026-09-09 角色脏数据保存版本诊断

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2868—2876 行；历史时点记录，不代表当前仍在执行。

- 检查范围：`GameServer/Entity/Role/OnlineRole.cs`、`GameServer/Entity/Role/RoleNetSender.cs`、`GameServer/Components/RoItemComponent.cs`、`GameServer/MessageHandler/Knapsack/RoleKnapsackInfoHandler.cs`、`CenterServer/DB/Task/CRoleWriteTask.cs`、`CenterServer/DB/Table/RoleTable.cs`。
- 静态结论：CenterServer 已按请求版本与 `RoleTable.ItemSaveVersion` 区分新版本、同版本幂等和过期版本；GameServer 重试会复用在途请求版本，正常 ACK 不会因为重试重复递增。
- 风险结论：角色级 `_saveVersion` 未从登录回包显式初始化，且保存定时器早于背包数据加载启动；背包加载失败、回包时序延迟或加载前发生修改时，可能产生低于 CenterServer 权威版本的请求。
- 风险结论：过期版本返回失败后当前发送器仍会重试同一旧请求，可能形成无限重试；需要在后端修复中增加版本冲突恢复或旧会话隔离。
- 溢出结论：`long` 的正常运行周期内不会实际达到上限；严禁达到上限后回绕到 0。应在接近上限时告警，并采用 epoch 或停服迁移方式原子重置。
- 验证状态：本轮未修改业务代码，未执行构建；待后端 Agent 实现版本基线、加载门闩和过期冲突处理后，重新执行三端构建及保存链路测试。

## [VERIFY][背包商城][TR-308] 2026-09-09 物品扣除绑定分类验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2877—2885 行；历史时点记录，不代表当前仍在执行。

- 静态检查：`GetItemForReduce`、`GetItemsByTID` 区域无 `auto`、`std::`、`vector`、`.first`、`.second` 等 C++ 残留；新增代码无三目运算符，中文注释完整。
- 语义检查：默认容器 `-1` 使用配置所属容器，全部容器 `-2` 遍历已初始化容器，其它值按 `KnapsackType` 获取；同 ID 物品按 `IsBind()` 分入绑定和非绑定列表，并按 UID 倒序保持扣除优先级。
- 构建命令：`dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug -p:OutputPath=D:/unitypro/LYFMMORGP/_temp/reduce-item-build-final/bin/ -p:BaseIntermediateOutputPath=D:/unitypro/LYFMMORGP/_temp/reduce-item-build-final/obj/`。
- 构建结果：成功，0 个错误、14 条既有警告。
- 临时产物：`D:/unitypro/LYFMMORGP/_temp/reduce-item-build-final/`（前置构建产物位于 `D:/unitypro/LYFMMORGP/_temp/reduce-item-build/`），待产品经理确认后清理。
- 未执行：真实背包扣除运行验证和四端联调。

## [VERIFY][综合][TR-309] 2026-09-09 RoleManager foreach 迁移验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2886—2897 行；历史时点记录，不代表当前仍在执行。

- 静态检查：`LYF_Server/GameServer/manger/RoleManager.cs` 中 `foreach` 命中数为 0，`for` 命中数为 6；本次新增遍历没有三目运算符。
- 语义检查：在线角色、断线角色 ID、待激活角色和附近广播均先创建快照列表再按下标遍历；快照元素是原 `OnlineRole` 引用，角色状态修改仍会写入原对象；原字典可在处理过程中安全删除。
- 格式检查：`git diff --check -- LYF_Server/GameServer/manger/RoleManager.cs` 通过。
- 首次代码审查：不通过。发现离线角色移除未清理会话索引、更新后可能误删同 ID 新实例、文件残留三目、进入世界缺少会话校验和公开函数注释不足，已退回后端 Agent 返工。
- 返工验证：移除角色前使用 `TryGetValue` 和 `ReferenceEquals` 确认原实例，并同步删除仍指向该角色的会话键；进入世界按 Gate/Unity 会话取得角色并核对请求角色 ID；公开方法和关键字段已补充中文说明。
- 最终静态检查：`rg "foreach|\\?:|\\?" LYF_Server/GameServer/manger/RoleManager.cs` 无命中；`git diff --check` 无空白错误，仅提示工作树行尾将按 Git 设置转换。
- 最终构建：`dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug -p:OutputPath=D:/unitypro/LYFMMORGP/_temp/rolemanager-final-check/bin/ -p:BaseIntermediateOutputPath=D:/unitypro/LYFMMORGP/_temp/rolemanager-final-check/obj/` 成功，0 个错误、14 个既有警告。
- 后续约束：修改其他文件时禁止新增 `foreach`，历史 `foreach` 按文件触碰逐步迁移，不进行无范围控制的全项目机械替换。
- 配置检查：`AGENTS.md`、`Docs/AI/PROJECT_CONTEXT.md`、`Docs/AI/DECISIONS.md` 和 `AIconfig/config.toml` 均已记录 `foreach` 禁用及快照引用语义。
- 现状说明：工作区历史代码仍有 117 处 `foreach`；本次只迁移需求涉及的 `RoleManager.cs`，避免无范围批量重构。后续新增或修改的代码不得再引入 `foreach`。

## [VERIFY][综合][TR-310] 2026-09-09 角色保存版本归属重构验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2898—2904 行；历史时点记录，不代表当前仍在执行。

- 构建命令：`dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/save-version-owner-build`。
- 构建结果：成功，0 个错误，14 个既有警告。
- 静态检查：角色版本追平逻辑已移除；`RoleComponentBase.GetSaveVersion` 为非虚函数；修改文件未新增三目运算符。
- 未执行：真实 CenterServer ACK、登录时序和四端联调；需代码审核 Agent 复核。

## [VERIFY][综合][TR-311] 2026-09-09 角色保存加载门闩与批次版本返工

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2905—2916 行；历史时点记录，不代表当前仍在执行。

- 静态检查：`OnlineRole.cs`、`SaveRoleDataTimer.cs`、`RoleKnapsackInfoHandler.cs`、`RoleCountComponent.cs`、`ItemDataListDB.cs`、`BagTransition.cs`、`RoleComponentContainer.cs` 均为 `foreach=0`、三目符号命中数 `0`；`RoleComponentContainer.GetSaveVersion` 无命中。
- 版本检查：角色仅在 `RoleKanpsackInfo` 完整装载、`InitializeSaveVersion` 成功且 `CompleteRoleDataLoad` 调用后启动保存定时器；重复或冲突版本不会覆盖已记录基线；`SaveAllMoudle` 在 `long.MaxValue` 拒绝新快照并记录中文告警。
- ACK 检查：`ItemDataListDB` 和 `RoleCountComponent` 的保存代数与保存版本绑定，ACK 需同时匹配版本和代数才清理脏状态。
- 构建命令：`dotnet build LYF_Server/MMoRpgCommon/MMoRpgCommon.csproj --no-restore --configuration Debug -p:OutputPath=D:/unitypro/LYFMMORGP/_temp/rolemanager-for-review2/common-bin/ -p:BaseIntermediateOutputPath=D:/unitypro/LYFMMORGP/_temp/rolemanager-for-review2/common-obj/`。
- 构建结果：MMoRpgCommon 成功，0 个错误、4 个既有警告。
- GameServer 构建：`dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug -p:OutputPath=D:/unitypro/LYFMMORGP/_temp/rolemanager-for-review2/bin/ -p:BaseIntermediateOutputPath=D:/unitypro/LYFMMORGP/_temp/rolemanager-for-review2/obj/`；被工作区已有 `LYF_Server/GameServer/Components/RoItemComponent.cs:631` 的 `delay_del_item_map/emplace` C++ 语法残留阻断，2 个错误，非本次改动引入。
- 格式检查：`git diff --check` 通过，仅有工作树行尾转换提示。
- 未执行：完整 GameServer 运行、真实 CenterServer ACK、登录时序及四端联调；待 `RoItemComponent.cs:631` 修复后重跑 GameServer 构建和门闩/版本行为测试。
- 临时产物：`D:/unitypro/LYFMMORGP/_temp/rolemanager-for-review2/`，待产品经理确认后清理。

## [VERIFY][构建][TR-312] 2026-09-09 角色保存版本返工最终构建

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2917—2924 行；历史时点记录，不代表当前仍在执行。

- 修改文件：`LYF_Server/GameServer/MessageHandler/Knapsack/RoleKnapsackInfoHandler.cs`，删除重复的 `using System;`。
- 静态检查：本次角色保存版本返工涉及的 7 个文件均无 `foreach` 和三目运算符；`RoleComponentContainer.GetSaveVersion` 无命中；`git diff --check` 通过。
- 构建命令：`dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug -p:OutputPath=D:/unitypro/LYFMMORGP/_temp/rolemanager-for-review-final/bin/ -p:BaseIntermediateOutputPath=D:/unitypro/LYFMMORGP/_temp/rolemanager-for-review-final/obj/`。
- 构建结果：成功，0 个错误、12 个既有警告；此前 `RoleKnapsackInfoHandler.cs` 的 `CS0105` 重复 using 警告已消失。
- 未执行：真实 CenterServer ACK、登录时序、长时间保存周期、数据库事务和 Center/Game/Gate/Unity 四端联调。
- 临时产物：`D:/unitypro/LYFMMORGP/_temp/rolemanager-for-review-final/`，待产品经理确认后清理。

## [VERIFY][背包商城][TR-313] 2026-09-10 #ItemInfo 图标路径校验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2925—2932 行；历史时点记录，不代表当前仍在执行。

- 修改文件：`Luaban/MiniTemplate/Datas/#ItemInfo.xlsx`，仅 `Icon` 列 10 条路径发生变化。
- 校验脚本：使用 openpyxl 回读表格，确认 30 条物品记录均存在；逐条检查独立 PNG 文件或图集 `.meta` 中的 Sprite 子资源名称。
- 校验结果：30/30 路径通过，失败 0；与修改前副本对比，非 `Icon` 列的值和样式均未变化。
- 临时文件：`D:/unitypro/LYFMMORGP/_temp/item-icon-fix-20260910/#ItemInfo.before.xlsx`、`validation.txt`，用于变更对比和验证记录，待产品经理确认后清理。
- 未执行：未启动 Unity/YooAsset 实机加载和重新构建资源包；需在资源包重新构建后验证图集子资源地址是否被当前 YooAsset 版本接受。

## [VERIFY][综合][TR-314] 2026-09-10 Sprite 普通资源与图集切片兼容验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2933—2940 行；历史时点记录，不代表当前仍在执行。

- 静态检查：普通 Sprite 优先，失败或为空才执行图集回退；图集通过 `LoadSubAssetsAsync<Sprite>` 和 `GetSubAssetObject<Sprite>` 获取。
- 规则检查：修改文件无 `foreach` 和三目运算符，关键分支和状态均有中文注释，`git diff --check` 无空白错误。
- 构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/yooasset-atlas-sprite-build` 成功，0 错误、21 个既有警告。
- 代码审核：通过，无阻塞问题。当前实现按 PNG 图集主资源约定解析路径。
- 未执行：尚未重建 YooAsset 资源包并在 Unity 界面确认独立 Sprite 与图集切片实际显示。

## [VERIFY][综合][TR-315] 2026-09-10 YooAsset 无效切片地址日志修复

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2941—2949 行；历史时点记录，不代表当前仍在执行。

- 复现：`LoadAssetAsync<Sprite>` 收到 `Assets/GM_Game/UISprites/Icon/Item/Icon_ItemConsumables01/UI_icon_item_Zeng.png` 时，YooAsset 在回退前输出 `The location is invalid`。
- 修复验证：`ResourceMgr.LoadSpriteAsync` 已在普通加载前调用 `CheckLocationValid`；无效虚拟路径不再传给 `LoadAssetAsync`，而是直接解析并加载图集主资源。
- API 核对：YooAsset 1.5.7 的 `ResourcePackage.CheckLocationValid` 使用当前活动清单的 `TryMappingToAssetPath`，只检查地址，不发起资源加载。
- 构建：`GM_Game.csproj` 编译成功，0 个错误；产物位于 `D:/unitypro/LYFMMORGP/_temp/yooasset-atlas-sprite-build-check2`。
- 规则检查：`ResourceMgr.cs` 无 `foreach` 和三目运算符，`git diff --check` 通过。
- 未执行：需要在当前 Unity 运行环境重新打开购买弹窗，确认控制台不再出现该无效地址错误，并确认图集切片正确显示。

## [VERIFY][界面][TR-316] 2026-09-10 客户端 UI MVC 分层审查报告

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2950—2961 行；历史时点记录，不代表当前仍在执行。

- 审查方式：扫描 UI 对网络、全局模型、配置、商城及资源管理器的依赖，并读取登录、商城、技能、背包和角色属性关键文件核对调用链。
- 通过项：`Loginview`、`CreateRoleView`、`MainView` 主要负责窗口注册和事件暴露；现有 UI Controller 已承担部分协议回包及流程协调；`BuyDialog` 数量步进与控件刷新属于合理的 View 表现逻辑。
- P1：`BuyItemWidget.cs` 与 `GoodsItemWidget.cs` 直接查询限购/货币配置、判断购买条件并调用 `ShopMgr.SendBuyRequest`；应由商城控制器准备商品 ViewModel 并提交购买。
- P1：`SkillSlotWidget.cs` 直接订阅全局输入并调用 `Global.Instance.roleCtrlBase.UseSkill`；技能释放应由技能或角色控制器处理。
- P1：`LoginWindow.cs`、`RegistWindow.cs`、`GameServerWindow.cs` 直接读写 `PlayerPrefs`、创建协议和调用 `NetSocketMgr.Client.SendData`；应移动到 `LoginCtrl` 或账号服务。
- P2：`NpcShopWindow.cs` 直接查询和筛选 Luban 商城配置；`SkillInfoWindow.cs`、`RoleCurrInfoWindow.cs`、`RoleAttriibuteWindow.cs` 及部分 Widget 直接读取 `Global`/`LubanMgr` 并完成配置映射，应由 Controller/ViewModel 层提供展示数据。
- P2：`ChatWindow.cs` 直接控制 `PlayerInputCtr`；应通过 ChatCtrl 或界面输入协调器处理游戏输入状态。
- 规范风险：本次读取到的部分历史 UI 文件仍有 `foreach` 或三目表达式；本轮未修改，后续重构触碰时必须按项目规则迁移。
- 构建：本轮为只读架构审查，未修改业务代码，未执行构建。

## [VERIFY][界面][TR-317] 2026-09-10 客户端 UI MVC 分层重构验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2962—2976 行；历史时点记录，不代表当前仍在执行。

- 架构范围：登录、选服、商城、技能、角色当前信息、装备展示、聊天、NPC 对话、主界面组合控制器和控制器释放流程。
- 修改文件范围：`SocketDispatcher.cs`、`NetSocketMgr.cs`、`ShopMgr.cs`、`CtrlBase.cs`、六个 UI Controller、六个 ViewModel、登录相关 View、主界面的商城/技能/角色/聊天/NPC/小地图 View 和 Widget、`MainView.cs`、`UIRoot.cs`；没有修改协议、服务端、场景和预制体。
- 首次构建：Unity 刷新 `GM_Game.csproj` 后，`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/mvc-main-verification-3` 成功，0 个错误、18 个既有警告。
- 首次代码审核：不通过。发现小地图主角异步创建时序、旧连接商城回包污染新请求、技能旧槽位残留、聊天异步回显清除新输入、View 持有协议/领域对象和新增注释不足等问题，已退回客户端 Agent 返工。
- 返工验证：`NetSocketMgr`、`SocketDispatcher` 和 `ShopMgr` 使用连接代次隔离网络消息与商城在途请求；`MainRoleCtrl.OnStart -> UIRoot.RegisterMainUIKeyHandler -> MainCtrl.RegisterMainInput` 会在主角创建后重新绑定小地图；技能槽和聊天输入时序问题已修正；登录服务器与 NPC 对话改用纯展示模型。
- 最终构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/mvc-refactor-final-build` 成功，0 个错误、18 个既有警告；日志位于 `_temp/mvc-refactor-final-build.log`。
- Unity 编译：`Editor.log` 显示脚本编译和热重载完成，最新日志未出现 `error CS`、`Compilation failed` 或脚本编译异常。
- 静态边界：正式 UI 对 `NetSocketMgr|SocketDispatcher|PlayerPrefs|LubanMgr|ShopMgr|Global.Instance.roleCtrlBase|PlayerInputCtr|GetServerListRet|NpcEntity` 的扫描结果为 0。
- 规则检查：`controller/UIController`、`UI/Login`、`UI/Main`、`ShopMgr`、`NetSocketMgr` 和 `SocketDispatcher` 无 `foreach`、无三目运算符；新增六个 ViewModel 均存在同名 `.meta`；`git diff --check` 无空白错误，仅有工作树行尾转换提示。
- Inspector 兼容：预制体已绑定的 `OnLoginBtnClicked`、`onGotoRegisterBtnClicked`、`OnVerifyCodeBtnClicked`、`OnGotoServerListBtnClicked`、`OnConfirmBtnClicked`、`OnSendBrnClicked` 和商品购买入口仍存在。
- 未执行：没有在 Unity Play Mode 连接真实服务器执行登录选服、断线/自动重连期间购买、小地图移动、技能移除换绑、快速连续聊天输入及重复打开商城测试；这些属于剩余运行态风险。
- 临时产物：`_temp/mvc-client-build`、`_temp/mvc-main-verification-*`、`_temp/mvc-client-refactor-fix-build`、`_temp/mvc-refactor-final-build` 及对应日志继续保留，待产品经理确认后清理。

## [VERIFY][综合][TR-319] 2026-09-11 ClientRole 与 RoleWorldSystem 验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2985—2994 行；历史时点记录，不代表当前仍在执行。

- 修改范围：客户端 `FunlModule/Role`、`FunlModule/Component`、背包/次数/商城角色依赖，以及 `Global`、开始游戏、主城、网络断线、商城、主控制器和角色信息控制器入口。
- 类型检查：新增 `ClientRole.cs`、`RoleWorldSystem.cs` 及对应 `.meta`，一类一文件；客户端 `OnlineRole` 文件和引用均已移除。
- 职责检查：未发现 `RoleNetSender`、`RoleDataFieldManager`、`GateSessionId`、`UnitySessionId`、`SaveAllMoudle`、`OnSaveAcknowledged` 等服务端持久化或会话符号残留。
- 入口检查：开始游戏创建 `LocalRole`；背包完整回包装载 `RoItemComponent` 与 `RoleCountComponent`；其他玩家同步进入角色世界索引；断线和应用退出清理角色世界。
- 规则检查：本次目标文件无 `foreach`、无三目运算符；唯一 `?` 命中为 `KnapsackType?` 可空值类型，不是三目表达式；`git diff --check` 无空白错误，只有工作区行尾转换提示。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/client-role-final-build`。
- 构建结果：成功，0 个错误、17 个既有警告；构建产物保留在 `_temp/client-role-final-build`，等待产品经理确认后清理。
- 未执行：Unity Play Mode 和真实服务器联调；开始游戏与背包快照消息时序、其他玩家退出、断线自动重连、切换角色仍属于运行态验证缺口。

## [VERIFY][网络][TR-320] 2026-09-11 RoleKanpsackInfo 协议结构统一验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2995—3005 行；历史时点记录，不代表当前仍在执行。

- 协议检查：`ResultEntity.proto` 中 `RoleKanpsackInfoRet` 仅保留结果码和嵌套 `RoleKanpsackInfo`；嵌套结构包含容量列表、四类背包、当前装备栏、虚拟物品背包和 `save_version`。`MainRoleInfo` 的字段 3（money）及其名称已 reserved，`StartGameRet` 的旧容量字段已 reserved。
- 登录链路检查：CenterServer 的 `CentRoleModel.RoleKanpaskInfo` 统一向嵌套对象写入背包、虚拟货币、容量、次数和保存版本；GameServer 仅由 `RoleKnapsackInfoHandler` 装载嵌套对象；开始游戏处理器不再读取旧容量字段；Gate 按外层 `RoleKanpsackInfoRet.CmdCode` 校验。
- 规则检查：`CentRoleModel.cs`、`LoginModle.cs`、`Game_LoginCtrl.cs`、生成协议文件和 proto 文件扫描 `foreach` 与三目运算符均为 0；`MainRoleInfo` money 读写扫描为 0。
- 协议生成：使用 `LYF_Server/ProtoBuf/bin/protoc.exe` 重新生成 `ResultEntity.cs` 到 `_temp/role-kanpsack-proto-gen`，并更新 `LYF_Server/Network/Proto/ResultEntity.cs`。
- 构建：Network 成功（0 错误，4 个既有警告）；GameServer 成功（0 错误，14 个既有警告）；CenterServer 成功（0 错误）；GateServer 成功（0 错误，4 个既有警告）；Unity `GM_Game.csproj` 成功（0 错误，21 个既有警告）。
- 客户端协议：已将新构建的 `Network.dll` 和 `MMoRpgCommon.dll` 更新到 `LYF_MMO/Assets/GM_Game/Plugins/Net` 与 `Plugins/Common`，使 Unity 使用嵌套结构和 `int64 ItemUid`。
- 未执行：未连接真实 MySQL、未进行 Center→Game→Gate→Unity 登录联调；`_temp/role-kanpsack-*` 构建产物待产品经理确认后清理。
- 补充修复：`GateClientResponseForwarder` 对商城回包缺少 `ErrorInfo` 的分支改为 `if/else`，去除三目表达式；GateServer 已重新构建通过（0 错误，4 个既有警告）。

## [VERIFY][背包商城][TR-321] 2026-09-11 背包 MVC 最终测试复核

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3006—3014 行；历史时点记录，不代表当前仍在执行。

- 构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/bag-mvc-final-test/client` 成功，0 个错误、17 个既有警告；`GameServer.csproj` 使用隔离输出成功，0 个错误、10 个既有警告。日志保存在 `_temp/bag-mvc-final-test/client-build.log` 和 `server-build.log`。
- 静态检查：背包 MVC 目标文件 `foreach=0`、三目运算符 `?:=0`；目标文件均包含中文注释/说明。`KnapsackWindowMockServer` 和 `MockAddItemButton` 在正式 `UIRoot.prefab` 中命中数均为 0。
- 隔离行为测试：基于最新客户端构建产物重新编译运行 `_temp/bag-mvc-final-smoke/bin/KnapsackMvcSmoke.exe`，输出“通过：初始81格、扩容、删除、穿戴事件链路”。覆盖四类背包初始 81 格、扩容到 90 且事件单次通知、部分删除数量变化和跨容器穿戴移动。
- 回归边界：角色切换/断线清理通过代码路径核对：`KnapsackCtrl.BindRole` 解绑旧组件后绑定新组件，`Dispose` 解除事件，`RoleWorldSystem.Clear` 释放本地角色并触发 `LocalRoleChanged`；`MainCtrl` 监听并重新绑定。`RoItemComponent.ReduceItem` 当前代码已走 `DeleteItem` 扣除路径，但尚未使用真实配置单独运行回归。未启动 Unity Play Mode，未进行真实服务器联调；`LoadKnapsack` protobuf 快照路径未在普通 .NET 烟测执行（Unity `System.Memory` 依赖冲突），已完成静态检查。
- 空白检查：目标代码 `git diff --check` 无业务代码空白错误；仅 `UIRoot.prefab` 存在由 Unity 序列化产生的尾随空格提示，属于既有/资源格式内容，未改动清理。
- 临时产物：`D:/unitypro/LYFMMORGP/_temp/bag-mvc-final-test/`、`_temp/bag-mvc-final-smoke/`，用途为构建与隔离行为验证，待产品经理确认后清理。

## [VERIFY][背包商城][TR-322] 2026-09-11 背包 MVC 改造最终审核与测试

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3015—3023 行；历史时点记录，不代表当前仍在执行。

- 代码审核：通过。审核确认 `KnapsackWindow` 仅负责展示，`KnapsackCtrl` 负责数据转换，`RoItemComponent` 负责背包状态；角色切换和断线清理会解除旧组件事件订阅。
- 构建：`GM_Game.csproj` 成功，0 错误、17 个既有警告；`GameServer.csproj` 成功，0 错误、10 个既有警告。产物位于 `_temp/bag-mvc-final-test/`。
- 隔离烟测：使用最新客户端程序集重编并运行 `_temp/bag-mvc-final-smoke/bin/KnapsackMvcSmoke.exe`，退出码 0，输出“通过：初始81格、扩容、删除、穿戴事件链路”。
- 静态检查：目标文件 `foreach=0`、三目运算符 `?:=0`，关键状态和函数均有中文注释；正式 `UIRoot.prefab` 未发现 `KnapsackWindowMockServer` 或 `MockAddItemButton`。
- 未执行：Unity Play Mode、Center/Game/Gate/Unity 真实联调、真实数据库和 protobuf 快照运行态验证；`ReduceItem` 尚未使用真实配置进行单独回归。
- 空白检查：业务代码无新增空白错误；`UIRoot.prefab` 仅有 Unity 序列化空字段产生的尾随空格提示，未进行无关清理。

## [VERIFY][综合][TR-323] 2026-09-11 RolePackPlain 独立容器语义复核

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3024—3031 行；历史时点记录，不代表当前仍在执行。

- 变更范围：客户端 `KnapsackCtrl.cs`、`KnapsackWindow.cs` 的页签展示语义；未修改测试脚本和服务端业务代码。
- 静态结果：`RolePackPlain` 页面只读取普通背包容器，不再追加装备、消耗品和材料；展示位置使用物品真实 `BagIndex`。
- 构建：`GM_Game.csproj` 成功，0 错误、21 个既有警告；产物位于 `D:/unitypro/LYFMMORGP/_temp/knapsack-plain-build/`。
- 规则检查：目标文件无 `foreach`、无三目运算符，`git diff --check` 通过。
- 未执行：本轮未改服务端协议/回包，因此未进行服务端联调；历史服务端兼容聚合逻辑需后续单独迁移任务处理。

## [VERIFY][背包商城][界面][TR-324] 2026-09-11 背包 UI 增量刷新验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3032—3039 行；历史时点记录，不代表当前仍在执行。

- 构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/bag-incremental-build2` 成功，0 个错误、17 个既有警告。
- 静态检查：`RoItemComponent`、`KnapsackCtrl`、`KnapsackWindow`、`KnapsackSlotWidget` 和新增背包变更类型文件无 `foreach`、无三目运算符；变更事件仅携带不可变基础字段，不向窗口传递 `ItemBase`。
- 实现检查：新增/删除/数量/移动事件按 UID 和格位增量更新；数量变化由 `KnapsackSlotWidget.RefreshCount` 只更新数量文本；登录、切页和全量快照仍使用完整渲染。
- 未执行：Unity Play Mode、真实服务器联调和真实资源图标加载；需要后续测试组件覆盖可见格与非可见格变更。
- 临时产物：`D:/unitypro/LYFMMORGP/_temp/bag-incremental-build2/`，待产品经理确认后清理。

## [VERIFY][背包商城][界面][TR-325] 2026-09-11 背包 UI 增量刷新最终复核

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3040—3047 行；历史时点记录，不代表当前仍在执行。

- 代码范围：`RoItemComponent`、`KnapsackCtrl`、`KnapsackWindow`、`KnapsackSlotWidget` 及两个背包变更快照类型。
- 静态核对：新增/删除/数量变化/移动路径均直接调用局部刷新；只有角色绑定、切页和完整快照调用 `RefreshCurrentCategory`；数量变化调用 `RefreshCount`，不重新加载图标。
- 构建：`GM_Game.csproj` 成功，0 错误、17 个既有警告，产物位于 `D:/unitypro/LYFMMORGP/_temp/knapsack-incremental-build/`。
- 规则检查：目标文件无 `foreach`、无三目运算符；变更事件不暴露可变 `ItemBase`；新增类型各自独立文件并有中文注释。
- 未执行：Unity Play Mode、真实四端联调、资源包图标加载和完整测试场景回归。

## [VERIFY][背包商城][界面][TR-326] 2026-09-11 背包 UI 增量刷新继续验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3048—3054 行；历史时点记录，不代表当前仍在执行。

- 格式清理：客户端 Agent 清理 `KnapsackWindow.cs` 两处尾随空格，并合并重复 XML `summary` 注释。
- 构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/knapsack-incremental-final` 成功，0 个错误、17 个既有警告。
- 静态检查：`RoItemComponent`、`KnapsackCtrl`、`KnapsackWindow`、`KnapsackSlotWidget` 及变更快照类型无 `foreach`、无三目运算符；目标范围 `git diff --check` 通过。
- 隔离烟测：运行 `_temp/bag-mvc-final-smoke/bin/KnapsackMvcSmoke.exe` 成功，输出“通过：初始81格、扩容、删除、穿戴事件链路”。
- 未执行：Unity Play Mode、真实四端联调、真实资源包图标加载；上述属于环境联调项，不影响本次代码构建验收。

## [VERIFY][背包商城][界面][TR-327] 2026-09-11 背包 UI 增量刷新专项测试

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3055—3065 行；历史时点记录，不代表当前仍在执行。

- 最新客户端构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/bag-incremental-specialized-build` 成功，0 个错误；仅报告既有依赖版本和弃用 API 警告。
- 详细事件烟测：在 `_temp/bag-incremental-detailed-smoke` 使用最新 `GM_Game.dll` 编译运行，退出码 0。验证容量变更、新增物品、堆叠数量部分删除、完整删除、同容器移动、跨容器移动均各产生一次对应 `KnapsackChangedEventArgs`，背包类型、旧/新格位和数量字段正确。
- 基础回归烟测：`_temp/bag-mvc-final-smoke/bin/KnapsackMvcSmoke.exe` 退出码 0，初始 81 格、扩容、删除、穿戴事件链路通过。
- 增量入口静态检查：`KnapsackCtrl.ApplyIncrementalChange` 调用 `RefreshSlot` 4 次且不调用整页 `Render`；`KnapsackWindow.RefreshSlot` 不调用整页 `Render`；数量变化最终调用 `KnapsackSlotWidget.RefreshCount`，避免重复加载图标。
- 规则检查：目标文件 `foreach=0`、三目运算符 `?:=0`；`KnapsackWindow.cs`、控制器和事件快照无新增尾随空格；目标范围 `git diff --check` 通过。
- 非可见格覆盖：代码路径确认 `RefreshSlot` 先更新 `_displayItemsBySlot` 缓存，再按可见范围决定是否绑定 Slot，后续滚动会从缓存读取；未在 Unity Play Mode 实测滚动表现。
- 容量变化说明：`UpdateCapacity` 会重新计算虚拟列表布局并强制刷新当前可见 Slot；这是容量变化后的布局重绑定，不会重建整页数据或重新加载非可见格图标。
- 未执行与风险：未启动 Unity Play Mode、未进行真实 Center/Game/Gate/Unity 联调、未验证 YooAsset 实际图标加载；拖拽移动的 UI 入口仍需运行态验证，真实配置下 `ReduceItem` 扣除路径也未单独回归。
- 临时产物：`D:/unitypro/LYFMMORGP/_temp/bag-incremental-specialized-build`、`_temp/bag-incremental-detailed-smoke`，用途为本次构建和隔离烟测，待产品经理确认后清理。

## [VERIFY][背包商城][TR-329] 2026-09-11 商城购买客户端改造代码审核

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3073—3081 行；历史时点记录，不代表当前仍在执行。

- 审核范围：`ShopMgr.cs`、`ShopCtrl.cs`、`MainCtrl.cs`。
- 审核结论：通过。`ShopMgr` 未缓存货币余额或购买次数；发包前分别从本地角色的 `RoItemComponent` 和 `RoleCountComponent` 读取并校验。
- 回包行为：`HandleResponse` 只解析 `BuyShopItemRes.Error`、清理在途状态并触发 `PurchaseCompleted`，没有根据请求本地扣除货币或增加购买次数；`ShopCtrl` 负责提示和成功后的商城显示刷新。
- 规范检查：目标文件中文说明完整，`foreach=0`，三目运算符=0，`git diff --check` 通过。
- 构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/shop-review-build` 成功，0 个错误、17 个既有警告。
- 非阻塞建议：`SendBuyRequest` 可补充校验 `ShopItemInfo.ShopId == shopTableId`，避免调用方传入跨商店商品；该校验不替代服务端权威校验。
- 未执行：Unity Play Mode 真实商城购买、服务器四端联调和服务端扣款回包刷新验证。

## [VERIFY][网络][背包商城][构建][TR-330] 2026-09-11 商城服务端同步构建

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3082—3087 行；历史时点记录，不代表当前仍在执行。

- 命令：`dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/purchase-sync-build3`。
- 结果：构建成功，0 个错误、8 个既有警告。
- 静态约束：本次触碰的商城处理器、物品组件、事务类和次数组件无 `foreach`、无三目运算符；新增同步逻辑含中文注释。
- 未执行：Center/Gate/Game/Unity 真实购买联调，需验证客户端对 `UpdateItemRet` 和 `BuyShopItemRes` 快照的合并行为。

## [VERIFY][背包商城][TR-331] 2026-09-11 移除商城购买回包重复限购读取验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3088—3092 行；历史时点记录，不代表当前仍在执行。

- 静态核对：`ShopStatic.BuyItems` 调用 `RoleShopRecord.UseBuyTimes`，该路径通过 `RoleCountComponent` 发送次数变化；本次要求删除 `RequestBuyShopItem` 成功响应中的重复次数读取。
- 待验证：后端 Agent 修改后重新构建 GameServer 和 GM_Game，并确认客户端不再依赖 `BuyShopItemRes.RemainingPurchaseCount`。

## [VERIFY][网络][TR-332] 2026-09-11 客户端次数同步验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3093—3099 行；历史时点记录，不代表当前仍在执行。

- 协议：更新客户端 `Network.dll`，使用只包含 `ErrorInfo` 的 `BuyShopItemRes`。
- 客户端：注册 `CMD_CountUpdateSyncInfoCode`，解析 `CountUpdateSyncInfo` 后写入 `RoleCountComponent`，由 `CountChanged` 驱动商城显示。
- 构建：Network 0 错误；GM_Game 0 错误、17 个既有警告。
- 静态检查：客户端不再读取购买回包的剩余次数或虚拟背包字段；真实购买联调仍未执行。

## [VERIFY][网络][背包商城][TR-333] 2026-09-11 商城购买物品、货币与限购同步验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3100—3105 行；历史时点记录，不代表当前仍在执行。

- GameServer 构建：`dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/purchase-final-build`，0 错误，8 个既有警告。
- GM_Game 构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/purchase-client-final`，0 错误，17 个既有警告。
- 静态检查：购买请求的回滚遍历已使用 `for`；客户端商城不缓存角色货币和限购数据，服务器结果分别写入虚拟物品组件和次数组件。
- 未执行：真实服务器四端购买联调、数据库落库校验和 Unity Play Mode；需要重点验证多商品请求的限购回包是否逐商品返回（当前协议只提供单个 `remaining_purchase_count`）。

## [VERIFY][综合][TR-334-FILES] 查询任务的正式文档产物：2026-09-12 客户端脏标记设计文档检查

- 该任务记录的正式文档路径：`项目分析/CLIENT_DIRTY_MARK_DESIGN.md`。
- 查询正文、当时状态及原文验证边界见 `Docs/AI/QUERY_TASKS.md` 的 `[TR-334]`；本条只保留文件产物追溯，不新增完成或检查结论。

## [VERIFY][综合][TR-335] 2026-09-12 客户端脏标记机制实现测试

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3112—3113 行；历史时点记录，不代表当前仍在执行。


## [VERIFY][综合][TR-336] 2026-09-12 客户端脏标记返工复验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3114—3115 行；历史时点记录，不代表当前仍在执行。


## [VERIFY][综合][TR-337] 2026-09-12 CenterServer Unsafe 依赖修复验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3116—3141 行；历史时点记录，不代表当前仍在执行。

- 根因确认：`CenterServer.csproj` 引用 6.0.0.0，而原 `CenterServer/bin/Debug/System.Runtime.CompilerServices.Unsafe.dll` 实际为 4.0.4.1；启动时触发程序集清单不匹配。
- 后端修复：统一 CenterServer 使用 `packages/System.Runtime.CompilerServices.Unsafe.6.0.0/lib/net461`，启用精确版本和私有复制，避免旧 DLL 残留。
- 构建：`dotnet build LYF_Server/CenterServer/CenterServer.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/center-unsafe-build`，0 个警告、0 个错误。
- 运行验证：从 `_temp/center-unsafe-build` 启动 CenterServer，进程保持运行超过 3 秒，未出现 Unsafe 加载异常或 `AccoutTable` CodeFirst 异常；随后主动停止测试进程。
- 未解决：未连接实际 MySQL 数据库执行完整建表验证；原 `bin/Debug` 目录需使用最新构建产物启动。

- 客户端 Agent 修复了模块聚合器误调用、旧格位漏标记及旧事件绕过 UI 脏刷新问题。
- `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/dirty-mark-final3`：0 个错误，仅既有警告。
- 静态检查：目标代码未发现 `foreach` 或三目运算符；商城和背包均通过窗口激活帧消费脏状态。
- 代码审核复验：通过。未执行 Unity Play Mode、真实服务器四端联调。

- 构建：执行 `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/dirty-mark-test-build`，结果成功，0 个错误；仅有 Unity 依赖版本冲突、过时 API 和隐藏成员等既有警告。
- 工程文件：`GM_Game.csproj` 已包含 `ClientDirtyModule.cs`、`ClientDirtyTracker.cs`、`ClientDirtySnapshot.cs`、`ClientChangeContext.cs` 四个脏标记类型文件。
- 静态规则：脏标记、背包、次数、商城和窗口控制器目标文件扫描 `foreach` 为 0；三目运算符模式为 0。空值条件运算符和可空类型不属于三目表达式。
- 延迟刷新核对：`RoItemComponent.NotifyKnapsackChanged` 先更新模型并写入角色背包页脏状态；正式代码未发现 `KnapsackChangedDetailed` 的 UI 订阅。`KnapsackCtrl` 仅在窗口帧事件且窗口可见时消费当前页脏快照，支持完整刷新和指定格位刷新，消费后清除标记。
- 次数模块核对：`RoleCountComponent.NotifyChanged` 写入 `ClientDirtyModule.RoleCount`；`ShopCtrl` 在商城窗口帧事件中检查并消费次数脏标记，重新生成限购展示。当前未发现其他界面对 `CountChanged` 的订阅。
- 跨模块支持：`ClientChangeContext` 保存事务编号和来源；跨背包移动会同时标记源页与目标页，为拍卖行等后续交换预留上下文。
- 生命周期核对：`ClientRole.Dispose` 清理全部脏状态；控制器解绑角色时清空本地缓存，避免切换角色或断线后旧标记污染新角色。
- 未执行：Unity Play Mode、真实四端联调、商城实际购买、滚动到非可见格后的运行态刷新仍需后续环境测试。
- 风险项：`RoItemComponent.NotifyKnapsackChanged` 仍保留并触发历史 `KnapsackChangedDetailed`/`KnapsackChanged` 事件；当前正式脚本未发现 UI 订阅，但后续新增订阅必须仅用于模型层，不能直接刷新窗口。
- 风险项：`ShopCtrl.OnPurchaseCompleted` 在购买成功回包时会主动调用 `RefreshShopWindow`；若严格要求所有商城限购展示均通过 `RoleCount` 脏标记延迟消费，应由客户端 Agent 移除该即时刷新或限定为提示层不重建商品列表。
- 风险项：同一背包内移动仅标记目标格位；若旧格位 UI 仍缓存旧物品，需要同时标记旧格位或改为完整刷新。跨背包移动已同时标记源页和目标页。

## [VERIFY][综合][TR-338] 2026-09-12 CenterServer Unsafe 版本冲突专项验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3142—3150 行；历史时点记录，不代表当前仍在执行。

- 根因核对：`CenterServer.csproj` 引用 `System.Runtime.CompilerServices.Unsafe.6.0.0`，但旧输出目录中的 DLL 清单为 4.0.4.1，导致 SqlSugar CodeFirst 加载时程序集引用不匹配。
- 修改文件：`LYF_Server/CenterServer/CenterServer.csproj`。Unsafe 引用增加 `SpecificVersion=True` 和 `Private=True`，确保 6.0.0.0 DLL 被复制到 CenterServer 输出目录。
- 构建命令：`dotnet build LYF_Server/CenterServer/CenterServer.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/unsafe-center-build-final`。
- 构建结果：成功，0 个错误、0 个警告。
- 输出验证：`_temp/unsafe-center-build-final/System.Runtime.CompilerServices.Unsafe.dll` 和 `CenterServer/bin/Debug/System.Runtime.CompilerServices.Unsafe.dll` 均为 `Version=6.0.0.0`；生成配置包含重定向到 6.0.0.0。
- 未执行：真实数据库连接、SqlSugar CodeFirst 初始化和 CenterServer 长时间运行验证。

## [VERIFY][综合][TR-339] 2026-09-12 CenterServer CodeFirst 实际启动复验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3151—3152 行；历史时点记录，不代表当前仍在执行。


## [VERIFY][综合][TR-340] 2026-09-12 GateServer/GameServer 购买后断线修复验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3153—3154 行；历史时点记录，不代表当前仍在执行。


## [VERIFY][网络][TR-341] 2026-09-12 未注册协议日志验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3155—3173 行；历史时点记录，不代表当前仍在执行。

- 代码审核：后端代理确认修改范围正确，未注册协议会记录协议号并安全丢弃，不再因字典异常断开连接。

- `NetClient` 和 `Session` 对未知协议改用 `TryGetValue`，现在会输出 `command not regist, protoCode=协议号` 的 Warn 日志，并丢弃该包，不再抛字典异常导致 `OnReceiveCB` 主动断开。
- 同时修复了连接异常日志中的条件表达式，改为统一错误码方法，符合项目禁止三目运算符约束。
- GateServer 构建：0 个警告、0 个错误；GameServer 构建：0 个错误（既有 7 个警告）。
- 代码审核：通过；未执行真实未知协议注入和四端联调。

- 日志分析：购买成功后的 `11150` 次数同步未在 GateServer 的客户端命令表注册，导致 `NetClient.HandleCommand` 字典下标异常，`OnReceiveCB` 随后断开连接；`10054` 是断开结果，不是购买业务错误。
- 修改：GateServer 注册 `CMD_CountUpdateSyncInfoCode`，网关对 11150 使用 `CountUpdateSyncInfo` 解析校验后透传；GameServer 明确填写同步结果 `CmdCode=Succeed`。
- 构建：GateServer 0 个警告、0 个错误；GameServer 0 个错误（7 个既有警告）。产物位于 `_temp/gate-count-fix-build` 与 `_temp/game-count-fix-build`。
- 代码审核：通过，确认 11150 注册、协议校验和中文注释完整。
- 未执行：真实商城购买四端联调；需使用新构建覆盖运行目录后复测购买、次数同步、物品同步和断线重连。
- 返工复验：清理 `GateServerApp.cs` 尾随空格后，`git diff --check` 通过；GateServer 最终构建 0 个警告、0 个错误，产物位于 `_temp/gate-count-fix-final`。

- 环境核对：MySQL `127.0.0.1:3308` 端口可连接，符合 `DBMgr` 的当前连接配置。
- 从 `D:/unitypro/LYFMMORGP/_temp/center-unsafe-build` 启动 CenterServer，运行 4 秒后进程仍存活，输出已监听 `127.0.0.1:10110`。
- 结果：`DBMgr.initDB()` 中的 `SqlSugar.CodeFirst.InitTables` 已实际执行，未再出现 Unsafe 加载异常或 `AccoutTable` 建表异常；测试进程已主动停止。

## [VERIFY][综合][TR-342] 2026-09-13 Unity 主城卡顿诊断

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3174—3183 行；历史时点记录，不代表当前仍在执行。

- 范围：只读检查 `New Scene` 与 `Scene_MainCity`，未修改项目文件、场景、预制体或资源。
- 硬件：Intel Core i9-14900HX；NVIDIA GeForce RTX 4070 Laptop GPU，显存约 8GB；系统内存约 32GB。当前证据不足以判定硬件不足。
- 场景静态检查：两个场景均启用烘焙光照设置，但 `m_LightingDataAsset` 引用空 GUID；`New Scene` 有两盏 Directional Light、PostProcessVolume、Main Camera 和大量模型/角色表现对象。
- 运行检查：`New Scene` Play Mode 初始化期间 MCP 连接失联；Unity 日志确认 YooAsset 执行模拟构建，存在 Unknown 脚本，`NpcManager.Awake` 在 YooAsset 包初始化完成前调用 `LoadPrefabAsync`，抛出 `Package initialize not completed`；同时存在服务端未启动导致的 10061 重连日志。
- 资源检查：`Assets/GM_Game/Scenes/MainCity_Res` 约 731 个文件、964MB，PNG 约 850MB；大贴图最高超过 30MB，存在集中加载和显存上传压力。
- 验收结果：已完成场景、日志、硬件和进程证据采集；未执行 Profiler 帧采样、重新烘焙或任何修复。Unity MCP 在 Play Mode 中失联，因此运行时统计不完整。
- 交接建议：客户端 Agent 后续先处理测试脚本初始化顺序/缺失脚本，再验证光照烘焙；之后用 Profiler 区分 CPU 初始化、GPU 渲染和资源加载耗时。

## [VERIFY][综合][TR-343] 2026-09-13 运行帧 Statistics 补充

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3184—3190 行；历史时点记录，不代表当前仍在执行。

- 数据来源：用户提供的 Unity 运行时 Statistics 截图。
- 关键数据：31.5 FPS（31.7ms/帧）；CPU Main 31.7ms；Render Thread 8.4ms；Batches 1718；SetPass Calls 1032；Shadow Casters 903；Tris 6.1M；Verts 7.3M；Visible Skinned Meshes 61。
- 结论：当前卡顿首先是主线程帧耗时超预算，优先排查脚本 Update、Animator、CharacterController/Physics 和阴影相关准备；渲染线程明显低于主线程，暂不支持“显卡性能不足”为主因。
- 风险提示：Statistics 不能拆分主线程耗时，仍需 Unity Profiler Timeline 进一步确认具体函数；本次没有修改项目文件。

## [VERIFY][综合][TR-344] 2026-09-13 MainCity_Geometry 与灯光 A/B 验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3191—3198 行；历史时点记录，不代表当前仍在执行。

- 用户实测：关闭 `MainCity_Geometry` 后帧率上升；关闭灯光后帧率上升。
- 结论：已确认卡顿与主城几何和灯光处理强相关。结合 903 个 Shadow Casters，实时阴影和几何剔除/提交是当前最高优先级排查项。
- 下一步：保留灯光，仅将 Directional Light 的阴影关闭进行对照；然后逐步降低阴影距离、分辨率和级联数量，并检查远景几何是否仍投射阴影。不能仅凭该 A/B 结果断定所有耗时都来自灯光，仍需 Profiler Timeline 定位。
- 本次没有修改项目文件。
- 静态复核：`New Scene` 存在两个启用方向光，其中一个 `m_Shadows.m_Type=2`（软阴影）；场景 `m_LightingDataAsset` 和 `m_OcclusionCullingData` 均为空引用。该组合可解释关闭灯光后帧率上升，但仍需 Profiler 对照确认各项耗时比例。

## [VERIFY][综合][TR-345] 2026-09-13 Occlusion Culling 操作说明

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3199—3204 行；历史时点记录，不代表当前仍在执行。

- 本次为只读指导，没有修改场景或生成烘焙数据。
- 已明确步骤：对象标记 Occluder/Occludee Static；Window > Rendering > Occlusion Culling > Bake；Main Camera 勾选 Occlusion Culling；从同一相机位置对比 FPS、Batches、SetPass、Shadow Casters。
- 限制：遮挡剔除只影响渲染可见性；如果 `MainCity_Geometry` 是单个大网格，或场景没有有效遮挡数据，收益会很小；碰撞体仍需单独优化。

## [VERIFY][综合][TR-346] 2026-09-13 Occlusion Culling A/B 复核

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3205—3210 行；历史时点记录，不代表当前仍在执行。

- 用户实测：开启遮挡剔除后帧率变化不明显。
- 静态检查：`MainCity_Geometry.prefab` 约 681 个 Transform，属于大型嵌套预制体；内部包含多个实时灯光。当前场景仍缺少有效 `LightingDataAsset`。
- 结论：遮挡剔除不是当前主要瓶颈；关闭灯光后帧率上升更符合实时灯光/光照 Pass 成本。后续应优先烘焙静态灯光、减少实时点光源、关闭非关键阴影，再用 Profiler 验证。
- 本次未修改场景、预制体或资源。

## [VERIFY][构建][场景实体][TR-347] 2026-09-13 共享实体系统构建验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3211—3218 行；历史时点记录，不代表当前仍在执行。

- `dotnet build LYF_Server/MMoRpgCommon/MMoRpgCommon.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/entity-common-namespaced`：0 错误，4 个既有警告。
- `dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/entity-game-namespaced`：0 错误，7 个既有警告。
- 新增共享/服务端实体代码静态检查：`foreach=0`，三目运算符=0，实体顶级类型按独立文件拆分。
- 客户端构建暂未通过：工程引用旧版 `MMoRpgCommon.dll`，导致新增 `MMoRpgCommon.SceneEntity` 类型不可见；需更新插件 DLL 后复验。
- 未执行 Unity Play Mode 和真实四端场景同步联调。

## [VERIFY][场景实体][TR-348] 2026-09-13 客户端角色实体命名迁移验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3219—3229 行；历史时点记录，不代表当前仍在执行。

- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/entity-client-check2`。
- 构建结果：成功，0 个错误，26 个既有警告；输出已写入 `_temp/entity-client-check2/`。
- 静态检查：客户端业务范围未发现 `ClientRole`、`RoleWorldSystem`、`RoleWorld` 或公开 `.EntityWorld` 旧引用；未发现 `foreach` 或三目运算符。
- 类型核对：本地角色使用 `PlayerEntity`，其他玩家使用 `RoleEntity`，`SceneWorldSystem` 内部统一驱动场景实体世界并通过实体 ID 绑定表现组件。
- 未执行：Unity Editor Play Mode、主城资源加载、网络四端实体同步和实体组件运行时联调。

- 复验构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/entity-client-final-acceptance` 成功，0 个错误，26 个既有警告。
- Unity MCP 编辑器复核：请求脚本刷新并编译后，编辑器状态为空闲且控制台错误数为 0；未进入 Play Mode。

## [VERIFY][场景实体][TR-349] 2026-09-13 PlayerEntity/RoleEntity 实体继承最终验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3230—3239 行；历史时点记录，不代表当前仍在执行。

- 静态类型核对：`PlayerEntity : ROObject`、`RoleEntity : ROObject` 均位于 `Assets/GM_Game/Scripts/SceneEntity/Entity`，未发现 `FunlModule/Role` 下重复同名类型。
- 管理器核对：`SceneWorldSystem` 直接使用单个 `SceneEntityWorld` 管理实体；未发现 `PlayerEntityId`、`BindPlayerEntity`、公开 `SceneEntityWorld EntityWorld`、`ClientRole` 或 `RoleWorldSystem` 残留。
- 规则扫描：客户端实体范围无 `namespace` 声明、`foreach` 或三目运算符；实体和实体组件按独立源文件组织。
- 构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/player-entity-fix3`，0 个错误。
- Unity 验证：刷新并请求脚本编译后，`mcpforunity://editor/state` 显示编辑器空闲且 `is_compiling=false`；`read_console(types=["error"])` 返回 0 条错误。
- 未执行：Unity Play Mode、NPC/角色实体运行时生成、真实网络实体同步和四端联调。
- 临时产物：`_temp/player-entity-fix3/`、`_temp/player-entity-build3.log`，用途为本轮客户端构建和日志记录，待产品经理确认后清理。

## [VERIFY][场景实体][TR-350] 2026-09-14 客户端实体事件系统验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3240—3249 行；历史时点记录，不代表当前仍在执行。

- 参考核对：已阅读 `F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Event/MEventType.cs`、`MEventDispatcher.cs` 与 `Entity/MObject.cs`。
- 构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/entity-event-build` 成功，0 个错误，31 个既有工程警告。
- 静态规则：`SceneEntity` 未发现 `namespace`、`foreach` 或三目运算符；新增顶级类型均独立同名文件。
- 行为静态核对：重复监听被拒绝；派发使用监听快照；回调异常隔离；实体销毁前后派发事件并清空监听。
- 输入边界：键盘、鼠标、触摸和轴事件由 `EntityInputEventBridge` 接收输入层调用后派发，未在 `ROObject` 中轮询 Unity `Input`。
- 未执行：Unity Play Mode、实际输入控制器转发和服务器同步领域事件联调。
- 临时产物：`_temp/entity-event-build/`，用途为本轮客户端构建，待产品经理确认后清理。

## [VERIFY][场景实体][TR-351] 输入桥接补充验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3250—3257 行；历史时点记录，不代表当前仍在执行。

- `PlayerInputCtr` 已在不修改自动生成 `PlayerInput.cs` 的前提下，将 Shift、Jump 和 Movement 的 Input System 回调，以及 `Keyboard.current.onTextInput` 通过 `EntityInputEventBridge` 投递给当前 `PlayerEntity`。
- 空状态安全：`Global.Instance`、`SceneWorld` 或本地玩家不存在时会直接返回，不影响输入控制器既有事件。
- 复验构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/entity-event-input-build` 成功，0 个错误，31 个既有工程警告。
- 静态核对：未修改自动生成 `PlayerInput.cs`；`PlayerInputCtr` 没有新增 `namespace`、`foreach` 或三目运算符；Shift、Jump 和 Movement 的目标回调均调用事件桥接。
- 临时产物：`_temp/entity-event-input-build/`，用途为输入桥接复验构建，待产品经理确认后清理。

## [VERIFY][场景实体][TR-352] 2026-09-14 实体事件强类型参数重构验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3258—3265 行；历史时点记录，不代表当前仍在执行。

- 构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/entity-event-strongly-typed-build` 成功，0 个错误、31 个既有警告。
- 静态扫描：`LYF_MMO` 内 `MEventArgs` 引用为 0；事件目录和直接调用文件未发现 `object`、`object[]`、`dynamic`、`Init(params object[])`、`foreach`、三目运算符或新增命名空间。
- 行为核对：无载荷事件传递 `null`；有载荷事件按监听器克隆为不可变具名参数；分发器不自动调用调用方载荷的 `Recycle`，避免动作状态组件暂存事件时发生对象池复用污染。
- 未执行：Unity Play Mode、真实键盘/鼠标/触摸和 Input Action 输入回归；未处理工程既有的依赖版本、类型冲突和过时 API 警告。
- 临时产物：`_temp/entity-event-strongly-typed-build/`，用于本轮客户端构建，待产品经理确认后清理。

## [VERIFY][场景实体][TR-353] 审核返工复验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3266—3272 行；历史时点记录，不代表当前仍在执行。

- 问题：`PlayerInputCtr` 曾将 `InputActionEventArgs` 派发至 `InputKey*` 和 `InputAxisChanged`，造成同一事件类型拥有不同参数类型。
- 修复：新增 `InputActionStarted`、`InputActionPerformed`、`InputActionCanceled`；Shift、Jump、Movement 的 Input System 回调全部迁移到动作专用事件。输入桥会拒绝将 Input Action 参数派发到其他事件。
- 构建：同一构建命令复验成功，0 个错误、31 个既有警告。
- 静态核对：`PlayerInputCtr` 向旧键盘/输入轴事件派发 Input Action 的匹配为 0；`InputKey*` 仅通过 `KeyInputEventArgs` 派发，`InputAxisChanged` 仅通过 `InputAxisChangedEventArgs` 派发；目标范围禁用模式仍为 0。

## [VERIFY][技能][TR-354] 2026-09-14 RO 风格 MModel 关系重设计检查

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3273—3281 行；历史时点记录，不代表当前仍在执行。

- 检查范围：`项目分析/ANIMATOR_STATE_MACHINE_DESIGN.md` 及本轮协作记录。
- 设计检查：确认实体关系为 `MObject → MSceneObject → MEntity`，`MEntity` 直接持有 `MModel Model`，`MModel` 直接持有 `MAnimator Ator`。
- 设计检查：确认文档不再使用 `EntityViewComponent`、`AnimatorFacade` 或普通组件容器查找 Animator 的绑定方案；动作组件统一访问 `Entity.Model.Ator`。
- 设计检查：确认 `MModel` 表现组件、实体普通组件、`MActionStateContainer` 动作状态三类对象边界明确，四个专用组件不进入普通组件集合。
- 静态结果：文档结构核对通过；本轮没有客户端/服务端代码变更，因此未执行编译和 Unity PlayMode。
- 待完成：由独立审核 Agent 复核类结构与 RO_cs422nb 源码的一致性；实现阶段再执行客户端构建、异步资源和状态机运行验证。

## [VERIFY][技能][TR-355] 2026-09-14 RO 风格 MModel 关系设计审核复验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3282—3287 行；历史时点记录，不代表当前仍在执行。

- 独立审核 Agent 复核通过：`MObject → MSceneObject → MEntity` 层级、`MEntity.Model`、`MModel.Ator` 和 `Entity.Model.Ator` 访问边界符合用户要求。
- 返工项已完成：正文不再保留旧表现中间层类型名称；`MActionStateContainer` 使用 `ActionStateKey` 唯一主索引，并以 `StateDefine` 建立辅助索引。
- 范围声明已补充：RO 的 `MPubVehicle`、`MSoul`、`MSceneTriggerObject`、`MDrop` 暂不纳入本轮实体实现。
- 验收结果：设计文档检查通过；无业务代码变更，因此未执行编译或 Unity PlayMode。

## [VERIFY][技能][TR-356] 2026-09-14 客户端 RO 模型动画链路实现验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3288—3295 行；历史时点记录，不代表当前仍在执行。

- 构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/model-build-final2` 成功，0 个错误，警告为既有工程警告。
- 静态检查：`SceneEntity` 及主城绑定代码未发现 `namespace`、`foreach`、三目运算符、`EntityViewComponent` 或 `AttachView` 残留。
- 结构核对：`MEntity.Model` 直接持有 `MModel`，`MModel.Ator` 持有 `MAnimator`；动作组件通过 `Entity.Model.Ator` 播放；模型和组件池均在实体销毁时回收。
- 未执行：Unity Play Mode、真实资源异步加载、角色/怪物状态切换及旧 `RoleFSM` 隔离运行回归。
- 临时产物：`_temp/model-build-final2/` 与 `model-build-final2.log`，用途为本轮客户端构建验证，待产品经理确认后清理。

## [VERIFY][综合][TR-357] 2026-09-14 客户端 RO 动作状态实现验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3296—3302 行；历史时点记录，不代表当前仍在执行。

- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug`。
- 结果：0 个错误，31 个既有警告；日志位于 `_temp/client-entity-build3.log`。
- 静态检查：SceneEntity 范围无 `namespace`、`foreach`、三目运算符；Builder 不再引用 `EntityViewComponent`。
- 结构检查：`MEntity` 直接持有 `MModel`；`MStateMachine` 注册状态容器并以秒值更新；实体世界转发 LateUpdate；动作状态由专用池回收。
- 未执行：Unity Play Mode、模型异步加载和实际 Animator 状态切换回归；`EntityViewComponent.cs` 文件仍保留兼容，但不在实体初始化链路使用。

## [VERIFY][综合][TR-358] 2026-09-14 RO 动作状态注册冲突复验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3303—3309 行；历史时点记录，不代表当前仍在执行。

- 对照参考项目确认 `MDefaultActionComponent` 负责默认待机/移动表现，`MFearRunComponent` 对应独立 `kStateFearRun`；因此普通移动不应重复占用状态键。
- 构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug` 成功，0 个错误、31 个既有警告；日志位于 `_temp/client-entity-build4.log`。
- 静态扫描：`SceneEntity` 范围 `namespace=0`、`foreach=0`、三目运算符=0。
- 未执行：Unity Play Mode、模型异步加载、真实 Animator 状态切换及对象池多轮复用运行验证。
- 更新链路核对：`MComponentMgr.UpdateSpecialComponents` 仅驱动逻辑 Update；`MStateMachine.LateUpdate` 仅由 `MEntity.LateUpdate` 调用，不再同帧重复播放动画。

## [VERIFY][场景实体][TR-359] 2026-09-14 RO 实体状态系统最终客户端验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3310—3320 行；历史时点记录，不代表当前仍在执行。

- 构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug` 成功，0 个错误；最终日志 `_temp/client-ro-entity-final.log`，增量构建报告 2 个既有程序集版本冲突警告。
- 规则扫描：`SceneEntity`、`RoleCtrlBase.cs`、`Global.cs` 的 `namespace=0`、`foreach=0`、三目运算符=0。
- 命名扫描：`EntityMoveComponent/EntityBuffComponent/EntitySkillComponent` 和自造的具体 `*ActionComponent` 旧类型引用为 0；保留 RO 原名 `MDefaultActionComponent` 及内部抽象 `MBasicActionComponent`。
- 池链静态核对：全部 Builder 通过 `MEntityPool.Acquire` 获取实体；World 在 Destroy 后 Release；Transform/Attributes/Role/Monster/Projectile 普通组件通过 `MComponentMgr` 创建；Marker 明确不池化；Skill 只进入动作状态池。
- 生命周期核对：实体复用重置身份、生命周期、Transform、事件、模型及具体运行字段；Player 重置角色容器状态、DirtyTracker、flags、disposed 与 AfterAddedToWorld 防重复标记。
- 帧末链核对：全客户端只有 `Global.SceneWorld.LateUpdate` 和 `MEntity.Machine.LateUpdate` 两级调用点，`UpdateSpecialComponents` 不再提前调用 LateUpdate。
- 隔离核对：绑定 `MEntity` 后 `RoleCtrlBase.ChangeState` 提前返回，不调用旧 `RoleFSM.ChangeState`；未绑定对象仍保留原兼容入口。
- 未执行：Unity Play Mode、真实 Animator Controller/AnimationClip、YooAsset 动画句柄、角色与怪物完整状态切换、实体/组件/Animator 池多轮运行态复用和网络联调。

## [VERIFY][场景实体][TR-360] 2026-09-14 RO 实体状态系统独立测试（不通过）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3321—3332 行；历史时点记录，不代表当前仍在执行。

- 测试结论：不通过。客户端工程可以编译，静态规则与工程登记检查通过，但本地玩家入世界生命周期存在确定性缺口，不能进入最终验收。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug`。
- 构建结果：0 个错误、31 个既有警告；日志位于 `_temp/ro-state-final-test-20260914/GM_Game-build.log`，构建产物位于 `_temp/ro-state-final-test-20260914/build/`。
- 静态规则：`SceneEntity`、`MainCityMgr.cs`、`SceneWorldSystem.cs`、`Global.cs` 及 `GM_Game.csproj` 范围内 `namespace=0`、`foreach=0`、三目运算符=0、`EntityViewComponent/AttachView=0`。
- 工程登记：`SceneEntity` 共 95 个 `.cs` 文件，`GM_Game.csproj` 中缺失登记 0、重复登记 0；`git diff --check` 通过，仅有 LF/CRLF 转换提示。
- 阻塞问题：`SceneWorldSystem.LoadLocalRole` 在 `SceneWorldSystem.cs:34` 调用 `InitializeLocalRole` 后直接通知 `LocalRoleChanged`，没有调用 `PlayerEntity.AfterAddedToWorld`。而 `InitializeLocalRole` 只初始化角色组件，真正的 `_roleComponents.AfterAddedToWorld()` 位于 `PlayerEntity.cs:81-88`，因此正常世界创建路径下角色组件永远收不到入世界通知。
- 返工要求：客户端在本地玩家完成数据和角色组件初始化后、发布 `LocalRoleChanged` 前补齐一次 `AfterAddedToWorld`，并保持该通知的防重复语义；返工后重新执行构建、规则扫描和本地角色生命周期复验。
- 其他待审风险：`MStateMachine.TransferToState(IMStateTransform)` 的公开重载可绕过 `CanTransfer`；`MainCityMgr` 的异步模型加载在实体提前移除时可能遗留孤立对象；`SceneEntityWorld.Create/CreateProjectile` 未处理 `Add` 失败后的回收。这些问题需要代码审核确认并分别补测。
- 未执行：由于已发现阻塞问题，本轮未进入 Unity Play Mode；真实 Animator 播放、动作状态事件、YooAsset 异步加载及实体/模型/组件/动作池多轮复用仍待返工后运行态验证。

## [VERIFY][场景实体][TR-361] 2026-09-14 RO 实体状态系统返工复验（仍不通过）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3333—3345 行；历史时点记录，不代表当前仍在执行。

- 复验结论：首次阻塞已修复，但整体仍不通过；代码审核确认另有两个对象池/异步生命周期阻塞项。
- 已修复项：`SceneWorldSystem.LoadLocalRole` 当前调用顺序为 `InitializeLocalRole`（第 34 行）→ `AfterAddedToWorld`（第 36 行）→ `LocalRoleChanged`（第 37 行）。`PlayerEntity.AfterAddedToWorld` 在第 83 行校验初始化、销毁及防重复标记，对象池复用时在第 179-182 行重置相关状态，因此本地角色每轮生命周期能够通知一次且不会在公布前遗漏通知。
- 独立构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/ro-state-retest-20260914/build`，结果为 0 个错误、31 个既有警告；日志为 `_temp/ro-state-retest-20260914/GM_Game-retest.log`。
- 规则复验：`SceneEntity`、`MainCityMgr.cs`、`SceneWorldSystem.cs`、`Global.cs`、`GM_Game.csproj` 范围内 `namespace=0`、`foreach=0`、三目运算符=0、`EntityViewComponent/AttachView=0`。
- 工程登记复验：`SceneEntity` 共 95 个 `.cs` 文件，`CompileMissing=0`、`CompileDuplicate=0`；`git diff --check` 退出码为 0，仅输出既有 LF/CRLF 转换提示。
- 剩余阻塞一：`MainCityMgr.CreateRole` 第 92-129 行的异步回调只依据捕获实体对象当前的可变 `EntityId` 查询世界。原实体在加载完成前移除时，新建 `GameObject` 不会销毁；同一实体对象若已被池复用，旧请求甚至可能把旧模型绑定到新实体。
- 剩余阻塞二：`SceneEntityWorld.Create/CreateProjectile` 第 33-48 行忽略 `Add` 返回值。世界已释放等情况下，Builder 已创建或从池取出的实体不会执行 `Destroy/Release`，方法仍返回未纳管实体，导致实体、组件和模型池链不闭环。
- 非阻塞加固：`MStateMachine.TransferToState(IMStateTransform)` 没有再次调用 `CanTransfer`，但与 RO 参考实现一致；现有业务事件入口和枚举重载已校验，另外调用点用于强制回默认。建议后续收窄该重载的可见性，防止新调用绕过状态许可。
- 待审核项：普通组件创建目前可能执行两次 `RebindOwner`，第二次会清空 `OnAttachToHost` 记录的事件列表；当前普通组件尚未注册事件，但该行为会破坏后续普通事件组件的自动解绑语义，等待代码审核最终分级。
- 未执行：由于仍有确定性生命周期阻塞，本轮未进入 Unity Play Mode；修复后需继续执行异步失效、实体池复用和世界释放后创建的专项测试。

## [VERIFY][综合][TR-362] 2026-09-14 本地玩家入世界生命周期返工复验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3346—3354 行；历史时点记录，不代表当前仍在执行。

- 代码核对：`SceneWorldSystem.LoadLocalRole` 的正常路径顺序已调整为“实体世界创建 → `InitializeLocalRole` → `AfterAddedToWorld` → `LocalRoleChanged`”。`PlayerEntity.AfterAddedToWorld` 自带防重复判断，保留每轮实体复用只通知一次的语义。
- 静态扫描：`SceneWorldSystem.cs` 的 `namespace=0`、`foreach=0`、三目运算符=0。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/local-role-world-hook-build-20260914`。
- 构建结果：成功，0 个错误、31 个既有警告；日志为 `_temp/local-role-world-hook-build-20260914.log`。
- 未执行：Unity Play Mode、角色组件运行态回调、状态切换、真实模型加载及对象池多轮复用；由后续测试 Agent 继续验证。
- 临时产物未清理，等待产品经理确认。

## [VERIFY][场景实体][TR-363] 2026-09-14 RO 实体状态系统最终返工复验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3355—3369 行；历史时点记录，不代表当前仍在执行。

- 最终结论：静态验收通过，三组返工项均已闭环；未发现新的确定性阻塞。Unity Play Mode、真实资源包、状态播放和网络联调尚未执行，运行态验收仍需单独进行。
- 独立构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/ro-state-final-retest-20260914/build` 成功，0 个错误、31 个既有警告；日志位于 `_temp/ro-state-final-retest-20260914/GM_Game-final-retest.log`。
- 规则扫描：目标范围 `namespace=0`、`foreach=0`、三目运算符=0；`ClientRole`、`RoleWorldSystem`、`EntityViewComponent`、`AttachView` 残留引用均为 0。`object/dynamic` 唯一命中是 `ActionStateKey.Equals(object)` 的标准 .NET 相等性覆写，不是业务载荷。
- 工程登记：`SceneEntity` 共 100 个 `.cs` 文件，`GM_Game.csproj` 中 `CompileMissing=0`、`CompileDuplicate=0`；`git diff --check` 退出码 0，仅报告既有 LF/CRLF 转换提示。
- 本地玩家生命周期：`SceneWorldSystem.LoadLocalRole` 顺序为实体创建、`InitializeLocalRole`、`AfterAddedToWorld`、`LocalRoleChanged`；`PlayerEntity` 复用时重置通知标记，组件入世界通知每轮只触发一次。
- NPC 生命周期：`NpcManager` 只通过 `SceneWorld.CreateEntity(EntityType.Npc, ...)` 创建，销毁按实体 ID 移除；异步回调校验管理器版本、固定请求 ID、实体引用、Active 状态和世界登记，失效对象销毁。
- 实体/组件池：`SceneEntityWorld.Create/CreateProjectile` 对 Build 为空或 Add 失败执行 Destroy/Release 并返回空；普通组件唯一 `AddComponent` 绑定，专用组件和动作状态独立绑定/回收；普通组件 Shutdown 先于专用组件和模型释放，双 Rebind 问题已消除。
- 状态矩阵：`CommonStateTypeTool` 为 15×15、每行 15 项的 RO 许可矩阵，非法枚举范围拒绝；`Singing→Skill` 等合法切换可用，业务入口经过 `CanTransfer`。
- Animator：`controllerLocation` 通过 YooAsset 加载 Controller lease；失败、过期会话、空 Animator 和 Reset 均释放 lease。JX/Demon Controller 静态包含 `Movement(float)` 与 `Action(int)` 参数，原生状态不存在时使用 Idle/Move/技能等兼容映射。
- 旧状态机隔离：绑定且 Active 的 `MEntity` 由 `RoleCtrlBase` 路由到新状态机；未绑定对象才使用旧 `RoleFSM`，无双写 Animator 链路。
- 未执行与下一步：尚未执行 Unity Play Mode、真实 YooAsset 延迟回调、Animator Controller/Clip 播放、状态矩阵运行态遍历、实体/组件/模型/Animator 多轮池复用和四端网络联调。建议在专用测试场景执行这些运行态用例。
- 临时产物：`_temp/ro-state-final-retest-20260914/`，等待产品经理确认清理。

## [VERIFY][NPC][场景实体][TR-364] 2026-09-14 NPC 实体世界与强类型角色组件返工验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3370—3381 行；历史时点记录，不代表当前仍在执行。

- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/npc-rules-fix-20260914/build`。
- 构建结果：成功，0 个错误，31 个既有工程警告；完整日志位于 `_temp/npc-rules-fix-20260914/GM_Game-build.log`。
- 规则扫描：`NpcManager.cs`、`MEventHandler.cs`、`EventCallbackHandler.cs`、`RoleComponentBase.cs` 未发现 `namespace`、`foreach`、三目运算符、`??`、`object`、`object[]` 或 `dynamic`。
- 结构核对：`NpcManager` 不再 `new NpcEntity`，正式路径只调用 `SceneWorld.CreateEntity`；实体销毁按 `EntityId` 调用 `RemoveEntity`；异步回调校验管理器版本、实体 ID、实体引用和世界登记后才绑定模型。
- 类型拆分：事件委托和事件处理器分别位于 `EventCallbackHandler.cs`、`MEventHandler.cs`，`GM_Game.csproj` 新登记委托文件，两个文件均仅包含一个顶级类型。
- 强类型核对：`RoleComponentBase.Load(object)` 已删除；全部角色组件装载入口均使用具名协议类型，未发现覆盖和调用残留。
- 差异检查：`git diff --check` 通过，仅报告既有换行格式转换提示。
- 未执行：Unity Play Mode、真实 YooAsset 延迟回调、管理器销毁与实体池复用运行态测试；需后续测试 Agent 继续验证。
- 临时产物：`_temp/npc-rules-fix-20260914/`，用途为本轮客户端构建日志和程序集输出，待产品经理确认后清理。

## [VERIFY][场景实体][TR-365] 2026-09-14 客户端实体池与异步表现生命周期返工验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3382—3393 行；历史时点记录，不代表当前仍在执行。

- 修改范围：`MainCityMgr.cs`、`SceneEntityWorld.cs`、`MComponentMgr.cs`、`MObject.cs`；未修改服务端、协议、场景、预制体或旧 `RoleFSM`。
- 首次构建：输出至 `_temp/pool-lifecycle-fix-20260914/build`，因 `MainCityMgr` 的局部变量 `transformComponent` 未满足 C# 明确赋值规则而失败，结果为 1 个错误、31 个既有警告；失败日志为 `_temp/pool-lifecycle-fix-20260914/GM_Game-build.log`。
- 修复：将回调中的 `transformComponent` 明确初始化为空，不改变实体校验与绑定行为。
- 复验构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/pool-lifecycle-fix-20260914/build`，结果为 0 个错误、31 个既有警告；日志为 `_temp/pool-lifecycle-fix-20260914/GM_Game-build-rerun.log`。
- 规则扫描：四个目标文件未发现 `namespace`、`foreach`、三目运算符、`dynamic`、`object[]` 或业务 `object` 载荷，行尾空白命中 0。
- 差异检查：`git diff --check` 退出码为 0，仅输出 `MainCityMgr.cs` 既有 LF/CRLF 转换提示。
- 生命周期静态核对：异步回调同时校验世界引用、固定 ID、实体引用和 Active 生命周期；所有失效或无法绑定分支销毁加载对象；世界创建 Add 失败执行 Destroy/Release；普通组件和专用组件各自只有一个绑定入口；普通组件在专用组件和模型释放前完成 Shutdown。
- 未执行：Unity Play Mode、真实 YooAsset 延迟回调、池对象被移除并复用后的旧回调、世界释放后创建及事件组件多轮复用测试。
- 临时产物未清理，等待产品经理确认。

## [VERIFY][界面][TR-366] 2026-09-15 EntityId 与 UID 语义复核

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3394—3400 行；历史时点记录，不代表当前仍在执行。

- 检查范围：当前客户端 `MObject`、`ROObject`、`SceneEntityWorld`、`MEntityPool`，公共库 `EntityId`，以及参考项目 `Ro_cs422nb/rogamelibs/entity/object.h`、`ro_list_node_data.h`。
- 复核结果：参考项目使用 `INodeData::uuid_ / GetUUID()` 作为运行时对象唯一身份，`ROObject::GetID()` 作为类型/配置 ID；当前项目 `EntityId` 承担前者而 `MObject.UID` 未接入，存在重复身份设计。
- 结论：本轮未修改业务代码；已将 UID 统一运行时身份、ConfigId 配置身份、RoleId 持久身份和 EntityId 兼容迁移原则写入 `Docs/AI/DECISIONS.md`。
- 未执行：统一 UID 的代码迁移、客户端构建和 Unity Play Mode；这些属于后续实施任务。

## [VERIFY][界面][TR-367] 2026-09-15 EntityId 删除与 UID 统一迁移最终复验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3401—3408 行；历史时点记录，不代表当前仍在执行。

- 源码扫描：`LYF_Server`、`LYF_MMO` 业务源码和工程文件中，`EntityId`、`EntityIdAllocator`、`OwnerEntityId`、`SceneEntityId`、`ownerEntityId`、`sceneEntityId`、`AllocateEntityId`、`TargetEntityId` 命中均为 0。
- 公共库构建：`dotnet build LYF_Server/MMoRpgCommon/MMoRpgCommon.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/common-uid-final`，0 个错误、0 个警告。
- 客户端构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/uid-client-final`，0 个错误、31 个既有警告。
- 服务端构建：`dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/uid-server-final`，0 个错误、7 个既有警告。
- 语义核对：实体运行时身份统一为 `ulong UID`；`RoleId` 和 `ConfigId` 未改作 UID。未执行 Unity Play Mode、对象池运行态复用和网络联调。

## [VERIFY][界面][TR-368] 2026-09-15 重复 UID 生成清理验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3409—3416 行；历史时点记录，不代表当前仍在执行。

- 修改范围：`PlayerEntity.cs`、`NpcEntity.cs`、`KnapsackWindowMockServer.cs`；未修改服务端业务。
- 静态检查：客户端实体代码不再包含 `_legacyUid`、`AllocateLegacyUid` 或无 UID 的 `PlayerEntity(MainRoleInfo)`/`NpcEntity()` 构造入口；测试脚本通过 `SceneEntityWorld.Create` 获取实体 UID。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/uid-duplicate-cleanup-build`。
- 构建结果：0 个错误、31 个既有警告；日志与产物位于 `_temp/uid-duplicate-cleanup-build`。
- 未执行：Unity Play Mode、测试世界运行态对象池多轮复用及网络联调。

## [VERIFY][界面][TR-369] 2026-09-15 重复 UID 清理最终复验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3417—3425 行；历史时点记录，不代表当前仍在执行。

- 静态扫描命令：`rg -n -i "_legacyUid|AllocateLegacyUid|PlayerEntity\\s*\\(\\s*MainRoleInfo|NpcEntity\\s*\\(\\s*\\)" LYF_MMO/Assets/GM_Game/Scripts`，结果为 0 命中；场景实体范围未发现 `EntityId` 旧类型命名残留。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/uid-duplicate-final`。
- 构建结果：成功，0 个错误、31 个既有警告；产物位于 `_temp/uid-duplicate-final/`。
- UID 语义核对：实体自身只读取 `MObject.UID`；世界分配器产生 UID 后由 Builder/Pool 注入，实体子类不再维护独立计数器或兼容 UID。`OwnerUID`、`TargetUID` 为关系字段，保留合理。
- 测试脚本核对：`KnapsackWindowMockServer` 通过 `SceneEntityWorld.Create` 创建测试玩家，并在销毁时按玩家 `UID` 移除，测试实体不会脱离世界索引。
- 未执行：Unity Play Mode、对象池多轮复用、真实异步资源回调和网络联调。

## [VERIFY][界面][TR-370] 2026-09-15 OwnerUID 清理最终复验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3426—3436 行；历史时点记录，不代表当前仍在执行。

- 静态扫描：`rg -n "OwnerUID|ownerUid|ownerUID|OwnerUid" LYF_MMO LYF_Server --glob '*.cs' --glob '*.csproj' --glob '*.xml'`，结果为 0 命中。
- 客户端构建：0 个错误、31 个既有警告，产物 `_temp/owner-uid-final-client/`。
- 公共库构建：0 个错误、0 个警告，产物 `_temp/owner-uid-final-common/`。
- GameServer 构建：0 个错误、7 个既有警告，产物 `_temp/owner-uid-final-server/`。
- 语义核对：`MObject.UID`/`ROObject.UID` 是唯一实体实例身份；基础实体不再维护 OwnerUID，关联对象由具体组件使用 UID 表达。
- 未执行：Unity Play Mode、对象池多轮复用、真实资源异步回调和网络联调。

- 公共库复验：`dotnet build LYF_Server/MMoRpgCommon/MMoRpgCommon.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/uid-duplicate-common`，0 个错误、0 个警告。
- 服务端复验：`dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/uid-duplicate-server`，0 个错误、7 个既有警告。

## [VERIFY][界面][TR-371] 2026-09-15 OwnerUID 清理验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3437—3443 行；历史时点记录，不代表当前仍在执行。

- 静态扫描：`rg -n "OwnerUID|ownerUid" LYF_MMO/Assets/GM_Game/Scripts LYF_Server`，结果为 0 命中。
- 公共库、服务端和客户端构建均成功，0 个错误；日志与产物位于 `_temp/owner-clean-common`、`_temp/owner-clean-server3`、`_temp/owner-clean-client2`。
- 覆盖范围：实体基类、子类、Builder、Factory、World、对象池、公共快照及 NPC/测试创建入口均已改为 UID 单一身份。
- 未执行：Unity Play Mode、真实资源异步回调、对象池多轮运行态复用和网络联调。

## [VERIFY][综合][TR-372] 2026-09-15 PlayerInputCtr 30 ֡��������֤

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3444—3450 行；历史时点记录，不代表当前仍在执行。

- ��֤���`dotnet build GM_Game.csproj --no-restore --configuration Debug --output LYF_MMO/_temp/input-30-frame-build`��
- ����������ɹ���0 ������41 �����о��档
- ��̬��飺ȷ�� `Update` �������ﵽ 30 �����㣻δ�޸�ʵʱ `Movement` ��ȡ�����붯���¼��ص���
- δִ�У�Unity Play Mode ʵ�������¼���֤���ƶ��¼������߼��Դ�ҵ��ʵ�֡�

## [VERIFY][综合][TR-373] 2026-09-15 RO 风格统一输入事件重构验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3451—3456 行；历史时点记录，不代表当前仍在执行。

- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/event-ro-final-verify`。
- 构建结果：0 个错误，保留工程既有警告。
- 静态核对：`EntityInputEventBridge` 的所有输入统一派发 `MEventType.Input`，具体类别由 `InputEventArgs.Kind` 表示；旧输入事件枚举和旧专用参数不再被业务调用。
- 待审核：独立代码审核 Agent 正在检查 RO 结构对齐、注释完整性和工程引用；Unity Play Mode 输入回归尚未执行。

## [VERIFY][综合][TR-374] 2026-09-15 PlayerInputCtr 33 毫秒时间节流验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3457—3463 行；历史时点记录，不代表当前仍在执行。

- 验证命令：`dotnet build GM_Game.csproj --no-restore --configuration Debug --output _temp/input-time-build`。
- 结果：构建成功，0 个错误，37 个既有警告。
- 静态审查：`Update` 使用 `Time.unscaledDeltaTime` 累积时间，达到 `0.033f` 秒执行一次并保留余量；不再使用帧计数。
- 未执行：Unity Play Mode 实际输入事件验证；移动事件发送逻辑仍待业务实现。

## [VERIFY][综合][TR-375] 2026-09-15 RO 风格统一输入事件最终验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3464—3470 行；历史时点记录，不代表当前仍在执行。

- 最终构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/event-ro-final-verify4`。
- 构建结果：0 个错误，37 个既有警告。
- 静态扫描：事件目录、输入控制器和工程文件中未发现 `MEventArgs`、`InputEventKind`、`params object[]`、旧输入事件枚举或来源专用桥接 API。
- 当前事件链：`PlayerInputCtr -> EntityInputEventBridge.DispatchInput -> MEventType.Input -> InputEventArgs -> MEventDispatcher`。
- 未执行 Unity Play Mode 的真实输入回归；待最终审核 Agent 输出复核结论。

## [VERIFY][综合][TR-376] 2026-09-15 PlayerInputCtr 33 毫秒时间节流复验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3471—3477 行；历史时点记录，不代表当前仍在执行。

- 验证命令：`dotnet build GM_Game.csproj --no-restore --configuration Debug --output _temp/input-time-final`。
- 结果：构建成功，0 个错误，37 个既有警告。
- 静态审查：`Update` 使用 `Time.unscaledDeltaTime` 和 `0.033f` 秒周期，保留超出时间；`OnEnable`、`OnDisable` 清零累加器。
- 未执行：Unity Play Mode 实际输入事件验证；移动事件发送逻辑仍待业务实现。

## [VERIFY][综合][TR-377] 2026-09-15 RO 事件系统最终复验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3478—3487 行；历史时点记录，不代表当前仍在执行。

- 代码审核返工已完成：`MMoveEventArgs.Clone` 补齐 `isLocalMove`，对象池首次回收会先初始化队列，`IEventArg.Clone` 注释与共享参数的 RO 分发语义一致，`MMoveEventArgs` 增加类级说明。
- 最终构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/event-ro-final-verify5`。
- 最终构建结果：0 个错误，37 个既有警告。
- 静态扫描：事件目录和输入控制器未发现 `MEventArgs`、`InputEventKind`、`params object[]`、旧输入事件枚举或来源专用桥接接口。
- 待审核 Agent 对 verify5 输出最终结论；Unity Play Mode 输入实测尚未执行。
- 最终代码审核结论：通过。审核确认统一 `Input` 事件、桥接接口、参数回收、移动参数完整克隆、对象池首次回收和注释均符合要求。
- 审核依据：`_temp/event-ro-final-verify5.log`，0 个错误、37 个既有警告；静态扫描目标残留为 0。
- 剩余风险：尚未执行 Unity Play Mode 的真实输入回归。

## [VERIFY][网络][TR-378] 2026-09-15 事件参数对象池与主城其他玩家同步测试组件

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3488—3495 行；历史时点记录，不代表当前仍在执行。

- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/maincity-mock-build`。
- 构建结果：成功，0 个错误、39 个既有项目警告；日志位于 `_temp/maincity-mock-build.log`，产物位于 `_temp/maincity-mock-build/`。
- 对象池静态检查：`InputEventArgs` 和 `EntityStateChangedEventArgs` 的业务创建点均已改为静态工厂方法；源码扫描未发现直接 `new InputEventArgs(...)` 或 `new EntityStateChangedEventArgs(...)`。
- 主城链路静态检查：协议回调 `SyncotherOnlineHandle` 仅负责反序列化并调用 `HandleOtherRoleOnline`；该入口先检查角色是否已存在，只有首次创建才调用 `CreateRole`。
- 测试组件静态检查：`MainCityOtherRoleMock` 位于 `UI/test`，通过 `MainCityMgr` 正式入口创建角色，销毁时以 `RoleId + UID` 双重校验调用 `RemoveOtherRole`；按脚本名称和 meta GUID 扫描，正式 `.unity`/`.prefab` 中均为零命中。
- 未执行：Unity Play Mode 真实异步预制体回调、手动 ContextMenu 触发、重复角色同步和测试组件销毁后的角色回收验证。

## [VERIFY][综合][TR-379] 2026-09-15 事件对象池与主城模拟返工验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3496—3505 行；历史时点记录，不代表当前仍在执行。

- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/maincity-mock-build`。
- 构建结果：成功，0 个错误、41 个项目既有警告；日志为 `_temp/maincity-mock-build.log`。
- 移动对象池检查：`PlayerInputCtr` 未直接调用 `MEventPool<MMoveEventArgs>.GetEvent()`；仅 `MMoveEventArgs.Create` 管理该类型的获取和字段重置。
- 参数交接检查：两个输入桥接方法都在 `entity == null` 时调用 `eventArgs.Recycle()`；成功派发才将生命周期交给实体分发器。
- 所有权检查：真实同步入口先删除同 RoleId 的测试 UID 登记；测试清理由 `MainCityMgr.RemoveTestOtherRole` 校验登记 UID 与世界实体 UID 后执行。
- 资源检查：`MainCityOtherRoleMock.cs` 和 `.meta` 均存在，GUID 为 `59da04f052e200243825559c934988cf`，已登记 `GM_Game.csproj`；按类名和 GUID 扫描正式 `.unity`/`.prefab` 均为零命中。
- 未执行：Unity Play Mode 实测与真实网络同步联调。
- 最终复验：补齐返工范围局部变量注释后重新执行同一构建命令，结果仍为 0 个错误、41 个项目既有警告。

## [VERIFY][网络][TR-380] 2026-09-15 主城其他玩家同步测试组件最终异常回收验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3506—3512 行；历史时点记录，不代表当前仍在执行。

- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/maincity-mock-build`。
- 构建结果：成功，0 个错误、45 个既有项目警告；日志位于 `_temp/maincity-mock-build.log`。
- 静态核对：`MEventDispatcher.DispatchEvent` 的参数回收位于最外层 `finally`；输入桥接器的空实体分支直接回收，有效实体分支移交分发器回收。
- 主城测试核对：`MainCityOtherRoleMock` 位于 `UI/test` 并已登记工程；正式 `.unity`、`.prefab` 中未引用该组件；真实网络同步同角色 ID 会移除测试所有权记录。
- 未执行：Unity Play Mode 手动挂载、异步角色预制体加载、真实网络同步接管和对象池多轮复用运行态验证。

## [VERIFY][网络][TR-381] 2026-09-15 主城其他玩家同步测试组件最终独立审核

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3513—3519 行；历史时点记录，不代表当前仍在执行。

- 审核结论：通过。
- 复现构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/maincity-mock-final-audit`。
- 结果：0 个错误、41 个既有警告；日志为 `_temp/maincity-mock-final-audit.log`。该复现结果取代本轮先前记录的 45 个警告计数。
- 静态检查：正式 `.unity`、`.prefab` 不含 `MainCityOtherRoleMock`；分发器 finally 回收、桥接空实体回收和真实同步撤销测试所有权均通过独立复核。
- 未执行：Unity Play Mode 与真实网络联调。

## [VERIFY][技能][TR-382-FILES] 查询任务的正式文档产物：2026-09-15 Ro_cs422nb 状态机与动作安装机制只读分析

- 该任务记录的正式文档路径：`项目分析/RO_STATE_MACHINE_RUNTIME_ANALYSIS.md`。
- 查询正文、当时状态及原文验证边界见 `Docs/AI/QUERY_TASKS.md` 的 `[TR-382]`；本条只保留文件产物追溯，不新增完成或检查结论。

## [VERIFY][综合][TR-383] 2026-09-16 EntityType Flags 客户端迁移

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3526—3532 行；历史时点记录，不代表当前仍在执行。

- 公共库构建：dotnet build LYF_Server/MMoRpgCommon/MMoRpgCommon.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/entitytype-common-build，成功，0 个错误，4 个既有警告。
- 客户端构建：dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/entitytype-client-build，成功，0 个错误，工程既有警告。
- 静态检查：客户端实体工厂、MObject、MStateMachine 不再使用组合类别的精确相等判断；本地玩家构造和 Builder 均传入 Role | Player。
- 未执行：服务端 Flags 迁移、Unity Play Mode、网络快照运行态验证。

## [VERIFY][综合][TR-384] 2026-09-16 EntityType Flags 服务端迁移

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3533—3539 行；历史时点记录，不代表当前仍在执行。

- dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/entitytype-server-build：0 错误，7 个既有警告。
- 静态核对玩家为 Role | Player，工厂实体类别与组件装配使用位判断。
- 尚未执行客户端构建、Unity Play Mode 和跨端快照联调。

## [VERIFY][综合][TR-385] 2026-09-16 PresentTable 表加载验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3540—3547 行；历史时点记录，不代表当前仍在执行。

- 静态检查：`LubanMgr` 新增表现表区域未发现 `foreach`、三目运算符、命名空间声明、`object` 或 `dynamic`；已生成表文件和 `tbpresenttable.bytes` 路径存在。
- 语义检查：`Init()` 直接引用 `tables.TbPresentTable.DataMap`；`GetPresentTables()` 返回全量索引；`GetPresentTableById(int id)` 未初始化或 ID 不存在时返回 `null`。
- 构建命令：`dotnet build LYF_Server/Network/Network.csproj --no-restore --configuration Debug -p:OutputPath=D:/unitypro/LYFMMORGP/_temp/present-table-build-final/network/bin/ -p:BaseIntermediateOutputPath=D:/unitypro/LYFMMORGP/_temp/present-table-build-final/network/obj/`；`dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug -p:OutputPath=D:/unitypro/LYFMMORGP/_temp/present-table-build-final/gameserver/bin/ -p:BaseIntermediateOutputPath=D:/unitypro/LYFMMORGP/_temp/present-table-build-final/gameserver/obj/`。
- 构建结果：Network 0 个错误、4 条既有警告；GameServer 0 个错误、11 条既有警告。
- 临时产物：`D:/unitypro/LYFMMORGP/_temp/present-table-build-final/`，待产品经理确认后清理。
- 未执行：真实服务器运行时查询、Unity 表现资源加载和四端联调。

## [VERIFY][综合][TR-386] 2026-09-16 PresentTable 最终复核

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3548—3552 行；历史时点记录，不代表当前仍在执行。

- 复核命令：Network 与 GameServer 分别使用独立输出目录 `D:/unitypro/LYFMMORGP/_temp/present-table-build-final2/network/`、`D:/unitypro/LYFMMORGP/_temp/present-table-build-final2/gameserver/` 构建。
- 复核结果：Network 0 个错误、4 条既有警告；GameServer 0 个错误、11 条既有警告。
- 规则复核：表现表新增区域无 `foreach`、三目运算符、命名空间声明、`object` 或 `dynamic`。

## [VERIFY][技能][TR-387] 2026-09-16 RO 风格 MAnimator/MAnimationMgr 重构测试待命记录

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3553—3558 行；历史时点记录，不代表当前仍在执行。

- 计划验证：`GM_Game.csproj` 构建至 `D:/unitypro/LYFMMORGP/_temp`；扫描动画重构文件的 `namespace`、`foreach`、三目运算符和弱类型载荷；验证 `MEntity.OverrideAnim` 返回 `MAnimator.MAnimInfo`、延迟 `AfterOverrideAnim` 参数在实体销毁/复用时释放、`ForceToDefault` 触发状态动画刷新。
- 中间态证据：仅发现新增 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/AsyncObj/MAnimationMgr.cs`；`MAnimator.cs` 与 `MEntity.cs` 仍引用旧顶级 `MAnimInfo`，旧 `MAnimInfo.cs`、`MAnimationClipInfo.cs`、`MAnimationAssetProvider.cs` 仍未移除，`MDelayedEntityEvent`、`MAfterOverrideAnimEventArgs`、`MEvent_AfterOverrideAnim` 均未落地。
- 状态机证据：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/State/MStateMachine.cs` 的 `TransferToState` 内仍为注释的 `//MarkTriggered();`，因此 `ForceToDefault()` 不能让 `LateUpdate` 执行 `PlayAnim`。
- 结论：未执行构建及运行态测试，不能给出通过结论。客户端开发 Agent 发生两次连接中断，需待完整代码重新提交后由独立测试 Agent 执行本测试清单。

## [VERIFY][综合][TR-388] 2026-09-16 animInfo.txt RO 格式索引复核

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3559—3565 行；历史时点记录，不代表当前仍在执行。

- 检查对象：`LYF_MMO/Assets/GM_Game/Resources/ResInfo/animInfo.txt`。
- 检查结果：成功解析为二维 JSON，共 12 条记录；每条恰含 Location、时长秒、循环标记三列，未发现重复 Location。
- 资源核对：12 个 Location 对应 `.anim` 文件全部存在；索引时长与 `m_StopTime`、索引循环标记与 `m_LoopTime` 逐条一致。
- 未执行：YooAsset 运行时清单校验与 Unity Play Mode 动画加载验证。

## [VERIFY][技能][TR-389] 2026-09-16 RO 动画资源合并第二次返工验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3566—3576 行；历史时点记录，不代表当前仍在执行。

- 开发构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/anim-ro-merge-rework2`，结果为 0 个错误、39 个项目既有警告。
- 第一次独立复现构建：输出到 `_temp/animation-ro-first-review-build`，0 个错误、39 个既有警告；因启动预加载、播放门控、混合树、关键预加载集合、LRU 驱动和反初始化缺失，审核不通过并退回。
- 第二次独立复现构建：输出到 `_temp/animation-ro-second-review-build`，0 个错误、39 个既有警告；因 `IsPreloadFinished` 当前状态语义和正式动作真实剪辑安装链缺失，审核不通过并再次退回。
- 第二次返工静态结果：旧 `MAnimationAssetProvider`、顶级 `MAnimationClipInfo`、顶级 `MAnimInfo` 及其文件、Meta、工程项均为 0 残留；目标文件 `namespace`、`foreach`、三目、弱类型业务载荷和行尾空白均为 0 命中。
- 行为核对：`MAnimator.MAnimInfo` 同时持有 ready 与 preparing；快速 A→B→C、缓存同步回调、加载失败保留旧 ready、Reset/Uninit 旧回调隔离已通过静态推演。启动时动画索引成功前不创建 SceneWorld，失败每 3 秒重试；全局 Update 稳定驱动 LRU。
- 动作核对：正式 Play/CrossFade 前按 PresentTable 安装真实剪辑；没有通用真实来源的动作不会放行 Controller 占位剪辑，而是记录失败并安装真实 Idle 回退。
- 当前审核：第三次独立完整复审进行中，尚未给出最终通过结论。
- 未执行：Unity Play Mode、真实 YooAsset 延迟完成、快速连续覆盖、失败重试、对象池多轮复用和客户端退出时在途加载实测。

## [VERIFY][技能][TR-390] 2026-09-16 RO 动画资源合并第三至第五次独立验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3577—3587 行；历史时点记录，不代表当前仍在执行。

- 第三次独立构建：输出 `_temp/animation-ro-third-review-build`，0 个错误、39 个既有警告；因正式角色未接 RO Controller、槽位失败错误提升 ready、旧兼容移动映射缺失而退回。
- 第三次返工构建：输出 `_temp/anim-ro-merge-rework3`，0 个错误、39 个既有警告；本地/其他角色创建链均传 RO Controller 完整 Location，11 个要求槽位静态存在，preparing 的 Pending/Applied/Failed 闭环完成。
- 第四次独立构建：输出 `_temp/animation-ro-fourth-review-build`，0 个错误、39 个既有警告；因 DefaultPackage 未收集动画索引目录和动画目录而退回。
- 第四次返工构建：输出 `_temp/anim-ro-merge-rework4`，0 个错误、39 个既有警告；新增 AnimationIndex 与 Animations 收集组，12 个动画文件均存在且落入 Animations Collector，索引目录 GUID 与配置一致，RO Controller 由现有 artres Collector 覆盖。
- 第五次独立构建：审核 Agent 复现为 0 个错误、39 个既有警告。业务代码、资源配置、12 条 animInfo 路径与 Collector 闭环、旧类型/文件/Meta/工程项残留、禁用语法和目标行尾检查均通过；仅因协作记录未补齐而未放行。
- 静态生命周期结论：ready/preparing、同步缓存回调、Controller 先后到、快速 A→B→C、失败保留旧 ready、Reset/Uninit、混合树、关键状态预加载、LRU 与启动门控均通过完整推演。
- 未执行：Unity Play Mode、YooAsset EditorSimulate/资源包重新构建、运行时 `CheckLocationValid`、真实延迟加载、对象池多轮复用和退出时在途加载实测。Unity Editor 当前占用项目，未并行启动同项目 Batch Mode。
- 下一步：第六次完整独立审核；如不通过继续退回开发 Agent，返工后重新完整审核。

## [VERIFY][技能][TR-391] 2026-09-16 RO 动画资源合并第六次审核与第五次返工验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3588—3595 行；历史时点记录，不代表当前仍在执行。

- 第六次独立审核结论：不通过。`CreateRoleCtrl.StartGame` 在动画索引启动门控期间直接调用尚未创建的 `Global.SceneWorld.LoadLocalRole`，会空引用；简单判空会吞掉成功回包，因此必须补充可恢复的强类型排队机制。
- 第五次返工构建：开发 Agent 执行 `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/anim-ro-merge-rework5`，结果为 0 个错误、39 个项目既有警告。
- 静态时序推演：开始游戏回包先到时排队并在世界创建后消费；动画索引先到时立即加载；预载失败时保留角色请求并随 3 秒重试继续；重复成功回包按账号、角色、实体和场景切换状态去重；断线、换号或退出时清除待处理请求、当前角色和事件订阅。
- 静态边界检查：`LoadLocalRole` 正式调用收口到 `Global.TryConsumePendingLocalRole`；`CreateRoleCtrl` 对 `SceneWorld` 零直接访问；`MainCtrl`、`RoleInfoCtrl` 的相关协议入口补充世界门控；禁用语法与目标行尾空白扫描通过。
- 本条仅记录开发 Agent 自验，不代表独立审核通过。当前待同一检查 Agent 执行第七次从第 1 项开始的完整审核。
- 未执行：Unity Play Mode、YooAsset EditorSimulate 或资源包重建、运行时 `CheckLocationValid`、快速重复回包、断线重登、真实延迟资源加载和对象池多轮复用实测。

## [VERIFY][技能][TR-392] 2026-09-16 RO 动画资源合并第七次独立审核

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3596—3605 行；历史时点记录，不代表当前仍在执行。

- 审核结论：不通过。
- 独立构建：_temp/animation-ro-seventh-review-build，0 个错误、39 个项目既有警告。
- 通过项：旧动画类型、文件、Meta 与工程项零残留；ready/preparing 和异步剪辑时序；动作播放门控；RO Controller 与覆盖槽位；12 条动画资源与 YooAsset Collector；禁用语法、注释、协作记录和根变更说明。
- 阻塞项一：异步进城回调未校验会话与角色身份，断线清理后旧回调仍可能隐藏选角 UI 并初始化主界面。
- 阻塞项二：ClearLocalRoleSession 清理顺序使 LocalRoleChanged(null) 回调期间仍可读取旧 roleCtrlBase，小地图会遗留失效跟踪引用。
- 审核建议：同时覆盖连接失败不在主线程触发时的清理安全，以及快速自动重连时旧回调与新会话之间的代次隔离。
- 未执行：Unity Play Mode、YooAsset EditorSimulate 或资源包重建、运行时 CheckLocationValid、真实断线重登和快速重复回包实测。
- 后续：开发 Agent 返工并更新变更说明后，必须执行第八次从第 1 项开始的完整独立审核。

## [VERIFY][技能][TR-393] 2026-09-16 RO 动画资源合并第七次审核返工验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3606—3615 行；历史时点记录，不代表当前仍在执行。

- rework6 范围：Global.cs、NetSocketMgr.cs、CreateRoleCtrl.cs，以及根变更说明。
- 场景回调隔离：捕获 Global、SceneWorld、PlayerEntity、UID、账号、角色、连接代次和 UI 控制器实例；回调先复位 transition，校验失败不隐藏选角 UI、不调用 InitMainCtrl。
- 会话清理顺序：roleCtrlBase 在 SceneWorld.Clear 触发 LocalRoleChanged(null) 之前置空。
- 重连隔离：待处理角色仅能由相同连接代次消费；旧连接失败回调按失败代次清理，不能删除已由新代次接管的角色会话。
- 开发 Agent 首次构建因缺少 EntityLifecycleState 的 using 为 1 个错误；补充必要 using 后，_temp/anim-ro-merge-rework6 最终构建为 0 个错误、39 个项目既有警告。
- 开发 Agent 静态检查：三类竞态断言、禁用 namespace/foreach/三目/弱类型业务载荷和行尾空白均通过。本条不代替独立审核。
- 未执行：Unity Play Mode、真实断线及自动重连、YooAsset EditorSimulate 或资源包重建、运行时 CheckLocationValid。
- 后续：同一检查 Agent 必须从验收清单第 1 项执行第八次完整审核。

## [VERIFY][技能][TR-394] 2026-09-16 RO 动画资源合并第八次独立审核

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3616—3625 行；历史时点记录，不代表当前仍在执行。

- 审核结论：不通过。
- 独立构建：0 个错误、39 个项目既有警告。
- 通过项：旧动画类型、文件、Meta、工程项零残留；ready/preparing 生命周期；资源、Collector 和 13 个 Controller 槽位；项目禁用语法和行尾检查。
- 阻塞竞态：SceneMgr 使用共享 _obs 保存并发场景加载订阅。A/B 请求重叠时，A 完成会释放 B 的订阅而不是自己的订阅，A 订阅泄漏并重复回调；旧场景仍可能真实激活，造成场景串台与最终请求回调丢失。
- 返工验收：每请求独立订阅生命周期；明确 latest-wins 或串行队列；旧请求不影响新请求、不重复完成；最终请求进入正确场景；过期请求不改写当前场景、不隐藏有效加载 UI。
- 必须覆盖：A 主城→断线→B 选角、B 先完成、A 先完成、连续 A→B→C。
- 未执行：Unity Play Mode、真实断线自动重连、YooAsset EditorSimulate 或资源包重建。
- 后续：开发 Agent 返工后，执行第九次从第 1 项开始的完整独立审核。

## [VERIFY][技能][TR-395] 2026-09-16 RO 动画资源合并第八次审核返工验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3626—3635 行；历史时点记录，不代表当前仍在执行。

- rework7 范围：SceneMgr.cs 与根动画合并变更说明。
- 实现：每请求独占订阅和句柄；串行 latest-wins；等待槽仅保留最后意图；旧请求不释放新订阅、不执行回调、不写当前场景、不隐藏 Loading。
- 开发 Agent 最终构建：_temp/anim-ro-merge-rework7，0 个错误、39 个项目既有警告；首次构建曾因同名枚举遮蔽出现 1 个错误，使用完整限定名后修复。
- 静态时序：A→B 时 A 清理后 B 落地；串行结构下 B 不会先于 A 完成；A→B→C 时 B 不启动，A 清理后直接 C。
- 底层核对：YooAsset 1.5.7 的 suspendLoad 会设置 allowSceneActivation=false，UnSuspend 会改为 true。独立审核必须检查过期 Additive 请求解除挂起并立即卸载的运行语义，不能仅以未调用 SceneOperationHandle.ActivateScene 认定旧场景没有激活副作用。
- 开发 Agent 静态扫描：无 namespace、foreach、三目、object[]、dynamic、旧 _obs 或新增空白错误。本条不代替独立审核。
- 未执行：Unity Play Mode、YooAsset EditorSimulate、真实 A/B/C 场景重叠和断线自动重连。
- 后续：同一检查 Agent 从验收清单第 1 项执行第九次完整审核。

## [VERIFY][技能][TR-396] 2026-09-16 RO 动画资源合并第九次独立审核

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3636—3645 行；历史时点记录，不代表当前仍在执行。

- 审核结论：不通过；SceneMgr 1 个阻塞项，其余 11 项通过。
- 独立构建：_temp/animation-ro-ninth-review-build，0 个错误、39 个项目既有警告。
- 阻塞：过期请求 UnSuspend 会把 allowSceneActivation 设为 true，真实实例化旧 Additive 场景并触发 Awake、OnEnable、Start。
- 确定性竞态：旧主城 UnloadAsync 后未等待便启动新主城；新实例注册协议后，旧实例 OnDestroy 可能按协议号删除新实例处理器，导致新主城失去协议响应。
- 返工验收：过期场景业务生命周期无副作用；等待旧场景卸载完成再启动新请求；协议注册注销具备实例所有权；覆盖 A主城→B选角、A主城→B选角→C主城、生命周期顺序、失败和同步重入。
- 其余通过：旧类型零残留；唯一 ClipInfo/MAnimInfo；ready/preparing；动作门控；RO Controller/13槽位；12条索引与Collector；Global门控；登录和连接代次；禁用语法；Docs。
- 未执行：Unity Play Mode、真实场景生命周期、真实断线自动重连和 YooAsset 模拟清单。
- 后续：开发 Agent返工后，同一检查 Agent从第 1 项执行第十次完整审核。

## [VERIFY][技能][TR-397] 2026-09-16 RO 动画资源合并第九次审核返工验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3646—3655 行；历史时点记录，不代表当前仍在执行。

- rework8 范围：SceneMgr、SocketDispatcher、MainCityMgr、CreateRoleCtrl、LoginCtrl、MainCtrl、SkillCtrl，以及 Scene_CreateRole、Scene_MainCity 两张场景。
- 业务副作用隔离：两张场景全部原根收拢到唯一默认禁用 SceneBusinessRoot；过期 Additive 场景从完成加载到卸载结束不启用业务根。
- 卸载屏障：过期请求等待自身卸载完成；latest 启用前等待旧托管场景与普通场景卸载完成；请求中途过期时按旧场景、自身场景、最新请求的顺序串行。
- 协议所有权：RemoveEventHandler 只有协议号和当前强类型委托同时匹配才删除；全部正式调用方已迁移，无按协议号无条件删除旁路。
- 开发 Agent 最终构建：_temp/anim-ro-merge-rework8，0 个错误、39 个项目既有警告。
- 开发静态检查：7 个 C# 文件禁用语法全零；两场景各 BUSINESS=1、INACTIVE=1、SCENE_ROOTS=1、MISSING=0，除业务根外无普通顶级 Transform 或根 Prefab；差异空白检查通过。本条不代替独立审核。
- 未执行：Unity Play Mode、YooAsset EditorSimulate、过期场景生命周期、同场景 A→B→C 和真实断线重连。
- 后续：同一检查 Agent 从验收清单第 1 项执行第十次完整审核。

## [VERIFY][技能][TR-398] 2026-09-16 RO 动画资源合并第十次独立审核

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3656—3665 行；历史时点记录，不代表当前仍在执行。

- 审核结论：不通过；2 个 SceneMgr 阻塞项。
- 独立构建：_temp/animation-ro-tenth-review-build，0 个错误、39 个项目既有警告。
- 阻塞一：SceneOperationHandle.UnloadAsync 立即释放 Provider，调用后再读 SceneObject 无法获得卸载失败的真实场景，过期场景和上一托管场景的 Unity 兜底路径失效。
- 阻塞二：业务根启用早于隐藏旧 Loading；根内 Awake/OnEnable 同步发起新请求时，新 Loading 会被旧请求随后隐藏。
- 返工验收：卸载前缓存 Scene；失败兜底使用缓存并等待；旧场景未销毁不启用新根；Loading 显示、刷新、隐藏具备请求代次所有权并覆盖同步重入。
- 其余全量项通过：场景 YAML、旧类型零残留、动画生命周期、动作和混合树、Controller、12 条索引与资源、Collector、Global/登录/连接、协议所有权、规则扫描与协作记录。
- 未执行：Unity Play Mode、真实卸载失败、业务根同步重入、YooAsset EditorSimulate。
- 后续：rework9 后由同一检查 Agent从第 1 项执行第十一次完整审核。

## [VERIFY][技能][TR-399] 2026-09-16 RO 动画资源合并第十次审核返工验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3666—3675 行；历史时点记录，不代表当前仍在执行。

- rework9 范围：SceneMgr.cs 与根动画合并变更说明。
- 卸载快照：LoadedSceneSnapshot 与 PreviousManagedSceneSnapshot 均在 YooAsset UnloadAsync 前保存，卸载失败兜底不读取失效 handle。
- 卸载状态：三类场景统一保存 Scene 快照与 Unity 异步操作；空操作或完成后仍 loaded 最多重试 3 次，耗尽后明确阻断调度且不启用新根。
- Loading 所有权：显示、刷新、隐藏均校验请求代次；提交先隐藏旧 Loading，再启用业务根与调用完成回调，覆盖 Awake/OnEnable 和 callback 同步重入。
- 开发 Agent 最终构建：_temp/anim-ro-merge-rework9，0 个错误、39 个项目既有警告。
- 开发静态检查：无 namespace、foreach、三目、object[]、dynamic、旧 _obs 或新增空白错误。本条不代替独立审核。
- 未执行：Unity Play Mode、YooAsset EditorSimulate、卸载失败注入、Loading 同步重入和真实 A→B→C。
- 后续：同一检查 Agent 从验收清单第 1 项执行第十一次完整审核。

## [VERIFY][技能][TR-400] 2026-09-16 RO 动画资源合并第十一次独立完整审核

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3676—3685 行；历史时点记录，不代表当前仍在执行。

- 最终结论：通过，无阻塞项，不需要返工。
- 独立构建：D:/unitypro/LYFMMORGP/_temp/animation-ro-eleventh-review-build，0 个错误、39 个项目既有警告。
- 动画结构：旧三个顶级类型及文件、Meta、工程项零残留；唯一 MAnimationMgr.ClipInfo 与 MAnimator.MAnimInfo；ready/preparing 全时序通过。
- 动画资源：12 条 animInfo 记录逐条核对 Location、.anim、.meta、m_StopTime、m_LoopTime；RO Controller 13 槽与 Meta GUID、ResInfo/Animas/artres Collector 通过。
- 生命周期：Global 启动与登录门控、连接代次、异步进城、SceneBusinessRoot、三类场景卸载、cleanup blocked、Loading 代次和协议委托所有权通过静态推演。
- 场景资源：Scene_CreateRole 唯一默认禁用业务根包含 8 个原根；Scene_MainCity 唯一默认禁用业务根包含 15 个原根；父级、Prefab TransformParent、单位变换和 SceneRoots 完整。
- 规则检查：目标范围 namespace、foreach、三目、object[]、dynamic 均为零；旧文件扫描为零；git diff --check 无空白错误，仅有 LF/CRLF 提示。
- 未执行：Unity Play Mode、YooAsset EditorSimulate、卸载失败注入、Loading 同步重入、真实 A→B→C 与断线重连。以上为剩余运行态测试范围。

## [VERIFY][综合][TR-401] 2026-09-18 Unity 启动报错诊断验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3686—3692 行；历史时点记录，不代表当前仍在执行。

- 日志文件：`C:/Users/lyf/AppData/Local/Unity/Editor/Editor.log`。
- 发现 `Exception: YooAssets not initialize !`，调用链为 `Global.Awake:94 -> YooAssets.GetPackage`；根因是测试启动脚本的 YooAsset 包初始化没有可靠地先于 `Global.Awake` 完成。
- 发现 `RoleCtrlBase.Gravity:275 -> RoleCtrlBase.Update:100` 空引用；根因是 `PlayStateMgr.Instance.stats` 生命周期未满足时仍执行重力计算。
- 发现网络连接码 `10061` 和 OpenUPM `ECONNRESET`，分别归类为外部服务未启动和包仓库网络问题。
- 未执行代码修改和 Unity Play Mode；本报告为诊断结果，待用户确认后进入客户端修复流程。

## [VERIFY][综合][TR-402] 2026-09-18 测试脚本 YooAsset 初始化时序修复

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3693—3706 行；历史时点记录，不代表当前仍在执行。

- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/UI/test/test.cs`。
- 结果：初始化流程改为 `YooAssets.Initialize` -> 创建并设置 `DefaultPackage` -> 等待 `InitializeAsync` 成功 -> 添加 `Global`；测试脚本不再直接调用未执行的协程。
- 构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/test-yooasset-init-build`，0 个错误，39 个既有警告。
- 静态检查：目标脚本未命中 `namespace`、`foreach`、三目运算符、`object`/`dynamic` 载荷；代码注释为中文。
- 未执行：Unity Play Mode、EditorSimulate 清单实际加载和场景运行验证。## 2026-09-18 YooAsset ���������С������

- ��־���֣�Editor.log ��ȷ��¼ Invalid collect path : Assets/GM_Game/Resources/ResInfo��SimulateBuild pipeline build failed �� SimulateManifestFilePath is null or empty��ͬʱ��¼ Global.Awake -> YooAssets.GetPackage �� YooAssets not initialize !��
- �����飺test.cs ������ YooAsset ��ʼ����DefaultPackage ����/������Ĭ�ϰ����á�ģ���嵥·��У�顢InitializeAsync �ɹ��жϺͳ��� Global ���ã����ٵ��� NpcManager��UIRoot�����ٵȴ� SceneWorld�������� Global��
- ������飺New Scene.unity �� Global GameObject Ĭ�Ͻ����Ұ� Global ��������Զ����Ƴ� NpcManager �����
- ���������dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/test-yooasset-simple-build Ϊ 0 ������39 �����о��档
- ��̬�����Ŀ��ű��ͳ���δ���� namespace��foreach����Ŀ�������object��dynamic��git diff --check �޿հ״���
- δִ�У�Unity EditorSimulate �ɹ�������Play Mode ����ʵ Global Awake ������֤���ռ�·���������������Դ��������������

## [VERIFY][NPC][场景实体][TR-403] 2026-09-18 YooAsset 测试场景 NPC 初始化恢复

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3707—3714 行；历史时点记录，不代表当前仍在执行。

- Editor.log 根因：YooAsset 报 The collecting asset file is existed: Assets/GM_Game/Resources/ResInfo/animInfo.txt，因为 AnimationIndex（ResInfo）与 Resources 收集器重复覆盖同一文件；旧运行日志还包含模拟构建失败及 Global 提前读取资源包的连锁错误。
- 修改验证：测试脚本含 YooAsset 初始化成功门控、Global 启用、SceneWorld 等待和一轮 NPC 初始化保护；场景 Global 默认禁用且测试对象挂载 NpcManager；资源配置中 AnimationIndex 组为 0，Resources 组为 1，ResInfo 路径重复收集为 0。
- 构建命令：dotnet build LYFMMORGP/LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/restore-npc-test-build。
- 构建结果：通过，0 个错误，39 个项目既有警告。
- Unity 运行验证：MCP 实例 LYF_MMO@47ea91c229a904f3 初始可连接，已停止旧 Play Mode 后重新进入 New Scene；进入播放期间 MCP WebSocket 断开，连续读取 Editor 状态和 Console 均返回 session not ready，因此未能取得本轮运行态日志。Editor.log 的重复收集错误来自断线前旧会话，不能作为修复后的运行结果。
- 未解决：需要 Unity MCP 恢复后重新执行一次 Play Mode Console 验证，确认模拟清单、Global.SceneWorld 和 NPC 初始化日志。- 场景引用补充后复建：同一构建输出目录增量构建退出码 0，0 个错误；本次增量输出 2 个警告（完整冷构建基线为 39 个既有警告）。

## [VERIFY][NPC][TR-404] 2026-09-18 New Scene NpcManager 序列化绑定静态检查

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3715—3722 行；历史时点记录，不代表当前仍在执行。

- 检查文件：`LYF_MMO/Assets/GM_Game/Scenes/New Scene.unity`。
- 静态结果：`testGameObject=True`、`testGlobalRef=True`、`testNpcRef=True`、`npcComponent=True`、`globalDisabled=True`、`globalComponent=True`；测试对象、Global 引用、NpcManager 引用和 Global 默认禁用状态均通过。
- 关键断言：`--- !u!114 &138351007` 下存在 `_npcManager: {fileID: 138351009}`；`--- !u!114 &138351009` 属于 GameObject `138351006`；`--- !u!1 &700000001` 的 `m_IsActive` 为 `0`。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/bind-npc-manager-build`。
- 构建结果：通过，0 个错误，39 个既有警告；输出目录为 `D:/unitypro/LYFMMORGP/_temp/bind-npc-manager-build`。
- 未执行：Unity Play Mode 和运行时 NPC 实例化验证仍需 Unity MCP 恢复后执行。

## [VERIFY][场景实体][TR-405] 2026-09-18 测试场景模拟主角角色包验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3723—3731 行；历史时点记录，不代表当前仍在执行。

- 修改范围：`LYF_MMO/Assets/GM_Game/Scripts/UI/test/test.cs`；未修改 `Global.cs`、正式登录/角色协议处理代码或协议生成代码。
- 静态验证：角色包为强类型 `MainRoleInfo` 和 `RoleBaseInfo`；创建统一调用 `Global.SubmitLocalRoleLoad`，后续由正式 `SceneWorld.LoadLocalRole` 创建 `PlayerEntity`。测试入口等待 YooAsset 初始化成功、Global 启用和 SceneWorld 就绪后运行；测试侧提交标记、当前 LocalRole 身份比较以及 Global 内置账号/角色 ID 检查共同阻止重复创建。
- 连接代次验证：正式 `Global.SubmitLocalRoleLoad` 要求 `NetSocketMgr.CurrentConnectionGeneration > 0`。测试脚本发现代次为零时调用既有 `NetSocketMgr.ConnectServer(NetDefine.IPHost, NetDefine.LoginServerPort)`，由正式网络管理器同步分配代次；不修改 Global，不通过反射或字段写入伪造状态。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj -o D:/unitypro/LYFMMORGP/_temp/test-mock-role-packet-build`。
- 构建结果：通过，0 个错误，39 个项目既有警告。警告包括 Unity 参考程序集 `System.Numerics.Vectors` 版本冲突、现有类型冲突和未使用字段，均非本次 `test.cs` 引入。
- 输出目录：`D:/unitypro/LYFMMORGP/_temp/test-mock-role-packet-build`。
- Unity Play Mode：未执行。当前没有可用 Unity MCP 运行态会话，未验证 EditorSimulate 清单加载、网络连接成功与主角模型实例化；需要恢复 Unity 会话后在专用测试场景验证。

## [VERIFY][网络][TR-406] 2026-09-18 主城其他玩家正式协议模拟验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3732—3739 行；历史时点记录，不代表当前仍在执行。

- 修改范围：`LYF_MMO/Assets/GM_Game/Scripts/UI/test/MainCityOtherRoleMock.cs`、`LYF_MMO/Assets/GM_Game/Scenes/New Scene.unity`；未修改 `MainCityMgr.cs`、`Global.cs`、`SocketDispatcher.cs` 或协议生成代码。
- 协议核对：`CMD_SyncotherOnlineCode` 为 `11100`，服务端发送 `RoleBaseInfo.ToByteString()`；客户端正式处理为 `SocketDispatcher.DispatchEvent -> MainCityMgr.SyncotherOnlineHandle -> RoleBaseInfo.Parser.ParseFrom -> HandleOtherRoleOnline -> SceneWorldSystem.AddOrUpdateOtherRole`，首次角色继续调用 `CreateRole` 创建表现。
- 测试实现：已有 `MainCityOtherRoleMock` 构造 `RoleBaseInfo(RoleId=900001, Nickname=主城模拟玩家, JobId=1)` 并投递同一协议字节。Start 协程在延迟后逐帧等待 `MainCityMgr.Instance`、`Global.Instance` 与 `Global.Instance.SceneWorld`，只在全部就绪后自动投递；组件成功后阻止重复投递，不直接创建、更新或删除场景实体。
- 场景静态检查：`New Scene` 的专用测试对象 `138351006` 已挂载脚本 GUID `59da04f052e200243825559c934988cf`，`_simulateOnStart=1`；预置 `Global` 保持默认禁用，未改正式主城场景。
- 构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/mock-maincity-role-response-build` 成功，0 个错误、39 个既有警告。
- Unity Play Mode：未执行。当前无可用 Unity MCP 编辑器会话，未验证运行时日志和模型实例化；恢复后应在 `New Scene` 验证角色 ID `900001` 的 `Role/Role_MJS_Other` 表现对象。

## [VERIFY][综合][TR-407] 2026-09-18 主城其他玩家 Play Mode 测试执行记录

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3740—3746 行；历史时点记录，不代表当前仍在执行。

- 测试文件：`LYF_MMO/Assets/GM_Game/Scripts/UI/test/Editor/MainCityOtherRolePlayModeTests.cs`。测试按 Unity Test Framework 编写：打开 `New Scene`、进入 Play Mode、最长等待 30 秒，并断言 `Global.Instance.SceneWorld.TryGetRole(900001)` 成功。
- 执行命令：`D:/Program Files/2023.2.20f1c1/Editor/Unity.exe -batchmode -nographics -projectPath D:/unitypro/LYFMMORGP/LYF_MMO -runTests -testPlatform PlayMode -testResults D:/unitypro/LYFMMORGP/_temp/maincity-other-role-playmode-results.xml -logFile D:/unitypro/LYFMMORGP/_temp/maincity-other-role-playmode.log -quit`。
- 实际结果：未执行测试。Unity 日志在项目初始化阶段报错 `It looks like another Unity instance is running with this project open` 与 `Multiple Unity instances cannot open the same project`，随后退出；虽进程退出码为 0，但没有产生测试结果 XML，不能视为通过。
- 产物：日志已生成 `D:/unitypro/LYFMMORGP/_temp/maincity-other-role-playmode.log`；结果文件 `D:/unitypro/LYFMMORGP/_temp/maincity-other-role-playmode-results.xml` 不存在。
- 下一步：关闭占用 `LYF_MMO` 的 Unity 实例后复跑同一命令；通过条件为 XML 中测试通过，且运行期断言确认角色 ID `900001` 位于 `Global.Instance.SceneWorld`。

## [VERIFY][构建][TR-408] 2026-09-18 PlayMode 测试程序集编译修复验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3747—3755 行；历史时点记录，不代表当前仍在执行。

- 失败原因：旧测试脚本位于 `Assets/GM_Game/Scripts/UI/test/Editor/`，默认 Editor 程序集没有通过 asmdef 声明 `TestAssemblies`，所以编译时找不到 `UnitySetUp` 和 `UnityTest`。
- 修复范围：删除旧 `Editor/MainCityOtherRolePlayModeTests.cs`；新增 `PlayMode/MainCityOtherRolePlayModeTests.cs`、`PlayMode/MainCityOtherRolePlayModeTests.asmdef`。asmdef 名称为 `GM_Game.MainCityOtherRole.PlayModeTests`，引用 `GM_Game` 和 `TestAssemblies`。
- 生成项目核对：`LYF_MMO/GM_Game.MainCityOtherRole.PlayModeTests.csproj` 已包含测试类和 asmdef，证明 Unity 项目生成已识别新程序集。
- 构建命令：`dotnet build LYF_MMO/GM_Game.MainCityOtherRole.PlayModeTests.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/maincity-other-role-playmode-test-build`。
- 构建结果：成功，0 个错误、40 个既有工程警告；`UnitySetUp`、`UnityTest`、NUnit 和运行时场景 API 均已解析。
- 静态检查：测试类无 `namespace`、`foreach`、三目运算符、`object` 或 `dynamic`，且不包含 `using UnityEditor`。
- 运行限制：未重跑 Unity batchmode，因为项目仍被另一 Unity 实例占用；另外 `New Scene` 未进入 Build Settings，测试运行时会给出明确前置条件失败，不改写用户配置。

## [VERIFY][综合][TR-409] 2026-09-18 New Scene MainCityMgr 默认启用验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3756—3762 行；历史时点记录，不代表当前仍在执行。

- 场景检查：`New Scene.unity` 中 GameObject `MainCityMgr`（fileID `1480371565`）当前 `m_IsActive: 1`；其 `MainCityMgr` MonoBehaviour（fileID `1480371566`、脚本 GUID `583407fd4052c9746ad8e5261f7dac75`）当前 `m_Enabled: 1`。
- 预期链路：启动时 MainCityMgr 进入 Awake 并向 SocketDispatcher 注册 `CMD_SyncotherOnlineCode`；已有 `MainCityOtherRoleMock` 等待 Global、SceneWorld 和 MainCityMgr 后投递角色 `900001` 的正式协议。
- Unity 刷新检查：检测到 3 个 Unity 进程；等待 15 秒期间 `C:/Users/lyf/AppData/Local/Unity/Editor/Editor.log` 的长度和更新时间未变化，无法获得本次外部 YAML 修改后的 Console 新日志。未将历史 `Editor` 路径错误当作当前错误。
- 编译基线：`GM_Game.MainCityOtherRole.PlayModeTests.csproj` 上一轮独立构建为 0 错误；本轮只改场景激活位，未改 C# 文件。
- 未执行：未重启或关闭用户正在使用的 Unity 实例，未执行新的 PlayMode 运行。

## [VERIFY][技能][TR-410] 2026-09-18 普通角色 RO 待机动画组件

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3763—3771 行；历史时点记录，不代表当前仍在执行。

- 修改范围：新增 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MIdleComponent.cs`，并在 `RoleEntityBuilder`、`PlayerEntityBuilder` 注册；`MDefaultActionComponent` 的战斗静止状态固定返回 `FIdle`。
- 设计：`MIdleComponent` 是普通 View 组件，随角色实体容器完成创建、初始化、每帧等待模型 Animator 与对象池释放。模型准备完成后读取 `PresentDataView`，分别调用 `OverrideAnim(Idle, IdleAnim)` 和 `OverrideAnim(FIdle, IdleFAnim)`；两个槽位都登记成功后才触发状态机刷新。
- 载具处理：`IsRideVehicle` 或 `IsRidePubVehicle` 为真时跳过角色待机动画装配，不覆盖载具表现。
- 构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/role-idle-component-build`，通过，0 个错误、39 个项目既有警告。
- 静态检查：新增组件与两处角色构建器未命中 `namespace`、`foreach`、三目运算符、`object`/`dynamic` 载荷；`git diff --check` 通过。
- 未执行：Unity Play Mode 和主城角色实际动画播放验证，原因是当前 Unity 编辑器会话未通过 MCP 提供可用运行态连接。

## [VERIFY][技能][TR-411] 2026-09-18 普通角色 RO 待机动画组件审核返工

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3772—3780 行；历史时点记录，不代表当前仍在执行。

- 修复战斗待机状态：`MDefaultActionComponent` 的进入/退出战斗事件改用独立事件处理器，分别唯一设置 `Entity.BattleIdle` 为 true/false；刷新入口只在状态机当前状态为本默认组件时触发，特殊动作不被战斗事件中断。
- 射击保护：`StateName` 先处理 `State.Shoot`，移动时维持 `ZdMoveRun`，静止时维持 `ShootIdle`，不允许 `BattleIdle` 抢占。
- 规范清理：删除三目表达式、`FootSteps` 的 `object[]` 形参和相关死代码注释；`FootSteps()` 现在无参并校验 Animator 和脚步时间数组。
- 构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/role-idle-component-rework-build`，通过，0 个错误、39 个既有警告。
- 静态检查：`MDefaultActionComponent.cs` 未命中 `namespace`、`foreach`、三目运算符、`object`/`dynamic`；`git diff --check` 通过。
- 未执行：Unity Play Mode。需要编辑器会话恢复后验证 `CombatStarted -> FIdle`、`CombatEnded -> Idle`、射击静止/移动优先级及特殊状态不被覆盖。

## [VERIFY][技能][TR-412] 2026-09-18 普通角色 RO 待机动画组件第二次审核 P2 返工

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3781—3787 行；历史时点记录，不代表当前仍在执行。

- 业务逻辑未修改。已清除 `MDefaultActionComponent.cs` 第 149、152、157、211、214 行尾随空白。
- 根目录 `项目修改日志/2026-09-18-ROLE_IDLE_COMPONENT_CHANGELOG.md` 已将差异检查表述限定为本功能目标文件，并说明全局 `git diff` 受用户既有无关改动影响，不能等同全局通过。
- 验证：目标 C# 文件行尾空白检查通过；禁用语法扫描通过；`git diff --check -- MDefaultActionComponent.cs 项目修改日志/2026-09-18-ROLE_IDLE_COMPONENT_CHANGELOG.md` 通过。
- 本轮未运行完整构建，原因是仅格式与文档变更，上一轮 `role-idle-component-rework-build` 已验证业务代码为 0 错误、39 个既有警告。

## [VERIFY][技能][TR-413] 2026-09-18 普通角色 RO 待机动画组件最终 P2 文档返工

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3788—3793 行；历史时点记录，不代表当前仍在执行。

- 修改范围仅为 `项目修改日志/2026-09-18-ROLE_IDLE_COMPONENT_CHANGELOG.md` 的验证描述；未修改业务代码。
- 已对 `MIdleComponent.cs`、`MDefaultActionComponent.cs`、`RoleEntityBuilder.cs`、`PlayerEntityBuilder.cs` 分别执行 `git diff --no-index --check NUL <文件>`；每条命令未输出空白错误。退出码 1 仅表示文件内容与 NUL 不同，属于未跟踪文件的预期结果。
- 全局 `git diff --check` 受用户既有无关改动影响，不作为本功能通过依据。

## [VERIFY][技能][TR-414] 2026-09-18 普通角色 RO 待机动画组件最终 P2 验证说明补充

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3794—3798 行；历史时点记录，不代表当前仍在执行。

- 修改范围仅为根目录变更说明的无索引校验退出码解释，未修改业务代码。
- 已复核 `git diff --no-index --check NUL MDefaultActionComponent.cs`：输出仅含 CRLF 转换提示，没有 `trailing whitespace` 或 `new blank line` 诊断；退出码 1 是 NUL 与现有文件内容差异的预期结果。

## [VERIFY][综合][TR-415] 2026-09-18 本地主角 Role_MJS 模型入口

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3799—3806 行；历史时点记录，不代表当前仍在执行。

- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/Manger/MainCityMgr.cs`、`LYF_MMO/Assets/GM_Game/Scripts/UI/test/test.cs`。
- 根因验证：`test.cs` 原模拟主角角色包未设置 `JobId`；`MainCityMgr.Start` 只检查一次本地主角，Global 延迟启用时不会自动补建表现。其他角色路径明确使用 `Role/Role_MJS_Other`，不是本地主角模型。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/main-role-mjs-build`。
- 构建结果：通过，0 个错误，39 个既有警告。
- 静态检查：目标文件未命中 `namespace`、`foreach`、三目运算符、`object`/`dynamic`；`git diff --check` 未报告空白错误。
- 未执行：Unity Play Mode。需要编辑器空闲后确认主角模型为 `Role_MJS(Clone)`，以及其他玩家模型保持 `Role_MJS_Other(Clone)`。

## [VERIFY][场景实体][TR-416] 2026-09-18 测试场景本地主角与其他玩家模型隔离验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3807—3814 行；历史时点记录，不代表当前仍在执行。

- 检查范围：`MainCityMgr.CreateLocalRole`、`test.SimulateMainRolePacket`、`MainCityOtherRoleMock` 及 `New Scene` 序列化配置。
- 根因确认：本地主角代码已使用 `Role/Role_MJS`；`Role/Role_MJS_Other` 来自场景中自动启用的 `MainCityOtherRoleMock`，其 `_simulateOnStart` 原为 `1`。
- 修正结果：`New Scene` 的 `_simulateOnStart` 改为 `0`。登录主角测试不会自动创建其他玩家；其他玩家同步测试仍可手动启用该组件。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/test-local-role-prefab-fix-build`。
- 构建结果：0 个错误、39 个既有警告。
- 未执行：Unity Play Mode，需在编辑器空闲后确认运行时只出现 `Role_MJS(Clone)` 本地主角，并按需手动验证 `Role_MJS_Other(Clone)` 其他玩家测试。

## [VERIFY][综合][TR-417] 2026-09-18 本地主角 Role_MJS Play Mode 实际验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3815—3824 行；历史时点记录，不代表当前仍在执行。

- 验证场景：`Assets/GM_Game/Scenes/New Scene.unity`；Unity MCP 实例 `LYF_MMO@47ea91c229a904f3`。
- Console 实际日志：`模拟主角角色包已提交，Global 状态：Loaded`、`开始加载本地主角模型：Role/Role_MJS`、`角色表现绑定成功，角色 UID：2，资源路径：Role/Role_MJS`。
- Game View 截图：`LYF_MMO/Assets/Screenshots/screenshot-20260918-222801.png`，可看到主城角色模型。
- 结果：本地主角生成链路通过，使用 `Role/Role_MJS`；`Role_MJS_Other` 未参与本地主角入口。
- 发现的独立既有错误：`CameraMgr.Update` 第 50 行空引用、Global 资源包不存在时的重试日志；未在本任务中修改。

- 说明：Unity 截图属于本次验证产生的临时文件，已移入 `D:/unitypro/LYFMMORGP/_temp/main-role-playmode/screenshot-20260918-222801.png`，未保留在客户端资源目录。

## [VERIFY][综合][TR-418] 2026-09-18 测试主角禁止主动重连

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3825—3831 行；历史时点记录，不代表当前仍在执行。

- 修改范围：`LYF_MMO/Assets/GM_Game/Scripts/UI/test/test.cs`。
- 静态检查：已删除 `EnsureTestConnectionGeneration` 及其 `ConnectServer` 调用；测试协程仅等待 `CurrentConnectionGeneration > 0`，不主动断开或创建连接；未新增 `namespace`、`foreach`、三目运算符或 `object` 业务载荷。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/test-local-role-no-reconnect-build`。
- 构建结果：通过，0 个错误、39 个既有警告。
- 未执行：Unity Play Mode。应在专用 `New Scene` 场景中确认网络失败/重连不会回收本地主角，并确认模型为 `Role_MJS(Clone)`。

## [VERIFY][综合][TR-419] 2026-09-18 离线测试主角允许零连接代次

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3832—3838 行；历史时点记录，不代表当前仍在执行。

- 修改范围：`LYF_MMO/Assets/GM_Game/Scripts/Global.cs`、`LYF_MMO/Assets/GM_Game/Scripts/UI/test/test.cs`。
- 静态检查：test.cs 已删除连接代次等待协程和无网络提示；Global 仅在 `_localRoleConnectionGeneration > 0` 时校验当前网络代次，离线测试角色可直接进入 SceneWorld；未新增 `namespace`、`foreach`、三目运算符或 `object` 业务载荷。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/test-local-role-offline-build`。
- 构建结果：通过，0 个错误、39 个既有警告。
- 未执行：Unity Play Mode。应在无服务器的 `New Scene` 确认本地主角使用 `Role_MJS(Clone)`，并在真实连接下确认断线代次清理仍有效。

## [VERIFY][综合][TR-420] 2026-09-18 离线测试主角实际运行验收补充

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3839—3845 行；历史时点记录，不代表当前仍在执行。

- 无服务器监听端口时运行 `New Scene`，仍成功提交模拟主角包并绑定 `Role/Role_MJS`。
- Console 出现：`动画索引加载完成`、`模拟主角角色包已提交，Global 状态：Loaded`、`角色表现绑定成功，角色 UID：2，资源路径：Role/Role_MJS`。
- Game View 截图已移入 `D:/unitypro/LYFMMORGP/_temp/main-role-offline-playmode/screenshot-20260918-233329.png`，可看到主角模型。
- Play Mode 已停止。

## [VERIFY][综合][TR-421] 2026-09-19 主角模型被连接失败回调回收修复

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3846—3852 行；历史时点记录，不代表当前仍在执行。

- 静态证据：Editor.log 记录 `Role/Role_MJS` 绑定成功后持续出现 10061 连接失败；代码链为 `OnConnectionFailed -> ClearLocalRoleSession -> SceneWorld.Clear -> MEntity.OnDestroying -> MModel.Reset`。
- 静态修复：`NetSocketMgr` 的有效代次在连接成功前保持 0，并新增 `IsConnected`；Global 只有真实连接成功才记录代次；test 防止重复 Global 和覆盖已有 LocalRole。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/test-local-role-connection-state-build2`。
- 构建结果：通过，0 个错误、39 个既有警告。
- 未执行：Unity Play Mode；需要复验无服务器场景绑定主角后不再被 10061 回调清理。

## [VERIFY][综合][TR-422] 2026-09-19 初次连接失败回调保留

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3853—3858 行；历史时点记录，不代表当前仍在执行。

- 修改范围：`LYF_MMO/Assets/GM_Game/Scripts/Manger/NetSocketMgr.cs`。
- 静态检查：`failedGeneration <= 0` 分支只投递 `connFail`，未调用 `ShopMgr.HandleDisconnected` 或角色清理；回调仍在 Unity 主线程执行。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/test-local-role-connection-fail-callback-build`。
- 构建结果：通过，0 个错误、39 个既有警告。

## [VERIFY][综合][TR-423] 2026-09-19 零代次连接失败回调竞态修复

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3859—3865 行；历史时点记录，不代表当前仍在执行。

- 验证目标：避免初次连接失败回调晚于自动重连成功回调执行时误报失败。
- 修改范围：`NetSocketMgr.OnConnectionFailed` 零代次分支，在主线程闭包内增加客户端引用和 `CurrentConnectionGeneration == 0` 双重校验。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/test-local-role-connection-fail-race-build`。
- 构建结果：通过，0 个错误、39 个既有警告。
- 未执行：Unity Play Mode 断线/重连交错运行态回归。

## [VERIFY][网络][TR-426] 2026-09-21 移动同步广播时机澄清

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3876—3880 行；历史时点记录，不代表当前仍在执行。

- 设计修正：玩家 A 的移动经 GameServer 逻辑线程计算出权威结果后，应在同一逻辑帧生成移动变化通知并广播给 AOI 范围内玩家 B；不能等待下一次周期性完整快照。
- 快照定位：`RoleMovementSnapshot` 是服务器权威结果，可作为每次移动变化通知的载荷；周期性快照只用于校正、重连、初次进入场景和丢包恢复，不是移动广播的唯一方式。
- 当前队列约束：网络线程只入队，所谓“立刻”表示 GameServer 下一次逻辑消费后立即发送，不表示在网络回调线程直接修改角色或广播。

## [VERIFY][网络][场景实体][TR-428] 2026-09-21 移动同步场景数据补充

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3887—3892 行；历史时点记录，不代表当前仍在执行。

- 结论：服务器要权威计算坡面、落地、悬空、越界和不可通行区域，必须加载服务器可查询的场景数据；不需要运行 Unity 完整渲染和刚体物理。
- 推荐数据：每个场景/分线导出高度或导航网格、可行走区域、坡度限制、静态碰撞代理、区域属性和数据版本。服务器通过地面高度、地面法线和导航/碰撞查询修正角色位置。
- 爬坡处理：客户端只发送水平移动意图，服务器根据 XZ 查询坡面高度与法线，沿坡面投影速度并校验最大坡度，生成权威 Y 坐标和移动结果；客户端可预测，但最终以服务器结果为准。
- 未修改代码，未执行构建或运行测试。

## [VERIFY][网络][TR-430] 2026-09-21 移动同步压力模型修正

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3899—3904 行；历史时点记录，不代表当前仍在执行。

- 设计修正：大型 MMORPG 不应让 GameServer 对所有角色每个 50ms 运行完整 Unity 物理。服务器只维护运动学状态，使用分段位置计算、空间索引、NavMesh/高度查询和按需碰撞校验。
- 推荐策略：普通移动以轻量速度/位移/区域校验为主；移动段开始、方向变化、场景边界、碰撞风险和定期校正时才查询地面与静态碰撞。AOI 限制广播范围，动态物理只处理影响玩法的对象。
- 坡面处理：不要求每帧查询精确法线；服务器在移动段关键点或固定校验间隔查询地面高度/坡度，客户端负责平滑表现，超过误差阈值再纠正。
- 说明：无法获知逆水寒或一梦江湖的内部实现，以上是大型 MMORPG 常见的轻量服务器权威与客户端预测混合方案。

## [VERIFY][综合][TR-433] 2026-09-21 移动快照频率与轻量校验调整

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3919—3924 行；历史时点记录，不代表当前仍在执行。

- 设计调整：不采用固定 100ms 位置上报。角色开始移动、停止移动、方向/速度模式变化、技能开始和技能结束时立即同步；持续移动期间每约 250~300ms 发送一次位置快照，静止时不发送。
- 服务器校验降级为基础距离检查：根据两次服务器接收时间和配置最大速度计算允许距离，增加容差；一秒内移动超过明显不可能的距离时记录并回退/纠正，不做 NavMesh、坡度或完整碰撞检测。
- 客户端其他玩家使用快照插值；低频快照不足时只做短时平滑外推，收到停止或技能结束快照立即收敛到最新位置。
- 未修改代码，未执行构建或运行测试。

## [VERIFY][网络][TR-434] 2026-09-21 移动同步设计文档审核完成

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3925—3930 行；历史时点记录，不代表当前仍在执行。

- 文档：`项目分析/MOVEMENT_SYNC_DESIGN.md`。
- 审核 Agent：`movement_sync_doc_review`，独立审核两轮，最终通过。
- 验收结论：客户端移动期间固定 300ms 上传位置快照；开始、停止、转向、技能起止立即同步；服务器不加载 Unity 场景、不运行完整物理，只做逻辑线程时间/距离/速度异常检查；非法位置不广播给 AOI；关键事件按 MoveSeq 排序；下行使用 MovementVersion 丢弃旧数据但不作为公共帧号；客户端其他玩家使用插值和短时外推。
- 本次只修改设计文档和协作记录，未修改业务代码，未执行构建或运行测试。该文档可作为后续协议和代码实现基线。

## [VERIFY][技能][TR-435] 2026-09-21 跳跃功能设计审核

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3931—3940 行；历史时点记录，不代表当前仍在执行。

- 本轮只完成设计，没有修改客户端、服务端、协议和配置代码。
- 设计文档：项目分析/JUMP_DESIGN.md。
- 审核 Agent：jump_design_review。
- 审核结论：推荐显式垂直速度 + 重力 + CharacterController.Move；不推荐 Rigidbody 或固定时长曲线。
- 覆盖范围：一段跳、二段跳、三段跳、落地重置、土狼时间、跳跃输入缓存、空中水平控制、动画事件边界和对象池复位。
- 关键风险：当前项目同时存在旧 RoleCtrlBase/MainRoleCtrl 和现代 MMoveComponent 两套运动入口；实现时必须只保留一个重力与 Move 权威，避免重复位移。
- 验证状态：未执行编译和运行测试，因为本轮明确不写代码。

## [VERIFY][网络][技能][TR-436] 2026-09-21 跳跃网络同步设计审核

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3941—3952 行；历史时点记录，不代表当前仍在执行。

- 需求：在本地跳跃设计基础上补充网络同步方案，本轮仍未修改代码。
- 文档：项目分析/JUMP_SYNC_DESIGN.md。
- 审核 Agent：jump_sync_design_review，结论通过。
- 关键结论：客户端立即预测，GameServer 逻辑线程确认和校验，GateServer 只转发，服务器不运行 Unity 完整物理。
- 多段跳序号：MoveSeq + AirborneSequence + JumpId + JumpCount，避免重复、乱序和旧落地事件结束新跳跃。
- 关键事件：JumpStart、JumpEnd、JumpAdjust/HeadBump、ForceMoveStart/End 不得被普通 300ms 快照覆盖；普通空中状态允许快照合并。
- 服务器校验：状态、跳跃次数、时间、水平/垂直运动包络和强制位移优先级；不信任客户端速度、重力和落地时间。
- 远端表现：使用服务器实际跳跃参数和 EventAgeMs 重建，撞头和落地必须有独立事件；超时或重连无法恢复时归一到 Grounded。
- 验证状态：只完成文档和独立设计审核，未执行编译或运行测试。

## [VERIFY][技能][TR-437] 2026-09-23 客户端跳跃动画组件与 PresentTable 配置验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3953—3962 行；历史时点记录，不代表当前仍在执行。

- 表格验证：使用 Artifact Tool 导入 `Luaban/MiniTemplate/Datas/#PresentTable.xlsx`，确认 `JumpStartAnim`、`JumpLoopAnim`、`JumpEndAnim` 三列及第 4 行动画值存在；渲染范围 `AJ1:AS4` 可读。
- Luban 验证：执行 `dotnet Luaban/MiniTemplate/Luban/Luban.dll -t all -c cs-bin -d bin`，校验和生成成功；产物保存在 `D:/unitypro/LYFMMORGP/_temp/luban-present-jump/`。
- Network 验证：`dotnet build LYF_Server/Network/Network.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/network-jump-build`，0 个错误、0 个警告；新 Network.dll 已同步到 Unity 插件目录。
- 客户端验证：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/jump-animation-final-build3`，0 个错误、既有警告；新增 `MJumpComponent`、`BeginJump`、`EndJump` 编译通过。
- 静态检查：目标文件未命中 `namespace`、`foreach`、三目运算符、`object`/`dynamic`；`git diff --check` 通过。
- 未执行：Unity Editor Play Mode 和真实动画资源加载；后续需在专用测试场景确认 `animInfo.txt` 包含 `Anim_JX_Jump_Start/Loop/End`，并调用 `MMoveComponent.BeginJump(1/2/3)` 验证重复起跳动画和 `EndJump()` 单次落地动画。
- 追加修正：依据实际 `AnimCtrl_Role_jx.controller`，跳跃 Animator 状态键统一为 `Jump_Start`、`Jump_Loop`、`Jump_End`；重新构建输出 `D:/unitypro/LYFMMORGP/_temp/jump-animation-final-build4`，0 个错误。

## [VERIFY][技能][TR-438] 2026-09-23 跳跃组件类型索引冲突修复验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3963—3972 行；历史时点记录，不代表当前仍在执行。

- 验证目标：确保 `MIdleComponent` 与 `MJumpComponent` 不因共用 `EntityComponentType.View` 互相覆盖。
- 修改范围：`LYF_Server/MMoRpgCommon/SceneEntity/EntityComponentType.cs`、`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MJumpComponent.cs`，以及同步生成的 `MMoRpgCommon.dll`、`Network.dll`。
- 构建命令：`dotnet build LYF_Server/MMoRpgCommon/MMoRpgCommon.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/jump-component-type-common-build`；`dotnet build LYF_Server/Network/Network.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/jump-component-type-network-build`；`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/jump-component-type-client-build`。
- 结果：公共库 0 错误、4 个既有警告；Network 0 错误、0 警告；客户端 0 错误、项目既有警告；目标文件 `git diff --check` 通过。
- 静态约束：未新增 `namespace`、`foreach`、三目运算符、业务 `object/dynamic`；公共枚举文件保留项目既有命名空间以兼容现有引用。
- 审核：`jump_animation_review` 已复核通过，确认独立组件类型可避免待机组件与跳跃组件索引覆盖，注释和项目约束符合要求。
- 未执行：Unity Editor Play Mode；仍需在专用测试场景确认组件容器同时注册待机和跳跃组件后不抛重复类型异常，并验证三段跳动画。

## [VERIFY][技能][TR-439] 2026-09-23 通用 Animator 跳跃槽位验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3973—3974 行；历史时点记录，不代表当前仍在执行。


## [VERIFY][技能][TR-440] 2026-09-23 Animator 跳跃状态机重复挂载修复验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3975—3993 行；历史时点记录，不代表当前仍在执行。

- 复现证据：修复前根层同时包含三个 Jump 状态，并且另有 `Jump` 子状态机。
- 修复结果：根层不再包含三个 Jump 状态；根层挂载 `Jump` 状态机；Jump 子状态机仍包含三个状态。
- 静态校验：根层三个 Jump fileID 均不存在，Jump 状态机引用存在，Jump 子状态机三个状态均存在。
- 代码构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/jump-controller-fix-build`，0 个错误，保留项目既有警告。
- 约束校验：`git diff --check` 通过；本次未新增业务 C# 代码。
- 代码审核：`jump_animation_review` 复核通过，确认 `Base Layer.Jump.<状态>` 路径和动画 GUID 对应关系正确。
- 未完成：当前 Unity 编辑器无可用 MCP 实例，尚未进行 Play Mode 和 Animator 窗口实机确认；运行前需重新导入 Controller/刷新 YooAsset 模拟清单。

- 验证目标：确保通用 `MAnimator.controller` 提供三个可被 `AnimatorOverrideController` 按名称查找的原始剪辑槽位。
- 修改范围：`LYF_MMO/Assets/artres/Resources/Controller/MAnimator.controller`、`LYF_MMO/Assets/artres/Resources/Anims/Default/Jump_Start.anim`、`Jump_Loop.anim`、`Jump_End.anim` 及对应 `.meta`、`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/AsyncObj/MAnimator.cs`。
- 资源结构：`MAnimator.controller` 的 Base Layer 挂载独立 `Jump` 子状态机，三个状态分别引用 GUID 对应的同名占位剪辑；Jump_Loop 占位剪辑启用循环，实际角色剪辑运行时覆盖。
- 代码构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/jump-controller-slot-build`，结果 0 错误、43 个项目既有警告。
- 资源导入：Unity `Editor.log` 记录 `Jump_Start.anim`、`Jump_Loop.anim`、`Jump_End.anim` 和 `MAnimator.controller` 均由 NativeFormatImporter 成功导入，未出现资源解析错误。
- 静态检查：目标代码和资源未新增 `foreach`、三目运算符、业务 `object/dynamic`；`git diff --check` 通过；MAnimator 的嵌套状态路径使用 `Base Layer.Jump.<状态>`。
- 审核状态：`jump_animation_review` 已复核通过 AnimatorOverrideController 原始剪辑匹配和 `Base Layer.Jump.<状态>` 路径逻辑。
- 未执行：Unity Editor Play Mode，需用真实角色动画资源确认三个覆盖剪辑能够实际播放。

## [VERIFY][技能][TR-441] 2026-09-23 跳跃动画与快速移动输入最小修复验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3994—4001 行；历史时点记录，不代表当前仍在执行。

- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/jump-movement-animation-fix-build3`。
- 结果：构建成功，0 个错误，保留项目既有警告。
- 静态检查：四个目标文件未新增 `namespace`、`foreach`、三目运算符或业务 `object/dynamic`；目标改动未引入编译错误。
- 代码审核：`jump_animation_review` 已复核通过缺槽位 Warning；补充预制体 Controller 回退 Warning 后再次构建通过。
- 未执行：Unity Editor Play Mode 和真实资源异步加载验证。

## [VERIFY][技能][TR-442] 2026-09-23 客户端输入停止与跳跃等待修复验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4002—4010 行；历史时点记录，不代表当前仍在执行。

- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/controller/InputController/PlayerInputCtr.cs`、`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MJumpComponent.cs`。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/client-input-jump-fix-build`。
- 构建结果：成功，0 个错误，保留项目既有 39 个警告。
- 静态检查：目标文件未新增 `namespace`、`foreach`、三目运算符或业务 `object/dynamic`；`git diff --check` 通过。
- 行为核对：started/performed 进入 `EntityInputEventBridge.DispatchInputMove` 并由 `MEvent_Move` 监听；canceled 进入 `MEvent_StopMove`；移动参数由 `MEventDispatcher` 唯一回收，停止事件不创建池对象；跳跃组件在动画索引预加载完成前不调用短名解析。
- 未执行：Unity Editor Play Mode；尚需实测快速方向切换、松开停止和跳跃动画资源异步加载。
- 追加验证：`DispatchMovement` 的 `moveArgsOwnershipTransferred` 标记通过静态检查，确认桥接调用异常不会触发外层二次 `Recycle`；构建复跑 0 个错误、39 个既有警告。

## [VERIFY][技能][TR-443] 2026-09-23 Special 动作动画配置修复

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4011—4018 行；历史时点记录，不代表当前仍在执行。

- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MActionComponent.cs`。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/special-animation-fix-build`。
- 构建结果：0 个错误，保留项目既有 39 个警告。
- 静态审核：Special 映射按 `FreeStyleAnim`、`OpenAnim`、通用 `Default/Special.anim` 顺序读取；不复用 Idle；约束扫描通过；审核 Agent 结论通过。
- Unity 实测：通过 Unity MCP 清空旧日志，运行 `Assets/GM_Game/Scenes/New Scene.unity` 约 3 秒，控制台错误 0、警告 0，原“状态=Special”错误未复现；随后已停止 Play Mode。
- 未解决问题：若 YooAsset 清单不包含通用 Special 槽位，具体角色仍需补充资源索引或表现表配置。

## [VERIFY][技能][协作][TR-445] 2026-09-24 客户端跳跃链路与 Special 回退验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4025—4033 行；历史时点记录，不代表当前仍在执行。

- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/client-jump-component-fix-build-final`。
- 构建结果：成功，0 个错误，38 个项目既有警告。
- 静态检查：新增代码未命中 `namespace`、`foreach`、三目运算符、`object[]`、`dynamic`；目标文件 `git diff --check` 通过。
- 链路检查：`Jump.started` 唯一进入 `LocalRole.MoveComp.TryJump()`；旧 `JumpingEvent`、`JumpKeyPress` 已移除；项目内无 `SpecialActionStarted` 的兜底派发。
- 运动检查：`MMoveComponent` 最多接受三次起跳，每段重置向上速度；同一帧合并水平/垂直位移后调用一次 CharacterController；下降接地后调用 `EndJump()` 并重置次数。
- 未执行：当前没有可用 Unity MCP 实例，未执行 Play Mode；后续需在专用测试场景验证连续三段跳、第四次拒绝、落地重置和 Special 告警不再出现。

## [VERIFY][技能][TR-446] 2026-09-24 跳跃落地移动与 Shift 走跑切换验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4034—4051 行；历史时点记录，不代表当前仍在执行。

- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/controller/InputController/PlayerInputCtr.cs`、`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs`、`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MDefaultActionComponent.cs`、`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MJumpComponent.cs`。
- 根因检查：`MMoveComponent._targetDir` 由 `MEvent_Move` 更新，只在收到 `MEvent_StopMove` 或明确不可移动时清除；起跳及 `EndJump()` 不清方向。原地面移动不续行的直接原因是 `MJumpComponent` 落地动画计时结束后直接播放 Idle，覆盖了仍保留的移动表现。Shift 之前没有接入新 `MMoveComponent`，速度及默认动画一直固定跑步。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/client-movement-jump-run-build`。
- 构建结果：成功，0 个错误、38 个项目警告；警告为当前工程现存依赖冲突、重复类型及未使用字段等，不影响本次构建。
- 静态检查：目标文件约束扫描未发现 `namespace`、`foreach`、三目运算符或业务 `object/dynamic`；`git diff --check` 通过；未修改 `MainRoleCtrl`、`RoleCtrlBase`。
- 行为链检查：Shift 按键阶段直接写入本地 `MoveComp.IsRunning`；`PlayerUpdate` 使用普通/跑步不同速度且限幅；`MDefaultActionComponent` 依据战斗状态和跑步输入选择 Walk/Run 槽位；`MJumpComponent` 落地动画结束后交回默认状态机，读取落地时保留的移动方向和跑步模式。
- 未执行：当前 Agent 没有可用 Unity MCP 工具，未执行 Unity Play Mode；需在编辑器中实测按住方向键起跳并落地继续移动，以及按住/松开 Shift 的速度、动画切换。

### 注释门禁补充验证（2026-09-24）

- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs`、`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MDefaultActionComponent.cs`；仅补充方法/属性说明，不改行为。
- 检查：`PlayerUpdate` 参数标明逻辑帧间隔单位毫秒及负值的积分处理；`SetRunning`、`IsRunning`、`ApplyRunningState`、`StateName` 的用途、参数或状态语义均有说明。
- 构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/client-move-run-param-doc-build/output --verbosity quiet`；成功，0 错误、38 个项目现存警告。
- 临时产物：本次构建输出和日志位于 `D:/unitypro/LYFMMORGP/_temp/client-move-run-param-doc-build/`。
- 未执行：本次为注释门禁复核，不运行 Unity Play Mode。

## [VERIFY][综合][TR-447] 2026-09-24 空中按前进至落地仍 Idle 修复验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4052—4069 行；历史时点记录，不代表当前仍在执行。

- 本轮实际修改：仅 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs`；`PlayerInputCtr.cs` 的 33ms 移动采样是工作区既有代码，本轮未修改。
- 修复内容：修正移动组件在跳跃期间按 `!Entity.IsMovable` 调用 `stopMove()` 清除方向的问题：跳跃期间允许记录和保留水平输入，普通非跳跃状态仍使用原不可移动限制；输入变为零仍由既有 `canceled` 回调即时停止。
- 明确复现检查：起跳后空中按住前进时，started/performed 或既有 33ms 采样会把方向送入 `onMoveEvent`；跳跃期间事件校验不会因地面动作状态拒绝，`PlayerUpdate` 也不会因该状态清除方向，落地后默认状态机按 `Entity.IsMoving` 选择 Walk/Run。空中松开时 `canceled` 清除方向，后续采样读取零向量，不会重新启动移动。此为源码链静态核对，不代表 Play Mode 运行验收。
- 构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/air-jump-move-fix-build/output --verbosity quiet`；0 错误，38 个项目既有警告。
- 静态检查：目标 `MMoveComponent.cs` 未命中 `namespace`、`foreach`、三目运算符、`object` 或 `dynamic`；`git diff --check -- LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs` 无空白错误，仅报告 LF/CRLF 行尾提示。全工作区 `git diff --check` 有多个与本任务无关的已有脏文件空白告警，未做清理。
- Unity 运行：未执行 Play Mode；本轮没有可访问的 Unity MCP 工具，不能把静态链检查表述为运行时验收。
- 后续复测步骤：专用测试场景中先按 Space 起跳，空中再按住 W 直到落地，确认落地后仍前进并播放 Walk/Run；另测空中按 W 后落地前松开，确认停止且不会残留方向。

### 独立代码审查与 Play Mode 连接复查

- 独立测试 Agent 审查结论：通过静态逻辑审查。空中 `Movement.started/performed` 能进入 `onMoveEvent` 并设置 `_targetDir`、`_isMoving`；跳跃期间 `PlayerUpdate` 不再因 `IsMovable` 触发 `stopMove()`；`canceled` 仍走 `MEvent_StopMove` 清除方向；落地动画结束后默认动作按移动/Shift 状态选 Walk/Run 或 Idle。未发现必须返工的静态逻辑问题。
- 独立复建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/air-jump-move-fix-final-recheck/output --verbosity quiet`；0 错误、38 个现有警告。
- 约束复查：目标文件 `git diff --check` 无空白错误；本次涉及的 `MMoveComponent.cs` 未新增 `namespace`、`foreach`、三目运算符或业务 `object/dynamic`。`PlayerInputCtr.cs` 是此前已存在的工作区修改，本次未改动。
- Play Mode：未执行。当前 Unity MCP 工具不可用；Windows 桌面连接两次均返回 `unsupported Codex auth method: apikey`、`apps=[]`，无法可靠操控运行中的 Unity。此项属于未验收，不是通过，也没有实际功能失败可据此打回开发。
- 待办：需在可连接 Unity 的会话中执行“先起跳、空中按住 W 至落地”和“空中按 W 后落地前松开”两条真实输入回归；若任一不符预期，再按失败现象返工并重测。

## [VERIFY][技能][TR-448] 2026-09-24 跳跃落地移动动画直达验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4070—4086 行；历史时点记录，不代表当前仍在执行。

- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs`、`MJumpComponent.cs`、`Action/MDefaultActionComponent.cs`。
- 静态链路：非零移动事件设置 `HasDirectionalInput=true`；`MEvent_StopMove` 调用 `stopMove()` 清除方向输入；跳跃期间 `PlayerUpdate` 不清方向；`JumpEnded` 检测到方向输入时不播放 `Jump_End`，而是 `ForceToDefault()` 并 `MarkTriggered()`；默认状态对本地主角根据方向输入直接选择 Walk/Run。没有方向输入时保留 `Jump_End`，结束后回 Idle。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/jump-landing-direct-move-build/output --verbosity quiet`。
- 构建结果：成功，0 个错误、38 个项目既有警告。
- 约束检查：三个目标文件未命中 `namespace`、`foreach`、三目运算符、业务 `object/dynamic`；目标文件 `git diff --check` 无空白错误。
- Unity 运行：未执行 Play Mode；本轮没有可使用的 Unity MCP 工具，不能将静态检查记为运行通过。

### 第一次审查失败与返工复审

- 第一次审查：不通过。无方向输入落地时，`JumpEnded` 播放 `Jump_End` 后，状态机可在同帧消费停止移动遗留的 `_isTriggered`；由于 `IsJump` 已为 false，默认动作会立即播放 Idle 并覆盖 `Jump_End`。
- 返工内容：`MJumpComponent.IsLandingAnimationPlaying` 提供强类型只读保护状态；`MDefaultActionComponent.PlayAnim` 在 `IsJump` 或落地动画保护阶段返回原动画；`Jump_End` 完成后先清除保护状态，再执行 `ForceToDefault()` 与 `MarkTriggered()`。起跳、移动落地和组件回收都会清除落地保护状态。
- 二次构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/jump-landing-end-guard-build/output --verbosity quiet`；0 错误、38 个项目既有警告。
- 第二次审查：独立测试已确认静态逻辑与编译通过；未发现需要再次返工的问题。
- Play Mode：仍未执行。当前 Unity MCP/桌面连接不可用，因此二次审查通过仅代表静态逻辑和构建通过，不代表真实输入运行验收。

## [VERIFY][综合][TR-449] 2026-09-24 HighlightManager 空引用与 MainRoleCtrl 兼容验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4087—4107 行；历史时点记录，不代表当前仍在执行。

- 异常位置：`HighlightManager.Update()` 原第 263 行直接访问 `_mainRole._targetRole`。
- 根因验证：`Role_MJS.prefab` 包含 `HighlightManager`、`PlayerInputCtr`、`GhostEffect` 和 `CharacterController`，但未包含 `MainRoleCtrl`；原 `Start()` 的 `GetComponent<RoleCtrlBase>()` 必然返回 null。
- 修改文件：`LYF_MMO/Assets/GM_Game/Plugins/HighlightPlus/Scripts/HighlightManager.cs`、`LYF_MMO/Assets/GM_Game/Scripts/Manger/MainCityMgr.cs`。
- 空引用检查：选中角色、打开 NPC 对话、点击空白取消选择三处均通过 `ResolveMainRole()` 获取主角并判空；主角尚未准备完成时只更新高亮，不再访问空控制器。
- 兼容检查：主角模型绑定后补齐或复用 `MainRoleCtrl`，依次调用 `InitCtrl(RoleType.MainRole, baseInfo)` 与 `SetROObject(sceneEntity)`；`Awake/Start/UseSkill` 等非移动职责保留。
- 双重控制检查：活动场景实体绑定后，`RoleCtrlBase.Update()` 在调用 Gravity 和 `MainRoleCtrl.OnUpdate()` 前返回，`OnAnimatorMove()` 也提前返回，因此旧 `PlayerMovement`、重力、根运动和旧跳跃不会执行。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/highlight-mainrole-compat-final-build/output --verbosity quiet`。
- 构建结果：成功，0 错误、38 个项目既有警告；目标文件 `git diff --check` 无空白错误，仅有行尾格式提示。
- Unity 运行：未执行 Play Mode，需在可连接 Unity 的环境中点击角色、NPC 和场景空白复测控制台。

### 独立测试与代码审查

- 独立复核确认 `HighlightManager` 不再直接解引用 `_mainRole`；选择角色、NPC 对话和点击空白取消选择均通过 `ResolveMainRole()` 解析兼容控制器并执行空值保护。`NpcCtrl.OpenTalk()` 对空主角引用也有保护，不会再次触发原第 263 行空引用。
- 主角表现绑定后只补齐或复用一个 `MainRoleCtrl`，并依次执行 `InitCtrl(RoleType.MainRole, baseInfo)`、`SetROObject(sceneEntity)`；`Awake`、`Start`、`UseSkill`、相机初始化、选中和 NPC 对话兼容职责仍然保留。
- 活动 `MEntity` 下，`RoleCtrlBase.Update()` 在旧重力、旧 `CharacterController.Move` 和 `MainRoleCtrl.PlayerMovement()` 之前返回，`OnAnimatorMove()` 也在旧根运动之前返回；跳跃入口继续为 `PlayerInputCtr -> LocalRole.MoveComp.TryJump()`，未恢复第二套移动、重力或跳跃权威。
- 独立构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/highlight-mainrole-review-build --verbosity quiet`，结果为 0 个错误、38 个项目既有警告。
- 独立约束检查：本轮新增代码未使用 `foreach`、三目运算符、`dynamic` 或业务 `object` 载荷，目标文件 `git diff --check` 通过。
- Play Mode 未执行：当前 Unity MCP 工具不可用，Windows 桌面连接返回 `unsupported Codex auth method: apikey` 且应用列表为空。本结论仅表示静态逻辑、代码约束和编译通过，不代表点击角色、NPC、场景空白的运行验收通过。

## [VERIFY][综合][TR-450] 2026-09-24 快速点按方向停止修复验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4108—4115 行；历史时点记录，不代表当前仍在执行。

- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/client-quick-tap-stop-build/output --verbosity quiet`。
- 构建结果：成功，0 个错误、38 个项目已有警告。
- 输入链路：Movement started/performed 的非零值立即派发移动；回调零值、canceled、33ms 零值对账、OnDisable 和 OnDestroy 均会派发 Stop。`MMoveComponent.stopMove()` 清理方向、预测位置和运行态，并在方向标记单独变化时补发默认动画刷新。
- 生命周期复核：OnEnable 只重置输入缓存并启用动作资产，角色未创建时不会解引用角色；OnDisable/OnDestroy 重复 Stop 只做幂等清理。
- 静态检查：目标文件未命中 `namespace`、`foreach`、三目运算符、`dynamic` 或 `object[]`；`git diff --check` 无空白错误，仅报告既有行尾格式提示。
- 未执行：Unity Play Mode 真实快速点按。需要在编辑器连续快速点按 W/A/S/D，确认松开后位移立即停止且 Walk/Run 切回 Idle；并回归跳跃空中按住方向的落地继续移动。

## [VERIFY][技能][TR-451] 2026-09-24 跳跃落地移动输入收敛返工验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4116—4125 行；历史时点记录，不代表当前仍在执行。

- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/controller/InputController/PlayerInputCtr.cs`、`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs`。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/air-jump-input-convergence-build/output --verbosity quiet`。
- 构建结果：成功，0 个错误、38 个项目既有警告。
- 静态检查：目标文件未新增 `namespace`、`foreach`、三目运算符、`object[]` 或 `dynamic`；`git diff --check` 未报告目标代码空白错误，仅有既有行尾格式提示。
- 逻辑检查：非零输入通过 started/performed 即时写入移动组件，33ms 采样负责持续重试；canceled、零输入周期对账、OnDisable 和 OnDestroy 都会清除输入并发送停止；方向输入从 true 变 false 时即使 `_isMoving` 已为 false 也会刷新默认动画。
- Play Mode：未执行。当前没有 Unity MCP 工具，桌面控制返回 `apps=[]` 和 `unsupported Codex auth method: apikey`，无法操控 Unity；此项必须保持为未验收。
- 待测：起跳后空中按住 W 至落地继续 Walk/Run；空中按 W 后落地前松开应播放 `Jump_End` 后 Idle；地面快速点按前后方向并松开后必须停止。

## [VERIFY][技能][TR-452] 2026-09-24 跳跃落地输入时序修复验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4126—4135 行；历史时点记录，不代表当前仍在执行。

- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/controller/InputController/PlayerInputCtr.cs`。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/client-jump-held-input-build/output --verbosity quiet`。
- 构建结果：成功，0 个错误，保留项目既有 38 个警告。
- 静态逻辑：33ms 对账只接收 `Movement.IsPressed()` 的实时值；取消回调记录事件时间；迟到旧回调不会重新派发移动；真实新按键时间戳更晚时仍可正常开始移动。
- 约束检查：本轮目标文件未新增 `namespace`、`foreach`、三目运算符、`object[]` 或 `dynamic`；业务载荷未使用无约束对象。
- 未执行：Unity Editor Play Mode。当前 MCP/桌面连接不可用，不能将运行时跳跃和快速点按行为标记为通过。
- 后续交接：在可运行 Unity 的环境中执行三条回归用例：空中按住方向落地继续移动、空中松开后落地停止、地面快速点按后立即停止。

## [VERIFY][综合][TR-453] 2026-09-25 快速松开方向键输入时序修复复核

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4136—4145 行；历史时点记录，不代表当前仍在执行。

- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/controller/InputController/PlayerInputCtr.cs`。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/quick-tap-final-build/output --verbosity quiet`。
- 结果：0 个错误、38 个项目既有警告。
- 静态检查：目标输入和移动文件未发现新增 `namespace`、`foreach`、三目运算符、业务 `object/dynamic`；`git diff --check` 未发现目标文件空白错误。
- 逻辑审查：`Movement.IsPressed()` 过滤已释放按键的残留采样；`canceled` 时间戳拒绝旧回调；零值对账、取消、禁用和销毁均派发停止，避免重新进入 Walk/Run。
- 独立审查：已有测试 Agent 的静态审查结论为通过；本轮协作接口无法成功启动新的独立 Agent，未伪造额外审查结论。
- Unity Play Mode：未执行。当前 Unity MCP 返回不可用，桌面连接为 `apps=[]`/`unsupported Codex auth method: apikey`；因此运行时快速点按行为仍待实测。

## [VERIFY][技能][TR-454] 2026-09-25 快速 S 键/双击移动动画问题（未修复）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4146—4153 行；历史时点记录，不代表当前仍在执行。

- 用户新增复现：快速按一下 S 或过快双击方向键后，移动动画仍保持，无法回到 Idle。
- 现有构建证据不能覆盖该运行时问题；之前构建为 0 错误、38 个既有警告。
- 本轮没有修改业务代码，也没有执行新的构建；原因是主协作接口连续返回参数解析错误，无法按约束委派客户端 Agent。
- Unity Play Mode 仍未执行，不能据此判定输入和动画状态已通过。
- 待验证用例：单次 S 短按、S 双击、W/S 快速反向、空中按键后释放；每条都要确认位移停止、`HasDirectionalInput=false`、`IsMoving=false` 且默认动作回到 Idle。

## [VERIFY][技能][TR-455] 2026-09-25 快速松开移动动画收敛修复复核

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4154—4166 行；历史时点记录，不代表当前仍在执行。

- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs`。
- 修复验证：`stopMove()` 在 `_hasDirectionalInput` 从 true 清零、但 `_isMoving` 已为 false 时，若当前为默认动作状态仍会调用 `MStateMachine.MarkTriggered()`，下一次 LateUpdate 会按 `HasDirectionalInput=false` 选择 Idle。
- 构建命令：`dotnet build D:/unitypro/LYFMMORGP/LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/quick-release-fix-build/output --verbosity quiet`。
- 构建结果：0 个错误、38 个项目既有警告。
- 静态检查：目标文件未新增 `namespace`、`foreach`、三目运算符、业务 `object/dynamic`；修改范围注释已说明竞态和生命周期。
- 未执行：Unity Play Mode；当前环境没有可用 Unity MCP/桌面连接，不能将快速 S 点按、双击和 W/S 反向标记为实机通过。

- 返工补充：`PlayerInputCtr` 的取消闸门阻断 canceled 后的残留采样，新的非零 started/performed 回调才会开启下一轮移动。

- 审查返工：非零移动回调增加 `Movement.IsPressed()` 实时校验；释放状态下只进入停止收敛路径，不会再次派发移动。

## [VERIFY][技能][TR-456] 2026-09-25 Idle 状态误播移动动画修复验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4167—4176 行；历史时点记录，不代表当前仍在执行。

- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MDefaultActionComponent.cs`、`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs`。
- 静态根因：`MDefaultActionComponent.ShouldPlayMovementAnimation()` 之前仅检查本地玩家的 `HasDirectionalInput`；兼容状态入口将 `IsMoving` 设为 false 后，残留方向标记仍会让 `StateName` 返回 Walk/Run。`SetMovingForPresentation(false)` 原先在状态已为 false 时直接返回，也不会清除该标记。
- 修复验证：本地移动动画必须同时满足 `HasDirectionalInput=true` 和 `IsMoving=true`；兼容入口重复收到 Idle 也会清除方向、预测位移并保持状态机的既有刷新链路。
- 构建命令：`dotnet build D:/unitypro/LYFMMORGP/LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/idle-walk-state-fix-build/output --verbosity quiet`。
- 构建结果：0 个错误、38 个项目既有警告。
- 静态检查：目标文件未新增 `namespace`、`foreach`、三目运算符、业务 `object/dynamic`；`git diff --check` 未发现目标范围空白错误。
- Unity Play Mode：未执行。当前环境没有可用 Unity MCP/桌面连接，需在编辑器中复测短按 S、快速双击、W/S 反向和兼容 Idle 切换，确认 Animator 最终进入 Idle 槽位。

## [VERIFY][综合][TR-457] 2026-09-25 兼容 Controller Idle 播放修复验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4177—4185 行；历史时点记录，不代表当前仍在执行。

- 追加修改文件：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/AsyncObj/MAnimator.cs`。
- 根因验证：MJS 的 `AnimCtrl_Role_jx` 使用 `Movement/Action` 参数和 Blend Tree，不提供 `Idle`、`NonZdMoveWalk` 等独立覆盖槽位；原 `Play/CrossFade` 在 `IsClipReady` 为 false 时直接返回，Animator 继续保持旧移动状态。
- 修复验证：`MAnimator.Play/CrossFade` 在剪辑不可用时调用 `MAnimatorStateCompatibility.Apply`；Idle 将 Movement 设为 0，Walk/Run 将 Movement 设为 2，技能等状态仍写入 Action 参数。
- 最终构建：`dotnet build D:/unitypro/LYFMMORGP/LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/idle-walk-state-fix-build-final/output --verbosity quiet`；0 个错误、39 个项目既有警告。
- 静态检查：三个目标文件未新增 `namespace`、`foreach`、三目运算符或业务 `object/dynamic`；`git diff --check` 无目标范围空白错误。
- Unity Play Mode：未执行，当前无法连接 Unity 编辑器；真实 Animator 槽位和快速按键仍需实机回归。

## [VERIFY][技能][TR-458] 2026-09-25 Idle/跑步兼容动画审查返工验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4186—4194 行；历史时点记录，不代表当前仍在执行。

- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/AsyncObj/MAnimatorStateCompatibility.cs`、`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs`。
- 返工验证：兼容 Controller 的跑步状态写入 `Movement=3`，行走状态写入 `Movement=2`；重复 Idle 调用在默认状态显式标记状态机刷新，技能、跳跃等特殊状态不会进入该分支。
- 构建命令：`dotnet build D:/unitypro/LYFMMORGP/LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/idle-walk-state-fix-review-build/output --verbosity quiet`。
- 构建结果：0 个错误、39 个项目既有警告。
- 静态检查：目标文件未新增 `namespace`、`foreach`、三目运算符或业务 `object/dynamic`；`git diff --check` 无目标范围空白错误。
- Unity Play Mode：未执行，当前环境无法连接 Unity 编辑器。

## [VERIFY][构建][TR-459] 2026-09-25 方向键最短保持时间构建验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4195—4204 行；历史时点记录，不代表当前仍在执行。

- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/controller/InputController/PlayerInputCtr.cs`。
- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/min-hold-input-build/output --verbosity quiet`。
- 构建结果：0 个错误、39 个项目既有警告。
- 静态检查：目标文件未新增 `namespace`、`foreach`、三目运算符、业务 `object/dynamic`；输入停止使用 Update 非阻塞计时，没有 `Sleep` 或协程竞态。
- 逻辑检查：started/performed 开始或更新保持窗口；canceled/零值安排停止；0.1 秒内继续派发最后方向，超过时间才清理并派发 Stop；重新按键会取消等待。
- 竞态检查：取消后的同一时间戳仅允许新的 Started 回调清除闸门，迟到 Performed 不会恢复移动；W/S 同帧反向仍可开始新输入。
- Unity Play Mode：未执行，当前环境无法连接 Unity 编辑器，需在编辑器回归短按 S、快速双击、W/S 反向、长按和空中输入。

## [VERIFY][技能][构建][TR-460] 2026-09-25 正式 MAnimator 嵌套状态路径构建验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4205—4214 行；历史时点记录，不代表当前仍在执行。

- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/AsyncObj/MAnimator.cs`。
- 资源核对：`MainCityMgr` 使用 `Assets/artres/Resources/Controller/MAnimator.controller`；该控制器的正式状态位于 `Base Layer.Idle.*`、`Base Layer.Move.*`、`Base Layer.Jump.*`，并未声明 `Movement` 或 `Action` 参数。
- 修复验证：Idle、FIdle、四种 Walk/Run、Jump_Start、Jump_Loop、Jump_End 均可映射到嵌套状态全路径；剪辑未就绪时先播放原生状态，兼容参数仅在 `Supports` 成功时执行。
- 构建命令：`dotnet build D:/unitypro/LYFMMORGP/LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/current-anim-path-build/output --verbosity quiet`。
- 构建结果：0 个错误、39 个项目既有警告。
- 静态检查：目标文件未新增 `namespace`、`foreach`、三目运算符、业务 `object/dynamic`；`git diff --check` 无新增空白错误。
- Unity Play Mode：未执行，当前环境无法连接 Unity 编辑器；需实测短按方向、快速双击和快速反向。

## [VERIFY][网络][TR-461] 2026-09-25 移动同步与状态同步设计文档产出验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4215—4227 行；历史时点记录，不代表当前仍在执行。

- 产出文件：项目分析/移动同步与状态同步设计.md（新增，未改动业务代码）。
- 文档结构核对：8 个章节齐全——名词解释（18 个术语，每个含是什么/为什么/本方案怎么用）、总体架构（Mermaid 数据流图+同步层级表+参数表）、移动同步详细设计、状态同步详细设计、AOI 视野管理、类与函数接口实现、协议设计、落地实施与验收。
- 代码块静态检查：所有 C# 代码块无
amespace、无
oreach（统一用
or 或数组 CopyTo 后按下标遍历）、无三目运算符 ?:（统一 if/else）；字段/方法均有中文注释说明用途与单位。
- proto 字段编号核对：ReqMoveReport 1~10、NotifyMoveReconcile 1~6、NotifyMoveBroadcast 1、MoveSnapshotItem 1~9、NotifyStateChange 1~5、NotifyAttrDelta 1、AttrDeltaItem 1~4、NotifyEntityEnterView 1、NotifyEntityLeaveView 1、EntitySnapshotData 1~12，无重复编号。
- 括号匹配抽查：服务端 MoveComponent/SyncComponent/AoiGrid/ViewManager/MoveReportHandler 与客户端 MoveReporter/MoveReconciler/RemoteMoveInterpolator 代码块括号闭合一致。
- 编译验证：本次为纯设计文档，未新增/修改业务代码，不触发 GM_Game.csproj 或 GameServer 编译。
- 已知遗留：ViewManager 差集辅助参考方法存在两个分支逻辑相同的冗余 if/else（不影响落地）；落地时按"一类一文件"拆分并简化。
- 未执行：Unity Play Mode、多开客户端实机同步观察（属于 P1~P6 落地阶段验收）。

## [VERIFY][网络][TR-462-FILES] 查询任务的正式文档产物：2026-09-26 移动同步与状态同步方案复核验证

- 该任务记录的正式文档路径：`项目分析/移动同步与状态同步设计.md`。
- 查询正文、当时状态及原文验证边界见 `Docs/AI/QUERY_TASKS.md` 的 `[TR-462]`；本条只保留文件产物追溯，不新增完成或检查结论。

## [VERIFY][技能][TR-463] 2026-09-26 MJS 攻击/技能 FBX 剪辑批量提取（Editor 自动化）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4237—4245 行；历史时点记录，不代表当前仍在执行。

- 操作：新增 `Assets/Editor/MjsAtkClipExtractTool.cs`，通过哨兵文件触发 `[InitializeOnLoad]` 自动执行，批量重命名 6 个 FBX 内剪辑并提取独立 .anim。
- 编译：Unity 2023.2.20f1c1 脚本域重载编译通过，Editor.log 无 `error CS`、无 NullReference/CompilerException。
- 文件系统：`Assets/GM_Game/Animas/Role/MJS/Atk/` 下新增 Anim_JX_Atk02/Atk03/Skill01/Skill02/Skill03/Skill04 共 6 个 .anim 及同名 .meta；连同原有 Atk01 共 7 组 FBX+.anim。
- 内容抽查：Atk02/Skill01/Skill04 三个 .anim 文件头 m_Name 正确；各文件 FloatCurve 块 130（Skill04=135），关键帧数 2389~7189 不等，证明为各剪辑独立数据。
- 导入设置核对：6 个 FBX.meta 的剪辑 name 已与 FBX 同名，takeName 原样保留，帧范围与任务表一致，Skill04 avatarSetup=1 其余=2。
- 结论：通过。未做运行时播放验证。

## [VERIFY][技能][TR-464] 2026-09-26 MJS 独立动画剪辑重命名并移动（Editor 自动化）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4246—4253 行；历史时点记录，不代表当前仍在执行。

- 操作：新增 `Assets/Editor/MjsAnimRenameMoveTool.cs`，哨兵触发自动执行，MoveAsset 移动+重命名 7 个 .anim（JX→MJS）到 Common/ALL/Skill，并同步内部 clip.name。
- 编译：Unity 2023.2.20f1c1 脚本域重载编译通过，无 error CS、无 Exception。
- 文件系统：Skill 目录新增 7 个 Anim_MJS_*.anim 及同名 .meta；Atk 目录仅剩 7 个 FBX 及 .meta，无 .anim 残留。
- 内容抽查：Anim_MJS_Atk02.anim、Anim_MJS_Skill04.anim 文件头 m_Name 均为 Anim_MJS_*。
- 结论：通过。GUID 由 MoveAsset 保持不变，未做运行时引用绑定逐一核对。

## [VERIFY][技能][TR-465] 2026-09-26 RO 技能模块测试基线（开发进行中）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4254—4264 行；历史时点记录，不代表当前仍在执行。

- 验证命令：`git status --short`、`rg --files`、`rg -n`；使用打包 Python 的 `openpyxl.load_workbook(read_only=True, data_only=True)` 只读解析 `Luaban/MiniTemplate/Datas/#SkillInfo.xlsx`、`#SkillEffectInfo.xlsx`、`#SkillPresentationInfo.xlsx`，并使用 `Get-FileHash -Algorithm SHA256` 记录旧文件基线。
- 表格结果：旧技能 7 行、新效果 5 行、新表现 5 行；主键、`(SkillId, Level)` 与表现变体组合键无重复；效果的旧技能外键、表现的效果外键均有效；无目标模式的未来命中字段均为零，时间、费用和范围字段无负数。
- 旧文件基线：`#SkillInfo.xlsx` SHA-256 为 `90E32FE72DAB4BE4ECD573C59E222EEBA1F95C3116D08E50308D12634A854F47`；`LYF_Server/Network/LubanManage/Tb/tbskillinfo.bytes` SHA-256 为 `6878777A134EB3182D0F7DECEFEF9A699617E331982084CCAA5668C407287256`。该值是本轮检查时的工作区基线，并非 Git 历史原件证明。
- 动画结果：5 条 `ClipLocation` 均有实际 `.anim` 文件，均出现在 `animInfo.txt`，短名与文件名一致，`DurationMs` 与索引秒值换算误差小于 1ms；职业 1 的 `PrefabPath=Role/Role_MJS`、`PresentTableID=1`，表现表模型为 `Role_MJS`。
- 协议入口只读检查：`11170/11171/11173` 已在 `NetDefine` 定义，`RequestEntity.proto` 和 `ResultEntity.proto` 已定义施法请求、结果与起止通知，Gate 已登记请求和三条下行。此项只证明当前源码登记，生成物一致性及实际转发留待构建和联调。
- 后续测试矩阵：Luban 新旧表双端装载；proto 生成与 `Network.dll` 一致；Network、GameServer、GateServer、CenterServer、Unity 客户端构建；改动文件的中文注释、一类一文件、禁用 `namespace`/`foreach`/三目与弱类型载荷；成功施法的一次 MP/CD 提交、一次开始/结束通知；未学/零级、等级效果缺失、职业不符、MP 不足、CD/组 CD、死亡、未装载、非法复合会话、重复序号与同帧双请求；本人预测回滚、他人表现和资源释放。
- 时序专项：核对 11080 技能列表在在线角色激活后装载；旧角色重连处于待保存状态时，若进入世界只请求一次，检查新会话激活后是否还能收到技能列表。条件不足时记为未验证风险，不视作已具备装载能力。
- 当前未执行：开发尚在进行，暂不对缺少业务接线作最终失败判断；构建、专用施法测试、四服务联网与 Unity Play Mode 待代码稳定后执行。当前只读检查未生成临时文件。

## [VERIFY][网络][技能][构建][TR-466] 2026-09-26 RO 技能协议 Network 独立构建

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4265—4273 行；历史时点记录，不代表当前仍在执行。

- 命令：`dotnet build LYF_Server/Network/Network.csproj --no-restore --configuration Debug -p:OutputPath=D:/unitypro/LYFMMORGP/_temp/skill-network-check/bin/ -p:BaseIntermediateOutputPath=D:/unitypro/LYFMMORGP/_temp/skill-network-check/obj/ --verbosity quiet`。
- 首次结果：沙箱内访问 `C:/Users/lyf/AppData/Local/Microsoft SDKs` 被拒，MSBuild 报 MSB4184；这是运行环境权限失败，未进入项目代码编译。
- 复验结果：按工具权限流程在沙箱外执行同一命令，构建成功，0 错误、4 个 `MMoRpgCommon` 既有警告（CS0114 一条、CS0169 三条）。
- 产物：`_temp/skill-network-check/bin/Network.dll`，279040 字节；中间文件在 `_temp/skill-network-check/obj/`。未复制或覆盖 Unity 客户端 DLL，也未据此声明客户端接线完成。
- 协议命令：本机 `protoc 3.21.9` 对 `RequestEntity.proto`、`ResultEntity.proto` 执行 `--descriptor_set_out` 与 `--csharp_out`，临时结果分别位于 `_temp/skill-network-check/skill-proto-descriptor.pb` 和 `_temp/skill-network-check/proto-generated/`；未改正式生成代码。
- 协议结果：重新生成的 `SkillCastReq` 2 个字段、`SkillCastRet` 10 个字段、`SkillCastStartNtf` 6 个字段、`SkillCastEndNtf` 5 个字段，与正式 `Network/Proto` C# 生成文件逐项比较，属性名和字段号均一致。整文件 SHA-256 不一致，因此本条仅证明新增四个消息的字段契约一致，不证明历史整文件字节一致。

## [VERIFY][技能][TR-467] 2026-09-26 RO 技能配置运行时装载首次测试（失败，待返工）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4274—4280 行；历史时点记录，不代表当前仍在执行。

- 专用测试：新增 `LYF_Server/Tests/SkillConfigSmoke.cs`，不加入正式服务工程；它在运行基目录检查三张技能表，并调用 `LubanMgr.Instance.Init()`，核对五组旧技能、一级效果与职业一表现关系。测试入口编译至 `_temp/skill_build/Network/SkillConfigSmoke.exe`，无编译错误。
- 编译命令：VS2019 `csc.exe /target:exe /out:D:/unitypro/LYFMMORGP/_temp/skill_build/Network/SkillConfigSmoke.exe /r:<同目录 Network.dll> /r:<同目录 MMoRpgCommon.dll> /r:<同目录 Google.Protobuf.dll> LYF_Server/Tests/SkillConfigSmoke.cs`。
- 运行命令：`_temp/skill_build/Network/SkillConfigSmoke.exe`；退出码 1，`LubanMgr.Init()` 在 `LubanMgr.cs:48` 读取构建输出目录时报 `FileNotFoundException`：缺 `_temp/skill_build/Network/LubanManage/Tb/tbcountinfo.bytes`。测试确实进入输出目录优先分支，而非回退读取源码目录。
- 根因静态复核：`Tables.cs` 共装载 16 张二进制表，`Network.csproj` 的 `<Content>` 当前只登记 14 张；缺 `tbcountinfo.bytes` 和 `tbpresenttable.bytes`。失败已反馈服务端开发 Agent；补齐构建输出后原样重跑。构建输出和中间测试产物均在 `_temp`，测试源码位于专用 Tests 目录。

## [VERIFY][技能][TR-468] 2026-09-26 RO 技能配置运行时装载隔离复测

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4281—4286 行；历史时点记录，不代表当前仍在执行。

- 为隔离运行时逻辑与项目文件复制缺项，仅将源码已有的 `tbcountinfo.bytes`、`tbpresenttable.bytes` 复制到 `_temp/skill_build/Network/LubanManage/Tb`，未修改 `Network.csproj` 或正式输出。
- 原样运行 `_temp/skill_build/Network/SkillConfigSmoke.exe` 通过：`LubanMgr.Init()` 实际从构建目录装载 16 张表，旧技能 5、新效果 5、新表现 5 的关系校验全部通过。
- 结论：运行时表解析和新旧技能索引有效；`Network.csproj` 未登记两张既有表的发布缺项仍未修复，不能把本次隔离复制结果视为正式构建发布通过。

## [VERIFY][技能][TR-469] 2026-09-26 RO 技能正式输出及权威组件复验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4287—4298 行；历史时点记录，不代表当前仍在执行。

- 正式发布输出：服务端 Agent 在 `Network.csproj` 补齐 `tbcountinfo.bytes`、`tbpresenttable.bytes` 后，测试 Agent 使用全新 `_temp/skill-network-publish-retest/bin/` 构建 `Network.csproj`，0 错误、4 个公共库既有警告；输出目录现含 `Tables.cs` 所需的 16 张二进制表。首次沙箱内构建仍被本机 Windows SDK 路径权限拒绝，按权限流程重跑成功。
- 配置复验：`SkillConfigSmoke.cs` 重新编译引用全新输出中的 `Network.dll`、`MMoRpgCommon.dll` 和 `Google.Protobuf.dll`，并在同目录运行 `SkillConfigSmoke.exe`；退出码 0，`LubanMgr.Init()` 实际从该输出装载 16 张表，旧技能 5 / 新效果 5 / 新表现 5 的映射通过。本次不依赖前述手工复制的隔离产物。
- 服务端权威专用测试：新增 `LYF_Server/Tests/SkillAuthoritySmoke.cs`，不加入正式业务工程；以 VS2019 `csc.exe` 引用 `_temp/skill_build/Network` 中 `GameServer.exe`、`Network.dll`、`MMoRpgCommon.dll`、`Google.Protobuf.dll` 编译至同目录。首次执行测试 exe 因缺少 GameServer 程序集重定向配置在入口前异常退出 `-532462766`；仅在 `_temp` 内复制同目录 `GameServer.exe.config` 为测试 exe 配置后复验退出码 0。
- 权威断言通过：技能列表未装载时拒绝且 MP 不变；10010 一级成功扣 20 MP、生成非零服务器施法 ID 与开始通知；同序号同技能重传返回同一结果且不二次扣 MP/广播；结束活动施法后新序号仍受单技能 CD 拒绝；未学习、非法技能 ID、MP 不足与死亡拒绝路径均不扣费。测试角色无真实 Gate 会话，`InterruptActiveCast` 产生一次预期发送失败日志，不代表联网广播已验证。
- 静态约束：`rg` 对新增 `RoleSkillComponent`、`SkillCastHandler`、`PendingEnterWorldRequest`、`GateSkillCtrl`、客户端 `Skill` 目录、`SkillStartedEventArgs` 和 `SkillCtrl` 检查 `namespace`、`foreach`、三目运算符、`dynamic`、`object[]`/业务弱类型载荷，目标范围 0 命中。生成的 Luban/protobuf 文件是第三方生成边界，按项目例外不手改。
- 协议登记：11170/11171/11173 在 `NetDefine`；Gate 上行 11170、下行三条均登记且转发器解析消息；GameServer 11170 仅从 Gate 入逻辑线程 Handler；客户端 `SkillCtrl` 登记并注销三条回包处理。字段号另见前述临时 `protoc` 对照记录。
- 重连时序静态复核：新会话暂存在 `m_pendingRoleMap` 时，`HandleClientEnterWorld` 按同一 Gate/Unity 复合会话保存唯一请求；旧实例保存完成后 `ProcessPendingRoles` 先激活新角色，再重放该请求到 Center，11080 回包经复合会话定位新角色并调用 `LoadLearnedSkills`。伪造或过期会话不进入该重放路径；实际 DB ACK 时序和 11080 网络到达顺序尚未联调。
- 表现回收静态复核：客户端 `ApplyServerEnd` 仅接受当前 `ServerCastId`，同一动画 Location 的旧结束通知不能结束后一施法；`MAnimator.ReleaseAnim` 要求当前 `ClipPath` 与预期 Location 匹配，`MAnimInfo.Reset` 取消 preparing/ready 租约。尚未在 Unity Play Mode 验证模型加载、实际播放、断线或池复用。
- 未覆盖：真实四服务联网、客户端预测及他人广播、组 CD（当前 5 条真实效果的 `CooldownGroupId` 均为 0）、非零目标模式（当前真实数据均为 0）、效果缺失或职业不符的运行时分支、同帧双请求和服务器自然结束通知次数；这些保留为后续专项/联调验收，不以本次组件 smoke 代替。

## [VERIFY][技能][TR-470] 2026-09-26 RO 技能角色快照隔离与早到回包复审

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4299—4308 行；历史时点记录，不代表当前仍在执行。

- 修改文件：新增 `LYF_Server/Tests/SkillRoleSnapshotSmoke.cs`，仅作专用测试，不加入正式服务工程；测试变更说明归档于 `项目修改日志/2026-09-26-RO-SKILL-ROLE-SNAPSHOT-TEST-CHANGELOG.md`。
- 协议验证：`ResultEntity.proto` 的 `RoleSkillInfoRet.role_id = 3` 已由 CenterServer `CentRoleModel.RoleSkillInfo` 填入；正式 `ResultEntity.cs` 生成物的字段号和名称一致。`protoc 3.21.9` 对 `ResultEntity.proto` 成功生成 `_temp/skill-role-snapshot/ResultEntity.desc`。专用测试另外用 Protobuf 描述符断言字段 3 名为 `role_id`。
- 隔离构建：VS2019 MSBuild 将 `MMoRpgCommon`、`Network`、`GameServer` 构建至 `_temp/skill-role-snapshot/bin/`，均 0 错误；公共库 4 条、GameServer 7 条既有警告。首次沙箱内构建因本机 Windows SDK 目录访问受限报 MSB4184，按权限流程在沙箱外重跑通过。测试入口用 VS2019 `csc.exe` 引用同目录程序集编译，只有 Google.Protobuf 3.31.0/3.31.1 引用版本提示；复制同目录 GameServer 绑定重定向配置到测试 exe 后运行。
- 角色切换断言：`SkillRoleSnapshotSmoke.exe` 退出码 0。角色 B 占有复合会话时，A 的迟到 11080 经 `RoleSkillInfoHandler.Handle` 拒绝且 B 技能组件仍未装载；同会话 B 回包通过 `IsMatchingRole`，并由 B 组件成功装载。测试无真实 Gate，会话匹配后的转发由源码静态复核，未声明真实网络转发通过。
- 转发顺序：GameServer 将 11080 注册为 `CenterMessageRoute.Internal`；处理器先解析 `RoleId` 并按 Gate/Unity 复合会话核对当前在线角色，再允许 Gate 转发与组件装载。客户端 `SkillCtrl` 在本地角色未创建时按 RoleId 和连接代次暂存 11080；`LocalRoleChanged(null)` 保留同连接待应用快照但清空上一角色视图；新角色仅消费同 RoleId 快照，`Dispose` 清空缓存。客户端早到时序为静态复核，未执行 Unity Play Mode。
- 无表现技能：客户端成功结果先于开始通知到达时会记录服务器施法 ID；`CanStart` 在结束通知前阻止下一请求，11171 重复开始不重建实例，11173 按 ID 清理。该路径为静态复核，未做 Unity 运行验证。
- 产物一致性：项目正式 `_temp/skill_build/Network/Network.dll` 与 Unity 插件 `LYF_MMO/Assets/GM_Game/Plugins/Net/Network.dll` 的 SHA-256 均为 `AF805591BFC54F124315A474C0450FACA195861E357025954DB9A3A7BF3DE54B`。本次独立隔离构建的 DLL 哈希不同，因此专项测试仅证明当前源码的角色隔离行为，不以其字节哈希证明客户端插件一致。
- 最终结论：本轮代码审查提出的跨角色 11080 装载、旧技能缓存、无表现施法占用三项问题已修复并通过上述专项/静态验收。剩余 Warning：真实四服务联网、同连接换角迟到回包、Unity Play Mode 动画与 UI 早到回包未联调；后续按正式场景验证。

## [VERIFY][技能][TR-471] 2026-09-27 技能全学习模拟服务器与按键动画专项

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4309—4316 行；历史时点记录，不代表当前仍在执行。

- 独立构建：测试 Agent 使用隔离目录构建 `GM_Game.csproj`，0 错误、42 条既有警告；产物位于 `_temp/skill-playmode-review/build`。
- 静态审查：新增模拟服务器、Play Mode 探针与 Editor 入口未命中 `namespace`、`foreach`、三目或弱类型载荷；`git diff --check` 通过。审查确认测试对象只在运行态创建，不序列化进场景。连接关闭后的 Receive=0 清理仍属小风险，新增脚本 `.meta` 需 Unity 导入生成。
- Unity 状态：进程 37444 仍在运行且 Responding=True，窗口标题为 `LYF_MMO - New Scene`；Computer Use 返回空应用清单并报告浏览器认证错误，无法触发 Unity 菜单。Editor.log 最后更新时间 11:51；哨兵 `_temp/skill-playmode/start.pending` 仍在。
- Play Mode 结果：`_temp/skill-playmode/result.log` 不存在，技能截图不存在；因此没有证据证明 Mock TCP 已启动或 11080 已返回，也没有按键/TCP 11170/11171/11173/Animator clip/Idle 实测数据。本轮动画验收未通过，状态为未执行（环境阻塞），不得以独立构建通过代替运行验收。
- 后续：Unity 编辑器可操作后运行 `Tools/测试/全部学习技能与按键动画实测`，检查全部五个有表现配置技能的真实输入链和动画证据；同时确认 Unity 为新增测试脚本生成 `.meta`。详细记录见 `项目修改日志/2026-09-27-SKILL-MOCK-ANIMATION-CHANGELOG.md`。

## [VERIFY][技能][TR-472] 2026-09-27 技能全学习模拟服务器与按键动画专项复测通过

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4317—4326 行；历史时点记录，不代表当前仍在执行。

- 修改后构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output _temp/skill-input-build2 --verbosity quiet -clp:ErrorsOnly`，0 错误、42 个既有警告。
- 组件边界复核：`RoleAnimaBehaviour` 未被销毁，测试夹具仅设置 `enabled=false`；正式 `PlayEffect` 对失活组件直接返回。目标文件静态扫描无禁用语法命中，`git diff --check` 通过。
- Unity Play Mode 运行证据：2026-09-27 04:44:52Z 新轮次启动 Mock；11080 返回 RoleId=10001 的 7 项技能，全部 Level=1。Q/10010、E/10040、R/10050、F/10060、1/10070 均各有一次 11170、11171、11173；对应剪辑 `Anim_MJS_Atk01`、`Anim_MJS_Skill01`、`Anim_MJS_Skill02`、`Anim_MJS_Skill03`、`Anim_MJS_Skill04` 进度从 0 增长至 0.9828555~0.9939035，且 `结束Idle=true`。
- 自动化结论：`result.log` 末行是 `PASS 全部5项真实按键/TCP/Animator进度/结束Idle；Mock保持运行，Q/E/R/F/1可手动测试，退出Play自动关闭`。新轮次未出现旧的 `RoleAnimaBehaviour.DoAttackConfig` 空引用。
- 手动焦点补验：向 Unity 主窗口发送 Q、E 后，Mock 日志追加 Seq=6、Seq=7 的 11170/11171/11173，证明物理键盘路径能到达正式技能 TCP 链。
- 证据位置：`_temp/skill-playmode/result.log`、`_temp/skill-playmode/desktop.png`、`_temp/skill-playmode/manual-e.png`；旧失败轮次保留为 `_temp/skill-playmode/result-partial-2026-09-27-0425Z.log`。临时文件不清理，待产品经理确认。
- 最终状态：本次用户验收通过。未覆盖真实四服联机、伤害结算、Buff、多人广播和特效资源完整性，作为后续专项范围。

## [VERIFY][技能][TR-473] 2026-09-27 现有测试角色模拟技能回包与手动按键准备

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4327—4336 行；历史时点记录，不代表当前仍在执行。

- 正式链路静态复核：`test.cs` 在模拟角色生成后由旧技能表构造全项一级 `RoleSkillInfoRet`，调用 `ToByteString()`，经 `MainCtrl.LoadSkillResponse`、`SkillCtrl.TryParseRoleSkillInfo`、角色 ID 校验装载技能核心与两个 UI 窗口。联网回调仍保留连接代次校验；离线按键进入 `BeginLocalCast`，不以 TCP 连接为前置条件。
- 测试角色状态：专用测试脚本将 HP/MP 设为 1000，满足本地施法 MP/存活校验；模型加载后仅设置运行态 `RoleAnimaBehaviour.enabled=false`，不修改正式预制体。
- 界面证据：撤下探针前的 `_temp/skill-playmode/skill-ui-timeout.png` 可见 Q/E/R/F/1 五个快捷图标。该探针把预制体内 inactive 模板计入条目数而误判失败；截图只证明当时快捷栏显示，不能证明最新测试脚本的手动按键动画。
- 构建与审查：最新 `GM_Game.csproj` 构建 0 错误、38 条既有警告；目标文件 `git diff --check` 与禁用语法扫描通过。旧 `#SkillInfo.xlsx` 与 `tbskillinfo.bytes` 哈希保持既有基线。自动探针、Mock TCP 与 Editor 启动脚本已移除，生成项目文件已同步去掉两条已删除脚本索引。
- 未执行：按用户要求不再用代码注入键盘或自动播放测试；最新版本 Play Mode 中用户手动按键的 Animator 剪辑、进度、回 Idle 尚未验证，不能标为通过。
- 手动验收：打开 `New Scene` 进入 Play，控制台应显示技能回包装载数量；按 L 检查技能列表，按 Q/E/R/F/1 检查角色动画、冷却和可重复释放。失败时按具体现象返工。
- 编辑器状态核对：`GM_Game.dll` 更新至 2026-09-27 14:01:50，晚于本轮业务与测试脚本源文件；最终截图 `_temp/skill-playmode/editor-final-mode.png` 显示 Unity 停在 Edit Mode。未把旧 Play 域的自动探针日志用于本次验收。

## [VERIFY][技能][TR-474] 2026-09-27 技能结束回位定位与修复验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4337—4344 行；历史时点记录，不代表当前仍在执行。

- 根/骨骼区分：临时加载 `Role_MJS` 并采样 Skill01，模型根 Y 始终为 0；Idle 骨盆 Y≈1.171，Skill01 t=0.4 骨盆 Y≈0.637，双脚 Y≈0.19/0.14。原剪辑中段的骨盆下移存在于动作曲线，不能把它归因于模型 Transform 被重设。关闭 Y Bake 的临时克隆实验使根 Y≈-0.459，骨盆与双脚世界高度不变；正式资源未修改。
- 时序复现：Skill 动作结束时 `Finish` 同步进入 `AfterFinish` 和 `MSkill.Reset`，原 `ReleaseAnim` 先恢复占位槽；`MStateMachine.UpdateSeconds` 随后才转默认，Idle 过渡在 LateUpdate 执行。Unity 临时 Animator 实验确认提前恢复时当前 Animator 状态仍为 Skill，但 `CurrentAnimatorClipInfo` 已从 `Anim_MJS_Skill01` 变成占位 `Skill`。
- 修复验证：`MSkill.Reset` 请求延迟释放；`MAnimator.Update` 逐帧确认不在 Skill、且过渡结束后才释放。Unity 临时 Animator 对照中，Skill 期间保持真实剪辑，切 Idle 后释放；旧 Skill01 待释放后新 Skill02 成功接管、以及新剪辑加载失败仍保留旧 ready 再于 Idle 回收，两条路径均通过。临时实例已销毁，场景 dirty=false，Console 错误 0。
- 构建与审查：客户端子 Agent 构建 `GM_Game` 0 错误/38 条既有警告；独立审查 Agent 构建 0 错误/42 条既有警告，目标文件 `diff --check` 与禁用语法扫描通过。审查覆盖新施法同槽接管、异步成功/失败、Reset 清理。
- 实测边界：未替用户在 Game View 触发物理按键并目测最终效果；原动作中的下蹲/跳跃曲线仍保留，用户手动复验后才能确认感知上的“下沉”是否已满足预期。

## [VERIFY][技能][TR-475] 2026-09-27 施法期间移动阻断复测

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4345—4352 行；历史时点记录，不代表当前仍在执行。

- 独立构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/skill-move-review/rebuild --verbosity quiet -clp:ErrorsOnly`，0 错误、38 条既有警告；客户端开发侧构建同为 0 错误，Unity 脚本重编后 Console 错误 0。
- 静态时序：`MSkillComponent` 禁水平移动并于施法开始清除旧速度；`MMoveComponent` 在输入与逐帧积分两处拦截技能状态，仍推进垂直重力、CharacterController 位移及接地。方向键保持期间 `PlayerInputCtr` 约每 33 毫秒重派输入，技能结束后可重新进入移动。
- 审查返工：首次审查发现空中施法落地可由 `MJumpComponent` 播放 Jump_End 或提前切默认，故未通过。开发修复后，独立复审确认技能期间落地只清理跳跃表现；Jump_End 已开始再施法时旧结束标记和待播项清除，不会由旧时钟中断技能。物理 `EndJump` 保留，复审通过。
- 规范检查：三份目标文件无新增 `namespace`、`foreach`、三目、`dynamic`、`object[]` 业务载荷，目标差异空白检查通过。构建产物保留于 `_temp/skill-move-review/rebuild`。
- 运行边界：未用代码注入键盘或代替用户做 Game View 手动按键；按住方向施法、空中落地和技能结束恢复的画面及手感仍需用户确认。

## [VERIFY][技能][TR-476] 2026-09-27 技能按键入口简化验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4353—4360 行；历史时点记录，不代表当前仍在执行。

- 最终独立构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/skill-input-simplify-review --verbosity quiet -clp:ErrorsOnly`，0 错误、38 条既有警告；隔离产物时间晚于最终 `SkillCtrl.cs` 修改。
- 静态路径：直接装载的模拟技能回包按键走 `BeginLocalCast`，无连接状态前置判断；正式网络 11080 在传输可用时走预测和 11170 请求，传输不可用时仍播放本地表现。联网回包维持施法序号匹配，避免重复播放。
- 审查返工：连接代次切换时先撤销旧预测，再清待确认序号；角色切换时，即使命中暂存的早到 11080，也先清旧角色待确认和迟到序号，防止旧结果写入新角色。直接注入切本地会话时清理旧预测并过滤本地主角旧开始/结束广播。独立代码复审通过。
- 规范检查：目标文件无新增 `namespace`、`foreach`、三目、`dynamic` 或 `object[]` 业务载荷；目标 `git diff --check` 通过。构建中间产物保留在 `_temp/skill-input-simplify-review`。
- 运行边界：未使用代码注入键盘或自动探针；Unity Game View 的 Q/E/R/F/1 动画、冷却显示与真实四服回包顺序尚待手动/联调验证。

## [VERIFY][技能][界面][TR-477] 2026-09-27 RO SkillUuid 整改与独立复审

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4361—4369 行；历史时点记录，不代表当前仍在执行。

- RO 源码核对：`MPlayer.NextSkillUUID` 为 1 至 9999 循环，`MNavigationComponent` 用同一 UUID 请求和预表现，`MSkillComponent` 的开始/结束通知按 UUID 匹配当前或旧实例，服务端将请求 UUID 原样传入施法实例。RO 客户端连击重置 10 秒、服务端默认组合窗口 6 秒，本项目对应值一致。
- 协议与静态审查：`SkillCastReq`、`SkillCastRet`、`SkillCastStartNtf`、`SkillCastEndNtf` 均为 `SkillUuid`；业务源码不再有 `ClientCastSeq`、`ServerCastId`、服务器另分配 ID、最大序号门槛及历史结果缓存。原 `server_cast_id` 的结果包字段号 5 标记 reserved。目标文件 `git diff --check` 及禁用语法扫描通过。正式 Unity `Network.dll` 与本轮服务端构建产物 SHA-256 同为 `E3029A1B6ADBAC8E32D5D9161D39B529477FBBE6D695A8029027ECD5380F14E3`。
- 编译：GameServer 隔离构建 0 错误、11 条既有警告；客户端 `GM_Game` 最终返工后隔离构建 0 错误、38 条既有警告，产物位于 `_temp/skill-uuid-client/no-presentation-final`。所有中间文件仍在项目根目录 `_temp`，未经确认未清理。
- 运行测试：独立测试 Agent 用本轮 GameServer/Network 重新编译并运行 `LYF_Server/Tests/SkillAuthoritySmoke.cs`，退出码 0；覆盖 UUID 原样回传、活动状态阻断、CD、未学习、MP 不足和死亡。测试夹具首次缺旧程序集绑定重定向配置而启动失败，补用既有配置后通过；测试角色没有网关会话产生一行预期发送失败日志。
- 返工复审：`MSkillComponent` 删除无动画纯占位后，缺表现时仍保存 UUID 预测以接收 11170 MP/CD，11171 仅警告且不占动作状态，超时会回滚；Q 旧段仍按对应 UUID 回收。独立复审通过。
- 未执行：Unity Game View 物理按键、视觉动画、真实四服消息顺序和远端角色广播。本轮构建与夹具不替代这些运行验收。

## [VERIFY][技能][TR-478] 2026-09-27 RO 施法实例列表初审、返工与复审

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4370—4377 行；历史时点记录，不代表当前仍在执行。

- 初审失败：对照 RO 的当前施法及 `_skills`，本项目仅多段移交旧实例，单段会继续占槽并阻断下一技能；旧列表没有逐帧运行。开发子 Agent 改为通用列表留存和更新，解除旧动画槽。
- 首次复审失败：A 在旧列表、B 在当前槽时重复 `SkillStarted(A/B)`，原动作入口先自转并移走 B，`OnStart` 查重后 `Finish` 提前结束 B。整改为 `OnSkillStartedEvent` 在调用 `_onActionEvent.Invoke` 前通过 `FindSkill(UUID)` 检查当前与列表，重复直接返回。
- 最终静态复审通过：重复 A/B 不进入状态自转；不同 UUID 在状态许可下启动，任意活动技能退出动作槽后保留；11173 仅清理匹配 UUID；列表实例逐帧更新，动画释放后 `_animationReleased` 禁止重装；清空与 Q 700 毫秒路径无新增阻断。
- 构建：客户端子 Agent 最终返工构建 0 错误、38 条警告；主 Agent 使用最终源码、将输出和中间文件全部定向 `_temp/ro-skill-lifecycle-final/` 重建，0 错误、42 条警告。独立只读复审进程的构建在 Windows SDK 路径访问时报 MSB4184，没有进入业务编译，不能计为代码失败。
- 测试边界：上述 A/B 和生命周期为源码时序审查，未生成角色探针或测试按键脚本，未执行 Unity Play Mode、实际断线或网络事件回放。当前无实际弹体数据/逻辑，不能宣称弹道运行已验证。

## [VERIFY][技能][TR-479] 2026-09-27 RO 技能对象池验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4378—4387 行；历史时点记录，不代表当前仍在执行。

- 初审与返工：底层池初版额外永久活跃集合、缺 RO 创建委托和列表池元素约束被打回；客户端子 Agent 恢复 RO `CreateObj` / `UnityAction`、原 `WINDOWS && DEBUG` 检查与计数语义，列表借还双清理，并补齐目标中文注释。最终独立审查通过，无阻断项。
- 正式源码纯内存验证：`dotnet run --project _temp/ro-skill-pool-tests/SkillPoolTests.csproj` 退出码 0，115 项断言通过；独立审查再以 `--configuration Release` 重编运行，115 项同样通过。证据 `_temp/ro-skill-pool-tests/result.log`。首次夹具编译缺 `Debug.LogError` 边界替身，补齐后通过，该失败不属于生产源码缺陷。
- 覆盖：技能/核心/数据/预测复用清零、施法持有升级前核心、预测引用转移、确认/取消/超时、同级快照丢弃、当前与旧实例结束/拒绝、多个旧实例与当前同时清空、重复开始广播新核心消费、缺表现和初始化失败归还；未调用池 Clear 的完整生产周期各池活跃计数平衡。
- 编译：客户端返工构建 0 错误、42 条既有警告；主 Agent 最终隔离构建 0 错误、42 条警告，输出和中间产物 `_temp/ro-skill-pool-final/bin` 与 `obj`；独立审查构建 0 错误、38 条警告，产物 `_temp/skill-lifecycle-review/pool-final`。
- 静态：目标文件禁用语法扫描无命中；`git diff --check` 退出 0，仅已有行尾转换提示；四份新池 `.meta` 与技能 `.meta` 存在，`GM_Game.csproj` 索引齐全；客户端业务链未发现直接构造四种技能池对象的调用。原表、协议、场景和资源未列入本次修改范围。
- 边界：夹具直接链接正式池与技能源码，Unity、实体/动作机、配置访问边界采用轻量替身；未运行 Unity Game View、实际异步资源和 Animator 画面、按键、弹体或四服联调。没有生成角色探针或将测试组件挂正式场景。
- 盘点与修改记录：`项目分析/RO_SKILL_OBJECT_POOL_AUDIT.md`、`项目修改日志/2026-09-27-RO-SKILL-OBJECT-POOL-CHANGELOG.md`。临时文件保留根目录 `_temp`。

## [VERIFY][技能][TR-480] 2026-09-27 RO 技能子类与 10070 既有特效

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4388—4397 行；历史时点记录，不代表当前仍在执行。

- 范围：`MSkill` 抽象基类、`MArtSkill` 具体池借还、10070 剪辑事件特效与四份角色预制体旧动画事件组件失活。飞行物按用户要求暂不开发；Q 原有三段变体保持原流程。
- 审查闭环：旧 `RoleAnimaBehaviour` 双重播放风险打回后，四份正式预制体对应组件均 `m_Enabled: 0` 且完整保留；冷剪辑超过动作状态时长漏事件再次打回后，10070 独立持有 `MAnimationMgr.ClipInfo`，脱离可复用动作槽读取原事件。独立最终静态复审通过，引用借还对称，无跨回池异步完成回调。
- 构建：客户端返工及独立复审均 0 错误、42 条既有警告；最终独立产物在 `_temp/skill04-independent-review/cold-final/bin`、`obj`。目标新增/修改技能文件未发现 namespace、foreach、dynamic、object 业务载荷或三目语法；预制体差异空白检查通过，仅行尾转换提示。
- 纯内存回归：`_temp/ro-skill-pool-tests/result.log` 最终 171 项断言通过、退出码 0。夹具链接正式技能与池源码，Unity、资源、实体边界使用替身。覆盖两事件各一次、根/子粒子寿命、目标和无目标落点、池复用；新增实际组件 `StateUpdate(1.2s)` 超过 1.133 秒后自动退出动作槽并转旧列表，新技能替换共享槽后旧实例仍只读自己资源，拒绝/回池后资源晚到不补发，独立剪辑引用最终归零。
- 资源核对：`Anim_MJS_Skill04.anim` 约 0.1265/0.2969 秒事件正确引用 `Jx_Skill04.asset` 的两份粒子资源；落剑预制体没有弹体运动或碰撞组件。原表、动画与粒子资产未列入本轮写入范围。
- 验收边界：未运行真实 Unity 画面、挂点、YooAsset 冷加载或四服联调，不能将替身断言视为真实渲染通过。当前按键目标 UID 为零，大剑沿用角色前方 10 单位落点，无伤害结算。资源到达前施法若已被结束/拒绝/超时回收，不会补发；拒绝前已生成粒子会继续有限尾迹。
- 交接：用户在现有模拟角色手动按 1 确认表现。本轮未新增生成角色、按键探针或正式场景测试组件。修改文件、设计与返工记录见 `项目修改日志/2026-09-27-RO-SKILL-SUBTYPE-CHANGELOG.md`；RO 子类分析见 `项目分析/RO_SKILL_SUBTYPE_ALIGNMENT.md`。

## [VERIFY][技能][TR-481] 2026-09-27 RO 技能统一动画入口整改验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4398—4406 行；历史时点记录，不代表当前仍在执行。

- 文件范围：`SceneEntity/Skill/MSkill.cs`、`MArtSkill.cs`、`SceneEntity/Component/Action/MSkillComponent.cs`，以及根目录 `_temp/ro-skill-pool-tests` 夹具。未修改通用状态机、动画管理器、表格、协议、场景或美术资源。
- RO 对照与独立审查：通过。原 `Begin → TryPrepareAnimation` 及 Update 重试移除，使用 `Fire → Execute → Start` 启动逻辑；状态机统一 `PlayAnim → TriggerAnim` 覆盖/播放动画。Skill 组件不调用基类通用 PlayAnim，避免其缺少通用 Skill 字段时回退 Idle。Start 每次施法仅执行一次，10070 独立剪辑引用在 Reset 释放，旧实例只能继续特效时间轴，不能重装当前动画。
- 构建：客户端开发与独立审查构建均 0 错误、42 条既有警告。独立产物保留 `_temp/ro-skill-animation-interface-review`；旧接口与禁用语法扫描无命中，目标差异检查通过。
- 运行回归：`dotnet run --project _temp/ro-skill-pool-tests/SkillPoolTests.csproj` 退出码 0，189 项断言通过；证据 `_temp/ro-skill-pool-tests/result.log`。原 171 项迁移至 Fire 接口，覆盖 Q 700 毫秒、UUID、核心/预测/技能池、1 号特效、冷加载和拒绝回收。新增验证正式 Fire/Update 不调用 Override/Play/CrossFade；正式组件 PlayAnim 只触发一次覆盖与 0.1 参数 CrossFade、基类 PlayAnim 调用数为零；后续 Update 不重播；模型缺失经过 0.4 秒后统一入口以 0.2 归一进度 Play；已退出动作槽的旧实例 TriggerAnim 不抢槽。
- 静态补充：正式 `MStateMachine.LateUpdate` 的模型空值条件保留 `_isTriggered`，Animator 可用后调用 PlayAnim 并消费触发标记；该真实状态机未纳入本轮替身夹具。`Global` 在动画索引预加载成功后才创建场景世界并消费角色，技能 Start 单次查询剪辑符合既有初始化顺序。
- 边界：夹具仍使用 Unity/资源/模型边界替身，未运行 Unity Game View 实际按键、冷资源加载或四服联调。用户手动复验现有模拟角色 Q/E/R/F/1；没有新增角色或按键探针。日志见 `项目修改日志/2026-09-27-RO-SKILL-ANIMATION-ENTRY-CHANGELOG.md`。

## [VERIFY][技能][TR-482-FILES] 查询任务的正式文档产物：2026-10-02 RO 技能表现参考文档核验

- 该任务记录的正式文档路径：`项目分析/RO_SKILL_FX_ANIMATION_ROOT_MOTION_REFERENCE.md`、`项目修改日志/2026-10-02-RO-SKILL-FX-ANIMATION-REFERENCE-CHANGELOG.md`。
- 查询正文、当时状态及原文验证边界见 `Docs/AI/QUERY_TASKS.md` 的 `[TR-482]`；本条只保留文件产物追溯，不新增完成或检查结论。

## [VERIFY][技能][TR-483-FILES] 查询任务的正式文档产物：2026-10-02 RO 技能编辑器参考调查核验

- 该任务记录的正式文档路径：`项目分析/RO_SKILL_EDITOR_REFERENCE.md`、`项目修改日志/2026-10-02-RO-SKILL-EDITOR-REFERENCE-CHANGELOG.md`。
- 查询正文、当时状态及原文验证边界见 `Docs/AI/QUERY_TASKS.md` 的 `[TR-483]`；本条只保留文件产物追溯，不新增完成或检查结论。

## [VERIFY][综合][TR-484-FILES] 查询任务的正式文档产物：2026-10-07 YCombatEditor 融合方案审查

- 该任务记录的正式文档路径：`项目分析/YCOMBAT_EDITOR_INTEGRATION_DESIGN.md`。
- 查询正文、当时状态及原文验证边界见 `Docs/AI/QUERY_TASKS.md` 的 `[TR-484]`；本条只保留文件产物追溯，不新增完成或检查结论。

## [VERIFY][综合][TR-485] 2026-10-07 YCombatEditor 正式工程首轮导入（续接，非最终验收）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4428—4437 行；历史时点记录，不代表当前仍在执行。

- 在无正式 Unity Editor 进程情况下启动唯一 Unity 2023.2.20f1c1 batch 导入；嵌入包被 Package Manager 正确识别，最终返回 0。证据 `_temp/ycombat-integration/initial-import.log`。
- 首次资源扫描曾产生 CS0246（新 SkillRuntimePresentation/SkillRuntimeFxEvent 尚未导入）和强制重编译输出目录异常；后续资源刷新生成新类型索引，再编译 GM_Game、复制 ScriptAssemblies 并完成 ReloadAssembly。失败痕迹保留，不以该轮退出码认定最终编译门禁通过；最终冻结后须重新编译无错误。
- 运行开发子 Agent 临时全量源码索引构建 0 错误、43 条警告，日志 `_temp/ycombat-runtime-build/build.log`。修复旧事件冷回退、模型晚到不吞事件、特效真实尾迹和独立依赖租约；尚待独立审查和正式 Unity 复验。
- 原配置/动画保留核验基线共 44 个文件记录到 `_temp/ycombat-integration/preserved-assets-baseline.json`；该基线仅供本次实施后对比，不替代版本历史。
- 专门审查 Agent 已对特殊规则输出未通过：当前固定禁移动/互断无法由编辑资产配置。本轮委派 `casting_rules` 实际接入 RO InteruptSkill/MoveCasting，默认空/false；技能中主动起跳保持明确禁用，已有空中重力继续。未增加无效跳取消字段或新协议。
- 平台因 agent thread limit 拒绝再次启动专门终审 Agent，采用开发子 Agent 交叉独立审查：运行开发者审包/导出/规则，编辑端开发者审运行资源/时序，各自不审核自己写的代码作为独立结论。
- 未执行真实键盘、Game View、渲染预览或 YooAsset 实包加载；未生成游戏角色或键盘探针。

## [VERIFY][技能][TR-486] 2026-10-07 首次正式技能资产生成失败与返工

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4438—4444 行；历史时点记录，不代表当前仍在执行。

- 执行 `SkillEditorAssetTools.GenerateAndExport`，日志 `_temp/ycombat-integration/generate-assets.log`，Unity 返回 1。
- 失败点：生成器误把旧表已含 `Assets/.../Anim_MJS_Atk01.anim` 的完整 Location 当作短名再次拼目录/扩展名，实际动画文件存在，导致 AssetDatabase 查询为空。已打回编辑开发子 Agent 适配真实原表路径，禁止改旧表或替换资源。
- 新增包/规则源文件首次导入期间有暂态缺类型编译错误，随后进入生成方法；最终冻结后重新运行干净编译与生成，不能将此轮标记通过。
- 资产数量、保存重开及依赖门禁本轮未通过，后续修复后另追加结果；旧动画/配置未改。

## [VERIFY][综合][TR-487] 2026-10-07 第二次正式资产生成失败与挂点返工

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4445—4451 行；历史时点记录，不代表当前仍在执行。

- 完整 Location 修复后执行总生成器，证据 `_temp/ycombat-integration/generate-assets-retry.log`，Unity 返回 1，无此次新增 CS 编译错误。
- 失败原因：旧 RoleAtkConfig 的 JX `root/Hips/.../hand_R` 路径在 MJS 预览模型中不存在。校验阻止无挂点配置发布，已打回编辑开发核对真实模型骨骼及具名挂点映射，不可静默跳过或猜选首个骨骼。
- 已创建 3001001、3004001 两份编辑配置与绑定，旧资产未改，后续生成保留并续接。正式运行资产/规则 JSON 尚未完整生成，本轮数据门禁未通过。
- 运行开发子 Agent 已准备独立专用 Editor 合同测试（17 项），待数据生成后执行；没有生成游戏角色或键盘探针。

## [VERIFY][综合][TR-488] 2026-10-07 真实骨骼映射与第三轮导出白名单返工

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4452—4458 行；历史时点记录，不代表当前仍在执行。

- 专用只读诊断 `SkillAuthoringAssetDiagnostics.DumpModelSockets` 返回 0，无新增 CS/Exception；证据 `_temp/ycombat-integration/model-sockets.log`。只加载预制体资产，不实例化游戏角色、不改场景。
- 确认 JX Avatar RightHand=hand_R，MJS Avatar RightHand=hand_r，实际唯一 MJS 右手路径 `root/pelvis/spine_01/spine_02/spine_03/spine_04/spine_05/clavicle_r/upperarm_r/lowerarm_r/hand_r`。共享 SkillFxSocketResolver 仅对已确认的旧右手路径执行 Avatar 语义映射，未知/歧义仍拒绝；运行冷回退使用同一解析器，旧配置保留。
- 修复后总生成器已创建八份编辑配置与绑定；导出再次拒绝 Unity 引擎元字段 m_CorrespondingSourceObject（并非包运行字段），返回 1，证据 `_temp/ycombat-integration/generate-assets-socket-fixed.log`。已打回开发明确区分引擎原生序列化元字段和包字段，禁止 m_* 通配绕过业务白名单。
- 本轮正式运行资产/规则文件门禁仍未完成；所有失败日志保留，后续通过结果另追加。

## [VERIFY][构建][TR-489] 2026-10-07 第四轮编译失败与专用构建验证器修复

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4459—4463 行；历史时点记录，不代表当前仍在执行。

- 数据白名单已按具体引擎元字段修复；第四轮尚未进入生成方法，因新增专用资源构建验证器缺 `using YooAsset` 导致 CS0103（YooAssetSettingsData），返回1。证据 `_temp/ycombat-integration/generate-assets-final.log`。
- 编辑开发已补实际 API 所属命名空间；该修复只作用于专用 Editor 测试工具，未改第三方 API、游戏表现或旧资源。第五轮重新正式编译/生成，日志 `_temp/ycombat-integration/generate-assets-validated.log`。

## [VERIFY][综合][TR-490] 2026-10-07 第五轮专用验证器 API 可见性失败

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4464—4469 行；历史时点记录，不代表当前仍在执行。

- `using YooAsset` 补齐后实编发现 YooAssetSettingsData 在1.5.7中是 internal，跨程序集直接调用导致 CS0122，生成方法未执行，Unity 返回1。证据 `_temp/ycombat-integration/generate-assets-validated.log`。
- 已打回编辑开发核对实际 API 可见性，改用明确的序列化/反射边界或经核实的文件命名实现，禁止修改第三方库可见性。须先隔离编译专用验证器，再重跑正式生成。
- 此失败属于新专用资源验证脚本，未产生技能资源、旧表或场景改动；验收仍未通过。

## [VERIFY][综合][TR-491] 2026-10-07 第六轮持久化比较返工与真实 Editor 复验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4470—4477 行；历史时点记录，不代表当前仍在执行。

- 第六轮已创建全部编辑/运行资产、规则 JSON 和收集组，但 ValidateAll 拒绝 3001001 的持久化时长，证据 `_temp/ycombat-integration/generate-assets-ready.log`。开发子 Agent 将预期时长显式转换为 float；秒、米、角度及向量各轴使用明确 1e-6 绝对容限，并先拒绝 NaN/Infinity。资源身份、ID、帧、枚举与旧事件标志仍精确比较。独立源码复审通过，隔离 Editor 编译 0 错误、1 警告。
- 用户随后打开正式 GUI Editor（PID 2156），后续全部使用此唯一实例，未关闭用户窗口或另开 batch。工具客户端保留旧握手失败，改用已恢复的官方 HTTP MCP JSON-RPC；先读取 editor/state/custom-tools/menu-items 与工具实际 schema，再执行测试。实例非 Play、空闲，真实源码编译及 ReloadAssembly 后 Console 错误为零。
- 当前 Editor 的 ValidateAll 已通过；再执行 GenerateAndExport，先检查技能资产无未保存编辑，保存后针对 Skill SO 强制同步重导入，最终 ValidateAll 通过。证据 `_temp/ycombat-integration/mcp-generate-reload-verification.txt`；78 个生成资产/元数据/规则/收集器文件重复生成 SHA256 零变化，证据 `generated-assets-idempotence-result.json`。
- 专用 Editor 合同测试真实执行通过 17 项，Editor.log 与 Console 均记录 `YCOMBAT_CONTRACT_PASS assertions=17`；证据 `_temp/ycombat-integration/mcp-contract-tests.txt`、`mcp-final-contract-console.txt`。测试仅修改内存副本，未生成游戏角色或写入正式场景。
- 原动画、RoleAtkConfig 与相关元数据基线 44 个文件最终 SHA256 全部一致，证据 `_temp/ycombat-integration/preserved-assets-final-result.json`。该基线不包含全部旧技能表，不能称为全表哈希验证。新增 51 个客户端文件均有 meta；新增收集组空值的 Unity YAML 行尾空格已由开发子 Agent 限定修复，差异检查通过。

## [VERIFY][协作][构建][TR-492] 2026-10-07 服务端完整隔离构建与规则回归

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4478—4484 行；历史时点记录，不代表当前仍在执行。

- 首次设置 BuildProjectReferences=false 后缺引用程序集，CS0006，证据 `_temp/ycombat-integration/server-final-build.log`。之后主 Agent 未成功创建隔离 props 却运行完整 Build，0 错误、11 警告，但输出误落 LYF_Server 三项目 bin/Debug；证据 `server-final-references-build.log`。该轮不能作为隔离构建通过，标准目录产物保留，不清理用户文件。
- 已单独创建并核实 `_temp/ycombat-integration/server-final/isolated-output.props`，使用 CustomBeforeMicrosoftCommonTargets 对每个项目设置输出与中间目录，再完整 Rebuild。0 错误、11 条既有警告，MMoRpgCommon、Network、GameServer 均输出根 `_temp`；证据 `_temp/ycombat-integration/server-final-isolated-build.log`。正式两端 JSON 和构建输出 Config/SkillCastingRules.json 的 SHA256 一致：`AE614206BE0800DF77F82E91EEFF8BDA0C0FABA14B079744BF0006994C82DC2C`。未修改协议或覆盖客户端 Network.dll。
- 最新正式程序集复跑规则测试 26 项通过，证据 `_temp/ycombat-casting-rules/authority-final-result-correct-fixture.log`。首次误复制默认禁止规则导致测试“E允许打断Q”失败，保留 `authority-final-result.log`；改用原专用测试 Config 后通过，正式规则始终为五行空名单/MoveCasting=false。无网关会话的两条发送失败为独立进程夹具边界。
- 覆盖新技能名单方向、重复 UUID、成功互断、未学/MP/CD 拒绝不取消旧施法。服务端无水平移动权威链，MoveCasting 不代表两端坐标结算已验收；Game View、真实按键与四服联调仍未执行。

## [VERIFY][界面][TR-493] 2026-10-07 YCombatEditor 实包与编辑窗口最终验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4485—4494 行；历史时点记录，不代表当前仍在执行。

- 正式 Editor 后台没有执行最初排入的 delayCall，结果文件未出现。只读委托诊断确认唯一待执行委托来自本次临时 MCP 编译程序集；精准移除该委托后，直接调用现有专用 `SkillPresentationBuildVerification.BuildAndVerify`，未新建 runner，也未执行其他任务的回调。
- 同步 MCP 工具调用约 30 秒后返回 success=false/message=null，但 Unity 构建继续执行。因此该传输响应不作为构建结果；独立完成文件 `_temp/ycombat-integration/bundle-live-verification-result.txt` 为 PASS，真实 Editor.log 最终记录 `YCOMBAT_BUNDLE_VERIFY_SUCCESS actual-bundles=46 runtime-assets=7`。精简日志 `_temp/ycombat-integration/final-unity-verification-markers.log`。
- 使用正式 YooAsset 收集设置和构建映射生成完整模拟清单（1573 资产、760 包），核对七份运行配置的完整 Location，Authoring 条目为零；真实构建技能相关完整依赖闭包的 46 个原生 Bundle（26,238,573 字节），从文件加载全部包并实际读取七份强类型 SO，粒子引用无空值。10070 另从实包读取原剪辑、蓄力与落剑各一条原事件及资源。全部加载包在 finally 中 Unload(true) 后才打印成功。
- 实包与清单路径 `_temp/ycombat-integration/bundle-verification-20261007111447/`。此门禁采用 YooAsset SimulateBuild 全清单和原生 Bundle 子集加载，证明收集映射与实际依赖资源有效；不代表完整 YooAsset 发布、Player 构建或正式 YooAsset 句柄冷加载已验收。
- 已调用正式 `SkillEditorAssetTools.Open` 打开 `JX_Attack01_Reference`，核对窗口配置为该资产、模型为 Role_JX、真实预览 Animator 存在，正式场景脏状态没有变化。证据 `_temp/ycombat-integration/mcp-editor-open-verification-retry.txt`。第一次临时验收代码误将私有 previewAnim 当公开字段，内存编译失败，未执行窗口操作；改用注明原因的反射边界后通过。没有修改业务源码。
- 打开窗口后 Console 错误仍为零，证据 `_temp/ycombat-integration/mcp-console-final.txt`。预览模型由原编辑器生成隐藏且不保存的副本，不是游戏测试角色；没有进入 Play、模拟按键、添加正式场景测试组件或保存正式场景。窗口载入验证不等于实际 GPU 预览画面或技能按键通过。
- 子 Agent 最终交叉独立审查通过：编辑开发者审运行表现与资源生命周期；运行开发者审编辑/导出、规则和最终实包证据。静态规范、元数据与差异检查通过。实现与配置门禁完成，实际 Game View、目标头顶、技能移动/跳跃手感、四服联调及运行句柄冷加载交由后续实机验收。

## [VERIFY][技能][界面][TR-494] 2026-10-07 技能编辑器中英文界面验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4495—4506 行；历史时点记录，不代表当前仍在执行。

- 范围：14个 SkillEditorWindow partial、两个宿主 Inspector、SkillEditorAssetTools.Open 标题，以及三个独立语言/显示帮助类和 meta。原 Runtime、技能配置与规则JSON、场景、协议不在本轮代码修改范围。仅本项目固定嵌入包改动，原 YCombatEditor 仓库不修改。
- 独立审查闭环：完整 Tooltip 和 Header 漏译、priorityRule.Any 上下文、Flags原事务回调、持久资产场景引用限制、只读 Range/Min 控件差异均打回对应开发者修复。最终独立静态审查 PASS，无剩余代码阻断，报告 `_temp/ycombat-language/independent-final-review.md`。独立元数据检查覆盖236处 Header/Tooltip、227个唯一原文，漏项为零；框架验证文案未映射候选为零，证据 `review-metadata-coverage.json`。
- 编译：冻结后 `compile/editor-build.log` 与 `host-compile/host-build.log` 两项隔离编译均0错误、各1条既有Unity引用版本冲突警告。早期核心编译0错误/3警告不代替最终冻结编译。正式 Unity 第一次 scripts 刷新后发现最新只读修复晚于程序集时间，随后执行 all 强制刷新并重编；`final-unity-state-frozen.txt` 确认编译/域重载完成、资源扫描结束，Console切换前后均0错误，见 `final-unity-console-before-switch.txt`、`final-unity-console-after-switch.txt`。
- 验收脚本失败留痕：第一次 MCP 内存编译引用 Global 时撞到两个程序集的同名类，未执行窗口操作，记录 `live-language-verification-first-failure.txt`。改用 SkillConfigSO 所在程序集反射定位轨道枚举；随后发现重编后的现有窗口配置为空，记录 `live-language-verification-empty-window.txt`，没有覆盖用户已载入配置。通过原 `SkillEditorAssetTools.Open` 复用空窗口恢复先前 JX 参考，正式场景仍未标脏，记录 `restore-editor-reference.txt`。这两次未计为通过。
- 实时双向切换：已有 JX 窗口执行中文→English→中文，标题实际为“ACT 技能编辑器”/“ACT Action Editor”，轨道为“动画”/“Animation”，属性标签和玩家/敌人枚举同步变化，稳定内部轨道键始终 Animation。两方向 EditorPrefs 重读、无偏好默认中文、非法历史值默认中文均通过；finally 恢复原个人偏好。证据 `live-language-verification-result.txt`，原停止状态结果另存 `live-language-verification-idle-pass.txt`。
- 数据与进度隔离：切换前后配置对象身份、完整 EditorJsonUtility 序列化、原资源脏标记、预览对象身份、选择、缩放、Undo组和正式场景状态保持一致。为发现归零/停止回归，仅在同一次主线程内临时设置第17帧、0.25秒计时与播放标志；三次切换均未改变，finally恢复原值，未调用动画采样、驱动特效或进入 Play。此项证明语言回调保持视图状态，不是实际连续GPU播放验收。
- 最终状态：`final-window-state.txt` 记录中文、JX参考、第0帧、停止、计时0，assetDirty=false、sceneDirty=false。无生成游戏角色、自动按键、正式场景测试组件或技能配置保存。
- 保护与规范：最终 `review-change-summary.json` 记录17既有源码变化、3新源码及同名meta；187保护文件SHA256零变化。独立66文件保护也零变化。新增/修改范围规范扫描无问题，强类型/反射边界、中文注释、稳定序列化键、数值约束由独立审查核对。目标 git diff --check 退出0；因包本身未跟踪，再独立比较17源码前置快照与三个新源码，新增行尾空白为零，证据 `final-whitespace-check.json`、`final-incremental-whitespace-check.json`。所有中间产物保留根 `_temp/ycombat-language`。
- 验证边界：未取得真实编辑器截图，没有鼠标逐个展开所有菜单，也未对最小900px窗口的字体/截断作像素视觉验收。本轮功能结论依据正式 Editor API、源码显示调用、元数据覆盖、编译及保护验证。Game View/物理按键/四服与Bundle构建不属于本次显示改造门禁，未重复前轮测试。说明与完整改动记录分别见 `项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md`、`项目修改日志/2026-10-07-YCOMBAT-EDITOR-LANGUAGE-CHANGELOG.md`。

## [VERIFY][技能][TR-495] 2026-10-08 技能源码与配置目录分离

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4507—4517 行；历史时点记录，不代表当前仍在执行。

- 范围：Data/Skill顶层9运行/配置类型源码移至SceneEntity/Skill，Editor直接6源码与原宿主Editor asmdef移至Assets/GM_Game/Editor/Skill，原Tests完整目录移至UI/test/Editor/SkillVerification。源码、asmdef与原meta按原身份搬迁；4份工程仅更新准确9/7/6/3处路径，服务端3个共享源码Link仍指Config。
- 保护：迁移前99文件基线，45项按独立映射迁移、54项保留原位置，SHA256全一致；Data/Skill下.cs/.asmdef均0，24.asset/1.json/29.meta不变。独立24配置m_Script均唯一解析到原类型及原程序集；7外围保护文件不变。原20源码按最近asmdef归入9运行、6宿主Editor、5验证Editor，验证源不误归合同测试或GM_Game。报告 `_temp/skill-code-relocation-20261008/independent-relocation-review.json`、`independent-static-verification.json`。
- 独立审查：PASS，无需返工；源码内容没有变化，无新增行为测试。开发项目路径脚本初次预检误用新旧前缀的全局逆转而安全停止，任何csproj尚未写入；随后改为精确替换区间验证，通过后才写四份工程。保留原开发脚本/路径记录，不计此前预检为成功。
- 编译：冻结源码独立Rebuild GM_Game为0错误/37警告，LYF.SkillAuthoring.Editor为0错误/2警告，LYF.SkillAuthoring.Verification.Editor为0错误/2警告。使用冻结正式csproj派生临时工程、直接编译实际源，依次引用本轮前置输出。警告含既有代码/Unity引用冲突和隔离AdditionalFile适配USG0001。GameServer连MMoRpgCommon/Network完整Rebuild为0错误/11警告，通过CustomBeforeMicrosoftCommonTargets将每个项目bin/obj定向本轮_temp。没有覆盖正式Network.dll或服务端输出。摘要 `independent-build-summary.json`，四份日志分别client-main-build.log、host-editor-build.log、verification-editor-build.log、server-build.log。
- 部署数据：正式客户端Resources JSON、游戏服Config JSON及隔离GameServer/bin/Config部署JSON的SHA256均为 `AE614206BE0800DF77F82E91EEFF8BDA0C0FABA14B079744BF0006994C82DC2C`。构建后静态保护再次通过。
- 实时边界：当前正式GUI Unity已运行，另2个Unity进程为导入worker；未启动第二实例/关闭用户窗口。MCP8081拒绝连接，8080初始化返回404；未安装工具或重配Unity。Editor.log从本轮基线字节2,311,416起暂无此次迁移导入/编译/域重载，不能声称正式Unity实编或Console清零。新增Editor及Editor/Skill文件夹meta由当前Unity下次刷新生成，未猜造GUID。原Tests文件夹meta随目录保留为SkillVerification.meta。
- 差异：4份工程git diff --check通过；独立精确字节区间验证表明只变预期路径，源码不改。未改场景、旧表、资源、配置和业务行为；未创建角色/按键探针或重复Bundle构建。所有临时结果位于 `_temp/skill-code-relocation-20261008`，未经确认不清理。
- 结论：目录整理、资产身份保护、独立审查和对应隔离编译门禁完成；正式Unity导入待当前实例刷新。开发/审查/测试报告分别migration-content-hashes.json、independent-final-review.md、independent-test-report.md。正式归档 `项目修改日志/2026-10-08-SKILL-CODE-DIRECTORY-CHANGELOG.md`，用法 `项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md`。

## [VERIFY][技能][TR-496] 2026-10-08 连续 Q 普攻动画切换修复验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4518—4526 行；历史时点记录，不代表当前仍在执行。

- 需求与范围：连续 Q 普攻时人物短暂下沉复原。本轮业务仅修改 MAnimator.cs 的 CrossFade 两重载；独立开发 q_ground_develop、审查 q_ground_review、测试 q_ground_test，主 Agent 维护记录。
- 定位证据：RO 的 CrossFade 在当前状态等于目标或处于过渡时直接 Play，其余固定秒数融合。本项目此前无条件 Animator.CrossFade，Q 三段复用 Skill 槽时产生自过渡且秒单位错误；本轮恢复 RO 的分支。原 RootT.y 曲线是 Humanoid 身体曲线，不等同于实体/脚骨地下证据；旧 RoleCtrlBase 对 Active 场景实体的更新与根运动早退，静态未发现 Q 独立修改实体 Y 的路径。
- 独立门禁：开发与测试两次最终 GM_Game 隔离 Rebuild 均 0 错误；独立审查 PASS，无返工项。真实 MAnimator.cs 与真实兼容适配源码链接到薄 Animator 测试边界，40 项调用断言 PASS：同 Skill 重播、过渡中重播、首次进入/退出固定 0.1 秒、归一化起点按实际已安装剪辑长度换秒、旧 ready/占位、异步恢复及兼容路径。该回归只证明业务播放分支与时间语义，未模拟 Unity Humanoid 的姿态或物理。
- 保护与规范：仅 CrossFade 增量，中文注释完整，无新增 namespace/foreach/三目/弱类型业务载荷；差异空白检查通过。Atk02 哈希与本次基线相同，保留用户既有 Y Bake 修改；Q 三段、700ms窗、原表、其他动画、正式场景及物理未修改。
- 产物：全部基线、开发/独立编译及边界验证位于 `_temp/q-combo-ground-20261008`。开发交付 develop-result.md，审查 independent-code-review.md，独立报告 test/independent-test-report.md，断言输出 test/animator-boundary-result.log。独立编译共 37 个诊断（35 C#、1 MSB3277、1 隔离 USG0001），无错误。正式归档 `项目修改日志/2026-10-08-Q-COMBO-GROUND-CHANGELOG.md`。
- 运行边界：当前 MCP 不可用，没有自动生成角色、模拟按键、挂正式测试组件或另开同工程 Editor。正式 Library/ScriptAssemblies/GM_Game.dll 时间 10:05:22 早于修复 MAnimator.cs 的 10:30:22，当前 Editor 尚无本次刷新编译证据；隔离编译不能当作 Unity 实编。真实连续 Q 地下画面、实体/模型/脚骨世界高度均待当前 Unity 正常编译后用现有角色手动复验；若仍下沉继续交开发定位，不能宣称已取得下沉完全消失的画面验收。

## [VERIFY][技能][TR-497-FILES] 查询任务的正式文档产物：2026-10-08 当前技能动画与特效播放链只读核对

- 该任务记录的正式文档路径：`项目修改日志/2026-10-08-Q-COMBO-GROUND-CHANGELOG.md`。
- 查询正文、当时状态及原文验证边界见 `Docs/AI/QUERY_TASKS.md` 的 `[TR-497]`；本条只保留文件产物追溯，不新增完成或检查结论。

## [VERIFY][技能][TR-499] 2026-10-08 删除旧动画事件技能特效链验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4542—4554 行；历史时点记录，不代表当前仍在执行。

- 需求：用户明确要求删除旧的clip.events消费链并由子Agent检查。运行开发legacy_fx_develop、Editor/配置开发legacy_fx_assets_develop、独立审查测试legacy_fx_review_test。
- 修改：5运行源码删除AnimationEvent消费、TryPlayLegacyEffect、ClipInfo特效租约、10070旧路径和PreserveLegacyFxEvents；宿主导出/绑定、Inspector、中英标签、BuildVerification及合同测试删除旧例外；8绑定与7运行配置清理旧标志，10070运行资产发布两FX。原动画/表/RoleAtkConfig/预制体、八作者轨道、UUID/池/旧实例列表/Q700ms窗和MAnimator修复保留。
- 行为：统一FxEvents时间轴。10070按既有30FPS作者轨道第4/9帧播放蓄力/落剑（0.13333334/0.3秒），保留EffectPos与TargetTop无目标前10单位。Fire快照本段表现ID和原表时长，冷配置就绪后按施法时钟补发；Q换段不串轨道。无效整轨只报一次，不读取旧事件。Editor首次导入旧动画作制作参考仍保留。
- 编译：开发GM_Game隔离编译0错误；独立GM_Game、LYF.SkillAuthoring.Editor、LYF.SkillAuthoring.Verification.Editor、LYF.SkillAuthoring.ContractTests.Editor最终源码隔离Rebuild均0错误，警告37/2/2/2。所有bin/obj均本轮_temp，未覆盖正式程序集。
- 回归：链接真实技能/核心/缓存/生命周期源码薄Unity/YooAsset调用边界，27断言通过；冷加载补发一次、真实Core换段ID锁定/700ms窗、模型晚到、有/无目标落剑、回池旧加载隔离、同ID缓存重建、无效轨道一次报错/Reset恢复、尾粒租约释放均通过。没有镜像重写特效业务实现。
- 审查：无本轮增量阻断项；运行旧标识残留0，10070两阶段逐字段与作者轨道一致，其他绑定/运行资产仅删标志。1578保护文件SHA256保持，八作者轨道及29meta字节保持。
- 既有配置差异：首次七轨道全量比较发现3001003编辑稿仅F23，运行发布仍F23+F54；开发前快照确认本轮之前已存在。保留first-static-failure.json后改以本轮增量验收，不覆盖用户稿件或删已发布F54。本轮不能声称七条编辑轨道均已同步发布；该旧差异记录交接，主动重发布3001003时使用编辑稿。
- 失败保留：开发静态秒容限1e-8过严，改为生产一致1e-6复验通过；独立隔离工程生成encoding='utf8'引发MSB4025，改为utf-8后编译通过。失败log保留，正式业务与数据未为夹具失败更改。
- 边界：未执行正式Unity域重载、Editor合同、实际Bundle/GPU/Game View或YooAsset运行冷句柄；薄边界不等于真实画面。待当前Editor正常编译后手动现有角色验证特效；不创建角色/自动按键/第二Editor，不清理_temp。
- 证据：_temp/legacy-fx-removal-20261008，运行runtime-development-result.md，Editor editor-assets/develop-result.md，独立test/independent-review.md、test/independent-test-report.md、test/independent-build-summary.json、test/independent-static-verification.json、test/runtime-boundary-result.log及verified-source-hashes.json。独立最终审查PASS，无返工项。正式归档项目修改日志/2026-10-08-LEGACY-SKILL-FX-REMOVAL-CHANGELOG.md，使用说明同步项目分析/YCOMBAT_EDITOR_INTEGRATION_GUIDE.md和USAGE.md。

## [VERIFY][技能][TR-500] 2026-10-08 Q普攻三段特效配置验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4555—4565 行；历史时点记录，不代表当前仍在执行。

- 需求：第一段补特效，第三段删多余特效。配置开发q_fx_config_develop，独立审查验证q_fx_config_review。
- 修改仅3asset：第一段作者SkillConfigSO_3001001与Runtime_3001001复制现JX_Attack01_Reference唯一F48@120FPS刀光，保持粒子/EffectPos/偏移/旋转/缩放；第三段Runtime_3001003只删除F54尾事件，F23事件原字节保留，与作者稿同步。第二段及全部源码/动画/表/资源不改。
- 验证：独立62项静态断言PASS。三段作者/运行均1/1/1，触发秒数.4/.19166666/.19166666，均≤700ms窗与完整剪辑。作者字段逐项映射到生产ReadFx字段一致、同一粒子GUID唯一解析、fileID属于真实ParticleSystem组件、EffectPos完整路径存在。54配置文件基线仅3变化，其他51项/meta/JX参考/第二段/第三作者/绑定保持。
- 证据：_temp/q-basic-fx-20261008/develop/baseline.json、change-manifest.json、develop-result.md，review/independent-static-verification.json。目标差异空白检查通过，纯数据改动不重复编译或造运行测试脚本。
- 边界：没有此次Unity导入/Bundle更新/Game View按键画面，静态通过不代表JX参数在MJS模型上视觉对齐已实测。当前Editor正常刷新后由现有角色手测；没有自动按键/角色生成/第二Editor，不清理_temp。
- 日志：项目修改日志/2026-10-08-Q-BASIC-FX-CONFIG-CHANGELOG.md。三段单刀光现状已同步项目分析/YCOMBAT_EDITOR_INTEGRATION_GUIDE.md与USAGE.md。此前3001003未发布差异本次按用户要求解决，第一段不伪造原MJS剪辑事件来源。

- 独立终审补充：_temp/q-basic-fx-20261008/review/independent-review.md PASS，无返工项；35项原模型/粒子/表/动画与冻结源码保护哈希保持。临时解析夹具三次失败留review/fixture-failures.md，最终仅修正_temp解析器后静态62断言通过，业务数据未为测试失败更改。

## [VERIFY][技能][TR-501] 2026-10-08 第三普攻双阶段纠正验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4566—4574 行；历史时点记录，不代表当前仍在执行。

- 需求：用户再次陈述“第三下普攻有两段特效”。主发澄清且只读核查后按“第三应有两段”合理默认纠正上一轮误删，并公开说明；当前保留用户后续更正意图可能。
- 开发q_fx_config_develop、独立审查q_third_fx_inspect。仅第三作者SkillConfigSO_3001003与Runtime_3001003两asset恢复F54第二Effect_Skill3原事件，F23第一刀光及首两段保持；作者/runtime2/2，当前三段1/1/2。第二参数从本项之前保存的删前原配置逐字恢复，不猜测新的空间定位。
- 独立188静态断言PASS，无返工。54配置基线仅2目标变，其他52含全部meta/首两段保护；118项本轮非目标保护+35项前轮冻结保护哈希保持。运行资产与原双阶段快照完整字节一致；作者仅追加第二项，全部运行字段映射、真实粒子GUID/fileID、帧时点F23/.19166666、F54/.45与700ms窗核对通过。差异空白检查0。
- 只读澄清证据：原Atk03动画存在.19360256/.4461279两PlayEffect事件；正式Role_MJS的RoleAnimaBehaviour失活且PlayEffect有isActiveAndEnabled守卫，因此不能因原资源有两条直接断言新链重复；当前运行旧消费已删除。此次为双阶段配置纠正，未据静态结果宣称旧组件重复故障。
- 边界：纯配置无业务修改，不重复编译/Bundle/角色生成/自动按键；未取得正式Unity导入或Game View画面。原第二世界绝对定位仍保持原参数，未实测位置对齐；当前Editor正常刷新后手测，不清理_temp。
- 证据：_temp/q-third-fx-20261008/develop/develop-result.md、review/independent-review.md、review/independent-static-verification.json。根日志项目修改日志/2026-10-08-Q-THIRD-FX-CORRECTION-CHANGELOG.md；当前GUIDE/USAGE同步第三两阶段。

## [VERIFY][技能][TR-502] 2026-10-08 技能编辑器预览位移修复验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4575—4587 行；历史时点记录，不代表当前仍在执行。

- 需求：动画编辑时预览人物跑远。开发 preview_motion_develop；独立审查/测试 preview_motion_review；主 Agent 验收当前窗口并维护记录。
- 根因：真实绑定 Role_MJS 根预制体保存 (67.43,19.414,80.668976) 米，旧克隆继承该历史位置；原 Y 同路径也有缺口。显式采样不断重建图且无根基准另有漂移风险，本项未以原生根运动数值证明风险是唯一原因。
- 唯一业务修改：包 Editor/Window/SkillEditorWindow.Preview.cs。预制体副本放原点，保留朝向/缩放，保存实际采样的角色/Animator根局部变换，采样前后/finally复位；骨骼和EffectPos姿态仍由动画采样。场景源沿原实例化姿态，旧模式保持，正式ActionClipAnimator/角色根运动/动画配置不改。
- 编译：开发完整Editor隔离0错误/3诊断；独立正式csproj派生全源码Rebuild0错误，保留既有及隔离引用/分析器/AdditionalFile警告。开发74与独立216真实方法薄调用边界断言通过，覆盖重复同帧、倒拖、回零循环、嵌套Animator、骨骼保留、异常finally、销毁/重建及源对象保护。边界不模拟原生Humanoid/Playable。
- 代码审查：最终PASS。曾修创建/采样Animator选择不一致，并纠正场景源世界对齐注释；冻结SHA256 e57a70799716c018128cb97587aa88678416cdcc033e183d831ccb44a70a5175。包中文手册、CHANGELOG及项目USAGE补审通过，F聚焦文字已按审查更正为事件位置。
- 正式Unity：computer-use技能在现有实例执行Assets/Refresh，最终Editor DLL本机20:26:05晚于最终源码；Editor-final-refresh.log记录Tundra成功、Mono重载和资产刷新完成。首次域重载3条内置TransformInspector/GameObjectInspector异常保留，无本包栈；最终刷新没有再现。未新开Editor。
- 实际UI验收：最终3001003/Role_MJS显式120FPS，从第293帧点回0，姿态更新且在同一区域。现有播放按钮循环预览约56秒（2026-10-08T12:29:48.031Z至12:30:43.677Z），人物仍留固定Scene视角内同一区域，随后停止回0。截图 final-frame0、final-loop、final-stopped-frame0及最终窗口保存在ui目录；截图Console0错误/3警告、窗口问题0。仅此动画观察，未测全七段/原生Transform数值，也未测PlayMode、GameView按键、Bundle/服务器。
- 保护：独立521前置文件仅源码变化、520项保持；主有意改两包文档后终检521项仅源码与两文档变化，518保持，Q作者/Runtime1/1/2保持。项目USAGE和协作/修改日志是正式记录增量。没有保存或导出用户未保存资产，没有角色生成/自动游戏按键或正式测试组件。
- 证据：_temp/skill-editor-preview-motion-20261008/develop/develop-result.md；review/independent-review.md、independent-test-report.md；ui/final-protection.json、loop-observation.json、Editor-final-refresh.log、compiled-source-times.txt及PNG。所有_temp产物未经确认不清理。
- 未决：旧时间模式及场景源有父级世界矩阵兼容不在本项范围，Motion预览仍只绘参考、不驱动根位移。正常编辑第三段可继续；其他动画若出现具体漂移再按同流程定位。正式归档：项目修改日志/2026-10-08-SKILL-EDITOR-PREVIEW-MOTION-CHANGELOG.md；用法：项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md。

## [VERIFY][综合][TR-503] 2026-10-08 普通攻击3-2刀光及局部参数验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4588—4597 行；历史时点记录，不代表当前仍在执行。

- 用户明确3-2仍用普通攻击特效，局部位置(.92,.36,.07)米、局部欧拉角(298.69,254.45,98.72)度。配置开发q32_fx_develop，独立q32_fx_review。
- 仅第三作者SkillConfigSO_3001003及Runtime_3001003第二事件，粒子替换为第一刀光同一真实Effect_Attack01，GUID31a002e239edec74f82b993716735ce8/fileID2613513961126100727。EffectPos NamedSocket=1，作者followCharacter=1/useWorldSpace=0，生产SetParent(false)后赋局部位置/旋转，完整映射一致。F54/.45s、第一事件原字节、原尾部、缩放与前三段数量1/1/2保持。
- 独立PASS，90静态断言通过。4168本轮前置文件仅2目标变，4166保持，无增删；meta、原表、动画、粒子预制体、角色、技能业务/编辑源码与上一轮预览修复保持。开发391前置其余389保护保持。真实粒子GUID唯一解析、组件fileID、EffectPos存在、作者/Runtime时间/变换/定位逐字段与增量范围核验通过。
- 当前GUIDE/USAGE说明已更新为第三两次普攻Effect_Attack01及用户局部参数，独立复核准确。原技能3特效资源保留，历史恢复说明不改写。
- 失败留痕：开发第一次临时换行识别误判在写入前退出；独立第一次增量空白夹具误判既有行，first-static-failure.json保留。仅修_temp夹具，正式数据未为测试调整，最终复验通过。
- 边界：纯配置没有重复编译/新业务逻辑/角色生成/游戏自动按键，未操作用户未保存编辑窗口，不全量保存/生成；没有此次Unity导入/Bundle更新或画面实测，静态通过不等于视觉对齐通过。
- 证据_temp/q-third-second-basic-fx-20261008/develop/develop-result.md、review/independent-review.md、independent-static-verification.json及基线；归档项目修改日志/2026-10-08-Q-THIRD-SECOND-BASIC-FX-CHANGELOG.md，使用项目分析/YCOMBAT_EDITOR_INTEGRATION_GUIDE.md和USAGE.md。验收结束，不清理_temp。

## [VERIFY][技能][TR-504] 技能编辑器代码集中与中文注释验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4598—4610 行；历史时点记录，不代表当前仍在执行。

- 需求：源码集中便于阅读、补具体中文注释；用户明确保留位移、碰撞、飞行物、音效、镜头、流程等全部现有制作功能。开发editor_simplify_develop、支持editor_support_comments、独立审查测试editor_simplify_review，主维护记录。
- 源码：Editor整树迁入LYF_MMO/Assets/GM_Game/Editor/Skill/ActionEditor，65内部文件加原目录meta共66项，原目录已不存在。保持Ethan.ActionEditor.Editor.asmdef、35meta/GUID及全部菜单/API；Runtime、配置和原参考仓库不动。28Editor源码增改中文注释，2原语言源已有完整说明、字节保持；宿主SkillEditorAssetTools.cs仅补3局部和精修5说明。
- 允许实现差异仅两处：Help.Open与Agent.NewResult通过typeof(SkillConfigSO).Assembly定位仍在嵌入包中的Runtime。独立30源码2153声明中文覆盖0缺口，28源码RawKind/Text全等价，2源码仅指定typeof变化，预处理/namespace/foreach/三目原结构保持。宿主6源255声明核对，目标5594 token等价、其他5源字节保持。
- 编译：四程序集实际新路径全源隔离Rebuild均0错误，Editor4警告、宿主/Verification/Contract各2警告。引用冲突/既有字段/分析器提示保留；没有覆盖正式DLL或手改Unity自动csproj。隔离工程按旧源到新源映射读取本次冻结稿。
- 有意义回归：实际Help.Open与Agent.NewResult源码链接独立Runtime程序集，薄Unity PackageInfo/Application边界22断言PASS；三份真实包内文档存在、URL解析、空包/缺文档原错误行为及包版本上下文均验证。未调用Unity UI或将全部类型塞同程序集假验证。
- 保护：4417前置文件，66搬迁、35meta字节相同，35内容修改均允许、4382内容保持，非计划新增0。原Q作者/Runtime1/1/2、3-2普攻粒子与局部变换、原表/动画/预制体/前轮预览根修复保护；开发191资源保护通过。新增限制策略/Inspector已撤销，不存在重复源码。
- 返工：首注释工具换行/跨度错误独立604语法错误，第二次562错误，均打回且保留原文证据；第二次独立确认SDK Roslyn树在2048边界误重建逗号。正式窗口先恢复自身前置原稿，在_temp按同源跨度映射生成候选，标识符断言、词法/语法/源trivia等价后应用。独立审查以128前置空白避SDK解析边界，另有实际程序集编译。#region伪覆盖、短名语义、XML位置和两条旧说明修正后最终PASS。
- 文档：源码阅读索引、目录整理方案、项目GUIDE/USAGE、包中文手册/CHANGELOG/README/AGENTS/AgentGuide/AgentCapabilities同步；自动化入口相对路径真实存在。修改归档项目修改日志/2026-10-10-SKILL-EDITOR-SIMPLIFY-CHANGELOG.md。
- 边界：computer-use native pipe不可用，已重试及重初始化恢复失败；本轮未取得正式Unity Refresh/域重载/窗口画面证据，正式DLL早于最终源。隔离通过不等于当前Editor采用最终稿或PlayMode/Bundle通过。没有保存用户未保存资产、生成角色/模拟按键、挂正式测试组件或开启第二Unity。后续正常Editor刷新再生工程并手动开窗口核界面；原单次FX粘贴endKeyNumber问题仅交接，不混入整理修改。
- 证据：_temp/skill-editor-simplify-20261010/review/independent-review.md、independent-test-report.md、ast-final.json、host-ast-final.json、independent-protection.json、四build.log、package-boundary-result.log、fixture-failures.md；develop/develop-result.md、migration-manifest.json、final-hashes.json；comments/develop-result.md与host-narrow-develop-result.md。临时文件不清理。

## [VERIFY][技能][TR-505-FILES] 查询任务的正式文档产物：RO技能音效只读核对

- 该任务记录的正式文档路径：`项目修改日志/2026-10-10-RO-SKILL-AUDIO-COMPARE.md`。
- 查询正文、当时状态及原文验证边界见 `Docs/AI/QUERY_TASKS.md` 的 `[TR-505]`；本条只保留文件产物追溯，不新增完成或检查结论。

## [VERIFY][技能][TR-506-FILES] 查询任务的正式文档产物：RO技能轨道类别只读核对

- 该任务记录的正式文档路径：`项目修改日志/2026-10-10-RO-SKILL-TRACK-COMPARE.md`。
- 查询正文、当时状态及原文验证边界见 `Docs/AI/QUERY_TASKS.md` 的 `[TR-506]`；本条只保留文件产物追溯，不新增完成或检查结论。

## [VERIFY][技能][TR-507] 2026-10-10 RO 技能制作轨道补齐验收准备

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4623—4630 行；历史时点记录，不代表当前仍在执行。

- 独立审查测试 Agent：ro_tracks_review_test。数据开发 ro_tracks_data_develop，界面开发 ro_tracks_editor_develop；失败只交开发修改，审查员不代改。
- 当前九类新增 RO 数据：RotateList、ShakeList、CameraFxData、CameraPostEffect、ChangeColorList、SkillBarList、MobUnitList、AIEventList、BuffList。原字段/枚举/秒语义须与实际 proto 核对，不能引用历史摘要中误记的 Args2。
- 保护基线已准备：4823项广文件快照；编译准备五程序集派生工程，所有新增 Runtime 类型需参与真实编译再供 Editor/宿主使用。
- 尚未完成最终编译、独立回归或UI验收，当前不出通过结论。默认零后处理的Unity内联序列化、禁用但含非默认参数的发布拒绝、旧资产兼容、保存复制删除拖动时间窗是验收重点。
- 临时证据：_temp/ro-skill-tracks-20261010/review；方案项目分析/RO_SKILL_TRACK_EXTENSION_DESIGN.md；修改日志项目修改日志/2026-10-10-RO-SKILL-TRACK-EXTENSION-CHANGELOG.md。

## [VERIFY][综合][TR-508] 2026-10-10 RO 九类制作轨道最终验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4631—4643 行；历史时点记录，不代表当前仍在执行。

- 独立审查 ro_tracks_review_test 最终PASS，无遗留代码阻断项；数据/界面开发收到所有问题并返工复验。源码范围详见项目修改日志/2026-10-10-RO-SKILL-TRACK-EXTENSION-CHANGELOG.md。
- 五程序集真实源码隔离Rebuild0错：Runtime59/Editor32/宿主6/Verification5/Contract1源；警告2/4/2/2/2为既有Unity引用/AdditionalFile/未使用字段提示。全部19新增Runtime实际参与编译，依赖使用本轮输出，不写正式Library或Unity工程。
- 最终真实源码薄边界518断言PASS：完整MSkillTrackTiming+真实FrameRate86；窗口30个完整Add/Remove/Move/Count/Copy/Paste方法170；完整标签类262。覆盖九类末端/FPS/循环/无效时间/执行门禁、null列表/单对象/亚帧跨度/当前引用/复制/删除及字段枚举双语。
- RO原schema9类63字段/9枚举原值一致，原16TrackType身份保持，新16至24唯一。新19Runtime198声明+新2Editor208声明共406具体中文覆盖0缺口；新源无namespace/foreach/三目。21新meta GUID唯一。
- 实际失败及返工：首TAIL未定义编译失败保留；关闭后处理制作末端遗漏、全零内联对象、英文截断已修。最后核真实RO镜头源，删除负FOV/三轴振幅拒绝、Frequency修为Time内次数；Index沿MListElemData有符号int删除原创非负门禁。最终增加4个具体Timing断言后复验，不扩无关测试。
- 保护：4823项4811保持，12计划内变化，42新增全在两manifest，54冻结路径0差异，无删除/未计划新增。七技能/Q1/1/2/3-2粒子与局部变换、原表/动画/角色/正式运行/预览根修复保持。主额外重算最终54SHA也0差异。
- 边界：Contract仅编译未执行NUnit；Unity/Mathf/GUI/JSON/Undo为薄测试边界，不等于原生窗口、真实撤销/保存重开。无正式Unity Refresh/域重载、PlayMode、Bundle或新增音效/镜头/召唤游戏运行证据。未新建角色、自动按键、挂正式测试组件、操作未保存资产或开第二Unity。新增内容为制作保存能力，正式执行/Scene预览后续接入，宿主非默认内容拒绝发布。
- 证据：_temp/ro-skill-tracks-20261010/review/independent-review.md、independent-test-report.md、independent-build-summary.json、independent-protection.json、verified-source-hashes.json、三个boundary/source-manifest.json和result.log；主root-manifest-check.json；两开发manifest及develop-result.md。失败日志和_temp按规则保留。
- 下一步交接：正常Editor刷新后从原技能窗口添加九类轨道，原生交互/Undo/保存重开实际核对尚待；出现具体问题再委派开发与独立复审。正式记录、项目设计/使用/阅读索引、包手册/CHANGELOG已同步。

## [VERIFY][技能][TR-509] 2026-10-10 RO真实声音与Animation水平根运动验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4644—4659 行；历史时点记录，不代表当前仍在执行。

- 独立audio_motion_review审查，资产audio_motion_assets、运行audio_motion_runtime开发；问题打回，不由主/测试代改业务。
- 12wav来自RO真实Action.bank样本，经独立解码hash/非空/非静音/24kHz16bit时长核对，正式AudioImporter meta真实GUID唯一。非RO Timeline样例未使用。
- 六程序集隔离Rebuild真实源0错（GM265/Runtime59/集中Editor34/宿主8/Verification5/Contract1），本轮依赖输出逐级引用；既有警告37/2/4/2/2/2。Contract编译不等于NUnit执行。
- 真实16运行源薄边界36断言：Q释放动作槽单次尾音保留、Reset租约清理、当前owner与准备中clip/过渡/窗口/水平Y保护/单次consume、普通/战斗走跑循环、Idle、重复起跳、冷回调等。
- 真实编辑方法薄边界24断言：固定AudioClip强引用、FPS秒映射、单帧粘贴end=key合法、区间/随机拒绝、根含末帧与重叠/绕重力碰撞拒绝、源运行依赖、预览人体尺度/同帧/倒拖/回零/烘焙XZ保护。
- Unity2023隔离工程实际AssetDatabase/生产PrepareAndExport/独立NativeAudioMotionValidation.Run162断言PASS，报告review/native-asset-result.json。实际验证七源/Runtime声音和根字段/依赖、Q1整段保护/Q特效1/1/2和3-2参数、六稿skillID0/真实剪辑、RootPreview前向/重复帧/回零，二次Setup70文件SHA幂等。没有实例化游戏角色。
- 生成配置保护回拷28文件（14原asset+14新asset/meta），写前所有原SHA等于最初输入，所有新路径不存在；不回拷worker临时GM.asmdef/测试代码/Prefab/场景/动画/材质/meta身份替换。最终范围保护报告待追加。
- 真实失败留痕：首隔离环境缺Network.dll/NUnit、后缺SkillFxSocketResolver，补真依赖/临时排除包Tests后成功，未改生产为stub。独立发现Q切槽StopAudio截尾、原Y音效粘贴语义、借用Camera声源及baseVolume均返工修；Native首验期望3007001单音过时，核最终F4蓄力/F9落剑双音+双FX后只修临时夹具，原失败保留。
- 正式Editor曾自动刷新/域重载更新3程序集及12音频meta；未获取最新配置回拷后的正式UI/PlayMode现场证据。desktop native pipe失败按流程恢复仍失败，MCP8081活跃instances空；未用替代UI绕过。
- 隔离Prefab省略五类非相关业务脚本并有部分shader资源缺失，警告保留；原生资产/RootT曲线预览不等于运行Animator.deltaPosition/CharacterController/听觉/完整YooAsset Bundle通过。用户手动游戏角色/按键验收，Agent未生成角色或自动按游戏键。原表、动画、Prefab、场景和临时资料保留。
- 证据_temp/skill-audio-rootmotion-20261010/review，开发manifest和generated-assets-copy-manifest在runtime/assets子目录；正式归档项目修改日志/2026-10-10-SKILL-AUDIO-ROOT-MOTION-CHANGELOG.md。临时文件不清理。

- 独立总审最终PASS，无阻断。920基线895保持/25计划修改，58新增均在manifest，无删除/未计划修改；正式28配置SHA与原生验收产物一致；运行16源、编辑7源方法边界冻结当前零差异，末稿六程序集0错。新源与修改范围中文注释/无namespace/foreach/三目规则审查通过。主22+41+28=91路径哈希再次核0差异。终审报告review/independent-review.md及independent-test-report.md，游戏听感/按键/原生碰撞现场验证边界继续保持。

## [VERIFY][技能][TR-510] 2026-10-10 技能/走跳无声修复验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4660—4668 行；历史时点记录，不代表当前仍在执行。

- 根因实证：New Scene世界Main Camera无AudioListener；UIRoot/UICamera监听世界位置(987.3,999.4,0)，角色位置(62.1493416,19.4139996,80.6689758)，距离1350.105米。音源spatialBlend=1、minDistance1/maxDistance25米、Logarithmic；结论为严重衰减，不误称25米外硬静音。当前EditorSimulate清单含新普通稿/七声音配置，AudioManager音量1、DisableAudio0。
- 独立audio_motion_review终审PASS：GM_Game265真实源隔离Rebuild0错/37既有警告；当前CameraMgr五个完整监听方法薄边界19断言PASS，包含原生单组件Add约束、重复绑定、原状态恢复、临时停启、无UI、无关组件保护、A→B→A同帧往返与最终回收。
- Unity2023隔离API RunReuse真实通过：A/B各1个AudioListener、回A同组件身份、A启用/B禁用；临时相机finally清理，无角色生成/资产保存。此项证明原生组件复用契约，不等于完整正式Play音频输出。
- 实际返工：首帧尾Destroy同帧切回失败，第二pending新增监听方案原生失败（Unity拒绝同GO第二AudioListener），均打回开发。末稿_ownedAudioListeners只跟踪自建组件，换相机暂禁保留、最终销毁统一清理，复验通过；原始失败报告未覆盖。
- 范围保护：2606文件2605字节保持，仅CameraMgr修改，无删除/未计划变化。原表、12声音、作者/Runtime配置、根位移、Q特效1/1/2及3-2参数、场景/Prefab保持。新增/修改范围中文注释通过，无新增namespace/foreach/三目/弱类型。主重核SHA与开发/独立/边界/编译冻结一致cae67152139b344e182375e3bcdcde515712fb635bc1a9381577dbef9ac8ba27。
- 现场边界：当前Play仍旧DLL，新InitCamera需正常编译后下一次运行。未取得最终听感，不强停用户运行、不增每帧补救、不调整系统音量；用户停止Play等待编译重开后手动测试Q与走跳。无代码阻断；现场听感待确认。
- 证据_temp/skill-no-audio-20261010/review/independent-review.md、independent-test-report.md、GM_Game-final-build.log、listener-boundary-final.log、independent-protection.json、verified-source-hashes.json、native-listener-reuse.json/log；首listener-pending-first-failure.log与native-listener-multiplicity.json/log保留。正式归档项目修改日志/2026-10-10-SKILL-NO-AUDIO-FIX-CHANGELOG.md，不清理_temp。

## [VERIFY][构建][TR-511] 2026-10-10 无声修复正式程序集只读补验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4669—4674 行；历史时点记录，不代表当前仍在执行。

- 独立audio_motion_review只读Cecil元数据/IL PASS：正式GM_Game.dll更新22:54:13，598016字节，SHA256 a6cfc44b74afffae3d7680b70fe46bb40da3c9c568043f3d557c31636405949f；CameraMgr六生命周期方法与最终owned列表特征一致，不存在pending候选，InitCamera确有Bind调用。源码SHA仍为cae67152139b344e182375e3bcdcde515712fb635bc1a9381577dbef9ac8ba27。
- 正式日志有Csc/强制重编/域重载，之后主角表现与模拟全学包再次加载，无该段新增声音失败记录；不证明InitCamera条件值、实际运行监听位置或音频输出正常。旧DLL交接结论已按最新证据纠正。
- 未执行正式类型/修改业务/写Library或重复既有回归。本轮desktop Computer Use尝试及恢复均native pipe不可用，现场对象/听感未验；用户最新反馈待回。没有擅自静音/音量操作或创建角色/按游戏键。
- 证据_temp/skill-no-audio-20261010/review/formal-assembly-followup.md、formal-assembly-inspection.json/log、formal-refresh-log-evidence.json。正式归档项目修改日志/2026-10-10-SKILL-NO-AUDIO-FIX-CHANGELOG.md，不清理_temp。

## [VERIFY][技能][TR-512] 2026-10-11 多段技能音效调整（制作与独立验收完成）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4675—4683 行；历史时点记录，不代表当前仍在执行。

- 用户要求：技能声音单调，部分多段动作只听到一段。利用现有多AudioEvents，按动作阶段制作，不新增播放系统。
- 已完成：Setup+Q3/E/R/F四作者/四Runtime共9原文件；新增RO Action.bank原样Swing_Big_b121/Swing_Short_h344/Swing_Big_c512及Unity meta共6新文件。E两声F9/0.3秒Big_b/.65、F36/1.2秒Short_h/.30；R原F63/.525秒Knight/.35并加F136/1.133333秒Big_c/.45；Q3第二F54音量.20；F第二从F84/.7秒移F142/1.183333秒、音量.25，粒子F84保留。
- 理由：E原长声包络晚且第二快段尾弱；R原声音已有多包络，新增后刀提升辨识，不误称第二阶段完全无声。F原第二音约.9805秒结束，早于第二手快段1.2583秒；F142+20ms RMS主窗中心.09秒约1.2733秒，动作附近发声。Q3/F第二声原相对第一约13.3/10.6dB，调量缩小至约3.75/3.02dB（全段RMS估算不是LUFS或听感测量）。
- 流程：原生隔离Unity生产制作两次70文件幂等；候选独立170检查与32迁移原生断言PASS，宿主8源隔离Rebuild0错/2旧警告，才保护回拷14目标。写前8旧SHA全等、6新路径不存在。最终独立广保护2608中2599保持/9计划变化/6计划新增，无删除/未计划修改；15正式哈希和编译冻结一致，主再次核15路径0差异。
- 保留：七行所有非声音字段、FX/root/动画/表/场景/Prefab/CameraMgr、原12波形、Q1/Q2、1技能与走跳全保护。Setup精确旧clip/frame/defaultVolume迁移；自定义音量原项保留；若用户改帧/资源则原项保留且缺新默认追加，说明已同步。
- 源码规范/来源/GUID映射/作者运行秒帧审查通过。四离线试听已有Q3/E/R/F，严格使用发布时点/音量、不EQ/normalize/裁剪；实际三维游戏听感待用户手动测试，不宣称声音主观已通过。不自动角色/按键、不保存正式场景、不写正式测试入口、不清理_temp。
- 交接：正常Unity导入后重新运行测试E/R/F与Q第三段；无需重新制作整套。方案项目分析/SKILL_AUDIO_POLISH_NOTES.md；声音映射项目分析/SKILL_AUDIO_ROOT_MOTION_INTEGRATION.md与用法YCOMBAT_EDITOR_INTEGRATION_USAGE.md已同步。正式归档项目修改日志/2026-10-10-SKILL-AUDIO-POLISH-CHANGELOG.md（沿发起日）。证据_temp/skill-audio-polish-20261010/assets/manifest.json、generated-assets-copy-manifest.json、develop-result.md、stage-preview-manifest.json及review/independent-protection.json/独立报告。

## [VERIFY][技能][TR-513] 2026-10-11 音频中文命名及分类验收完成

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4684—4691 行；历史时点记录，不代表当前仍在执行。

- 已迁移十五wav及原meta到MJX/移动、跳跃、技能（通用挥刀、E、R、1、备用），例如E_第一段重挥刀.wav、E_第二段短挥刀.wav、跳跃_起跳破风.wav。原波形/导入参数/meta/GUID均保持，共享声音不重复复制。新目录8份meta由Unity创建。
- 源码仅SkillEditorAudioMotionSetup.cs音频相对路径与对应说明同步；制作脚本旧默认迁移也引用新备用资源位置，无正式旧音频文件名路径残留。collector原SO依赖链覆盖，未修改。所有作者/Runtime资产保持原字节，不重新导出；事件帧秒/音量/FX/root、表/动画/场景/角色与监听修复保护。
- 开发audio_organize_develop，独立audio_organize_review审查：Unity原生MoveAsset迁移十五身份通过，独立原生67项资源检查包含44个作者/Runtime声音引用；必要宿主8源码隔离Rebuild0错/2旧警告。2614前置文件2583原位保持，30wave/meta按映射迁移，1计划源码修改，8新目录meta；无未计划改动。最终39路径冻结一致，主重算39SHA0差异。
- 文档项目分析/AUDIO_ASSET_USAGE_GUIDE.md包括十五原名/新名、目录及七技能12声音事件和六普通稿10声音事件的帧/FPS秒/音量。项目分析/SKILL_AUDIO_ROOT_MOTION_INTEGRATION.md当前声音表、YCOMBAT_EDITOR_INTEGRATION_GUIDE.md和USAGE.md入口同步，原来源名用于追溯。
- 开发临时入口反斜线转义与独立编译临时工程编码夹具失败只修_temp，原失败日志保留，未为测试改播放逻辑。本轮不重导全配置、不生成角色/按游戏键、不保存用户未保存场景、不调整系统音量或打开第二正式Unity。只资源与必要宿主验证，正式Game听感/发布Bundle未实测。
- 正式日志项目修改日志/2026-10-11-AUDIO-ORGANIZE-CHANGELOG.md；证据_temp/audio-organize-20261011/develop/migration-manifest.json、final-manifest.json、native-move-result.json、develop-result.md和review/independent-protection.json、native-reference-result.json、host-build.log及独立报告。临时资料不清理。无代码/引用阻断，用户可从新目录选声音并按原编辑器发布。

## [VERIFY][技能][TR-514] 2026-10-11 全Audios英文目录与用途名（最终验收）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4692—4700 行；历史时点记录，不代表当前仍在执行。

- 已完成全量十八wav：Characters/MJS/Movement、Jump、Skills（Shared/E/R/SwordDrop/Legacy），以及Shared/Skills/Effects三既有特效音源。所有资源与目录名全ASCII，文件名前缀Movement_、Jump_、Skill_识别类别；例如Movement_Footstep01、Jump_TakeoffWhoosh、Skill_E_Phase02_ShortSlash。旧Skill/MJX及中文路径不再存在。
- 三原特效声音按实际Prefab AudioSource引用命名：DragonAttack到Skill_04_ChargeFx，Clip_Skill5到Skill_04_ReleaseFx，Shield_buff到Skill_02_ChargeFx；Prefab不改，未凭文件名添加龙角色或绑定新技能。
- 开发audio_organize_develop，独立audio_organize_review最终范围验证PASS。十八wave和原meta、十个旧目录meta/GUID全保；三新父目录meta由Unity实际生成。源码仅Setup英文根与相对路径/相关中文注释。所有70配置、3FX预制体、collector/表/动画/场景/播放实现/声音时点音量/根位移保持。
- 验证：Unity原生迁移与独立79项检查PASS，含18clip/13目录/44作者Runtime音频强引用及依赖/3FX AudioSource强引用及依赖/collector；宿主8真实源隔离Rebuild0错/2旧警告。2629前置2582保持，36wave/meta和10目录meta按映射迁移，1计划源码更新、3新目录meta；无未计划变化。31clip/目录GUID各唯一、无旧源filename/root字符串。50正式冻结与原生输入一致，主重核50SHA0差异、全18wave路径无非ASCII。
- 使用说明项目分析/AUDIO_ASSET_USAGE_GUIDE.md现是全目录英文结构、18旧源/新用途映射、22声音事件帧秒音量；三个现用技能说明路径及素材表同步。旧中文方案历史记录保留但当前决策采用英文。
- 失败留痕：迁移临时MoveAsset已到目标目录no-op被拒绝，只修_temp工具跳过no-op并用原GUID恢复隔离输入重跑；独立GUID扫描夹具加CRLF容忍以读取旧meta，未修改生产meta迎合夹具。不重导制作配置，不生成角色/游戏按键/保存用户未保存场景或启动第二正式Unity。本轮仅资源/引用/必要编译，实际Game声音和完整Bundle发布未测。
- 正式日志项目修改日志/2026-10-11-AUDIO-ENGLISH-REFACTOR-CHANGELOG.md；证据_temp/audio-english-refactor-20261011/develop/migration-manifest.json、final-manifest.json、native-move-result.json、develop-result.md；review/native-reference-result.json、independent-protection.json、host-build.log及独立报告，主root-manifest-check.json。临时不清理。无代码/引用阻断，后续编辑从新英文目录选择同GUID声音。

## [VERIFY][技能][TR-515] 2026-10-11 技能轨道拖动修复（验证结果待终审归档）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4701—4711 行；历史时点记录，不代表当前仍在执行。

- 开发track_drag_develop修复五源：ActionEditor/Window/SkillEditorWindow.Interaction.cs、Timeline.cs、Structure.cs，ActionEditor/Timeline/BuiltinTrackRegistry.cs，以及Editor/Skill/SkillEditorAudioMotionTools.cs（均在LYF_MMO/Assets/GM_Game内）。声音恢复独立轨道，显式contentKind保证空Audio不混入Fx；仅视觉参与粒子/预警共享行，Sound复制粘贴/选择/删除沿原fxList下标。
- 通用拖动修复10类：Attack/Fx/Sound/Warning/Cancel/Projectile/SuperArmor/AdjustMotion/Trail/Move。正数点末帧跟随起点、0/负单次哨兵保留、区间跨度保持。已核其他动画/Camera/Interaction/Flow/Jump/HitFx及九RO秒字段，不引入新运行系统。
- 独立Unity原生前稿复现key8/end8左拖变key4/end8和Sound无独立行；末稿238断言PASS，覆盖14帧型事件/九RO秒字段/全部25枚举对应21规范行、Sound60→30叠层高度及FX高度保护、隐藏/布局重排/滚动/复制删除、真实Undo/Redo及切配置。通过反射执行真实生产方法，不等于正式窗口视觉和实际鼠标拖动现场验收。
- 独立真实源码三程序集隔离Rebuild：Runtime59/Editor34/宿主8源，0错，2/4/2既有警告。开发Editor34源编译也通过。主重算五源SHA与开发final-manifest一致。
- 正式Editor自动刷新日志已见forced synchronous recompile与Mono successfully reloaded assembly；MCP无活动实例，未获取正式窗口截图。没有重新强编正式工程或修改用户现场。
- 用户当前SkillConfigSO_3007001/fxList[2]的蓄力音频仍为key4/end8，正式单次音效不支持区间；原生导出正确拒绝且未改作者数据。不能仅凭历史差异确定用户意图。新诊断明确资源/条目/起末帧和工具/显示高级属性下将结束帧设0或等于开始帧再发布的操作。该既有区间是当前发布的剩余限制。
- 现用说明项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md已追加轨道与单次设置；证据_temp/skill-track-drag-20261011/develop/final-manifest.json、develop-result.md、editor-build.log，review/native-before-repro.json、native-track-result.json、native-track-core-result.json、independent-build-summary.json、三编译日志，主root-manifest-check.json。
- 独立保护及终审报告归档尚在进行；不提前标记整体完成。临时不清理。

## [VERIFY][技能][TR-516] 2026-10-11 技能轨道拖动修复（最终验收完成）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4712—4721 行；历史时点记录，不代表当前仍在执行。

- 独立track_drag_review终审PASS，无业务代码阻断；238项Unity原生回归、三真实程序集Rebuild0错完成。全部25TrackType按原别名形成21制作行，音效独立，10类原点/区间事件拖动共同修复，其他动画/流程/镜头/交互/HitFx及九RO轨道核验通过。
- 正式五源：LYF_MMO/Assets/GM_Game/Editor/Skill/ActionEditor/Window/SkillEditorWindow.Interaction.cs、SkillEditorWindow.Timeline.cs、SkillEditorWindow.Structure.cs，ActionEditor/Timeline/BuiltinTrackRegistry.cs，及Editor/Skill/SkillEditorAudioMotionTools.cs。现用说明项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md已同步；正式日志项目修改日志/2026-10-11-SKILL-TRACK-DRAG-FIX-CHANGELOG.md。
- 保护2632前置中2627保持/5计划源码修改，无新增生产文件/删除/未计划变化。101真实编译源末稿一致；五源与开发/独立/原生输入一致，主重算也0差异。配置、音频/meta/GUID、原表/动画/特效/root/运行逻辑和用户当前制作改动均保持。
- 五源规范通过，自动增量审查4ActionEditor源，ReadAudio第五源人工逐行审查；原Builtin namespace包装保持第三方身份，无新增namespace/foreach/三目/弱类型业务载荷。新增参数/局部变量等有中文用途注释。
- 剩余交接：SkillConfigSO_3007001/fxList[2]的Skill_SwordDrop_Charge仍是开始4/结束8区间，正式单次播放仍拒绝。若按单次播放，选中音效→工具/显示高级属性→结束帧设0或等于开始帧→重新发布。禁止根据历史差异擅自重写用户资产；本轮修复会避免以后合法点拖动意外变区间。
- 原生测试直接调用完整窗口生产方法、真实Unity Undo/SerializedObject，现场用户鼠标和OnGUI像素/截图尚未验证；不把方法/布局数据回归冒称视觉实测。正式Editor日志只读见自动编译域重载，MCP无会话。不造角色/按游戏键/保存用户未保存场景/重导配置/开第二正式Unity。没有未解决代码阻断，既有区间发布限制已明确告知。
- 证据_temp/skill-track-drag-20261011/review/independent-review.md、independent-test-report.md、independent-protection.json、verified-source-hashes.json、native-before-repro.json、native-track-result.json、independent-build-summary.json、编译日志；develop/final-manifest.json/develop-result.md，主root-manifest-check.json。原229回归和临时XML编码失败留痕，临时资料不清理。此次验收完成，不扩大无关测试。

## [VERIFY][技能][TR-517] 技能编辑器特效暂停与双重声音对齐：末稿验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4722—4735 行；历史时点记录，不代表当前仍在执行。

- 范围：九份既有集中窗口源、两份新增源及两份meta，13正式路径；主协调，preview_pause_develop／sound_waveform_develop开发，preview_pause_review独立审查测试，失败只交开发修改。源码文件及设计详见项目修改日志/2026-10-11-SKILL-PREVIEW-PAUSE-ALIGN-CHANGELOG.md。
- 原生旧版复现：playFrame=false时粒子时间仍由0推进到0.25秒。修复后原生61断言通过，覆盖根／子粒子冻结、暂停重绘实例保持、倒拖重建、回零、恢复、循环、30／120FPS、倍率、自定义寿命、停止／切配置回收、重叠声音独立源及样本位置、活跃true内嵌音源接管和原false不自动发声。正式Skill04的Audio节点原来不活跃，保持其不播放，不以禁音所有源代替暂停。
- 原生波形／对齐34断言通过：真实GetData双声道反相保留、跨4096块负主峰、尾块回绕排除、缩放缓存、静音／空／Streaming失败、clip身份替换、完整长度不改点事件末帧／END、重叠独立行、触发点／内部点平移、0哨兵／区间跨度、负触发帧拒绝及真实Undo／Redo。
- 原拖动核心229项本轮一次复验通过；旧OverlapAndExport9项按点跨度分lane的期望已过时，长声音现在按真实显示长度分lane，由本轮新断言覆盖，不将旧错误预期作为生产要求。自动seed／playOnAwake／随机速度1～4真实粒子F12→F3→F12：两次种子1、4颗seed逐颗一致，最大位置差0，无需额外种子策略修改。
- 三程序集103真实源隔离Rebuild0错：Runtime59、Editor36、宿主8；警告2／4／2为既有引用和未使用字段等提示，输出／obj全在_temp。末稿固定步dt和清理秒单位注释、停用预览不重复Scene重绘由开发返工后复编复验。
- 保护2632基线：2623不变／9计划修改／4新增，无删除及未计划修改；13正式／隔离worker一致，103编译源冻结一致。主再次重算develop／waveform两manifest共13正式SHA无差异。作者Runtime配置、音频波形/meta/GUID、原表／动画／FX／root／场景及正式运行链保持。
- 夹具失败留痕：循环范围假设两次、真实Prefab不活跃节点GetComponent未含inactive一次；只修_temp夹具，不改生产迎合测试。波形手势在batch中Event.current getter为null，直接赋值不建立GUI上下文；临时OnGUI＋SendEvent无图形和图形各一次仍无回调。保留诊断及失败日志，未直接设置抓偏变量伪装验证。抓取偏移算法经源码审查，真实OnGUI鼠标／像素未验。
- 现场边界：MCP当前无Unity会话；computer-use当前缺node_repl入口，未能按该技能初始化。原生AudioSource状态和timeSamples不等于设备声音输出，隔离方法／布局断言不等于正式用户窗口视觉。没有创建正式角色／按游戏键、保存未保存场景、重导用户技能稿、修改音频导入或开启第二正式Unity。现有3007001 key4/end8区间单次发布限制沿前轮交接。
- 证据_temp/skill-preview-pause-align-20261011/review：native-before-repro.json、native-preview-result.json、native-waveform-result.json、native-random-seed.json、原拖动核心结果、independent-build-summary.json、三build.log、independent-protection.json、verified-source-hashes.json及独立最终报告；develop／waveform交接和final-manifest，主root-manifest-check.json。临时资料不清理。使用步骤项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md已补，最终独立报告归档后记录结论。

最终结论：preview_pause_review独立审查与测试均PASS，报告已归档review/independent-review.md和independent-test-report.md。主读取两报告并重核13正式SHA通过，新增两meta在Assets／Packages全域GUID各唯一（root-new-guid-check.json）。无剩余代码阻断，正式GUI鼠标／像素及实际设备听感保留未验边界；本轮验收结束，不扩大测试。

## [VERIFY][技能][TR-518] 1技能仅首段与其他轨道预览：独立末稿验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4736—4748 行；历史时点记录，不代表当前仍在执行。

- 本轮preview_pause_develop开发、preview_pause_review独立测试审查，主协调／验收／记录。最终七既有窗口源，完整范围项目修改日志/2026-10-11-SKILL-PREVIEW-MULTISTAGE-CHANGELOG.md；没有新增生产文件或游戏运行修改。
- 真问题复现：当前3007001 END34@30FPS，F4蓄力／F13落剑均触发；落剑延迟层0.8～1.05秒，旧预览F34循环清理只留下Sword前段。独立跨Editor.update证实F34延迟层0、F38有6、F45有119、F58有303，实例一直存活，stopAction猜测证伪并保持源设置。取证仅隔离作者克隆改世界定位以独立检查真实粒子，不生成正式角色。
- 首稿打回：尾段延长后650帧所有29粒子系统count0但手工Simulate＋Pause下IsAlive仍真，无法循环。开发追加有限上界和实际count0清理，保留真实延迟／尾粒与倍率。两处测试前置0帧／最小编辑总帧误断言只修_temp，全部失败证据保留。
- 终稿原生58／58通过，Unity退出0：真实1完整后段F38／45／58、285个30FPS tick有限循环、同一落剑只触发一次、尾段暂停／倒拖／回到同帧／继续／停止；声音在END后续尾、准确样本位置、负例END之后新FX／声音均不发；手动飞行／派生提示／内嵌循环音源有限结束、自定义清理保持；标尺SetFrame／Seek尾段定位及制作范围不变；空测试动画容器尾段固定正式END且源变换不采样；四组事件速度／系统simulationSpeed换算一次及失活层保护。
- 其他轨道核查：FX和Sound统一循环截尾已修；动画／根运动保持末帧；碰撞／位移／轨迹Scene参考原区间不扩；手动飞行／派生仍原手动入口；镜头、九RO轨道、Buff／AI等原制作保存及参考范围，不新增或冒称Scene执行。
- 独立三真实程序集103源码Rebuild0错：Runtime59／Editor36／宿主8，警告2／4／2为既有提示；输出及obj在_temp，末稿103源冻结一致。保护2636基线2629保持／7计划源码变化／无新增删除及未计划变化，七生产／worker／开发清单一致。主再次重核七源SHA通过。
- 当前3007001配置key4／13、placementKind2／forwardDistance10、rotation(-90,180,0)、声音key4/end8、END34与baseline字节一致。全部作者Runtime资产、音频／meta／GUID、原表／动画／Prefab／场景及正式运行链保留。用户原区间声音单次发布限制仍按前轮交接处理。
- 边界：真实Prefab原生count/time及空容器动画状态验证不等于用户Scene像素、鼠标和实际设备听感；TargetTop未设目标时前方10米兜底不代表实际头顶现场已验。没有保存未保存现场、创建正式角色／游戏按键、自动重导配置、改原资源或开启第二正式Unity。未重跑无关229编辑／服务器／Bundle回归。
- 证据_temp/skill-preview-multistage-20261011/review：native-actual-before.json、native-tick-before.json、native-multistage-first-candidate.json、native-multistage-failed-lifecycle.json、native-multistage-final.json、native-multistage-final-after-rework.log、三build.log、independent-build-summary.json、independent-protection.json、verified-source-hashes.json和两独立最终报告；develop交接／final-manifest，主root-manifest-check.json。临时资料不清理，独立报告归档后记录最终结论。

最终结论：review/independent-review.md及independent-test-report.md已归档，独立返工复验PASS。主读取两报告并核对58／58原生结果、103真实源零错、2636保护及七末稿SHA一致，记录／说明齐全，无剩余代码阻断。正式GUI与实际设备输出边界保留，不把本轮粒子／音频状态验证扩称现场用户体验已验；本轮验收结束。

## [VERIFY][技能][TR-519] 2026-10-11 RO 技能时间线：基线与验收准备

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4749—4767 行；历史时点记录，不代表当前仍在执行。

- 本轮开发 skill_timeline_develop，独立审查测试 skill_timeline_review，主协调记录。范围与设计见 `项目修改日志/2026-10-11-SKILL-TIMELINE-RO-CHANGELOG.md`。
- 独立冻结 2765 前置路径，含当前技能源码、用户配置资产、原动画、场景、协议与 ProjectSettings。禁止将旧 Git 版本当作当前基线覆盖用户已有修改。
- 当前真实 GM_Game 基线 265 源隔离 Rebuild：0 错误、37 既有警告；临时工程首次 XML 编码问题仅修 `_temp` 夹具，失败日志保留。
- RO 已确认 FireEvents 在 MArtSkill 为 protected virtual void，Start 调用；SetTimer 零/负 interval 同步回调并返回 token=0；AddedTimerToken 与 KillTimerAll 管理本次施法票据。RO 的 object[] 在本项目以具名强类型替代。
- 正式 Unity MCP 只读 instance_count=0，未运行正式 Editor 测试、操作用户现场或保存资产。末稿必须分别记录真实源编译、调度与技能生命周期回归、保护验证，以及未验的实机范围。
- 证据 `_temp/skill-timeline-ro-20261011/review/baseline.json`、`GM_Game-baseline-build.log`、`baseline-build-summary.json`。当前为验收准备，尚未完成。
- 首稿：独立 GM_Game 267 真实源 Rebuild 0 错误、37 既有警告；完整生产计时器/技能/核心/缓存/声音/根运动边界回归 69 项通过。
- 独立追加重入用例发现阻断：零时刻扩展回调 Reset 后，FireEvents 注册循环继续读取已经清空的表现资产并抛空引用。已打回开发修复，必须验证取消后安全退出及回调重新 Fire 同一池实例。失败证据 `review/runtime-boundary-reentrant-failure.log`，不将首稿编译通过视为最终完成。
- 返工：FireEvents 冻结配置资产、UUID、施法代次，同步回调返回核原批次；Reset 更新代次，载荷核 UUID/代次。零秒 Reset、不同及同 UUID 再 Fire、旧代次负例与当前代次阳性均通过。Fire 返回值核原代次仍有效，防止启动过程取消仍误报成功。
- 最终独立测试 85/85 通过，证据 `review/runtime-boundary-final-with-pool.log`。23 份真实生产链源含 MTimerMgr/MSkill/MArtSkill/Core/Cache/Sound/Root 及 MSkillFactory/MCommonObjectPool/MObjectPool/MListPool，调度和工厂池借还均未 Stub；Unity 原生对象、资源、声音/动画边界采用薄夹具。厂内外部 Luban 表查询在此专用测试不装配配置表，正式全程序集另行覆盖真实依赖。先前82项日志保留，最后3项补足真实工厂借还链，最终统计使用85。
- 回归覆盖零/负/正延迟、至少1ms、同毫秒稳定序、跨帧、查询秒单位、取消与回调内取消重入、pause/Elapsed同停恢复、异常隔离、旧票据保护；暖/冷资产按原锁定行与已过时间补发、缺模型pending两轨独立恢复与取消、池复用、t0重入与代次；Q旧/新双实例、原根运动窗口/租约、粒子独立资源尾迹、普通走跳声音。现有技能没有可调timeline时间倍率入口，不冒称测试了新增倍率功能。
- 末稿 GM_Game 267 真实源隔离 Rebuild：0 错误、37 个基线既有警告，没有新增警告。`review/source-consistency.json` 核267编译源和23完整链输入与正式末稿一致。
- 最终保护 `review/independent-protection.json`：2765 基线中2762字节保持，仅3计划源码修改和4计划新增，无删除/未计划改变。两meta GUID各唯一；五源码全文件无 namespace/foreach/三目/弱类型业务载荷，增改声明/参数/局部变量中文具体注释独立审核通过。所有测试逻辑在 `_temp`，未挂正式组件。
- 主已读取两独立最终报告，重算开发七路径manifest全部相符，证据 `root-manifest-check.json`。正式日志 `项目修改日志/2026-10-11-SKILL-TIMELINE-RO-CHANGELOG.md`，当前任务/上下文/决策同步。
- 最终结论：开发63/63、独立85/85、真实编译及保护审查通过，无剩余代码阻断。本轮MCP无活动实例，未执行正式Unity角色画面/设备听感/PlayMode/服务器联机/Bundle发布；只读Editor自动刷新域重载不等同实机验收。计时器pause仅停止技能时钟，不代表Animator/物理/设备整体暂停。用户资产与未保存现场保持，临时不清理；本轮完成。

## [VERIFY][综合][TR-520] 2026-10-11 当前表现导出：独立复现与验收准备

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4768—4787 行；历史时点记录，不代表当前仍在执行。

- 用户当前 3007001：完整动画34@30FPS、FX4/45、声音0/end2与46/end0、制作END46。独立 worker 原生 Export 复现四层失败：显式完整动画末帧误拒、FX45越旧动画范围、声音0–2单帧门禁、声音46越旧动画范围。分层试验仅隔离内存克隆，未写正式资产。
- 开发 export_fix_develop；独立测试审查 export_fix_review 独占既有隔离 worker；只读分析 export_trace_analysis；主协调记录。八既有源目标详见项目修改日志/2026-10-11-SKILL-EXPORT-FIX-CHANGELOG.md。
- 统一声音契约为当前预览实际单次完整音频，保留作者结束帧制作标记；动画与旧表原时长、动作窗、根运动不延长，制作时间轴独立支持晚事件。已有 Interrupted 必须透传区分正常保尾与真正中断清理。
- 待末稿验证真实导出、自然结束后FX45/声音46、尾音回收、中断、冷资产及模型、同帧终点、池复用、旧资产兼容；编译、规范和生产保护独立通过方可结束。
- 证据 _temp/skill-export-fix-20261011，正式现场界面与设备声音尚未验，当前仍开发中。

### 当前表现导出修复：末稿验证

- 八既有源最终冻结。独立原生Export27/27通过，真实3007001原稿完整动画end34、FX45、Audio46、Audio0/end2原样保留，Runtime动画Duration1.133/独立Timeline46/30秒，依赖与所有已发布内容逐项一致；30/120FPS与24FPS非整数自然末及真裁剪等负例通过。
- 辅助独立运行45/45通过，24完整生产源包含实际组件/计时器/工厂/对象池；自然End后45/46与完整声尾、当前槽退出/根解除、真正打断、冷配置/迟模型、旧0资产、同UUID与同池对象重新Fire/旧票据、20秒长音和自然End先于预测确认均通过。原生对象/状态机/加载为薄边界，声音结束通过Stop交付状态；SkillCtrl Interrupted透传源码核对，未执行真实protobuf/网络。
- 反馈返工两项：完整长音纳入RO十秒兜底起点；旧预测到期只回收仍IsPredicted实例，避免已自然结束的尾段被误清。失败仅交开发修改。
- 四370真实源Rebuild零错：267/59/36/8，警告37/2/4/2均基线既有。编译源与正式冻结一致。
- 2786前置保护：2778不变/8计划源码修改/无新增删除或未计划改变；八源码规范/中文注释通过，开发manifest及主root-manifest-check.json全部一致。作者/Runtime、原表、声音/meta/GUID、动画/特效/场景保持，不自动重导用户配置、不操作用户角色/未保存现场。
- 证据_temp/skill-export-fix-20261011/review（27原生、构建、保护和SHA）；analysis-runtime（45真实链、24SHA、实际Compile项及独立报告）；develop（交接和8manifest）；主root-manifest-check.json。夹具依赖、换行重建、测试静态类型和主PowerShell无效空游标预检失败均仅临时侧修复/留痕，不改生产迎合测试；临时资料保留。
- 独立总审报告归档待最终确认；代码和隔离导出验证通过，正式GUI像素/鼠标、设备听感与真实服务器联调未验。用户退出Play、等待编译后通过原绑定按钮显式导出。

最终结论：export_fix_review已归档independent-review.md及independent-test-report.md，总审PASS。主读取两报告与source-consistency.json、45运行报告及2786保护结果，八源开发manifest重算一致；27原生/45运行/370编译/24运行输入/4原生修改源均对应同一末稿。CURRENT_TASK/PROJECT_CONTEXT/DECISIONS、正式日志与用法已同步，本轮完成；现场GUI/设备听感/联机边界保持未验，不自动发布用户资产，不扩大测试，临时资料保留。

## [VERIFY][综合][TR-521] 2026-10-11 RO 时间线职责纠正：最终独立验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4788—4802 行；历史时点记录，不代表当前仍在执行。

- 结论：PASS，无剩余代码阻断。开发57项，本轮独立94/94＝45项当前后动画生命周期重新验证＋49项真实MModel/Cache职责专项；94为本轮当前源结果，未复用上轮85项作为验收。267完整GM_Game生产源Rebuild 0错、37基线既有警告；26份完整生产运行输入含真实MModel、Cache、计时器、技能/组件/核心、工厂/对象池、声音与根采集。
- 四源：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MArtSkill.cs`、`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/SkillRuntimePresentationCache.cs`、`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/MModel.cs`、`LYF_MMO/Assets/GM_Game/Scripts/Global.cs`。Update实际仅base.Update与PruneAudio；后者回收已结束声源，不触发或重试声音。单独驱动MTimerMgr即可派发到期事件，真实缓存完成与真实MModel.FinishLoad通知可不调用技能Update补已到期指令，反复Update不重注册/重派发。
- 缓存专项：冷完成按原累计时钟差值补发、暖同步完成、资源包缺失后一次安装通知、地址/身份无效或加载失败终态、取消监听、异常隔离、旧同ID记录/句柄释放、同步再入取消及同UUID旧ready闭包拒绝均通过。
- 完整MModel专项：Load/SetGameObject/FinishLoad/Reset、模型/动画/声音/根采集完成先后、外部回调顺序、旧会话完成、外回调Reset、首监听Reset截断旧快照、监听异常隔离、模型重建恢复及技能Reset解绑后无旧池对象监听均通过。
- 当前后动画45项本轮复验覆盖自然End后F45/F46、完整声尾回池、20秒长音、旧Timeline=0、冷资产/模型迟到、同UUID工厂池重借、自然End先于预测确认、真正Interrupted立即清理、当前槽退出根释放；Q旧新实例、旧UUID取消不抢新根、真实clip/config门控就绪后无需Update重绑定采XZ、Y丢弃且delta只消费一次通过。Collector→MMove源码本轮字节保持。
- 缺固定挂点：声音独立执行，粒子按队首保pending；带齐层级模型reloadReady补两粒子、已执行声音不重发通过。当前无正式业务动态挂点组装入口，本轮不扩新系统或轮询。
- 保护：当前工作区2788路径基线（历史2786清单补纳本就存在MTimerMgr源/meta），2784原字节保持，4计划既有源变化，无新增/删除/未计划变化，无新GUID；用户作者/Runtime、表/声音/动画/粒子/场景/预制体/meta与所有未提交前置成果保持。中文具体注释及禁止namespace/foreach/三目/弱类型业务载荷审查通过；MModel.Delegate[]仅.NET事件分发边界，中文原因已说明。
- 末稿一致：267编译输入独立核0差异，26运行输入主支持重算0差异；四修改源与开发final-manifest、独立verified-source-hashes、当前SHA全部一致。证据 `_temp/skill-timeline-responsibility-20261011/root-manifest-check.json`。
- 构建证据：`_temp/skill-timeline-responsibility-20261011/review/GM_Game-build.log`、`source-consistency.json`；运行 `review/runtime-final.log` 末行RESPONSIBILITY_RUNTIME_PASS assertions=94；独立结论 `review/independent-review.md` 与 `review/independent-test-report.md` 均PASS且主已读取；保护/规范见同目录independent-protection.json、static-style-check.json、verified-source-hashes.json、final-diff.txt。
- 失败保留：runtime-first.log薄Vector3夹具缺magnitude编译失败，只修_temp；runtime-third.log的Q夹具Cleanup清所有核心却期待只取消旧UUID，改为专用ApplyServerEnd(old,true)复验，只修_temp。两个失败及后续通过日志完整保留，不改生产迎合夹具。
- 未执行：本轮未检测MCP，不援引旧无实例结果；正式Unity GUI/设备听感/PlayMode/真实网络/服务器/Bundle发布未验。原生对象/Animator状态与delta、YooAsset完成是薄边界交付，声音结束通过Stop交付，不能扩称设备或正式场景体验。未挂正式测试组件、未自动导出或保存用户现场；临时资料保留，由产品经理确认后清理。
- 正式归档：`项目分析/RO_SKILL_TIMELINE_RESPONSIBILITY.md`、`项目修改日志/2026-10-11-SKILL-TIMELINE-RESPONSIBILITY-CHANGELOG.md`；四协作入口同步，本轮完成，下一步原正式入口现场体验，不扩大已通过的隔离测试。

## [VERIFY][技能][TR-522] 2026-10-11 RO技能注册代码精简与FireEvents入口恢复：最终验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4803—4812 行；历史时点记录，不代表当前仍在执行。

- PASS，无代码阻断；唯一业务修改 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MArtSkill.cs`。FireEvents仍protected virtual void，末稿117（原116因参数中文注释增行）；Start53直接65调用。主链Start→FireEvents→SetTimer→OnProcessTimer→switch→fx/audio，两for直接AddedTimerToken(SetTimer)。删除TryResolve层但完整资产/全轨校验搬入FireEvents；Ready短捕获代次交具名方法，fx/audio为实际原消费器，不新增薄wrapper。必要强类型SO、cache/model、取消订阅、UUID代次及同步重入保留；Update仍仅base+Prune，TriggerAnim/Collector→MMove根职责不变。
- 独立99/99＝45最新后动画当前复验＋49职责重新执行＋5直接FireEvents入口专项；99是当前末稿重新执行，不用旧94充数。5专项验证冷Start也立即进入protected FireEvents、Update不注册、冷Ready回同FireEvents一次、重复Execute/Update不重入、暖Start单次进入与播放；原自然End晚事件/完整声尾、Interrupted、同UUID池重入、冷/迟模型、Q旧新根归属与挂点reload恢复保持。
- 267完整GM_Game源Rebuild0错/37基线既有警告；26完整生产运行源含真实MModel/Cache/计时器/技能/组件/工厂/池/声音/根采集。267编译输入独立0差异，26当前SHA主辅助重算0差异，一源开发/独立manifest/当前哈希相同：bafde6665be75a6a71982dec092d7dd4f76dc2a564d659a8e1026d294dfd45c2。
- 独立同口径减量：Start方法体有效29→7；注册/Ready/消费方法体含原TryResolve100→79；整文件有效301→281、物理588→559、复合条件112→96、分号169→158。分号含for头部，仅符号统计，不称精确减少11条独立语句；开发含声明109→89属于另一口径，不混算。
- 当前保护2788：2787字节保持，仅1计划MArtSkill变化，无新增/删除/未计划；其它业务、用户配置/资源、场景、协议、meta/GUID不动。两独立报告PASS主已读。失败仅runtime-before.log首次.NET9 System.Private.CoreLib BadImageFormatException环境异常，原命令重试通过，无生产调整；日志名称不表示旧业务复现。
- 证据 `_temp/skill-timeline-ro-simplify-20261011/`：review/independent-review.md、review/independent-test-report.md、review/GM_Game-build.log、review/runtime-final.log（RO_SIMPLIFY_RUNTIME_PASS assertions=99）、review/independent-protection.json、review/source-consistency.json、review/structure-measure.json、review/verified-source-hashes.json、develop/final-manifest.json、develop/simplification-metrics.json、root-manifest-check.json。测试仅_temp，未保存用户现场、挂正式测试组件或自动重导。
- 未验正式Unity画面/设备听感/联机/PlayMode/Bundle，未新查MCP；原生状态/Animator及加载交付仍薄边界，不扩称设备体验。当前任务、本轮日志 `项目修改日志/2026-10-11-SKILL-TIMELINE-RO-SIMPLIFY-CHANGELOG.md` 及既有分析 `项目分析/RO_SKILL_TIMELINE_RESPONSIBILITY.md` 已增量归档，临时保留由产品经理确认后清理；本轮完成，下一步原正式入口体验。

## [VERIFY][综合][TR-523] 2026-10-11 RO 到期回调与模型 GO 边界：最终独立验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4813—4827 行；历史时点记录，不代表当前仍在执行。

- 结论：通过，无剩余代码阻断。ro_timeline_responsibility_develop 开发，ro_timeline_responsibility_review 独立测试审查，ro_root_motion_reference 只读核真实RO；主只协调、验收、记录，没有直接修改业务。
- 三份业务文件：LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MArtSkill.cs、MSkill.cs，以及SceneEntity/3DModel/MModel.cs。Start:49精确base.Start/FireEvents两句；FireEvents:83仍protected virtual void；OnProcessTimer:321→InnerProcessTimer:150→audio:170/fx:206单条到期数据。模型AddLoadGoCallback:264/RemoveLoadGoCallback:287暖零票据、冷可取消；删除启动模型监听、OnModelReady、PresentationReady事件、两套技能pending与无参消费者。Update只基类时钟和已结束声音回收。
- 独立末注释稿92/92通过：45项当前后动画生命周期重新执行＋47项新GO职责，不以旧99/94/85充数。RuntimeBoundary直接链接26完整生产源含MModel、缓存、计时器、技能/组件、工厂/池、声音及根采集；Unity原生对象、Animator/状态机、声源及资源交付为薄边界。
- GO专项覆盖到期才申请、Start/Update不申请、暖同步零票据/冷一次交付、取消兄弟/异常隔离、外部回调Reset、Load旧会话、同模型重载保尚未交付任务、真正Reset清等待、Interrupted立即取消、自然End保GO/声尾但解除动画owner、同UUID真池复用拒绝旧闭包、Q独立任务与新动画owner、暖零秒Reset/重Fire、冷配置补发、缺固定挂点跳过/冷完整挂点正常、取消再添加和有符号token回绕的登记顺序。
- 45生命周期包含当前锁定配置行与冷配置、自然End后F45/F46、完整长声尾、旧Timeline=0、真正打断、同UUID真实工厂池重借、Q及当前动作槽解除根归属、尾音完成后回池；Collector→MMove本轮字节保持，实际动画水平增量未移入技能Update。
- 独立267完整GM_Game源Rebuild零错、37基线既有警告。开发3源、独立3源、运行前26源、编译267源冻结均对应正式末稿，主重算299项SHA零差异，root-manifest-check.json；独立final-manifest-check.json三个差异列表均空、runtimeExit=0/runtimeAssertions=92。
- 当前2788前置路径中2785字节保持、恰三计划源码变化，无新增/删除/未计划变化。MArtSkill本轮前置SHA a822049e2332baab09b5480c92964db31c503d72c8077d1a416b8145fe154183；保用户SetTimer换行/既有空格，不从旧bafde稿覆盖。资产、原表、音频/meta/GUID、动画/粒子/场景/预制体及服务器保持；未自动重导配置或保存现场。
- 中文具体注释由独立逐行审查，本轮增改无namespace/foreach/三目/弱类型业务载荷；MModel.Load最后两条局部/回调参数注释缺漏交开发补齐，末注释稿复编复验通过。RO object[]继续适配为已有MSkillTimerArgs强类型载荷。
- 实际返工：Dictionary删除首项后新增复用槽位导致GO交付3→2，runtime-order-repro.log保留。开发_loadGoOrder记录真实登记序，取消同步移除、逐条交付，删除再Add/int回绕通过。首次薄MGameObject缺NativeObject、首次finalize早于编译缺源SHA、开发临时csproj编码失败只修_temp夹具，未改生产迎合测试；失败保留，不计通过。
- RO事实：fx到期才调用模型AddLoadGoCallback，Stop移token；audio使用FMOD模型接口，冷普通声可跳过、区间实例由模型音频持有。本项目Unity AudioSource需Transform，冷声音在GO边界等待是本地适配，不声称RO音频同FX。完整已加载模型缺固定挂点报告并跳过该粒子为明确行为变化，不再无限待重载；冷GO正常等待初始化。
- 主已读取review/independent-review.md及independent-test-report.md，均通过；证据_temp/skill-timeline-ro-callback-20261011/review中的runtime-candidate-final.log、GM_Game-build.log、runtime-executed-source-hashes.json、compiled-source-hashes.json、final-manifest-check.json、independent-protection.json、static-style-check.json、verified-source-hashes.json与final-diff.txt；develop/develop-result.json、final-manifest.json和主root-manifest-check.json。正式归档项目修改日志/2026-10-11-SKILL-TIMELINE-RO-CALLBACK-CHANGELOG.md，分析项目分析/RO_SKILL_TIMELINE_RESPONSIBILITY.md，四协作记录已同步。
- 未验边界：本轮未新调用MCP；正式Unity GUI/PlayMode/真实Animator根运动和设备听感/网络/服务器/Bundle发布未验，不把完整生产逻辑加薄原生边界扩大为现场体验。未挂正式测试组件；下一步原正式入口体验，不扩大已通过隔离验证或自动发布资产。临时证据保留，由产品经理确认后清理。

## [VERIFY][技能][TR-524] 2026-10-11 特效时间显示：验收准备

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4828—4849 行；历史时点记录，不代表当前仍在执行。

- 独立冻结本轮UI和用户技能配置基线。单帧DrawEventTrack最小色块8像素、标签4像素导致裁剪；EffectTrackLabel无帧秒内容，Inspector无秒摘要。
- 三既有显示源由fx_time_develop开发，fx_time_review独立验证。纯显示lane/时间资源标签/tooltip/Inspector摘要，真实色块与编辑命中仍原帧范围；不改正式播放、作者或Runtime。
- 待验缩放/横滚、同帧/相邻、点末帧哨兵/真实区间、双语、30/120FPS、数据保护及对应编译。证据_temp/skill-fx-time-display-20261011，当前尚未完成。

- 用户澄清触发时间和持续时间都要看，范围增为四集中窗口既有源；持续只读复用预览保守参数估算、区别资源循环/技能单轮/custom截断，不改实际播放算法或END。
- 首稿36真实编辑器源Rebuild零错/4既有警告；147相关UI/技能配置保护基线仅4授权源码变化。原生19项帧秒、速度、延迟/子系统、缓存等通过后，真实1技能Prefab持续参数失败：AssetDatabase源的activeInHierarchy不能直接代表生成后粒子活跃状态。已打回开发在源根内按activeSelf祖先链只读判断，root本来inactive仍排除，不激活资源；正式与隔离Prefab字节一致，不是漏拷资源。
- 隔离原生GUI首次MouseMove未投递，后带图形隔离worker诊断出现native Crash，已停止GUI尝试并保日志；只继续无图形原生参数/字体布局验证，正式GUI鼠标/像素未验。未修改正式现场。

- 原生只读取证明确：真实Juqi源activeSelf=true/activeInHierarchy=false/16粒子，落剑true/false/13粒子。开发改为生成源根内activeSelf祖先路径判断，根失活仍排除，不激活原Prefab。返工后两真实资源持续估算3.500/9.050秒，原作者字段保持；这只是预览参数估算，不是实测精确寿命。
- 前述隔离worker所谓native Crash的实际原因已查明为上一隔离进程尚未退出产生Multiple instances锁冲突；只处理隔离worker，正式现场无影响。GUI事件未投递边界保持未验。无图形EditorStyles.miniLabel未初始化使后续字体布局夹具失败，独立审查正尝试原生字体边界初始化，不改生产GUI迎合夹具。
- 主规范检查补正修改函数DrawEventTrack内既有laneH三目为if/else、Structure新循环变量具体注释；开发撤回仅自己误匹配到的无关动画等价lane变更，最终动画方法保持本轮基线。正对最终四源复编复核，首稿/Prefab修稿manifest保留不作最终证据。

### 特效两类时间：最终独立验收

- fx_time_review独立总审PASS，independent-review.md和independent-test-report.md已归档；fx_time_develop返工Prefab资源活跃判定与规范末稿后通过。主读取两报告/source-consistency/147保护，重算开发4manifest+36编译输入40项SHA零差异，root-manifest-check.json。
- 原生无图形33/33，真实1两Prefab活跃路径有效（16/13系统，参数估算3.500/9.050秒）、双语/30及120FPS/点及区间、延迟/倍率/子系统/失活祖先/cache/custom/loop、真实字体CalcSize+纯显示lane/横滚不改END、生产拖动方法哨兵与跨度保持。字体初始化仅薄边界，未完成正式OnGUI像素/悬停/物理鼠标。
- 36真实集中Editor源Rebuild零错/4既有警告；147相关保护中143不变、4授权源码变化、无增删或未计划。用户最新作者声音60帧和Runtime/meta保持；正式播放链、原粒子/声音/动画/场景未改，不自动发布或保存现场。
- 误匹配无关动画lane两行自身改动已由开发撤掉，独立全文函数与baseline一致；曾称native Crash实际是隔离进程未退出导致多实例锁冲突，独立仅处理精确隔离worker，不影响正式Unity。GUI空回调、MiniLabel未初始化及锁失败日志均保留，不作为生产通过证据。
- 证据_temp/skill-fx-time-display-20261011/review中两报告、native-final-native-font.log/native-fx-time-result.log、编译、source-consistency、verified hashes、静态规范/保护与final-diff；develop末/首/Prefab稿manifest/result；主root-manifest-check.json。当前任务、上下文、使用说明及项目修改日志/2026-10-11-SKILL-FX-TIME-DISPLAY-CHANGELOG.md已同步。本轮完成，用户等待Unity编译后查看现有轨道和选中属性，无剩余代码阻断，临时保留不扩验。

## [VERIFY][技能][TR-525] 2026-10-11 RO 技能位移与根运动：独立验收准备

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4850—4872 行；历史时点记录，不代表当前仍在执行。

- 已核实际RO普通技能链为FireEvents→MTimerMgr(ECharge=13)→OnProcessTimer→InnerProcessTimer→charge→MDisplacementComponent.onStart/Update/onStop；角度单位度，曲线值为累计位移比例。全仓检索发现通用OnAnimatorMove桥但无角色业务订阅，不把它描述为RO角色Animator根驱动。
- 本轮ro_skill_motion_review冻结当前2788路径，基线GM_Game267真实源编译零错/37旧警告；92既有生命周期/GO用例在准备夹具中通过，仅为基线，不计本轮新运动实现完成。
- 原生导出验证使用本轮_temp/ro-skill-motion-20261011/review下专属native-worker，与另一特效显示任务及正式Unity现场隔离；首次batch导入正常退出。临时UTF-8/重复stub夹具问题只修_temp、失败保留。
- 开发范围和验收方案见项目修改日志/2026-10-11-RO-SKILL-MOTION-CHANGELOG.md及项目分析/RO_SKILL_MOTION_IMPLEMENTATION.md。当前客户端开发进行中，最终运动/导出/碰撞/生命周期与规范结果待独立审查，失败必须交开发返工。

- 独立首稿新增52项中51通过，真实失败为计时器Init后clock重合不能识别旧运动；已返工会话身份。原生默认Program误拒为Unity真实零曲线wrap=ClampForever与假设Default不符，已返工按原默认曲线比较；不是修改作者或夹具迎合生产错误。
- 后续同步换GO不追历史、旧pending失效与冷Clamp负基线等专项复验进行中。侧碰撞首次夹具在Fire后才换GO却未先建reload基线，导致正确冷保护跳过历史位移；只修_temp设置时序，保失败日志，不修改生产迎合。
- 七原资产只读检查发现3007001作者声音60帧/2秒而发布Runtime制作范围为1.8秒/54帧，ValidatePublished失败；最初口头“46帧”已按真实回读纠正。本轮before两旧导出源对同一当前输入已原生复现相同ReadAudio失配，证据native-baseline-mismatch.txt/log；明确既有未发布边界，不把隔离克隆新导出通过冒称七原资产全一致，不自动重导。

### RO 程序位移：最终独立验收

- 结论：通过，无本轮未修复的已复现阻断。ro_skill_motion_analysis只读参考、ro_skill_motion_develop修改客户端、ro_skill_motion_review独立测试审查；主仅协调/验收/正式记录，已读取independent-review.md、independent-test-report.md及final-audit.json。
- 155运行断言通过（63新增运动＋92当前既有生命周期重跑），31完整生产源链接真实Skill/Timer/Model/Move/Cache/Factory/Pool/Collector链；Unity Animator/CharacterController/表/资源交付为薄边界。覆盖首帧不双积分、sameclock、大tick多段末量、三策略/硬cap/回拉/瞬时/非零首末、冷资源剩余及过窗、同步GO和完成段pending、暂停/Init重合clock、Q/自然End/Interrupted/同UUID池复用、输入MoveCasting/跳跃重力一次提交、侧碰撞停止/非Player最小分支、非法数据与重叠门禁、真实权威确认切共享Core后仍用本次冻结时长。
- 64隔离Unity原生导出断言通过、退出0，真实SkillConfigSO/SerializedObject/AnimationCurve/ReadCharge/ReadRootMotion/Export/ValidatePublished。覆盖30/120FPS、Program End无+1、默认ClampForever、合法无限阶梯切线、曲线复制/权重/wrap、三策略及未支持参数门禁、程序与动画点/重叠/相邻、真实发布及反校验。七原作者Validate/Animation投影/空ChargeList兼容通过，六发布完全一致；3007001既有2秒vs1.8秒未发布失配单列，before原生复现同失败，不改资产迎合。
- 独立最终271完整GM_Game源Rebuild零错/37既有警告；8宿主Editor源零错/2既有警告。开发相同实际数量；旧口头59宿主源已纠正，基线267不算末稿。两次最后时长修复只动MSkill/driver，不影响原生170输入，按SHA保其64项有效。
- 范围：9既有业务源、4新增独立类型及4meta，共13源17路径；2788前置保护中2777不变、9本轮、2并发授权Window，新增8、零删除/未授权变化。前特效动画lane差分是撤销自身短暂无关等价改动恢复其真正原函数，已由前任务与主核，未纳入运动成果或再改既有函数。Collector、作者Schema与所有用户配置/资源、场景/协议/服务器保持。
- 规范：13运动源逐声明人工中文用途/单位/身份/生命周期注释审查，独立去字符串注释扫描无namespace/foreach/三目/弱业务载荷；四meta GUID唯一，测试仅_temp。主重核开发17、独立17、运行31、独立编译271＋8、原生170、开发编译271＋8共793项SHA零差异，root-manifest-check.json=PASS。
- 真实返工闭环：Init后重合clock续旧运动→会话标识；默认零竖直曲线误拒→真实默认比较；同步换GO及完成pending→核NativeObject清旧量；null旧资产/非有限采样先于cap/冷Clamp负基线→适配；真实BeginPrediction/ApplyCastResult换Core短段误拒当前长段→只读冻结时长。侧碰撞失败为换GO夹具未先建基线，只修_temp；初期XML/重复stub/GBK亦只修_temp。已读历史共享Core失败尾段另存shared-core-failure-captured-tail.txt，说明来源，当前runtime-final.log为155终验成功，其他首稿/返工失败日志保留。
- 证据_temp/ro-skill-motion-20261011/review：两独立报告、final-audit.json、verified-source-hashes.json、runtime-final.log/runtime-executed-source-hashes.json、GM_Game-build.log/Editor-build.log及两编译输入SHA、native-charge-result.json/native-charge-final.log/native-input-hashes.json、native-baseline-mismatch.txt/log。develop有末稿17manifest、279编译输入、develop-result.md/json、final-style.json/final-diff.txt，主有root-manifest-check.json。
- 未验现场：真实Animator.deltaPosition/Collector精确末帧及跨窗、正式PlayMode/画面/设备听感、原生CharacterController墙体物理、真服务器联调与Bundle发布。本轮未新调用MCP、未挂正式测试组件或自动发布。正式分析/使用说明、项目修改日志/2026-10-11-RO-SKILL-MOTION-CHANGELOG.md及四入口已同步，下一步原正式入口体验；临时保留待产品经理确认清理。

## [VERIFY][技能][TR-526] 2026-10-11 特效播放范围与重叠：验收准备

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4873—4893 行；历史时点记录，不代表当前仍在执行。

- 用户截图为1技能蓄力F4/0.133秒、落剑F45/1.500秒，只有触发小块和持续参数文字。源码按各事件独立Spawn并逐实例Simulate，没有等待前实例结束条件；实际资源delay和双实例存活待独立原生验证。
- fx_range_develop最小三源实施时间范围/延迟分段/分行与暂停定位，fx_range_review独立验证，失败打回。三源before已冻结在_temp/skill-fx-range-20261011/develop；审核独占隔离Unity，不改正式现场或用户资源，不自动导出。
- 正式记录项目修改日志/2026-10-11-SKILL-FX-RANGE-CHANGELOG.md。编译、粒子状态、GUI边界与保护结果完成后追加；临时资料保留。主首次记录补丁因TEST_REPORT锚点不存在失败，改用独立补丁及追加，未涉及源码。

### 特效播放范围：原生并发与末稿一致性

- 真实1技能隔离原生生产PlayFxAndSound/SeekPreviewFrame/SampleAnim/FxUpdate验证：F45第二实例立即生成，F46两计时独立增加，无排队门禁；同帧全部生成，暂停/倒拖/恢复及清理通过。内存克隆仅改WorldAbsolute定位避免生成角色，粒子资源、帧和倍率原样；不称正式TargetTop角色或GUI画面实测。
- 逐帧粒子计数显示Juqi最后非零F49、落剑首非零F53，4–90帧无非零计数重叠；空对象存活不能称可见重叠。落剑各层原delay0.25～1.1秒解释视觉先后。计数也不等于渲染像素。
- 独立末稿原生43项通过，native-final.log返回0；范围显示/倍率/FPS/同值与随机TwoConstants/custom/失活/双语/显示分行/横滚/真实拖动哨兵/作者END不变覆盖。真实参数预计Juqi F4→F109、落剑F53→F317，尾端是保守估算，未当实测最后可见帧。
- 主核开发3源manifest、独立36编译输入及原生77输入共116项SHA全一致，root-manifest-check.json=PASS。两独立总审报告及602路径保护结论待审核归档；未验正式GUI像素/悬停/鼠标，不扩大无关测试。

### 特效播放范围：最终独立验收

- fx_range_review两报告PASS，主已阅读independent-review.md、independent-test-report.md及final-audit.json。末稿43原生通过/退出0，36真实Editor源零错/4既有警告；602既有保护599不变/3授权源码变化，无新增删除未授权；中文注释及新增规范门禁通过。
- 开发3manifest、独立编译36、原生77共116当前SHA主重算零差异，root-manifest-check.json=PASS；开发/审核当前最终输入一致。此次同值TwoConstants及旧三行说明修正已复验，首18/候选41日志保留不重复计成果。
- 实际修改Structure/Timeline/SerializedInspector三既有Window源；四行文字及独立时间范围、重叠显示分行与原Seek/Sample暂停导航，原配置/作者END/Runtime/粒子资源、声音波形、RO正式技能/运动链保持。
- 排队疑问已核为独立实例按帧触发，真实可见粒子计数存在原资源时序先后：Juqi末F49/落剑首F53，无非零计数重叠。参数尾端不是可见尾端；计数不是像素。正式GUI/物理鼠标按钮、原角色TargetTop/挂点、实际渲染/听感及PlayMode/服务器未验。
- 本轮无剩余代码阻断，隔离worker已退出。使用说明、CURRENT_TASK/PROJECT_CONTEXT与项目修改日志/2026-10-11-SKILL-FX-RANGE-CHANGELOG.md同步；用户等编译后在原窗口查看/暂停逐帧，不自动发布或保存现场。临时证据保留待产品经理确认清理，不继续扩大测试。

## [VERIFY][技能][TR-527] 2026-10-11 特效制作区间导出：复现准备

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4894—4912 行；历史时点记录，不代表当前仍在执行。

- 用户新截图index1粒子笼统错误。当前3007001落剑已用户改F13/end111、最后声音F49，与历史F45/F60不同，本轮独立冻结新基线，不回退用户调整。只读ReadFx确认非零结束帧拒绝触发；其它混音/倍率/custom字段默认。
- 开发fx_export_develop最小适配制作标记与预览单次播放，审核fx_export_review独立before原生复现、末稿隔离Export/边界/拒绝项/编译/保护，失败返工；不自动发布正式Runtime或保存现场。首次rg旧Global路径不存在，rg --files已找到Runtime/Data/Global.cs，未产生修改。
- 证据_temp/skill-fx-export-20261011；正式日志项目修改日志/2026-10-11-SKILL-FX-EXPORT-CHANGELOG.md。最终结果待验证，临时资料保留。

- 验收假设纠正：主核原ActionTiming.ContentEnd对fxList只keyNumber，end111是原显示标记，不应额外延长Runtime触发时长。撤销最初“制作时长保111”假设，只保作者字段；开发/审核已收到，ReadTimelineDuration不改。原Y end<=start为单帧哨兵，兼容原契约，不自创倒序拒绝。

### 特效制作区间导出：最终验收

- fx_export_develop仅ReadFx及summary末稿，fx_export_review独立总审PASS。before原生Validate/Export复现截图错误，末稿64原生通过/退出0：实际落剑13/30秒单次事件，2条FX稳定排序，声音49及作者末111保持；原TimelineDuration=49/30，结束显示标记不扩时长。
- 0/等开始/非负倒序/111/10000制作末标记兼容，负帧沿原Y ACT112拒；120FPS、未接入各字段和NaN保拒、绑定/fxList/字段值准确诊断、无粒子/零缩放等原门禁保持。七作者Validate通过，六旧发布兼容；当前1原发布既有差异单列，隔离Export后反校验及依赖通过，未改正式资产。
- 8真实宿主Editor源Rebuild零错/2旧警告；2788保护2787不变/1授权源码变化/无增删未授权。ReadFx注释及禁namespace/foreach/三目/弱载荷审查通过，ReadTimelineDuration和包/预览/运行链保持。
- 主重核开发1manifest/独立8编译/301原生输入共310项SHA零差异，_temp/skill-fx-export-20261011/root-manifest-check.json PASS；末稿SHA3358a03a03523b5d714560a198dbe1ad7e2294a61781a10510a3fea00a9a6096。
- 正式GUI鼠标点击/PlayMode画面/听感未验，原生导出只隔离，正式用户配置F13/end111/F49、Runtime/meta/资源及场景保护。无剩余本轮代码阻断；等待编译后用户原按钮显式导出，不自动发布或保存现场。
- 证据_temp/skill-fx-export-20261011/review下before/final原生日志、native-result.json、8源编译日志、final-audit、两报告、保护/一致性及哈希；develop下result/manifest/diff/host-build。正式日志项目修改日志/2026-10-11-SKILL-FX-EXPORT-CHANGELOG.md及使用说明/协作入口同步，临时保留，不扩大测试。

- 主已读取本轮两独立报告并确认最终PASS；独立审计夹具初稿按方法签名前缀比较时误把合法summary变化判越界，仅调整_temp比较边界至summary之前，最终方法以外完整相同。不是业务失败或修改生产迎合测试，原生/保护结果不受影响，临时证据保留。

## [VERIFY][技能][TR-528-FILES] 查询任务的正式文档产物：2026-10-11 RO特效End只读核对

- 该任务记录的正式文档路径：`项目分析/RO_SKILL_FX_END_SEMANTICS.md`、`项目修改日志/2026-10-11-RO-FX-END-ANALYSIS-CHANGELOG.md`。
- 查询正文、当时状态及原文验证边界见 `Docs/AI/QUERY_TASKS.md` 的 `[TR-528]`；本条只保留文件产物追溯，不新增完成或检查结论。

## [VERIFY][技能][场景实体][TR-529] 2026-10-11 位移组件实体挂接：验收准备

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4918—4938 行；历史时点记录，不代表当前仍在执行。

- 用户明确位移组件必须经实体CreateComponent挂接。独立冻结当前真实工作区，核RO实体:964与本地MComponentMgr/EntityComponentBase/普通容器/实体生命周期；业务由开发Agent改，独立不代改。
- 验收新增实体创建/唯一组件查询、普通优先阶段与专用Move时序、容器不二次推进、真实释放与池重绑、停止、重力碰撞一次提交，重跑63运动/92生命周期。既有155仅上轮证据，不计本轮完成。
- 对应完整客户端/共享编译、enum旧值兼容与DLL实际引用、规范注释及当前资产保护待末稿。原生导出源未改则仅按SHA继承边界，不冒称本轮新原生运行。
- 临时_temp/displacement-entity-20261011，正式日志项目修改日志/2026-10-11-DISPLACEMENT-ENTITY-COMPONENT-CHANGELOG.md，当前开发中，发现失败交开发返工。

### 位移组件实体挂接：最终独立验收

- 结论PASS，无剩余已复现代码阻断。主已读取displacement_entity_review的independent-review.md、independent-test-report.md及final-audit.json；业务由displacement_entity_develop修改，主协调、验收和记录。
- 189项末稿运行通过：63运动、92当前技能生命周期重新运行、34新增真实实体组件。链接45完整生产文件，真实MObject→MSceneObject→MEntity、ROObject、MComponentMgr、EntityComponentBase、EntityComponentContainer、事件派发及Skill/Timer/Model/Move/Factory/Pool/Collector；状态机、Unity原生与配置资源仍薄边界。
- Created挂接、实体快捷与容器同实例、重复Initialize、Active禁止追加、每帧唯一曲线求值、Move不再推进、输入/重力/程序量唯一碰撞提交、自然末帧先提交后退出通过；禁用、睡眠、直接卸载、重挂、Destroy、PrepareForReuse、真实ResetForPool/池重借新宿主、旧代停止隔离与事件记录清理通过。候选直接卸载未立即清状态由主要求开发加OnDetachFromHost Stop，四新增回归通过；最后枚举类型注释稿已重新189运行。
- 独立271真实GM_Game源码Rebuild零错/37既有警告，实际引用新Common DLL；65共享源码Rebuild零错/4旧警告。compiled-shared-source-hashes.json有66项，为65源＋1csproj，不是66源码。
- 最终六源码及两DLL共八业务路径；MComponentMgr、EntityComponentBase、Collector、作者Schema、Runtime及原资源场景meta保持。五客户端差分和共享enum逐声明中文注释人工审查及禁foreach/三目/弱业务载荷扫描通过；客户端无namespace，共享原类型身份保持，不新增声明。
- 独立2796前置保护中2789不变、7授权变化（5客户端源＋2DLL），无新增、删除或越界。共享enum没有事前字节快照，不能称完整before SHA保护；原工具全文与可靠旧DLL核旧0–31及导出类型保持，仅新增32。可靠旧DLL SHA与冻结旧Common相同；当前Common/Net字节相等且等于开发同次构建产物，独立共享构建因元数据不同按反射契约一致核验。
- 主重算开发8末稿＋336编译源＋独立8末稿＋271客户端输入＋66共享输入＋45运行输入，共734项SHA零差异，root-manifest-check.json=PASS。两报告和final-audit匹配同一最终源码，不引用旧摘要稿运行。
- 本轮未新跑原生导出；旧170输入已有8后续授权差异，previous-native-input-comparison.json明确不继承旧64为本轮验收。当前作者F13/end111/F49字节保持，不自动导出或保存现场。
- 夹具XML编码、数学stub、默认碰撞组件重复添加、睡眠float比较及旧Move.Shutdown归属断言仅修_temp，失败日志保留，不把夹具修正冒称生产返工。
- 未验边界：真实Unity PlayMode/画面/设备听感、原生墙体碰撞、Animator.deltaPosition精确末帧与跨窗、服务器与Bundle。下一步使用原正式入口体验，不扩大已通过测试，不挂正式测试组件或自动发布。
- 证据_temp/displacement-entity-20261011/review下两报告、final-audit.json、runtime-final.log/45运行输入、GM_Game及MMoRpgCommon编译日志/输入SHA、shared-assembly-contract.json、previous-native-input-comparison.json；develop下8末稿manifest/336源SHA/编译/规范/差分，主root-manifest-check.json。正式分析、项目修改日志/2026-10-11-DISPLACEMENT-ENTITY-COMPONENT-CHANGELOG.md及四协作入口已同步；临时保留待产品经理确认清理。

## [VERIFY][场景实体][TR-530] 2026-10-11 实体显式组件初始化精简：验收准备

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4939—4951 行；历史时点记录，不代表当前仍在执行。

- 仅删除MEntity.OnBeforeInitialize的AppendExtraComponents调用并调整函数说明，确认显式位移挂接与基类普通初始化保留。客户端Assets/Packages登记依赖检查及末稿真实编译、实体生命周期回归由独立Agent执行。
- 新证据_temp/entity-explicit-components-20261011；前次189与编译仅历史证据，不替代本轮。正式日志项目修改日志/2026-10-11-ENTITY-EXPLICIT-COMPONENTS-CHANGELOG.md，结果待报告。

### 实体显式组件初始化精简：最终验收

- 独立两报告PASS，主已读取；271真实GM_Game源码本轮Rebuild零错/37既有警告，原夹具复制新目录重新执行189断言（63运动＋92生命周期＋34真实实体组件），退出0。不新增镜像测试或复用历史通过数。
- Assets/Packages的RegisterExtraComponents/AppendExtraComponents仅定义无实际调用；MEntity显式位移创建、原Builder装配及真实MObject.Initialize的虚装配→普通容器Initialize→Active保持。唯一源码变化为删Append调用及修summary；中文注释及禁止语法检查通过。
- 2796保护中2795不变，恰MEntity授权变化，无增删越界。上轮组件八路径除MEntity外七项保持，含共享枚举与两DLL；用户F13/end111/F49、Runtime、资源meta和场景保持。主核开发1末稿＋独立1末稿＋271编译输入＋45运行输入，共318项SHA零差异，root-manifest-check.json=PASS。
- 本轮未重建共享DLL或跑原生导出。Unity原生/状态机/表资源仍薄边界；真实画面、PlayMode、墙体、Animator精确末帧跨窗、听感、服务器/Bundle未验。无剩余已复现业务阻断，不扩大测试或自动发布。
- 审计夹具误假设CRLF但真实before为LF，仅修_temp精确删行检查后通过；没有生产返工或覆盖旧证据。证据_temp/entity-explicit-components-20261011/review下两报告、final-audit、271编译与45运行输入/日志、依赖搜索/精确差分及develop的before/末稿manifest，主root-manifest-check。正式日志项目修改日志/2026-10-11-ENTITY-EXPLICIT-COMPONENTS-CHANGELOG.md及CURRENT_TASK已同步；临时保留待产品经理确认清理。

## [VERIFY][技能][TR-531] 2026-10-11 RO 技能CD与脱手生命周期：验收准备

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4952—4967 行；历史时点记录，不代表当前仍在执行。

- 独立核RO两类CD键/秒单位/工厂借还、MSkill.Update返回bool/Cease及当前实体与状态机更新时序；冻结本轮现有工作区，准备真实链CD行为和脱手回归。
- 业务由ro_cd_lifetime_develop实现，ro_cd_lifetime_review独立验证，不通过返工。旧189仅历史证据，本轮必须重新运行受影响链并核实际预测/权威CD查询及回收，真实源编译与最终输入SHA。
- 临时_temp/ro-skill-cd-lifetime-20261011，不覆盖旧证据；正式日志项目修改日志/2026-10-11-RO-SKILL-CD-LIFETIME-CHANGELOG.md。资产F13/end111/F49保持，现场Unity与隔离边界分别记录。

### RO 技能CD与脱手生命周期：最终独立验收

- 独立两报告及final-audit均PASS，主已读取；253末稿断言通过＝原189本轮重跑（63运动＋92既有生命周期＋34实体）＋48新CD＋16新生命周期，dotnet运行退出0。47完整生产文件包含真实实体/组件管理器/基类/容器、技能/Core/工厂/池/计时器/模型/运动及两个新类型，Unity原生、表资源和状态机仍薄边界。
- 48CD覆盖实际Set影响CanStart/查询、两表同引用和同数字键隔离、同值刷新/缩短校正、零值/多项到期/清场归池、毫秒桥/不缩放/时钟跳变/同刻不双扣/睡眠暂停、预测与成功/拒绝/重复旧UUID/超时回滚、升级/移除/Shutdown。16生命周期覆盖当前/脱手唯一推进、默认与true Cease归池、未来Timer和冷GO取消、正在播放声音停止、删前项不漏双更新、同对象池重借重Fire旧false隔离。自然End晚事件/冷GO/完整声尾及Q/位移末帧通过当前原189回归。
- 独立273完整GM_Game源码Rebuild零错/37基线旧警告，开发同数量零错；基线271编译另存，不能当末稿。主开发10manifest＋273编译、独立10manifest＋273编译＋47运行，共613项SHA零差异，root-manifest-check.json=PASS。
- 范围5旧业务源、2新独立类型及2meta、既有生成工程索引共10路径。独立2796保护2791不变/5授权旧C#修改/4新源码meta，无删除越界；作者F13/end111/F49、Runtime、资源/场景/meta、共享DLL、服务器协议表及前置成果保持。七源逐声明中文注释和禁namespace/foreach/三目/弱载荷通过，新GUID各唯一。
- 生成GM_Game.csproj不在双方baseline且没有before字节快照；仅核两新Compile各一次、原271索引均在及开发replace脚本，不能声明其他节点完整before保护。新增差分无尾空白，HEAD整文件仍有MArtSkill既有用户登记行空白，不误称全HEAD检查通过。
- 主审首稿Cease(false)不终止而等待自然End，已交开发修为任意cleanUp都真结束；默认资源注销专项复验通过，自然End等待仍仅原ApplyServerEnd(false)契约。原189断言和数据保持，薄Tick改真实Skill先Machine调度。夹具XML声明utf8和float精确1.7阈值残量仅修_temp，生产未迎合；239/249候选和失败日志保留，不计末稿通过数。
- 证据_temp/ro-skill-cd-lifetime-20261011/review两报告、final-audit/10末稿、final-diff、runtime-final.log/47输入、GM_Game-build.log/273输入及独立baseline；develop的before/末稿/编译/规范/保护，主root-manifest-check。正式分析、项目修改日志/2026-10-11-RO-SKILL-CD-LIFETIME-CHANGELOG.md及四入口已同步。
- 无剩余已复现代码/规范阻断。本轮未重建共享DLL、执行原生导出、挂正式测试组件或保存现场；网络仅源码核链及完整编译。实际PlayMode/画面/键盘施法/听感/墙体/Animator精确末帧跨窗/服务器联调/Bundle未验。下一步原正式入口体验，不扩大测试或自动发布资产，临时保留待产品经理确认清理。

## [VERIFY][技能][TR-532] 2026-10-11 RO 技能去除额外确认等待：验收准备

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4968—4973 行；历史时点记录，不代表当前仍在执行。

- 新需求覆盖旧逐笔预测确认超时回滚；分析、开发、独立审核已委派。新目录保存当前 before，生成工程提前保存快照，保护当前作者 F13/end111/F49 与既有成果。
- 待验证：无服务器确认的再次施放和生命周期、实际回包 CD/MP/拒绝链、旧 UUID 去重、Q、自然尾段与实体位移；本轮尚未得到编译或运行结果，旧 253 仅历史证据。
- 正式日志 `项目修改日志/2026-10-11-RO-SKILL-NO-CONFIRM-WAIT-CHANGELOG.md`，新临时证据 `_temp/ro-skill-no-confirm-wait-20261011/`，失败交开发返工。

## [VERIFY][技能][TR-533] RO 技能去除额外确认等待：最终验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4974—4984 行；历史时点记录，不代表当前仍在执行。

- 主已读取本轮 independent-review.md、independent-test-report.md 与 final-audit.json，三者结论均 PASS；605 项 SHA 复核和六份正式文档差分空白检查通过，当前任务已收尾。
- 独立 final-audit PASS，无生产代码失败；本轮 275 项重新运行通过：92 既有表现生命周期、63 运动、34 实体、49 新语义 CD、16 Cease/池重入、21 无确认专项。旧确认超时断言按用户新语义替换，历史 253 不作为本轮结果。
- 51 完整生产输入包括真实 SkillCtrl、SkillViewData、实体/组件/技能/Core/工厂/计时/模型/运动及 protobuf 的真实 Request/Result 编解码；快捷输入→发包→立即表现→跨旧期限再次施放→迟到 CD/MP/两窗口、旧拒绝保新实例、无 End 回池后最新结果、重复/旧连接、UUID 重用、升级旧核心 Cease 保新及权威 CD 均通过。输入/Socket/UI/基础信息/SceneWorld、Unity原生资源和状态机仍薄边界，不冒称真实网络联调。
- 独立及开发 273 真实客户端源码 Rebuild 零错误/37 既有警告；四源码中文声明与参数/局部用途审查及禁 namespace/foreach/三目/弱业务载荷通过，新增差分无空白问题。业务中不加入测试入口。
- 主复核开发 4 末稿+273 编译、独立 4 末稿+273 编译+51 运行，共 605 项 SHA 零差异；`root-manifest-check.json` PASS。独立 2801 路径前置保护2797不变、四授权旧源改动，无新增删除越界；本轮工程有真实 before，全部字节不变。旧 MSkillPrediction 定义仍编译/链接但无运行业务调用，未删除用户源/meta/索引。
- 必要自查修正：新施放清已应用 UUID 标记支持回绕；Cease 读当前安装 Core 避免升级前旧核心清新 CD；均重新编译和运行。无未来制作的脱手推进夹具改为含未来事件，旧期限期望改新语义、剪辑时长和重复薄角色定义只修 `_temp`，失败日志保留，生产未迎合夹具。
- 当前无未修复已复现业务阻断。未重建共享 DLL、执行原生导出或发布 Bundle；作者 F13/end111/F49、Runtime、资源/meta/正式场景、服务器协议/表保持。真实 TCP/服务器、PlayMode/画面/听感、原生墙体与 Animator 精确末帧跨窗未验。
- 证据：本轮 review 下 runtime-final.log、51运行/273编译输入SHA、GM_Game-build.log、final-audit、verified-source-hashes、独立两报告及before；develop下四源manifest/diff/273编译/规范/保护，主root_verify.py/root-manifest-check.json。正式日志和分析同步，临时保留待产品经理确认清理，下一步原正式入口体验，不扩大无关验证。

## [VERIFY][技能][TR-534] 2026-10-11 RO 技能更新精简：验收准备

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4985—4991 行；历史时点记录，不代表当前仍在执行。

- 开发与独立审查已委派，新 `_temp/ro-skill-update-simple-20261011/` 保存事前源码、包装源/meta和生成工程完整快照。
- 待验真实 RO 当前动作 OnUpdate 与常驻列表倒序 Update 分工、每帧一次/Cease自删除、Q/自然尾/声音/冷 GO/Timer、无确认/迟到结果和运动实体；去除仅用于假设重入的旧断言，不为它们恢复快照包装。
- AGENTS.md、AIconfig/workflow.md 已加入参考移植优先原结构/最小适配/不扩需求外机制的约束；实际业务结果尚待末稿，不引用旧275作为本轮通过。
- 日志 `项目修改日志/2026-10-11-RO-SKILL-UPDATE-SIMPLE-CHANGELOG.md`，未验真实Unity或网络；不改资源、不自动发布、不清临时。

## [VERIFY][技能][TR-535] RO 技能更新精简：最终独立验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4992—5002 行；历史时点记录，不代表当前仍在执行。

- 主已读取本轮 independent-review.md、independent-test-report.md、final-audit.json，结论均PASS，无已复现业务阻断。独立274断言＝92生命周期+63运动+34实体+49CD+15列表/Cease+21无确认，50完整生产源含真实SkillCtrl/protobuf执行；272真实GM_Game源Rebuild零错/37旧，不能用历史275/273替代本轮。
- 真实Entity验证当前仅动作OnUpdate推进、Skill先Machine导致移交当帧不重复、下一帧列表一次推进；倒序三普通生产技能两项自Cease移除，中间存活技能不漏。Q/自然Timer晚事件/冷GO/完整声音/默认Cease/无确认续放/迟到CD与MP/实体运动均重新运行。移除三项假设虚Update删兄弟/重Fire断言，补两项普通自删除回归，275−3＋2＝274，不为人造场景恢复包装。
- 五业务路径为两源码、工程索引和包装源/meta删除；2801保护2796不变、三授权修改/两授权删除，无新增或越界。删除类型仅前次Agent新增且无用户其他使用；CD更新块完整字节相同，真实异步计时代次保持。
- 独立工程保护发现删Compile丢BOM，交开发恢复后精确核92687→92598，仅89字节Compile删除，其余BOM/换行/节点一致。未变业务不扩大重测。首夹具缺Stop仅补_temp薄边界，失败日志保留；生产未为夹具改逻辑。
- 主root_verify.py重算开发3末稿+272编译、独立3末稿+272编译+50运行，共600项SHA零差异，另核两删除文件before SHA及正式路径不存在，root-manifest-check PASS。两源码修改范围中文声明/参数/索引/局部用途和禁namespace/foreach/三目/弱载荷、新增差分空白检查通过。
- 正式约束修改AGENTS.md和AIconfig/workflow.md，四协作入口和项目分析/RO_SKILL_CD_LIFETIME.md同步；主只协调/记录，业务由开发Agent实施并独立审核。详细日志项目修改日志/2026-10-11-RO-SKILL-UPDATE-SIMPLE-CHANGELOG.md。
- 证据本轮review两报告、final-audit、manifest/删除SHA、274日志、272编译/50运行SHA；develop的before/manifest/diff/272构建/保护和csproj-byte-check，主600项复核。作者F13/end111/F49/Runtime、资源/正式场景、服务端/表/协议及共享DLL保持，不导出或挂测试组件。
- 未验真实Unity PlayMode/画面/听感、TCP/服务器、原生墙体、Animator精确末帧或Bundle；Unity/状态机/输入SocketUI/资源交付为薄边界。下一步原入口体验，不扩大测试或清临时。

## [VERIFY][技能][TR-536] 2026-10-11 技能 CD 提前清零：验收准备

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 5003—5008 行；历史时点记录，不代表当前仍在执行。

- 用户报告无限施放；源码疑点为Cease自然回收调用Reload清单CD。独立需在修改前完整输入链复现10秒CD/0.5秒表现、约0.61秒后再次输入成功，再用同场景验证修复后CD剩余/请求数。
- 当前尚无本轮运行或编译结果；274历史结果不能替代新缺陷验证。旧“预表现Cease清CD”期望必须按本次实际缺陷修正，真实等待层不复活。
- 新证据 `_temp/skill-cd-early-reset-20261011/`，正式日志 `项目修改日志/2026-10-11-SKILL-CD-EARLY-RESET-CHANGELOG.md`；开发最小两源，独立复现/编译/正常CD及受影响回归后收尾。

## [VERIFY][技能][TR-537] 技能 CD 提前清零：最终独立验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 5009—5020 行；历史时点记录，不代表当前仍在执行。

- 主已读取independent-review.md、independent-test-report.md、final-audit.json，三者PASS。before完整SkillCtrl/protobuf输入实际输出0.61秒CD=0/已回池/第二次请求成功；同一末稿保9.39秒契约在冻结旧两源实际失败，末稿通过，before两日志与契约失败报告保留。
- 独立298当前断言＝274受影响回归重跑+9真实输入早回收CD+15正式五技能配置。0.61秒回池但仍9.39秒CD、20次快捷键不发包不创建、9.99秒阻断/10秒归零成功，中断保9.9秒；旧UUID/升级回收/成功拒绝迟到重复、Q、未来Timer/冷GO/声尾/实体运动均通过。零CD仅兼容用例，当前正式配置没有零CD行。
- 50完整生产源包含真实SkillCtrl/协议编解码/Factory/Core/Component/Timer/池/Entity/Model/Move等；当前实际生成TbSkillEffectInfo/ByteBuf独立只读解析bytes，CD五行为500/8000/10000/9000/13000毫秒、组0，与xlsx一致，真实Factory乘0.001f和正式行值输入验证通过。查询映射/资源交付、Unity原生/状态机/输入SocketUI/基础角色仍薄边界，不称现场网络或资源加载实测。
- 独立及开发272真实GM_Game源码Rebuild零错误/37既有警告；实际两旧源仅删除Cease→Reload清CD与无其他调用Reload定义，补一中文职责注释，无新增声明/字段/接口/状态。禁namespace/foreach/三目/弱载荷、新增差分空白和中文修改范围审查通过。
- 主root_verify.py复核开发2末稿+272编译、独立2末稿+272编译+50运行+10实际配置读取，共608项SHA零差异；root-manifest-check PASS。2799前置保护2797保持/两授权修改，无增删越界，工程真实before全部字节一致，用户作者/Runtime/资源/meta/场景/表/协议/DLL保持。
- before输入共50：保护baseline覆盖42，额外8共享实体/生成协议源按原before运行SHA另核不变；同契约before与原before全部50一致，不冒称保护baseline覆盖全部输入。独立审计曾以布尔false表示部分输入未在baseline，已改具名覆盖42+8/各自差异0结构，非业务失败，没有扩大或重复运行。
- 旧测试缺口为自然无确认回池/迟到结果通过却未断言池后仍有合法CD，本轮beforeFAIL/finalPASS新增用例补齐，未保错误清CD期望。无剩余已复现阻断；不改配置、不重建共享DLL、不导出或发布、不挂正式测试组件。
- 证据本轮review两报告/audit、before与同契约失败、298运行、272编译/50运行/10配置SHA及真实生成读取器结果；develop两末稿/diff/编译/保护/实际表证据，主608复核。正式日志项目修改日志/2026-10-11-SKILL-CD-EARLY-RESET-CHANGELOG.md与四入口/分析同步。
- 未验真实Unity PlayMode/画面/听感、TCP/服务器、原生墙体、Animator精确末帧/Bundle；下一步原入口体验，临时保留待产品经理确认清理，不扩大已通过验证。

## [VERIFY][界面][TR-538] 2026-10-11 中央真气/气力弧条：验收准备

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 5021—5026 行；历史时点记录，不代表当前仍在执行。

- 用户澄清是中央圆盘两侧另加弧条，不是把底HPMP改义；主按左真气右气力，保既有中央怒气暗满/侧槽/底条，仍只界面未接领域数据。
- 开发/独立本轮重新冻结当前稿，拟窗口两强类型独立进度入口＋UIRoot局部弧形；默认空/无效上限/负值/超上限/两条及怒气独立性、引用/编译/原生样例/比例待验。历史29/685只历史，不计本轮结果。
- 证据 `_temp/skill-hud-qi-stamina-20261011/`，日志 `项目修改日志/2026-10-11-SKILL-HUD-QI-STAMINA-CHANGELOG.md`；不加真实玩法规则、不保存正式场景或清临时。

## [VERIFY][界面][TR-539] 中央真气/气力弧条：最终验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 5027—5035 行；历史时点记录，不代表当前仍在执行。

- 末稿四路径：窗口和UIRoot局部修改，专用HudResourceArc PNG/meta新增。窗口两个字段/两个独立强类型入口，除此与before字节相同；UIRoot仅新增16YAML块、父子项/两引用，剔新增后原块完整字节保持。原底HPMP/中央怒气/侧槽/输入/CD和本机设置、工程保护不变。
- 开发/独立本轮272真实GM_Game源Rebuild各0错/37既有警告；独立24条真实接口断言通过，直接链接当前两控件和两ViewData四源，Unity/资源等为薄替身。覆盖默认空、负值/零值/部分/刚满/超上限、无效上限、缺失组件、两弧互不覆盖/怒气和HPMP独立、原R技能ID/CD基准保持，不将前轮29当本轮新测试。
- 独立final-audit PASS：6474保护6472保持/两授权旧路径修改＋两新增，无删除越界；新GUID唯一、四弧正确引用，616×616透明环中心alpha0/alpha0..255、无mipmap/Sprite导入及原UISprites CollectAll适用。十锚完整、无未解析fileID/HUD外变化，HUD Graphic不拦截点击，五比例几何无屏外Graphic。
- 原生Unity开发执行末稿复制品，主与独立目检空轨/部分/满弧＋满怒通过；三组各五比例1920×1080、1280×720、1920×1200、2560×1080、3440×1440及三裁图共18PNG。左青真气右金气力外弧与中央怒气内环可分辨，底HPMP不变，无新增文字。初用图集厚环放大模糊已返工新细环，首失败日志保留，空轨按最终616资源重渲染冻结。
- 三末稿日志preview-empty-final/partial/full-rage完成且退出0，无脚本或shader错误。主689项SHA一致＝开发4末稿＋272编译、独立4末稿＋272编译＋4方法、115原生输入＋18图，root-manifest-check PASS；正式分析/日志/入口同步。
- 两接口没有生产来源调用，正式默认空；原生比例和满怒是隔离样例，不代表已接通真气/气力/怒气玩法。未验真实服务器/正式场景施法/资源消耗或Bundle，不挂正式测试组件、不清临时，无剩余已复现阻断。后续用户确定玩法来源再接入。

## [VERIFY][技能][界面][TR-540] 2026-10-11 技能 HUD 视觉返工：验收准备

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 5036—5042 行；历史时点记录，不代表当前仍在执行。

- 用户认为前版丑，要求去人物信息和血蓝文字、中央满怒才亮；已确认只做界面，怒气玩法后续接入。本轮没有服务器怒气字段/规则可用，不以CD/MP生成满怒。
- 开发/独立从当前未提交前稿冻结输入，旧568项/原生图只为历史，不能替代本轮结果。拟两源码三Prefab，最小怒气显示接口、默认暗态及细边透明风格。
- 待验：真实方法默认暗/无效max不亮/满态切换/异步槽与图标、普通CD槽不受影响；去头像角色信息/生命法力数值文字、原血蓝条/输入/CD保持，原生暗满CD图/多比例与272源编译、资产引用和保护。主先审暗态图再收尾。
- 证据 `_temp/skill-hud-refine-20261011/`，日志 `项目修改日志/2026-10-11-SKILL-HUD-REFINE-CHANGELOG.md`；当前无本轮通过结果，真实怒气机制不在本轮范围。

## [VERIFY][技能][界面][TR-541] 技能 HUD 视觉返工：最终验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 5043—5052 行；历史时点记录，不代表当前仍在执行。

- 末稿五业务/资源路径，两源码RoleCurrInfoWindow/SkillSlotWidget、三Prefab UIRoot/SkillSlotWidget/SkillCoreWidget；头像昵称职业与血蓝文字、无用字段/头像请求去除，血蓝条/原外部ViewData契约保持。改引用现有LiberationSans显示键/CD数字，不改字体资产本体。
- 开发/独立272真实GM_Game源本轮Rebuild各0错误/37既有警告。独立29断言直接引用两生产控件和两ViewData四源，Unity/TMP/UniRx/资源薄替身：默认暗、0/99/100/101与无效上限/负当前、晚到图标按当前状态、R晚建继承、普通槽不受影响、UI状态不改skillID/CD及HPMP条/MP约束通过。不是现场Unity/网络或怒气玩法运行验证。
- 独立final-audit PASS，6474路径6469保持/五授权修改，无删除越界；HUD外YAML块完整字节保持，无新未解析fileID，十键锚点/新字段/原CD四字段引用正确。Core原meta/GUID和工程before完整字节、本机设置及控制器输入链保持。两个原CD方法字节保持，普通槽无ReadyVisual，中央默认inactive且青蓝Graphic全部归它子树，HUD/槽无raycast拦截。
- 暗满各五比例1920×1080、1280×720、1920×1200、2560×1080、3440×1440，独立几何无屏外Graphic；开发同版本Unity末稿复制品原生渲染、主与独立目检暗/满/CD通过。总14PNG含一CD全图与三原像素裁图；暗态无青蓝环/技能图标，满态细环和原R图标，Q/R冷却2.5样例数字与键位清晰。三原生日志均完成/退出0，无脚本或shader编译错误。
- 首暗态实蓝底/默认蓝柔光按主目检意见打回，换中性灰黑细边和硬圆片后通过；未为视觉扩大玩法。怒气来源检索无独立字段，用户明确仅UI：当前无调用默认不亮，原R施法仍按原CD，不把满态截图当真实怒气数据接入。
- 主root_verify.py复核开发5末稿＋272编译、独立5末稿＋272编译＋4运行、113原生输入＋14图，共685项SHA零差异，root-manifest-check PASS；独立报告及最终字段/几何/保护证据随本轮review保留。分析已按最终稿重写，正式日志/入口同步。
- 未真实登录服务器/正式场景施法、未执行怒气玩法或发布Bundle；薄替身及隔离样例边界明确，安装包按原流程更新热更代码与资源。无剩余已复现阻断，不挂正式测试组件，不清临时。

## [VERIFY][技能][界面][TR-542] 2026-10-11 技能 HUD 武当布局：验收准备

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 5053—5059 行；历史时点记录，不代表当前仍在执行。

- 已委派客户端开发与独立审核；本轮拟修改角色HUD预制体/槽外观及必要槽定位，不改服务器、施放逻辑、冷却容器或背包功能。
- 验收目标为参考布局/背包蓝白样式、十个旧快捷键与真实HP/MP/CD、空槽不交互、装饰不遮挡世界点击、常见分辨率不溢出及资产引用有效。开发须提供可视证据，独立审核失败返工。
- 当前尚无本轮编译/运行/视觉通过结果；Unity MCP读取无会话，本机存在Unity进程，开发继续调查隔离渲染方式。不得用历史技能298断言冒称本轮界面验收。
- 证据 `_temp/skill-hud-layout-20261011/`，正式日志 `项目修改日志/2026-10-11-SKILL-HUD-LAYOUT-CHANGELOG.md`。

## [VERIFY][技能][界面][TR-543] 技能 HUD 武当布局：最终独立验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 5060—5069 行；历史时点记录，不代表当前仍在执行。

- 主已读取本轮independent-review.md、independent-test-report.md、final-audit.json与final-manifest-check.json，均PASS；已查看hud-detail.png和hud-cd-detail.png并通过视觉验收。源码最小两处接入：R选择中央外观、技能挂对应同名锚点。两旧Prefab改样式/位置，新中央Prefab/meta复用原SkillSlotWidget，无新增业务类型、伪技能或点击施法系统。
- 开发及独立272正式GM_Game源本轮Rebuild各0错误/37既有警告；主root_verify.py复核560项末稿/编译/合同与8张原生图SHA共568项零差异，root-manifest-check.json PASS。不用历史298断言替代本轮结果，也未重新运行技能Core计时/服务器行为测试。
- 独立6469当前路径保护6466保持、仅3授权旧文件改动，无删除/越界；授权新增中央Prefab及meta。UIRoot HUD外完整YAML块字节相同，无新增外节点/未解析fileID，十旧key锚点齐、新GUID唯一、原CollectAll与资源地址适用。两份本机UserSettings与SkillCtrl/PlayerInputCtr/RoleInfoCtrl/SkillSlotWidget源合同保持，工程before完整字节保持。
- 正式CanvasScaler与RectTransform独立几何核五比例1920×1080、1280×720、1920×1200、2560×1080、3440×1440无屏外Graphic。开发同版本Unity隔离原生渲染，独立读取/目检末稿截图；主不冒称独立重复运行Unity或正式场景现场。普通键/昵称/状态文字恢复，中央圆形清楚，CD图中Q与R原遮罩半填和2.5样例文字可辨；Graphic raycast全部关闭、血蓝Slider不可交互。
- 实际返工：首稿MaskCircle01素材是菱形，正式改用背包Common1真实圆片21300192并原生Mask；隔离TMP缺Resources/TMP Settings导致Awake直接返回、字形网格为空，补原依赖后恢复，正式字体无修改。嵌入包临时测试依赖/重复GUID只在隔离工程调整；独立PyYAML误把数字m_Name当整数仅修临时夹具，首失败日志与报告保留。
- 图像为末稿HUD原子树和槽Prefab隔离复制品原生渲染，移除业务脚本避免执行网络单例；项目图标、状态名称及CD2.5仅视觉样例，不写正式角色数值、不称已联调HP/MP/施法。现场服务器登录/正式场景/真实各CD阶段仍未验；未发布Bundle，安装包按原流程同步热更代码/资源。
- 证据本轮develop五比例/两裁图/CD全图及final-manifest/编译/preview-unity-final.log/preview-unity-cd.log，review两报告/audit/manifest/编译/保护/visual-evidence-hashes，主568项复核；正式日志与分析同步，无已复现阻断，临时保留待产品经理确认清理。

## [VERIFY][协作][TR-544] 2026-10-11 客户端/服务端 Git 上传：准备

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 5070—5075 行；历史时点记录，不代表当前仍在执行。

- 已读取仓库分支/远端/状态，客户端和服务端在同一根仓库，main存在一个未推送提交，origin/main只读查询成功。
- 本次不修改业务或导出资源；最近技能298运行/272编译为已有证据，不声明本次新跑。检查暂存完整性、临时文件排除、文件大小和远端提交一致，结果完成追加。
- .gitignore与上传日志/协作记录纳入；本机UserSettings保留未暂存，缓存/备份/锁文件留本地。日志 `项目修改日志/2026-10-11-CLIENT-SERVER-GIT-UPLOAD-CHANGELOG.md`，证据 `_temp/git-upload-20261011/`。

## [VERIFY][协作][TR-545] 客户端/服务端 Git 上传：完成验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 5076—5083 行；历史时点记录，不代表当前仍在执行。

- 暂存验收PASS：授权清单与实际暂存909条增删路径完全一致，Git重命名统计902个变更文件；客户端550、服务端6、Luban190，其余为协作配置与正式文档。无超100 MiB文件，客户端Assets内脚本对应meta齐全，无本机设置/临时文件误入。
- 正常提交 `c724c259e3f7e2db18f9bb081bdc7603f608878f` 并push origin/main成功，远端由60b2b74快进至c724c25，包含原已有本地提交01f8063；ls-remote与本地HEAD精确相同，未强推、未重写历史。
- 上传后工作区仅两份本机设置有未暂存修改：LYF_MMO/UserSettings/EditorUserSettings.asset、LYF_MMO/UserSettings/Layouts/default-2023.dwlt；保留用户原内容，不回退或清理。
- 全量暂存空白检查存在Unity meta常规尾空格及既有文档尾空白，未为上传格式化无关文件。本轮只修改忽略规则与上传记录；不把已有298断言/272源编译宣称为本轮新测试，不扩大为全客户端资源、服务器或现场验收。
- 证据 `_temp/git-upload-20261011/staged-validation.json`、staged-files.json、git-add-literal.log与staged-whitespace.log。修正重复暂存已删除路径的临时清单逻辑后通过，未改业务代码；无未解决上传问题，完成记录另行提交并推送。

## [VERIFY][综合][TR-546] 2026-10-13 双账号主城互见与移动：末稿验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 5084—5095 行；历史时点记录，不代表当前仍在执行。

- Unity只读现场：Scene_MainCity在Play、TimeScale1，世界仅UID1/RoleId1。本地模型与MainRoleCtrl绑定正常，IsMovable真/default；输入启用、当前向量0。场景另有MonsterCtrl，不将其当作第二账号。Editor.log有真实本地移动11180，不以静止快照认定输入失效。
- 服务端CLR只读堆：OnlineMap/SessionMap均2，RoleId1/2同Map1，当前位置距离约8.23米；客户端仍无第二玩家实体。服务器初始Pos空、附近判断按零坐标；初始11070早于模型完成/11180且后续移动无11100补建，已确认源码时序问题。
- 唯一业务MainCityMgr.cs改初次11180/11070顺序、远端服务器坐标和既有OtherRoleCtrl实体绑定。开发末稿280源0错38旧警告、独立最终280源0错39旧警告，独立警告签名无新增；服务Game0错11旧/Gate0错4旧为未改服务检查。
- 独立真实ClientMessageManager队列/MoveOperationHandler/RoleManager/网关透传/回环TCP：修前8项复现空Pos过滤及移动不补基础数据，修后19项双方11100、角色身份、会话路由和双方11181通过；测试没有连接正式账号或数据库。早期5/14项被最终8/19项取代，不累加报数量。
- Unity原生隔离正式OtherPrefab＋实体：旧控制器未绑定有额外位移、同对象绑定后位移0。高处及SnapToGround只证明双写，不能当作正式症状下落幅度。最终实际修改段原文抽取验证远端坐标、实体绑定、旧Update零写、本地出生保持4项全真；不是完整MainCity异步callback验收。临时对象/PreviewScene已清理，无正式场景保存。
- 主核两份280源编译清单及5份开发末稿清单共565条SHA全一致，root-final-hash-check.json；最终业务SHA75A4EB68F8660CA6F933C68E0F532BAA1C5BB59BCD85BE23D9DB9DBD0196DEA5，独立final-audit PASS。末次Unity控制台0Error/Warning，未清控制台；当前Play仍修前已加载代码，不称Unity新脚本已编译或正式联机通过。
- 尚未验收：修后真实双客户端互见、本地用户按键和远端动画；不停止当前Play、不修改正式数据库或运行服务。证据 _temp/two-account-city-20261013，修改日志 项目修改日志/2026-10-13-TWO-ACCOUNT-CITY-CHANGELOG.md。
- 用户随后确认两个账号当时仍在主城且互不可见。主拟沿修后顺序补发本地位置/入场，实际调用时编辑器已退出Play回Boot，获取Global.SceneWorld在反射边界失败（TargetInvocationException），未执行任何发送。随即资源确认isPlaying=False/无编译中/域已重载、控制台0错误警告；本轮主未停止Play，也没有完成正式修后联机验收。

## [VERIFY][构建][TR-547] 2026-10-13 双账号主城互见：正式服务程序集隔离验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 5096—5101 行；历史时点记录，不代表当前仍在执行。

- 只读CLR工具0错误0警告，两次非挂起读取GameServer61096，在线/会话索引各2、Role1(session8)/Role2(session7)同图约8.23米、时钟推进。首64位宿主与32位进程不匹配，改现有x86 dotnet运行后成功；不注入或写目标进程。历史离线Role2(session6)Pos空仅作此前快照事实。
- EnterWorldPositionSmoke使用当前正式GameServer.exe/Network.dll副本，SHA与原一致；真RoleManager/ClientMessageManager/MoveOperationHandler＋回环临时端口13项通过：空Pos→原点、出生距原点>100、先入場双方11100缺失、出生11180入场前可消费且只11181、位置保存、后11070双方11100身份/会话正确。结果ENTER_WORLD_POSITION_PASS checks=13，不把隔离结果称正式双客户端现场。
- 测试编译0错/1条既有Protobuf版本匹配CS1702；首运行漏Config出现规则目录错误，保原enter-world-position-runtime.log，仅补_temp规则后enter-world-position-runtime-final.log无该错误。未修改正式服务源码、配置、协议、数据库，未新构建或重启服务。当前控制台缓冲小且过滤读取未保留入场历史，不凭空宣称捕获正式入场发送日志。
- 证据 _temp/two-account-city-20261013/server/live-state-1.json、live-state-2.json、deployed-assembly-hashes.json及enter-world-position-runtime-final.log；分析 项目分析/TWO_ACCOUNT_CITY_SERVER_POSITION_ORDER.md；日志 项目修改日志/2026-10-13-TWO-ACCOUNT-CITY-SERVER-CHECK-CHANGELOG.md。客户端最小时序适配与主现场验收继续由对应职责完成，不清理临时。

## [VERIFY][构建][TR-548] 2026-10-13 Unity.Burst/Jobs后处理失败：本轮验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 5102—5108 行；历史时点记录，不代表当前仍在执行。

- Console真实Exception为用户贴的Processing assembly/Jobs段；Editor.log对应ILPostProcess Unity.Burst Trigger退出1，Tundra失败，后续Player scripts compile errors和GenerateStripedAOTDlls failed。没有用此文本推断CS业务错误或永久依赖损坏。
- 相同真实响应参数/123defines/243references、相同程序集和现有Runner，唯一测试变化为输出到_temp，单次Jobs/Burst后处理均完成、exit0；isolated-trigger.log及exit.txt保存，不等于编辑器整图/热更/AOT构建已通过。
- 已请求编辑器普通RequestScriptCompilation，原输入及正式热更/工具/Packages6路径SHA保留frozen-inputs.json，不删除文件或清控制台。原失败具体内部原因与恢复最终结果待补充，证据 _temp/burst-ilpp-20261013；日志 项目修改日志/2026-10-13-BURST-JOBS-ILPP-CHANGELOG.md。
- 恢复补验：真实CompilePlayerScripts输出到_temp返回typeDB=True、26个程序集，player-compilation-result.txt确认完成；工具等候被用户中断不覆盖已完成的原生结果。临时诊断首稿把ReadOnlyCollection当数组用Length编译失败，修Count后执行，不修改业务迎合夹具。当前新主城Play有两个真实玩家实体，控制台0Error/Warning；不能扩大为完整AOT/包发布验收。

## [VERIFY][综合][TR-549] 2026-10-13 待机平移：本轮末稿验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 5109—5116 行；历史时点记录，不代表当前仍在执行。

- 用户明确本地A和B观察A均概率待机平移，随后说明随便按移动键就可能出现。初读真实Play存在两个账号实体/控制器已绑；本地默认FIdle但当前Skill04且无触发，远端FIdle一致，初始静止快照不当移动帧根因证明。
- RO InState比较最新播放请求；本项目旧实现比较当前过渡源。停止向Idle的过渡中再次Walk/Run时会短路，旧目标继续落Idle。最小MAnimator单方法改现有_currentState比较；Default仅删没有定时调度的后摇返回/空killTimer/常量，正常实际Play恢复，无协议/状态机或同步组件改动。
- 独立5生产方法原文抽取30项before/final对照：本地/远端×走跑×战斗8组合前复现/后恢复；正常止走、同状态不重复、战斗跑速、技能回FIdle、起跳/落地保护保持。动画采样与基类播放是薄边界，不能称原生或正式双客户端验收。早期27被最终30取代，不累加。
- 开发280源0错38旧警告/独立280源0错39旧，初次双源码SHA分别Default FA48ECA…、MAnimator 3AAE49F3C58E670A71D73B0DF680F338B5725312A7C8E914D34CB2BB4BF78BF2；收口仅移除Default修改范围一处行尾空格，最终Default SHA BB7966F721F650C67134B7D004FCCCDA0D1418230A5EC289076D2D521D7010AC。开发/独立重新编译及30项运行、最终audit均PASS，主565条最终编译/运行SHA复核全一致，MainCity75A4…保持。没有业务返工，两差分仅授权范围。
- 原生夹具首两次分别缺临时兼容Ctrl、临时表现表查询Null；仅调夹具，未加生产检查。用户后续已退出Play并资源构建，Unity工具多次未返有效结果，纯Animator探针未验收；不反复争抢构建或宣称原生通过。失败记录root-native-attempts.txt，当前包版本是否含最终双源仍待生效核对。
- 本轮未主动停止用户Play/构建，未保存正式场景或发布热更。证据 _temp/move-idle-slide-20261013；日志 项目修改日志/2026-10-13-MOVE-IDLE-SLIDE-CHANGELOG.md、项目修改日志/2026-10-13-MOVE-IDLE-SLIDE-INDEPENDENT-REVIEW.md。下一步末稿生效后原生和正式双号反复停走画面验证。

## [VERIFY][综合][TR-550] 2026-10-21 原生网格恢复：续接连接复核

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 5117—5123 行；历史时点记录，不代表当前仍在执行。

- 只读复核确认Unity进程仍在，当前无Unity MCP工具，配置的127.0.0.1:8082无监听；没有执行对象激活或原生显示恢复，没有新的场景实图。沿用此前inactive定位，不能将历史状态冒充最新实时读取。
- 未修改业务、场景或资源；只补协作记录及项目修改日志。连接恢复后验收MainCityNavigation激活、原生网格实图、自绘隐藏，保持场景不保存。

## [VERIFY][导航][网络][技能][盘点][NPC][协作][TR-551-FILES] 查询任务的正式文档产物：2026-10-22 寻路与技能配置位移网络同步专项检查

- 该任务记录的正式文档路径：`项目分析/NAVIGATION_CHARGE_NETWORK_SYNC_CHECK.md`、`项目分析/项目难点.md`、`项目修改日志/2026-10-22-NAVIGATION-CHARGE-SYNC-CHECK-CHANGELOG.md`。
- 查询正文、当时状态及原文验证边界见 `Docs/AI/QUERY_TASKS.md` 的 `[TR-551]`；本条只保留文件产物追溯，不新增完成或检查结论。

## [VERIFY][协作][技能工具][TR-552] 2026-10-23 Skill 安装验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 5140—5145 行；历史时点记录，不代表当前仍在执行。

- `structured-context-compressor/scripts/render_template.py` 退出码0，输出九段交接模板。
- `token-budget/scripts/tokenop.py` 对123行样例输出47.57%压缩，保留错误、约束和下一步；按SHA恢复121-123行成功。
- `repo-context-ledger/scripts/ledger.py --repo D:/unitypro/LYFMMORGP init --dry-run` 退出码0，输出77项创建、2项更新预览，明确 `Dry run only. No files were written.`。
- `git diff --check` 通过；本轮没有业务代码或正式资源变化。证据：`_temp/skill-verify-20261023/`。
## [VERIFY][任务][TR-553] 2026-10-23 RO 任务系统 UI 归档验证与 Task.prefab 重建静态检查

> 中文对应同步；英文对应 `Docs/AI/TEST_REPORT.en.md` `[TR-553]`。仅同步协作记录，不改任何 Unity 资源、代码、配置、场景或 Git。

- 最终复查验收（2026-10-23，优先于下方中间状态）：真实 Unity 中全部 38 个 UI prefab 资产加载缺失组件均为 0，有效组件类型/数量与适配前快照完全一致（`_temp/ui-recheck-20261023/unity-before-adapt-types.json`、`unity-final-types.json`）。Codex 通过把 726 个已导入组件的持久 ID 与 YAML 文档逐一对应，再读取真实类型与当前 MonoScript GUID，确立 15 个旧 UI 类型的映射（`unity-legacy-mapping.json`），属 Editor 运行时类型证据，不是字段名推断。
- 独立严格保全核验通过：22 个文件、删除 586 个目标组件、适配 726 条 `m_Script` 行、移除 75 条无效覆盖项、10 条引用清零；无多余文档或属性变化，已校验的元数据保留。证据：`_temp/ui-recheck-20261023/codex-preservation-result.json`，开发侧第 4 轮自检亦通过。未执行渲染后的 UI 交互、客户端/服务端构建或玩法测试。
- 剩余边界：Task 原先 dirty 编辑现场在可另存之前已消失，其磁盘文件在获授权清理前保持未变；那些未保存编辑是否保留无法确认，最终 Task 使用备份的磁盘基线，而非宣称已恢复的快照。UI 之外的特效 prefab 不在本次清理范围。范围、执行者身份、文件清单与 Git 凭证：`项目修改日志/2026-10-23-UI失效自定义脚本复查清理.md`。
- 轮次身份补充：第 1 轮扫描、第 2—4 轮三次开发，不是 4 轮失败；豆包会话 `38448883626832642`、run `59485369211741442` 未实际交付且已 cancelled，不计有效轮次；`qwen3:8b-light` 单次 API 请求 60 秒超时、无输出、无落盘、未重试，故本 WorkBuddy 会话第 5 轮仅同步中文，不计为额外的资源修复失败。Git 精确暂存、提交与推送待 Codex 执行，不得声称已上传。

- 归档验证：`LYF_MMO/Assets/artres/Resources/UI/Task/` 下 8 个文件（`Task.prefab`、`QuickTaskPanel.prefab`、`TaskNavigation.prefab`、`Task.png` 及对应 `.meta`）SHA-256 对比全部通过。
- 静态 GUID 审查：依赖共享图集/字体/TMP/材质/按钮/窗口资产及动画控制器、`MoonClient.dll`、TextMeshPro 运行时；目标工程仅部分包含匹配的共享 GUID，本检查不建立 prefab 可加载性。后续 GUID 解析对运行时兼容性失败：三个 prefab 引用缺失的 RO 脚本 GUID `0a06b724a7ecc1948b8fd5525e4102e6`（MoonClient）与 `f70555f144d8491a825f0804e09c671c`；`TaskNavigation.prefab` 把一个 RO 图集 GUID 静默绑定到本地 `UI/Atlas/Map.png`。当前 prefab 归档视为运行时无效。
- `Task.prefab` 重建静态检查通过：对两个 RO 脚本 GUID 均零引用；全部 5 个脚本引用与 `UI/Prefabs/NoData.prefab` 使用的目标工程 UGUI Image/Text 引用相同，字体 GUID 解析到 `UI/Font/Common.TTF`；YAML 对象/组件计数与重建层级匹配（6 GameObject/RectTransform、5 CanvasRenderer/MonoBehaviour）。
- UI 预制体失效脚本清理静态检查通过：38 个 prefab 中仅 4 个 GUID 在 `Assets`、`Packages`、`Library/PackageCache` 无 `.meta` 来源；从三个 prefab 删除 10 个对应组件。删除后四 GUID 零引用，剩余 26 个唯一 `m_Script` GUID 全部可解析，且三个修改 YAML 资产的 GameObject 组件引用均指向已声明对象。
- 清理明细：`_temp/missing-script-cleanup-20261023/result.md`。未执行三个 prefab 的 Unity Inspector 逐项检查；`DailyTask.prefab` 有 AssetImportWorker 导入记录，`AdventureDiaryTask.prefab` 与 `FriendChatPrefab.prefab` 尚未发现本轮新导入记录。
- `UI/Task` 的失效脚本清理静态检查通过：`QuickTaskPanel.prefab`、`Task.prefab`、`TaskNavigation.prefab` 共删除 337 个使用 3 个未解析 GUID 的组件，含 `Task.prefab` 中 6 个 `stripped` 嵌套组件。删除后三个 YAML 均无 `m_Script` 引用，且无悬挂 GameObject 组件引用；节点、`.meta`、图片、字体均未改。明细：`_temp/missing-script-cleanup-task-20261023/result.md`。
- 三个任务 prefab 都有 AssetImportWorker PreviewImporter 导入记录，但未在 Unity Inspector/实际界面逐项验证，不能据此宣称视觉或交互功能可用。
- Unity AssetImportWorker 已成功导入该资源且对应导入块无错误；场景/Canvas 实例化尚未验证。未执行 Unity 导入/场景测试、prefab 实例化、客户端/服务端编译、网络协议或玩法循环（本轮为归档与修复，属有意延后的验收阶段）。
- 范围与源路径：`项目修改日志/2026-10-23-RO任务系统资源归档与设计.md`。分支 `codex/qwen-bilingual-memory` 不符合 `codex/feature-development`，存在无关脏改动，未提交/推送。


## [VERIFY][协作][VERIFY-SUBAGENT-SKILL-20261024] 2026-10-24 项目本地子Agent协作技能

> 中文对应同步；英文对应 `Docs/AI/TEST_REPORT.en.md` `[VERIFY-SUBAGENT-SKILL-20261024]`。仅同步协作记录，不改任何 Unity 资源、代码、配置、场景或 Git。

- `.agents/skills/project-subagent-coordination` 通过 `skill-creator` 校验器：frontmatter、命名与脚手架检查均通过。`agents/openai.yaml` 以 UTF-8 重新生成，接口元数据已复查。
- 人工内容复查确认该技能把「建任务时的模型选择」与「禁止中途切换模型」区分开，并保留项目的范围、执行身份、修复轮数与最终验收约束。
- 本项目仅为项目本地指令/元数据变更。不适用任何 Unity/客户端/服务端构建或运行时测试。既有无关工作区修改予以保留。
- 未决：未创建 WorkBuddy UI 任务以实测选择 Hy4 preview，也未新建豆包任务；未来派发时的模型可用性仍取决于客户端菜单。中文记录同步与 Git 状态见变更清单。

## [VERIFY][协作][VERIFY-SUBAGENT-VISIBLE-WINDOW-20261024] 子 Agent 可见性说明验收

> 中文对应同步；英文对应 `Docs/AI/TEST_REPORT.en.md` `[VERIFY-SUBAGENT-VISIBLE-WINDOW-20261024]`。仅同步协作记录，不改任何 Unity 业务代码或运行时资源。

- 添加可见性章节后，运行 `skill-creator/scripts/quick_validate.py .agents/skills/project-subagent-coordination` 通过，输出 `Skill is valid!`。
- 文档与技能修改的 `git diff --check` 通过。人工复核确认：有可用原生执行窗口时必须使用；没有实时窗口时公开 Hub/任务/终端状态条目；禁止声称状态输出就是实时操作视图。
- 本次为仅指令更新，未创建 WorkBuddy、Doubao 或 Qwen 真实任务，因此客户端窗口实际可用性和模型菜单行为仍未验证；不适用 Unity/客户端/服务端构建。
- 变更日志与中英文同步状态见 `项目修改日志/2026-10-24-子Agent协作技能.md`。
