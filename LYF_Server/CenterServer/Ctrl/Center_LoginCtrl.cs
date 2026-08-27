using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;


/// <summary>
/// 中心服务器处理登录模块的相关逻辑
/// </summary>
public class Center_LoginCtrl : IContainer
{
    // 登录与角色业务的数据访问层。
    private LoginModle _loginModle = null;

    /// <summary>
    /// 创建登录控制器。
    /// </summary>
    /// <param name="loginModle">登录和角色相关业务模块。</param>
    public Center_LoginCtrl(LoginModle  loginModle)
    {
        _loginModle = loginModle;
    }

    /// <summary>
    /// 处理客户端直接发送到中心服务器的命令。
    /// 当前登录流程中的请求由服务器间命令处理。
    /// </summary>
    public void OnClientCommand(ServerBase serverBase, BasePackage basePackage)
    {
    }

    /// <summary>
    /// 控制器初始化入口。
    /// </summary>
    public void OnInit()
    {
    }

    /// <summary>
    /// 根据协议号分发 LoginServer 转发的请求。
    /// </summary>
    /// <param name="serverBase">当前网络服务器。</param>
    /// <param name="basePackage">包含协议号和 protobuf 数据的网络包。</param>
    public void OnServerCommand(ServerBase serverBase, BasePackage basePackage)
    {
        switch (basePackage.ProtoCode)
        {
            // 注册账号。
            case NetDefine.CMD_RegistCode:
                OnRegistHandle(serverBase, basePackage);
                break;
            // 账号密码登录。
            case NetDefine.CMD_LoginCode:
                OnLoginHandle(serverBase, basePackage);
                break;
            // 获取游戏服务器列表。
            case NetDefine.CMD_GetServerListCode:
                OnGetServerListHandle(serverBase, basePackage);
                break;
            // 选择服务器并查询已有角色。
            case NetDefine.CMD_LoginGameServerCode:
                OnLoginGameServerHandle(serverBase, basePackage);
                break;
            // 创建新角色。
            case NetDefine.CMD_CreateRoleCode:
                OnCreateRoleHandle(serverBase, basePackage);
                break;
            // 进入游戏，加载角色完整信息。
            case NetDefine.CMD_StartGameCode:
                OnStartGameCodeHandle(serverBase, basePackage);
                break;
            default:
                break;
        }


    }

    /// <summary>
    /// 处理进入游戏请求，返回主角色的完整属性信息。
    /// </summary>
    private void OnStartGameCodeHandle(ServerBase serverBase, BasePackage basePackage)
    {
        StartGameReq req = StartGameReq.Parser.ParseFrom(basePackage.Data);
        
        LogMsg.Info("OnStartGameCodeHandle=>req::" + req.ToString());
        StartGameRet ret=_loginModle.StartGame(req);
        LogMsg.Info("OnStartGameCodeHandle=>ret::" + ret.ToString());
        serverBase.SendData(basePackage,basePackage.ProtoCode,ret.ToByteString());
    }

    /// <summary>
    /// 处理创建角色请求。
    /// </summary>
    private void OnCreateRoleHandle(ServerBase serverBase, BasePackage basePackage)
    {
        CreateRoleReq req = CreateRoleReq.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("OnCreateRoleHandle=>req::" + req.ToString());
        CreateRoleRet ret=_loginModle.CreateRole(req);
        LogMsg.Info("OnCreateRoleHandle=>ret::" + ret.ToString());
        serverBase.SendData(basePackage,basePackage.ProtoCode,ret.ToByteString());
    }

    /// <summary>
    /// 处理选择游戏服务器请求，并返回该账号已创建的角色信息。
    /// </summary>
    private void OnLoginGameServerHandle(ServerBase serverBase, BasePackage basePackage)
    {
        LoginGameServerReq req = LoginGameServerReq.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("OnLoginGameServerHandle=>req::" + req.ToString());
        LoginGameServerRet ret=_loginModle.LoginGameServer(req);
        LogMsg.Info("OnLoginGameServerHandle=>ret::" + ret.ToString());
        serverBase.SendData(basePackage,basePackage.ProtoCode,ret.ToByteString());
    }

    /// <summary>
    /// 处理获取游戏服务器列表请求。
    /// </summary>
    private void OnGetServerListHandle(ServerBase serverBase, BasePackage basePackage)
    {
        GetServerListReq req = GetServerListReq.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("OnGetServerListHandle=>req::" + req.ToString());
        GetServerListRet ret=_loginModle.GetServerList(req);
        LogMsg.Info("OnGetServerListHandle=>ret::" + ret.ToString());
        serverBase.SendData(basePackage,basePackage.ProtoCode,ret.ToByteString());
    }

    /// <summary>
    /// 处理账号密码登录请求。
    /// </summary>
    private void OnLoginHandle(ServerBase serverBase, BasePackage basePackage)
    {
        LoginReq req = LoginReq.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("OnLoginHandle=>req::" + req.ToString());
        LoginRet ret=_loginModle.Login(req);
        LogMsg.Info("OnLoginHandle=>ret::" + ret.ToString());
        serverBase.SendData(basePackage,basePackage.ProtoCode,ret.ToByteString());
    }

    /// <summary>
    /// 处理注册事件
    /// </summary>
    /// <param name="serverBase"></param>
    /// <param name="basePackage"></param>
    private void OnRegistHandle(ServerBase serverBase, BasePackage basePackage)
    {

        RegistReq req = RegistReq.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("OnRegistHandle=>req::" + req.ToString());
        RegistRet ret=_loginModle.RegistAccont(req);
        LogMsg.Info("OnRegistHandle=>ret::" + ret.ToString());
        serverBase.SendData(basePackage,basePackage.ProtoCode,ret.ToByteString());
    }
    
    
}
