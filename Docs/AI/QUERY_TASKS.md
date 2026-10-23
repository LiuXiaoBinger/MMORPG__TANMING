# 查找、询问与方案记录
> 【双语记忆】本文件为开发人员中文版本，完整查询历史正文在此；Agent 默认读取对应的精简英文版 `Docs/AI/QUERY_TASKS.en.md`。英文为精简工作记忆，旧编号尚未迁移的英文段落，按需由子 Agent 按编号补录（流程见 AGENTS.md 与 `Docs/AI/PROJECT_CONTEXT.en.md` 的 DOCS-READ/DOCS-ROUTES）。

> 本文件保存从 CURRENT_TASK、TEST_REPORT 迁移的查询正文及其原始结论/未确认项。这里的历史方案不表示已实施；修改产生的正式文档在原执行入口保留短追溯。

## [QUERY][任务][TASK-SYSTEM-DESIGN-20261023] RO 任务系统设计证据，2026-10-23

- 用户要求参照 RO 为 LYFMMORGP 做任务系统设计，但不做实现。静态证据在 `_temp/task-system-design-20261023/lookup.md`；正式设计在 `项目分析/任务系统设计-RO对照与接入方案-2026-10-23.md`。本次查询只确立事实，未编译、未运行 Unity 与服务端，也未做玩法验收。
- RO 侧：`Achievable` 是已接取任务目标在客户端的投影，`Finished` 由服务端权威判定。可复用其对角色任务聚合体与静态定义的分离、由服务端判定目标与奖励、全量加增量同步、任务窗口与快捷追踪共用同一份客户端数据源的做法；不要移植 Lua/MoonClient、其庞大的 target/trigger 层级，以及客户端可信的 NPC 与位置上报。
- 目标项目：服务端 `#QuestInfo.xlsx` 与生成物 `QuestInfo/TbQuestInfo` 已存在，但没有业务调用方，且只支持单目标、单奖励。`UI/Prefabs` 下的任务资源可用；已核查的十个任务预制体没有旧 RO 脚本 GUID。`Task.prefab` 与 QuickTaskPanel 应当做绑定，而不是重建。
- 由源码核查确立的缺口：没有任务角色组件、协议、数据库任务表、存档 proto 字段、处理器，也没有客户端任务 Ctrl 与窗口；服务端没有 NPC 交互，也没有击杀与死亡推进；`OnlineRole.GetItemCount` 返回零。现有 AddItem 与带版本号的数量存档事务是可复用的边界，但任务状态与奖励必须先进入同一事务，才能宣称具备防止重复领奖的能力。
- 豆包会话 `38448850417874946`：运行 `59504459316410370` 与 `59495837363055618` 均在未投递工具结果的情况下停止（后者为桌面端导航或页面关闭报错），因此两者都不计为查找轮次。WorkBuddy Hub `hub-68136605-06c6-4feb-9ca3-571d7522b5a4` 第 1 轮 `run-85f883ec-1130-4913-b0b4-95cba5373c14`，原生会话 `5af3d7d9-4c42-406d-a6c7-7a8949f0dcf0`，执行了被验收通过的只读证据查找。

## [INDEX][协作][QUERY-INDEX] 查询标签用法

- 先在本文件用主题标签检索二级标题，再按唯一编号读取一段；示例：`rg -n '^## .*\[网络\]' Docs/AI/QUERY_TASKS.md`。
- CT编号来自原CURRENT_TASK，TR编号来自原TEST_REPORT，保留两类记录分别陈述目标/过程与检查证据，不合并推测结论。
- 导航、网络、NPC、技能、背包商城、界面、协作、构建、盘点、场景实体、性能、技能工具为主题；同一任务可命中多个主题。新增查询也遵循 `[QUERY][主题][唯一任务编号]`。
- 历史追加的MMORPG盘点与外部任务状态原本混在同步检查尾部，完整保留于 `[CT-453]`、`[TR-551]`，同时添加盘点/NPC/协作标签以便检索。
- 纯查询只更新本文件；如创建分析文档等正式文件，执行入口只写文件变化/文档验收与此处编号。原始完整文本及哈希见 `项目修改日志/协作记录归档/2026-10-23/README.md`。

## [QUERY][盘点][CT-001] 2026-10-23 功能完成度盘点图文版保存（纯文档，已完成）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 1—6 行；历史时点记录，不代表当前仍在执行。

- 用户要求解析`项目分析/MMORPG功能完成度与开发优先级.md`并以图文展示，随后要求保存到该参考文件同目录。已新增`项目分析/MMORPG功能完成度与开发优先级-图解.html`（单页HTML/SVG，含完成度矩阵、六大缺口、P0/P1/P2路线图、核心玩法闭环图、五步开发顺序），内容严格取自原Markdown，未改业务代码。
- 本轮文件：新增图解HTML与`项目修改日志/2026-10-23-MMORPG-COMPLETION-INFOGRAPHIC-CHANGELOG.md`，更新CURRENT_TASK、TEST_REPORT；未提交未推送（用户未要求）。无代码变更，无编译/Unity/服务器验证；盘点结论本身未重新核验。
- 下一步交接：若启动P0开发，按原盘点P0三项（双客户端联机验收、战斗最小闭环、导航边界）拆解任务；若更新盘点，需同步原Markdown与图解HTML。

## [QUERY][协作][CT-455] 2026-10-23 Qwen3 8B 本地开发子 Agent 可行性初评

- 目标：核对本机 Qwen3 8B 是否适合作为本项目开发子 Agent。仅查询与本地合成样例推理，没有授权其读写业务文件，没有修改既有豆包/WorkBuddy分工或安装接入程序。
- 本机证据：Ollama `http://127.0.0.1:11434/api/tags` 返回 `qwen3:8b`、8.2B、Q4_K_M、模型文件5225388164字节；`/api/show` 返回 completion/tools/thinking能力及40960上下文元数据。NVIDIA查询为RTX 4070 Laptop、8188MiB显存。实测显式设置4096窗口、think=false，`/api/ps` 返回context_length=4096、size_vram=5586592726字节；标称窗口不等于当前运行窗口或全窗口性能已验。
- 工具选择冒烟：合成读取请求生成正确的 `read_project_file` 调用及 `Assets/Scripts/Example.cs` 参数，198输入/26输出token、约4.17秒。没有执行该合成文件读取，也没有验证完整工具执行、回传和多轮自主工作。
- C#合成样例：正确把 `i <= values.Count` 改为 `<`，把foreach改为for并保留跳过负数；231输入/168输出token、约4.72秒。明确要求函数、参数和局部变量中文注释，但实际只给两个sum变量加注释，约束遵循未通过。未落盘、编译或进行真实项目开发；单个短样例耗时不能代表长上下文吞吐或正式开发质量。
- 初评：技术上可通过Ollama API配合文件/命令工具执行器作为外部子Agent，优先明确路径查找、日志摘要及少量文件的简单修改；代码交付仍由Codex核对并编译验收。复杂网络/线程/跨模块修改的可靠性未建立，不据此次冒烟替换正式分工。
- 接入建议：先限定文件、工具和交付格式，以4K短上下文开始，小范围试测后再评估8K窗口及真实项目任务；不把全部协作历史塞入小模型，也不承诺零Codex消耗。接入程序、工具多轮、完整项目约束、Unity/服务端编译和实际任务质量仍待验证。
- 外部资料：Ollama工具调用官方页面 `https://docs.ollama.com/capabilities/tool-calling` 可访问；Qwen官方Hugging Face模型卡请求连接失败，因此不引用未取到的卡片内容。以上能力和限制以本地API及实际输出为依据。

## [QUERY][协作][CT-454] 2026-10-23 发布新任务的上下文占用核对（已完成）

- 用户询问当前发布一个任务会使用多少上下文。本轮未创建任务、未修改业务代码或配置。
- 结论：新任务是独立对话，不会逐字继承本对话完整历史；实际输入占用由新任务任务单、系统/产品固定指令、项目 `AGENTS.md`、按需加载的协作记录和技能说明共同决定。任务单短且文件范围明确时，新增的可变输入通常只占数百 token；本项目较长的 `AGENTS.md` 与协作约束是每个新任务更主要的固定上下文来源。
- Codex 当前界面和可用接口没有提供“发布前精确 token 预估”或“单个新任务实际已用 token”的可核验读数，因此不能给出固定数值。官方 OpenAI Docs 搜索入口本轮返回 HTTP 403，未以其内容推断具体上限或计费。
- 建议：任务单保留目标、允许文件、已知证据、验收条件和必要交接，避免粘贴完整历史；本项目既有“短交接、新建独立对话”的规则可避免历史对话无限累积。

## [QUERY][NPC][协作][CT-006] 2026-10-22 用户澄清：角色走过去但NPC对话不显示，继续定位

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 30—35 行；历史时点记录，不代表当前仍在执行。

- 用户确认点击NPC后角色会走过去，当前重点转为导航到达回调和窗口显隐。上一轮只修已确认文字绑定，不当作面板不弹的最终根因。
- 沿豆包原本项目local会话38448648846393602续查，run59396782753161730；分支codex/npc-dialog-fix、基线32a7fa7。当前仍无Unity现场进程/MCP和新日志，尚无新复现/修复通过结论。
- 同轮日志项目修改日志/2026-10-22-NPC-DIALOG-CHANGELOG.md；证据_temp/npc-dialog-20261022/arrival-followup；原9用户文件保持。

## [QUERY][NPC][协作][CT-009] 2026-10-22 NPC点击无对话：定位中

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 49—54 行；历史时点记录，不代表当前仍在执行。

- 用户反馈点击NPC无对话；先豆包官方桌面本项目只读定位（会话38448648846393602/run59391213363817218，第1轮），Codex沿路线核验后委派最小修复，最后复测。
- 当前无Unity MCP工具及可见Unity/LYF_MMO进程，Editor.log为10月21日历史输入；没有现场复现或本轮测试通过结果。保留原9用户文件，不启停用户游戏、不替热更字节、不保存场景。
- 本轮文件、事实与后续结果：项目修改日志/2026-10-22-NPC-DIALOG-CHANGELOG.md；证据_temp/npc-dialog-20261022。

## [QUERY][网络][CT-022] 2026-10-22 当前任务：网络TCP与UDP/KCP方案完成，尚未实施（历史方案轮）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 144—149 行；历史时点记录，不代表当前仍在执行。

- 用户本轮只要求参考RO给方案。推荐第一版移动11180/11181与技能11170/11171/11173同一可靠有序KCP流，其余业务TCP；后续有实测需要再拆不可靠位置UDP。保留先UpdateMove再施法与现有本地预表现，披露内网TCP和客户端终点保存并未变成服务器权威移动。
- 豆包官方桌面local只读任务NET-TRANSPORT-RO-DESIGN-20261022，会话38448607062546178，首轮59328209991283202、定向纠错59334090250597122均completed；Codex沿路线核验，纠正OnRecv继承、KCP无原生不可靠模式及循环UUID不能永久去重。没有WorkBuddy开发任务或业务修改。
- 本轮仅5文档，专题项目分析/RO_NETWORK_TCP_KCP_DESIGN.md；逐项文件与Git回执见项目修改日志/2026-10-22-NETWORK-TRANSPORT-DESIGN-CHANGELOG.md。未运行RO/Unity抓包、联机、弱网或性能测试；下一步待用户决定是否进入第一版实施，不将方案写成已开发。

## [QUERY][导航][协作][CT-027] 2026-10-22 当前任务：RO两端寻路核查完成，豆包查找对比待连接

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 179—187 行；历史时点记录，不代表当前仍在执行。

- 用户要求核实RO客户端与服务器是否均寻路，并实际比较豆包最高模型/推理与当前Codex源码查找能力。本轮只做源码分析和实验记录，不改游戏代码、资源或场景，不扩展服务端导航。
- 已查实RO客户端经MNavigationComponent→MNavigationMgr→SWIG→C++ MNavigation→Detour执行A*；服务器AI经AIMoveTo事件进入RONavigationComponent.StaticStart并实际PathFind，场景加载导航与碰撞数据。玩家上报移动另有合法点及速度/距离检查；跨地图路线另经WorldMap规划。LYF客户端已接纯C#算法，Common同源编译8源，服务端导航组件为空且正式移动直接保存/广播上报终点，尚未接RO同等运行寻路和移动结算。
- 实验SEARCH-RO-NAV-20261022已冻结三题、同范围和15项证据标准；Codex初稿178.604秒、SHA256 84B3ADB8D062464879C24F4136EFDE0023D7201D185B3276F803437D6C763630。Codex已有项目先验且计时前读过部分RO源码，非盲测，不据此推广纯模型速度或能力差距；当前会话具体型号/推理未从运行元数据独立核实。
- 豆包官方桌面进程存在，但9225连接available=false/fetch failed。未读到当前可用模型和最高配置，未派发、无conversationId/runId、无初稿，不算模型失败得分。已请求用户保存并托盘退出，等待确认后沿既有启动入口恢复；不强杀、不改用网页/headless或低配置代替。
- 修改范围仅5文档：新增项目分析/RO_NAVIGATION_CLIENT_SERVER_SEARCH_COMPARISON.md及本轮根日志，更新项目分析/README.md和根CURRENT_TASK.md、TEST_REPORT.md。源码证据和原始实验材料在_temp/source-search-comparison-20261022，初稿保持冻结。
- 下一步：确认退出后核本项目local环境、实际最高模型与max推理，派同题只读工作任务并保存真实配置/耗时；双方初稿冻结后回查15项评分。当前两端问题已核查，对比实验尚未完成。Git结果追记本轮根日志；不操作main，保留已有用户改动。

## [QUERY][导航][CT-052] 2026-10-20 本项目导航资源现状核查完成

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 421—426 行；历史时点记录，不代表当前仍在执行。

