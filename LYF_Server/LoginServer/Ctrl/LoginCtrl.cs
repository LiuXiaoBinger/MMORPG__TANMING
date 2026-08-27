using System;

public class LoginCtrl:IContainer
{
    /// <summary>
    /// 登录服务器作为客户端，收到中心服务器发来的处理结果数据
    /// </summary>
    /// <param name="serverBase"></param>
    /// <param name="basePackage"></param>
    public void OnClientCommand(ServerBase serverBase, BasePackage basePackage)
    {
        Session seesion = SessionMgr.Instance.GetSession(basePackage.UnitySessionId);
        switch (basePackage.ProtoCode)
        {
            case NetDefine.CMD_RegistCode:
                OnRegistResultHandle(seesion, basePackage);
                break;
            case NetDefine.CMD_LoginCode:
                OnLoginResultHandle(seesion, basePackage);
                break;
            case NetDefine.CMD_GetServerListCode:
                OnGetServerListResultHandle(seesion, basePackage);
                break;
            case NetDefine.CMD_LoginGameServerCode:
                OnLoginGameServerResultHandle(seesion, basePackage);
                break;
            
        }
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

    private void OnGetServerListResultHandle(Session seesion, BasePackage basePackage)
    {
        GetServerListRet ret = GetServerListRet.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("OnLGetServerListResultHandle::" + ret.ToString());
        if (ret.CmdCode != CmdCode.Succeed)
        {
            seesion.SendError(basePackage,ret.CmdCode);
            return;
        }
        //把结果数据返回给unity
        seesion.SendData(basePackage);
    }

    private void OnLoginResultHandle(Session seesion, BasePackage basePackage)
    {
        LoginRet ret = LoginRet.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("OnLoginResultHandle::" + ret.ToString());
        if (ret.CmdCode != CmdCode.Succeed)
        {
            seesion.SendError(basePackage,ret.CmdCode);
            return;
        }
        //把结果数据返回给unity
        seesion.SendData(basePackage);
    }

    /// <summary>
    /// 注册结果处理
    /// </summary>
    /// <param name="serverBase"></param>
    /// <param name="basePackage"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void OnRegistResultHandle(ServerBase seesion, BasePackage basePackage)
    {
        RegistRet ret = RegistRet.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("OnRegistResultHandle::" + ret.ToString());
        if (ret.CmdCode != CmdCode.Succeed)
        {
            seesion.SendError(basePackage,ret.CmdCode);
            return;
        }
        //把结果数据返回给unity
        seesion.SendData(basePackage);
    }

    public void OnInit()
    {
    }

    public void OnServerCommand(ServerBase serverBase, BasePackage basePackage)
    {

        switch (basePackage.ProtoCode)
        {
            case NetDefine.CMD_RegistVarifyCode:
                OnRegistVarifyHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_RegistCode:
                OnRegistHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_LoginCode:
                OnLoginHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_GetServerListCode:
                OnGetServerListHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_LoginGameServerCode:
                OnLoginGameServerHandle(serverBase, basePackage);
                break;
            
            default:
                break;
        }


    }

    private void OnRegistVarifyHandle(ServerBase serverBase, BasePackage basePackage)
    {
        RegistVarifyReq req = RegistVarifyReq.Parser.ParseFrom(basePackage.Data);
        //basePackage.UnitySessionId = (serverBase as Session).SessionID;
        //serverBase._client.SendData(basePackage);
        //发送邮箱账号给Varify服务器

        GRPCMgr.Instance.GetVarifyCodeAsync(req.Email);
        LogMsg.Info("OnRegistVarifyHandle::" + req.ToString());
    }

    

    private void OnLoginGameServerHandle(ServerBase serverBase, BasePackage basePackage)
    {
        LoginGameServerReq req = LoginGameServerReq.Parser.ParseFrom(basePackage.Data);
        //basePackage.UnitySessionId = (serverBase as Session).SessionID;
        serverBase._client.SendData(basePackage);
        LogMsg.Info("OnLoginGameServerHandle::" + req.ToString());
    }

    private void OnGetServerListHandle(ServerBase serverBase, BasePackage basePackage)
    {
       
        GetServerListReq req = GetServerListReq.Parser.ParseFrom(basePackage.Data);
        //basePackage.UnitySessionId = (serverBase as Session).SessionID;
        serverBase._client.SendData(basePackage);
        LogMsg.Info("OnGetServerListHandle::" + req.ToString());
    }

    /// <summary>
    /// 登录请求
    /// </summary>
    /// <param name="serverBase"></param>
    /// <param name="basePackage"></param>
    private void OnLoginHandle(ServerBase serverBase, BasePackage basePackage)
    {
        LoginReq req = LoginReq.Parser.ParseFrom(basePackage.Data);
        //basePackage.UnitySessionId = (serverBase as Session).SessionID;
        long timer = DataUtils.Instance.GetLoginMilliseconds(req.UserName);
        if (timer > 0&&DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - timer <300)
        {
            serverBase.SendError(basePackage,CmdCode.UserOftenLogin);
            return;
        }
        
        DataUtils.Instance.AddLoginMilliseconds(req.UserName,DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        serverBase._client.SendData(basePackage);
        
        LogMsg.Info("OnLoginHandle::" + req.ToString());
    }

    /// <summary>
    /// 处理注册事件
    /// </summary>
    /// <param name="serverBase"></param>
    /// <param name="basePackage"></param>
    private void OnRegistHandle(ServerBase serverBase, BasePackage basePackage)
    {

        RegistReq req = RegistReq.Parser.ParseFrom(basePackage.Data);
        //验证
        if (!DataUtils.IsValidUserName(req.UserName))
        {
            serverBase.SendError(basePackage,CmdCode.UserNameIllegal);
            return;
        }

        if (!DataUtils.IsValidEmail(req.Email))
        {
            serverBase.SendError(basePackage,CmdCode.PhoneNumIllegal);
            return;
        }

        if (req.Password.Length < 4 || req.Password.Length > 16)
        {
            serverBase.SendError(basePackage,CmdCode.PasswordIllegal);
            return;
        }
        //MD5密码加密
        //basePackage.UnitySessionId = (serverBase as Session).SessionID;
        serverBase._client.SendData(basePackage);
        LogMsg.Info("OnRegistHandle::" + req.ToString());
    }
        
}
