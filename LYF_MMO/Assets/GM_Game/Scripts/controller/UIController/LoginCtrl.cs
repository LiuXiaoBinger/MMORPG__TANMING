using System.Collections.Generic;
using Google.Protobuf;
using UnityEngine;

/// <summary>
/// 登录模块控制器，负责校验、协议发送和窗口流程。
/// </summary>
public class LoginCtrl : CtrlBase
{
    /// <summary>本地保存的用户协议键。</summary>
    private const string AgreementKey = "Agreement";
    /// <summary>本地保存的账号键。</summary>
    private const string AccountKey = "Account";

    /// <summary>登录模块视图。</summary>
    private readonly Loginview _loginview;

    /// <summary>当前准备连接的游戏服务器编号。</summary>
    private int _serverId;

    /// <summary>服务器协议对象映射，仅由控制器保存连接所需字段。</summary>
    private readonly Dictionary<int, GameServer> _gameServers = new Dictionary<int, GameServer>();

    /// <summary>创建登录控制器并恢复本地登录状态。</summary>
    public LoginCtrl(UIBase view) : base(view)
    {
        _loginview = view as Loginview;
        if (_loginview == null)
        {
            return;
        }

        _loginview.InitView();
        RegisterCommands();
        RegisterViewActions();
        bool agreementAccepted = PlayerPrefs.GetInt(AgreementKey) == 1;
        string account = PlayerPrefs.GetString(AccountKey);
        _loginview.SetSavedLoginState(agreementAccepted, account);
    }

