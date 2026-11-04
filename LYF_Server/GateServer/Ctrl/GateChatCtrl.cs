/// <summary>网关聊天通道，按现有网络框架要求实现 IContainer。</summary>
public sealed class GateChatCtrl : IContainer
{
    /// <summary>聊天命令由网关启动入口统一注册。</summary>
    public void OnInit() { }
    /// <summary>把玩家发言原样送往持有登录角色的 GS。</summary>
    /// <param name="link">已绑定玩家会话号的入口。</param><param name="packet">原始 SendChatMsg 请求。</param>
    public void OnServerCommand(BaseLink link, BasePackage packet) { link._client.Send(packet); }
    /// <summary>把经过现有白名单解析的回执或聊天消息下发给目标玩家。</summary>
    /// <param name="link">来自 GS 的连接。</param><param name="packet">已指定目标会话的包。</param>
    public void OnClientCommand(BaseLink link, BasePackage packet)
    {
        GateClientResponseForwarder.ForwardToUnity(SessionMgr.Instance.GetSession(packet.UnitySessionId), packet);
    }
}
