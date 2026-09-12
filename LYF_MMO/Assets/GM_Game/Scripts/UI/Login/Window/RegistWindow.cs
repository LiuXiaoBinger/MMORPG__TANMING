using System;
using TMPro;
using UnityEngine;

/// <summary>
/// 注册窗口，只采集注册输入并抛出用户操作事件。
/// </summary>
public class RegistWindow : WindowBase
{
    [SerializeField, Header("账号输入框")] private TMP_InputField _iptAcct;
    [SerializeField, Header("邮箱输入框")] private TMP_InputField _iptMobile;
    [SerializeField, Header("验证码输入框")] private TMP_InputField _iptVerify;
    [SerializeField, Header("密码输入框")] private TMP_InputField _iptPasd;
    [SerializeField, Header("确认密码输入框")] private TMP_InputField _iptSurePasd;

    /// <summary>注册提交事件。</summary>
    public Action<string, string, string, string, string> RegisterRequested;

    /// <summary>验证码请求事件，保留旧字段名兼容已有绑定代码。</summary>
    public Action<string> verifyCodeBtnClickedAction;

    /// <summary>返回登录窗口事件。</summary>
    public Action BackRequested;

    /// <summary>Inspector 绑定入口：提交注册表单。</summary>
    public void OnRegistBtnClicked()
    {
        if (RegisterRequested == null)
        {
            return;
        }

        RegisterRequested.Invoke(GetText(_iptAcct), GetText(_iptMobile), GetText(_iptVerify),
            GetText(_iptPasd), GetText(_iptSurePasd));
    }

    /// <summary>Inspector 绑定入口：请求验证码。</summary>
    public void OnVerifyCodeBtnClicked()
    {
        if (verifyCodeBtnClickedAction != null)
        {
            verifyCodeBtnClickedAction.Invoke(GetText(_iptMobile));
        }
    }

    /// <summary>Inspector 绑定入口：返回登录窗口。</summary>
    public void OnBackBtnClicked()
    {
        if (BackRequested != null)
        {
            BackRequested.Invoke();
        }
    }

    /// <summary>安全读取输入框文本。</summary>
    private static string GetText(TMP_InputField inputField)
    {
        if (inputField == null)
        {
            return string.Empty;
        }
        return inputField.text;
    }
}
