using Google.Protobuf;

/// <summary>处理创建角色协议，根据消息来源区分客户端请求和中心服回包。</summary>
public sealed class CreateRoleHandler : IGameMessageHandler
{
    public bool CanHandle(GameCommandSource source, ushort commandCode)
    {
        return commandCode == NetDefine.CMD_CreateRoleCode &&
            (source == GameCommandSource.GateServer || source == GameCommandSource.CenterServer);
    }

    public void Handle(GameMessageHandlerContext context)
    {
        if (context == null || context.Package == null)
        {
            LogMsg.Info("创建角色消息为空", LogMsgType.Error);
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

        LogMsg.Info("拒绝创建角色未知消息来源: " + context.Source, LogMsgType.Error);
    }

    private static void HandleClientRequest(GameMessageHandlerContext context)
    {
        if (context.Transport == null)
        {
            LogMsg.Info("创建角色请求缺少传输链路", LogMsgType.Error);
            return;
        }

        try
        {
            CreateRoleReq.Parser.ParseFrom(context.Package.Data);
            Game_LoginCtrl.HandleClientCreateRole(context.Transport, context.Package);
        }
        catch (InvalidProtocolBufferException ex)
        {
            LogMsg.Info("创建角色请求协议解析失败: " + ex.Message, LogMsgType.Error);
        }
    }

    private static void HandleCenterResult(GameMessageHandlerContext context)
    {
        // 创建角色回包已经由统一入口发送给网关，不在此处解析或重复转发。
    }
}
