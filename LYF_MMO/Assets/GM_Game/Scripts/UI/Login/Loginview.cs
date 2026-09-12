using System;
using UnityEngine;

/// <summary>
/// 登录模块视图集合，向控制器提供各窗口的事件绑定入口。
/// </summary>
public class Loginview : UIBase
{
    /// <summary>登录窗口。</summary>
    [SerializeField, Header("登录窗口")] private LoginWindow _loginWindow;
    /// <summary>注册窗口。</summary>
    [SerializeField, Header("注册窗口")] private RegistWindow _registWindow;
    /// <summary>当前服务器窗口。</summary>
    [SerializeField, Header("服务器窗口")] private GameServerWindow _gameServerWindow;
    /// <summary>服务器列表窗口。</summary>
    [SerializeField, Header("服务器列表窗口")] private ServerListWindow _serverListWindow;

    /// <summary>初始化登录模块的窗口映射。</summary>
    public override void InitView()
    {
        base.InitView();
        windowDic.Add(WindowType.LoginWindow, _loginWindow);
        windowDic.Add(WindowType.RegistWindow, _registWindow);
        windowDic.Add(WindowType.GameServerWindow, _gameServerWindow);
        windowDic.Add(WindowType.ServerListWindow, _serverListWindow);
    }

    /// <summary>注册当前服务器点击事件。</summary>
    public void RegisGameServerBtnClicked(Action<int> handler)
    {
        _gameServerWindow.gameServerBtnClickedAction = handler;
    }

    /// <summary>注册验证码请求事件。</summary>
    public void RegisVerifyCodeBtnClicked(Action<string> handler)
    {
        _registWindow.verifyCodeBtnClickedAction = handler;
    }

    /// <summary>注册登录窗口的用户操作。</summary>
    public void RegisterLoginActions(Action<string, string, bool, bool> loginHandler,
        Action registerWindowHandler)
    {
        _loginWindow.LoginRequested = loginHandler;
        _loginWindow.RegisterRequested = registerWindowHandler;
    }

    /// <summary>注册注册窗口的用户操作。</summary>
    public void RegisterRegistrationActions(Action<string, string, string, string, string> registerHandler,
        Action backHandler)
    {
        _registWindow.RegisterRequested = registerHandler;
        _registWindow.BackRequested = backHandler;
    }

    /// <summary>注册服务器窗口的用户操作。</summary>
    public void RegisterServerActions(Action serverListHandler, Action closeListHandler,
        Action<int> confirmHandler)
    {
        _gameServerWindow.ServerListRequested = serverListHandler;
        _serverListWindow.CloseRequested = closeListHandler;
        _serverListWindow.ConfirmRequested = confirmHandler;
    }

    /// <summary>显示控制器读取到的本地登录状态。</summary>
    public void SetSavedLoginState(bool agreementAccepted, string account)
    {
        _loginWindow.SetSavedState(agreementAccepted, account);
    }

    /// <summary>清理控制器注册的事件。</summary>
    public void ClearActions()
    {
        _loginWindow.LoginRequested = null;
        _loginWindow.RegisterRequested = null;
        _registWindow.RegisterRequested = null;
        _registWindow.verifyCodeBtnClickedAction = null;
        _registWindow.BackRequested = null;
        _gameServerWindow.gameServerBtnClickedAction = null;
        _gameServerWindow.ServerListRequested = null;
        _serverListWindow.CloseRequested = null;
        _serverListWindow.ConfirmRequested = null;
    }
}
