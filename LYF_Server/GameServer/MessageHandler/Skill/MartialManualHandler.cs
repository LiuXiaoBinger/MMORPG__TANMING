using Google.Protobuf;

/// <summary>由游戏服消息分派器通过既有 IGameMessageHandler 契约转交秘笈操作。</summary>
public sealed class MartialManualHandler : IGameMessageHandler
{
    /// <summary>只接受从网关来的秘笈请求。</summary>
    /// <param name="source">投递请求的服务。</param><param name="commandCode">协议号。</param>
    public bool CanHandle(GameCommandSource source, ushort commandCode)
    {
        return source == GameCommandSource.GateServer && commandCode == NetDefine.CMD_MartialManual;
    }

    /// <summary>按网关会话定位当前角色，返回该角色最新进度。</summary>
    /// <param name="context">游戏主循环派发的消息上下文。</param>
    public void OnCall(GameMessageHandlerContext context)
    {
        // 客户端没有指定角色或消耗品的权限。
        ManualRequest request = ManualRequest.Parser.ParseFrom(context.Package.Data);
        // 原网关会话当前登录的角色。
        OnlineRole role = RoleManager.Instance.GetOnlineRoleBySession(context.Package.GateSessionId, context.Package.UnitySessionId);
        // 角色未载入时向原窗口说明原因。
        ManualReply reply = new ManualReply { Action = request.Action, Message = "角色数据尚未就绪" };
        if (role != null && role.State == OnlineRoleState.Online && role.IsRoleDataReady())
            reply = MartialManualSystem.Execute(role, request);
        context.Transport.Send(context.Package, NetDefine.CMD_MartialManual, reply.ToByteString());
    }
}
