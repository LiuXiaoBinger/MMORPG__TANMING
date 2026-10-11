# 项目设计决策

最新参考移植约束见本文件末节“参考移植忠实与简洁约束”：用户指定参考项目时以原结构/接口/时序为准，不自行扩展需求外机制。该要求已同步 AGENTS.md 与 AIconfig/workflow.md，并覆盖历史技能快照更新方案。

## 最新 RO 到期操作与模型加载接口（2026-10-11）

- 本节替代历史启动 PresentationReady 订阅、技能 pending 和缺挂点等待重载方案。Start 仅 base.Start/FireEvents，protected virtual void FireEvents 只登记时间线，冷配置终态返回同入口；OnProcessTimer→InnerProcessTimer→fx/audio 执行一条到期数据，不在 Update 注册、补播或绑定根。
- 模型异步边界使用 RO 同名 AddLoadGoCallback/RemoveLoadGoCallback；仅到期操作按需申请，暖同步零票据、冷可取消非零票据。保实际登记顺序，取消再添加与整数回绕不得因字典槽位重排。真正归池 Reset 清全部等待；同模型资源替换保尚未交付请求，旧加载会话不能交付新对象。
- 删除技能启动 `_presentationModel`、`_modelReady`、OnModelReady、模型 PresentationReady 与两套 pending。施法代次只用于实际异步操作和计时载荷的池复用保护；自然 End 保 GO 请求与完整声尾，Interrupted 注销等待并停止声音。
- RO fx 使用 GO 回调，RO audio 使用 FMOD 模型接口，两者冷加载语义不同。本项目 Unity AudioSource 必须依附 Transform，因此声音到期也等待 GO 是适配，禁止把此处描述为 RO 音频原样实现。
- 统一 TriggerAnim 登记当前动作所有者，模型到达后只恢复该动画；离槽解除，不混入 FX/Audio。动画根增量继续由 Collector→MMove 走重力与碰撞链，不把 RO ECharge 当作 Animator 根运动。
- 已加载完整模型缺固定挂点为配置错误，报告并跳过该粒子，不保无限等待、不阻塞后续事件。冷 GO 初始化完成后正常解析挂点。
- 当前强类型载荷、锁定表现行、UUID/代次、独立粒子租约与用户资产保护继续生效；范围和验收归档 `项目修改日志/2026-10-11-SKILL-TIMELINE-RO-CALLBACK-CHANGELOG.md`，参考事实见 `项目分析/RO_SKILL_TIMELINE_RESPONSIBILITY.md`。

## 项目文档归档规则（2026-09-24）

- 项目根目录的 `项目修改日志/` 是唯一的项目修改说明归档目录。代码、配置、协议、表格、资源、场景、工具或文档发生修改时，都必须在此新增或更新记录，包含文件范围、修改/设计思路、验证结果和未解决项；禁止继续在根目录、源码目录、`Docs/AI` 或 `_temp` 散落日志。
- 项目根目录的 `项目分析/` 是项目解析与设计文档的唯一归档目录，包含参考项目解析、架构设计、功能方案及专题使用说明；新增此类文档必须放在该目录。
- `Docs/AI/` 仅保留项目上下文、当前任务、决策和测试报告四类协作入口记录；详细的专题设计和解析正文放在 `项目分析/`，变更说明放在 `项目修改日志/`。
- 文档互相引用使用项目根目录相对路径，避免文件移动后链接失效。

## 协作流程

1. 产品经理拆解需求并给出验收标准。
2. 策划确认数据库表、配置表和数值规则。
3. 客户端和后端在各自边界内并行开发。
4. 测试 Agent 编译、运行和回归验证。
5. 测试不通过时记录失败项，开发修复后重新验证；不直接覆盖用户改动。

## 代码注释约束（2026-09-15）

- 新增或修改的业务代码必须为每个类型/接口、函数（含构造函数）、字段、属性、常量、事件、参数及局部变量补充具体中文注释。
- 注释需要说明用途、数据语义、单位/范围或生命周期等实际信息，不能只重复标识符名称；修改既有代码时，在修改范围内一并补齐缺失注释。
- 该约束适用于客户端、服务端和公共库；代码审核与测试静态检查必须将注释完整性列为验收项。

## 持久记录规则

- 每项工作开始前，必须依次读取 `PROJECT_CONTEXT.md`、`CURRENT_TASK.md`、`DECISIONS.md` 和相关 `TEST_REPORT.md`。
- 每项工作开始时，在 `CURRENT_TASK.md` 明确需求、验收标准、文件范围、负责人、测试方案和当前状态。
- 每项工作结束时，必须更新 `CURRENT_TASK.md`，记录实际修改文件、验收结果、未解决问题和下一步交接信息。
- 每次执行构建、静态检查、运行验证、协议生成或资源操作，都必须在 `TEST_REPORT.md` 新增一条带日期记录；失败记录不得删除，修复后追加新记录。
- 只读分析或未产生代码修改的工作，也要记录分析范围、结论和未修改内容，避免后续 Agent 重复调查。
- 结构发生变化时同步更新 `PROJECT_CONTEXT.md`；架构取舍发生变化时同步更新 `DECISIONS.md`。
- 临时产物只允许写入 `_temp`，记录中要写明用途和是否需要产品经理确认清理。

## 战斗与同步

