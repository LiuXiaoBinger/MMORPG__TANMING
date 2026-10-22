/// <summary>
/// 实时通道发送结果：把“不属于实时通道”与“实时通道已启用后失效”彻底分开。
/// 传输钩子返回本枚举而不是布尔值，避免通道失效时实时协议被静默改投 TCP 造成重复执行或乱序。
/// </summary>
public enum RealtimeSendResult : byte
{
    /// <summary>不属于实时集合，或通道尚未启用成功：按原规则继续走 TCP。</summary>
    KeepTcp = 0,

    /// <summary>已由实时通道接管并写入成功，不再走 TCP。</summary>
    Sent = 1,

    /// <summary>
    /// 实时通道曾启用成功但当前已失效：本条必须丢弃。
    /// 已发出的移动与技能无法确定对端是否执行过，改投 TCP 会造成重复执行，
    /// 恢复必须依赖既有断线重连而不是换通道重发。
    /// </summary>
    Dropped = 2,
}
