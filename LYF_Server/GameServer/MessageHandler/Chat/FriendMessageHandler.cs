using Google.Protobuf;

/// <summary>游戏服按已有 IGameMessageHandler 回调契约转交好友操作，从真实网关会话补全发起角色。</summary>
public sealed class FriendMessageHandler : IGameMessageHandler
{
    /// <summary>登记玩家可发起的五个好友 RPC / PTC。</summary>
    /// <param name="server">接收 Gate 请求的服务端。</param><param name="messages">游戏逻辑线程分发器。</param>
    internal static void Register(NetServer server, ClientMessageManager messages)
    {
        // 同一无状态处理器承接一段连续的公开操作编号。
        var handler = new FriendMessageHandler();
        // 依次注册对应连续区间的客户端请求编号。
        for (ushort code = FriendCode.GetFriendInfo; code <= FriendCode.ReadPrivateMessage; code++)
        {
            server.RegistCommand(code, messages);
            messages.Register(new GameMessageHandlerKey(GameCommandSource.GateServer, code), handler, CenterMessageRoute.Internal);
        }
        // 好友群通过相同的会话鉴权和游戏线程向 MS 路由。
        for (ushort code = ChatRoomCode.GetChatRoomInfo; code <= ChatRoomCode.SendChatMsg; code++)
        {
            server.RegistCommand(code, messages);
            messages.Register(new GameMessageHandlerKey(GameCommandSource.GateServer, code), handler, CenterMessageRoute.Internal);
        }
    }

    /// <summary>只处理来自 Gate 的好友操作。</summary><param name="source">接收来源。</param><param name="code">协议编号。</param>
    public bool CanHandle(GameCommandSource source, ushort code)
    { if (source != GameCommandSource.GateServer) return false;
        if (code >= ChatRoomCode.GetChatRoomInfo && code <= ChatRoomCode.SendChatMsg) return true;
        return code >= FriendCode.GetFriendInfo && code <= FriendCode.ReadPrivateMessage; }

    /// <summary>将原包随已验证的角色编号交给 MS，不向世界或场景频道广播。</summary><param name="context">游戏线程当前正在处理的请求。</param>
    public void OnCall(GameMessageHandlerContext context)
    {
        // 网关和玩家两级会话唯一定位本次调用者。
        BasePackage packet = context.Package;
        OnlineRole role = RoleManager.Instance.GetOnlineRoleBySession(packet.GateSessionId, packet.UnitySessionId);
        if (role == null || role.State != OnlineRoleState.Online) return;
        // 沿现有社交链路传递真实角色和原始协议。
        var request = new MasterTeamRolePacket { RoleId = role.GetID(), Packet = packet };
        // 群操作和好友私聊在 MS 对应各自的业务入口。
        ushort route = FriendCode.RoleRequest; ushort error = FriendCode.Error;
        if (packet.ProtoCode >= ChatRoomCode.GetChatRoomInfo) { route = ChatRoomCode.RoleRequest; error = ChatRoomCode.Error; }
        if (!MasterTeamLink.Instance.SendToMaster(new BasePackage { ProtoCode = route, Data = request.ToByteString() }))
            role.SendToClient(error, new FriendError { Error = "好友服务暂未连接" }.ToByteString());
    }
}
