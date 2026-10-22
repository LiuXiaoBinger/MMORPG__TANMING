using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Google.Protobuf;

/// <summary>
/// 网关侧 UDP 网络：监听 UDP 端口、按 conv 路由数据报、维护待绑定表与 KCP 会话。
/// 只做传输与会话绑定，不解析业务语义，也不结算技能或位置。
/// </summary>
public sealed class UdpNetwork
{
    /// <summary>待绑定条目：TCP 会话、令牌与创建时间，用于完成绑定握手。</summary>
    private struct PendingBind
    {
        /// <summary>等待 UDP 绑定的 TCP 会话。</summary>
        public Session Session;

        /// <summary>本次绑定下发的 16 字节令牌。</summary>
        public byte[] Token;

        /// <summary>登记时刻刻度，超过有效窗口后丢弃。</summary>
        public long CreatedTicks;
    }

    /// <summary>监听 Socket，绑定后由接收线程阻塞读取。</summary>
    private Socket _socket;

    /// <summary>接收数据报的后台线程。</summary>
    private Thread _receiveThread;

    /// <summary>驱动 KCP 更新与超时清理的后台线程。</summary>
    private Thread _updateThread;

    /// <summary>运行标记，停止时让两个后台线程退出。</summary>
    private volatile bool _running;

    /// <summary>保护三张映射表，接收线程与逻辑线程可能并发访问。</summary>
    private readonly object _sync = new object();

    /// <summary>
    /// Socket 发送锁。KCP 输出、保活回执、绑定回执可能来自不同线程，
    /// UDP Socket 的并发发送顺序与安全性由本锁串行；本锁内不做任何业务回调。
    /// </summary>
    private readonly object _sendSync = new object();

    /// <summary>conv 到 Peer 的映射，仅用于数据报查表。</summary>
    private readonly Dictionary<uint, KcpPeer> _peersByConv = new Dictionary<uint, KcpPeer>();

    /// <summary>TCP 会话 ID 到 Peer 的映射，用于下行选路与释放。</summary>
    private readonly Dictionary<int, KcpPeer> _peersBySession = new Dictionary<int, KcpPeer>();

    /// <summary>待绑定表：conv 到待完成握手的 TCP 会话。</summary>
    private readonly Dictionary<uint, PendingBind> _pending = new Dictionary<uint, PendingBind>();

    /// <summary>待绑定有效期刻度跨度，避免长期悬挂的绑定占位。</summary>
    private const long PendingLifetimeTicks = KcpDefine.BindPendingLifetimeMilliseconds * 10000L;

    /// <summary>当前 UDP 监听端口，供绑定响应下发。</summary>
    public int Port { get; private set; }

    /// <summary>当前运行的 UDP 网络实例，供会话断开时释放绑定；未启动时为空。</summary>
    public static UdpNetwork Instance { get; private set; }

    /// <summary>UDP 通道是否已经启动并可用。</summary>
    public bool IsRunning
    {
        get { return _running; }
    }

