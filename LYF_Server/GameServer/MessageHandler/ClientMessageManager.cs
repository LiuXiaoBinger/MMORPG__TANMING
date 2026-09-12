/// <summary>
/// 客户端消息入口。
/// Gate/Center 网络回调只负责把命令投递到总队列，本模块在逻辑线程按来源和协议分发。
/// </summary>
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

public sealed class ClientMessageManager : IContainer
{
    private static readonly ClientMessageManager _instance = new ClientMessageManager();
    private const int MaxPendingCommands = 10000;
    private const int MaxCommandsPerUpdate = 3000;
    private readonly ConcurrentQueue<GameCommand> _pendingCommands =
        new ConcurrentQueue<GameCommand>();
    // 注册表只描述协议到处理器的映射，业务逻辑由独立 Handler 承担。
    private readonly Dictionary<GameMessageHandlerKey, GameMessageRegistration> _handlers =
        new Dictionary<GameMessageHandlerKey, GameMessageRegistration>();
    private int _pendingCount;

    /// <summary>客户端消息管理器单例。</summary>
    public static ClientMessageManager Instance
    {
        get { return _instance; }
    }

    private ClientMessageManager()
    {
    }

    public void OnInit()
    {
    }

    /// <summary>当前客户端命令积压量。</summary>
    public int PendingCommandCount
    {
        get { return _pendingCount; }
    }

    /// <summary>
    /// 注册一条来源和协议号唯一的消息处理器。
    /// 重复注册会被拒绝，避免启动配置覆盖已有业务处理逻辑。
    /// </summary>
    public bool Register(GameMessageHandlerKey key, IGameMessageHandler handler, CenterMessageRoute centerRoute)
    {
        if (handler == null)
        {
            LogMsg.Info("消息处理器注册失败：处理器为空", LogMsgType.Error);
            return false;
        }

        if (key.Source != GameCommandSource.GateServer &&
            key.Source != GameCommandSource.CenterServer)
        {
            LogMsg.Info("消息处理器注册失败：未知来源 " + key.Source, LogMsgType.Error);
            return false;
        }

        if (key.Source != GameCommandSource.CenterServer && centerRoute != CenterMessageRoute.Internal)
        {
            LogMsg.Info("消息处理器注册失败：只有 CenterServer 消息可以转发网关，命令=" +
                key.CommandCode, LogMsgType.Error);
            return false;
        }

        if (!handler.CanHandle(key.Source, key.CommandCode))
        {
            LogMsg.Info("消息处理器注册失败：处理器声明与注册键不一致，命令=" +
                key.CommandCode, LogMsgType.Error);
            return false;
        }

        if (_handlers.ContainsKey(key))
        {
            LogMsg.Info("消息处理器重复注册，来源=" + key.Source + "，命令=" +
                key.CommandCode, LogMsgType.Error);
            return false;
        }

        _handlers.Add(key, new GameMessageRegistration(handler, centerRoute));
        return true;
    }

    /// <summary>总消息队列消费后，在逻辑线程投递到客户端消息队列。</summary>
    public bool EnqueueNetMessage(GameCommand command)
    {
        if (command == null)
        {
            return false;
        }

        int pendingCount = Interlocked.Increment(ref _pendingCount);
        if (pendingCount > MaxPendingCommands)
        {
            Interlocked.Decrement(ref _pendingCount);
            LogMsg.Info("客户端消息队列已满，拒绝命令 " + command.CommandCode, LogMsgType.Error);
            return false;
        }

        _pendingCommands.Enqueue(command);
        return true;
    }

    /// <summary>主循环消费客户端命令，业务逻辑只在此线程执行。</summary>
    public int ProcessPendingCommands()
    {
        int processedCount = 0;
        GameCommand command;
        while (processedCount < MaxCommandsPerUpdate && _pendingCommands.TryDequeue(out command))
        {
            Interlocked.Decrement(ref _pendingCount);
            try
            {
                ProcessCommand(command);
            }
            catch (Exception ex)
            {
                LogMsg.Info("客户端命令处理失败，序号=" + command.Sequence + ": " + ex, LogMsgType.Error);
            }

            processedCount++;
        }

        if (_pendingCount > 0)
        {
            LogMsg.Info("客户端消息队列积压: " + _pendingCount, LogMsgType.Warn);
        }

        return processedCount;
    }

