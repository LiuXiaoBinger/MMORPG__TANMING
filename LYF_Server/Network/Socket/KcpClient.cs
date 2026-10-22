using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Google.Protobuf;
using KcpProject;

/// <summary>
/// 客户端 KCP 通道：用对空载荷的 MAC 证明 UDP 地址，等待网关经 TCP 回启用确认后承载实时协议。
/// 网关地址与令牌来自 TCP 绑定响应，UDP 只承载移动与技能实时协议。
/// KCP 通道本身不是线程安全的，本类用实例锁把全部通道操作串行化。
/// </summary>
public sealed class KcpClient
{
    /// <summary>通道状态：未开始。</summary>
    private const int StateIdle = 0;

    /// <summary>通道状态：已发出绑定请求，等待 TCP 启用确认。</summary>
    private const int StateBinding = 1;

    /// <summary>通道状态：网关已确认启用，可承载实时协议。</summary>
    private const int StateReady = 2;

    /// <summary>通道状态：绑定失败、运行期失效或已关闭。</summary>
    private const int StateClosed = 3;

    /// <summary>
    /// 通道串行锁。业务发送、UDP 接收线程与更新线程会同时操作同一份 KCP 状态，
    /// Send/Input/Update/PeekSize/Recv 及复用接收缓存必须同锁串行。
    /// 业务回调在锁外执行，避免持锁回调造成阻塞或死锁。
    /// </summary>
    private readonly object _sync = new object();

    /// <summary>Socket 发送锁：发送缓冲与 Socket 发送在多线程间串行，不与通道锁反向嵌套。</summary>
    private readonly object _sendSync = new object();

    /// <summary>UDP Socket，客户端侧使用 Connect 形式固定对端地址。</summary>
    private Socket _socket;

    /// <summary>底层 KCP 通道。</summary>
    private KCP _channel;

    /// <summary>接收线程。</summary>
    private Thread _receiveThread;

    /// <summary>更新线程，驱动重传、绑定期限、保活与静默检测。</summary>
    private Thread _updateThread;

    /// <summary>当前状态，跨线程以 Interlocked 读写。</summary>
    private int _state = StateIdle;

    /// <summary>网关会话号，仅用于数据报查表。</summary>
    private uint _conv;

    /// <summary>绑定令牌，同时作为数据报认证密钥；只在内存中使用，不回传 UDP。</summary>
    private byte[] _token;

    /// <summary>网关 UDP 地址。</summary>
    private IPEndPoint _remote;

    /// <summary>接收缓存，按 MTU 分配并复用；只在通道锁内使用。</summary>
    private readonly byte[] _receiveBuffer = new byte[KcpDefine.KcpMtu];

    /// <summary>数据报缓冲，用于封装待发送的 KCP 段。</summary>
    private readonly byte[] _sendBuffer = new byte[KcpDefine.MaxDatagramBytes];

    /// <summary>接收数据报缓冲；与发送缓冲分离，避免收发线程互相覆盖。</summary>
    private readonly byte[] _datagramBuffer = new byte[KcpDefine.MaxDatagramBytes + 64];

    /// <summary>最近一次收到已认证数据报的刻度，用于运行期静默检测。</summary>
    private long _lastInboundTicks;

    /// <summary>最近一次发出绑定请求的刻度，用于有限次重发。</summary>
    private long _lastBindRequestTicks;

    /// <summary>最近一次发出保活的刻度。</summary>
    private long _lastKeepAliveTicks;

    /// <summary>绑定期限刻度，超过后确定保持 TCP。</summary>
    private long _bindDeadlineTicks;

    /// <summary>是否已收到网关的 UDP 地址证明回执，用于停止无谓重发。</summary>
    private int _pathAcked;

    /// <summary>
    /// 是否曾经收到过启用确认。用于区分“初次绑定未成功”（可继续走 TCP）
    /// 与“启用后失效”（实时协议必须丢弃，不能换通道重发）。
    /// </summary>
    private int _everEnabled;

