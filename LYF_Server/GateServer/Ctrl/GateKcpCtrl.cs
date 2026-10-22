using Google.Protobuf;

/// <summary>
/// 网关 KCP 绑定入口：为已完成登录的 TCP 会话下发会话号与短期令牌。
/// 绑定由客户端在创建本地主角表现前发起，早于进入世界请求，因此不校验角色是否已进入世界。
/// 只做参数分配，不校验角色权限，也不保存业务状态。
/// </summary>
public sealed class GateKcpCtrl : IContainer
{
    /// <summary>绑定入口无需预加载资源。</summary>
    public void OnInit()
    {
    }

    /// <summary>
    /// 处理客户端绑定请求：分配 conv 与令牌并回包，等待客户端 UDP 握手。
    /// </summary>
    /// <param name="serverBase">接收请求的网关会话。</param>
    /// <param name="basePackage">携带绑定请求的协议包，协议号由命令表保证。</param>
    public void OnServerCommand(ServerBase serverBase, BasePackage basePackage)
    {
        // 把网关基类转为本会话类型，转换失败说明连接类型不符，直接忽略。
        Session session = serverBase as Session;
        if (session == null)
        {
            return;
        }

        // 客户端放弃绑定：载荷固定为结果码 + conv + 零令牌 + 端口，只有 Abandoned 才处理。
        // 该消息经可靠 TCP 到达，是网关清理残留 Peer 的唯一可信依据。
        // 协议体可选：未携带数据时按空处理，避免对 null 引用调用 ToByteArray。
        byte[] request = null;
        if (basePackage.Data != null)
        {
            request = basePackage.Data.ToByteArray();
        }
        if (request != null && request.Length == KcpBindPayload.PayloadBytes)
        {
            // 解析放弃载荷；长度合法但结构损坏时按普通绑定请求继续走分配流程。
            KcpBindPayload abandonRequest = KcpBindPayload.Decode(request);
            if (abandonRequest != null && abandonRequest.Result == KcpBindResult.Abandoned)
            {
                // 取当前 UDP 网络实例：未启动时只清理会话侧引用，不访问空引用。
                UdpNetwork abandonNetwork = UdpNetwork.Instance;
                if (abandonNetwork != null)
                {
                    // 先释放待绑定条目与 Peer，再解除会话侧引用，两侧都不再保留实时通道。
                    abandonNetwork.ReleaseSession(session.SessionID);
                }
                session.DetachRealtimePeer();
                LogMsg.Info("KCP 绑定被客户端放弃，已释放残留通道，SessionID=" + session.SessionID);
                return;
            }
        }

        // 当前 UDP 网络实例：为空表示网关未启动 KCP 监听，本会话无法建立实时通道。
        UdpNetwork udpNetwork = UdpNetwork.Instance;
        // 待下发的绑定响应载荷，先置空，由分配结果填充或构造不可用响应。
        KcpBindPayload payload = null;
        if (udpNetwork == null)
        {
            // 未启用 UDP 时明确回不可用；客户端据此清理通道并沿现有断线恢复重建连接。
            payload = new KcpBindPayload();
            payload.Result = KcpBindResult.ServerUnavailable;
            payload.Token = new byte[KcpDefine.BindTokenBytes];
        }
        else
        {
            // 向 UDP 层分配 conv 与令牌；失败时把结果码原样回填为响应载荷。
            KcpBindResult result = udpNetwork.TryAllocateBinding(session, out payload);
            if (result != KcpBindResult.Granted || payload == null)
            {
                payload = new KcpBindPayload();
                payload.Result = result;
                payload.Token = new byte[KcpDefine.BindTokenBytes];
            }
        }

        // 绑定参数必须经 TCP 下发；该协议不在实时集合内，钩子会保留原封包。
        session.SendData(NetDefine.CMD_KcpBindCode, ByteString.CopyFrom(payload.Encode()));
    }

    /// <summary>
    /// 绑定协议没有下行业务回包；网关不会把该协议转发给游戏服。
    /// </summary>
    /// <param name="serverBase">收到回包的连接，本入口不使用。</param>
    /// <param name="basePackage">回包内容，本入口不使用。</param>
    public void OnClientCommand(ServerBase serverBase, BasePackage basePackage)
    {
    }
}