- 服务器是最终权威，客户端可以预测表现，但必须用服务器结果校正。
- 使用 `attackSeq` 匹配客户端预测和服务器回包，避免重复结算。
- Buff 时间使用服务器时间或服务器 Tick，不使用客户端本地时间作为最终依据。
- 随机结果使用由战斗种子、攻击者、攻击序号、技能和命中序号组成的确定性输入；不要依赖双方独立调用的连续随机状态。

## UI 弹窗

- 购买确认弹窗同一时间只允许存在一个。
- 异步加载弹窗时也必须锁定入口，防止快速双击绕过已显示状态检查。
- 弹窗销毁时必须释放管理器中的引用或锁定。

## 背包容量

- `RoleKanpsackInfoRet.KanpsackTypeCountLst[].Count` 的语义为该包裹已开格子容量，禁止填入当前物品数量。
- 新角色和缺失历史数据的角色默认至少开启 81 格；客户端按容量显示空格，按 `RoleItemInfo.BagIndex` 放置物品。
- GameServer 的网络回调只能投递不可变 `GameCommand`；涉及角色背包容量的修改必须在 `GameCommandDispatcher` 的主循环消费者中执行。

## GameServer 逻辑线程

- 借鉴参考 C++ GameServer，网络回调只负责复制协议输入并投递，禁止直接修改角色、场景或背包等游戏状态。
- C# GameServer 统一使用 `GameCommandDispatcher` 消费不可变 `GameCommand`。命令至少携带来源、协议号、会话、载荷、入队时间和序号；禁止使用捕获网络包的 `Action` 闭包作为正式业务队列。
- 主循环先限量消费网络命令，再以 50ms（20Hz）驱动逻辑 Tick。Buff、冷却、战斗和服务端定时逻辑应以 `GameGlobal.LogicTick` 或服务器时间为准。
- `RoleManager` 只由逻辑线程维护 `RoleId -> OnlineRole` 与复合会话 -> RoleId 双索引；跨线程枚举必须使用快照。
- 正式背包扩容必须使用服务端配置驱动的档位、上限和扣费事务。当前 `OpenGridCount` 仅用于兼容现有测试协议，不能信任为正式数值。

## GameServer 管理器队列与预注册

- `GameCommandDispatcher` 是跨网络线程的总生产者队列，只负责按协议和来源把命令投递到目标管理器队列，不直接执行管理器业务。
- 每个管理器必须拥有独立命令队列，并在自身 `Update(delta)` 中限量消费命令后执行固定帧逻辑；管理器异常不得阻断其他管理器。
- `GameModuleManager` 的注册顺序就是固定帧更新顺序，当前预注册顺序参考 roserver：Redis、小游戏、角色、解耦、HTTP、怪物波次、限时特惠、商人、商城刷新、系统开放、后台活动、限时系统、相框、场景、世界事件、GVG。
- 当前只有登录和角色模块接入实际协议处理，其余管理器为可扩展骨架；后续新增管理器必须以 `XXXManager.Instance` 形式在 `GameServerModules.cs` 或对应独立文件中注册，不在 `GameGlobal` 增加业务接口。
- `GameGlobal` 只负责时间基准、总队列消费和管理器 Tick，不提供角色增删查等业务转发接口；角色访问统一通过 `RoleManager`。

## Agent 执行边界与一类一文件（2026-09-04）

- 每个顶级类、接口、枚举和委托必须独立放置在一个同名源文件中，便于定位、审查和后续拆分。
- 主 Agent 只负责需求拆解、任务委派、测试与审查协调、最终验收和持久记录，不直接修改客户端或服务端业务代码。
- 业务代码变更必须由对应客户端或服务端开发子 Agent 执行；测试 Agent 只验证，审查 Agent 只提出问题。
- 涉及客户端或服务端业务代码时，主 Agent 必须先创建并委派对应开发子 Agent；不得临时代改或以主 Agent 直接补丁替代委派流程。

## 按职责划分目录（2026-09-04）

- 创建文件前先判断是否存在清晰、稳定的职责边界；同一职责的类型集中到独立目录，便于定位、审查和后续扩展。
- GameServer 角色组件统一放在 `GameServer/Entity/Components`，角色根实体仍放在 `GameServer/Entity`。
- 目录调整必须同步更新项目编译清单、引用路径、项目上下文和测试记录；不为单个孤立文件建立无意义的目录层级。

## 背包分类型容量（2026-09-04）

- `RoleKnapsackTable` 按 `KnapsackType` 的四类可扩容背包分别保存容量：`RolePackPlain`、`RolePackEquip`、`RolePackConsume`、`RolePackMaterial`；四类背包互不聚合。
- 新角色四类背包默认各开启 81 格；`RoleCurrtEquipPack` 仅表示当前穿戴栏，不参与容量扩容。
- 扩容请求必须携带背包类型，GameServer 在逻辑线程校验并只修改指定类型，再将类型和新容量同步 CenterServer 持久化。
- 背包查询回包为每个可扩容类型返回独立容量；客户端按当前枚举类型读取容量并显示空格。
- 旧版单容量记录迁移时，以原容量作为四类初始值并至少补齐到 81 格；迁移过程不得覆盖用户已有更大容量。

## RolePackPlain 独立容器语义（2026-09-11）