- 用户问.nav来源与本项目是否具备。只读磁盘包含忽略/隐藏文件并排除缓存和_temp，未查到.nav；主城及副本场景m_NavMeshData=0。NavMeshAreas是配置，动作编辑器NavMeshAgentBackend是位移适配，不计作地图资源或已接入寻路。
- 结果补入项目分析/RO_PATHFINDING_ANALYSIS.md第5.2节。当前正式业务仍无真正算路；未改业务、生成导航资源或调用Unity。RO原始几何导出器仍未定位，后续具体技术方案需用户明确实现需求后再落地。
- 正式4文档：分析正文、CURRENT_TASK、TEST_REPORT和项目修改日志/2026-10-20-PROJECT-NAVIGATION-ASSETS-CHANGELOG.md。证据_temp/project-navigation-assets-20261020/validation.json。

## [QUERY][性能][CT-061] 2026-10-20 RO游戏内帧率显示核查完成

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 494—498 行；历史时点记录，不代表当前仍在执行。

- Reporter运行时日志面板带FPS并已挂GameEntry，默认show=0；另有MyFPSCounter/MyFPSDisplay临时统计显示组件，旧FPS_Panel创建入口已注释。已核源码/meta/场景/Lua绑定，未启动RO或移植功能。
- 结果见项目分析/RO_FPS_DISPLAY.md；日志项目修改日志/2026-10-20-RO-FPS-DISPLAY-CHANGELOG.md。实际发行包入口未验，后续仅在用户要求后接入本项目。

## [QUERY][盘点][CT-066] 2026-10-15 开源 MMORPG 七项目源码解析（图文详解）：已完成

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 537—555 行；历史时点记录，不代表当前仍在执行。