    /// <summary>收到一条完整协议包时回调，参数为 BasePackage 序列化字节。</summary>
    public Action<byte[]> OnPackageReceived;

    /// <summary>网关经 TCP 确认启用后回调，由网络层记录状态，不直接切换业务。</summary>
    public Action OnBound;

    /// <summary>初次绑定失败回调：本会话确定保持 TCP，不触发断线恢复。</summary>
    public Action OnBindFailed;

    /// <summary>运行期通道失效回调：由网络层触发现有 TCP 断线恢复。</summary>
    public Action OnFailed;

    /// <summary>通道是否已经启用并可用；只有 TCP 启用确认能置位。</summary>
    public bool IsReady
    {
        get { return Volatile.Read(ref _state) == StateReady; }
    }

    /// <summary>
    /// 本通道是否曾经启用成功。启用后失效时实时协议必须丢弃而不是改投 TCP。
    /// </summary>
    public bool HasBeenEnabled
    {
        get { return Volatile.Read(ref _everEnabled) == 1; }
    }

    /// <summary>
    /// 建立本通道时的 TCP 连接代次，由网络层在创建时写入。
    /// 旧连接的失败回调据此判断是否仍应释放本通道，避免误关重连后新建的通道。
    /// </summary>
    public long OwnerGeneration { get; set; }

    /// <summary>
    /// 判断一次连接失败通知是否应释放本通道：只有通知代次与通道建立代次一致才释放。
    /// </summary>
    /// <param name="failedGeneration">失败通知携带的连接代次。</param>
    /// <returns>属于同一次连接、可以释放时返回 true。</returns>
    public bool ShouldReleaseOnFailure(long failedGeneration)
    {
        if (OwnerGeneration <= 0)
        {
            return false;
        }
        if (failedGeneration <= 0)
        {
            return false;
        }
        return OwnerGeneration == failedGeneration;
    }

    /// <summary>
    /// 发起绑定：创建 UDP Socket、发出首个绑定请求并启动驱动线程。
    /// </summary>
    /// <param name="host">网关主机地址，与 TCP 网关一致。</param>
    /// <param name="udpPort">网关下发的 UDP 端口。</param>
    /// <param name="conv">网关分配的会话号。</param>
    /// <param name="token">网关下发的 16 字节令牌，只保存在内存中。</param>
    /// <returns>本地 Socket 创建成功并已发出绑定请求时返回 true。</returns>
    public bool Start(string host, int udpPort, uint conv, byte[] token)
    {
        if (token == null)
        {
            return false;
        }
        if (token.Length != KcpDefine.BindTokenBytes)
        {
            return false;
        }
        if (udpPort <= 0 || udpPort > 65535)
        {
            return false;
        }
        if (Volatile.Read(ref _state) != StateIdle)
        {
            return false;
        }

        try
        {
            IPAddress address = IPAddress.Parse(host);
            _remote = new IPEndPoint(address, udpPort);
            _conv = conv;
            _token = token;

            // 新建的 UDP Socket，Connect 后固定对端地址，便于直接 Receive。
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            // 客户端固定对端地址，便于用 Receive 直接读取且拒绝其他来源的报文。
            socket.Connect(_remote);
            _socket = socket;

            KCP channel = new KCP(conv, OnKcpOutput);
            channel.SetMtu(KcpDefine.KcpMtu);
            channel.NoDelay(1, KcpDefine.UpdateIntervalMilliseconds, 2, 1);
            channel.WndSize(128, 128);
            _channel = channel;

            // 启动时刻刻度，用于初始化活动时间与计算绑定期限。
            long nowTicks = DateTime.UtcNow.Ticks;
            Volatile.Write(ref _lastInboundTicks, nowTicks);
            Volatile.Write(ref _lastKeepAliveTicks, nowTicks);
            _bindDeadlineTicks = nowTicks + KcpDefine.BindTimeoutMilliseconds * 10000L;
            Volatile.Write(ref _state, StateBinding);

            // 首个请求在线程启动前发出，避免与更新线程的发送缓冲竞争。
            SendBindRequest(nowTicks);

            _receiveThread = new Thread(ReceiveLoop);
            _receiveThread.IsBackground = true;
            _receiveThread.Name = "KcpClientReceive";
            _receiveThread.Start();

            _updateThread = new Thread(UpdateLoop);
            _updateThread.IsBackground = true;
            _updateThread.Name = "KcpClientUpdate";
            _updateThread.Start();
            return true;
        }
        catch (Exception ex)
        {
            Volatile.Write(ref _state, StateClosed);
            // 识别出的 SocketException 单独记录系统错误码，其余异常只记录消息。
            SocketException socketException = ex as SocketException;
            if (socketException == null)
            {
                LogMsg.Info("[KcpClient] 启动失败：" + ex.Message, LogMsgType.Error);
            }
            else
            {
                LogMsg.Info("[KcpClient] 启动失败，socketErrorCode=" +
                    (int)socketException.SocketErrorCode, LogMsgType.Error);
            }
            return false;
        }
    }

