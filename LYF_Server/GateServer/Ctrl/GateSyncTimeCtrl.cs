/// <summary>
/// 网关 TCP 往返测量处理器。实现命令注册要求的 IContainer，在收包会话上回显客户端计时标记。
/// </summary>
internal sealed class GateSyncTimeCtrl : IContainer
{
    /// <summary>供命令处理器框架初始化调用，回显处理器无需初始化状态。</summary>
    public void OnInit()
    {
    }

    /// <summary>验证计时标记占用 8 字节，随后将原字节发回接入会话。</summary>
    /// <param name="serverBase">收到探测请求的客户端 TCP 会话。</param>
    /// <param name="basePackage">已按协议号分发的封包，载荷是客户端的单调计时标记。</param>
    public void OnServerCommand(BaseLink serverBase, BasePackage basePackage)
    {
        if (basePackage.Data.Length != sizeof(long))
        {
            return;
        }

        serverBase.Send(NetDefine.RpcC2T_SyncTime, basePackage.Data);
    }

    /// <summary>供命令处理器框架提供上游回包入口，此协议由网关监听端直接应答。</summary>
    /// <param name="serverBase">收到上游回包的主动连接，此处理器不使用。</param>
    /// <param name="basePackage">上游回包，此处理器不使用。</param>
    public void OnClientCommand(BaseLink serverBase, BasePackage basePackage)
    {
    }
}
