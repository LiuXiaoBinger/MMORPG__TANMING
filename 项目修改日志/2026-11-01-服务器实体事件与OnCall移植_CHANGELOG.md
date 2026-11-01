# 服务器实体事件与 OnCall 移植修改日志

- 任务编号：`SERVER-ENTITY-EVENT-20261101`。
- 日期：2026-11-01；开发者已通过交互式弹窗明确选择“写入”后创建本正式记录，不追认此前自动写入。
- 分支：`codex/feature-development`；代码基线 `9bb53f8`，代码提交 `021b076bfc2a28f053c9ee8ca84f1a37eecd7513`。
- 参考工程：`F:/UnityPro/ro/Ro_cs422nb`（只读）。本项目目录：`D:/unitypro/LYFMMORGP`。
- 正文成果：`项目分析/RO技能链路思维导图-2026-11-01.html`。原始证据目录：`_temp/server-entity-event-20261101/`；临时调查方案早期内容已被后续验收修正，以本日志、最终源码与最终导图为准。

## 需求与实现范围

移植 RO 的服务端实体事件核心机制，让客户端施法请求通过事件驱动原有技能业务；将共享消息处理入口从 Handle 改成 OnCall；核实并补充用户提供的 DeepSeek 思维导图；类/函数/变量及文件分组尽量沿用 RO，本次明确例外于“一类一文件”；指定 WorkBuddy 国际版 DeepSeek v4.1 开发，运行测试后关闭本项目四服。

实际链路：`ClientMessageManager` → `SkillCastHandler.OnCall` → `ROClientFireSkillArgs` → `OnlineRole.GetEventDispatcher().DispatcherEvent` → `RoleSingingComponent.OnClientFireSkillRequest` → `ROSingingArgs` → `RoleSkillComponent.OnSingingRequest` → 既有 `ROSkillComponent.TryCast` → `SkillCastRet` 回包 → 成功时 `SkillCastStartNtf` 广播。

## 逐项文件清单

业务与测试/图共24个实际增改文件：新增4个、修改20个。HTML 原已存在但未跟踪，本次修改后首次入库。无移动、删除；本次无 Unity 资源与 `.meta` 变更。

