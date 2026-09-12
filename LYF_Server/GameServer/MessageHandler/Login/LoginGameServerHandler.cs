using Google.Protobuf;

/// <summary>处理登录游戏服协议，根据消息来源区分客户端请求和中心服回包。</summary>
public sealed class LoginGameServerHandler : IGameMessageHandler
{
    public bool CanHandle(GameCommandSource source, ushort commandCode)
    {
        return commandCode == NetDefine.CMD_LoginGameServerCode &&
            (source == GameCommandSource.GateServer || source == GameCommandSource.CenterServer);
    }

    public void Handle(GameMessageHandlerContext context)
    {
        if (context == null || context.Package == null)
        {
            LogMsg.Info("登录游戏服消息为空", LogMsgType.Error);
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

        LogMsg.Info("拒绝登录游戏服未知消息来源: " + context.Source, LogMsgType.Error);
    }

    /// <summary>GateServer 只转发客户端请求，必须使用原始传输链路继续发送。</summary>
    private static void HandleClientRequest(GameMessageHandlerContext context)
    {
        if (context.Transport == null)
        {
            LogMsg.Info("登录游戏服请求缺少传输链路", LogMsgType.Error);
            return;
        }

        try
        {
            LoginGameServerReq.Parser.ParseFrom(context.Package.Data);
            Game_LoginCtrl.HandleClientLoginGameServer(context.Transport, context.Package);
        }
        catch (InvalidProtocolBufferException ex)
        {
            LogMsg.Info("登录游戏服请求协议解析失败: " + ex.Message, LogMsgType.Error);
        }
    }

    /// <summary>CenterServer 回包已由统一入口转给网关，本处理器不重复解析或转发。</summary>
    private static void HandleCenterResult(GameMessageHandlerContext context)
    {
        // 网关负责协议校验及错误码提示，避免 GameServer 本地校验阻断客户端回包。
    }
}
