using System;

/// <summary>
/// 登录服控制器。上行：接收 Unity 的注册/登录/选服/验证码请求，做用户名密码邮箱合法性与登录频率
/// 限制后转发中心服；下行：接收中心服处理结果，按 Unity 会话回传错误码或结果数据。
/// </summary>
public class LoginCtrl:IContainer
{
    /// <summary>IContainer 初始化回调，登录控制器无预加载资源，留空实现。</summary>
    public void OnInit()
    {
    }

    #region 事件回调
    /// <summary>
    /// 登录服务器作为客户端，收到中心服务器发来的处理结果数据，按协议号分发回传 Unity。
    /// </summary>
    /// <param name="serverBase">中心服方向的连接对象。</param>
    /// <param name="basePackage">中心服回包协议。</param>
    public void OnClientCommand(BaseLink serverBase, BasePackage basePackage)
    {
        // 根据外层包保留的客户端编号找到原连接。
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

    /// <summary>读取中心服选服结果；成功返回角色列表，失败返回统一错误码。</summary>
    /// <param name="seesion">接收结果的客户端会话。</param>
    /// <param name="basePackage">中心服原始响应。</param>
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

    /// <summary>读取服务器列表结果并送回客户端。</summary>
    /// <param name="seesion">接收列表的客户端会话。</param>
    /// <param name="basePackage">中心服列表响应。</param>
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

    /// <summary>读取账号登录结果并送回客户端，失败时发送统一错误码。</summary>
    /// <param name="seesion">发起登录的客户端会话。</param>
    /// <param name="basePackage">中心服账号登录结果。</param>
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
    /// 把中心服注册结果送回原客户端；失败时改发统一错误码。
    /// </summary>
    /// <param name="seesion">注册请求对应的客户端连接。</param>
    /// <param name="basePackage">中心服返回的注册结果包。</param>
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

    /// <summary>向独立验证码服务发起邮箱申请；当前调用没有等待 RPC 结果或回送验证码响应。</summary>
    /// <param name="serverBase">接收申请的登录服连接，当前方法未使用。</param>
    /// <param name="basePackage">包含注册邮箱的请求。</param>
    private void OnRegistVarifyHandle(BaseLink serverBase, BasePackage basePackage)
    {
        RegistVarifyReq req = RegistVarifyReq.Parser.ParseFrom(basePackage.Data);
        //basePackage.UnitySessionId = (serverBase as Session).SessionID;
        //serverBase._client.Send(basePackage);
        //发送邮箱账号给Varify服务器

        GRPCMgr.Instance.GetVarifyCodeAsync(req.Email);
        LogMsg.Info("OnRegistVarifyHandle::" + req.ToString());
    }

    /// <summary>把选服请求原样交给中心服读取角色列表。</summary>
    /// <param name="serverBase">持有中心服连接的客户端会话。</param>
    /// <param name="basePackage">包含账号和游戏服编号的请求。</param>
    private void OnLoginGameServerHandle(BaseLink serverBase, BasePackage basePackage)
    {
        LoginGameServerReq req = LoginGameServerReq.Parser.ParseFrom(basePackage.Data);
        //basePackage.UnitySessionId = (serverBase as Session).SessionID;
        serverBase._client.Send(basePackage);
        LogMsg.Info("OnLoginGameServerHandle::" + req.ToString());
    }

    /// <summary>把服务器列表查询原样交给中心服。</summary>
    /// <param name="serverBase">持有中心服连接的客户端会话。</param>
    /// <param name="basePackage">服务器列表查询请求。</param>
    private void OnGetServerListHandle(BaseLink serverBase, BasePackage basePackage)
    {
       
        GetServerListReq req = GetServerListReq.Parser.ParseFrom(basePackage.Data);
        //basePackage.UnitySessionId = (serverBase as Session).SessionID;
        serverBase._client.Send(basePackage);
        LogMsg.Info("OnGetServerListHandle::" + req.ToString());
    }

    /// <summary>
    /// 限制同一用户名在 300 毫秒内重复登录，通过后把请求交给中心服验证账号密码。
    /// </summary>
    /// <param name="serverBase">接收客户端请求的登录服连接，持有到中心服的连接。</param>
    /// <param name="basePackage">携带原会话编号的请求包。</param>
    private void OnLoginHandle(BaseLink serverBase, BasePackage basePackage)
    {
        LoginReq req = LoginReq.Parser.ParseFrom(basePackage.Data);
        //basePackage.UnitySessionId = (serverBase as Session).SessionID;
        // 同一用户名上一次请求登录的 UTC 毫秒时间戳，用于短时间频率限制。
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
    /// 检查用户名、邮箱格式及 4 到 16 字符的密码长度，然后转发注册请求。
    /// </summary>
    /// <param name="serverBase">接收客户端请求的登录服连接，持有到中心服的连接。</param>
    /// <param name="basePackage">携带原会话编号的请求包。</param>
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
        // 此处直接转发密码字段，没有执行 MD5 或其他密码转换。
        //basePackage.UnitySessionId = (serverBase as Session).SessionID;
        serverBase._client.Send(basePackage);
        LogMsg.Info("OnRegistHandle::" + req.ToString());
    }
    #endregion
        
}
