using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 登录窗口，只采集输入并把用户操作交给登录控制器。
/// </summary>
public class LoginWindow : WindowBase
{
    /// <summary>登录账号输入；也用于回填本地保存的账号。</summary>
    [SerializeField, Header("账号输入框")] private TMP_InputField _iptAcct;
    /// <summary>本次登录密码输入，交给控制器校验和发送。</summary>
    [SerializeField, Header("密码输入框")] private TMP_InputField _iptPasd;
    /// <summary>是否要求控制器保存账号；窗口不自行写入本地存储。</summary>
    [SerializeField, Header("记住账号Toggle")] private Toggle _togRememberAcct;
    /// <summary>用户是否同意协议的表单开关。</summary>
    [SerializeField, Header("用户协议勾选")] private Toggle _togAgreement;

    /// <summary>提交登录表单事件，参数依次为账号、密码、记住账号、协议勾选。</summary>
    public Action<string, string, bool, bool> LoginRequested;

    /// <summary>前往注册窗口事件。</summary>
    public Action RegisterRequested;

    /// <summary>
    /// 显示本地保存的协议状态和账号；是否勾选记住账号由账号非空决定。
    /// </summary>
    public void SetSavedState(bool agreementAccepted, string account)
    {
        if (_togAgreement != null)
        {
            _togAgreement.isOn = agreementAccepted;
        }

        if (_iptAcct != null)
        {
            _iptAcct.text = account;
        }

        if (_togRememberAcct != null)
        {
            _togRememberAcct.isOn = !string.IsNullOrEmpty(account);
        }
    }

    /// <summary>Inspector 绑定入口：请求打开注册窗口。</summary>
    public void onGotoRegisterBtnClicked()
    {
        if (RegisterRequested != null)
        {
            RegisterRequested.Invoke();
        }
    }

    /// <summary>按钮入口：收集当前表单并通知控制器，本窗口不校验账号密码或协议状态。</summary>
    public void OnLoginBtnClicked()
    {
        // 以下默认值用于对应控件未配置时，按同一参数顺序交给控制器。
        string account = string.Empty;
        // 本次输入的密码，不在窗口中保存。
        string password = string.Empty;
        // 是否记住账号的用户选择。
        bool rememberAccount = false;
        // 是否已勾选用户协议。
        bool agreementAccepted = false;
        if (_iptAcct != null)
        {
            account = _iptAcct.text;
        }
        if (_iptPasd != null)
        {
            password = _iptPasd.text;
        }
        if (_togRememberAcct != null)
        {
            rememberAccount = _togRememberAcct.isOn;
        }
        if (_togAgreement != null)
        {
            agreementAccepted = _togAgreement.isOn;
        }

        if (LoginRequested != null)
        {
            LoginRequested.Invoke(account, password, rememberAccount, agreementAccepted);
        }
    }
}