- 用户要求对 F 盘 `F:\MMORPG_OpenSource\` 下 7 个开源 MMORPG 项目（ET、FishMMO、UnityMMO、NoahGameFrame、OpenCoreMMO、ServUO、Intersect-Engine）做“超级详细、图文并茂（Mermaid + PNG）”的中文源码解析；在 `项目分析/开源MMORPG项目解析/` 建总索引 README 与每项目同名文件夹（主文档 `<项目>-源码解析.md` + `assets/`）；所有中间草稿/脚本/截图放 `_temp/opensource-mmo-analysis-20261015/`。追加硬性要求：第一轮 7 篇完成后由未参与写作的复查角色逐项目查漏，再由原作者补漏，最后复跑校验。
- 产出目录：`项目分析/开源MMORPG项目解析/`（总 README + 7 个项目文件夹，共 7 篇主文档 + 15 张 PNG）。
- 最终硬指标（行数 / Mermaid 图 / PNG / 不重复源码引用）：
  - ET 1758 / 9 / 2 / 78；FishMMO 991 / 8 / 2 / 172；UnityMMO 1187 / 9 / 3 / 82；NoahGameFrame 1039 / 10 / 2 / 77；OpenCoreMMO 833 / 8 / 2 / 52；ServUO 1471 / 10 / 2 / 66；Intersect-Engine 1220 / 9 / 2 / 63。
- 质量流程：每项目独立完成“源码扫描 → 官方文档读取 → 关键代码取证 → 图表生成 → 文档撰写”；再由 5 个未参与写作的独立复查 Agent（A~E）按 7 维度（目录覆盖、关键系统、构建步骤、图表、代码路径真实性、结论证据、借鉴点与面试问答）复查；原作者按遗漏清单补漏；最终用自动校验脚本复跑。
- 复查/补漏中纠正的关键事实（举例）：
  - ET：尾部“假结尾”拼接事故与断裂的 §23 借鉴表修复；自研 cn.etetet.* 包为 56 个（Packages 共 61 目录含 com.unity 官方包）；Entity 注解为 `[MemoryPackable(NoGenerate)]+[BsonIgnore]`；补 truesync 定点数学库。
  - FishMMO：怪物 AI 实为 NavMeshAgent + 9 状态 FSM（非“未确认”）；Redis 工厂已接线但无业务调用；伤害为 ScriptableObject 固定 DamageAmount；补任务三层模型/对话树。
  - OpenCoreMMO：DI 容器实为 Microsoft ServiceCollection（非 Autofac，Autofac 包仅声明未启用）；模块 15 个；Dispatcher 30s CTS 未传 token、真实防卡靠 try/catch。
  - ServUO：包表 0x0A 是 Edit、EquipReq 在 0x13；0x73 PingReq 的 ingame=false；补 Party 组队、Targeting 系统；Scripts 顶层目录 17 个。
  - Intersect：聊天频道表重写；PacketHandler 服务端 3202 行/67 方法；网络为 RSA-2048+AesGcm（4096 仅 Web JWT）；迁移分 schema 约 394（Sqlite 196 + MySql 198，按 Game/Player/Logging 三 DbContext）+ 数据迁移 5 个；删约 100 行注水克隆段。
- 验收：7 篇代码围栏全配对、图片引用全部存在、Mermaid 数量充足（8~10）；总 README 14 个相对链接全部有效；最终校验仍报“缺失”的仅 8 个引用，均为运行时生成文件（logging.json/appsetting.json/update.json/version.client.json）或未拉取子模块（skynet.lua/json.lua/config_scene/bufferevent.h），文档已说明，无凭空捏造路径。
- 边界：7 个项目全部仅静态源码走读 + 官方文档核对，未编译/未运行（各项目缺 Unity/.NET/数据库/C++ 等环境，已在每篇文档显著位置诚实标注）。
- 未修改 `F:\MMORPG_OpenSource` 下任何源码，未改 D 盘业务代码；分析脚本放 `_temp` 且中文注释。
- 日志：`项目修改日志/2026-10-15-OPENSOURCE-MMO-ANALYSIS-CHANGELOG.md`。
- 下一步交接：用户按 README 第六节学习路线阅读；如需把某项目的具体机制（如 Fiber、AOI、包表、ECA）落到 LYFMMORGP，需单独立项并走开发/测试流程。

## [QUERY][综合][CT-087] 2026-10-13 RO 目标选择流程（只读完成）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 751—756 行；历史时点记录，不代表当前仍在执行。

- 已核点击射线→碰撞体name的UID→场景实体→Player.Target；瞄准按钮循环/技能自动选候选；技能按敌友/自身/死亡友方要求筛选，再交导航/发包Target.UID。
- 目标UI与脚下特效读取当前实体；地面点另走Terrain射线。未确认键盘Tab绑定、未移植本项目或实测RO场景。
- 分析 项目分析/RO_TARGET_SELECTION_FLOW.md；日志 项目修改日志/2026-10-13-RO-TARGET-SELECTION-ANALYSIS-CHANGELOG.md。

## [QUERY][技能][CT-088] 2026-10-13 RO 气墙施法检查用途（只读完成）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 757—761 行；历史时点记录，不代表当前仍在执行。

- CheckWall第三参数true为技能规则，对登记气墙检查IsCrossAtk；阻挡则不发技能请求。不是角色穿模修正或所有建筑Collider射线检测。
- 只读实现与矩形/圆形组件核对，业务未改；分析 项目分析/RO_AIR_WALL_SKILL_CHECK.md，日志 项目修改日志/2026-10-13-RO-AIR-WALL-ANALYSIS-CHANGELOG.md。

## [QUERY][技能][CT-089] 2026-10-13 RO MSkillComboTableConfig 用途核对（只读完成）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 762—767 行；历史时点记录，不代表当前仍在执行。

- 已读取类全体与导航/技能组件/UI/AI调用：前技能＋当前技能＋配置level匹配，检查有效秒窗口与Lua条件，解析替换效果，判断起手/接招资格与连招UI提示。
- GetComboReplaceSkillID实际返回SkillEffectTable效果ID；前技能与ComboTime来自服务器连招通知及重连同步。未改业务或移植功能，不作编译/运行验收。
- 分析 项目分析/RO_SKILL_COMBO_TABLE_CONFIG.md；日志 项目修改日志/2026-10-13-RO-SKILL-COMBO-ANALYSIS-CHANGELOG.md。

## [QUERY][技能][CT-090] 2026-10-13 RO 施法发送与服务器确认语义核对（只读完成）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 768—773 行；历史时点记录，不代表当前仍在执行。

- 用户引用外部 RO MNavigationComponent.OnAfterAction 的“无服务器确认下空放”注释，已核真实 MNetClient.Send 与 MSocket.Send：返回true表示本地已接受并入发送队列，不是服务器确认收到或准许施法。
- RO OnAfterAction 在 Send 返回true后立即启动本地吟唱/瞬发预表现；IsWaitingForServer 标记与本地预表现同时存在，不是预表现前的确认等待。
- 原注释措辞不准确，建议为“施法请求成功提交到本地发送队列后立即启动预表现，不等待服务器确认”。本轮未修改参考或本项目业务源码，仅记录核对结论；范围与证据见 项目修改日志/2026-10-13-RO-SEND-CONFIRM-SEMANTICS-CHANGELOG.md。

## [QUERY][技能][CT-126] 2026-10-10 技能编辑器图文解析文档（只读代码分析 + HTML 文档新增）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 1097—1109 行；历史时点记录，不代表当前仍在执行。

- 任务目标：检查本项目技能编辑器，总结使用方法，产出一份自包含 HTML 图文解析文档，包含详细代码接口解析；建议交付路径 `项目分析/SKILL_EDITOR_GRAPHIC_GUIDE.html`。
- 验收标准：覆盖五部分架构与程序集关系、窗口界面标注、全部 Timeline 轨道与 Clip（位移/碰撞/飞行物/音效/镜头/流程等不得遗漏）、SkillConfigSO 等数据资产字段、校验导出链路、运行时接口签名、施法规则配置接口、端到端使用教程、按模块分组的接口手册；至少六张高保真图（架构图、界面布局标注图、轨道体系图、导出时序图、运行时播放流程图、规则数据流图）；单文件、资源内嵌、断网可开、全中文；每个接口标注源码相对项目根目录路径。
- 修改范围：只读分析，禁止修改任何业务代码、配置资产、场景、预制体与包源码；仅允许新建交付 HTML、协作记录与 `项目修改日志/` 变更记录。
- 执行方式：主 Agent 拆分为三个并行只读研读子 Agent（A 编辑器窗口与轨道 / B 嵌入包Runtime与宿主导出链路 / C 运行时技能体系与施法规则），研读成果回收后统一组装 HTML，浏览器自检后交付。
- 当前状态：已完成（2026-10-10）。
- 完成情况：三个并行只读研读子 Agent 逐文件读完嵌入包 Runtime（`LYF_MMO/Packages/com.ethan.act-action-editor`）、包编辑器镜像与宿主桥接（`LYF_MMO/Assets/GM_Game/Editor/Skill`）、运行时技能（`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill`）、技能配置资产目录与独立验证程序集；主 Agent 统一组装为单文件 HTML `项目分析/SKILL_EDITOR_GRAPHIC_GUIDE.html`（11 章、9 张内联 SVG、约 260KB），含架构图、界面布局标注图、轨道 Clip 体系图、导出时序图、运行时播放流程图、规则数据流图与教程流水线图。
- 验收结果：shot.py 自检桌面端 consoleErrors/resourceErrors/horizontalOverflow 均为空、无挤压列；全部图渲染正常、代码块语法高亮正确、表格字段完整；单文件资源内嵌、无外网 CDN、断网可开；接口解析均标注源码相对项目根目录路径。移动端表格/代码块横向滚动、图表占满宽度（窄屏首行字段折行为已知外观取舍）。
- 未修改代码声明：全程只读，未改动任何业务代码、配置资产、场景、预制体与包源码；仅新增交付 HTML、本协作记录与 `项目修改日志/` 变更记录。
- 未解决问题：无（源码层面疑点如 jumpList 差一、NavMesh 空实现、MoveCasting 服务端不校验等均已在文档中以提示框注明）。
- 下一步交接：无；后续如需增补章节或随源码迭代更新文档，复用本次研读成果与片段即可。

## [QUERY][背包商城][CT-168] 2026-09-06 背包延迟持久化设计方案（只读分析�?

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 1263—1264 行；历史时点记录，不代表当前仍在执行。
- 本次仅完成设计，不修改业务代码、协议或数据库�?- 设计目标：`BagTransition` 提交后立即更�?GameServer 权威内存并同步客户端；CenterServer 持久化由角色脏数据队列按固定帧批量提交�?- 结构：`BagTransition` 负责事务，`BagChangeSet` 记录变更，`RoleBagDirtyState` 聚合角色脏状态，`BagPersistenceQueue` 负责队列，`BagPersistenceManager.Update` 负责限量刷盘、重试和退出前强制刷盘�?- 约束：同一角色的变更必须按版本顺序提交；CenterServer 只接受版本不落后的快�?增量；保存失败不能回�?GameServer 已确认的业务结果，必须进入重试或告警状态�?

## [QUERY][背包商城][CT-169] 2026-09-06 背包系统总体设计方案（只读分析）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 1265—1267 行；历史时点记录，不代表当前仍在执行。

- 本次仅完成架构方案，不修改客户端、服务端、协议或数据库代码�?- 设计结论：GameServer 持有背包权威运行时状态；CenterServer 负责持久化；GateServer 负责连接转发和协议边界；Unity 只维护显示投影�?- 核心模型：角色组件持有四类可扩容容器（`RolePackPlain`、装备、消耗品、材料），容器以物品实例 UID 为主键、格位为独立索引，ItemID 只用于配置查询和堆叠统计�?- 后续落地顺序：先统一物品/容器模型，再实现事务服务和单协议处理器，随后接入 Center 持久化与增量同步，最后改造客户端窗口和专项测试�?- 当前边界：ItemUID 已使用运行时 `long`，数据库�?protobuf �?UID 类型迁移必须作为独立任务，禁止在背包改造中隐式截断�?

## [QUERY][协作][CT-191] Agent 职责配置位置核对�?026-09-04�?

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 1338—1339 行；历史时点记录，不代表当前仍在执行。
- 需求：确认项目�?Agent 的职责与配置文件位置�?- 检查范围：`AGENTS.md`、`Docs/AI`、项�?`.codex` 目录�?Agent 相关文件名扫描�?- 结论：当前没有按产品、策划、客户端、后端、测试分别保存的独立 Agent 配置文件；职责定义集中在根目�?`AGENTS.md` 的“角色分工”。项目长期协作规则和上下文位�?`Docs/AI`。Codex 会话内临时运行的�?Agent 不写入项目文件，其状态由 Codex 应用管理�?- 当前状态：已完成，只读检查，未修改业务代码�?- 下一步交接：如需让职责可单独编辑和版本管理，可新�?`Docs/AI/AGENT_ROLES.md`，按五类角色拆分职责、输入、输出和验收标准�?

## [QUERY][协作][CT-193] AIconfig Agent 模板解析�?026-09-04�?

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 1342—1343 行；历史时点记录，不代表当前仍在执行。
- 需求：解析项目根目�?`AIconfig`，确认模板的配置结构、角色职责、工作流和与当前项目的差异�?- 检查范围：`AIconfig/config.toml`、`AIconfig/workflow.md`、`AIconfig/agent/architect.md`、`code_reviewer.md`、`game_server.md`、`unity_client.md`�?- 结论：模板由全局配置、协作工作流�?4 个角色定义组成；架构师负责设计不写码，客户端和服务端负责实现，代码审查员只审查不改码。它定义了先规划、并行开发、审查、打回修复、复审、集成验证的流程�?- 与当前项目的主要偏差：模板写的是 Unity 2022.3�?NET 8、`Shared/Protocol`、DOTS-ECS/URP/MemoryPack；当前项目实际是 Unity 2023.2.20f1c1�?NET Framework 4.8、`LYF_Server/ProtoBuf/proto`、YooAsset/UniRx/HybridCLR/Google.Protobuf。模板目标平台为 PC/Android，但当前平台未在协作文档中确认�?- 角色覆盖偏差：模板只�?4 类角色，没有单独的产品经理和测试 Agent；架构师承担需求设计，代码审查员承担质检，但这不等价于项目现行的产品经理、策划、测试职责�?- 生效说明：`AIconfig` 当前是项目内模板文件；本项目当前实际强制规则仍以根目�?`AGENTS.md` �?`Docs/AI` 为准。除非后续将其接�?Codex/Agent 启动流程，否则修�?`AIconfig` 不会自动改变当前 Agent 行为�?- 当前状态：已完成，只读解析，未修改 `AIconfig` 或业务代码�?- 下一步交接：如要正式启用模板，应先按当前项目实际版本、目录和协议路径修订，再决定是否增加产品经理、策划、测试角色，并明确它�?`AGENTS.md` 的优先级�?

## [QUERY][背包商城][CT-244] 2026-09-05 RItem �?ItemBase 的物品基础模型设计（只读解析）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 1551—1553 行；历史时点记录，不代表当前仍在执行。

- 需求：参�?`F:/UnityPro/ro/Ro_cs422nb/roserver/gameserver/ro_item/ro_item.h/.cpp` �?`RItem`，为本项目定义可落地�?`ItemBase` 基类；本条仅完成设计与拆分，不修改业务源码�?- 解析范围：参考项目的 `RItem`、`RoItemContainer`，以及本项目 `MMoRpgCommon/Item/ItemBase.cs`、`ItemDesc.cs`、`ItemFactory.cs`、`GameServer/Service/item/RoItemContainer.cs`、`RoItemComponent.cs`�?- 关键结论：参考项目把“实�?UID”和“物品配�?ID”严格分开。容器唯一主索引为 `ItemUID`，按配置统计才使�?`ItemTypeID`；位置查询分别返�?UID 与配�?ID。当前项�?`RoItemContainer` 的字典、重复判断、移出和位置查询均错误地把配�?ID 当作 UID，必须随 `ItemBase` 改造一起修正�?- `RItem` 的可借鉴职责：持有实例身份、配�?ID、数量、状态位、货币与价格、创�?到期时间、容器位置、扩展参数；负责创建/恢复/复制基础状态；由容器负责放入、取出、位置索引、负重和生命周期钩子；装备属性、客户端协议映射和数据库映射由派生类或边界适配器处理�?- 目标 `ItemBase` 字段：`long ItemUID`（正数、运行时唯一）、`int RoleID`、`int ItemTypeID`、`KnapsackType ContainerType`、`int SlotIndex`（空位为 `-1`）、`long Count`、`ItemBind Bind`、`int StateFlags`、`int MoneyType`、`long TotalPrice`、`long CreateTimeUtc`、`long EffectiveTimeUtc`（`0` 表示永久）、`ItemExtraData ExtraData`。扩展键值和装备词条不塞入基础类，分别由独立的扩展数据类型�?`EquipBase` 承担�?- 目标公开 API：`CreateNew` 由工厂调�?`UIDHelper.CreateUID(UIDType.UID_Item)` 创建；`Restore` 用持久化记录恢复且只允许恢复路径写入 UID；`CopyMutableStateFrom` 深拷贝数量、状态、价格、时间与扩展数据，但绝不复制 UID、所属角色、容器和格位；`SetCount`/`AddCount` 拒绝负数；`SetBind`/`IsBound`、`SetFlag`/`HasFlag`；`GetFinalExpireTime` 只计算到期结果，不直接删物品；`SetContainerLocation` 仅由 `RoItemContainer` 调用。`ItemBase` 不直接发送协议、不直接执行 SQL、不直接查询 Luban�?- UID 类型边界：参�?`RItem` �?`uint64`，当�?`UIDHelper` 返回正数 `long`，因此目标运行时 API 使用 `long ItemUID`。这与当前数据库 `int ItemUID`、背包协�?`int32 item_id` 或待讨论�?`uint32 item_uid` 不兼容；若接入真�?UID，必须另立迁移任务，把数据库升级�?`BIGINT`、protobuf 改为 `uint64 item_uid` 并同步客户端 DLL。该类型迁移不在本轮 ItemBase 改造内，禁止静默截�?UID�?- 容器索引语义：`RoItemContainer` 应同时维护只读暴露的 `Dictionary<long, ItemBase> itemsByUid` �?`Dictionary<int, long> itemUidBySlot`。`SwapIn` 校验 UID、数量、格位和容量后同时写入两索引并更新位置；`SwapOutByUID` 先执行前置钩子，再删除两索引、清空物品位置、更新负重，最后执行后置钩子；数量归零必须通过移出流程，不能直接删除字典。`GetItemUIDByPos` 返回 `ItemUID`，`GetItemItemIDByPos` 返回 `ItemTypeID`，`CountItem` 只按 `ItemTypeID` 聚合。堆叠合并还必须额外比较绑定、状态、货币类型和配置允许的最大堆叠数�?- 初始化与工厂边界：`ItemFactory` 当前�?`ItemCategory`、`IItemCategoryProvider`、`LubanItemCategoryProvider`、`ItemFactory` 定义在一个文件，违反“一类型一文件”，并且 Common 层的占位分类查询始终返回普通物品。改造时 `ItemBase` 保留�?`MMoRpgCommon`，依�?Luban 的类别查询和具体对象工厂迁入 `GameServer/Service/item/Factory`；每个顶级类型独立文件并�?`GameServer.csproj` 显式登记�?- 明确不在本轮引入：`RAttrItem` 的属性节�?属性模块、`REquip` 的精�?卡槽/套装、快捷栏映射、标�?时装/货币等专用容器、重连通知、时效轮、客户端 protobuf 直接映射、SQL 直接持久化、物品堆叠策略、负重配置和商城发放事务。这些必须在权威物品表、Luban 配置和交易事务就绪后按领域分别接入�?- 开发拆分：后端开�?Agent A 负责 `ItemBase` 与基础值对象、工厂目录和项目文件；后端开�?Agent B 负责 `RoItemContainer`/`RoItemComponent` �?UID 双索引与生命周期调整；策�?架构 Agent 先确�?UID 的数据库与协议迁移方案；代码审查 Agent 检�?UID/配置 ID 边界、中文注释、一类型一文件和无隐式截断；测�?Agent 执行 Common、GameServer 构建�?UID/格位/堆叠行为测试。任何一项审查不通过必须返工后再验收�?- 当前状态：设计完成，未修改业务源码，未执行构建；后续开发必须先由产品经理确认目�?UID 迁移时机和协议类型�?

## [QUERY][背包商城][CT-251] 2026-09-06 ItemDataListDB 延迟物品持久化机制解析（只读�?

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 1572—1573 行；历史时点记录，不代表当前仍在执行。
- 结论：参考项目的 `ItemDataListDB` 是单个物品列表的脏标记和 protobuf 数据容器，不直接发送中心服消息。`OnAddItem`、`OnDelItem` 只更新列表数量并�?`needsave_`；后续由背包/角色持久化管理器在固定帧或定时周期扫�?`NeedSave()`，调�?`Save2Db` 汇总脏数据后才发起数据库或中心服写入�?- 本次仅解析用户提供的 C++ 片段和既有背包延迟持久化设计，未修改业务代码、协议、数据库或配置；`Save2Db` 函数体未提供，无法断言其具体序列化和发送实现�?

## [QUERY][网络][CT-355] 2026-09-21 角色状态效果同步方案设计（仅分析）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 2106—2111 行；历史时点记录，不代表当前仍在执行。

- 需求：依据当前 GameServer、GateServer、客户端角色组件和消息队列架构，设计 Buff/DeBuff/控制/护盾等角色状态效果同步方案，参考逆水寒手游与一梦江湖手游的权威状态、增量同步和表现分层思路。
- 本次范围：只输出设计方案，不修改客户端、服务端、协议或配置代码。
- 核心结论：GameServer 作为唯一权威；状态按实例 ID 管理；登录/进场发送完整快照，运行中只发送新增、更新、移除增量；客户端使用服务器时间计算剩余时间；通过角色状态版本检测丢包并请求快照，不使用每个状态一次 ACK。
- 后续交接：实现前先由策划确认状态配置字段、持久化范围和叠加规则，再由后端设计协议与 RoleStatusComponent，客户端实现角色状态模型和表现组件，最后由测试覆盖断线重连、状态过期、叠层和跨模块事务。

## [QUERY][网络][CT-356] 2026-09-21 移动同步数据方案设计（仅分析）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 2112—2116 行；历史时点记录，不代表当前仍在执行。

- 需求：参考逆水寒手游和一梦江湖手游的常见移动同步体验，先明确项目需要同步的移动数据，不编写代码。
- 分析结论：客户端上行移动意图和输入序号，GameServer 以 50ms 逻辑帧权威计算位置；服务器下行带服务器帧、位置、朝向、速度、移动模式和校正原因的快照；本地角色采用预测与校正，其他角色采用插值。瞬移、击退、跳跃落地和场景切换使用离散移动事件，不依赖连续位置包。
- 后续交接：实现前确认移动模式枚举、场景坐标单位、速度配置和 TCP 下的发送频率；实现后测试本地预测、断线重连、碰撞校正、瞬移、多人旁观插值和移动作弊校验。

## [QUERY][网络][CT-358] 2026-09-21 移动同步方案审核与重设计

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 2122—2127 行；历史时点记录，不代表当前仍在执行。

- 审核 Agent 结论：上一版按每个 50ms 逻辑帧广播移动结果不通过，虽然不构成严格锁步帧同步，但会演变为按逻辑帧发送状态，网络开销和设计边界不合适。
- 最终方案：采用服务器权威的事件驱动移动段同步。客户端只在开始、停止、方向/速度模式变化和 100~150ms 心跳时发送移动意图；GameServer 仍按 50ms 校验和推进，但只在命令或权威状态变化时向 AOI 广播移动段；周期性校正为 500~1000ms，完整快照只用于进场、重连和丢包恢复。
- 关键规则：不使用公共 FrameId，不要求客户端同帧模拟；其他客户端只根据服务器下发的移动段插值/短时外推，不执行玩家 A 的输入；移动命令按角色合并最新输入，停止和强制移动事件不得丢弃。
- 本次仍未修改代码，未执行构建或运行测试。

## [QUERY][场景实体][CT-360] 2026-09-21 Unity 场景数据导出方案

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 2134—2139 行；历史时点记录，不代表当前仍在执行。

- 设计结论：不让 GameServer 读取 Unity Scene、Prefab 或 FBX；由 Unity Editor 导出服务器专用场景数据，GameServer 运行时只读取结构化二进制。
- 推荐第一版：导出 NavMesh 三角形作为可行走地面，导出标记为服务器碰撞的静态碰撞代理，加入场景边界、最大坡度、区域属性和数据版本；使用项目已有 Google.Protobuf 序列化为 `.bytes`。
- 导出流程：编辑器读取场景中的 NavMesh 与 ServerCollision 标记对象，转换为坐标、三角形、碰撞代理和元数据，写入临时目录校验后复制到服务端正式 SceneData 目录。服务器按 SceneId 加载并通过地面、可行走和碰撞查询计算权威移动。
- 未修改代码，未执行构建或运行测试。

## [QUERY][网络][盘点][CT-362] 2026-09-21 魔兽世界开源模拟器移动同步参考

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 2146—2152 行；历史时点记录，不代表当前仍在执行。

- 事实说明：魔兽世界原始客户端和服务器并不开源；可参考的是 TrinityCore、AzerothCore 等开源服务器模拟器，具体操作码会随版本不同。
- 参考结论：客户端发送移动开始/停止/方向/跳跃/转向和周期心跳，载荷包含 MovementFlags、客户端时间、位置、朝向、运输对象和坠落信息；服务器在会话逻辑线程校验并更新角色移动状态，再向 AOI 附近玩家广播移动数据。
- 服务器不运行完整刚体物理，而是使用 Map/VMap/MMap 等地图、地形和碰撞查询，在越界、速度异常、瞬移或强制位移时纠正位置；服务器控制移动可使用样条/路径数据。
- 对当前项目的借鉴：采用移动事件+心跳的状态同步，不做公共帧锁步；使用场景碰撞数据做按需校验，服务器将已确认移动状态广播给 AOI，客户端本地即时表现并按校正包修正。
- 未修改代码，未执行构建或运行测试。

## [QUERY][网络][CT-363] 2026-09-21 客户端位置快照同步简化方案

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 2153—2159 行；历史时点记录，不代表当前仍在执行。

- 设计调整：当前阶段不让 GameServer 加载 Unity 场景碰撞、NavMesh 或高度数据，采用客户端定时上报具体位置的轻量同步方案。
- 流程：客户端移动时按约 100ms 上报位置、朝向、移动状态和序号；GameServer 只校验会话角色、场景一致性、时间和最大位移，接受后保存并广播给 AOI 附近玩家；客户端其他角色对快照做插值。
- 服务器不做：坡面查询、完整物理、复杂静态碰撞和每帧权威模拟。客户端负责本地地形表现，服务器仅保留基础异常保护；停止移动和场景切换立即发送快照。
- 风险：客户端可伪造位置，不能直接用于严格 PvP、攻击距离和反作弊结算。后续需要时只对战斗区域或关键技能增加服务器场景校验，不改变普通移动同步主流程。
- 未修改代码，未执行构建或运行测试。

## [QUERY][技能][CT-366] 2026-09-21 客户端跳跃功能设计（仅方案，未改代码）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 2172—2184 行；历史时点记录，不代表当前仍在执行。

- 需求：为当前客户端设计本地角色一段跳、二段跳、三段跳；本轮不实现代码、不处理网络同步。
- 方案：采用显式垂直初速度 + 重力积分 + CharacterController.Move 的运动学模型，不使用 Rigidbody 或固定时长曲线。
- 结构：正式位移权威归 PlayerEntity -> MEntity -> MMoveComponent -> MModel；旧 MainRoleCtrl/RoleCtrlBase 只能作为输入兼容桥，禁止再次执行重力和位移。
- 多段跳：以 JumpCount 和 MaxJumpCount（1/2/3）控制起跳次数，每次空中起跳重置该段垂直初速度；落地后次数清零。
- 辅助机制：首跳支持约 0.1~0.15 秒土狼时间和跳跃输入缓存；二段、三段跳不消耗土狼时间。
- 动画：第一阶段使用 JumpStarted/JumpEnded 与 MMoveComponent.IsJump，暂不强制新增共享 StateDefine.kStateJump；未来资源稳定后再加入独立跳跃动作组件。
- 设计文档：项目分析/JUMP_DESIGN.md。
- 审核：jump_design_review 已审核通过，确认方案适配现有 CharacterController，并指出需避免旧/新两套运动入口重复 Move。
- 当前状态：设计完成，未修改客户端、服务端、协议或配置代码；未执行编译和运行测试。
- 下一步：若开始实现，先由客户端 Agent 编写，再由代码审核 Agent 检查注释、单一位移入口和多段跳边界，最后由测试 Agent 验收 1/2/3 段跳、落地重置、土狼时间和输入缓存。

## [QUERY][网络][技能][CT-367] 2026-09-21 跳跃网络同步设计（仅方案，未改代码）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 2185—2193 行；历史时点记录，不代表当前仍在执行。

- 设计文档：项目分析/JUMP_SYNC_DESIGN.md。
- 基线：复用移动同步的客户端预测、GameServer 轻量校验、AOI 广播和 300ms 普通快照。
- 关键事件：起跳、二段跳、三段跳、落地、撞头、击退/拉回/传送使用不可合并的可靠事件。
- 序号：客户端 MoveSeq，服务器 MovementVersion，一轮空中 AirborneSequence，每次起跳 JumpId 和 JumpCount。
- 审核结论：jump_sync_design_review 通过；方案不是帧同步，不要求 GameServer 运行 Unity 场景物理。
- 当前状态：设计完成，未修改协议、客户端或服务端代码。

## [QUERY][综合][CT-375] 2026-09-24 Special 状态误触发原因分析（只读）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 2252—2259 行；历史时点记录，不代表当前仍在执行。

- 用户现象：按跳跃/特殊操作按键后动作状态显示为 `Special`，用户认为无对应动画时不应切到 Special。
- 分析结论：`RoleCtrlBase.RouteStateToEntity` 只显式处理 Idle、Run/FastRun、Attck、Hit，其余状态全部派发 `SpecialActionStarted`。当前 `RoleState` 枚举没有 Special，但包含 Jump、Slider；因此 Jump/Slider 都会误进入 `MSpecialComponent`。动画缺失检查发生在状态切换之后，`MActionComponent.PlayAnim` 只将动画播放名改为 Idle，不会把 `MStateMachine._current` 改回默认状态。
- 设计结论：状态路由必须显式处理 Jump/Slider，未知状态不得作为 Special 的兜底；特殊动作必须由明确的特殊动作请求触发。动画配置/资源未就绪时应在允许切状态前判定，拒绝该动作或等待资源，不能先进入错误状态再只替换视觉动画。
- 修改范围：只读检查 `RoleCtrlBase.cs`、`EnumDefine.cs`、`MainRoleCtrl.cs`、`MSpecialComponent.cs`、`MActionComponent.cs`、`MStateMachine.cs`；未修改业务代码。
- 下一步：用户确认修复时，由客户端 Agent 修改状态路由和状态切入前动画可用性门控，并覆盖 Jump、Slider、未知状态及真正 Special 的测试。

## [QUERY][网络][CT-393] 2026-09-26 移动同步与状态同步方案复核

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 2422—2429 行；历史时点记录，不代表当前仍在执行。

- 当前任务：结合 LYFMMORGP 真实源码审查并修订 `项目分析/移动同步与状态同步设计.md`，不修改业务代码。
- 完成情况：已新增第 0 节“项目现状复核与强制修订”，结论为方向有条件可行、旧方案不能直接落地。
- 关键修订：补齐 `SceneManager -> EntityWorld` 生命周期前置、复合会话到 `SceneUid` 的身份链、Gate 双向路由；移动改为边沿即时加 150~200ms 快照；校验改为服务器单调时间与滑动窗口；补充三段跳、状态序号、双向 AOI、按需全量恢复和项目协议字段。
- 风险处理：第 6 节原参考代码与当前接口不一致，已标记为“历史参考代码（已弃用，禁止直接实现）”。
- 修改日志：`项目修改日志/2026-09-26-MOVE-STATE-SYNC-DESIGN-REVIEW-CHANGELOG.md`。
- 下一步：若开始编码，必须先实施 P0 世界生命周期、身份、协议和 Gate 接线，再做单场景基础移动链；业务代码须委派对应开发子 Agent。

## [QUERY][技能][CT-410] 2026-10-02 RO 技能编辑器参考调查（完成）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 2584—2590 行；历史时点记录，不代表当前仍在执行。

- 目标：核对 RO 技能编辑器如何组织数据、导出运行配置与接入预览；本轮仅参考分析，不改业务代码、表格或资源。
- 交付：`项目分析/RO_SKILL_EDITOR_REFERENCE.md`；对应日志 `项目修改日志/2026-10-02-RO-SKILL-EDITOR-REFERENCE-CHANGELOG.md`。
- 验收：核对两份同名 `.config`/`.txt` 样本、`.bytes` 存在、`pbl_skill.proto`、C# `.bytes` 加载和 C++ 技能预览注入接口；明确没有找到窗口、时间轴交互或导出源码，不能据此声称已还原完整编辑器。
- 修改文件：上述分析文档和日志，以及 `Docs/AI/CURRENT_TASK.md`、`Docs/AI/TEST_REPORT.md`。未解决问题是 RO 编辑器 UI 和导出实现源码缺失；下一步如需本项目编辑器，须先确认功能范围，并继续保留原表和代码。

## [QUERY][综合][CT-411] 2026-10-07 YCombatEditor 融合设计（方案完成，实施未开始）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 2591—2597 行；历史时点记录，不代表当前仍在执行。

- 目标：评估 `D:/unitypro/JNBJ/YCombatEditor` 能否用于本项目技能编辑，并给出保留原表和现有施法链、尽量沿用 Y 类/函数名的方案。用户要求子 Agent 独立检查，不通过则返工。
- 修改文件：`项目分析/YCOMBAT_EDITOR_INTEGRATION_DESIGN.md`、`项目修改日志/2026-10-07-YCOMBAT-EDITOR-DESIGN-CHANGELOG.md`、`Docs/AI/CURRENT_TASK.md`、`Docs/AI/TEST_REPORT.md`。未修改客户端/服务端业务代码、表格、协议或资源。
- 结果：两个调查子 Agent 分别核对 Y 编辑器源码与本项目技能链；独立审查子 Agent 多轮打回并完成复审，最终方案通过静态审查。Y `SkillEditorWindow`、`SkillConfigSO` 等名称保留为第三方包；首期仅导出完整单段动画和角色根粒子，现有 `MSkill`/服务器权威逻辑保留，10070 继续原特效事件。完整映射、资源生命周期、字段白名单和验收阶段见分析文档。
- 未解决与交接：Y 包最低 Unity 6000.0，本项目为 2023.2.20f1c1；尚未做隔离兼容性编译、编辑器预览、YooAsset 构建或 Game View。下一步先执行文档阶段 0 兼容性门禁，成功后再委派客户端开发和独立测试审查；失败则回到方案选择，不直接试装正式项目。

## [QUERY][技能][CT-416] 2026-10-08 当前技能动画与特效播放链只读核对

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 2664—2671 行；历史时点记录，不代表当前仍在执行。

- 请求：说明当前技能如何播放动画及特效；本轮仅核对源码/配置，没有修改业务或资源。
- 范围：SkillCtrl.OnSkillKey/RequestAuthoritativeCast、MSkillComponent.BeginPrediction/OnStart/PlayAnim、MStateMachine.LateUpdate、MSkill.Fire/TriggerAnim、MArtSkill.Start/Update/PlayRuntimeEffect/TryPlayLegacyEffect、SkillRuntimeFxLifetime，以及七份 Runtime 配置。
- 结论：按键校验后以 skill_uuid 发请求并本地立即预表现；统一状态机入口安装并播放 Skill 槽。表现特效由 MArtSkill 按独立施法经过秒数派发，编辑帧导出为 TimeSeconds；冷配置整次沿用旧剪辑事件，10070保留旧蓄力/落剑事件。Q第一段当前无新配置特效，第二段23帧约0.1917秒，第三段23/54帧约0.1917/0.45秒。特效尾粒结束销毁并释放资源引用。
- 验证：只读函数调用与配置交叉核对；没有运行画面或新编译测试，不扩大为运行验收。上一轮连续Q修复的真实画面验收仍待Unity正常编译后复验。
- 记录：项目修改日志/2026-10-08-Q-COMBO-GROUND-CHANGELOG.md。下一步：将核对结果直接说明给用户。

## [QUERY][综合][CT-417] 2026-10-08 旧 AnimationEvent 消费链用途核对

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 2672—2678 行；历史时点记录，不代表当前仍在执行。

- 请求：解释 MArtSkill 中读取 clip.events 的循环，核对新 RO 实现是否需要此路径。
- 范围与结论：只读核对 RO MArtSkill.fxListEvents（237起）、EArtsSkillTimerCb.EFx分派、fx，以及本项目 MArtSkill.Start/Update 和3007001配置。RO按技能数据FxList/At向MTimerMgr注册特效回调，不读取AnimationClip.events；用户指出该循环非RO新链属实。本项目额外保留它作为旧兼容：新配置未ready/无效时走旧事件，10070运行资产PreserveLegacyFxEvents=1且FxEvents为空，所以1技能蓄力/落剑确实仍依赖该循环，不能描述为死代码。
- 状态：本轮解释与参考核对，没有修改业务/资源或执行编译。清理该链须先把10070两条特效完整导出到正式轨道，并处理配置冷加载；不能直接删除导致1技能特效消失。用户本轮为用途询问，未在此解释任务中展开配置迁移开发。
- 验证：真实参考源码和本项目配置交叉核对，无运行验证。后续如进入清理，必须委派开发并独立测试审核；保留旧动画和原表。

## [QUERY][技能][CT-424] RO技能音效实现只读对照（完成）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 2750—2755 行；历史时点记录，不代表当前仍在执行。

- 请求：解释RO音效/特效是否与当前实现类似。只读核对RO MArtSkill.FireEvents/fxListEvents/audio与MModel音频接口、MSkill.TriggerAnim，以及当前MArtSkill/运行配置/ReadFx/编辑器预览。
- 结果：时间轴配置思路相似，RO按FxList/AudioList的At注册MTimerMgr，正式音效通过MAudioMgr/FMOD实例播放并按End及释放清理；当前正式链仅导出粒子FxEvents按Update时钟播放，audioClip被ReadFx拒绝，编辑器音效预览不代表正式运行已支持。RO动画使用ClipName/SkillClips/AnimList，与当前自定义字段名不同。
- 无业务/资产改动，无编译/运行验收。下一步向用户说明事实，不自行接入音效。记录项目修改日志/2026-10-10-RO-SKILL-AUDIO-COMPARE.md。

## [QUERY][技能][CT-425] RO技能编辑时间轴内容核对（完成）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 2756—2761 行；历史时点记录，不代表当前仍在执行。

- 用户询问特效之外的轨道。核对原协议、MArtSkill.FireEvents及.config：动画/音效/位移/转向/预警/震屏/镜头/变色/技能条与命中、弹道、召唤、AI、Buff数据类别存在。
- RO窗口源码仍未找到，不能断言真实UI名称或独立轨道数量；弹道嵌套ResultList.LongAttackData，组合技能为配置列表，不能凭运行字段称其独立轨道。只读结论已补项目分析/RO_SKILL_EDITOR_REFERENCE.md，修改记录项目修改日志/2026-10-10-RO-SKILL-TRACK-COMPARE.md。
- 无业务/配置修改，无编译/运行测试，下一步直接解释给用户。

## [QUERY][技能][CT-447] 2026-10-11 RO特效结束时间只读核对

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 3028—3032 行；历史时点记录，不代表当前仍在执行。

- 状态：已完成当前事实询问。源码MSkillFxData实际有At/End/DestroyDelay秒字段，MArtSkill.playFx将(End-At+DestroyDelay)*_timeScale写playTime，MFxMgr.Update正DestroyTime倒计时到零移除。当前Y结束帧仅制作显示，确有RO能力未接入差异。
- 本轮仅新增项目分析/RO_SKILL_FX_END_SEMANTICS.md、项目修改日志/2026-10-11-RO-FX-END-ANALYSIS-CHANGELOG.md及入口记录；业务、配置、资源没有修改，不自动实现或发布。后续若接入需另委派开发及独立审核。

## [QUERY][导航][网络][技能][盘点][NPC][协作][CT-453] 2026-10-22 寻路与技能配置位移网络同步检查（检查完成，双端现场未验）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/CURRENT_TASK.md`，第 3081—3098 行；历史时点记录，不代表当前仍在执行。