| 变化 | 项目根相对路径 | 用途与具体变化 |
|---|---|---|
| 新增 | `LYF_Server/GameServer/Event/ROEvent.cs` | 集中定义 ROEventDefine、ROVector3、ROEvent、ROClientFireSkillArgs、ROSingingArgs；沿用 RO 命名与分组，字段载荷强类型。 |
| 新增 | `LYF_Server/GameServer/Event/ROEventDispatcher.cs` | 集中定义 IROEventListener、监听登记与同步派发器；注册替换、解除、嵌套派发和 UnInit 清理。 |
| 新增 | `LYF_Server/GameServer/Components/RoleSingingComponent.cs` | 初始化注册客户端施法事件监听，转换为吟唱事件；Dispose 解除监听。 |
| 新增 | `LYF_Server/Tests/EventDispatcherSmoke.cs` | 专用独立测试，覆盖9项派发行为及经 OnCall 的真实环回 TCP 成功/拒绝发送路径；不挂正式场景或业务组件。 |
| 修改 | `LYF_Server/GameServer/Components/RoleSkillComponent.cs` | 注册/解除 kActionSinging；在 OnSingingRequest 中复用 TryCast，先返回结果，再广播开始。 |
| 修改 | `LYF_Server/GameServer/Entity/Role/OnlineRole.cs` | 每角色持有派发器、提供 GetEventDispatcher，注册施法请求组件，在角色释放时 UnInit。 |
| 修改 | `LYF_Server/GameServer/GameServer.csproj` | 在旧式显式 Compile Include 清单登记3个新增生产源文件。 |
| 修改 | `LYF_Server/GameServer/MessageHandler/ClientMessageManager.cs` | IGameMessageHandler 声明与调用点 Handle 改 OnCall；未将开发者既有队伍协议注册增量混入代码提交。 |
| 修改 | `LYF_Server/GameServer/MessageHandler/Skill/SkillCastHandler.cs` | 入口改 OnCall；构造 ROClientFireSkillArgs 后派发，不再直接 TryCast；原入口错误回包保留。 |
| 修改 | `LYF_Server/GameServer/MessageHandler/Login/LoginGameServerHandler.cs` | 登录游戏服协议入口 Handle 改 OnCall，业务保持原样。 |
| 修改 | `LYF_Server/GameServer/MessageHandler/Login/CreateRoleHandler.cs` | 角色创建协议入口改 OnCall。 |
| 修改 | `LYF_Server/GameServer/MessageHandler/Login/StartGameHandler.cs` | 开始游戏协议入口改 OnCall。 |
| 修改 | `LYF_Server/GameServer/MessageHandler/Role/EnterWorldHandler.cs` | 进入世界协议入口改 OnCall。 |
| 修改 | `LYF_Server/GameServer/MessageHandler/Role/MoveOperationHandler.cs` | 移动上报协议入口改 OnCall。 |
| 修改 | `LYF_Server/GameServer/MessageHandler/Role/RoleSkillInfoHandler.cs` | 技能列表回包入口适配共享 OnCall 接口。 |
| 修改 | `LYF_Server/GameServer/MessageHandler/Role/SaveRoleDataHandler.cs` | 角色保存协议入口改 OnCall。 |
| 修改 | `LYF_Server/GameServer/MessageHandler/Knapsack/OpenKnapsackGridHandler.cs` | 背包扩格入口改 OnCall。 |
| 修改 | `LYF_Server/GameServer/MessageHandler/Knapsack/RoleKnapsackInfoHandler.cs` | 背包信息入口改 OnCall。 |
| 修改 | `LYF_Server/GameServer/MessageHandler/Knapsack/SyncKnapsackGridCountHandler.cs` | 背包格数同步入口改 OnCall。 |
| 修改 | `LYF_Server/GameServer/MessageHandler/Shop/RequestBuyShopItem.cs` | 商店购买请求入口改 OnCall。 |
| 修改 | `LYF_Server/GameServer/MessageHandler/Shop/BuyShopItemCenterResultHandler.cs` | 中心服购买回执处理器适配共享 OnCall 接口。 |
| 修改 | `LYF_Server/GameServer/MessageHandler/Team/TeamMessageHandler.cs` | 队伍消息入口改 OnCall。 |
| 修改 | `LYF_Server/Tests/SkillRoleSnapshotSmoke.cs` | 已有技能列表隔离测试调用 Handle 改 OnCall。 |
| 修改（首次入库） | `项目分析/RO技能链路思维导图-2026-11-01.html` | 保留原图，修正接口/字段/状态数/频率/token与引用行号；新增派发语义和本项目边界，校正参考路径。 |

本次获准收尾记录另包含以下6个文件，不重复创建独立流水：

| 变化 | 项目根相对路径 | 用途与具体变化 |
|---|---|---|
| 新增 | `项目修改日志/2026-11-01-服务器实体事件与OnCall移植_CHANGELOG.md` | 本唯一完整日志：文件清单、设计取舍、执行身份、验证边界、失败修正与Git回执。 |
| 修改 | `Docs/AI/CURRENT_TASK.en.md` | 追加同编号英文实现事实、宿主适配、续作边界和本日志引用。 |
| 修改 | `Docs/AI/CURRENT_TASK.md` | 由子Agent同步本编号中文段，并仅核验本次增量一致性。 |
| 修改 | `Docs/AI/TEST_REPORT.en.md` | 追加同编号英文编译、行为/协议/导图验收事实与未验证项。 |
| 修改 | `Docs/AI/TEST_REPORT.md` | 由子Agent同步本编号中文验收段与边界。 |
| 修改 | `Docs/AI/RECORD_INDEX.md` | 双语同步完成后机械更新源摘要、标题和精确行段，不复制正文。 |

## 设计与 RO 对应关系

