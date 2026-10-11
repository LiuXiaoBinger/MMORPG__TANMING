# GameServer 消息处理器架构设计

## 目标

将 `GameServer/manger/ClientMessageManager.cs` 中按命令号编写的 `switch` 业务分发拆分为“一条消息对应一个处理类”。`ClientMessageManager` 仅保留网络回调入队、逻辑线程消费、注册表查询和异常隔离，不再引用登录、角色或背包领域的业务入口。

本设计参考 `F:/UnityPro/ro/Ro_cs422nb/roserver/gameserver/ptcregister.cpp` 的 `CProtocolRegister::Regiter()`：注册器只将协议实例登记到全局表，具体业务在 `protocol/<domain>/` 的同名处理文件实现。例如 `RpcC2G_UseItem` 在注册表中注册，其实现位于 `protocol/item/rpcc2g_useitem.cpp`。

## 必须保留的线程模型

1. GateServer 或 CenterServer 的 Socket 回调仍只调用 `ClientMessageManager.OnServerCommand` 或 `OnClientCommand`，复制输入为 `GameCommand` 并进入 `GameCommandDispatcher` 总队列。
2. `GameGlobal.ProcessNetMessage` 先从总队列投递到 `ClientMessageManager` 私有队列，再由 `ProcessPendingCommands` 在 GameServer 主逻辑线程处理。
3. `ClientMessageManager` 使用 `(GameCommandSource, CommandCode)` 查询注册表，找到一个处理器后调用其 `Handle`；未知来源、未知命令或重复注册必须记录错误并拒绝处理。
4. 处理器不得在 Socket 回调线程运行；处理器中对角色、背包和会话索引的访问均发生在逻辑线程。
5. 处理器只做协议解析、来源/会话校验、调用领域服务或控制器、发送结果。复杂规则继续留在 `RoleManager`、`KnapsackService` 等领域类，禁止把业务再堆回处理器。

## 目录与类型

根目录新增 `LYF_Server/GameServer/MessageHandler/`。该目录只存放协议处理器和注册基础设施，不和 `Entity`、`Service` 或 `manger` 混放。

```text
GameServer/
  MessageHandler/
    IGameMessageHandler.cs
    GameMessageHandlerContext.cs
    Login/
      LoginGameServerHandler.cs
      CreateRoleHandler.cs
      StartGameHandler.cs
    Role/
      EnterWorldHandler.cs
      RoleSkillInfoHandler.cs
    Knapsack/
      RoleKnapsackInfoHandler.cs
      OpenKnapsackGridHandler.cs
      SyncKnapsackGridCountHandler.cs
    Item/                 # 仅预建目录，不创建空 Handler
    Shop/                 # 仅预建目录，不创建空 Handler
    Equip/                # 仅预建目录，不创建空 Handler
    Task/                 # 仅预建目录，不创建空 Handler
```

`IGameMessageHandler` 是一个顶级接口，独立文件，建议定义：

```csharp
bool CanHandle(GameCommandSource source, ushort commandCode);
void Handle(GameMessageHandlerContext context);
```

`GameMessageHandlerContext` 是一个顶级类，独立文件，封装当前不可变 `GameCommand`、由命令恢复的 `BasePackage`、`ServerBase Transport`、来源和可选 Gate `Session`。它是处理器获取输入与回包通道的唯一上下文，不承担领域状态。处理器自行处理协议反序列化异常，并以已有 `CmdCode` 或日志反馈。

处理器的粒度是“一个协议一个业务处理类”，文件名不携带 Gate 或 Center 来源。注册键仍包含来源和命令号，避免两条网络链路串包；同一协议来自 Gate 与 Center 时，注册器将同一个处理器实例登记两次，处理器通过 `GameMessageHandlerContext.Source` 分支校验和调用对应领域入口。

## ClientMessageManager 改造边界

- 保留：单例、`ConcurrentQueue<GameCommand>`、积压上限、`EnqueueNetMessage`、两个网络回调、`ProcessPendingCommands`、每条命令异常隔离。
- 新增：私有 `Dictionary<GameMessageHandlerKey, IGameMessageHandler>` 注册表，`Register` 方法和 `RegisterAllHandlers` 私有启动方法。`GameMessageHandlerKey` 若需要作为顶级结构体，必须独立同名文件；也可直接使用不会引入顶级类型的复合字符串键，但首选显式类型以避免来源冲突。
- 删除：`ProcessCommand` 中的 `switch`，以及 `DispatchLogin`、`DispatchCreateRole`、`DispatchStartGame` 三个私有业务转发方法。
- `GameServerApp` 不再逐条向 `NetServer`/`NetClient` 绑定 `ClientMessageManager`。新增独立的 `GameMessageProtocolRegister`（放在 `MessageHandler` 根目录）统一执行网络回调注册，形式对应 `CProtocolRegister::Regiter()`；它只写 `server.RegistCommand`、`client.RegistCommand` 和 `ClientMessageManager.Register`，不含处理业务。

