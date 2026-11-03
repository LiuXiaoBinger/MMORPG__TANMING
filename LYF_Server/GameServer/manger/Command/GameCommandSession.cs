/// <summary>
/// 命令在不同服务器链路中的会话标识。
/// </summary>
public sealed class GameCommandSession
{
    /// <summary>客户端在网关上的会话标识。</summary>
    public int UnitySessionId { get; private set; }
    /// <summary>网关与 GameServer 连接的会话标识。</summary>
    public int GateSessionId { get; private set; }

    /// <summary>保存从网关到客户端的两层会话编号，供游戏服把响应送回原连接。</summary>
    /// <param name="unitySessionId">客户端在网关上的会话编号。</param>
    /// <param name="gateSessionId">网关连接游戏服时的会话编号。</param>
    public GameCommandSession(int unitySessionId, int gateSessionId)
    {
        UnitySessionId = unitySessionId;
        GateSessionId = gateSessionId;
    }
}