1. `rogamelibs/core/event/event.h` 的事件定义分组对应本项目 `Event/ROEvent.cs`；`event_dispatcher.h/.cpp` 对应 `Event/ROEventDispatcher.cs`。保留 RO 原拼写 `kActionClienFireSkill`，只接入有真实消费者的客户端施法与吟唱两类事件。
2. C# 引用与 GC 持有事件生命周期，不机械复制 `MAKE_SP_EVENT` 手工引用计数。监听身份为 `IROEventListener`，载荷为具名 ROEvent 派生类，不用 object/dynamic 作业务载荷。
3. 派发同步就地执行，重复注册替换；按索引遍历保留原登记引用的快照，解除/替换立即可见，新登记不加入本轮快照。此处是 C# 集合迭代适配，不等同于逐行移植 C++ `event_lock_` 与惰性 erase。
4. 当前生产施法业务属于 OnlineRole/RoleSkillComponent，因此每个在线角色持有派发器并随组件注册、解除与角色 Dispose 清理。既有 `SceneEntity/World/EntityWorld.cs` 确实存在 UID 字典和 TryGet；它尚未接入生产施法链，不能表述成“项目没有实体索引”，本次没有新造注册表或接通整套场景世界。
5. 学习/MP/CD/UUID/拒绝码继续复用既有 TryCast，结算内部未改。不加第二套校验、双等待队列、重试、异步调度或新状态机。协议 `SkillCastReq` 目前只有 SkillId/SkillUuid，事件 target_ 与 cast_point_ 保留零值默认；ROVector3 为纯服务端轻量坐标载荷。
6. RO `TransferStateByFunc` 可能异步，也可能在无旧状态时当场回调；RO `DispatcherEvent` 本身同步。本次没有移植状态互斥等待、真实吟唱时长/中断态、请求队列、对象池、延迟派发和 timer_handler_，不能将完整 RO 技能状态机当成本次已完成。
7. `CanHandle` 和 RoleManager 内部 HandleX 保留；其它共享 IGameMessageHandler 实现连同中心服回执一并适配 OnCall，是接口变更所需，不重命名无关内部方法。

## 思维导图核验与补充

- 修正 `ROSingingArgs` 实际具有 `skill_combo_id_`，来源 `rogamelibs/core/event/event.h:716/:733`。
- 注册状态数量更正为36；吟唱期合法性检查沿 SingingComponent.OnUpdate 逐次执行，不能写为100ms定时器。
- 区分 `GetSkillCoreCheckReplace`（4参，后两参可默认）与 `GetReplaceSkillCoreOnClientRequest`（5参），按各调用点保留函数，不能全局互换。
- `StateMachineComponent::ActionToken()` 在 `state_machine_component.h:38` 调用 `current_->Token()`，后者读取 ActionStateComponent 保存的令牌；ROEvent 自身令牌另用于延迟派发记账。不能断言“全工程无人读取”，脚本侧调用未全量核对。
- 更新已确认的关键行号映射，增加“⑧ 事件分发器语义”和“⑨ 本项目接入边界”；最终69节点、9主干。页首参考路径改为本轮真实参考目录，正确类型为 ROSingingArgs。

## 执行身份、真实轮数与失败修正

- 主Agent：Codex，负责范围/任务发布、必要核对、最终行为与导图验收、正式英文事实、记录和Git集成；未代写委派业务成果。
- 执行者：WorkBuddy国际版 `WorkBuddyAI.exe`（`E:/Program Files/WorkBuddyAI/WorkBuddyAI.exe`）；实际原生模型 `deepseek-v4.1-flash`。用户的免费模型偏好不代表网关已读取计费或余额。
- 原Hub任务：`hub-de717361-5b83-4381-872e-03f678bafd41`；原生会话：`df323eca-beff-41fc-a54a-840e40ff96d5`；来源均为 `desktop_queue`，已实际验收 accepted。

| 真实轮次 | attemptId | 实际内容与结果 |
|---|---|---|
| 1 | `run-b1f026d1-69b8-4681-8fa9-bd160f4aa381` | 只读调查。初版一类一文件、额外队列和“无UID索引”等偏差被纠正后才授权开发。 |
| 2 | `run-29987c72-3b00-4927-a405-2de5d3ce3936` | 实现事件链、OnCall、测试和导图；编译/初版测试通过，但图残留错句和成功回包发送证据不足，未立即验收。 |
| 3 | `run-c20a4728-72d1-478e-8cf2-10fbd87ef8a8` | 修正图与注释，补环回TCP结果/开始通知及拒绝验证。业务产物落地，但回报漏写 tests/ 导致Hub INVALID_OUTPUT/ENOENT拒收。 |
| 4 | `run-2dd0ee6c-d50e-435c-be19-596f2f9ef2e6` | 仅修回报路径及报告准确性；明确旧式生产 bin/obj 仍在默认目录，并纠正将状态拒绝误称CD拒绝的表述。 |
| 5 | `run-e4dfff21-aab6-4542-b514-1c7ddce0b054` | 仅修图页首旧参考路径与过程化元信息，新增摘要误写 ROSingArgs。 |
| 6 | `run-82f4f6b3-45ef-47dc-bf61-955ad44dbfc2` | 仅将页首 ROSingArgs 定点修正为 ROSingingArgs。主Agent最终渲染检查通过。 |

