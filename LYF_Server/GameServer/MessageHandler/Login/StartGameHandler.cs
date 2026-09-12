using Google.Protobuf;

/// <summary>处理开始游戏协议，根据消息来源区分客户端请求和中心服回包。</summary>
public sealed class StartGameHandler : IGameMessageHandler
{
    public bool CanHandle(GameCommandSource source, ushort commandCode)
    {
        return commandCode == NetDefine.CMD_StartGameCode &&
            (source == GameCommandSource.GateServer || source == GameCommandSource.CenterServer);
    }

    public void Handle(GameMessageHandlerContext context)
    {
        if (context == null || context.Package == null)
        {
            LogMsg.Info("开始游戏消息为空", LogMsgType.Error);
            return;
        }

        if (context.Source == GameCommandSource.GateServer)
        {
            HandleClientRequest(context);
            return;
        }

        if (context.Source == GameCommandSource.CenterServer)
        {
            HandleCenterResult(context);
            return;
        }

        LogMsg.Info("拒绝开始游戏未知消息来源: " + context.Source, LogMsgType.Error);
    }

    private static void HandleClientRequest(GameMessageHandlerContext context)
    {
        if (context.Transport == null)
        {
            LogMsg.Info("开始游戏请求缺少传输链路", LogMsgType.Error);
            return;
        }

        try
        {
            StartGameReq.Parser.ParseFrom(context.Package.Data);
            Game_LoginCtrl.HandleClientStartGame(context.Transport, context.Package);
        }
        catch (InvalidProtocolBufferException ex)
        {
            LogMsg.Info("开始游戏请求协议解析失败: " + ex.Message, LogMsgType.Error);
        }
    }

    private static void HandleCenterResult(GameMessageHandlerContext context)
    {
        // 统一入口已先转发。目标网关会话失效时不能创建孤立在线角色。
        if (context.GateSession == null)
        {
            return;
        }

        try
        {
            Game_LoginCtrl.HandleCenterStartGame(context.Package);
        }
        catch (InvalidProtocolBufferException ex)
        {
            LogMsg.Info("开始游戏结果协议解析失败: " + ex.Message, LogMsgType.Error);
        }
    }
}
