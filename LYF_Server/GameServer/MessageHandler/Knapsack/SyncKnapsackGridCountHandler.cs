using Google.Protobuf;

/// <summary>处理 CenterServer 返回的背包容量持久化结果。</summary>
public sealed class SyncKnapsackGridCountHandler : IGameMessageHandler
{
    public bool CanHandle(GameCommandSource source, ushort commandCode)
    {
        return source == GameCommandSource.CenterServer &&
            commandCode == NetDefine.CMD_SyncKnapsackGridCountCode;
    }

    public void Handle(GameMessageHandlerContext context)
    {
        if (context == null || context.Source != GameCommandSource.CenterServer || context.Package == null)
        {
            LogMsg.Info("背包容量持久化结果为空", LogMsgType.Error);
            return;
        }

        try
        {
            // 此协议是 GameServer 与 CenterServer 的持久化确认，绝不能因携带会话字段转发到网关。
            OpenKnapsackGridRet.Parser.ParseFrom(context.Package.Data);
            RoleManager.HandleCenterSyncKnapsackGridCount(context.Package);
        }
        catch (InvalidProtocolBufferException ex)
        {
            LogMsg.Info("背包容量持久化结果协议解析失败: " + ex.Message, LogMsgType.Error);
        }
    }
}