连接恢复不计开发轮：最初原任务排队，国际版缺网关回环端口；核实6个原生会话均已 completed、无活动任务后按已有授权重启国际版，保留原任务/会话/模型，随后实际领取。无伪造claim、审批或回执。

本次开发业务代码实际修改2轮（第2轮实现、第3轮返修）；其余为调查、回报或图元信息修正。后续获准日志的中文同步作为独立限定记录任务执行，其真实身份与检查证据在本日志续记，不冒充原6轮业务开发。WorkBuddy原任务回报 `skills_applied: []`，不冒称使用额外技能。

## 验证结果与证据

测试基线为保留既有开发者改动的本地工作区，非干净克隆或正式部署验收。

| 检查 | 实际结果 | 项目根相对证据路径 |
|---|---|---|
| GameServer及依赖编译 | `dotnet build GameServer.csproj --no-incremental -v:m`：0错误、14个既有警告。直接 MSBuild.exe/dotnet msbuild 在WorkBuddy宿主被命令策略拒绝后，改用实际成功的dotnet build；新增源已显式登记。 | `_temp/server-entity-event-20261101/build-gameserver-r3.log` |
| 事件9用例 | 同实例传递、不同回调重复注册替换、解除/回调内解除、同类型嵌套、角色隔离、UnInit/Dispose、实际施法发送全部通过。 | `_temp/server-entity-event-20261101/tests/smoke1-r3-utf8.log` |
| 环回TCP成功 | 从OnCall进入两段事件，结果11170先于开始11171；SkillId10010、UUID7、权威MP500→480、带CD快照。 | 同上；`LYF_Server/Tests/EventDispatcherSmoke.cs` |
| 环回TCP拒绝 | UUID8收到 SkillStateBlocked(32)，MP仍480，300ms未发Start。原测试只断言非Succeed，具体码由已捕获帧反解；不能称为已测CD拒绝。 | 同上；`_temp/server-entity-event-20261101/development-report.md` |
| 技能列表回归 | A迟到回包拒绝，B有效回包装载；exit0。 | `_temp/server-entity-event-20261101/tests/smoke2-r3-utf8.log` |
| Codex独立必要验收 | 实际运行EventDispatcherSmoke.exe与SkillRoleSnapshotSmoke.exe，各exit0；不是只读执行者结论。 | `_temp/server-entity-event-20261101/codex-runtime-acceptance.json`、`EventDispatcherSmoke-codex.raw.log`、`SkillRoleSnapshotSmoke-codex.raw.log`（后两者同任务目录） |
| 四服真实TCP探针 | 四服在线，连接GameServer10130，实际无会话施法请求返回InvalidSession。 | `_temp/server-entity-event-20261101/tests/probe-r3-utf8.log` |
| 四服关闭 | 第3轮Center58592/Login59492/Game65580/Gate64468均按本项目绝对exe路径结束；Codex在2026-11-01 18:38:02+08:00复查进程0、10110/10120/10130/10140监听0。 | `_temp/server-entity-event-20261101/codex-servers-final.json`、`development-report.md`（同任务目录） |
| 导图 | 最终脚本/结构检查通过；Codex Edge headless实际渲染无pageerror，本项目边界面板显示正常。 | `_temp/server-entity-event-20261101/mindmap-render.json`、`mindmap-overview.png`、`mindmap-project-boundary.png`（同任务目录） |
| Git暂存差异 | 只暂存本任务24个增改文件；原有队伍注册与SkillCastHandler换行未混入，差异空白检查通过。 | `_temp/server-entity-event-20261101/staged-business-files.json`、`git-commit.log`、`git-push.log`（同任务目录） |

