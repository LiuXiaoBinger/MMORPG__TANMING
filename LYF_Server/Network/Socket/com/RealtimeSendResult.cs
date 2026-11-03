/// <summary>
/// 实时通道发送结果：分别表示继续使用 TCP、已交给 KCP 或本条丢弃。
/// 传输钩子返回本枚举而不是布尔值，避免通道失效时实时协议被静默改投 TCP 造成重复执行或乱序。
/// </summary>
public enum RealtimeSendResult : byte
{
    /// <summary>本次不由玩家 KCP 通道承载，例如常规协议或服务之间的连接，继续走 TCP。</summary>
    KeepTcp = 0,

    /// <summary>已由实时通道接管并写入成功，不再走 TCP。</summary>
    Sent = 1,

    /// <summary>
    /// 玩家实时通道未就绪或写入失败，本条丢弃；也包括从未绑定成功的情况。
    /// 已发出的移动与技能无法确定对端是否执行过，改投 TCP 会造成重复执行，
    /// 恢复必须依赖既有断线重连而不是换通道重发。
    /// </summary>
    Dropped = 2,
}