- 用户本轮要求只检查是否网络同步，未授权扩展修复。分支codex/feature-development，基线b04e75bd；没有修改客户端/服务器业务、协议、资源或场景。
- 导航和Charge差量都进入MMove统一提交，PlayerEntity按实际位置变化上报11180；服务器保存并向同地图100米内其他在线角色发11181，远端已有角色通过MEvent_Move消费。技能IsMoving=false仍可IsMoved=true上报。
- 远端有非零曲线差量的帧不再叠加影子追赶；当前MoveCasting=false的技能期间，超过0.1米的追赶受IsActiveMovable限制，恢复默认态再追。小误差可直接吸附。远端技能以接收执行时的时钟/当前模型朝向起播，未消费服务器开始时间，不能报告轨迹严格一致。
- 本轮当前生产方法隔离编译运行11项通过（含暂停追赶的行为复现）；不是业务修复验收或真实联机验收。证据_temp/navigation-root-sync-20261022/；现场双客户端、弱网、原生墙体与部署版本生效未验。没有因本轮检查启停Unity或服务。
- 正式文件：新增项目分析/NAVIGATION_CHARGE_NETWORK_SYNC_CHECK.md及项目修改日志/2026-10-22-NAVIGATION-CHARGE-SYNC-CHECK-CHANGELOG.md；修改项目分析/项目难点.md、本文件、Docs/AI/TEST_REPORT.md。提交仅这五份文档，不混入用户已有改动。
- 交接：按专题中的双账号用例观察导航/静止技能/墙体附近技能的坐标误差；用户要求修复后再明确最小范围并委派桌面开发子Agent，主Agent不代写业务。

