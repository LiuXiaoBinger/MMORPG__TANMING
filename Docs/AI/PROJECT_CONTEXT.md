# 项目工作上下文

## [INDEX][协作][DOCS-READ] 启动时定向读取

- 先遵守 `AGENTS.md`。会话上下文已足够时，只读本段和 `[DOCS-ROUTES]`；不扫描旧任务、五类文件全文、两种语言或归档。
- 缺少上下文时，在相关英文入口按主题/编号搜索标题，只读命中段。入口中缺失时，搜索 `项目修改日志/协作记录归档/2026-10-26/INDEX.md`，读取准确的归档段落。保留的历史状态只是带日期的证据，不证明当前状态；稳定规则以 AGENTS 与相关技能为准。
- 每次任务结束后应用 `.agents/skills/task-log-policy/SKILL.md`：先列拟写日志路径、内容、理由和未验证边界，再询问开发者是否写入。收到明确同意后才更新正式任务记录；已有明确日志授权时沿用授权，不重复询问。对输入未变或重复事实建议不写，但由开发者决定。直接授权的业务、技能或规则修改照常完成；如实区分已写入、待确认和开发者拒绝写入。
- Codex 维护英文事实，子 Agent 只同步/检查修改的中文段并交付范围与证据。未改历史原文字节保留在归档。按 AGENTS 报告实际使用的技能与可观测 token 覆盖边界。

## [STRUCTURE][协作][DOCS-ROUTES] 文件职责与恢复入口

| 需要的信息 | 仅在相关时读取 |
|---|---|
| 稳定结构与读取路由 | `Docs/AI/PROJECT_CONTEXT.en.md` 的对应主题 |
| 当前变化与续作交接 | `Docs/AI/CURRENT_TASK.en.md` 的对应任务 |
| 检查与未解决边界 | `Docs/AI/TEST_REPORT.en.md` 的同一任务 |
| 长期实现决策 | `Docs/AI/DECISIONS.en.md` 的对应决策 |
| 新调查与证据 | `Docs/AI/QUERY_TASKS.en.md` 的对应查询 |

- 同名中文版本供开发人员使用。完整逐项文件清单/Git 回执只放一份在 `项目修改日志/`，分析放 `项目分析/`，工具原始输出放 `_temp/`，各职责之间不复制正文。
- 恢复路径：`项目修改日志/协作记录归档/2026-10-26/INDEX.md` → 原文快照和行段；哈希见同目录 `manifest.json`。归档失败、未解决边界和执行身份仍可恢复；本次整理没有重新验证历史。
- 客户端 `LYF_MMO/Docs/AI` 入口已指向根入口。仅在相关主题需要时读取其归档的 2026-10-24/25 UI/字体证据，其中包含旧根入口缺少的较晚事实。

## [CONTEXT][协作][QWEN-LIGHT-20261023] 2026-10-23 Qwen 轻量模型精确约束

- 当前轻量任务统一使用本机 Ollama 模型 `qwen3:8b-light`，调用方必须显式传完整模型名，不得静默替换为 `qwen3:8b` 或 `qwen3:8b-cpu`。
- 2026-10-23 本机 Ollama 标签检查已返回 `qwen3:8b-light`；指定标签不可用时仍按一次尝试规则切豆包或 WorkBuddy，不重复尝试 Qwen，也不无记录换模型。
- 这是当前生效配置约束；历史 CT-455 对 `qwen3:8b` 的测试证据保持原样，不代表当前默认模型。

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