    /// <summary>
    /// 发送一条实时协议载荷。调用方需结合 HasBeenEnabled 判定失败语义：
    /// 从未启用时保持 TCP，启用后失效时整条丢弃。
    /// </summary>
    /// <param name="payload">BasePackage 序列化字节。</param>
    /// <returns>写入 KCP 通道成功时返回 true。</returns>
    public bool Send(byte[] payload)
    {
        if (payload == null)
        {
            return false;
        }
        if (payload.Length == 0)
        {
            return false;
        }
        // 绑定未完成时不写入通道，避免实时协议在两条连接上混发。
        if (Volatile.Read(ref _state) != StateReady)
        {
            return false;
        }
        lock (_sync)
        {
            KCP channel = _channel;
            if (channel == null)
            {
                return false;
            }
            // 通道写入返回值：非负表示已送入发送队列，负值为协议错误。
            int result = channel.Send(payload, 0, payload.Length);
            return result >= 0;
        }
    }

    /// <summary>
    /// 接收网关经 TCP 下发的启用确认；只有本次绑定的 conv 且仍在等待时才生效。
    /// </summary>
    /// <param name="conv">确认携带的会话号。</param>
    /// <returns>确认被接受并切换到可用状态时返回 true。</returns>
    public bool ConfirmEnabled(uint conv)
    {
        if (conv != _conv)
        {
            return false;
        }
        // 用 CAS 保证并发重复确认只切换一次。
        int previous = Interlocked.CompareExchange(ref _state, StateReady, StateBinding);
        if (previous != StateBinding)
        {
            return false;
        }
        Volatile.Write(ref _lastInboundTicks, DateTime.UtcNow.Ticks);
        Volatile.Write(ref _everEnabled, 1);
        // 本地缓存的启用成功回调，确认生效后在锁外触发，避免回调内重入状态切换。
        Action bound = OnBound;
        if (bound != null)
        {
            bound.Invoke();
        }
        return true;
    }

    /// <summary>
    /// 关闭通道并释放线程与 Socket；TCP 断开或重连时调用，不触发失败回调。
    /// </summary>
    public void Close()
    {
        CloseInternal(false, false);
    }

