/// <summary>处理 CenterServer 返回的背包容量持久化结果。</summary>
public sealed class SyncKnapsackGridCountHandler : IGameMessageHandler
{
    /// <summary>只匹配中心服返回的背包容量保存结果。</summary>
    /// <param name="source">消息来自网关或中心服。</param>
    /// <param name="commandCode">消息的协议编号。</param>
    public bool CanHandle(GameCommandSource source, ushort commandCode)
    {
        return source == GameCommandSource.CenterServer &&
            commandCode == NetDefine.CMD_SyncKnapsackGridCountCode;
    }

    /// <summary>读取容量保存结果并交给 RoleManager 记录失败，不向客户端重复发送扩容结果。</summary>
    /// <param name="context">逻辑线程中的消息、会话和原网络连接。</param>
    public void OnCall(GameMessageHandlerContext context)
    {
        if (context == null || context.Source != GameCommandSource.CenterServer || context.Package == null)
        {
            LogMsg.Info("背包容量持久化结果为空", LogMsgType.Error);
            return;
        }

        // 此协议是 GameServer 与 CenterServer 的持久化确认，绝不能因携带会话字段转发到网关。
        OpenKnapsackGridRet.Parser.ParseFrom(context.Package.Data);
        RoleManager.HandleCenterSyncKnapsackGridCount(context.Package);
    }
}
