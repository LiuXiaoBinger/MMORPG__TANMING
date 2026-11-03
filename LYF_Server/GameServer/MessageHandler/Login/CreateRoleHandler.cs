/// <summary>处理创建角色协议，根据消息来源区分客户端请求和中心服回包。</summary>
public sealed class CreateRoleHandler : IGameMessageHandler
{
    /// <summary>按建角协议号匹配网关请求和中心服结果两种来源。</summary>
    /// <param name="source">消息来自网关或中心服。</param>
    /// <param name="commandCode">消息的协议编号。</param>
    public bool CanHandle(GameCommandSource source, ushort commandCode)
    {
        return commandCode == NetDefine.CMD_CreateRoleCode &&
            (source == GameCommandSource.GateServer || source == GameCommandSource.CenterServer);
    }

    /// <summary>网关请求继续转给中心服，中心服回包已由统一转发入口发送给客户端。</summary>
    /// <param name="context">逻辑线程中的消息、会话和原网络连接。</param>
    public void OnCall(GameMessageHandlerContext context)
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

    /// <summary>确认回传链路存在后，把原请求交给登录控制器继续转发。</summary>
    /// <param name="context">网关请求的逻辑线程上下文。</param>
    private static void HandleClientRequest(GameMessageHandlerContext context)
    {
        if (context.Transport == null)
        {
            LogMsg.Info("创建角色请求缺少传输链路", LogMsgType.Error);
            return;
        }

        CreateRoleReq.Parser.ParseFrom(context.Package.Data);
        Game_LoginCtrl.HandleClientCreateRole(context.Transport, context.Package);
    }

    /// <summary>处理已通过统一入口转发的中心服结果，不重复向网关发送。</summary>
    /// <param name="context">中心服回包及关联的网关会话。</param>
    private static void HandleCenterResult(GameMessageHandlerContext context)
    {
        // 创建角色回包已经由统一入口发送给网关，不在此处解析或重复转发。
    }
}
