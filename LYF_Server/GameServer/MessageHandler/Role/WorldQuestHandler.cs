using Google.Protobuf;

/// <summary>通过游戏服分派器要求的 IGameMessageHandler 接收角色委托请求。</summary>
public sealed class WorldQuestHandler : IGameMessageHandler
{
    /// <summary>委托只接受玩家网关的可靠请求。</summary>
    /// <param name="source">真实服务来源。</param><param name="commandCode">外层消息编号。</param>
    public bool CanHandle(GameCommandSource source, ushort commandCode)
    {
        return source == GameCommandSource.GateServer && commandCode == NetDefine.CMD_WorldQuest;
    }

    /// <summary>按会话取角色，在游戏逻辑线程处理本次意图并回送当前列表。</summary>
    /// <param name="context">网关原始包和回包通道。</param>
    public void OnCall(GameMessageHandlerContext context)
    {
        // 角色身份沿网关会话取得，玩家不能指定其他角色。
        OnlineRole role = RoleManager.Instance.GetOnlineRoleBySession(context.Package.GateSessionId, context.Package.UnitySessionId);
        // 协议解析沿用游戏服直接解析入口。
        WorldQuestRequest request = WorldQuestRequest.Parser.ParseFrom(context.Package.Data);
        if (role == null || !role.IsRoleDataReady()) return;
        // 初次登录尚未就绪时由背包/次数装载完成入口主动推送。
        string message = WorldQuestSystem.Apply(role, request);
        context.Transport.Send(context.Package, NetDefine.CMD_WorldQuest, WorldQuestSystem.Snapshot(role, message).ToByteString());
    }
}
