using Google.Protobuf;

/// <summary>处理客户端背包开格子请求。</summary>
public sealed class OpenKnapsackGridHandler : IGameMessageHandler
{
    public bool CanHandle(GameCommandSource source, ushort commandCode)
    {
        return source == GameCommandSource.GateServer && commandCode == NetDefine.CMD_OpenKnapsackGridCode;
    }

    public void Handle(GameMessageHandlerContext context)
    {
        // 上下文缺失或来源不是网关时直接丢弃，避免后续解引用空对象。
        if (context == null || context.Source != GameCommandSource.GateServer)
        {
            LogMsg.Info("背包开格子请求缺少 Gate 传输链路", LogMsgType.Error);
            return;
        }
        // 传输链路或协议包缺失同样属于无效网关会话。
        if (context.Transport == null || context.Package == null)
        {
            LogMsg.Info("背包开格子请求缺少 Gate 传输链路", LogMsgType.Error);
            return;
        }

        try
        {
            OpenKnapsackGridReq.Parser.ParseFrom(context.Package.Data);
            RoleManager.Instance.HandleClientOpenKnapsackGrid(context.Transport, context.Package);
        }
        catch (InvalidProtocolBufferException ex)
        {
            LogMsg.Info("背包开格子请求协议解析失败: " + ex.Message, LogMsgType.Error);
        }
    }
}
