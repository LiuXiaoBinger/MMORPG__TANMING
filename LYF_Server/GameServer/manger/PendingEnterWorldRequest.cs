/// <summary>重连角色等待旧实例保存期间暂存的一次进入世界请求。</summary>
public sealed class PendingEnterWorldRequest
{
    /// <summary>原 GateServer 到 GameServer 的传输链路，用于激活后转发 CenterServer。</summary>
    public ServerBase Transport { get; private set; }
    /// <summary>携带原 Gate/Unity 复合会话及不可变字节载荷的协议包副本。</summary>
    public BasePackage Package { get; private set; }

    /// <summary>复制原协议包，避免网络处理流程后续复用包实例改变会话语义。</summary>
    /// <param name="transport">接收原进入世界请求的 GameServer 传输对象。</param>
    /// <param name="package">已解析并经待激活角色会话校验的进入世界协议包。</param>
    public PendingEnterWorldRequest(ServerBase transport, BasePackage package)
    {
        Transport = transport;
        Package = package.Clone();
    }
}