- `RolePackPlain` 仅表示普通背包自身，不包含 `RolePackEquip`、`RolePackConsume` 或 `RolePackMaterial` 的物品。
- 客户端背包页签按枚举类型读取对应独立容器，`RolePackPlain` 不再作为“全部背包”聚合页。
- 后续协议、中心服回包和客户端装载逻辑不得新增普通背包与其他分类的重复物品；历史兼容聚合字段如需清理，必须单独安排迁移任务。

## GameServer 客户端消息入口（2026-09-04）

- GateServer 和 CenterServer 到 GameServer 的网络回调统一注册到 `ClientMessageManager`，由其将输入复制为 `GameCommand` 并在逻辑线程按协议、来源分发。
- `RoleManager` 只负责在线角色状态、角色业务、中心服结果处理和固定帧更新，不再承担网络回调或 `ProcessNetMessage` 协议分发。
- `GameModuleManager` 与 `GameModuleBase` 暂予保留：前者维护协议到模块的路由和固定帧顺序，后者提供每个模块的独立队列和限量消费；两者职责不同，不能简单合并。

## GameServer 协议处理器注册表（2026-09-04）

- `ClientMessageManager` 是入站消息队列消费者和处理器注册表，不再承载按命令号堆叠的领域业务分发；网络回调只入队，处理器只在逻辑线程运行。
- 每个 `(GameCommandSource, CommandCode)` 组合注册一个独立同名处理器；相同命令号来自 Gate 与 Center 的语义不同，必须拆为不同 Handler，禁止在单个 Handler 内重新堆积来源分支。
- Handler 根目录为 `GameServer/MessageHandler`，当前按 `Login`、`Role`、`Knapsack` 分域；后续物品、商城、装备、任务创建 `Item`、`Shop`、`Equip`、`Task` 子目录。目录可预建，但没有协议和权威业务时禁止创建空 Handler。
- `GameMessageProtocolRegister` 对应 roserver 的 `CProtocolRegister::Regiter()`：它只注册网络回调和 Handler，不含协议解析或业务逻辑。
- 旧式 `GameServer.csproj` 必须显式列出每个 Handler 文件；一类一文件约束对注册接口、上下文、注册键和每个 Handler 同样生效。

## CenterServer 客户端回包前置转发（2026-09-05）

- CenterServer 消息的客户端可见性必须由 `GameMessageProtocolRegister` 在注册时明确标记：`CenterMessageRoute.ForwardToGate` 或 `CenterMessageRoute.Internal`；禁止在转发器或各 Handler 维护协议号白名单。
- `ClientMessageManager.ProcessCommand` 在逻辑线程查到注册项后，对 `ForwardToGate` 消息必须先按 `GateSessionId` 原样转发给 GateServer，再调用 Handler。Handler 的本地协议解析、状态更新或异常都不得阻断该客户端回包。
- `Internal` 消息不得因携带会话字段转发，例如 `CMD_SyncKnapsackGridCountCode` 只能由 GameServer 处理中心服持久化确认。
- GateServer 是面向 Unity 的协议与业务错误码校验边界；扩容失败先返回权威格子数原包，再发送 `CMD_ErrCode` 触发 Tips；商城购买失败仅保留专用响应，避免重复提示。

## 管理器直接固定帧更新（2026-09-04）

- 业务管理器（包括 `RoleManager`、Redis、场景和活动管理器）使用普通单例类，不继承 `GameModuleBase` 或 `GameManagerModule`。
- `GameGlobal` 是固定帧入口，按 roserver 约定顺序直接调用各管理器的 `Update(delta)`，不通过无业务价值的模块包装层转发。
- 客户端消息管理器负责网络命令队列消费和协议分发，使用自己的最小队列实现；角色等业务管理器不继承通用消息模块基类。
- `GameGlobal` 直接调用各业务管理器的 `Update(delta)`；`GameModuleManager`、`GameManagerModule`、`IGameModule` 和 `GameModuleBase` 不再作为业务管理器的中间层。
## 服务端分类 UID 位布局（2026-09-05）

- 公共库 UID 使用正数 `long`，最高符号位不参与编码；布局固定为 `UIDType 3 位 | 自定义纪元毫秒 40 位 | 逻辑服实例 ID 8 位 | 同毫秒序列 12 位`。
- 分类只使用 `UID_Item`、`UID_Shop`、`UID_Reward`、`UID_Pet`、`UID_Mail` 和 `UID_Common`；`UID_Max = ((1 << 3) - 1)` 仅表示类型位上限，保留值和 `UID_Max` 均禁止生成。无可用业务类别时使用 `UID_Common`。
- 纪元固定为 `2025-01-01 UTC`；每个服务进程必须在生成前显式配置 `0-255` 的实例 ID。生成器在进程内通过锁处理并发、时钟回拨和序列耗尽。
- UID 生成器只负责生成，不提供字段解析。当前物品 `ItemUID` 仍为 `int`/`int32`，不得直接将该 `long` UID 接入数据库或协议；迁移必须单独实施。
- 同一实例 ID 不得在多个并发或刚重启的进程中复用；部署系统需保证实例 ID 唯一。若该前提无法满足，再引入持久化序列或启动代际字段。

## 代码表达式规范（2026-09-06）

- 项目客户端、服务端和公共库代码禁止使用三目运算符（`?:`）。
- 需要条件分支时统一使用 `if/else`，以保证代码可读性并方便后续审查。

## 业务强类型载荷规范（2026-09-14）

