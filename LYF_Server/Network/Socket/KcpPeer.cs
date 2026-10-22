using System;
using System.Net;
using System.Threading;
using KcpProject;

/// <summary>
/// 网关侧单个 KCP 会话：持有 KCP 通道、远端地址、会话令牌和收发入口。
/// 一个 Peer 只服务一个已认证的 TCP 会话，conv 仅用于查表。
/// KCP 通道本身不是线程安全的，本类用实例锁把全部通道操作串行化。
/// </summary>
public sealed class KcpPeer
{
    /// <summary>所属 UDP 网络，用于输出 KCP 段和回报异常。</summary>
    private readonly UdpNetwork _owner;

    /// <summary>
    /// 通道串行锁。TCP 下行、UDP 接收线程、更新线程会同时调用
    /// Send/Input/Update/PeekSize/Recv，KCP 只保护段池，不保护通道状态，必须由本锁串行。
    /// 业务分发不在锁内执行，避免持锁回调业务造成长时间占用。
    /// </summary>
    private readonly object _sync = new object();

    /// <summary>最近一次收到合法数据报的计时器刻度，用于空闲超时清理；跨线程以原子读写访问。</summary>
    private long _lastActiveTicks;

    /// <summary>
    /// 投递独占标记：0 表示无人在投递，1 表示已有线程取得投递权。
    /// KCP 按序完整到达，但“取出到回调完成”之间存在时间窗口；若允许第二个线程同时取包，
    /// 先取出的消息可能后交付。取得投递权的线程负责按序交付直到通道暂空，业务回调不持通道锁。
    /// </summary>
    private int _delivering;

    /// <summary>本 Peer 绑定的 TCP 会话 ID，用于下行选路。</summary>
    public int SessionId { get; private set; }

    /// <summary>会话号，仅用于数据报查表，不能作为身份凭据。</summary>
    public uint Conv { get; private set; }

    /// <summary>16 字节会话令牌，作为该 Peer 数据报的认证密钥；只在网关与客户端内存中保存。</summary>
    public byte[] Token { get; private set; }

    /// <summary>当前远端地址；认证通过后才会更新，避免被伪造源地址劫持。</summary>
    public IPEndPoint Remote { get; set; }

    /// <summary>本 Peer 关联的 TCP 会话；解绑或释放时置空，避免继续向旧角色交付。</summary>
    public Session OwnerSession { get; set; }

    /// <summary>底层 KCP 通道实例，负责可靠有序交付。</summary>
    public KCP Channel { get; private set; }

    /// <summary>接收缓存，单条 KCP 消息按 MTU 分配，复用以避免频繁分配；只在通道锁内使用。</summary>
    private readonly byte[] _receiveBuffer = new byte[KcpDefine.KcpMtu];

    /// <summary>
    /// 创建一个已认证的 KCP 会话。
    /// </summary>
    /// <param name="owner">所属 UDP 网络，用于把 KCP 段写回网络。</param>
    /// <param name="sessionId">绑定的 TCP 会话 ID。</param>
    /// <param name="conv">网关分配的会话号。</param>
    /// <param name="token">绑定令牌，与客户端一致。</param>
    /// <param name="remote">客户端 UDP 地址。</param>
    public KcpPeer(UdpNetwork owner, int sessionId, uint conv, byte[] token, IPEndPoint remote)
    {
        _owner = owner;
        SessionId = sessionId;
        Conv = conv;
        Token = token;
        Remote = remote;
        Channel = new KCP(conv, OnKcpOutput);
        // MTU 与数据报预算保持一致，整包不会超过 1200 字节。
        Channel.SetMtu(KcpDefine.KcpMtu);
        // 低延迟模式：nodelay=1、interval=10ms、快速重传 2 次、关闭拥塞窗口自适应。
        Channel.NoDelay(1, KcpDefine.UpdateIntervalMilliseconds, 2, 1);
        Channel.WndSize(128, 128);
        _lastActiveTicks = DateTime.UtcNow.Ticks;
    }

    /// <summary>最后活动时间刻度，供 UdpNetwork 判定空闲超时。</summary>
    public long LastActiveTicks
    {
        get { return Interlocked.Read(ref _lastActiveTicks); }
    }

    /// <summary>
    /// 刷新活动时间；只由已通过认证的数据报调用。
    /// </summary>
    public void MarkActive()
    {
        Interlocked.Exchange(ref _lastActiveTicks, DateTime.UtcNow.Ticks);
    }