    /// <summary>
    /// 启动 UDP 监听并开启接收与更新线程。
    /// </summary>
    /// <param name="ip">监听地址，与 TCP 网关同机部署时使用相同主机。</param>
    /// <param name="port">监听端口，默认取 KcpDefine.GateUdpPort。</param>
    /// <returns>启动成功返回 true；端口占用等异常返回 false。</returns>
    public bool Start(string ip, int port)
    {
        if (_running)
        {
            return true;
        }
        try
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            // UDP 不建立连接，绑定即可收发；大缓冲降低突发丢包概率。
            _socket.ReceiveBufferSize = 1 << 20;
            _socket.SendBufferSize = 1 << 20;
            _socket.Bind(new IPEndPoint(IPAddress.Parse(ip), port));
            Port = ((IPEndPoint)_socket.LocalEndPoint).Port;
            _running = true;
            Instance = this;

            _receiveThread = new Thread(ReceiveLoop);
            _receiveThread.IsBackground = true;
            _receiveThread.Name = "KcpUdpReceive";
            _receiveThread.Start();

            _updateThread = new Thread(UpdateLoop);
            _updateThread.IsBackground = true;
            _updateThread.Name = "KcpUdpUpdate";
            _updateThread.Start();

            LogMsg.Info("KCP UDP 通道已启动，端口=" + Port);
            return true;
        }
        catch (Exception ex)
        {
            _running = false;
            // 识别出的 SocketException 单独记录系统错误码，便于定位端口占用等问题。
            SocketException socketException = ex as SocketException;
            if (socketException == null)
            {
                LogMsg.Info("[UdpNetwork] 启动失败：" + ex.Message, LogMsgType.Error);
            }
            else
            {
                LogMsg.Info("[UdpNetwork] 启动失败，socketErrorCode=" +
                    (int)socketException.SocketErrorCode, LogMsgType.Error);
            }
            return false;
        }
    }

    /// <summary>停止监听并释放全部 Peer，进程退出时调用。</summary>
    public void Stop()
    {
        _running = false;
        // 停止后不再允许会话通过静态引用访问已释放的通道。
        if (Instance == this)
        {
            Instance = null;
        }
        // 拷贝当前 Socket 引用后立即清空字段，避免停止后仍被新调用拿到。
        Socket socket = _socket;
        _socket = null;
        if (socket != null)
        {
            try
            {
                socket.Close();
            }
            catch (Exception ex)
            {
                LogMsg.Info("[UdpNetwork] 关闭 Socket 失败：" + ex.Message, LogMsgType.Error);
            }
        }

        lock (_sync)
        {
            _peersByConv.Clear();
            _peersBySession.Clear();
            _pending.Clear();
        }
    }

    /// <summary>
    /// 为已登录并在线的 TCP 会话分配绑定参数。
    /// </summary>
    /// <param name="session">已经确认在线角色的网关 TCP 会话。</param>
    /// <param name="payload">输出的绑定响应，含 conv、令牌与 UDP 端口。</param>
    /// <returns>通道不可用或会话无效时返回对应结果码。</returns>
    public KcpBindResult TryAllocateBinding(Session session, out KcpBindPayload payload)
    {
        payload = null;
        if (session == null)
        {
            return KcpBindResult.SessionNotReady;
        }
        if (!_running)
        {
            return KcpBindResult.ServerUnavailable;
        }

        // 本次分配的随机会话号，随机冲突时会重新生成。
        uint conv = KcpAuth.CreateRandomConv();
        // 本次绑定下发的 16 字节随机令牌，既是握手证明也是数据报 HMAC 密钥。
        byte[] token = KcpAuth.CreateRandomBytes(KcpDefine.BindTokenBytes);
        // 随机 conv 需在两张表中都不冲突，冲突时重新生成。
        int attempt = 0;
        while (true)
        {
            // 标记随机 conv 是否已被现有 Peer 占用，占用则进入下一轮重试。
            bool convTaken;
            lock (_sync)
            {
                convTaken = _peersByConv.ContainsKey(conv);
                if (!convTaken)
                {
                    // 同一会话重复绑定先释放旧 Peer，重登后不复用旧连接。
                    ReleaseSessionLocked(session.SessionID);
                    // 新登记的待绑定条目：登记会话、令牌与创建时刻，等待 UDP 绑定请求。
                    PendingBind pending = new PendingBind();
                    pending.Session = session;
                    pending.Token = token;
                    pending.CreatedTicks = DateTime.UtcNow.Ticks;
                    _pending[conv] = pending;
                }
            }
            if (!convTaken)
            {
                break;
            }
            attempt++;
            // 冲突重试次数有限，避免异常情况下无限循环。
            if (attempt >= 8)
            {
                return KcpBindResult.ServerUnavailable;
            }
            conv = KcpAuth.CreateRandomConv();
        }

        payload = new KcpBindPayload();
        payload.Result = KcpBindResult.Granted;
        payload.Conv = conv;
        payload.Token = token;
        payload.UdpPort = Port;
        // 重新绑定期间旧通道立即失效：新启用确认到达前，实时下行不得继续写入已作废的 Peer。
        session.InvalidateRealtimePeer();
        return KcpBindResult.Granted;
    }

    /// <summary>
    /// 释放指定 TCP 会话的 KCP 绑定，TCP 断开、退出角色或重绑时调用。
    /// </summary>
    /// <param name="sessionId">网关 TCP 会话 ID。</param>
    public void ReleaseSession(int sessionId)
    {
        lock (_sync)
        {
            ReleaseSessionLocked(sessionId);
        }
    }

    /// <summary>在持锁状态下释放会话绑定与其待绑定条目。</summary>
    private void ReleaseSessionLocked(int sessionId)
    {
        // 按会话取出的已绑定 Peer，命中后从两张表摘除并清空归属。
        KcpPeer peer;
        if (_peersBySession.TryGetValue(sessionId, out peer))
        {
            _peersBySession.Remove(sessionId);
            _peersByConv.Remove(peer.Conv);
            peer.OwnerSession = null;
        }

        // 待绑定条目同样属于该会话，一并清理避免旧令牌被复用。
        // pendingConvs：遍历时收集出来、待删除的本会话 conv 列表，避免边遍历边改字典。
        List<uint> pendingConvs = new List<uint>();
        // convKeys：当前待绑定表的键快照，先复制键再逐项比对归属。
        List<uint> convKeys = new List<uint>(_pending.Keys);
        // index：键快照遍历下标，逐项取出待绑定键。
        for (int index = 0; index < convKeys.Count; index++)
        {
            // 当前待绑定键，与待删除列表逐项比对。
            uint key = convKeys[index];
            // 该键对应的待绑定条目，用于判断是否属于被释放会话。
            PendingBind item = _pending[key];
            if (item.Session != null)
            {
                if (item.Session.SessionID == sessionId)
                {
                    pendingConvs.Add(key);
                }
            }
        }
        // index：待删除 conv 列表遍历下标，逐个从待绑定表移除。
        for (int index = 0; index < pendingConvs.Count; index++)
        {
            _pending.Remove(pendingConvs[index]);
        }
    }

    /// <summary>
    /// 查询指定会话当前可用的 Peer。
    /// </summary>
    /// <param name="sessionId">网关 TCP 会话 ID。</param>
    /// <returns>已绑定的 Peer；未绑定返回 null。</returns>
    public KcpPeer GetPeerOfSession(int sessionId)
    {
        lock (_sync)
        {
            // 按会话查到的 Peer，命中即返回，未命中返回 null。
            KcpPeer peer;
            if (_peersBySession.TryGetValue(sessionId, out peer))
            {
                return peer;
            }
            return null;
        }
    }

    /// <summary>
    /// 把 KCP 协议段封装为数据报并发往 Peer 的远端地址。
    /// </summary>
    /// <param name="peer">目标 Peer。</param>
    /// <param name="buffer">KCP 段缓冲。</param>
    /// <param name="length">段字节数。</param>
    public void SendPeerSegment(KcpPeer peer, byte[] buffer, int length)
    {
        if (peer == null || buffer == null)
        {
            return;
        }
        if (length <= 0)
        {
            return;
        }
        SendDatagram(peer.Remote, KcpDefine.DatagramTypeKcpData, peer.Conv, peer.Token,
            buffer, 0, length);
    }

    /// <summary>
    /// 封装并发送一条数据报。
    /// </summary>
    private void SendDatagram(IPEndPoint remote, byte type, uint conv, byte[] token,
        byte[] payload, int offset, int length)
    {
        // 取一次 Socket 引用，期间停止置空则直接放弃本次发送。
        Socket socket = _socket;
        if (socket == null || remote == null)
        {
            return;
        }
        // 数据报总长受 MTU 约束，超限说明上游分段错误，直接丢弃而不是分片发送。
        int totalLength = KcpDefine.DatagramHeaderBytes + length;
        if (totalLength > KcpDefine.MaxDatagramBytes)
        {
            LogMsg.Info("[UdpNetwork] 数据报超过上限，已丢弃，长度=" + totalLength, LogMsgType.Warn);
            return;
        }

        // 封装后的待发送字节缓冲，长度为头部加载荷。
        byte[] buffer = new byte[totalLength];
        // KcpDatagram.Write 返回实际写入字节数，非正表示封装失败。
        int written = KcpDatagram.Write(buffer, type, conv, token, payload, offset, length);
        if (written <= 0)
        {
            return;
        }
        try
        {
            lock (_sendSync)
            {
                // 关闭与发送可能并发，取锁后重新确认 Socket 仍可用。
                Socket sendSocket = _socket;
                if (sendSocket == null)
                {
                    return;
                }
                sendSocket.SendTo(buffer, 0, written, SocketFlags.None, remote);
            }
        }
        catch (Exception ex)
        {
            // 识别出的 SocketException 单独记录错误码，其余异常只记录消息。
            SocketException socketException = ex as SocketException;
            if (socketException == null)
            {
                LogMsg.Info("[UdpNetwork] 发送失败：" + ex.Message, LogMsgType.Error);
            }
            else
            {
                LogMsg.Info("[UdpNetwork] 发送失败，socketErrorCode=" +
                    (int)socketException.SocketErrorCode, LogMsgType.Error);
            }
        }
    }

    /// <summary>接收线程：阻塞读取数据报并交给分发逻辑。</summary>
    private void ReceiveLoop()
    {
        // 接收缓冲：1200 预算外加 64 字节冗余，容纳略大的异常报文。
        byte[] buffer = new byte[KcpDefine.MaxDatagramBytes + 64];
        EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
        while (_running)
        {
            // 每轮取一次当前 Socket，停止置空则退出循环。
            Socket socket = _socket;
            if (socket == null)
            {
                break;
            }
            try
            {
                // 本次实际收到的数据报字节数，<=0 跳过继续等待。
                int length = socket.ReceiveFrom(buffer, ref remote);
                if (length <= 0)
                {
                    continue;
                }
                // 发送方地址，仅在认证通过后作为该 Peer 的合法远端。
                IPEndPoint source = remote as IPEndPoint;
                HandleDatagram(buffer, length, source);
            }
            catch (ObjectDisposedException)
            {
                // 正常停止时 Socket 已关闭，退出接收循环。
                break;
            }
            catch (SocketException ex)
            {
                // 系统级 Socket 错误：停止状态退出，运行期记录错误码后继续接收。
                if (!_running)
                {
                    break;
                }
                LogMsg.Info("[UdpNetwork] 接收失败，socketErrorCode=" +
                    (int)ex.SocketErrorCode, LogMsgType.Warn);
            }
            catch (Exception ex)
            {
                // 其他未预期异常：停止状态退出，运行期记录后继续，不让接收线程整体退出。
                if (!_running)
                {
                    break;
                }
                LogMsg.Info("[UdpNetwork] 接收异常：" + ex.Message, LogMsgType.Error);
            }
        }
    }

    /// <summary>更新线程：定期驱动所有 Peer 的重传与发送队列，并清理空闲 Peer。</summary>
    private void UpdateLoop()
    {
        while (_running)
        {
            try
            {
                // Peer 快照：锁内复制当前会话列表，锁外逐 Peer 更新与投递。
                List<KcpPeer> snapshot;
                lock (_sync)
                {
                    snapshot = new List<KcpPeer>(_peersBySession.Values);
                }
                for (int index = 0; index < snapshot.Count; index++)
                {
                    // 快照中的当前 Peer，逐个驱动 KCP 并尝试投递。
                    KcpPeer peer = snapshot[index];
                    peer.Update();
                    SnapshotDeliver(peer);
                }
                CleanupIdlePeers();
                Thread.Sleep(KcpDefine.UpdateIntervalMilliseconds);
            }
            catch (Exception ex)
            {
                // 更新周期内未预期异常：停止状态退出，运行期记录后休眠一个周期再继续。
                if (!_running)
                {
                    break;
                }
                LogMsg.Info("[UdpNetwork] 更新异常：" + ex.Message, LogMsgType.Error);
                Thread.Sleep(KcpDefine.UpdateIntervalMilliseconds);
            }
        }
    }

    /// <summary>清理空闲超时 Peer 与过期待绑定条目。</summary>
    private void CleanupIdlePeers()
    {
        // 当前 UTC 刻度，作为本次空闲判定的基准时间。
        long nowTicks = DateTime.UtcNow.Ticks;
        // 空闲超时刻度跨度：毫秒换算为 100ns 刻度（毫秒×10000）。
        long idleTicks = KcpDefine.PeerIdleTimeoutMilliseconds * 10000L;
        // 本次判定为空闲、待释放的会话 ID 列表，锁外统一释放。
        List<int> expiredSessions = new List<int>();
        lock (_sync)
        {
            // 会话到 Peer 表的键快照，锁内逐键判定空闲。
            List<int> sessionKeys = new List<int>(_peersBySession.Keys);
            for (int index = 0; index < sessionKeys.Count; index++)
            {
                // 当前待判定的会话键。
                int key = sessionKeys[index];
                // 该会话对应的 Peer，用于读取最后活动时间。
                KcpPeer peer = _peersBySession[key];
                if (nowTicks - peer.LastActiveTicks > idleTicks)
                {
                    expiredSessions.Add(key);
                }
            }
            // 待绑定表键快照，用于清理超过有效期的登记。
            List<uint> convKeys = new List<uint>(_pending.Keys);
            for (int index = 0; index < convKeys.Count; index++)
            {
                // 当前待判定的待绑定键。
                uint key = convKeys[index];
                // 该键对应的待绑定条目，用于比较创建时刻。
                PendingBind item = _pending[key];
                if (nowTicks - item.CreatedTicks > PendingLifetimeTicks)
                {
                    _pending.Remove(key);
                }
            }
        }

        for (int index = 0; index < expiredSessions.Count; index++)
        {
            // 本次待释放的会话 ID。
            int sessionId = expiredSessions[index];
            ReleaseSession(sessionId);
            // 按 ID 取回的网关会话，用于通知其实时 Peer 已失效。
            Session session = SessionMgr.Instance.GetSession(sessionId);
            if (session != null)
            {
                // 已启用后失效：保留“曾启用”标记，让实时下行明确丢弃而不是悄悄退回 TCP。
                session.InvalidateRealtimePeer();
            }
            // 不再向客户端补发 TCP 提示：运行期失效按约定由客户端静默检测后走既有断线恢复，
            // 避免在这里把已启用的实时协议悄悄退回 TCP。
            LogMsg.Info("KCP 会话空闲超时，已解除绑定，SessionID=" + sessionId, LogMsgType.Warn);
        }
    }

    /// <summary>
    /// 取出某 Peer 已完整按序到达的消息并交给所属会话。
    /// 接收线程与更新线程都会调用本方法，因此必须先取得该 Peer 的投递权：
    /// 同一 Peer 只允许一个线程取包并回调，回调在通道锁与投递权之外不会重入，
    /// 从而保证业务回调按 KCP 序号严格串行，不会出现先取出的包后交付。
    /// </summary>
    private void SnapshotDeliver(KcpPeer peer)
    {
        if (!peer.TryBeginDelivery())
        {
            // 已有线程在投递，由它把当前可读消息按序交付完；新到消息留到下一次更新周期。
            return;
        }
        try
        {
            while (true)
            {
                // TryReceive 输出的一条已重组消息，未取到则退出投递循环。
                byte[] message;
                if (!peer.TryReceive(out message))
                {
                    return;
                }
                // 消息归属的会话，为空说明已解绑，停止继续投递。
                Session session = peer.OwnerSession;
                if (session == null)
                {
                    return;
                }
                DeliverPackage(session, message);
            }
        }
        finally
        {
            peer.EndDelivery();
        }
    }

    /// <summary>把一条 KCP 载荷解析为 BasePackage 并交给会话原分发链。</summary>
    private void DeliverPackage(Session session, byte[] message)
    {
        try
        {
            // 由 KCP 载荷反序列化出的业务封包，交给会话原分发链。
            BasePackage basePackage = BasePackage.Parser.ParseFrom(message);
            session.HandleRealtimePackage(basePackage);
        }
        catch (InvalidProtocolBufferException ex)
        {
            // 协议解析失败只丢弃本条，不断开已经认证的可靠通道。
            LogMsg.Info("[UdpNetwork] KCP 载荷解析失败：" + ex.Message, LogMsgType.Warn);
        }
    }

    /// <summary>
    /// 处理一条收到的数据报：绑定请求走握手，数据报走认证与 KCP 输入。
    /// </summary>
    /// <param name="buffer">接收缓冲。</param>
    /// <param name="length">实际字节数。</param>
    /// <param name="source">来源地址。</param>
    private void HandleDatagram(byte[] buffer, int length, IPEndPoint source)
    {
        // 数据报类型，决定走握手、保活还是 KCP 数据分发。
        byte type;
        // 会话号，仅用于路由查表。
        uint conv;
        // 载荷在接收缓冲中的起始下标。
        int payloadOffset;
        // 载荷字节数。
        int payloadLength;
        if (!KcpDatagram.TryReadHeader(buffer, length, out type, out conv,
            out payloadOffset, out payloadLength))
        {
            return;
        }

        if (type == KcpDefine.DatagramTypeBindRequest)
        {
            HandleBindRequest(buffer, conv, payloadOffset, payloadLength, source);
            return;
        }
        if (type == KcpDefine.DatagramTypeKeepAlive)
        {
            HandleKeepAlive(buffer, conv, payloadLength, source);
            return;
        }
        if (type == KcpDefine.DatagramTypeKcpData)
        {
            HandleKcpData(buffer, conv, payloadOffset, payloadLength);
        }
    }

    /// <summary>
    /// 处理保活：认证后刷新活动时间并原样回执，使静止站立玩家不会被空闲超时清理。
    /// 保活不进入业务分发，也不占用 KCP 序号。
    /// </summary>
    /// <param name="buffer">接收缓冲。</param>
    /// <param name="conv">会话号，仅用于查表。</param>
    /// <param name="payloadLength">载荷长度，保活必须为空。</param>
    /// <param name="source">来源地址。</param>
    private void HandleKeepAlive(byte[] buffer, uint conv, int payloadLength, IPEndPoint source)
    {
        if (payloadLength != 0)
        {
            return;
        }
        // 按 conv 查到的已绑定 Peer，未命中直接忽略保活。
        KcpPeer peer;
        lock (_sync)
        {
            if (!_peersByConv.TryGetValue(conv, out peer))
            {
                return;
            }
        }
        if (!KcpDatagram.VerifyMac(buffer, payloadLength, peer.Token))
        {
            return;
        }
        peer.MarkActive();
        SendDatagram(source, KcpDefine.DatagramTypeKeepAlive, conv, peer.Token, null, 0, 0);
    }

    /// <summary>
    /// 处理绑定请求：只校验对空载荷的 MAC，通过后建立 Peer、经 TCP 回启用确认并挂接会话。
    /// </summary>
    private void HandleBindRequest(byte[] buffer, uint conv, int payloadOffset, int payloadLength,
        IPEndPoint source)
    {
        // UDP 不得回传作为密钥的令牌，唯一证明是对空载荷计算的认证码。
        if (payloadLength != 0)
        {
            LogMsg.Info("KCP 绑定被拒绝：绑定载荷应为空", LogMsgType.Warn);
            return;
        }

        // 按 conv 取出的待绑定快照，命中后用于 MAC 复核与完成绑定。
        PendingBind pending;
        // 待绑定表是否存在该 conv：false 时转查已绑定 Peer 的幂等重发路径。
        bool hasPending;
        lock (_sync)
        {
            hasPending = _pending.TryGetValue(conv, out pending);
        }

        if (hasPending)
        {
            if (!KcpDatagram.VerifyMac(buffer, payloadLength, pending.Token))
            {
                LogMsg.Info("KCP 绑定被拒绝：认证码不符", LogMsgType.Warn);
                return;
            }
            CompleteBind(pending, conv, source);
            return;
        }

        // 回执丢失后客户端会重发同一请求：已绑定且 MAC 正确时幂等重发回执与启用确认。
        KcpPeer existing;
        lock (_sync)
        {
            _peersByConv.TryGetValue(conv, out existing);
        }
        if (existing != null)
        {
            if (!KcpDatagram.VerifyMac(buffer, payloadLength, existing.Token))
            {
                LogMsg.Info("KCP 绑定被拒绝：认证码不符", LogMsgType.Warn);
                return;
            }
            existing.MarkActive();
            SendDatagram(source, KcpDefine.DatagramTypeBindAccept, conv, existing.Token, null, 0, 0);
            SendEnabledConfirmation(existing.OwnerSession, conv);
            return;
        }

        // 未分配、已过期或已释放的 conv 一律拒绝，不为其创建任何状态。
        LogMsg.Info("KCP 绑定被拒绝：未知 conv", LogMsgType.Warn);
    }

    /// <summary>
    /// 完成一次绑定：在待绑定表锁内复核快照仍有效，通过后建立 Peer、入表并挂接会话。
    /// 快照取出后若已被 TCP 放弃/断开删除或换给别的会话，直接放弃：不重建 Peer、不重挂会话。
    /// 挂接与释放共用同一把表锁串行，取消先完成时本方法提前返回，不会出现释放后又把 Peer 挂回会话。
    /// </summary>
    /// <param name="pending">接收线程持锁时取出的待绑定快照。</param>
    /// <param name="conv">会话号，同时作为路由键。</param>
    /// <param name="source">客户端 UDP 来源地址，作为认证后的唯一远端。</param>
    /// <param name="session">快照对应的网关会话，方法内先判空再使用。</param>
    private void CompleteBind(PendingBind pending, uint conv, IPEndPoint source)
    {
        // 快照中登记的网关会话，为空说明已解绑，直接放弃完成。
        Session session = pending.Session;
        if (session == null)
        {
            return;
        }

        // 本次绑定新建的通道；仅在复核通过、入表成功后赋值，复核失败保持为空。
        KcpPeer peer;
        lock (_sync)
        {
            // 快照取出后放锁期间，TCP 放弃/断开可能已删除待绑定条目：必须在同一把表锁内
            // 重新确认条目仍属于本会话，与 ReleaseSession 串行，取消完成后不得再重建或重挂。
            // 当前待绑定条目：用于与持有的快照比对归属，键不存在时为默认值。
            PendingBind current;
            // 待绑定条目仍有效标记：存在且归属同一会话才允许继续完成绑定。
            bool stillPending = _pending.TryGetValue(conv, out current);
            if (stillPending && current.Session != session)
            {
                stillPending = false;
            }
            if (!stillPending)
            {
                return;
            }

            _pending.Remove(conv);
            peer = new KcpPeer(this, session.SessionID, conv, pending.Token, source);
            peer.OwnerSession = session;
            // 同一会话重新绑定：释放旧 Peer 后只保留最新一条通道，旧通道数据不再投递。
            // 同会话已存在的旧通道引用，命中时从 conv 表摘除并清空其归属。
            KcpPeer previous;
            if (_peersBySession.TryGetValue(session.SessionID, out previous))
            {
                _peersByConv.Remove(previous.Conv);
                previous.OwnerSession = null;
            }
            _peersBySession[session.SessionID] = peer;
            _peersByConv[conv] = peer;
            // 挂接在表锁内完成：与 ReleaseSession 的删除/Detach 互斥，
            // 释放先执行时本方法已在上面返回，这里挂接后再由释放清理，不会反向挂回。
            session.AttachRealtimePeer(peer);
        }

        // 启用依据必须走可靠 TCP；UDP 回执只是地址证明的快速回执，丢失也不会造成单边启用。
        SendEnabledConfirmation(session, conv);
        SendDatagram(source, KcpDefine.DatagramTypeBindAccept, conv, pending.Token, null, 0, 0);
        LogMsg.Info("KCP 绑定成功，SessionID=" + session.SessionID);
    }

    /// <summary>
    /// 通过已建立的 TCP 会话告知客户端“通道已启用”。该消息不携带认证密钥。
    /// </summary>
    /// <param name="session">目标网关会话，为空时跳过。</param>
    /// <param name="conv">本次启用的会话号，客户端据此核对是否为自己等待的绑定。</param>
    private void SendEnabledConfirmation(Session session, uint conv)
    {
        if (session == null)
        {
            return;
        }
        // 启用确认响应载荷：携带结果码与 conv，令牌保持全零。
        KcpBindPayload payload = new KcpBindPayload();
        payload.Result = KcpBindResult.Enabled;
        payload.Conv = conv;
        // 启用确认不重复下发密钥，令牌保持全零。
        payload.Token = new byte[KcpDefine.BindTokenBytes];
        payload.UdpPort = Port;
        // 该协议不在实时集合内，钩子会保留原 TCP 封包。
        session.SendData(NetDefine.CMD_KcpBindCode, ByteString.CopyFrom(payload.Encode()));
    }

    /// <summary>
    /// 处理 KCP 数据报：认证通过后送入通道，并把认证后的源地址作为当前远端。
    /// </summary>
    private void HandleKcpData(byte[] buffer, uint conv, int payloadOffset, int payloadLength)
    {
        // 按 conv 查到的已绑定 Peer，未绑定或已释放直接丢弃。
        KcpPeer peer;
        lock (_sync)
        {
            if (!_peersByConv.TryGetValue(conv, out peer))
            {
                // 未绑定或已释放的 conv 直接丢弃，不回复也不创建状态。
                return;
            }
        }

        if (!KcpDatagram.VerifyMac(buffer, payloadLength, peer.Token))
        {
            LogMsg.Info("KCP 数据报认证失败，已丢弃，conv=" + conv, LogMsgType.Warn);
            return;
        }

        peer.MarkActive();
        if (!peer.Input(buffer, payloadOffset, payloadLength))
        {
            return;
        }
        // 输入后立即尝试交付，避免等待下一次更新线程造成额外延迟。
        SnapshotDeliver(peer);
    }
}
