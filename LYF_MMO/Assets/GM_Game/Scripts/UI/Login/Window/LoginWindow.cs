using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 登录窗口，只采集输入并把用户操作交给登录控制器。
/// </summary>
public class LoginWindow : WindowBase
{
    [SerializeField, Header("账号输入框")] private TMP_InputField _iptAcct;
    [SerializeField, Header("密码输入框")] private TMP_InputField _iptPasd;
    [SerializeField, Header("记住账号Toggle")] private Toggle _togRememberAcct;
    [SerializeField, Header("用户协议勾选")] private Toggle _togAgreement;

    /// <summary>登录按钮点击事件。</summary>
    public Action<string, string, bool, bool> LoginRequested;

    /// <summary>前往注册窗口事件。</summary>
    public Action RegisterRequested;

    /// <summary>
    /// 显示本地保存的协议状态和账号。
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

    /// <summary>Inspector 绑定入口：提交当前登录输入。</summary>
    public void OnLoginBtnClicked()
    {
        string account = string.Empty;
        string password = string.Empty;
        bool rememberAccount = false;
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