- 项目业务代码禁止使用 `object`、`object[]`、`dynamic` 或无约束泛型承载事件数据、领域数据或业务接口参数。
- 每种事件必须定义语义明确的具名 `IEventArg` 实现，例如相机目标变更、输入轴、鼠标按钮和触摸事件分别使用独立类型；禁止恢复通用 `MEventArgs.Data`、`IntValue`、`FloatValue` 等万能字段。
- 第三方库、反射和序列化适配边界确需使用 `object` 时，必须限定在适配层并用中文注释说明原因，禁止向领域层继续透传。

## 集合遍历规范（2026-09-09）

- 项目客户端、服务端和公共库代码禁止使用 `foreach`；新增或修改代码必须使用 `for`。
- 字典或只读枚举集合需要修改遍历对象时，先复制为 `List<T>` 快照再按下标访问；快照中的引用类型元素仍指向原对象，可以修改对象成员。
- `for` 只是遍历语法，不会自动让返回集合可写：修改对象成员与替换集合元素、增删原集合是三种不同操作，必须根据接口职责明确授权。
- 现有历史 `foreach` 不做无差别批量改写；以后触碰相关文件时迁移为 `for`，需要整批迁移时必须单独记录范围、构建和回归结果。

## 类型文件拆分规则（2026-09-07）

- 已有源文件中已经存在多个顶级类、接口、枚举或委托时，保持现状，不为此规则强制拆分，避免无意义的历史文件迁移和用户改动冲突。
- 后续新设计并新增多个顶级类型时，必须按一个类型一个同名源文件创建，并根据职责放入清晰的目录。
- 修改已有多类型文件时，只在确有职责边界、用户明确要求或后续新增类型需要时拆分；拆分前必须由负责 Agent 说明文件范围并更新测试记录。
## 客户端脏标记边界（2026-09-11）

- 客户端脏标记表示“本地模型或展示缓存发生变化”，不表示服务器已接受，也不表示客户端拥有权威状态。
- 服务器是角色属性、装备、背包、技能、Buff、任务和货币的最终权威。服务端回包应用到客户端模型时只触发变更通知和 View 脏位，不进入客户端持久化脏状态。
- 客户端本地保存数据（设置、按键、画质和非权威缓存）可以使用独立的保存脏标记；游戏状态不能通过客户端脏标记直接写服务器，必须走请求、服务器结算和权威回包。
- 脏标记按角色组件和领域拆分，并携带本地版本或变更版本；ACK/异步刷新只能清除对应版本，不能清除期间产生的新变化。
- 优先使用已有 `RoItemComponent` 的细粒度事件做背包 UI 增量刷新；不再用每帧扫描全部角色字段的方式实现脏检查。角色基础属性、装备和其他领域应通过受控 Apply 方法发布不可变变更快照，避免直接修改可变 protobuf 对象绕过通知。

## 实体运行时身份统一（2026-09-15）

- 对照 `Ro_cs422nb`：`INodeData::uuid_ / ROObject::GetUUID()` 是实体实例的运行时唯一身份；`ROObject::GetID()` 是非 UUID 的类型/配置身份（如怪物表 ID、NPC ID），不是场景实例身份。
- 当前项目 `EntityId` 被用于实体世界字典、对象池和异步回调，而 `MObject.UID` 同时存在但未真正分配，形成重复身份模型；该设计确认不符合 RO 语义。
- 后续重构以 `UID` 作为唯一运行时实体身份，世界索引、归属关系、异步会话校验和池复用代际均使用 `ulong UID`；`ConfigId` 保留为配置/表身份，角色持久化 `RoleId` 单独保留。
- `EntityId` 不再作为第二套实体身份。迁移期间如需兼容旧接口，只允许作为 UID 的临时别名，禁止继续独立生成或与 UID 表示不同对象。

## OwnerUID 清理约束（2026-09-15）

- 实体基础层不保留 `OwnerUID`，不得通过 OwnerUID 复制、包装或持久化实体自身 UID。
- 实体之间确有主人、目标、施法者或来源关系时，只能由对应业务组件保存关联实体的 `UID`；基础实体、世界索引和基础快照不承担这类关系字段。

## YCombatEditor 与 RO 技能链接入（2026-10-07）

- Y 编辑器负责制作表现，原 Luban 技能表、MP/CD 与服务器权威施法、skill_uuid、池、当前/旧技能实例列表及统一动画入口继续保留。JX_Attack01 为独立原事件参考，不替换 MJS Q 的原表绑定。
- 编辑资产经完整身份与字段校验导出独立运行 SO；仅支持完整单段原动画和五种定位的单次粒子事件。未接入的多段/裁剪/音频/判定/位移/取消/飞行等非默认数据阻止发布，不显示为已运行生效。
- 2026-10-08用户明确要求删除旧事件后：运行不再读取 AnimationClip.events、不再旧事件回退，移除 PreserveLegacyFxEvents 标志及10070例外。单次 Fire 锁定表现ID与原表时长，配置冷加载只等待该行就绪并按经过时间补发。10070与其他技能统一从编辑轨道导出，蓄力/落剑默认F4/F9@30FPS；允许按已实现粒子语义编辑。原动画/RoleAtkConfig及一次性Editor导入参考保留，粒子独立持有运行资产租约。
- 规则唯一编辑源 SkillCastingRulesConfig.asset 同时导出两端 JSON；客户端角色开放前同步加载，游戏服只读取部署 Config，不从请求或开发机器绝对目录找规则。默认五技能空 InteruptSkill/MoveCasting=false，保留现有行为。
- InteruptSkill 名称与方向按 RO，新技能列表包含可被打断的旧技能；服务器全部权限/费用检查成功后才结束旧 UUID。MoveCasting 目前仅具备客户端水平移动门控；技能中主动起跳固定禁止、空中重力保留。未增加跳取消协议或未实现字段。
- 包元数据从 Unity 6000.0 适配 2023.2，经过隔离与正式工程编译；保留原仓库、MIT 和独立程序集。验证范围与未测实机场景必须按 TEST_REPORT 描述，不能把模拟清单/原生实包子集扩大成完整 Player/YooAsset 发布或句柄冷加载通过。