- 本阶段豆包run59396782753161730恢复返回task_unavailable，stop确认cancelled/stopped=true，未取得报告，不计模型失败轮。已沿本项目委派WorkBuddy桌面只读续查Hub hub-1284e335-707f-4b74-8252-b84d93defbce；仍未修改新业务或取得新验收结论。
- 2026-10-23 MMORPG 功能盘点：
  - 目标：对照市面 MMORPG 检查已完成、部分完成和未开始模块，确定 P0/P1/P2 开发顺序。
  - 范围：只读核对客户端、服务端、协议注册、配置线索和既有专题报告；本轮不修改业务代码、资源、场景或协议。
  - 结论：客户端导航三角网格 A* 已存在，服务端 NavigationComponent 仍为空壳；登录/入场、移动/KCP、技能表现、NPC 对话、背包和商城有基础闭环；任务 Handler、装备/物品 Handler、服务端装备穿脱、聊天服务、怪物伤害死亡掉落、组队副本及多数社交系统未形成完整闭环。
  - 优先级：P0 正式双客户端/重连弱网/服务端权威战斗与位置；P1 任务、装备物品、怪物掉落、组队副本；P2 公会、好友、邮件、排行榜、成就、拍卖和运营后台。
  - 正文：`项目分析/MMORPG功能完成度与开发优先级.md`；逐项日志：`项目修改日志/2026-10-23-MMORPG-FEATURE-INVENTORY-CHANGELOG.md`。
  - 当前分支：`codex/npc-dialog-fix`；工作区已有用户修改保持不动。本轮下一步是完成文档验证、精确暂存、提交并推送，不合并 `main`。

## [QUERY][盘点][TR-001] 2026-10-23 功能完成度盘点图文版：文档验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1—6 行；历史时点记录，不代表当前仍在执行。

- 本轮纯文档：新增`项目分析/MMORPG功能完成度与开发优先级-图解.html`，为`项目分析/MMORPG功能完成度与开发优先级.md`的可视化版本。已确认文件落盘为完整UTF-8 HTML（含DOCTYPE、meta charset、viewport、title），无外部依赖，浏览器可直接打开。
- 内容核对：11个模块（已完成6/部分完成4/未完成1）、六大缺口、P0/P1/P2、P0验收线（双客户端同地图连续战斗30分钟等）、五步开发顺序、三条盘点限制与原Markdown一致。
- 未执行代码编译、Unity或服务器验证（无代码变更）；未对盘点结论做新一轮源码核验。逐项文件与边界见项目修改日志/2026-10-23-MMORPG-COMPLETION-INFOGRAPHIC-CHANGELOG.md。

## [QUERY][网络][TR-022] 2026-10-22 网络双通道方案：源码核验，未实施（历史方案轮）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 147—153 行；历史时点记录，不代表当前仍在执行。

- 回读RO MSocket的Stream/Tcp与统一Send入口、UdpNetwork/KcpPeer的KCP API、BaseLink public OnRecv与公有继承；gateserver范围未找到UdpNetwork业务初始化引用，不推导为线上技能UDP。RO具体施法上行与预编译网络库/实际部署未完整验证。
- 回读LYF NetServer/NetClient、NetUtils、GateServerApp、PlayerEntity.UpdateMove、MNavigationComponent施法前UpdateMove、移动/技能Handler及ROMoveComponent，确认当前TCP、完整终点采样、MP/CD权威及非服务器权威移动。
- 豆包同会话第2轮已撤回无法链接误判、KCP原生不可靠模式、循环skill_uuid永久幂等三项表述；Codex核验关键文件，非独立重做全项目查找。
- 本轮仅5文档，静态文档/差异校验与Git回执见项目修改日志/2026-10-22-NETWORK-TRANSPORT-DESIGN-CHANGELOG.md；未修改业务、协议、Network.dll或场景，无游戏编译或运行验收。弱网、跨通道创建实体顺序、绑定/重连和IL2CPP配套列为后续验收，不报告通过。

## [QUERY][导航][TR-027] 2026-10-22 RO两端寻路与源码查找实验：源码核查完成，对比未运行

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 183—191 行；历史时点记录，不代表当前仍在执行。

- 分支codex/feature-development，基线1fbe5a2bca1135aa68dcc92da4dd79bdad459acb。只修改根分析、索引、协作和日志共5文档；没有业务、资源、场景变化，不运行游戏编译或启停Unity/服务器。
- 直接核对RO客户端业务/管理器/SWIG边界、C++ MNavigation/MBaseHelperNav/Detour，以及服务器导航组件、AI事件、场景初始化和服务端编译宏、导航管理器加载、移动协议/校验和跨图规划。确认服务器存在实际PathFind调用，不将声明或客户端封装当作运行证据。
- LYF直接核对客户端PathFind的cost+heuristic、管理器StreamingAssets加载、Common.csproj的8份同源Compile、服务端空NavigationComponent及EntityFactory装配、MoveOperationHandler→ROMoveComponent的正式终点保存/广播。限定GameServer/Common源码排除bin/obj后，运行导航查询/地图加载检索无命中；不将此静态结果当运行验收。
- Codex初稿起止2026-10-22 07:33:48.4680837至07:36:47.0719779（UTC+8），178.604秒；冻结SHA256=84B3ADB8D062464879C24F4136EFDE0023D7201D185B3276F803437D6C763630。先验和计时前源码预读已披露，未取得当前会话精确运行模型元数据，不能称公平盲测或纯模型速度。
- 豆包官方CLI本轮cdp status实际available=false/127.0.0.1:9225/fetch failed，进程存在。模型列表、最高配置、本项目工作任务均未确认，无原生任务和初稿；不输出比较分数或胜负。等待用户保存并托盘退出后重启恢复，不把阻断写成执行完成。
- 文档校验：28处源码链接均存在且行号有效，4处专题引用齐全，Codex初稿哈希保持；git diff --check通过。正式专题为项目分析/RO_NAVIGATION_CLIENT_SERVER_SEARCH_COMPARISON.md，逐项文件和Git结果为项目修改日志/2026-10-22-NAVIGATION-SEARCH-COMPARISON-CHANGELOG.md。原始协议、初稿、计时和本轮检查证据在_temp/source-search-comparison-20261022；后续沿冻结题目续跑豆包。

## [QUERY][导航][TR-052] 2026-10-20 本项目.nav与导航资源核查

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 419—424 行；历史时点记录，不代表当前仍在执行。

- 磁盘rg --files --hidden --no-ignore在排除缓存/中间目录后未查到.nav；Assets文本NavMeshData类型块/NavMeshSurface未命中，主城两场景m_NavMeshData均为0。已读NavMeshAreas、包清单、动作编辑器适配、客户端/服务端导航组件与模型移动代码，区分参数/适配与真实地图导航资源。
- 静态搜索、关键输入SHA及本轮文档段编码核验见_temp/project-navigation-assets-20261020/validation.json。一次rg搜索因模式以连字符开头报参数错误，改为显式-e重跑完成；未据失败命令作结论。
- 正式只修改4份文档，无运行/编译/烘焙或业务写入。结论限当前磁盘和源码，未检查Unity内存或外部下载的运行时数据。RO首份.nav导出器仍未知，未擅自实现导出器或安装导航包。

## [QUERY][导航][TR-053] 2026-10-20 RO导航烘焙：静态源码与文档核查

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 425—430 行；历史时点记录，不代表当前仍在执行。

- 已核BatchToolMenu普通地图构建保存链、MeshLoaderObj输入字段、MBase默认值与覆盖、MBaseDynamicObstacle普通分块烘焙/写文件及工程清单；证据_temp/ro-navigation-bake-20261020/validation.json。新增正文与本轮首段单独检查，历史记录保持。
- .nav原始导出器与对象筛选规则未定位；工具exe存在但未运行，源码与二进制对应、地图最终参数及实际工具编译未验。主初看工程清单后半段误判实现未列，最终全文定位264行确认include/MBaseDynamicObstacle.cpp已是ClCompile输入，正文已修正。路径猜测Assets/Resources/NavDatas不存在，已改按实际工具传入路径描述，不据历史打包清单认定当前文件存在。
- 只改项目分析/RO_PATHFINDING_ANALYSIS.md、CURRENT_TASK、TEST_REPORT及根日志4文档，无业务或RO资源修改；纯解释任务未编译、启动Unity/工具或重新烘焙。

## [QUERY][导航][TR-054] 2026-10-20 RO寻路整理：源码与文档核查通过

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 431—438 行；历史时点记录，不代表当前仍在执行。

- 已逐项核入口到路径查询、路点推进、到达回调、目标追踪、跨图及移动同步；主核24份源码定位/SHA，独立核12份原生链文件并复核正文，无剩余实质错误。主清单初次把本项目MoveModel签名误写成void，改为实际bool后定位通过，只修临时核查标记。
- 确认Detour A*及ALL_CROSSINGS路点、同步调用、部分路径返回语义、两个PathFind重载差异、.nav文本/.bytes网格和吸附bool边界；正文按真实函数体表述。本项目当前导航未实现与RO功能分开记录，技能查询目的点和进入射程停止用0/20米、0～10米示例区分。
- 证据_temp/ro-pathfinding-analysis-20261020/main-source-manifest.json、review/source-manifest.json、review/source-excerpts.txt、review/native-findings.md及root-final-validation.json；正文项目分析/RO_PATHFINDING_ANALYSIS.md，正式修改6份文档，日志项目修改日志/2026-10-20-RO-PATHFINDING-ANALYSIS-CHANGELOG.md。
- 本轮为文档与静态源码核查，未运行Unity/RO/服务器、未编译或发布、未修改业务，不作路径成功率、耗时、GC、动态障碍或打包DLL版本一致性结论；无已复现待修故障，也不自动进入移植。
- 全文编码扫描命中协作记录旧历史段的乱码，本轮新增首段编码正常；核查范围改为实际修改段，保留首次报告，不借整理改写旧记录。

## [QUERY][性能][TR-061] 2026-10-20 RO帧率显示：只读源码验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 498—502 行；历史时点记录，不代表当前仍在执行。