    /// <summary>
    /// 统一的关闭入口：按原因只回调一次，避免重复走断线恢复。
    /// </summary>
    /// <param name="runtimeFailed">运行期失效，需要触发现有 TCP 断线恢复。</param>
    /// <param name="bindFailed">初次绑定失败，本会话保持 TCP。</param>
    private void CloseInternal(bool runtimeFailed, bool bindFailed)
    {
        // 交换出的旧状态：仅旧态为绑定中或就绪时才需要回调结果通知。
        int previous = Interlocked.Exchange(ref _state, StateClosed);
        // 交换出的旧 Socket，置空后关闭，避免关闭后仍被发送拿到。
        Socket socket = Interlocked.Exchange(ref _socket, null);
        if (socket != null)
        {
            try
            {
                socket.Close();
            }
            catch (Exception ex)
            {
                LogMsg.Info("[KcpClient] 关闭 Socket 失败：" + ex.Message, LogMsgType.Error);
            }
        }

        // 已关闭或从未启动的实例不产生回调，保证每次绑定最多一次结果通知。
        if (previous != StateBinding && previous != StateReady)
        {
            return;
        }
        if (runtimeFailed)
        {
            // 本地缓存的运行期失效回调，触发一次现有 TCP 断线恢复。
            Action failed = OnFailed;
            if (failed != null)
            {
                failed.Invoke();
            }
            return;
        }
        if (bindFailed)
        {
            // 本地缓存的初次绑定失败回调，本会话确定保持 TCP。
            Action bindFailure = OnBindFailed;
            if (bindFailure != null)
            {
                bindFailure.Invoke();
            }
        }
    }

    /// <summary>KCP 输出回调：把待发送段封装为数据报写回 UDP。</summary>
    /// <param name="buffer">KCP 段缓冲。</param>
    /// <param name="length">段字节数。</param>
    private void OnKcpOutput(byte[] buffer, int length)
    {
        SendDatagram(KcpDefine.DatagramTypeKcpData, buffer, length);
    }

    /// <summary>发出一次绑定请求：载荷为空，唯一证明是对空载荷计算的认证码。</summary>
    /// <param name="nowTicks">本次发送时刻刻度。</param>
    private void SendBindRequest(long nowTicks)
    {
        SendDatagram(KcpDefine.DatagramTypeBindRequest, null, 0);
        Volatile.Write(ref _lastBindRequestTicks, nowTicks);
    }

    /// <summary>发出一次保活；网关回执用于刷新本端入向活动时间。</summary>
    /// <param name="nowTicks">本次发送时刻刻度。</param>
    private void SendKeepAlive(long nowTicks)
    {
        SendDatagram(KcpDefine.DatagramTypeKeepAlive, null, 0);
        Volatile.Write(ref _lastKeepAliveTicks, nowTicks);
    }

    /// <summary>封装并发送一条数据报。</summary>
    /// <param name="type">数据报类型。</param>
    /// <param name="payload">载荷，可为 null。</param>
    /// <param name="length">载荷字节数。</param>
    private void SendDatagram(byte type, byte[] payload, int length)
    {
        lock (_sendSync)
        {
            // 取一次当前 Socket 引用，停止置空则放弃本次发送。
            Socket socket = _socket;
            // 取一次当前令牌引用，作为本次数据报的 HMAC 密钥。
            byte[] token = _token;
            if (socket == null || token == null)
            {
                return;
            }
            // KcpDatagram.Write 返回实际写入字节数，非正表示封装失败。
            int written = KcpDatagram.Write(_sendBuffer, type, _conv, token, payload, 0, length);
            if (written <= 0)
            {
                return;
            }
            try
            {
                socket.Send(_sendBuffer, 0, written, SocketFlags.None);
            }
            catch (Exception ex)
            {
                // 识别出的 SocketException 单独记录错误码，其余异常只记录消息。
                SocketException socketException = ex as SocketException;
                if (socketException == null)
                {
                    LogMsg.Info("[KcpClient] 发送失败：" + ex.Message, LogMsgType.Error);
                }
                else
                {
                    LogMsg.Info("[KcpClient] 发送失败，socketErrorCode=" +
                        (int)socketException.SocketErrorCode, LogMsgType.Error);
                }
            }
        }
    }

