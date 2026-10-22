using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


/// <summary>
/// 一条已建立的 TCP 会话：负责异步接收、拆包、按协议号分发命令，并在断开时回调宿主。
/// </summary>
public class Session : BaseLink
{
    /// <summary>会话 ID，由 SessionMgr 分配。</summary>
    public int SessionID{get;set;}

    /// <summary>当前绑定的 KCP 会话；为空表示实时协议没有可用通道，只允许丢弃，不回退 TCP。绑定由 UDP 线程写入，业务线程读取。</summary>
    private volatile KcpPeer _realtimePeer;

    /// <summary>断开回调，参数为会话 ID；由宿主投递到逻辑线程处理角色下线。</summary>
    private readonly Action<int> _onDisconnected;

    /// <summary>构造会话并注册到 SessionMgr。</summary>
    /// <param name="cmdDic">命令处理器表引用，通常与 NetServer 共用同一份。</param>
    /// <param name="client">本服务连接上游的连接器，可为 null。</param>
    /// <param name="onDisconnected">断开回调。</param>
    public Session(Dictionary<int, IContainer> cmdDic, NetClient client, Action<int> onDisconnected = null)
    {
        _client = client;
        _cmdDic = cmdDic;
        _onDisconnected = onDisconnected;
        SessionMgr.Instance.AddSession(this);
    }

    /// <summary>
    /// 开始接收客户端发来的数据
    /// </summary>
    /// <param name="socket"></param>
    public void ReceiveData(Socket socket)
    {
        
        _socket = socket;
        BeginReceive();

    }


    protected override void HandleCommand(BasePackage basePackage)
    {

        IContainer container;
        if (!_cmdDic.TryGetValue(basePackage.ProtoCode, out container) || container == null)
        {
            // 未注册协议只记录并丢弃，不能因为单个未知包中断客户端连接。
            LogMsg.Info("command not regist, protoCode=" + basePackage.ProtoCode, LogMsgType.Warn);
            return;
        }

        if (_client != null)
        {
            if (_client._clientType == ClientType.LoginServer||_client._clientType == ClientType.GateServer)
            {
                basePackage.UnitySessionId = SessionID;
            }

            if (_client._clientType == ClientType.GameServer)
            {
                basePackage.GateSessionId = SessionID;
            }
        }
        container.OnServerCommand(this, basePackage);
    }

    public override void Disconnect()
    {
        LogMsg.Info("客户端会话断开，SessionID=" + SessionID);
        // TCP 断开即失效 UDP 绑定，旧 Peer 不得继续操控该角色。
        // 顺序与 Abandoned 一致：先经 UDP 层释放待绑定与 Peer 表项，再清空会话实时引用。
        // CompleteBind 在同一把表锁内复核 Pending 并 Attach，先拿锁完成挂接时，
        // 由本次 ReleaseSession 删表后紧跟 Detach 统一清掉会话引用，避免表已空而 Session 仍挂已删除 Peer。
        // 当前 UDP 网络实例，为空说明网关未启动 KCP，仅清会话侧引用。
        UdpNetwork udpNetwork = UdpNetwork.Instance;
        if (udpNetwork != null)
        {
            udpNetwork.ReleaseSession(SessionID);
        }
        DetachRealtimePeer();
        base.Disconnect();
        // 仅通知宿主，不在 Socket 线程直接修改角色状态；宿主应投递到逻辑线程。
        _onDisconnected?.Invoke(SessionID);
    }

    /// <summary>
    /// 关联已通过令牌认证的 KCP 会话，此后实时协议改走 UDP。
    /// </summary>
    /// <param name="peer">网关为该会话建立的可靠 KCP 通道。</param>
    public void AttachRealtimePeer(KcpPeer peer)
    {
        _realtimePeer = peer;
    }

    /// <summary>
    /// 解除 KCP 绑定。实时协议不存在 TCP 兜底：解除后本条会话的实时下行只能丢弃，
    /// 由客户端按现有断线恢复流程重建连接与通道。
    /// </summary>
    public void DetachRealtimePeer()
    {
        _realtimePeer = null;
    }

    /// <summary>
    /// 已启用的实时通道失效：解除绑定，后续实时下行明确丢弃，
    /// 等既有断线重连后由新会话重新绑定。
    /// </summary>
    public void InvalidateRealtimePeer()
    {
        _realtimePeer = null;
    }

    /// <summary>
    /// 实时协议分流：网关面向 Unity 的会话只允许 KCP 承载，游戏服内部链路仍走 TCP。
    /// </summary>
    /// <param name="basePackage">待发送协议包。</param>
    /// <param name="payload">已序列化的协议包字节。</param>
    /// <returns>KCP 已接管返回 Sent；网关会话未绑定或写入失败返回 Dropped，不回退 TCP。</returns>
    protected override RealtimeSendResult TrySendRealtime(BasePackage basePackage, byte[] payload)
    {
        if (!KcpDefine.IsRealtimeProtocol(basePackage.ProtoCode))
        {
            return RealtimeSendResult.KeepTcp;
        }
        // 游戏服到网关的服务间链路不是玩家实时通道，必须继续走原 TCP 帧。
        if (IsGameServerLink())
        {
            return RealtimeSendResult.KeepTcp;
        }
        // 本地缓存当前已挂接的 Peer，为空表示尚未绑定、绑定失败或已释放。
        KcpPeer peer = _realtimePeer;
        if (peer == null)
        {
            // Unity 实时协议只走 KCP：没有通道时本条丢弃，禁止改投 TCP 造成跨通道乱序或重复执行。
            LogMsg.Info("[Session] 实时通道未绑定，丢弃实时协议，SessionID=" + SessionID +
                "，protoCode=" + basePackage.ProtoCode, LogMsgType.Warn);
            return RealtimeSendResult.Dropped;
        }
        if (peer.Send(payload))
        {
            return RealtimeSendResult.Sent;
        }
        // Peer 已挂接但本次写入失败，同样只能丢弃。
        LogMsg.Info("[Session] 实时通道写入失败，丢弃实时协议，SessionID=" + SessionID +
            "，protoCode=" + basePackage.ProtoCode, LogMsgType.Warn);
        return RealtimeSendResult.Dropped;
    }

    /// <summary>
    /// 判断本会话是否为游戏服到网关的服务间连接：该链路不属于玩家实时通道，实时协议保持 TCP。
    /// </summary>
    /// <returns>上游连接器为 GameServer 类型时返回 true。</returns>
    private bool IsGameServerLink()
    {
        // 上游连接器在 NetServer 创建会话时写入，为空表示本会话没有服务间上游。
        NetClient upstream = _client;
        if (upstream == null)
        {
            return false;
        }
        return upstream._clientType == ClientType.GameServer;
    }

    /// <summary>兼容历史调用名称，统一进入可重写的断开流程。</summary>
    public void DisConnect() { Disconnect(); }



}
