/// <summary>
/// KCP 绑定结果码，用于 TCP 绑定响应告知客户端是否可以启用实时通道，
/// 也用于客户端经可靠 TCP 主动回传“已放弃本次绑定”。
/// </summary>
public enum KcpBindResult : byte
{
    /// <summary>绑定参数已下发，可开始 UDP 握手。</summary>
    Granted = 0,

    /// <summary>当前 TCP 会话尚未确认在线角色，拒绝下发参数。</summary>
    SessionNotReady = 1,

    /// <summary>网关未启用 UDP 通道或分配失败。</summary>
    ServerUnavailable = 2,

    /// <summary>
    /// 网关已收到客户端合法的 UDP 地址证明并挂接 Peer，通知客户端把实时协议切到 KCP。
    /// 该结果经可靠 TCP 下发，是客户端唯一的启用依据；UDP 回执丢失不会造成单边启用。
    /// </summary>
    Enabled = 3,

    /// <summary>
    /// 客户端在绑定期限内未拿到启用确认，已放弃本次绑定并经可靠 TCP 回传本结果。
    /// 网关据此释放待绑定条目与可能已挂接的 Peer，保证两端最终都只保留 TCP，不残留单边通道。
    /// </summary>
    Abandoned = 4,
}
