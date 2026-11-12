/// <summary>框架要求实现消息处理器契约；在原逻辑队列中处理本人复活。</summary>
public sealed class RoleReviveHandler : IGameMessageHandler
{
    /// <summary>只接受网关转交的复活请求。</summary>
    /// <param name="source">命令的真实网络来源。</param>
    /// <param name="commandCode">收到的协议编号。</param>
    public bool CanHandle(GameCommandSource source, ushort commandCode)
    {
        return source == GameCommandSource.GateServer && commandCode == NetDefine.CMD_RoleReviveCode;
    }

    /// <summary>按复合会话反查本人，客户端不提供可伪造的复活目标。</summary>
    /// <param name="context">队列中的消息及其网关、客户端会话。</param>
    public void OnCall(GameMessageHandlerContext context)
    {
        // 空请求也直接解析，保留协议边界的原始失败行为。
        RoleReviveReq.Parser.ParseFrom(context.Package.Data);
        // 请求作用于此连接绑定的唯一在线角色。
        OnlineRole role = RoleManager.Instance.GetOnlineRoleBySession(context.Package.GateSessionId, context.Package.UnitySessionId);
        if (role == null) return;
        TeamManager.Instance.ReviveRole(role);
    }
}
