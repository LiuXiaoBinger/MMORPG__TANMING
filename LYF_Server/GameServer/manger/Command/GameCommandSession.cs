/// <summary>
/// 命令在不同服务器链路中的会话标识。
/// </summary>
public sealed class GameCommandSession
{
    /// <summary>客户端在网关上的会话标识。</summary>
    public int UnitySessionId { get; private set; }
    /// <summary>网关与 GameServer 连接的会话标识。</summary>
    public int GateSessionId { get; private set; }

    public GameCommandSession(int unitySessionId, int gateSessionId)
    {
        UnitySessionId = unitySessionId;
        GateSessionId = gateSessionId;
    }
}