    /// <summary>接收线程：读取数据报、校验认证并按类型送入通道或记录活动。</summary>
    private void ReceiveLoop()
    {
        while (Volatile.Read(ref _state) == StateBinding || Volatile.Read(ref _state) == StateReady)
        {
            // 每轮取一次当前 Socket，停止置空则退出接收循环。
            Socket socket = _socket;
            if (socket == null)
            {
                break;
            }
            try
            {
                // 本次实际收到的数据报字节数，<=0 跳过继续等待。
                int length = socket.Receive(_datagramBuffer, 0, _datagramBuffer.Length, SocketFlags.None);
                if (length <= 0)
                {
                    continue;
                }
                HandleDatagram(_datagramBuffer, length);
            }
            catch (ObjectDisposedException)
            {
                // 正常关闭时 Socket 已释放，退出接收循环。
                break;
            }
            catch (SocketException ex)
            {
                // 系统级 Socket 错误：已关闭则退出，运行期记录错误码后继续。
                if (Volatile.Read(ref _state) == StateClosed)
                {
                    break;
                }
                LogMsg.Info("[KcpClient] 接收失败，socketErrorCode=" +
                    (int)ex.SocketErrorCode, LogMsgType.Warn);
            }
            catch (Exception ex)
            {
                // 其他未预期异常：已关闭则退出，运行期记录后继续。
                if (Volatile.Read(ref _state) == StateClosed)
                {
                    break;
                }
                LogMsg.Info("[KcpClient] 接收异常：" + ex.Message, LogMsgType.Error);
            }
        }
    }

    /// <summary>
    /// 更新线程：驱动重传、绑定期限与保活，并在就绪后按序交付消息。
    /// </summary>
    private void UpdateLoop()
    {
        while (Volatile.Read(ref _state) == StateBinding || Volatile.Read(ref _state) == StateReady)
        {
            try
            {
                // 本次更新周期的当前时刻刻度，用于期限、重发与静默判定。
                long nowTicks = DateTime.UtcNow.Ticks;
                // 本次周期读到的通道状态，绑定中与就绪走不同分支。
                int state = Volatile.Read(ref _state);
                if (state == StateBinding)
                {
                    if (nowTicks > _bindDeadlineTicks)
                    {
                        // 期限到仍未拿到 TCP 启用确认：本次会话确定保持 TCP，网关也不会挂接 Peer。
                        CloseInternal(false, true);
                        break;
                    }
                    if (Volatile.Read(ref _pathAcked) == 0)
                    {
                        // 最近一次发出绑定请求的刻度，达到重发间隔才再次发送。
                        long lastRequest = Interlocked.Read(ref _lastBindRequestTicks);
                        if (nowTicks - lastRequest >= KcpDefine.BindRetryIntervalMilliseconds * 10000L)
                        {
                            SendBindRequest(nowTicks);
                        }
                    }
                }
                else
                {
                    // 最近一次收到已认证数据报的刻度，超过静默阈值判定运行期失效。
                    long lastInbound = Interlocked.Read(ref _lastInboundTicks);
                    if (nowTicks - lastInbound > KcpDefine.ClientSilenceTimeoutMilliseconds * 10000L)
                    {
                        // 运行期静默超时：不再向 TCP 改投已发出的实时协议，直接走既有断线恢复。
                        LogMsg.Info("[KcpClient] KCP 通道静默超时，触发断线恢复", LogMsgType.Warn);
                        CloseInternal(true, false);
                        break;
                    }
                    // 最近一次发出保活的刻度，达到保活间隔才再次发送。
                    long lastKeepAlive = Interlocked.Read(ref _lastKeepAliveTicks);
                    if (nowTicks - lastKeepAlive >= KcpDefine.KeepAliveIntervalMilliseconds * 10000L)
                    {
                        SendKeepAlive(nowTicks);
                    }
                }

                lock (_sync)
                {
                    KCP channel = _channel;
                    if (channel != null)
                    {
                        channel.Update();
                    }
                }
                DeliverPackages();
                Thread.Sleep(KcpDefine.UpdateIntervalMilliseconds);
            }
            catch (Exception ex)
            {
                // 更新周期内未预期异常：已关闭则退出，运行期记录后休眠一个周期再继续。
                if (Volatile.Read(ref _state) == StateClosed)
                {
                    break;
                }
                LogMsg.Info("[KcpClient] 更新异常：" + ex.Message, LogMsgType.Error);
                Thread.Sleep(KcpDefine.UpdateIntervalMilliseconds);
            }
        }
    }