测试工程、测试exe与日志在 `_temp`。生产旧式GameServer构建仍写入 `LYF_Server/GameServer/bin`、`obj` 等既有默认输出，未提交，未清理或移动开发者既有产物；这是本次实际输出边界，不能声称所有构建中间文件都在_temp。

## 未解决与未验证边界

- 未完成真实 Login/Gate 登录后成功施法的实网验收；环回TCP发送验证不能代替完整登录验收。
- Unity客户端表现、多真实角色并发/广播可见性、性能未验证。
- RO状态互斥等待、真实吟唱时长/打断等未移植，本次同步接入不是完整RO技能状态机。
- 本次日志获准后仅维护记录，不新增代码、不重跑输入未变的编译与运行测试，也不重新启动四服。

## 技能与可观测用量

Codex在开发阶段使用 workbuddy-executor（国际版派工/回执/返修）、project-subagent-coordination（范围与身份/验收）、karpathy-guidelines（最小移植与保留既有结算）、token-budget（定向读取与证据落盘）、task-log-policy（先确认再写、索引）、token-monitor（collect/report/context与原始压缩事件统计）。本次获准记录收尾沿用除karpathy-guidelines外的上述5项技能，不增加业务开发。

此前可观测窗口2026-11-01 17:23–18:49（上海）：新增输入~254720、缓存输入~22971904、输出~73307 tokens，来源本地token-monitor的Codex实验性适配器，不能当精确账单。上下文/项目规则、用户需求、工具输入输出、重复/返工/验证不能独立分摊；WorkBuddy token未披露。此前原始本对话JSONL完整覆盖至18:50:51时压缩累计0次；本次记录收尾的更新统计与覆盖边界保存在 `_temp/server-entity-event-20261101/logging/`，不合并其他会话、不将缺失归因记为0。账户剩余额度本地不可测。

## Git回执

- 业务提交：`021b076bfc2a28f053c9ee8ca84f1a37eecd7513`，`feat: 移植RO服务端实体事件链并统一OnCall入口`，24文件；`9bb53f8..021b076` 已推送到 `origin/codex/feature-development`，`git ls-remote`核对一致。未合main，未强推。
- 获准记录收尾：本日志、CURRENT_TASK/TEST_REPORT双语新增段与索引作为后续docs提交；仅集成本编号增量，保留其他任务未提交记录。真实收尾提交与推送结果在本节续记，不为Git回执重复维护全部双语入口。


## 获准中文同步与记录验收回执

- 用户在原交互式弹窗明确选择“写入”后，Codex新建本完整日志，并先追加 CURRENT_TASK.en.md / TEST_REPORT.en.md 的同编号英文事实。
- 中文同步执行者：WorkBuddy国际版 `deepseek-v4.1-flash`；独立日志任务 `hub-29f2d7af-f7ed-4eb3-9d08-86153362e425`，原生会话 `df323eca-beff-41fc-a54a-840e40ff96d5`，真实第1轮，`attemptId=run-52728c01-a0a9-4f7a-96df-f264e212bdb9`，`runtimeSource=desktop_queue`。本记录任务不计入原业务6轮。执行者明确回报本次未使用额外技能。
- 实际只向 CURRENT_TASK.md 追加2478字节、TEST_REPORT.md追加2776字节；交付中文精确新增段与 SHA256/前缀保留证据。16项中英增量核验16项一致、0差异，结论 `ZH_SYNC_OK`。
- Codex仅检查实际修改范围、英文输入SHA256未变、中文原始前缀与精确新增后缀吻合，并核对执行者检查证据；未再逐项重复翻译或全量核对历史。
- 证据：`_temp/server-entity-event-20261101/logging/bilingual-sync-report.json`、`CURRENT_TASK.zh.append.md`、`TEST_REPORT.zh.append.md`、`codex-record-scope-check.json`（后三者同logging目录）。
- 双语新增段不包含本轮Git回执重复流水；Git提交与推送仅续记在本完整日志。现有工作区其它任务记录和业务修改继续保留，提交仅取本编号新增段。工作区索引与提交索引分别按各自源文件生成，避免把未获本任务授权的旧任务正文混入本次提交。
- 本次只维护记录，未重跑编译、测试或启停四服；代码输入与原验收边界未改变。