## 技能配置与代码目录分离（2026-10-08）

- 用户指定的 `LYF_MMO/Assets/GM_Game/Scripts/Data/Skill` 只保存技能配置资产、JSON和对应元数据；运行逻辑与配置类型不得继续写入该配置目录。
- 技能运行源码统一归入 `Scripts/SceneEntity/Skill`，宿主编辑桥接归入 `Assets/GM_Game/Editor/Skill`；保留独立的 LYF.SkillAuthoring.Editor 名称，避免与包内 Global 和游戏 Global 混淆。
- 资源诊断/构建验证统一归入 `Scripts/UI/test/Editor/SkillVerification`，独立Verification程序集与现有合同测试隔离。目录整理搬迁原 `.meta`，保留类名、GUID、程序集名称和原配置路径/内容，不通过重建配置或替换脚本身份完成迁移。

## 技能编辑器代码集中与功能保留

- 用户明确位移、碰撞、飞行物、音效、镜头、流程等现有编辑器功能全部保留，当前目标是源码集中方便阅读并补中文注释；撤销仅暴露动画和粒子的界面简化方案。
- 编辑器源码集中到`LYF_MMO/Assets/GM_Game/Editor/Skill/ActionEditor`，按职责保留分文件，随原meta和asmdef搬迁，保持`Ethan.ActionEditor.Editor`身份；Runtime、配置及原参考仓库保持原位。
- 帮助与版本报告通过仍在嵌入包中的`SkillConfigSO`所属Runtime程序集定位包；不依赖已移入Assets的Editor程序集获得包路径。
- 编辑器轨道保留与本游戏正式执行范围分开说明，本轮目录/注释整理不增加运行轨道、网络协议或服务器结算。

## RO 技能制作轨道扩展（2026-10-10）

- 按真实 RO proto 补齐 RotateList、ShakeList、CameraFxData、CameraPostEffect、ChangeColorList、SkillBarList、MobUnitList、AIEventList、BuffList 九类制作数据，字段及枚举沿原定义，秒字段只在UI边界投影到帧。
- CameraPostEffect 保持原单对象，兼容 Unity 将空内联数据恢复为全零对象；已关闭但含非默认参数的内容仍保留显示和编辑。CameraFxData 为重复列表；Buff 不新增原协议没有的 BuffId。
- 震屏 Frequency 是 Time 秒内的震动次数，沿原 Time/Frequency 间隔语义；各轴及 FOV 振幅允许正负。Index 沿原有符号整数保存，不增加 RO 源码未声明的非负限制。
- 配置制作保留原 Y 窗口与所有已有轨道。新增入口、方法集中 SkillEditorWindow.RoTracks；原模型预览根姿态修复与现有资产不改。
- 本轮不扩展正式游戏执行器或服务器协议。新字段默认空兼容旧发布；未接入的非默认内容明确拒绝正式导出及要求处理器的播放校验。原 Collision/Projectile 并不表示完整复制 RO ResultList/LongAttackData。
- 方案项目分析/RO_SKILL_TRACK_EXTENSION_DESIGN.md；真实测试及UI实测边界见TEST_REPORT，不能将隔离编译/薄边界回归当成Unity原生实测。


## 单次声音与Animation根运动（2026-10-10）

- 既有单动画/粒子导出范围扩展为原Y Audio单帧固定音量及Move Animation水平窗口；end<=key为原Y单次语义，Move含end帧导出max(start,end)+1/FPS并夹到动画末尾。未接入其它轨道非默认门禁保持。
- 正式音源12份由RO Action.bank真样本解码，放MJX目标目录；SkillRuntimeAudioEvent/RootMotionSegment强类型SO数据，声音依赖由配置持有。
- 普通及战斗走跑/起跳/落地六独立作者稿skillID0，SoundComponent按真实ReadyClipPath/NormalizedTime执行；不编造服务器技能。Q正常动作槽退出保留单次声尾，取消/结束回收Reset停止和归还句柄。
- 模型创建一次接管Animator.applyRootMotion；当前占用技能槽且实际clip就绪时采deltaPosition仅XZ，经MMove合并原重力/碰撞一次MoveCharacter。切段清旧归属/buffer，旧_skills不推动新动作，Unbind恢复开关。
- 原Q1 F0–15位移用户稿完整保留；空Q2/Q3/E/F补全动画倍率1，R/1不加位移。原统一动画接口、UUID/池/旧列表/状态窗口/原表保持。RootT仅用于编辑绝对曲线预览，不作为正式移动值。

## 技能多动作声音制作（2026-10-11）