注册器必须显式登记每个方向：一个命令若同时来自 Gate 与 Center，必须分别注册网络回调和处理器。启动顺序为：创建网络对象 -> `GameGlobal.Init()` -> `GameMessageProtocolRegister.Register(server, client)` -> 主循环。不得保留未使用的 `RoleManager roleManager` 局部变量。

## 当前真实消息与处理器清单

以下清单以当前 `NetDefine.cs`、`.proto`、`GameServerApp.cs` 和 `ClientMessageManager.cs` 为准。仅这些条目应在本轮创建 Handler 文件。

| 来源 | 命令 | 处理器 | 目标领域入口 |
| --- | --- | --- | --- |
| GateServer | `CMD_LoginGameServerCode` | `Login/LoginGameServerHandler` | `Game_LoginCtrl.HandleClientLoginGameServer` |
| CenterServer | `CMD_LoginGameServerCode` | `Login/LoginGameServerHandler` | `Game_LoginCtrl.HandleCenterLoginGameServer` |
| GateServer | `CMD_CreateRoleCode` | `Login/CreateRoleHandler` | `Game_LoginCtrl.HandleClientCreateRole` |
| CenterServer | `CMD_CreateRoleCode` | `Login/CreateRoleHandler` | `Game_LoginCtrl.HandleCenterCreateRole` |
| GateServer | `CMD_StartGameCode` | `Login/StartGameHandler` | `Game_LoginCtrl.HandleClientStartGame` |
| CenterServer | `CMD_StartGameCode` | `Login/StartGameHandler` | `Game_LoginCtrl.HandleCenterStartGame` |
| GateServer | `CMD_EnterWroldCode` | `Role/EnterWorldHandler` | `RoleManager.HandleClientEnterWorld` |
| CenterServer | `CMD_RoleSkillInfoCode` | `Role/RoleSkillInfoHandler` | `RoleManager.HandleCenterRoleSkillInfo` |
| CenterServer | `CMD_RoleKnapsackInfoCode` | `Knapsack/RoleKnapsackInfoHandler` | `RoleManager.HandleCenterRoleKnapsackInfo` |
| GateServer | `CMD_OpenKnapsackGridCode` | `Knapsack/OpenKnapsackGridHandler` | `RoleManager.HandleClientOpenKnapsackGrid` -> `KnapsackService.OpenGrid` |
| CenterServer | `CMD_SyncKnapsackGridCountCode` | `Knapsack/SyncKnapsackGridCountHandler` | `RoleManager.HandleCenterSyncKnapsackGridCount` |

`CMD_SyncRoleEnterWorldCode` 与 `CMD_SyncotherOnlineCode` 当前仅由 GameServer 出站发送，未发现 GameServer 入站业务处理，不创建入站 Handler。`CMD_ErrCode` 和登录服相关命令也不属于当前 GameServer 消费范围。

## 后续协议项，不得创建空处理器

用户提出的购买物品、使用物品和装备强化，在参考 roserver 中存在 `RpcC2G_RequestBuyShopItem`、`RpcC2G_UseItem`、`RpcC2G_EquipRefineUpgrade` 等独立处理器；但当前项目的 `NetDefine.cs`、`RequestEntity.proto` 和 `ResultEntity.proto` 没有对应命令号、请求/结果消息、物品持久化事务或扣费规则。

因此本轮只预建 `Shop`、`Item`、`Equip` 目录；禁止创建可编译但不执行权威校验的空 Handler。后续开发必须先由策划与系统架构师定义：协议号、请求字段、结果字段、物品/货币/装备表与数据库事务、幂等请求序号、错误码和客户端回包，再按本目录新增一个协议一个 Handler。

## GameServer.csproj 编译策略

项目为旧式 .NET Framework 4.8 显式 `Compile Include` 工程。每新增一个 `.cs` 文件都必须添加一条精确的 `<Compile Include="MessageHandler\\...\\同名文件.cs" />`，且一个顶级类、接口、枚举、委托或结构体一个同名文件。不可改用无边界的递归通配符，以免将 `_temp`、生成文件或测试文件误编译进正式服务端。

开发完成后，测试 Agent 应检查：注册表中 11 个键唯一、每个网络注册键都有处理器、处理器不含业务 `switch`、未知来源/命令拒绝、源码与 csproj 清单一致、一类一文件，以及完整 `LYF_Server.sln` 构建。

## 非目标

- 本轮不改变任何协议号、protobuf 字段、数据库表或客户端逻辑。
- 本轮不把 `Game_LoginCtrl`、`RoleManager`、`KnapsackService` 的领域业务复制进 Handler。
- 本轮不实现购买、使用物品、装备强化、任务等尚未定义协议的功能。
