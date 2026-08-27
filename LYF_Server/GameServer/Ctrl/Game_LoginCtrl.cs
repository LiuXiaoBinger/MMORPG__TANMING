



public class Game_LoginCtrl:IContainer
{
    public void OnInit()
    {
        
    }
    /// <summary>
    /// 游戏服务器作为客户端，收到中心服务器发来的处理结果数据
    /// </summary>
    /// <param name="serverBase"></param>
    /// <param name="basePackage"></param>
    public void OnServerCommand(ServerBase serverBase, BasePackage basePackage)
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
    /// <summary>
    /// 开始游戏请求
    /// </summary>
    /// <param name="serverBase"></param>
    /// <param name="basePackage"></param>
    private void OnStartGameCodeHandle(ServerBase serverBase, BasePackage basePackage)
    {
        StartGameReq req = StartGameReq.Parser.ParseFrom(basePackage.Data);
        //发送给中心服务器
        serverBase._client.SendData(basePackage);
        LogMsg.Info("OnLoginGameServerHandle::" + req.ToString());
    }

    /// <summary>
    /// 创建角色
    /// </summary>
    /// <param name="serverBase"></param>
    /// <param name="basePackage"></param>
    private void OnCreateRoleHandle(ServerBase serverBase, BasePackage basePackage)
    {
        CreateRoleReq req = CreateRoleReq.Parser.ParseFrom(basePackage.Data);
        //发送给中心服务器
        serverBase._client.SendData(basePackage);
        LogMsg.Info("OnLoginGameServerHandle::" + req.ToString());
    }
    private void OnLoginGameServerHandle(ServerBase serverBase, BasePackage basePackage)
    {
        LoginGameServerReq req = LoginGameServerReq.Parser.ParseFrom(basePackage.Data);
        //basePackage.UnitySessionId = (serverBase as Session).SessionID;
        serverBase._client.SendData(basePackage);
        LogMsg.Info("OnLoginGameServerHandle::" + req.ToString());
    }

    /// <summary>
    /// 游戏服务器作为客户端，收到中心服务器发来的处理结果数据
    /// </summary>
    /// <param name="serverBase"></param>
    /// <param name="basePackage"></param>
    public void OnClientCommand(ServerBase serverBase, BasePackage basePackage)
    {
        Session seesion = SessionMgr.Instance.GetSession(basePackage.GateSessionId);
        switch (basePackage.ProtoCode)
        {
            case NetDefine.CMD_LoginGameServerCode:
                OnLoginGameServerResultHandle(seesion, basePackage);
                break;
            case NetDefine.CMD_CreateRoleCode:
                OnCreateRoleResultHandle(seesion, basePackage);
                break;
            case NetDefine.CMD_StartGameCode:
                OnStartGameResultHandle(seesion, basePackage);
                break;
        }
    }

    private void OnStartGameResultHandle(Session seesion, BasePackage basePackage)
    {
        StartGameRet ret = StartGameRet.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("OnStartGameResultHandle::" + ret.ToString());
        if (ret != null && ret.CmdCode == CmdCode.Succeed)
        {
            OnlineRole onlineRole = new OnlineRole()
            {
                UnitySessionId = basePackage.UnitySessionId,
                GateSessionId = basePackage.GateSessionId,
                mainRoleInfo = ret.MainRoleInfo,
            };
            GameGlobal.Instance.AddOlineRole(ret.MainRoleInfo.BaseInfo.RoleId,onlineRole);
        }
        //把结果数据返回给gate
        seesion.SendData(basePackage);
        
    }

    //创建角色
    private void OnCreateRoleResultHandle(Session seesion, BasePackage basePackage)
    {
        CreateRoleRet ret = CreateRoleRet.Parser.ParseFrom(basePackage.Data);
        
        //把结果数据返回给gate
        seesion.SendData(basePackage);
        LogMsg.Info("OnCreateRoleResultHandle::" + ret.ToString());
    }
    private void OnLoginGameServerResultHandle(Session seesion, BasePackage basePackage)
    {
        LoginGameServerRet ret = LoginGameServerRet.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("OnLGetServerListResultHandle::" + ret.ToString());
        
        //把结果数据返回给unity
        seesion.SendData(basePackage);
    }
}