- 声音按真实动画动作阶段和采样自身起音制作，不把粒子帧等同所有声音的固定触发帧；F第二声音可从原粒子F84独立移到F142，原特效/根位移保持。
- 优先既有Audio多事件配置丰富技能，新增短剑原音源、平衡各段音量，不增加随机音高/目标命中音或新运行系统。RO裸PCM不是完整FMOD事件；手曲线峰与RMS包络用于制作依据，不代表真实刀尖或耳感验收。
## 音频资源命名与分类（2026-10-11）

- 移动、跳跃、技能按用途分目录，文件名使用中文动作/阶段说明。共享声音避免把某单一事件帧写成音频固有属性；具体帧/FPS秒由作者稿定义并在使用映射列出。
- 资源重命名与目录迁移必须保原meta/GUID/波形，依赖强引用无需重做技能配置，制作脚本字符串路径同步。备用原声保留，不为目录分类增新的播放规则或collector组。
## Audios英文命名规则（2026-10-11最新用户要求）

- 用户明确撤换中文资源名/目录名，整个Audios采用英文。角色用Characters，技能Skills，移动Movement，跳跃Jump；文件名Movement_、Jump_、Skill_前缀后接用途/阶段，可直接识别类别。代码注释与用户说明仍中文。
- 共享音源维持一份与原GUID，时间按作者事件配置。三旧音源依真实特效Prefab引用标Skill04蓄力/释放、Skill02蓄力，不按DragonAttack原名猜角色。整理不更改声音时点、音量或播放系统。
## 技能编辑器声音独立行与拖动语义（2026-10-11）

- Sound保持独立表现轨道，原fxList仍保存粒子/声音。旧缺Sound布局有声音时仅恢复显示，不自动写布局；显式contentKind区分空Audio/Visual，Legacy推断兼容。Fx/Warning仍统一视觉行；音效不参与其高度和lane排序。
- Interaction的MovedEventEnd统一十类事件末帧平移：正数点跟随起点，0/负旧单次哨兵保持，区间保持跨度。音效仅在Sound复制粘贴，不跨入Fx菜单。
- 声音正式单次门禁仍保留；ReadAudio诊断区间资源/起末帧与修正入口。参考项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md和项目修改日志/2026-10-11-SKILL-TRACK-DRAG-FIX-CHANGELOG.md；回归和现场边界见TEST_REPORT。

## 技能编辑器暂停与双重声音对齐

- 用户明确要求暂停逐帧调整触发时刻与音频波形内部峰值对齐，两种入口同时提供。播放／暂停保留预览姿态与声音采样位置；停止单独回收。粒子与飞行物按配置FPS的固定时间轴步模拟，不再脱离播放头读取墙钟独立推进。
- 音频包络来自真实PCM全部声道的有符号极值，完整长度用于显示，绝不转成单次事件结束帧或延长正式技能时长。Alt选点优先于主峰，对齐按配置FPS量化移动触发帧，超界拒绝，沿原Undo链保持点哨兵及区间跨度。
- 特效内嵌音源只接管克隆中活跃、启用且原playOnAwake=true的组件，保留原参数，不启用原来不自动播放的声音。原资源和用户技能稿不为预览自动改写，正式音频区间发布限制延续。

## 多段特效预览结束与有限收尾

- 正式END与预览尾段分别计时：已触发表现的延迟发射和尾音可以超出正式END完成预览，动画／根运动固定在正式末帧。正式END之后的新事件不得因尾段延长而补发；显示和播放头导航扩尾不改作者制作范围、END或配置资产。
- 只读粒子参数与真实数量共同判定编辑器有限收尾，避免手工Simulate＋Pause时IsAlive常真而永不循环；包含活跃子层延迟、发射、尾粒、子发射器和倍率，保留非零尾粒。原失活节点不激活，资源stopAction不因未经证实的猜测修改。
- 核查其他轨道沿既有能力：声音／手动飞行／提示尾段统一等待；碰撞／位移参考按原区间，镜头和RO制作轨道不在本轮新增执行器。
## RO 技能回调注册与强类型计时器（2026-10-11）

- 用户明确要求 FireEvents 同 RO 接口名、timeline 使用 MTimerMgr；正式声音/粒子通过 FireEvents→SetTimer→OnProcessTimer→InnerProcessTimer 注册与派发，票据沿 AddedTimerToken/KillTimerAll 管理。
- RO object[] 回调载荷不符合本项目业务强类型规则，改为具名 MSkillTimerArgs，保留相关函数名；回调枚举 EFx=10、EAudio=11 沿原值，只覆盖已有正式运行能力。
- 全局毫秒计时与技能累计时钟使用同一实体帧间隔；零/过期事件同步补发，正数至少一毫秒异步。同毫秒注册稳定顺序，模型迟到独立 pending，不使用资产逐帧扫描模拟 timeline。
- UUID 之外携带施法代次，Reset 先注销票据并更新代次；同步回调返回后核对冻结资产及原代次，阻止同 UUID/池对象重入继续旧批次。Q 动作槽退出不等同真正取消，原旧实例和声音/粒子尾迹语义保留。
- 本轮不扩大服务器权威逻辑或编辑器未接入轨道执行范围；实现及验证说明归档 `项目修改日志/2026-10-11-SKILL-TIMELINE-RO-CHANGELOG.md`。


## 2026-10-11 技能导出和后动画表现契约