- 已核Reporter.Update的frames/dt（时间大于0.25秒且帧数大于10）、面板FPS显示开关、GameEntry启用组件/show=0与Lua字段绑定；另核60帧临时计数器及其显示0～99，旧加载入口全部注释。
- 仅源码/场景静态核查，不是RO包运行验证；无业务改动或编译。专题项目分析/RO_FPS_DISPLAY.md，根日志项目修改日志/2026-10-20-RO-FPS-DISPLAY-CHANGELOG.md。

## [QUERY][盘点][TR-068] 2026-10-15 开源 MMORPG 七项目解析：文档静态验收

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 554—570 行；历史时点记录，不代表当前仍在执行。

- 范围：新建 `项目分析/开源MMORPG项目解析/`（总 README + 7 个项目文件夹，共 7 篇主文档 + 15 张 PNG）。纯文档源码分析轮，不改 `F:\MMORPG_OpenSource` 下任何源码、不改 D 盘业务代码，不编译/不启动服务/不连数据库。
- 验证方式（自动 + 人工结合）：
  - 自动校验脚本 `_temp/opensource-mmo-analysis-20261015/_shared/verify_docs.py`：逐篇校验行数、代码围栏配对、Mermaid 数量与空图、图片引用存在性、源码引用三级命中（精确路径 + 文件名兜底 + 缺失）；
  - 总索引校验脚本 `verify_readme.py`：校验 README 自身链接；
  - 主 Agent 人工目检 PNG：抽查 ServUO 双线程图、Intersect 分层图、ET Fiber 图，中文渲染与布局正常。
- 最终硬指标（行数 / Mermaid / PNG / 不重复源码引用）：
  ET 1758/9/2/78；FishMMO 991/8/2/172；UnityMMO 1187/9/3/82；NoahGameFrame 1039/10/2/77；OpenCoreMMO 833/8/2/52；ServUO 1471/10/2/66；Intersect-Engine 1220/9/2/63。
- 验收结果：
  - 7 篇代码围栏全配对、Mermaid 图 8~10 张无空图、15 张 PNG 引用全部存在；
  - 总 README 14 个相对链接全部有效；
  - 合计 590 个不重复源码引用（精确路径 + 文件名兜底），仅 8 个报“缺失”：运行时生成文件（FishMMO logging.json/appsetting.json、Intersect client/update.json/version.client.json）与未拉取子模块/vcpkg（UnityMMO skynet.lua/config_scene、NF json.lua/event2/bufferevent.h），均已在文档说明，非捏造路径。
- 复查流程：5 个未参与写作的独立复查 Agent（A~E）按 7 维度复查，原作者按遗漏清单补漏，纠正了包表错误、DI 容器、AI 形态、包数、迁移数、删注水段等（详见 README 第八节逐条记录）。
- 未验证：7 个项目全部仅静态源码走读 + 官方文档核对，未编译/未运行（各项目缺 Unity/.NET/数据库/C++ 等环境，已在每篇文档显著位置诚实标注），运行时行为均为静态推演。
- 结果：文档静态验收 PASS；无业务返工。

## [QUERY][综合][TR-086] 2026-10-13 RO 目标选择只读核对

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 733—738 行；历史时点记录，不代表当前仍在执行。

- 读取TouchObject.JudegeType/setPlayerTarget、MSkillTargetMgr.OnSelectTarget/FindEnemyByAimBtn/FindEnemyBySkillBtn/ResetTarget、MSkillTargetDefine.FindAllTarget排序与UISkillController.GetTargetFilterSkill，确认射线UID选实体、候选循环及技能目标筛选实际链。
- 区分普通点击地面寻路与技能地面选点，均有Terrain射线但用途不同。未确认输入键位绑定、未运行场景或编译，业务/配置保持。
- 分析 项目分析/RO_TARGET_SELECTION_FLOW.md；日志 项目修改日志/2026-10-13-RO-TARGET-SELECTION-ANALYSIS-CHANGELOG.md。

## [QUERY][技能][TR-087] 2026-10-13 RO 气墙CheckWall只读核对

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 739—743 行；历史时点记录，不代表当前仍在执行。

- 核MAirWallMgr.CheckWall及MSceneTriggerAirWallComponent：true选技能跨墙规则、检查登记触发墙几何与IsCrossAtk；false选移动通行规则。方法不修正身体位置，也不扫描所有建筑Collider。
- 只读源码，无业务/资源改动，不编译或运行地图测试。分析 项目分析/RO_AIR_WALL_SKILL_CHECK.md；日志 项目修改日志/2026-10-13-RO-AIR-WALL-ANALYSIS-CHANGELOG.md。

## [QUERY][技能][TR-088] 2026-10-13 RO MSkillComboTableConfig 只读核对

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 744—749 行；历史时点记录，不代表当前仍在执行。

- 完整读取MSkillComboTableConfig，并核SkillComboTable字段、OnAfterAction替换与发包、CanSkill、技能栏遮罩/提示及skillcombo/reconnect回包更新。
- 已核：EffectiveTime为秒并乘1000与UtcMillis/ComboTime比较；GetComboReplaceSkillID取EffectIDs[level-1]返回效果ID；匹配失败的连招技能是否可起手由FirstComboSkillTable判断；Use接口是资格查询，不在该类发包。
- 仅文档分析，业务/表不改，不编译或执行运行测试，未载入真实配置值。分析 项目分析/RO_SKILL_COMBO_TABLE_CONFIG.md；日志 项目修改日志/2026-10-13-RO-SKILL-COMBO-ANALYSIS-CHANGELOG.md。

## [QUERY][技能][TR-089] 2026-10-13 RO Send 返回值与施法预表现只读核对

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 750—755 行；历史时点记录，不代表当前仍在执行。

- 读取外部MoonClient/Skill/MNavigationComponent.cs:434、Network/ApplicationLayer/MNetClient.cs:108、Network/SessionLayer/MSocket.cs:189。MNetClient.Send在Socket.Send后直接return true；MSocket.Send执行_protocolQueue.Enqueue，未等待服务器回包。
- OnAfterAction在Send返回true的同一调用中播放吟唱或瞬发预表现；原注释把本地提交成功描述为服务器确认，语义不准确。仅记录与解释，没有修改业务代码，无需重编或重跑旧专项。
- 未验证真实网络送达，不以本地队列成功表示服务器已收到/允许。记录 项目修改日志/2026-10-13-RO-SEND-CONFIRM-SEMANTICS-CHANGELOG.md。

## [QUERY][技能][TR-117] 2026-10-10 技能编辑器图文解析文档（只读分析 + HTML 文档新增）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1036—1044 行；历史时点记录，不代表当前仍在执行。

- 分析范围：逐文件研读技能编辑器五部分——① 嵌入包 Runtime `LYF_MMO/Packages/com.ethan.act-action-editor/Runtime`（SkillConfigSO 与 Global 内嵌类型、ActionPlayer 播放/校验/流程/输入缓冲、位移后端与 MoveSolver、碰撞几何、交互窗、镜头/HitFeel）；② 包编辑器镜像 `LYF_MMO/Assets/GM_Game/Editor/Skill/ActionEditor`（SkillEditorWindow、Timeline、BuiltinTrackRegistry 16 轨道、Inspector、预览隔离副本、ActionEditorAgent）；③ 宿主桥接 `LYF_MMO/Assets/GM_Game/Editor/Skill`（SkillEditorAssetTools 校验/导出、SkillEditorBinding、SkillCastingRulesExportTool）；④ 运行时技能 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill`（MSkillComponent/Core/MSkill/MArtSkill、FxEvents 缓存与租约、施法入口链路）与配置资产目录 `LYF_MMO/Assets/GM_Game/Scripts/Data/Skill`；⑤ 独立验证程序集 `LYF_MMO/Assets/GM_Game/Scripts/UI/test/Editor/SkillVerification`。另核对服务端 `LYF_Server` RoleSkillComponent.TryCast 与两端 SkillCastingRules.json（逐字节一致）。
- 交付物：`项目分析/SKILL_EDITOR_GRAPHIC_GUIDE.html`，单文件、全中文、11 章、9 张内联 SVG（架构图、界面布局标注图、轨道 Clip 体系图、导出时序图、运行时播放流程图、规则数据流图、教程流水线图），固定侧边目录导航、暗色代码块与离线语法高亮；接口解析均标注源码相对项目根目录路径。
- 文档验证：使用 html skill 的 shot.py 在桌面（1440 宽）与移动（390 宽）两视口实际渲染。桌面 consoleErrors/resourceErrors/horizontalOverflow 均为空、无挤压列；目录锚点跳转正常；全部 SVG 图渲染无文字重叠（架构图箭头穿越条件编译胶囊、界面图标注压按钮等问题已修复并重新内联）；代码块语法高亮正确（修复了高亮脚本多趟替换打烂自身 span 标签的缺陷）；表格列宽完整。移动端卡片堆叠、表格与代码块横向滚动、图表占满视口宽度。
- 离线验证：CSS/JS/SVG 全部内联，无外网 CDN 与外部资源引用，断网可直接双击打开。
- 未修改代码声明：本次为只读代码分析，未改动任何业务代码、配置资产、场景、预制体与包源码；仅新增交付 HTML、Docs/AI 协作记录与 `项目修改日志/` 变更记录。
- 未运行项：未启动 Unity、未做实机截图（界面图均明确标注"界面示意图，非 Unity 实机截图"）；文档中的源码疑点（Jump/endKey 与 jumpList 差一、NavMeshAgentBackend.SetActorCollisionIgnored 空实现、causePlayerStagger Obsolete 仍序列化、SkillTypeCombined/Channelling 已定义未接入、MoveCasting 服务端不校验、MMoveComponent `#if false` 死代码等）均已在正文以提示框如实标注。

## [QUERY][协作][TR-167] 2026-09-04 AIconfig Agent 模板解析

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1503—1510 行；历史时点记录，不代表当前仍在执行。

- 检查目标：解析 `AIconfig` 模板的配置项、工作流、角色职责，并与当前项目实际配置对照。
- 检查文件：`AIconfig/config.toml`、`AIconfig/workflow.md`、`AIconfig/agent/architect.md`、`code_reviewer.md`、`game_server.md`、`unity_client.md`。
- 检查结果：确认模板包含全局策略、项目规则、质量门禁、4 个角色定义和 6 步协作流程；未发现独立产品经理或测试角色定义。
- 差异结果：模板中的 Unity 2022.3、.NET 8、`Shared/Protocol` 与当前 Unity 2023.2.20f1c1、.NET Framework 4.8、`LYF_Server/ProtoBuf/proto` 不一致；模板中的 DOTS-ECS、MemoryPack 也不是当前项目已确认依赖。
- 生效结果：确认 `AIconfig` 目前仅作为项目文件存在，当前会话实际遵循 `AGENTS.md` 和 `Docs/AI`；本次未修改模板和业务代码，未执行编译。

## [QUERY][协作][TR-168] 2026-09-04 Agent 职责配置位置核对

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1511—1517 行；历史时点记录，不代表当前仍在执行。

- 检查目标：定位项目内 Agent 职责和独立配置文件。
- 检查方式：读取 `AGENTS.md` 与 `Docs/AI`，扫描 `.codex` 和文档目录中的 Agent、TOML、JSON 配置候选文件。
- 检查结果：职责配置位于根目录 `AGENTS.md` 的“角色分工”；没有发现项目内独立的角色 Agent 配置文件。Codex 子 Agent 运行状态不属于项目文件。
- 结论：通过；本次为只读分析，不涉及源码、构建或临时产物。

## [QUERY][网络][TR-208] 2026-09-04 GameServer 协议处理器架构只读分析

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 1872—1880 行；历史时点记录，不代表当前仍在执行。

- 分析范围：`F:/UnityPro/ro/Ro_cs422nb/roserver/gameserver/ptcregister.cpp`、`protocol/item/rpcc2g_useitem.cpp`，以及当前 `GameServer/manger/ClientMessageManager.cs`、`GameServerApp.cs`、`GameGlobal.cs`、`Ctrl/Game_LoginCtrl.cs`、`Service/KnapsackService.cs`、`manger/RoleManager.cs`、`Network/Socket/com/NetDefine.cs` 和 protobuf 源文件。
- 参考结论：roserver 的 `CProtocolRegister::Regiter()` 仅登记协议实例；`RpcC2G_UseItem` 的业务在 `protocol/item/rpcc2g_useitem.cpp` 独立实现。该模式适合当前 GameServer，但必须保留现有队列和逻辑线程消费，不能在网络回调中直接调用 Handler。
- 当前消息盘点：已确认 11 个真实入站来源/命令组合，覆盖登录、创建角色、开始游戏、进入世界、角色技能回包、背包数据回包、开背包格子和持久化结果；其中同一登录命令号按 Gate/Center 来源拆成不同处理器。出站同步消息与其他服务命令不创建入站 Handler。
- 协议缺口：购买物品、使用物品、装备强化在当前 `NetDefine.cs` 和 protobuf 中均不存在；本轮设计明确禁止创建空处理类。
- 输出：新增 `项目分析/MESSAGE_HANDLER_ARCHITECTURE.md`，并在 `CURRENT_TASK.md` 登记开发范围和验收标准。
- 验证：本轮为只读架构分析与文档记录，未修改客户端/服务端业务代码，未执行构建；开发后需由测试 Agent 独立构建和验证。

## [QUERY][背包商城][TR-256] 2026-09-05 RItem 到 ItemBase 只读架构解析

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2386—2393 行；历史时点记录，不代表当前仍在执行。

