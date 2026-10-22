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
public class Session : ServerBase
{
    /// <summary>会话 ID，由 SessionMgr 分配。</summary>
    public int SessionID{get;set;}

    /// <summary>当前绑定的 KCP 会话；为空表示实时协议仍走 TCP。绑定由 UDP 线程写入，业务线程读取。</summary>
    private volatile KcpPeer _realtimePeer;

    /// <summary>
    /// 本会话是否曾经启用过实时通道。启用后失效时，实时下行只能丢弃，
    /// 不允许悄悄退回 TCP：客户端已按 KCP 的可靠有序语义消费，换通道会打乱顺序。
    /// </summary>
    private volatile bool _realtimeEverEnabled;

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
        _realtimeEverEnabled = true;
    }

    /// <summary>
    /// 解除 KCP 绑定并回到 TCP。只用于“客户端从未启用”的放弃路径，
    /// 释放后实时协议与普通业务一起走 TCP。
    /// </summary>
    public void DetachRealtimePeer()
    {
        _realtimePeer = null;
        _realtimeEverEnabled = false;
    }

    /// <summary>
    /// 已启用的实时通道失效：解除绑定但保留“曾启用”标记，
    /// 后续实时下行明确丢弃，等既有断线重连后由新会话重新绑定。
    /// </summary>
    public void InvalidateRealtimePeer()
    {
        _realtimePeer = null;
    }

    /// <summary>
    /// 实时协议分流：已绑定且属于实时集合时由 KCP 承载。
    /// </summary>
    /// <param name="basePackage">待发送协议包。</param>
    /// <param name="payload">已序列化的协议包字节。</param>
    /// <returns>KCP 已接管返回 Sent；曾启用后失效返回 Dropped，禁止回退 TCP。</returns>
    protected override RealtimeSendResult TrySendRealtime(BasePackage basePackage, byte[] payload)
    {
        if (!KcpDefine.IsRealtimeProtocol(basePackage.ProtoCode))
        {
            return RealtimeSendResult.KeepTcp;
        }
        // 本地缓存当前已挂接的 Peer，为空表示从未启用或已失效。
        KcpPeer peer = _realtimePeer;
        if (peer == null)
        {
            // 从未启用：本会话按约定保持 TCP；曾启用后失效：明确丢弃，不换通道重发。
            if (_realtimeEverEnabled)
            {
                return RealtimeSendResult.Dropped;
            }
            return RealtimeSendResult.KeepTcp;
        }
        if (peer.Send(payload))
        {
            return RealtimeSendResult.Sent;
        }
        // Peer 存在即意味着已启用过，写入失败只能丢弃。
        return RealtimeSendResult.Dropped;
    }

    /// <summary>
    /// KCP 上行入口：复用原命令分发链，把已认证的协议包交给注册处理器。
    /// </summary>
    /// <param name="basePackage">从 KCP 载荷解析出的协议包。</param>
    public void HandleRealtimePackage(BasePackage basePackage)
    {
        if (basePackage == null)
        {
            return;
        }
        // 客户端自填的会话标识不可信，清空后由 HandleCommand 按当前绑定会话补入。
        basePackage.GateSessionId = 0;
        HandleCommand(basePackage);
    }

    /// <summary>兼容历史调用名称，统一进入可重写的断开流程。</summary>
    public void DisConnect() { Disconnect(); }



}