    /// <summary>
    /// 把一条 KCP 段送入通道。
    /// </summary>
    /// <param name="payload">数据报中的 KCP 载荷。</param>
    /// <param name="offset">载荷起始下标。</param>
    /// <param name="length">载荷字节数。</param>
    /// <returns>输入被接受时返回 true。</returns>
    public bool Input(byte[] payload, int offset, int length)
    {
        if (payload == null)
        {
            return false;
        }
        lock (_sync)
        {
            // regular 为 true 表示按协议段解析；关闭立即 ACK 以合并确认减少小包。
            int result = Channel.Input(payload, offset, length, true, false);
            return result >= 0;
        }
    }

    /// <summary>
    /// 发送一条完整协议载荷，由 KCP 负责分段、重传与顺序。
    /// </summary>
    /// <param name="payload">已序列化的 BasePackage 字节。</param>
    /// <returns>写入通道成功时返回 true。</returns>
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
        lock (_sync)
        {
            // 通道写入返回值：非负表示已送入发送队列，负值为协议错误。
            int result = Channel.Send(payload, 0, payload.Length);
            return result >= 0;
        }
    }

    /// <summary>
    /// 驱动一次 KCP 更新，触发重传与确认。输出段在锁内写出，保证发送顺序与通道状态一致。
    /// </summary>
    public void Update()
    {
        lock (_sync)
        {
            Channel.Update();
        }
    }

    /// <summary>
    /// 取出一条已按序完整到达的消息。锁只覆盖通道读取与拷贝，返回后由调用方在锁外分发。
    /// </summary>
    /// <param name="message">输出的消息字节；没有可读消息时为 null。</param>
    /// <returns>取到消息返回 true。</returns>
    public bool TryReceive(out byte[] message)
    {
        message = null;
        lock (_sync)
        {
            // 通道中已重组完成、可一次性取出的下一条消息长度，无消息时 <=0。
            int peekSize = Channel.PeekSize();
            if (peekSize <= 0)
            {
                return false;
            }
            // 单条消息不得超出接收缓存。KCP 会把超长业务包分片重组，重组后仍超出
            // 接收缓存说明载荷大于 KcpDefine.KcpMtu，属对端违反约定，丢弃本条。
            if (peekSize > _receiveBuffer.Length)
            {
                // 超长消息丢弃缓冲：取出即释放通道队列，避免阻塞后续合法消息。
                byte[] discard = new byte[peekSize];
                Channel.Recv(discard, 0, peekSize);
                return false;
            }
            // 实际取出的消息字节数，<=0 表示本次未取到完整消息。
            int receivedLength = Channel.Recv(_receiveBuffer, 0, _receiveBuffer.Length);
            if (receivedLength <= 0)
            {
                return false;
            }
            // 拷贝出的独立消息副本，交给调用方在锁外分发。
            byte[] received = new byte[receivedLength];
            Buffer.BlockCopy(_receiveBuffer, 0, received, 0, receivedLength);
            message = received;
            return true;
        }
    }

    /// <summary>
    /// 尝试取得本 Peer 的投递权。接收线程与更新线程都会触发投递，
    /// 只有取得投递权的一方能取包并回调，保证同一 Peer 的业务回调串行且严格按 KCP 序号。
    /// </summary>
    /// <returns>取得投递权返回 true；已有线程在投递时返回 false，交由该线程继续交付。</returns>
    public bool TryBeginDelivery()
    {
        return Interlocked.CompareExchange(ref _delivering, 1, 0) == 0;
    }

    /// <summary>释放投递权；必须与 TryBeginDelivery 成对调用。</summary>
    public void EndDelivery()
    {
        Interlocked.Exchange(ref _delivering, 0);
    }

    /// <summary>
    /// KCP 输出回调：把待发送的协议段交给所属 UDP 网络封装并发送。
    /// 本回调在通道锁内被调用，UDP 发送侧用自己的发送锁保护 Socket，两者不会反向嵌套。
    /// </summary>
    /// <param name="buffer">KCP 段缓冲，由协议库复用。</param>
    /// <param name="length">段字节数。</param>
    private void OnKcpOutput(byte[] buffer, int length)
    {
        _owner.SendPeerSegment(this, buffer, length);
    }
}