    /// <summary>取出所有已完整到达的协议包并回调业务层；回调在通道锁外执行。</summary>
    private void DeliverPackages()
    {
        // 尚未拿到 TCP 启用确认时不交付业务消息，避免半启用状态下混通道投递。
        if (Volatile.Read(ref _state) != StateReady)
        {
            return;
        }
        while (true)
        {
            // 锁内取出的一条完整消息，交给锁外业务回调。
            byte[] message;
            lock (_sync)
            {
                KCP channel = _channel;
                if (channel == null)
                {
                    return;
                }
                // 通道中已重组完成、可一次性取出的下一条消息长度，无消息时 <=0。
                int peekSize = channel.PeekSize();
                if (peekSize <= 0)
                {
                    return;
                }
                // KCP 已按 MTU 分片重组；重组后仍超出接收缓存说明载荷大于 KcpDefine.KcpMtu，
                // 属对端违反约定，丢弃本条并继续处理后续可靠数据。
                if (peekSize > _receiveBuffer.Length)
                {
                    // 超长消息丢弃缓冲：取出即释放队列，避免阻塞后续可靠数据。
                    byte[] discard = new byte[peekSize];
                    channel.Recv(discard, 0, peekSize);
                    continue;
                }
                // 实际取出的消息字节数，<=0 表示本次未取到完整消息。
                int receivedLength = channel.Recv(_receiveBuffer, 0, _receiveBuffer.Length);
                if (receivedLength <= 0)
                {
                    return;
                }
                message = new byte[receivedLength];
                Buffer.BlockCopy(_receiveBuffer, 0, message, 0, receivedLength);
            }
            // 本地缓存的业务包回调，为空则跳过，在锁外触发避免重入。
            Action<byte[]> handler = OnPackageReceived;
            if (handler != null)
            {
                handler.Invoke(message);
            }
        }
    }

    /// <summary>处理一条收到的数据报。</summary>
    /// <param name="buffer">接收缓冲。</param>
    /// <param name="length">实际字节数。</param>
    private void HandleDatagram(byte[] buffer, int length)
    {
        // 数据报类型，决定走回执、拒绝、保活还是 KCP 数据输入。
        byte type;
        // 会话号，与本通道 _conv 不符直接丢弃。
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
        // conv 不符说明不是本会话报文，丢弃。
        if (conv != _conv)
        {
            return;
        }
        if (!KcpDatagram.VerifyMac(buffer, payloadLength, _token))
        {
            LogMsg.Info("[KcpClient] 数据报认证失败，已丢弃", LogMsgType.Warn);
            return;
        }

        // 任何通过认证的数据报都算入向活动，驱动静默检测。
        Volatile.Write(ref _lastInboundTicks, DateTime.UtcNow.Ticks);

        if (type == KcpDefine.DatagramTypeBindAccept)
        {
            // 地址证明回执：只用于停止重发，启用与否仍由 TCP 确认决定。
            Interlocked.Exchange(ref _pathAcked, 1);
            return;
        }
        if (type == KcpDefine.DatagramTypeBindReject)
        {
            CloseInternal(false, true);
            return;
        }
        if (type == KcpDefine.DatagramTypeKeepAlive)
        {
            // 保活回执只刷新活动时间，不进入业务分发。
            return;
        }
        if (type == KcpDefine.DatagramTypeKcpData)
        {
            lock (_sync)
            {
                KCP channel = _channel;
                if (channel != null)
                {
                    channel.Input(buffer, payloadOffset, payloadLength, true, false);
                }
            }
        }
    }
}