    /// <summary>Gate 网络回调只入队，不在 Socket 线程执行业务。</summary>
    public void OnServerCommand(ServerBase serverBase, BasePackage basePackage)
    {
        GameGlobal.Instance.EnqueueGameCommand(GameCommandSource.GateServer, serverBase, basePackage);
    }

    /// <summary>CenterServer 回调统一进入客户端消息队列。</summary>
    public void OnClientCommand(ServerBase serverBase, BasePackage basePackage)
    {
        GameGlobal.Instance.EnqueueGameCommand(GameCommandSource.CenterServer, serverBase, basePackage);
    }

    /// <summary>逻辑线程按来源和协议键查找独立处理器。</summary>
    private void ProcessCommand(GameCommand command)
    {
        if (command == null)
        {
            return;
        }

        if (command.Source != GameCommandSource.GateServer &&
            command.Source != GameCommandSource.CenterServer)
        {
            LogMsg.Info("拒绝未知消息来源，序号=" + command.Sequence + "，来源=" +
                command.Source, LogMsgType.Error);
            return;
        }

        if (command.CommandCode < ushort.MinValue || command.CommandCode > ushort.MaxValue)
        {
            LogMsg.Info("拒绝非法协议号，序号=" + command.Sequence + "，命令=" +
                command.CommandCode, LogMsgType.Error);
            return;
        }

        GameMessageHandlerKey key = new GameMessageHandlerKey(
            command.Source, (ushort)command.CommandCode);
        GameMessageRegistration registration;
        if (!_handlers.TryGetValue(key, out registration))
        {
            LogMsg.Info("拒绝未注册消息，序号=" + command.Sequence + "，来源=" +
                command.Source + "，命令=" + command.CommandCode, LogMsgType.Error);
            return;
        }

        GameMessageHandlerContext context = new GameMessageHandlerContext(command);
        if (command.Source == GameCommandSource.CenterServer &&
            registration.CenterRoute == CenterMessageRoute.ForwardToGate)
        {
            // 所有客户端可见的 Center 回包必须在任何 Handler 解析或业务校验前原样交给网关。
            // 即使后续本服处理失败，网关仍能向 Unity 返回协议错误或业务错误码。
            GameCenterResponseForwarder.TryForwardToGate(context);
        }

        registration.Handler.Handle(context);
    }
}

// ============================================================
// Center 消息路由与处理器抽象
// ============================================================

/// <summary>
/// CenterServer 消息在 GameServer 中的路由归属。
/// 此配置只描述消息是否最终交给网关，不改变具体 Handler 的业务处理职责。
/// </summary>
public enum CenterMessageRoute
{
    /// <summary>仅供 GameServer 内部消费，例如持久化确认。</summary>
    Internal = 0,

    /// <summary>客户端可见回包，必须按原 Gate 会话转发。</summary>
    ForwardToGate = 1
}

/// <summary>
/// GameServer 入站协议处理器。
/// 每个协议由一个独立实现类处理。
/// 同一协议来自不同服务器时，由处理器根据上下文来源分支处理。
/// </summary>
public interface IGameMessageHandler
{
    /// <summary>确认处理器接受注册的来源和协议号。</summary>
    bool CanHandle(GameCommandSource source, ushort commandCode);

    /// <summary>在 GameServer 逻辑线程中处理已经入队的命令。</summary>
    void Handle(GameMessageHandlerContext context);
}

/// <summary>
/// 消息处理器注册键，来源和协议号共同决定唯一处理语义。
/// </summary>
public struct GameMessageHandlerKey : IEquatable<GameMessageHandlerKey>
{
    public GameCommandSource Source { get; private set; }
    public ushort CommandCode { get; private set; }

    public GameMessageHandlerKey(GameCommandSource source, ushort commandCode)
    {
        Source = source;
        CommandCode = commandCode;
    }

    public bool Equals(GameMessageHandlerKey other)
    {
        return Source == other.Source && CommandCode == other.CommandCode;
    }

    public override bool Equals(object obj)
    {
        return obj is GameMessageHandlerKey && Equals((GameMessageHandlerKey)obj);
    }

    public override int GetHashCode()
    {
        return ((int)Source * 397) ^ CommandCode;
    }
}

/// <summary>
/// 一条 GameServer 协议注册的处理器和 CenterServer 路由元数据。
/// </summary>
public sealed class GameMessageRegistration
{
    public IGameMessageHandler Handler { get; private set; }

    public CenterMessageRoute CenterRoute { get; private set; }

