using System;

/// <summary>
/// KCP 传输层常量：UDP 端口、数据报预算、数据报类型与绑定协议号。
/// 所有数值均为协议约定，客户端与网关必须一致，改动等于协议升级。
/// </summary>
public static class KcpDefine
{
    /// <summary>网关 KCP UDP 默认监听端口；未配置时使用该值，可与 TCP 10140 独立改端口。</summary>
    public const int GateUdpPort = 10141;

    /// <summary>单个 UDP 数据报总字节上限（含 28 字节认证头）；不得大于 1200 以免路径分片。</summary>
    public const int MaxDatagramBytes = 1200;

    /// <summary>外层认证头长度：魔术字 4 + 版本 1 + 类型 1 + conv 4 + 载荷长度 2 + 认证码 16。</summary>
    public const int DatagramHeaderBytes = 28;

    /// <summary>KCP 段最大传输单元：数据报预算减去外层认证头，保证整包不超 1200 字节。</summary>
    public const int KcpMtu = MaxDatagramBytes - DatagramHeaderBytes;

    /// <summary>数据报魔术字，用于快速丢弃非本协议报文，字节序为小端写入。</summary>
    public const uint DatagramMagic = 0x504B4B4Cu;

    /// <summary>数据报外层版本号；不匹配时按旧或未知协议丢弃。</summary>
    public const byte DatagramVersion = 1;

    /// <summary>数据报类型：客户端请求绑定到其 TCP 会话。</summary>
    public const byte DatagramTypeBindRequest = 1;

    /// <summary>数据报类型：网关确认绑定成功，客户端此后可发送 KCP 数据。</summary>
    public const byte DatagramTypeBindAccept = 2;

    /// <summary>数据报类型：网关拒绝绑定，客户端本次会话保持 TCP。</summary>
    public const byte DatagramTypeBindReject = 3;

    /// <summary>数据报类型：KCP 协议段载荷。</summary>
    public const byte DatagramTypeKcpData = 4;

    /// <summary>
    /// 数据报类型：双向保活。客户端定时发送、网关原样回执，只刷新活动时间，不进入业务分发。
    /// 该类型不携带 KCP 段，因此不会打乱可靠通道的序号。
    /// </summary>
    public const byte DatagramTypeKeepAlive = 5;

    /// <summary>
    /// 绑定令牌字节数，128 位随机值，同时作为该会话数据报的认证密钥。
    /// 该令牌只在 TCP 上下发，绝不出现在 UDP 数据报载荷中，UDP 侧只出现其 MAC 结果。
    /// </summary>
    public const int BindTokenBytes = 16;

    /// <summary>数据报认证码截断字节数；HMAC-SHA256 前 16 字节足以抵抗伪造且控制带宽。</summary>
    public const int MacBytes = 16;

    /// <summary>KCP 更新间隔毫秒；与协议内部 interval 一致，作为驱动线程每次休眠基准。</summary>
    public const int UpdateIntervalMilliseconds = 10;

    /// <summary>Peer 空闲超时毫秒；超过后释放并让客户端走既有断线恢复。</summary>
    public const int PeerIdleTimeoutMilliseconds = 30000;

    /// <summary>
    /// 客户端绑定请求重发间隔毫秒。绑定请求本身可能丢失，网关只有在收到合法请求后才建立 Peer，
    /// 因此客户端必须有有限次重发，否则两端会长期停在“一边启用、一边等确认”的分裂状态。
    /// </summary>
    public const int BindRetryIntervalMilliseconds = 300;

    /// <summary>
    /// 客户端绑定总期限毫秒。期限到仍未收到 TCP 启用确认时关闭 UDP 通道，
    /// 本会话确定保持 TCP，不再重试，避免半个通道长期悬挂。
    /// </summary>
    public const int BindTimeoutMilliseconds = 3000;

    /// <summary>
    /// 网关待绑定条目有效期毫秒。与客户端绑定期限取同一值：
    /// 客户端超时后不会再发绑定请求，网关若保留更久就会出现“客户端已放弃、网关仍挂着待绑定”的单边残留。
    /// 客户端超时还会经可靠 TCP 主动回传放弃结果，网关据此立即清理可能已挂接的 Peer。
    /// </summary>
    public const int BindPendingLifetimeMilliseconds = BindTimeoutMilliseconds;

    /// <summary>客户端保活间隔毫秒；明显小于网关空闲超时，保证静止站立玩家不会被误判掉线。</summary>
    public const int KeepAliveIntervalMilliseconds = 5000;

    /// <summary>
    /// 客户端入向静默超时毫秒。通道就绪后超过该时间没有任何已认证数据报（含保活回执），
    /// 判定运行期通道失效并触发现有 TCP 断线恢复，不把已发出的实时协议改投 TCP。
    /// </summary>
    public const int ClientSilenceTimeoutMilliseconds = 15000;

    /// <summary>
    /// 判断协议号是否属于实时通道集合。
    /// </summary>
    /// <param name="protoCode">待判定的业务协议号。</param>
    /// <returns>属于移动或技能实时集合时返回 true，否则走 TCP。</returns>
    public static bool IsRealtimeProtocol(int protoCode)
    {
        // 移动上报与附近移动通知共用实时通道，保持先后顺序。
        if (protoCode == NetDefine.CMD_MoveOperationCode)
        {
            return true;
        }
        if (protoCode == NetDefine.CMD_SyncMoveCode)
        {
            return true;
        }
        // 施法请求与权威结果共用同一可靠通道，避免结果先于请求到达。
        if (protoCode == NetDefine.CMD_SkillCastCode)
        {
            return true;
        }
        // 技能开始与结束表现通知必须保序，避免远端动作倒序或停留。
        if (protoCode == NetDefine.CMD_SkillCastStartCode)
        {
            return true;
        }
        if (protoCode == NetDefine.CMD_SkillCastEndCode)
        {
            return true;
        }
        return false;
    }
}
