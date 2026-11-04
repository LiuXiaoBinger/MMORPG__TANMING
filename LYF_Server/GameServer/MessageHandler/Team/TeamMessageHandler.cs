
/// <summary>
/// 队伍协议处理器，对应 RO TeamMgr.lua 的请求分支。
/// 角色身份一律从网关复合会话取得，客户端载荷只作业务参数或身份一致性核对。
/// </summary>
public sealed class TeamMessageHandler : IGameMessageHandler
{
    /// <summary>客户端已有的六个组队协议；监听注册与能力判断共享同一份清单。</summary>
    private static readonly ushort[] Commands =
    {
        NetDefine.CMD_CreateTeamCode, NetDefine.CMD_InviteJoinTeamCode, NetDefine.CMD_AcceptTeamInvatationCode,
        NetDefine.CMD_LeaveTeamCode, NetDefine.CMD_BegInTeamByTeamIdCode, NetDefine.CMD_CaptainRequestEnterFBCode
    };

    /// <summary>同时注册监听端和游戏线程分派，认证成功的请求才会转交 MS。</summary>
    /// <param name="server">接受网关连接的游戏服监听端。</param>
    /// <param name="messages">消费网关命令的正式逻辑线程分派器。</param>
    public static void Register(NetServer server, ClientMessageManager messages)
    {
        // 所有组队请求共用同一个无状态转发处理器。
        TeamMessageHandler handler = new TeamMessageHandler();
        // 为每个已有客户端命令登记网络入口和对应处理器。
        for (int index = 0; index < Commands.Length; index++)
        {
            // 唯一的客户端命令号。
            ushort code = Commands[index];
            server.RegistCommand(code, messages);
            messages.Register(new GameMessageHandlerKey(GameCommandSource.GateServer, code), handler, CenterMessageRoute.Internal);
        }
    }

    /// <summary>仅接管从网关认证路径进入的既有队伍命令。</summary>
    /// <param name="source">消息的真实来源。</param><param name="commandCode">原客户端协议号。</param>
    /// <returns>命令是否在组队转发清单中。</returns>
    public bool CanHandle(GameCommandSource source, ushort commandCode)
    {
        return source == GameCommandSource.GateServer && System.Array.IndexOf(Commands, commandCode) >= 0;
    }

    /// <summary>按协议号分派到 TeamManager 的对应操作。</summary>
    /// <param name="context">逻辑队列提供的网关会话与协议包。</param>
    public void OnCall(GameMessageHandlerContext context)
    {
        // 上下文缺失或来源不是网关时直接丢弃，避免后续解引用空对象。
        if (context == null || context.Source != GameCommandSource.GateServer)
        {
            return;
        }
        // 传输链路或协议包缺失属于无效网关会话。
        if (context.Transport == null || context.Package == null)
        {
            return;
        }

        // 执行对象从会话索引取得；即使请求含有角色ID，也不能用它替换当前会话角色。
        // role 为网关复合会话解析出的在线角色，是本请求唯一的权威身份，为 null 说明会话已失效。
        OnlineRole role = RoleManager.Instance.GetOnlineRoleBySession(
            context.Package.GateSessionId, context.Package.UnitySessionId);
        if (role == null)
        {
            LogMsg.Info("队伍请求找不到在线角色，协议=" + context.Package.ProtoCode.ToString(),
                LogMsgType.Error);
            return;
        }

        MasterTeamLink.Instance.Forward(role, context.Package);
    }
}
