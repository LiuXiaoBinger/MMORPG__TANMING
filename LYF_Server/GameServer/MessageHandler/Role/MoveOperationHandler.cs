/// <summary>在现有游戏逻辑线程按 RO 移动协议入口反查在线角色并调用移动组件。</summary>
public sealed class MoveOperationHandler : IGameMessageHandler
{
    /// <summary>声明仅消费网关转发的移动上报。</summary>
    /// <param name="source">命令来源，移动操作来自 GateServer。</param>
    /// <param name="commandCode">网络协议号，移动上报为 11180。</param>
    public bool CanHandle(GameCommandSource source, ushort commandCode)
    {
        return source == GameCommandSource.GateServer && commandCode == NetDefine.CMD_MoveOperationCode;
    }

    /// <summary>从原复合会话查找角色，调用已装配的正式移动组件。</summary>
    /// <param name="context">逻辑队列提供的原网关/Unity 会话和协议包。</param>
    public void Handle(GameMessageHandlerContext context)
    {
        // 角色身份从现有会话索引取得，上报不携带可替换的角色 ID。
        OnlineRole pRole = RoleManager.Instance.GetOnlineRoleBySession(
            context.Package.GateSessionId, context.Package.UnitySessionId);
        if (pRole == null)
        {
            return;
        }
        // 网络反序列化边界沿用 protobuf，异常由现有逻辑队列统一处理。
        MoveOperationReq action = MoveOperationReq.Parser.ParseFrom(context.Package.Data);
        pRole.GetComponent<ROMoveComponent>().OnMoveStart(action);
    }
}
