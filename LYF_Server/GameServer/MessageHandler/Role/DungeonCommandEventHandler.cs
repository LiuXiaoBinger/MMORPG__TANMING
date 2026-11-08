/// <summary>沿现有框架处理NPC对话命令；IGameMessageHandler是注册器强制契约。</summary>
public sealed class DungeonCommandEventHandler : IGameMessageHandler
{
    /// <summary>只接收网关上行的原副本命令事件。</summary>
    /// <param name="source">现有网络命令来源。</param><param name="commandCode">上行协议编号。</param>
    public bool CanHandle(GameCommandSource source, ushort commandCode)
    {
        return source == GameCommandSource.GateServer && commandCode == NetDefine.CMD_DungeonCommandEventCode;
    }

    /// <summary>以真实复合会话定位角色，直接解析协议后交所属实例。</summary>
    /// <param name="context">逻辑线程的协议与会话上下文。</param>
    public void OnCall(GameMessageHandlerContext context)
    {
        // 客户端只能提交事件名，身份和实例都从当前会话读取。
        OnlineRole role = RoleManager.Instance.GetOnlineRoleBySession(context.Package.GateSessionId, context.Package.UnitySessionId);
        if (role == null) return;
        // Protobuf解析沿既有直接调用约定，不捕获或吞掉格式错误。
        DungeonCommandEventReq request = DungeonCommandEventReq.Parser.ParseFrom(context.Package.Data);
        TeamManager.Instance.CommandEvent(role, request.Name);
    }
}
