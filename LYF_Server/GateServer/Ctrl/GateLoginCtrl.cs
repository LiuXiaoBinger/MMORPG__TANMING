public class GateLoginCtrl:IContainer
{
    public void OnInit()
    {
        
    }
    /// <summary>
    /// 网关服务器作为服务器，接收unnity客户端
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

    private void OnStartGameCodeHandle(ServerBase serverBase, BasePackage basePackage)
    {
        StartGameReq req = StartGameReq.Parser.ParseFrom(basePackage.Data);
        //发送数据到游戏服务器
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
        //basePackage.UnitySessionId = (serverBase as Session).SessionID;
        //todo验证昵称合法
        
        //发送数据到游戏服务器
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

    public void OnClientCommand(ServerBase serverBase, BasePackage basePackage)
    {
        Session seesion = SessionMgr.Instance.GetSession(basePackage.UnitySessionId);
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
        if (ret.CmdCode != CmdCode.Succeed)
        {
            seesion.SendError(basePackage,ret.CmdCode);
            return;
        }
        //把结果数据返回给gate
        seesion.SendData(basePackage);
    }

    private void OnLoginGameServerResultHandle(Session seesion, BasePackage basePackage)
    {
        LoginGameServerRet ret = LoginGameServerRet.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("OnLGetServerListResultHandle::" + ret.ToString());
        if (ret.CmdCode != CmdCode.Succeed)
        {
            seesion.SendError(basePackage,ret.CmdCode);
            return;
        }
        //把结果数据返回给unity
        seesion.SendData(basePackage);
    }
    
    //创建角色
    private void OnCreateRoleResultHandle(Session seesion, BasePackage basePackage)
    {
        CreateRoleRet ret = CreateRoleRet.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("OnCreateRoleResultHandle::" + ret.ToString());
        if (ret.CmdCode != CmdCode.Succeed)
        {
            seesion.SendError(basePackage,ret.CmdCode);
            return;
        }
        //把结果数据返回给unity
        seesion.SendData(basePackage);
    }
}