using System;
using UnityEngine;

/// <summary>
/// 登录模块视图集合，向控制器提供各窗口的事件绑定入口。
/// </summary>
public class Loginview : UIBase
{

    #region 字段
    /// <summary>登录窗口。</summary>
    [SerializeField, Header("登录窗口")] private LoginWindow _loginWindow;

    /// <summary>注册窗口。</summary>
    [SerializeField, Header("注册窗口")] private RegistWindow _registWindow;

    /// <summary>当前服务器窗口。</summary>
    [SerializeField, Header("服务器窗口")] private GameServerWindow _gameServerWindow;

    /// <summary>服务器列表窗口。</summary>
    [SerializeField, Header("服务器列表窗口")] private ServerListWindow _serverListWindow;
    #endregion

    #region 事件注册与注销
    /// <summary>把登录和打开注册页的操作交给控制器。</summary>
    /// <param name="loginHandler">依次接收账号、密码、记住账号开关和协议勾选状态。</param>
    /// <param name="registerWindowHandler">切换到注册窗口的回调。</param>
    public void RegisterLoginActions(Action<string, string, bool, bool> loginHandler,
        Action registerWindowHandler)
    {
        _loginWindow.LoginRequested = loginHandler;
        _loginWindow.RegisterRequested = registerWindowHandler;
    }

    /// <summary>把注册表单和返回登录页操作交给控制器。</summary>
    /// <param name="registerHandler">依次接收账号、邮箱、验证码、密码和确认密码。</param>
    /// <param name="backHandler">返回登录窗口的回调。</param>
    public void RegisterRegistrationActions(Action<string, string, string, string, string> registerHandler,
        Action backHandler)
    {
        _registWindow.RegisterRequested = registerHandler;
        _registWindow.BackRequested = backHandler;
    }

    /// <summary>设置服务器列表的打开、关闭和确认选择回调。</summary>
    /// <param name="serverListHandler">打开完整服务器列表。</param>
    /// <param name="closeListHandler">关闭服务器列表。</param>
    /// <param name="confirmHandler">接收确认选择的服务器编号。</param>
    public void RegisterServerActions(Action serverListHandler, Action closeListHandler,
        Action<int> confirmHandler)
    {
        _gameServerWindow.ServerListRequested = serverListHandler;
        _serverListWindow.CloseRequested = closeListHandler;
        _serverListWindow.ConfirmRequested = confirmHandler;
    }
    #endregion

    #region 核心逻辑
    /// <summary>初始化登录模块的窗口映射。</summary>
    public override void InitView()
    {
        base.InitView();
        windowDic.Add(WindowType.LoginWindow, _loginWindow);
        windowDic.Add(WindowType.RegistWindow, _registWindow);
        windowDic.Add(WindowType.GameServerWindow, _gameServerWindow);
        windowDic.Add(WindowType.ServerListWindow, _serverListWindow);
    }

    /// <summary>设置当前服务器按钮回调 handler，参数为正数服务器编号。</summary>
    public void RegisGameServerBtnClicked(Action<int> handler)
    {
        _gameServerWindow.gameServerBtnClickedAction = handler;
    }

    /// <summary>设置验证码按钮回调 handler，参数为用户输入的邮箱地址。</summary>
    public void RegisVerifyCodeBtnClicked(Action<string> handler)
    {
        _registWindow.verifyCodeBtnClickedAction = handler;
    }

    /// <summary>显示 agreementAccepted 对应的协议勾选状态及 account 账号；账号非空时勾选记住账号。</summary>
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
    #endregion
}
