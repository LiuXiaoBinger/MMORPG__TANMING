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
        if (context == null || context.Source != GameCommandSource.GateServer ||
            context.Transport == null || context.Package == null)
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
