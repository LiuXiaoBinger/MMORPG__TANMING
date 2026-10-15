/// <summary>移动请求和移动通知的网关透传入口，只保留现有会话路由。</summary>
public sealed class GateMoveCtrl : IContainer
{
    /// <summary>移动网关入口没有预加载资源。</summary>
    public void OnInit() { }

    /// <summary>把 Unity 的移动上报原样发送给游戏服。</summary>
    /// <param name="serverBase">接收 Unity 请求的网关会话，持有游戏服连接。</param>
    /// <param name="basePackage">含原 Unity 会话的 11180 请求。</param>
    public void OnServerCommand(ServerBase serverBase, BasePackage basePackage)
    {
        serverBase._client.SendData(basePackage);
    }

    /// <summary>把游戏服的附近移动通知原样发送给目标 Unity 会话。</summary>
    /// <param name="serverBase">接收游戏服通知的网关连接。</param>
    /// <param name="basePackage">含目标 Unity 会话的 11181 通知。</param>
    public void OnClientCommand(ServerBase serverBase, BasePackage basePackage)
    {
        // 复用网关现有协议校验和目标会话转发，不在网关修改角色位置。
        Session session = SessionMgr.Instance.GetSession(basePackage.UnitySessionId);
        GateClientResponseForwarder.ForwardToUnity(session, basePackage);
    }
}
