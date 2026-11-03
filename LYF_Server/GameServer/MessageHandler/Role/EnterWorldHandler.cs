/// <summary>处理客户端进入世界请求。</summary>
public sealed class EnterWorldHandler : IGameMessageHandler
{
    /// <summary>只匹配网关转来的进入世界请求。</summary>
    /// <param name="source">消息来自网关或中心服。</param>
    /// <param name="commandCode">消息的协议编号。</param>
    public bool CanHandle(GameCommandSource source, ushort commandCode)
    {
        return source == GameCommandSource.GateServer && commandCode == NetDefine.CMD_EnterWroldCode;
    }

    /// <summary>交给 RoleManager 检查角色与会话，并转发中心服加载技能和背包。</summary>
    /// <param name="context">逻辑线程中的消息、会话和原网络连接。</param>
    public void OnCall(GameMessageHandlerContext context)
    {
        // 上下文缺失或来源不是网关时直接丢弃，避免后续解引用空对象。
        if (context == null || context.Source != GameCommandSource.GateServer)
        {
            LogMsg.Info("进入世界请求缺少 Gate 传输链路", LogMsgType.Error);
            return;
        }
        // 传输链路或协议包缺失同样属于无效网关会话。
        if (context.Transport == null || context.Package == null)
        {
            LogMsg.Info("进入世界请求缺少 Gate 传输链路", LogMsgType.Error);
            return;
        }

        EnterWroldReq.Parser.ParseFrom(context.Package.Data);
        RoleManager.Instance.HandleClientEnterWorld(context.Transport, context.Package);
    }
}