- 检查类型：只读代码与静态结构对比，未修改业务源码、协议、数据库或客户端插件，未产生构建产物。
- 参考证据：`ro_item.h` 中 `RItem` 持有 `uint64_t item_uid_` 与独立 `uint32_t item_id_`；`ro_item_container.cpp` 使用 `item->GetUID()` 作为容器 `itemmap_` 主键，`GetItemUIDByPos` 与 `GetItemItemIDByPos` 分别返回实例 UID、配置 ID。
- 当前问题：`MMoRpgCommon/Item/ItemBase.cs` 含未完成 C++ 语法和私有 UID，无法满足 `ItemFactory` 赋值；`RoItemContainer.cs` 的 UID 查询、主键、重复检查、移出和按位置移出均使用 `ItemID`，将配置 ID 错当为实例 UID；`ItemFactory.cs` 同文件定义四个顶级类型，违反一类型一文件约束。
- 设计验收：已在 `CURRENT_TASK.md` 落档字段、API、初始化/复制语义、双索引容器规则、协议/数据库 UID 类型迁移边界、明确排除功能及后续五角色任务拆分。
- 未验证：本轮未构建。待实现后必须验证 Common、GameServer、UID 唯一索引、同配置多实例、格位双索引、数量归零移出、绑定差异堆叠和非法 UID 拒绝。

## [QUERY][背包商城][TR-264] 2026-09-06 ItemDataListDB 延迟物品持久化解析

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2491—2495 行；历史时点记录，不代表当前仍在执行。

- 本次为只读架构解析，未执行构建或测试，未修改任何业务文件。
- 依据用户提供的 `ItemDataListDB` 代码：新增/删除物品仅修改 `size_` 并调用 `SetNeedSave()`；该类没有网络对象、消息队列或发送调用，因此不能直接完成中心服通知。`Save2Db` 的函数体未提供，具体序列化细节无法验证。

## [QUERY][场景实体][TR-318] 2026-09-10 客户端角色组件化架构分析

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 2977—2984 行；历史时点记录，不代表当前仍在执行。

- 检查范围：客户端 `Scripts/Manger`、`Scripts/controller`、`Scripts/FunlModule/Bag` 及现有 UI MVC 结构。
- 现状：客户端同时存在进程级 Singleton Manager、UI Controller、角色 MonoBehaviour Controller，以及 `BagMgr/BagDataModel/BagApi/BagEquipMgr/BagTipsMgr/BagItemCdMgr` 等尚未落地的背包骨架；职责命名和生命周期尚未统一。
- 风险：若背包、任务、战斗、基因和 Buff 都继续新增 Singleton Manager，会产生初始化顺序、切角色残留、跨模块双向依赖和 UI 直接读取全局状态问题。
- 建议：使用 `LocalPlayerContext + RoleComponentContainer + RoleComponentBase` 管理角色级纯 C# 组件；使用 Module Handler、Application Service 和 UI MVC 分离网络、业务用例与表现；多实体战斗保留为 `CombatSystem`，不强行塞入角色组件。
- 验证状态：本轮是只读架构分析，没有修改客户端业务代码、协议、场景或预制体，因此未执行编译和运行测试。

## [QUERY][综合][TR-328] 2026-09-11 客户端脏标记机制只读分析

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3066—3072 行；历史时点记录，不代表当前仍在执行。

- 分析范围：`ClientRole`、`RoleWorldSystem`、`RoleComponentBase`、`RoleComponentContainer`、`RoItemComponent`、`RoleCountComponent`、`KnapsackCtrl`、`RoleInfoCtrl` 和 `Global`。
- 结论：客户端已有背包和次数的事件通知及背包增量刷新，但没有统一的角色脏位聚合；角色基础信息更新缺少变更事件，外部直接修改可变 protobuf 数据可能绕过通知。
- 验证方式：静态阅读调用链和 `rg` 检查变更通知、脏状态、版本与刷新入口；本次没有修改代码，因此未执行构建、运行测试或 Unity Play Mode。
- 未解决问题：尚未实现 `RoleDirtyMask`、本地版本、变更快照、同帧批处理和角色基础属性/装备/技能/Buff/任务领域接入；后续实现需单独安排客户端开发与测试任务。

## [QUERY][综合][TR-334] 2026-09-12 客户端脏标记设计文档检查

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3106—3111 行；历史时点记录，不代表当前仍在执行。

- 本次为只读设计输出，未修改业务代码、协议、配置表、场景或预制体。
- 已生成 `项目分析/CLIENT_DIRTY_MARK_DESIGN.md`，覆盖角色组件、脏位聚合、背包/次数现有事件、商城同步、MVC 边界、角色生命周期和验收标准。
- 文档检查：文件存在，章节结构完整；未执行编译或运行测试，因为本次没有代码变更。

## [QUERY][技能][TR-382] 2026-09-15 Ro_cs422nb 状态机与动作安装机制只读分析

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3520—3525 行；历史时点记录，不代表当前仍在执行。

- 分析范围：参考项目 `F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/` 下的 `AsyncObj/MAnimator.cs`、`Model3D/MModel.cs`、`Entity/MEntity.cs`、`Components/State/MStateMachine.cs`、`Components/Action/MActionComponent.cs`、`Components/Common/MIdleComponent.cs`、`Components/Common/MMoveComponent.cs`、`Components/Local/MLocalSyncComponent.cs`，以及 SWIG 暴露的原生 `StateMachineComponent`。
- 验证结果：确认状态组件在 `MEntity.InitComponents` 创建；模型加载完成后由 `MModel.onModelLoadCallback` 初始化 `MAnimator`；待机/移动/技能等由各动作组件调用 `MEntity.OverrideAnim`，统一进入 `MAnimator.OverrideAnim`；状态切换由 `MStateMachine` 记录并在 `LateUpdate` 调用当前状态 `PlayAnim`。
- 结论：Controller 节点是稳定状态插槽，角色专属 Clip 通过 `AnimatorOverrideController` 动态覆盖；原生状态机负责逻辑/同步状态，C# 状态机负责客户端动作生命周期和动画表现。
- 本轮为只读解析，未修改业务代码，未执行编译；详细分析见根目录 `项目分析/RO_STATE_MACHINE_RUNTIME_ANALYSIS.md`。

## [QUERY][网络][TR-424] 2026-09-21 角色状态效果同步方案分析记录

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3866—3870 行；历史时点记录，不代表当前仍在执行。

- 分析范围：PROJECT_CONTEXT.md、CURRENT_TASK.md、DECISIONS.md，以及当前 GameServer 消息队列、OnlineRole/角色组件、Gate 转发和客户端 SceneWorld/角色组件架构。
- 分析结论：状态效果由 GameServer 在逻辑帧中权威创建、刷新、叠加和移除；GateServer 不参与结算；客户端按快照和增量维护状态模型，UI 仅订阅模型变化。状态实例携带实例 ID、配置 ID、来源、层数、服务器开始时间、服务器结束时间、状态版本和可见性。临时战斗状态不写数据库，长期状态通过组件脏标记延迟保存。
- 未修改代码，未执行构建或运行测试。本记录对应设计阶段，后续实现必须追加协议、配置、代码和测试结果。

## [QUERY][网络][TR-425] 2026-09-21 移动同步数据方案分析记录

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3871—3875 行；历史时点记录，不代表当前仍在执行。

- 分析范围：现有 GameServer 50ms 逻辑帧、GameCommand 网络队列、OnlineRole/SceneWorld、客户端 PlayerEntity/RoleEntity 与主城同步入口。
- 设计结论：移动同步分为客户端输入包、服务器权威移动快照、离散移动事件三类；客户端不上传最终位置作为可信数据，服务器不按每帧广播所有对象；本地角色预测后按服务器快照回滚校正，其他角色使用带服务器时间的快照插值。UI、相机、Animator 参数和本地平滑中间值不进入协议。
- 未修改代码，未执行构建或运行测试。本记录对应设计阶段，后续实现需追加协议和运行验证记录。

## [QUERY][网络][TR-427] 2026-09-21 移动同步方案审核与重设计

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3881—3886 行；历史时点记录，不代表当前仍在执行。

- 审核 Agent 结论：上一版按每个 50ms 逻辑帧广播移动结果不通过，虽然不构成严格锁步帧同步，但会演变为按逻辑帧发送状态，网络开销和设计边界不合适。
- 最终方案：采用服务器权威的事件驱动移动段同步。客户端只在开始、停止、方向/速度模式变化和 100~150ms 心跳时发送移动意图；GameServer 仍按 50ms 校验和推进，但只在命令或权威状态变化时向 AOI 广播移动段；周期性校正为 500~1000ms，完整快照只用于进场、重连和丢包恢复。
- 关键规则：不使用公共 FrameId，不要求客户端同帧模拟；其他客户端只根据服务器下发的移动段插值/短时外推，不执行玩家 A 的输入；移动命令按角色合并最新输入，停止和强制移动事件不得丢弃。
- 本次仍未修改代码，未执行构建或运行测试。

## [QUERY][场景实体][TR-429] 2026-09-21 Unity 场景数据导出方案

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3893—3898 行；历史时点记录，不代表当前仍在执行。

- 设计结论：不让 GameServer 读取 Unity Scene、Prefab 或 FBX；由 Unity Editor 导出服务器专用场景数据，GameServer 运行时只读取结构化二进制。
- 推荐第一版：导出 NavMesh 三角形作为可行走地面，导出标记为服务器碰撞的静态碰撞代理，加入场景边界、最大坡度、区域属性和数据版本；使用项目已有 Google.Protobuf 序列化为 `.bytes`。
- 导出流程：编辑器读取场景中的 NavMesh 与 ServerCollision 标记对象，转换为坐标、三角形、碰撞代理和元数据，写入临时目录校验后复制到服务端正式 SceneData 目录。服务器按 SceneId 加载并通过地面、可行走和碰撞查询计算权威移动。
- 未修改代码，未执行构建或运行测试。

## [QUERY][网络][盘点][TR-431] 2026-09-21 魔兽世界开源模拟器移动同步参考

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3905—3911 行；历史时点记录，不代表当前仍在执行。

- 事实说明：魔兽世界原始客户端和服务器并不开源；可参考的是 TrinityCore、AzerothCore 等开源服务器模拟器，具体操作码会随版本不同。
- 参考结论：客户端发送移动开始/停止/方向/跳跃/转向和周期心跳，载荷包含 MovementFlags、客户端时间、位置、朝向、运输对象和坠落信息；服务器在会话逻辑线程校验并更新角色移动状态，再向 AOI 附近玩家广播移动数据。
- 服务器不运行完整刚体物理，而是使用 Map/VMap/MMap 等地图、地形和碰撞查询，在越界、速度异常、瞬移或强制位移时纠正位置；服务器控制移动可使用样条/路径数据。
- 对当前项目的借鉴：采用移动事件+心跳的状态同步，不做公共帧锁步；使用场景碰撞数据做按需校验，服务器将已确认移动状态广播给 AOI，客户端本地即时表现并按校正包修正。
- 未修改代码，未执行构建或运行测试。

## [QUERY][网络][TR-432] 2026-09-21 客户端位置快照同步简化方案

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 3912—3918 行；历史时点记录，不代表当前仍在执行。

- 设计调整：当前阶段不让 GameServer 加载 Unity 场景碰撞、NavMesh 或高度数据，采用客户端定时上报具体位置的轻量同步方案。
- 流程：客户端移动时按约 100ms 上报位置、朝向、移动状态和序号；GameServer 只校验会话角色、场景一致性、时间和最大位移，接受后保存并广播给 AOI 附近玩家；客户端其他角色对快照做插值。
- 服务器不做：坡面查询、完整物理、复杂静态碰撞和每帧权威模拟。客户端负责本地地形表现，服务器仅保留基础异常保护；停止移动和场景切换立即发送快照。
- 风险：客户端可伪造位置，不能直接用于严格 PvP、攻击距离和反作弊结算。后续需要时只对战斗区域或关键技能增加服务器场景校验，不改变普通移动同步主流程。
- 未修改代码，未执行构建或运行测试。

## [QUERY][综合][TR-444] 2026-09-24 Special 状态误触发原因分析（只读）

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4019—4024 行；历史时点记录，不代表当前仍在执行。

- 检查范围：`RoleCtrlBase.RouteStateToEntity`、`RoleState`、`MainRoleCtrl` 跳跃/滑铲入口、`MSpecialComponent` 事件订阅、`MActionComponent.PlayAnim` 失败回退、`MStateMachine.TransferToState/LateUpdate`。
- 结论：未知状态兜底分支派发 `SpecialActionStarted`；Jump 与 Slider 目前没有路由分支，故被误判为 Special。动画加载失败时仅将 Animator 播放目标设为 Idle，状态机当前状态仍是 Special。
- 当前 Unity MCP 资源不可访问，因此未能读取本轮编辑器控制台或触发按键复现；结论来自源码调用链静态检查。
- 本轮未修改业务代码、未执行构建或运行测试。

## [QUERY][网络][TR-462] 2026-09-26 移动同步与状态同步方案复核验证

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4228—4236 行；历史时点记录，不代表当前仍在执行。

- 验证范围：`项目分析/移动同步与状态同步设计.md` 与当前客户端、GameServer、GateServer、公共实体层接口的一致性。
- 源码证据：`GameGlobal` 每 50ms 调用 `SceneManager.Update`，但当前 `SceneManager.Update` 为空；`OnlineRole.SceneUid` 已定义但场景实体尚未进入正式业务绑定；Gate 下行转发会拒绝未登记协议。
- 独立审查：架构审查 Agent 完成只读复核，确认场景生命周期、Gate 接线、位移校验公式、AOI 双向关系、状态事件版本和协议命名为必须修订项；结论已纳入文档。
- 静态验证：Markdown 代码围栏数量为偶数；标题结构可解析；`git diff --check -- 项目分析/移动同步与状态同步设计.md` 未报告目标文档空白错误。
- 业务验证：本次没有修改客户端、GateServer 或 GameServer 业务代码，因此未执行编译和 Unity Play Mode；不能据此声明移动同步已经实现。
- 待后续验证：两个独立客户端的移动开始/停止/转向/三段跳、重连与切图、跨 AOI、负坐标、多实例隔离、超速修正，以及主城 CPU/带宽/队列积压压测。

