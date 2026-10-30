/// <summary>
/// 队伍与副本入场协议的网关透传入口。
/// 上行（Unity→GameServer）原样交给游戏服，下行（GameServer→Unity）按 UnitySessionId 找到目标会话再回发，
/// 与现有 GateMoveCtrl 保持同一套会话路由和错误码校验，网关不解释队伍业务。
/// </summary>
public sealed class GateTeamCtrl : IContainer
{
    /// <summary>队伍网关入口没有预加载资源。</summary>
    public void OnInit() { }

    /// <summary>
    /// 把 Unity 的队伍上行请求原样发送给游戏服。
    /// 复用接收会话持有的游戏服连接，Unity 会话身份随协议包原样保留。
    /// </summary>
    /// <param name="serverBase">接收 Unity 请求的网关会话，持有游戏服连接。</param>
    /// <param name="basePackage">队伍上行请求，含原 Unity 会话号。</param>
    public void OnServerCommand(BaseLink serverBase, BasePackage basePackage)
    {
        serverBase._client.Send(basePackage);
    }

    /// <summary>
    /// 把游戏服下发的队伍通知按 Unity 会话转发给对应客户端。
    /// 队伍状态、邀请、入场通知与角色离开视野都走这条路径。
    /// </summary>
    /// <param name="serverBase">接收游戏服通知的网关连接。</param>
    /// <param name="basePackage">队伍下行通知，含目标 Unity 会话号。</param>
    public void OnClientCommand(BaseLink serverBase, BasePackage basePackage)
    {
        // session 是按包内 UnitySessionId 反查出的目标玩家会话；为 null 时由转发器按错误码回绝，不在此处丢弃。
        Session session = SessionMgr.Instance.GetSession(basePackage.UnitySessionId);
        GateClientResponseForwarder.ForwardToUnity(session, basePackage);
    }
}
