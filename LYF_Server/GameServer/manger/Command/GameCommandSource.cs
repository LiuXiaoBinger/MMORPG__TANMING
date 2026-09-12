/// <summary>GameServer 命令的来源服务器。</summary>
public enum GameCommandSource
{
    /// <summary>来自 GateServer 的客户端请求。</summary>
    GateServer = 1,
    /// <summary>来自 CenterServer 的跨服结果或通知。</summary>
    CenterServer = 2,
}
