/// <summary>在现有逻辑队列中消费场景交互，框架注册要求实现处理器契约。</summary>
public sealed class DungeonInteractHandler : IGameMessageHandler
{
    /// <summary>仅接受网关转交的玩家副本按钮请求。</summary>
    /// <param name="source">原网络命令来源。</param>
    /// <param name="commandCode">副本交互协议号。</param>
    public bool CanHandle(GameCommandSource source, ushort commandCode)
    {
        return source == GameCommandSource.GateServer && commandCode == NetDefine.CMD_DungeonInteractCode;
    }

    /// <summary>以原复合会话确定身份，协议内容直接解析。</summary>
    /// <param name="context">逻辑线程原始命令及会话。</param>
    public void OnCall(GameMessageHandlerContext context)
    {
        // 客户端不能自行指定其他角色或实例。
        OnlineRole role = RoleManager.Instance.GetOnlineRoleBySession(context.Package.GateSessionId, context.Package.UnitySessionId);
        if (role == null) return;
        // Protobuf 边界不包装或吞掉解析异常。
        DungeonInteractReq request = DungeonInteractReq.Parser.ParseFrom(context.Package.Data);
        TeamManager.Instance.Interact(role, request);
    }
}
