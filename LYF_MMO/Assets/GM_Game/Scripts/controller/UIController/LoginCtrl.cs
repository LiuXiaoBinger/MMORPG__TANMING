using System.Collections.Generic;
using Google.Protobuf;
using UnityEngine;

/// <summary>
/// 组织账号登录、邮箱注册和区服选择：校验输入、收发协议，并在游戏服登录成功后进入选角场景。
/// </summary>
public class LoginCtrl : CtrlBase
{

    #region 字段
    /// <summary>PlayerPrefs 中记录是否勾选用户协议的键，值 1 表示已同意。</summary>
    private const string AgreementKey = "Agreement";

    /// <summary>PlayerPrefs 中保存记住账号的键；此控制器不保存密码。</summary>
    private const string AccountKey = "Account";

    /// <summary>登录模块视图。</summary>
    private readonly Loginview _loginview;

    /// <summary>当前准备连接的游戏服务器编号。</summary>
    private int _serverId;

    /// <summary>服务器协议对象映射，仅由控制器保存连接所需字段。</summary>
    private readonly Dictionary<int, GameServer> _gameServers = new Dictionary<int, GameServer>();
    #endregion

    #region 构造、初始化与事件注册
    /// <summary>创建登录控制器并恢复本地登录状态。</summary>
    /// <param name="view">登录模块的 Loginview，保存各登录窗口及其交互回调。</param>
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
        // 恢复上次勾选协议的状态，缺少记录时为 false。
        bool agreementAccepted = PlayerPrefs.GetInt(AgreementKey) == 1;
        // 恢复记住的账号，未保存时为空字符串。
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

    /// <summary>注销登录协议和视图事件。</summary>
    public override void Dispose()
    {
        SocketDispatcher.Instance.RemoveEventHandler(
            NetDefine.CMD_RegistCode, OnRegistHandle);
        SocketDispatcher.Instance.RemoveEventHandler(
            NetDefine.CMD_LoginCode, OnLoginHandle);
        SocketDispatcher.Instance.RemoveEventHandler(
            NetDefine.CMD_GetServerListCode, OnGetServerListHandle);
        SocketDispatcher.Instance.RemoveEventHandler(
            NetDefine.CMD_LoginGameServerCode, OnLoginGameServerHandle);
        if (_loginview != null)
        {
            _loginview.ClearActions();
        }
        _gameServers.Clear();
    }
    #endregion

    #region 交互响应
    /// <summary>连接用户选中的游戏服务器。</summary>
    /// <param name="serverId">玩家在当前服务器窗口确认的服务器编号。</param>
    private void OnGameServerBtnClicked(int serverId)
    {
        // 从服务端列表缓存中取得连接 IP 和端口，窗口本身不保存连接对象。
        GameServer server;
        if (!_gameServers.TryGetValue(serverId, out server) || server == null)
        {
            TipsMgr.Instance.ShowSystemTips("服务器信息已失效，请重新选择");
            return;
        }
        _serverId = server.ServerId;
        MNetClient.Instance.ConnectServer(server.IpHost, server.Prot, OnConnSucced, OnConnFilded);
    }

    /// <summary>校验邮箱并发送验证码请求。</summary>
    /// <param name="email">接收验证码的邮箱；这里仅检查非空，不检查邮箱格式。</param>
    private void OnVerifyCodeBtnClicked(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            TipsMgr.Instance.ShowSystemTips("邮箱不能为空");
            return;
        }
        if (MNetClient.Client == null)
        {
            TipsMgr.Instance.ShowSystemTips("登录服务器尚未连接");
            return;
        }