    /// <summary>注册登录协议回包。</summary>
    private void RegisterCommands()
    {
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_RegistCode, OnRegistHandle);
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_LoginCode, OnLoginHandle);
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_GetServerListCode, OnGetServerListHandle);
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_LoginGameServerCode, OnLoginGameServerHandle);
    }

    /// <summary>注册登录模块的视图事件。</summary>
    private void RegisterViewActions()
    {
        _loginview.RegisterLoginActions(OnLoginRequested, OnRegisterWindowRequested);
        _loginview.RegisterRegistrationActions(OnRegisterRequested, OnLoginWindowRequested);
        _loginview.RegisVerifyCodeBtnClicked(OnVerifyCodeBtnClicked);
        _loginview.RegisGameServerBtnClicked(OnGameServerBtnClicked);
        _loginview.RegisterServerActions(OnServerListRequested, OnGameServerWindowRequested,
            OnServerConfirmed);
    }

    /// <summary>显示注册窗口。</summary>
    private void OnRegisterWindowRequested()
    {
        ShowWindow(WindowType.RegistWindow);
    }

    /// <summary>显示登录窗口。</summary>
    private void OnLoginWindowRequested()
    {
        ShowWindow(WindowType.LoginWindow);
    }

    /// <summary>关闭服务器列表并返回当前服务器窗口。</summary>
    private void OnGameServerWindowRequested()
    {
        ShowWindow(WindowType.GameServerWindow);
    }

    /// <summary>确认服务器列表中的选择。</summary>
    private void OnServerConfirmed(int serverId)
    {
        GameServer gameServer;
        if (!_gameServers.TryGetValue(serverId, out gameServer) || gameServer == null)
        {
            TipsMgr.Instance.ShowSystemTips("请选择服务器");
            return;
        }
        ShowWindow(WindowType.GameServerWindow, CreateServerViewData(gameServer));
    }

    /// <summary>校验并发送登录请求，同时保存用户选择的本地状态。</summary>
    private void OnLoginRequested(string account, string password, bool rememberAccount,
        bool agreementAccepted)
    {
        if (string.IsNullOrWhiteSpace(account))
        {
            TipsMgr.Instance.ShowSystemTips("账号不能为空");
            return;
        }
        if (string.IsNullOrEmpty(password))
        {
            TipsMgr.Instance.ShowSystemTips("密码不能为空");
            return;
        }
        if (!agreementAccepted)
        {
            TipsMgr.Instance.ShowSystemTips("请阅读并勾选用户协议");
            return;
        }
        if (NetSocketMgr.Client == null)
        {
            TipsMgr.Instance.ShowSystemTips("登录服务器尚未连接");
            return;
        }

        PlayerPrefs.SetInt(AgreementKey, 1);
        if (rememberAccount)
        {
            PlayerPrefs.SetString(AccountKey, account);
        }
        else
        {
            PlayerPrefs.SetString(AccountKey, string.Empty);
        }
        PlayerPrefs.Save();

        LoginReq request = new LoginReq
        {
            UserName = account,
            Password = password
        };
        NetSocketMgr.Client.SendData(NetDefine.CMD_LoginCode, request.ToByteString());
    }

    /// <summary>校验并发送注册请求。</summary>
    private void OnRegisterRequested(string account, string email, string verifyCode,
        string password, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(verifyCode) || string.IsNullOrEmpty(password) ||
            string.IsNullOrEmpty(confirmPassword))
        {
            TipsMgr.Instance.ShowSystemTips("请完整填写注册信息");
            return;
        }
        if (!string.Equals(password, confirmPassword))
        {
            TipsMgr.Instance.ShowSystemTips("两次密码不一致");
            return;
        }
        if (NetSocketMgr.Client == null)
        {
            TipsMgr.Instance.ShowSystemTips("登录服务器尚未连接");
            return;
        }

        RegistReq request = new RegistReq
        {
            UserName = account,
            Email = email,
            Varify = verifyCode,
            Password = password
        };
        NetSocketMgr.Client.SendData(NetDefine.CMD_RegistCode, request.ToByteString());
    }

    /// <summary>请求最新服务器列表。</summary>
    private void OnServerListRequested()
    {
        if (NetSocketMgr.Client == null)
        {
            TipsMgr.Instance.ShowSystemTips("登录服务器尚未连接");
            return;
        }

        GetServerListReq request = new GetServerListReq
        {
            ServerId = 0
        };
        NetSocketMgr.Client.SendData(NetDefine.CMD_GetServerListCode, request.ToByteString());
    }

    /// <summary>连接用户选中的游戏服务器。</summary>
    private void OnGameServerBtnClicked(int serverId)
    {
        GameServer server;
        if (!_gameServers.TryGetValue(serverId, out server) || server == null)
        {
            TipsMgr.Instance.ShowSystemTips("服务器信息已失效，请重新选择");
            return;
        }
        _serverId = server.ServerId;
        NetSocketMgr.Instance.ConnectServer(server.IpHost, server.Prot, OnConnSucced, OnConnFilded);
    }

    /// <summary>提示游戏服务器连接失败。</summary>
    private void OnConnFilded()
    {
        TipsMgr.Instance.ShowSystemTips("登录服务器失败");
    }

    /// <summary>连接成功后发送游戏服登录请求。</summary>
    private void OnConnSucced()
    {
        if (NetSocketMgr.Client == null || Global.Instance.LoginInfo == null)
        {
            TipsMgr.Instance.ShowSystemTips("登录状态无效");
            return;
        }

        LoginGameServerReq request = new LoginGameServerReq
        {
            AccountId = Global.Instance.LoginInfo.AccountId,
            GameServerId = _serverId
        };
        NetSocketMgr.Client.SendData(NetDefine.CMD_LoginGameServerCode,
            request.ToByteString());
    }

    /// <summary>校验邮箱并发送验证码请求。</summary>
    private void OnVerifyCodeBtnClicked(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            TipsMgr.Instance.ShowSystemTips("邮箱不能为空");
            return;
        }
        if (NetSocketMgr.Client == null)
        {
            TipsMgr.Instance.ShowSystemTips("登录服务器尚未连接");
            return;
        }

        RegistVarifyReq request = new RegistVarifyReq
        {
            Email = email
        };
        NetSocketMgr.Client.SendData(NetDefine.CMD_RegistVarifyCode, request.ToByteString());
    }

    /// <summary>处理游戏服登录结果并进入角色场景。</summary>
    private void OnLoginGameServerHandle(ByteString data)
    {
        LoginGameServerRet result = LoginGameServerRet.Parser.ParseFrom(data);
        if (result == null || result.CmdCode != CmdCode.Succeed)
        {
            TipsMgr.Instance.ShowSystemTips("登录游戏服务器失败");
            return;
        }

        SceneMgr.Instance.LoadSceneMode(SceneType.Scene_CreateRole, () =>
        {
            ShowView(false);
            if (result.CreateRoleInfo != null)
            {
                UIRoot.Instance.CreateRoleCtrl.ShowWindow(WindowType.SelectRoleWindow,
                    result.CreateRoleInfo);
            }
            else
            {
                UIRoot.Instance.CreateRoleCtrl.ShowWindow(WindowType.CreateRoleWindow);
            }
        });
    }

    /// <summary>处理服务器列表回包。</summary>
    private void OnGetServerListHandle(ByteString data)
    {
        GetServerListRet result = GetServerListRet.Parser.ParseFrom(data);
        if (result != null && result.CmdCode == CmdCode.Succeed)
        {
            _gameServers.Clear();
            List<GameServerViewData> serverList = new List<GameServerViewData>();
            for (int index = 0; index < result.GameServers.Count; index++)
            {
                GameServer gameServer = result.GameServers[index];
                if (gameServer == null || gameServer.ServerId <= 0)
                {
                    continue;
                }
                _gameServers[gameServer.ServerId] = gameServer;
                serverList.Add(CreateServerViewData(gameServer));
            }
            ShowWindow(WindowType.ServerListWindow, serverList);
            return;
        }
        TipsMgr.Instance.ShowSystemTips("获取服务器列表失败");
    }

    /// <summary>处理账号登录回包。</summary>
    private void OnLoginHandle(ByteString data)
    {
        LoginRet result = LoginRet.Parser.ParseFrom(data);
        if (result != null && result.CmdCode == CmdCode.Succeed)
        {
            Global.Instance.LoginInfo = result;
            TipsMgr.Instance.ShowSystemTips("登录成功");
            GameServer gameServer = result.GameServer;
            if (gameServer != null && gameServer.ServerId > 0)
            {
                _gameServers[gameServer.ServerId] = gameServer;
                ShowWindow(WindowType.GameServerWindow, CreateServerViewData(gameServer));
            }
            return;
        }
        TipsMgr.Instance.ShowSystemTips("登录失败");
    }

    /// <summary>处理账号注册回包。</summary>
    private void OnRegistHandle(ByteString data)
    {
        RegistRet result = RegistRet.Parser.ParseFrom(data);
        if (result != null && result.CmdCode == CmdCode.Succeed)
        {
            TipsMgr.Instance.ShowSystemTips("注册成功，请登录");
            ShowWindow(WindowType.LoginWindow);
            return;
        }
        if (result != null && result.CmdCode == CmdCode.VarifyError)
        {
            TipsMgr.Instance.ShowSystemTips("验证码错误");
            return;
        }
        TipsMgr.Instance.ShowSystemTips("注册失败");
    }

    /// <summary>把服务器协议对象转换为不含网络类型的展示数据。</summary>
    private GameServerViewData CreateServerViewData(GameServer gameServer)
    {
        bool isNew = false;
        if (gameServer.IsNew == 1)
        {
            isNew = true;
        }
        return new GameServerViewData(gameServer.ServerId, gameServer.ServerName,
            gameServer.RunState, isNew);
    }

    /// <summary>注销登录协议和视图事件。</summary>
    public override void Dispose()
    {
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.CMD_RegistCode);
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.CMD_LoginCode);
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.CMD_GetServerListCode);
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.CMD_LoginGameServerCode);
        if (_loginview != null)
        {
            _loginview.ClearActions();
        }
        _gameServers.Clear();
    }
}
