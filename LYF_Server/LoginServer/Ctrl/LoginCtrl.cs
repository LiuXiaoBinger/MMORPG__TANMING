using System;

/// <summary>
/// 登录服控制器。上行：接收 Unity 的注册/登录/选服/验证码请求，做用户名密码邮箱合法性与登录频率
/// 限制后转发中心服；下行：接收中心服处理结果，按 Unity 会话回传错误码或结果数据。
/// </summary>
public class LoginCtrl:IContainer
{
    /// <summary>
    /// 登录服务器作为客户端，收到中心服务器发来的处理结果数据，按协议号分发回传 Unity。
    /// </summary>
    /// <param name="serverBase">中心服方向的连接对象。</param>
    /// <param name="basePackage">中心服回包协议。</param>
    public void OnClientCommand(BaseLink serverBase, BasePackage basePackage)
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
        seesion.Send(basePackage);
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
        seesion.Send(basePackage);
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
        seesion.Send(basePackage);
    }

    /// <summary>
    /// 注册结果处理
    /// </summary>
    /// <param name="serverBase"></param>
    /// <param name="basePackage"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void OnRegistResultHandle(BaseLink seesion, BasePackage basePackage)
    {
        RegistRet ret = RegistRet.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("OnRegistResultHandle::" + ret.ToString());
        if (ret.CmdCode != CmdCode.Succeed)
        {
            seesion.SendError(basePackage,ret.CmdCode);
            return;
        }
        //把结果数据返回给unity
        seesion.Send(basePackage);
    }

    /// <summary>IContainer 初始化回调，登录控制器无预加载资源，留空实现。</summary>
    public void OnInit()
    {
    }

    /// <summary>
    /// 登录服务器作为服务器，接收 Unity 上行请求并按协议号分发到验证码、注册、登录、列表、选服处理。
    /// </summary>
    /// <param name="serverBase">收到 Unity 请求的登录服传输对象。</param>
    /// <param name="basePackage">上行请求协议包。</param>
    public void OnServerCommand(BaseLink serverBase, BasePackage basePackage)
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

    private void OnRegistVarifyHandle(BaseLink serverBase, BasePackage basePackage)
    {
        RegistVarifyReq req = RegistVarifyReq.Parser.ParseFrom(basePackage.Data);
        //basePackage.UnitySessionId = (serverBase as Session).SessionID;
        //serverBase._client.Send(basePackage);
        //发送邮箱账号给Varify服务器

        GRPCMgr.Instance.GetVarifyCodeAsync(req.Email);
        LogMsg.Info("OnRegistVarifyHandle::" + req.ToString());
    }

    

    private void OnLoginGameServerHandle(BaseLink serverBase, BasePackage basePackage)
    {
        LoginGameServerReq req = LoginGameServerReq.Parser.ParseFrom(basePackage.Data);
        //basePackage.UnitySessionId = (serverBase as Session).SessionID;
        serverBase._client.Send(basePackage);
        LogMsg.Info("OnLoginGameServerHandle::" + req.ToString());
    }

    private void OnGetServerListHandle(BaseLink serverBase, BasePackage basePackage)
    {
       
        GetServerListReq req = GetServerListReq.Parser.ParseFrom(basePackage.Data);
        //basePackage.UnitySessionId = (serverBase as Session).SessionID;
        serverBase._client.Send(basePackage);
        LogMsg.Info("OnGetServerListHandle::" + req.ToString());
    }

    /// <summary>
    /// 登录请求
    /// </summary>
    /// <param name="serverBase"></param>
    /// <param name="basePackage"></param>
    private void OnLoginHandle(BaseLink serverBase, BasePackage basePackage)
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
        serverBase._client.Send(basePackage);
        
        LogMsg.Info("OnLoginHandle::" + req.ToString());
    }

    /// <summary>
    /// 处理注册事件
    /// </summary>
    /// <param name="serverBase"></param>
    /// <param name="basePackage"></param>
    private void OnRegistHandle(BaseLink serverBase, BasePackage basePackage)
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
        serverBase._client.Send(basePackage);
        LogMsg.Info("OnRegistHandle::" + req.ToString());
    }
        
}