## [QUERY][技能][TR-482] 2026-10-02 RO 技能表现参考文档核验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4407—4414 行；历史时点记录，不代表当前仍在执行。

- 文件：`项目分析/RO_SKILL_FX_ANIMATION_ROOT_MOTION_REFERENCE.md`；本轮仅修改文档和协作记录，没有业务代码、配置或资源改动，故不执行编译。
- 静态证据：核对 RO `pbl_skill.proto` 的 `MSkillData/AnimationData/FxData/ChargeData`；追踪 `MSkillFactory.BuildSkillData`、`MSkill.Fire/TriggerAnim`、`MSkillComponent.PlayAnim`、`MArtSkill.FireEvents/fxListEvents/OverrideAnim/fx/playFx/charge`、`MTimerMgr.SetTimer/Update`、`MFxMgr.CreateFx`、`MDisplacementComponent.onStart/Update/onStop`。用 XML 解析只读核对两份真实技能 `.txt` 与 `EffectTable.csv` 的特效 ID/时间，未修改 RO 项目。
- 核对结果：多段动画和特效是同一施法时钟上的独立列表；计时器以秒配置、毫秒舍入并在更新帧派发。实体冲锋由独立位移组件按曲线移动；所查技能、组件、模型和 Animator C# 无显式 `OnAnimatorMove`/`deltaPosition`/`applyRootMotion` 技能位移链。对所有 FBX 根曲线或预制体设置不作全局推断。
- 文档检查：`git diff --check -- 项目分析/RO_SKILL_FX_ANIMATION_ROOT_MOTION_REFERENCE.md` 退出码 0。未运行 RO 客户端、逐帧录屏或当前游戏，具体显示帧须结合剪辑 FPS、播放倍率和实机计时器验证。
- 修改日志：`项目修改日志/2026-10-02-RO-SKILL-FX-ANIMATION-REFERENCE-CHANGELOG.md`。

## [QUERY][技能][TR-483] 2026-10-02 RO 技能编辑器参考调查核验

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4415—4421 行；历史时点记录，不代表当前仍在执行。

- 文件：`项目分析/RO_SKILL_EDITOR_REFERENCE.md`、`项目修改日志/2026-10-02-RO-SKILL-EDITOR-REFERENCE-CHANGELOG.md` 及两份协作记录；仅文档变更，无业务代码、配置、资源改动，不需编译。
- 只读证据：RO `config/Assets/Editor/EditorResources/SkillData` 有 3422 份 `.config`，`config/Assets/Editor` 没有 C#；`clientproj/Assets/Editor` 的 28 个 C# 文件中未找到技能编辑窗口。核对 `MConfigData` 与同名 `MSkillData`、protobuf、`MSkillFactory.BuildSkillData`、`ROSkillComponent.AttachSkillEditorSkill`、`SkillReader.LoadSkill` 和 `ROSkillCore.SetSkillEditorSkillData`。
- 结论边界：可确认编辑辅助配置、运行时 `.bytes` 加载和预览注入接口；未找到编辑器 UI、保存/导出及时间轴绘制源码。未运行 RO Unity 编辑器，不验证实际界面和操作体验。
- 文档检查：`git diff --check` 通过；未解决的源码缺口已在分析文档注明。

## [QUERY][综合][TR-484] 2026-10-07 YCombatEditor 融合方案审查

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4422—4427 行；历史时点记录，不代表当前仍在执行。

- 范围：只读核对 `D:/unitypro/JNBJ/YCombatEditor` 的 `package.json`、`SkillEditorWindow`、`SkillConfigSO`、`Global`、`ActionPlayer`、`ActionTiming`、`ActionContracts` 与文档；对照本项目 Unity 版本、`MSkillFactory`、`MSkillData`、`MArtSkill`、`MSkillComponent`、Luban 表。仅新增分析文档、修改日志和协作记录，不执行客户端/服务端编译。
- 独立审查：首轮未通过，指出 10070 挂点/目标落点无法由 Y FX 表达、音频无运行处理器、单段裁剪/混合不兼容；复审又指出隐藏轨道/默认运行字段必须白名单、异步句柄与 YooAsset 收集缺口、Y 自动回填 `skillClip`、默认 `moveCurve` 非空。主 Agent 修订 `项目分析/YCOMBAT_EDITOR_INTEGRATION_DESIGN.md` 后，审查 Agent 最终确认无剩余静态阻断，方案通过。
- 验证边界：源码和文档审查不等于 Unity 2023 兼容。尚未运行隔离 UPM 编译、编辑器保存/预览、YooAsset 实包和角色 Game View，均列为方案中的实施门禁；本轮未修改正式业务系统。

## [QUERY][技能][TR-497] 2026-10-08 当前技能动画与特效播放链只读核对

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4527—4534 行；历史时点记录，不代表当前仍在执行。

- 请求：说明当前技能如何播放动画及特效；本轮仅核对源码/配置，没有修改业务或资源。
- 范围：SkillCtrl.OnSkillKey/RequestAuthoritativeCast、MSkillComponent.BeginPrediction/OnStart/PlayAnim、MStateMachine.LateUpdate、MSkill.Fire/TriggerAnim、MArtSkill.Start/Update/PlayRuntimeEffect/TryPlayLegacyEffect、SkillRuntimeFxLifetime，以及七份 Runtime 配置。
- 结论：按键校验后以 skill_uuid 发请求并本地立即预表现；统一状态机入口安装并播放 Skill 槽。表现特效由 MArtSkill 按独立施法经过秒数派发，编辑帧导出为 TimeSeconds；冷配置整次沿用旧剪辑事件，10070保留旧蓄力/落剑事件。Q第一段当前无新配置特效，第二段23帧约0.1917秒，第三段23/54帧约0.1917/0.45秒。特效尾粒结束销毁并释放资源引用。
- 验证：只读函数调用与配置交叉核对；没有运行画面或新编译测试，不扩大为运行验收。上一轮连续Q修复的真实画面验收仍待Unity正常编译后复验。
- 记录：项目修改日志/2026-10-08-Q-COMBO-GROUND-CHANGELOG.md。下一步：将核对结果直接说明给用户。

## [QUERY][综合][TR-498] 2026-10-08 旧 AnimationEvent 消费链用途核对

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4535—4541 行；历史时点记录，不代表当前仍在执行。

- 请求：解释 MArtSkill 中读取 clip.events 的循环，核对新 RO 实现是否需要此路径。
- 范围与结论：只读核对 RO MArtSkill.fxListEvents（237起）、EArtsSkillTimerCb.EFx分派、fx，以及本项目 MArtSkill.Start/Update 和3007001配置。RO按技能数据FxList/At向MTimerMgr注册特效回调，不读取AnimationClip.events；用户指出该循环非RO新链属实。本项目额外保留它作为旧兼容：新配置未ready/无效时走旧事件，10070运行资产PreserveLegacyFxEvents=1且FxEvents为空，所以1技能蓄力/落剑确实仍依赖该循环，不能描述为死代码。
- 状态：本轮解释与参考核对，没有修改业务/资源或执行编译。清理该链须先把10070两条特效完整导出到正式轨道，并处理配置冷加载；不能直接删除导致1技能特效消失。用户本轮为用途询问，未在此解释任务中展开配置迁移开发。
- 验证：真实参考源码和本项目配置交叉核对，无运行验证。后续如进入清理，必须委派开发并独立测试审核；保留旧动画和原表。

## [QUERY][技能][TR-505] RO技能音效只读核对

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4611—4616 行；历史时点记录，不代表当前仍在执行。

- 证据：RO MArtSkill.FireEvents为AudioList按At/LoopCycle/LoopCount注册EAudio，audio根据End>At选择PlayAudioInstance并注册EKillAudio，否则PlayAudio；实例列表在释放时StopAudioInstance。MModel交MAudioMgr播放3D音效或取得FMOD事件实例。FxList另注册EFx；动画MSkill.TriggerAnim用ClipName/SkillClips，有AnimList则交定时覆盖。
- 当前ReadFx检查audioClip必须为空且contentKind为粒子，SkillRuntimeFxEvent不包含音效；窗口PlayFxAndSound仅预览调用AudioSource.PlayOneShot。正式粒子按TimeSeconds在MArtSkill.Update派发。
- 结论仅为源码对照，不是编译/实机验证；没有修改业务/资源/协议，不自动补音效处理器。修改记录项目修改日志/2026-10-10-RO-SKILL-AUDIO-COMPARE.md。

## [QUERY][技能][TR-506] RO技能轨道类别只读核对

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4617—4622 行；历史时点记录，不代表当前仍在执行。

- 证据：pbl_skill.proto:8的MSkillData及嵌套数据、MArtSkill.FireEvents:259、Affix_HuoYanZhiDi_RongYanPenShe.config编辑辅助列表，与既有RO_SKILL_EDITOR_REFERENCE交叉复核。
- 确认时间轴数据类别：AnimList/AudioList/ChargeList/RotateList/WarningList/ShakeList/CameraFxData/CameraPostEffect/ChangeColorList/SkillBarList；ResultList/MobUnitList/AIEventList/BuffList由协议确认，未冒称客户端统一派发。LongAttackData位于ResultList中，CombineSkillList不是已确认独立编辑轨道。
- 边界：窗口UI源码缺失，具体轨道显示名称/数量不能确认；本轮仅源与产物核对，未编译/运行/界面操作，无业务或资产变更。记录项目修改日志/2026-10-10-RO-SKILL-TRACK-COMPARE.md。

## [QUERY][技能][TR-528] 2026-10-11 RO特效End只读核对

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 4913—4917 行；历史时点记录，不代表当前仍在执行。

- 只读真实proto/MArtSkill/MFx/MFxMgr，确认At/End/DestroyDelay单位秒、playTime=(DestroyDelay+End-At)*_timeScale，MFx.Init把时长记DestroyTime，Mgr.Update正值减帧秒归零移除；非正默认走特效表PlayTime/FxTimer路径。结束时间有实际用途，不能声称与本项目纯标记相同。
- 未改业务/用户资产、未运行编译或运行测试。分析项目分析/RO_SKILL_FX_END_SEMANTICS.md，归档项目修改日志/2026-10-11-RO-FX-END-ANALYSIS-CHANGELOG.md；当前查询完成，能力差异需后续开发审核接入。搜索未命中Editor命名文件不代表不存在编辑器，仅以已定位协议/执行链回答。

## [QUERY][导航][网络][技能][盘点][NPC][协作][TR-551] 2026-10-22 寻路与技能配置位移网络同步专项检查

> 整理前原文：`项目修改日志/协作记录归档/2026-10-23/TEST_REPORT.md`，第 5124—5139 行；历史时点记录，不代表当前仍在执行。

- 当前源码只读追踪：本地A*导航/Charge位移→碰撞后坐标→11180→服务端保存和附近广播11181→远端已有实体移动事件；协议字段和角色范围逐项核对。
- 执行_temp/navigation-root-sync-20261022/verify_sync.py。当前PlayerEntity.Update/UpdateMove、MMoveComponent.Update/onSyncMoveEvent/LagSyncUpdate方法原文及完整ROMoveComponent参加隔离编译运行，11项断言通过、零错误、1条抽取上下文未使用字段警告，输入哈希source-hashes.json、输出runtime.log。
- 通过：无方向输入导航发包、停止发包、非普通移动的技能位置上报、服务端坐标保存和字段透传、默认远端追赶、技能禁止移动时暂停大误差追赶和默认态恢复、小误差直接吸附、同帧曲线/追赶互斥、曲线帧后剩余距离追赶。
- 已复现限制：技能IsActiveMovable=false且目标误差大于0.1米时，LagSyncUpdate不推进；当前五条MoveCasting配置均false。本轮仅确认行为，没有修复，不能把11项通过当作严格同步验收。
- 技能开始广播未被客户端用于服务器时间对齐，方向来自远端当前模型；两端曲线独立播放。未跑真实Socket双客户端、Unity原生碰撞/画面、弱网、完整客户端或服务器构建及部署生效。数学/宿主/状态与发送为薄边界，未模拟网络和完整状态生命周期。
- 正式记录：项目分析/NAVIGATION_CHARGE_NETWORK_SYNC_CHECK.md、项目分析/项目难点.md、Docs/AI/CURRENT_TASK.md、本文件及项目修改日志/2026-10-22-NAVIGATION-CHARGE-SYNC-CHECK-CHANGELOG.md。无业务/资源修改，下一步按专题进行正式双号对照。

- 本阶段豆包run59396782753161730恢复返回task_unavailable，stop确认cancelled/stopped=true，未取得报告，不计模型失败轮。已沿本项目委派WorkBuddy桌面只读续查Hub hub-1284e335-707f-4b74-8252-b84d93defbce；仍未修改新业务或取得新验收结论。
- 2026-10-23 MMORPG 功能盘点：
  - 静态核对通过：已核查 `MNavigation` 客户端 A*、服务端 `NavigationComponent` 空壳、`ClientMessageManager` 协议注册、任务/装备/物品 Handler 目录、聊天本地回显、设置页面切换、商城 Handler 和装备服务端 `NotImplemented` 证据。
  - 文档验收：盘点正文和修改日志已创建；执行文档存在性检查与 `git diff --check`，结果通过。
  - 运行边界：本轮无业务代码修改，因此未执行 Unity/服务端编译、运行场景、双客户端、弱网或发布包测试；这些仍是 P0 专项验收项。
  - 交接：下一步仅提交本轮四份文档，保留工作区用户改动和未跟踪临时文件，不使用 `git add .`，提交后推送当前功能分支。
