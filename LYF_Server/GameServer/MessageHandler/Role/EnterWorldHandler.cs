using Google.Protobuf;

/// <summary>处理客户端进入世界请求。</summary>
public sealed class EnterWorldHandler : IGameMessageHandler
{
    public bool CanHandle(GameCommandSource source, ushort commandCode)
    {
        return source == GameCommandSource.GateServer && commandCode == NetDefine.CMD_EnterWroldCode;
    }

    public void Handle(GameMessageHandlerContext context)
    {
        if (context == null || context.Source != GameCommandSource.GateServer ||
            context.Transport == null || context.Package == null)
        {
            LogMsg.Info("进入世界请求缺少 Gate 传输链路", LogMsgType.Error);
            return;
        }

        try
        {
            EnterWroldReq.Parser.ParseFrom(context.Package.Data);
            RoleManager.Instance.HandleClientEnterWorld(context.Transport, context.Package);
        }
        catch (InvalidProtocolBufferException ex)
        {
            LogMsg.Info("进入世界请求协议解析失败: " + ex.Message, LogMsgType.Error);
        }
    }
}
