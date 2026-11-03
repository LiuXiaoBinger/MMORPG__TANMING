/// <summary>
/// 网关登录控制器。负责把 Unity 的选服、建角、开始游戏请求透传给 GameServer，
/// 并把 GameServer 的登录类结果按 Unity 会话回传；网关本身不做账号校验。
/// </summary>
public class GateLoginCtrl:IContainer
{
    /// <summary>IContainer 初始化回调，网关登录控制器无预加载资源，留空实现。</summary>
    public void OnInit()
    {

    }
    /// <summary>
    /// 网关服务器作为服务器，接收 Unity 客户端的登录类请求并按协议号分发转发。
    /// </summary>
    /// <param name="serverBase">收到 Unity 请求的网关传输对象。</param>
    /// <param name="basePackage">携带原会话标识的请求协议包。</param>
    public void OnServerCommand(BaseLink serverBase, BasePackage basePackage)
    {
        switch (basePackage.ProtoCode)
        {
            
            case NetDefine.CMD_LoginGameServerCode:
                OnLoginGameServerHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_CreateRoleCode:
                OnCreateRoleHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_StartGameCode:
                OnStartGameCodeHandle(serverBase, basePackage);
                break;
            default:
                break;
        }
    }

    /// <summary>解析开始游戏请求用于日志，并把原协议包转给游戏服。</summary>
    /// <param name="serverBase">持有游戏服连接的客户端会话。</param>
    /// <param name="basePackage">包含所选角色编号的请求。</param>
    private void OnStartGameCodeHandle(BaseLink serverBase, BasePackage basePackage)
    {
        StartGameReq req = StartGameReq.Parser.ParseFrom(basePackage.Data);
        //发送数据到游戏服务器
        serverBase._client.Send(basePackage);
        LogMsg.Info("OnLoginGameServerHandle::" + req.ToString());
    }

    /// <summary>
    /// 把建角请求交给游戏服继续转发；网关目前没有实现昵称检查。
    /// </summary>
    /// <param name="serverBase">接收客户端请求并持有游戏服连接的会话。</param>
    /// <param name="basePackage">保留客户端会话编号的建角请求。</param>
    private void OnCreateRoleHandle(BaseLink serverBase, BasePackage basePackage)
    {
        CreateRoleReq req = CreateRoleReq.Parser.ParseFrom(basePackage.Data);
        //basePackage.UnitySessionId = (serverBase as Session).SessionID;
        //todo验证昵称合法
        
        //发送数据到游戏服务器
        serverBase._client.Send(basePackage);
        LogMsg.Info("OnLoginGameServerHandle::" + req.ToString());
    }
    
    /// <summary>
    /// 选服请求透传：解析请求后连同原会话一起转发给 GameServer，网关不做校验。
    /// </summary>
    private void OnLoginGameServerHandle(BaseLink serverBase, BasePackage basePackage)
    {
        LoginGameServerReq req = LoginGameServerReq.Parser.ParseFrom(basePackage.Data);
        //basePackage.UnitySessionId = (serverBase as Session).SessionID;
        serverBase._client.Send(basePackage);
        LogMsg.Info("OnLoginGameServerHandle::" + req.ToString());
    }

    /// <summary>
    /// GameServer 登录类结果回包入口：按 UnitySessionId 找回会话，经转发器校验错误码后回传 Unity。
    /// </summary>
    public void OnClientCommand(BaseLink serverBase, BasePackage basePackage)
    {
        // 游戏服返回的客户端编号对应网关保存的原始会话。
        Session seesion = SessionMgr.Instance.GetSession(basePackage.UnitySessionId);
        GateClientResponseForwarder.ForwardToUnity(seesion, basePackage);
    }
}