    public GameMessageRegistration(IGameMessageHandler handler, CenterMessageRoute centerRoute)
    {
        Handler = handler;
        CenterRoute = centerRoute;
    }
}

/// <summary>
/// 单条入站命令在逻辑线程中的处理上下文。
/// 处理器只能通过该上下文读取命令快照、回包链路和目标网关会话。
/// </summary>
public sealed class GameMessageHandlerContext
{
    /// <summary>
    /// 当前正在处理的入站命令；命令为空时表示未提供有效请求。
    /// </summary>
    public GameCommand Command { get; private set; }

    /// <summary>
    /// 由命令还原出的协议包快照，用于读取协议号和请求数据；命令为空时为 <c>null</c>。
    /// </summary>
    public BasePackage Package { get; private set; }

    /// <summary>
    /// 接收入站命令的网络传输对象，用于逻辑线程向来源服务回包；命令为空时为 <c>null</c>。
    /// </summary>
    public ServerBase Transport { get; private set; }

    /// <summary>
    /// 命令的来源服务标识，用于区分 GateServer、CenterServer 等处理路径。
    /// </summary>
    public GameCommandSource Source { get; private set; }

    /// <summary>
    /// 与命令关联的 Gate 会话，用于向对应客户端回包；会话不存在或无法解析时为 <c>null</c>。
    /// </summary>
    public Session GateSession { get; private set; }

    /// <summary>
    /// 根据入站命令构造逻辑线程处理上下文，并解析命令关联的 Gate 会话。
    /// </summary>
    /// <param name="command">待处理的入站命令，可以为空。</param>
    public GameMessageHandlerContext(GameCommand command)
    {
        Command = command;
        Package = command == null ? null : command.ToBasePackage();
        Transport = command == null ? null : command.Transport;
        Source = command == null ? default(GameCommandSource) : command.Source;

        // CenterServer 回包须按原 Gate 会话回传；找不到会话时交给处理器拒绝处理。
        if (command != null && command.Session != null && command.Session.GateSessionId > 0)
        {
            GateSession = SessionMgr.Instance.GetSession(command.Session.GateSessionId);
        }
    }
}

/// <summary>
/// CenterServer 客户端可见回包的统一转发入口。
/// 调用方已经由协议注册元数据确认路由，GameServer 只负责依据原始 Gate 会话转发。
/// </summary>
public static class GameCenterResponseForwarder
{
    /// <summary>
    /// 将已注册为 <see cref="CenterMessageRoute.ForwardToGate"/> 的回包原样发送给目标 GateServer。
    /// 此方法由 ClientMessageManager 在 Handler 前调用，内部通知不会调用此入口。
    /// </summary>
    public static bool TryForwardToGate(GameMessageHandlerContext context)
    {
        if (context == null || context.Source != GameCommandSource.CenterServer ||
            context.Package == null)
        {
            return false;
        }

        if (context.GateSession == null)
        {
            LogMsg.Info("CenterServer 客户端回包找不到目标网关会话，命令=" +
                context.Package.ProtoCode, LogMsgType.Error);
            return false;
        }

        // 转发发生在 GameServer 的后续业务解析前，避免本服状态校验阻断客户端错误结果。
        context.GateSession.SendData(context.Package);
        return true;
    }

}