- 修复完整源动画末帧误判，不通过清零/回退用户稿来绕过校验；真实剪辑覆盖按源FPS计算。
- 原表DurationMs、Runtime.DurationSeconds、动作窗与根运动保持各自原用途。新增TimelineDurationSeconds仅表示制作末触发范围，零字段兼容原资产；Fx/Audio使用此范围，波形和粒子自然尾迹不作为制作END。
- 声音采用当前预览已实际实现的固定音量完整单次语义，作者endKeyNumber保留制作标记，不在本次导出修复新增RO区间停止/循环/随机/变速功能。
- 透传现有SkillCastEndNtf.Interrupted，不新增协议。正常End释放角色动作但保留原_skills施法实例，须注册完成、未来票据/pending/声音空才回池；打断/拒绝/清场即刻清理。RO宽限含完整音尾，预测超时仅取消仍未确认的实例。
- 不自动写用户作者/Runtime资产或改原资源。发布依旧编辑态原按钮显式进行；运行态禁用导出，失败说明显示在检查器。
- 实现与验证：项目修改日志/2026-10-11-SKILL-EXPORT-FIX-CHANGELOG.md；使用说明：项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md。

## 2026-10-11 用户明确的技能 Update 职责

- 时间线注册只能由Start/CacheReady一次FireEvents承接；到期声音/粒子由MTimerMgr→OnProcessTimer→InnerProcessTimer消费，模型迟到的已到期pending由真实MModel.PresentationReady补发。MArtSkill.Update仅基类时钟与PruneAudio回收，不登记、查询配置、补播或绑定根运动；guard只能防重，不能代替职责迁移。
- 缓存通知暖终态同步、冷终态一次、失败结束等待；资源包取得后Global一次NotifyResourcePackageReady启动旧记录，禁止转移到其它Update/helper继续轮询。Reset解除资源和模型监听，UUID/施法代次、旧缓存记录身份及模型session保护同步重入、池复用和旧加载完成。
- 当前动作根所有者在TriggerAnim/模型就绪登记；Collector按真实Clip/配置/当前槽身份门控Animator.deltaPosition，MMove合并水平增量与重力后提交碰撞。RO ECharge是配置曲线位移事件，不能把它当作动画rootdelta，也不为接口对齐删除既有根运动功能或伪造未接入Charge业务。
- MSkillTimerArgs强类型载荷、票据取消、TimelineDurationSeconds独立范围、自然End保尾、Interrupted真正清理、Q旧新实例和粒子独立租约保持。缺固定挂点保pending，带齐层级模型重载Ready补发；动态组装无正式入口，本轮不扩新轮询。
- 正式分析 `项目分析/RO_SKILL_TIMELINE_RESPONSIBILITY.md`，最终修改和验收 `项目修改日志/2026-10-11-SKILL-TIMELINE-RESPONSIBILITY-CHANGELOG.md`；实机GUI/听感/网络边界在TEST_REPORT如实保留。

## 2026-10-11 RO 程序位移与已有动画根运动并存

- 普通RO技能配置位移实际为ChargeList累计曲线，不是Animator根增量；沿FireEvents→MTimerMgr(ECharge=13)→OnProcessTimer→InnerProcessTimer→charge→MDisplacementComponent。onStart/onStop/Update/StopDisplacement保接口名，载荷改为项目要求的具名强类型，角度单位度。
- 已有Animation驱动和Collector算法保留。原作者moveSegmentList的Program前向/Self/固定方向/水平曲线子集投影运行ChargeList，不新增作者Schema或空RO轨道；两来源重叠拒绝。程序段At/End按FPS直接换秒，末帧不加一，零时长瞬时。
- 保原Y三种Clamp/Stop/Continue及maxDistance前向硬上限，Continue允许回拉；这是本项目制作语义适配，不声称原RO有同一硬上限。目标/纵向/动态方向/忽略重力或碰撞等未实现非默认参数拒绝。
- 位移驱动由MMoveComponent独占，按冻结全局毫秒原点及Init会话评价累计差量，持续插值不放技能Update。输入、重力、动画根、程序差量统一一次MoveCharacter；侧撞结束，不拉终点。自然离槽与Interrupted都停运动，声音/FX自然保尾契约保持。
- 冷配置/模型只移动当前窗剩余差量，过窗跳过；同步替换真实GO丢旧pending并重建基线；UUID、开始代次、当前槽、计时会话共同阻止旧运动影响新施法。旧ChargeList=null按空兼容，不自动重导任何用户资产。
- 参考分析、最终接口与验收归档项目分析/RO_SKILL_MOTION_IMPLEMENTATION.md、项目修改日志/2026-10-11-RO-SKILL-MOTION-CHANGELOG.md，实际验证边界见TEST_REPORT。

## 2026-10-11 用户纠正：位移组件必须由实体挂接

- 本节替代上节“位移驱动由MMoveComponent独占”的所有权选择。RO实体用MComponentMgr.CreateComponent挂MDisplacementComponent，当前项目照实体组件结构实现：继承EntityComponentBase、普通容器注册、实体_displacementComp快捷引用、统一初始化/更新/卸载/池回收；MMove不构造或拥有位移对象。
- 普通组件运动优先阶段先推进位移，再原专用Move/Buff/Skill/Machine，后普通阶段不重复位移。保现有Move先Skill结束顺序，不用ConsumeDelta隐藏更新或MMove手动驱动，避免一帧滞后、双推进及末帧丢量。
- 共享EntityComponentType追加Displacement=32，不借旧枚举类型，旧值及既有外部namespace身份保持，不新增namespace。构建产物同步当前Common/Net两相同MMoRpgCommon.dll，meta/GUID保持；不改协议和其它后端业务。
- 原算法、冻结身份/时长/计时会话、冷模型与GO、曲线三策略、单次重力碰撞及FX/Audio职责保持。直接卸载必须立即停止，不等后续Update。独立当前189运行及真实实体生命周期验证已通过，两端真实源编译零错，主734项SHA一致；记录项目修改日志/2026-10-11-DISPLACEMENT-ENTITY-COMPONENT-CHANGELOG.md及TEST_REPORT。本轮不把旧原生64统计当当前验收。