        // 验证码请求只携带邮箱，验证码生成与发送由登录服处理。
        RegistVarifyReq request = new RegistVarifyReq
        {
            Email = email
        };
        MNetClient.Instance.Send(NetDefine.CMD_RegistVarifyCode, request.ToByteString());
    }
    #endregion

    #region 事件回调
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
    /// <param name="serverId">列表中选定的服务器编号；这里只切回确认窗口，不立即连接。</param>
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
    /// <param name="account">玩家输入的账号名。</param>
    /// <param name="password">本次登录密码，只随请求发送。</param>
    /// <param name="rememberAccount">是否把账号写入 PlayerPrefs。</param>
    /// <param name="agreementAccepted">是否勾选协议，未同意时不发送登录。</param>
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
        if (MNetClient.Client == null)
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

        // 登录服需要的账号和密码，不包含本地记住账号选项。
        LoginReq request = new LoginReq
        {
            UserName = account,
            Password = password
        };
        MNetClient.Instance.Send(NetDefine.CMD_LoginCode, request.ToByteString());
    }

    /// <summary>校验并发送注册请求。</summary>
    /// <param name="account">注册账号。</param>
    /// <param name="email">接收过验证码的邮箱。</param>
    /// <param name="verifyCode">用户填写的邮箱验证码。</param>
    /// <param name="password">注册密码。</param>
    /// <param name="confirmPassword">仅用于本地比对的确认密码，不随协议发送。</param>
    private void OnRegisterRequested(string account, string email, string verifyCode,
        string password, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(verifyCode))
        {
            TipsMgr.Instance.ShowSystemTips("请完整填写注册信息");
            return;
        }
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
        {
            TipsMgr.Instance.ShowSystemTips("请完整填写注册信息");
            return;
        }
        if (!string.Equals(password, confirmPassword))
        {
            TipsMgr.Instance.ShowSystemTips("两次密码不一致");
            return;
        }
        if (MNetClient.Client == null)
        {
            TipsMgr.Instance.ShowSystemTips("登录服务器尚未连接");
            return;
        }

        // 校验两次密码一致后，发送账号、邮箱、验证码和一份密码。
        RegistReq request = new RegistReq
        {
            UserName = account,
            Email = email,
            Varify = verifyCode,
            Password = password
        };
        MNetClient.Instance.Send(NetDefine.CMD_RegistCode, request.ToByteString());
    }

    /// <summary>请求最新服务器列表。</summary>
    private void OnServerListRequested()
    {
        if (MNetClient.Client == null)
        {
            TipsMgr.Instance.ShowSystemTips("登录服务器尚未连接");
            return;
        }

        // ServerId 为 0，向登录服请求服务器列表。
        GetServerListReq request = new GetServerListReq
        {
            ServerId = 0
        };
        MNetClient.Instance.Send(NetDefine.CMD_GetServerListCode, request.ToByteString());
    }

    /// <summary>提示游戏服务器连接失败。</summary>
    private void OnConnFilded()
    {
        TipsMgr.Instance.ShowSystemTips("登录服务器失败");
    }

    /// <summary>连接成功后发送游戏服登录请求。</summary>
    private void OnConnSucced()
    {
        if (MNetClient.Client == null || Global.Instance.LoginInfo == null)
        {
            TipsMgr.Instance.ShowSystemTips("登录状态无效");
            return;
        }

        // TCP 已连到选定游戏服后，用已登录账号 ID 与目标区服 ID 继续登录。
        LoginGameServerReq request = new LoginGameServerReq
        {
            AccountId = Global.Instance.LoginInfo.AccountId,
            GameServerId = _serverId
        };
        MNetClient.Instance.Send(NetDefine.CMD_LoginGameServerCode,
            request.ToByteString());
    }

    /// <summary>处理游戏服登录结果并进入角色场景。</summary>
    /// <param name="data">游戏服返回的登录结果及已有角色摘要。</param>
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
    /// <param name="data">登录服返回的区服列表。</param>
    private void OnGetServerListHandle(ByteString data)
    {
        GetServerListRet result = GetServerListRet.Parser.ParseFrom(data);
        if (result != null && result.CmdCode == CmdCode.Succeed)
        {
            _gameServers.Clear();
            // 窗口只需要名称、状态、新服标记等展示字段。
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
    /// <param name="data">账号登录结果，包含账号身份和默认服务器信息。</param>
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
    /// <param name="data">注册结果；验证码错误单独给出提示。</param>
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
    #endregion

    /// <summary>把服务器协议对象转换为不含网络类型的展示数据。</summary>
    /// <param name="gameServer">保存 IP、端口等完整连接信息的协议对象。</param>
    private GameServerViewData CreateServerViewData(GameServer gameServer)
    {
        // 协议使用整数 1 标记新服，窗口使用布尔值。
        bool isNew = false;
        if (gameServer.IsNew == 1)
        {
            isNew = true;
        }
        return new GameServerViewData(gameServer.ServerId, gameServer.ServerName,
            gameServer.RunState, isNew);
    }
}