/// <summary>
/// GameServer 协议注册入口，对应 roserver 的 CProtocolRegister::Regiter。
/// 此处只登记网络回调和处理器，不承载协议业务逻辑。
/// </summary>
public static class GameMessageProtocolRegister
{
    public static void Register(NetServer server, NetClient client)
    {
        if (server == null || client == null)
        {
            LogMsg.Info("GameServer 协议注册失败：网络对象为空", LogMsgType.Error);
            return;
        }

        ClientMessageManager messageManager = ClientMessageManager.Instance;
        IGameMessageHandler loginGameServerHandler = new LoginGameServerHandler();
        IGameMessageHandler createRoleHandler = new CreateRoleHandler();
        IGameMessageHandler startGameHandler = new StartGameHandler();

        // GateServer 请求统一由服务端网络回调入队。
        server.RegistCommand(NetDefine.CMD_LoginGameServerCode, messageManager);
        server.RegistCommand(NetDefine.CMD_CreateRoleCode, messageManager);
        server.RegistCommand(NetDefine.CMD_StartGameCode, messageManager);
        server.RegistCommand(NetDefine.CMD_EnterWroldCode, messageManager);
        server.RegistCommand(NetDefine.CMD_OpenKnapsackGridCode, messageManager);
        server.RegistCommand(NetDefine.CMD_BuyShopItemCode, messageManager);

        // CenterServer 回包统一由客户端网络回调入队。
        client.RegistCommand(NetDefine.CMD_LoginGameServerCode, messageManager);
        client.RegistCommand(NetDefine.CMD_CreateRoleCode, messageManager);
        client.RegistCommand(NetDefine.CMD_StartGameCode, messageManager);
        client.RegistCommand(NetDefine.CMD_RoleSkillInfoCode, messageManager);
        client.RegistCommand(NetDefine.CMD_RoleKnapsackInfoCode, messageManager);
        client.RegistCommand(NetDefine.CMD_SyncKnapsackGridCountCode, messageManager);
        client.RegistCommand(NetDefine.CMD_BuyShopItemCode, messageManager);
        client.RegistCommand(NetDefine.CMD_SaveRoleDataCode, messageManager);

        // 同一协议的双向链路登记为同一个处理器，由处理器根据来源执行对应分支。
        RegisterHandler(messageManager, GameCommandSource.GateServer,
            NetDefine.CMD_LoginGameServerCode, loginGameServerHandler, CenterMessageRoute.Internal);
        RegisterHandler(messageManager, GameCommandSource.CenterServer,
            NetDefine.CMD_LoginGameServerCode, loginGameServerHandler, CenterMessageRoute.ForwardToGate);
        RegisterHandler(messageManager, GameCommandSource.GateServer,
            NetDefine.CMD_CreateRoleCode, createRoleHandler, CenterMessageRoute.Internal);
        RegisterHandler(messageManager, GameCommandSource.CenterServer,
            NetDefine.CMD_CreateRoleCode, createRoleHandler, CenterMessageRoute.ForwardToGate);
        RegisterHandler(messageManager, GameCommandSource.GateServer,
            NetDefine.CMD_StartGameCode, startGameHandler, CenterMessageRoute.Internal);
        RegisterHandler(messageManager, GameCommandSource.CenterServer,
            NetDefine.CMD_StartGameCode, startGameHandler, CenterMessageRoute.ForwardToGate);
        RegisterHandler(messageManager, GameCommandSource.GateServer,
            NetDefine.CMD_EnterWroldCode, new EnterWorldHandler(), CenterMessageRoute.Internal);
        RegisterHandler(messageManager, GameCommandSource.CenterServer,
            NetDefine.CMD_RoleSkillInfoCode, new RoleSkillInfoHandler(), CenterMessageRoute.ForwardToGate);
        RegisterHandler(messageManager, GameCommandSource.CenterServer,
            NetDefine.CMD_RoleKnapsackInfoCode, new RoleKnapsackInfoHandler(), CenterMessageRoute.ForwardToGate);
        RegisterHandler(messageManager, GameCommandSource.GateServer,
            NetDefine.CMD_OpenKnapsackGridCode, new OpenKnapsackGridHandler(), CenterMessageRoute.Internal);
        RegisterHandler(messageManager, GameCommandSource.CenterServer,
            NetDefine.CMD_SyncKnapsackGridCountCode, new SyncKnapsackGridCountHandler(), CenterMessageRoute.Internal);
        RegisterHandler(messageManager, GameCommandSource.GateServer,
            NetDefine.CMD_BuyShopItemCode, new RequestBuyShopItem(), CenterMessageRoute.Internal);
        RegisterHandler(messageManager, GameCommandSource.CenterServer,
            NetDefine.CMD_BuyShopItemCode, new BuyShopItemCenterResultHandler(),
            CenterMessageRoute.ForwardToGate);
        RegisterHandler(messageManager, GameCommandSource.CenterServer,
            NetDefine.CMD_SaveRoleDataCode, new SaveRoleDataHandler(), CenterMessageRoute.Internal);
    }

    private static void RegisterHandler(ClientMessageManager messageManager, GameCommandSource source,
        ushort commandCode, IGameMessageHandler handler, CenterMessageRoute centerRoute)
    {
        GameMessageHandlerKey key = new GameMessageHandlerKey(source, commandCode);
        if (!messageManager.Register(key, handler, centerRoute))
        {
            LogMsg.Info("GameServer 消息处理器注册被拒绝，来源=" + source + "，命令=" + commandCode,
                LogMsgType.Error);
        }
    }
}