## 2026-10-11 RO 同名冷却与脱手生命周期接口

- 采用RO Update(float)、bool MSkill.Update、IsEnd、Cease、OnSkillEnd和同名双CD容器/Get/Set/Clear/工厂借还。普通CD键技能主表ID，组CD键CooldownGroupId；CD与CurrentCD为秒，设置时两者刷新为传入值，零/无效清除归池，重复同值也校正剩余。
- 已安装Core与组件共用唯一CD记录，正式预测/权威/回滚/升级/CanStart/UI一致；旧Core截止仅兼容未安装独立核心。不缩放实时钟保持，CD按realtime差量递减/查询，不能逐字用缩放实体fDeltaT取代原权威CD契约。
- 常驻组件统一当前及脱手各推进一次，动作OnUpdate只判断IsEnd；池化值快照保存原对象及已有FireGeneration，防回调删兄弟或重借后误用旧false。保Move先Skill离槽末帧顺序，不新增frame代次或第二Update表现消费。
- Cease默认false与true都实际终止、注销及归池；cleanUp无额外本地策略，仅同名兼容。自然End仍经原ApplyServerEnd(false)保晚Timer/GO/声尾，完整收尾后才Cease，动作离槽不等生命周期终止。
- 独立253运行/273真实源零错、主613项SHA一致；无剩余阻断。实际范围、生成工程缺before限制及现场未验见项目修改日志/2026-10-11-RO-SKILL-CD-LIFETIME-CHANGELOG.md与TEST_REPORT；分析项目分析/RO_SKILL_CD_LIFETIME.md。不扩服务器协议、表、资源发布或共享DLL。
# RO 技能不增加确认等待（2026-10-11）

- 用户明确要求以真实 RO 为准，不使用 `_pendingSkillIds`、Core 每笔确认事务/Deadline/ExpirePrediction 或 HasPendingPrediction 施放门禁；本条覆盖下文历史预测确认超时回滚方案。
- 本地输入立即播放并设置 CD，后续施放由动作状态、CD、MP 等实际条件决定。服务器结果仍校正最新请求的 CD/MP，拒绝只结束匹配 UUID；旧结果不能覆盖新段。
- 真实 RO 实例 Update 的预表现 2 秒/正式 10 秒存活上限保留，使用累计技能时间；本项目制作末触发和完整音尾仍计入合法范围。不依赖服务器 End 才释放已完成的脱手实例，未完成表现按既有自然尾契约推进。
- 施法代次保护真实异步回调和池复用，不作为服务器确认队列。分析见 `项目分析/RO_SKILL_CD_LIFETIME.md`，本次实施日志见 `项目修改日志/2026-10-11-RO-SKILL-NO-CONFIRM-WAIT-CHANGELOG.md`。

# 参考移植忠实与简洁约束（2026-10-11，最新）

- 用户明确要求参考其他项目时主要照原代码实现，不自行扩展一堆需求外机制。已写入 AGENTS.md 和 AIconfig/workflow.md：沿用结构/接口/职责/时序，只作必要接入适配；扩展功能由用户提出后再做，审查不为人造极端场景强加包装。
- 本条覆盖历史 MSkillUpdateEntry 原引用/代次快照要求：MSkillComponent 按 RO 当前动作 OnUpdate 与常驻列表 Update 分工，列表倒序直接调用 skill.Update，false 就 Cease(true)，不用快照、Contains 或代次冻结。
- `_skills` 表示独立运行或已离开主动作槽的存活技能，不能一概称“脱手技能”；异步时间线原有施法代次保护不属于此循环包装，本轮不扩改。
- 实施日志 `项目修改日志/2026-10-11-RO-SKILL-UPDATE-SIMPLE-CHANGELOG.md`，证据 `_temp/ro-skill-update-simple-20261011/`。

# 技能 CD 与表现实例回收独立（2026-10-11，修复约束）

- 用户反馈全部技能可无限释放，独立before已实际复现：10秒CD、0.5秒表现，0.61秒正常回池后CD=0，第二次快捷键成功发包；根因为Cease→Reload把未回包的正常收尾当成预表现失败。
- 本条覆盖历史“预表现Cease调用Reload清单CD”的移植选择。角色CD由施放写入、倒计时、明确清场及权威结果决定，正常表现结束/归池/资源清理不能重置CD；不等确认不等于不受CD约束。
- 最小删除两源清CD联动，不恢复确认队列、额外门禁或快照。RO原失败清理不能与本地正常早回池混用；必要兼容修正以已复现事实为依据。
- 日志 `项目修改日志/2026-10-11-SKILL-CD-EARLY-RESET-CHANGELOG.md`，before/final证据 `_temp/skill-cd-early-reset-20261011/`。

