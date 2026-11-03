using System;
using TMPro;
using UnityEngine;

/// <summary>
/// 注册窗口，只采集注册输入并抛出用户操作事件。
/// </summary>
public class RegistWindow : WindowBase
{
    /// <summary>新账号名称输入。</summary>
    [SerializeField, Header("账号输入框")] private TMP_InputField _iptAcct;
    /// <summary>接收注册验证码的邮箱；字段沿用旧 Mobile 命名，实际传给协议 Email。</summary>
    [SerializeField, Header("邮箱输入框")] private TMP_InputField _iptMobile;
    /// <summary>用户收到后填写的邮箱验证码。</summary>
    [SerializeField, Header("验证码输入框")] private TMP_InputField _iptVerify;
    /// <summary>要为新账号设置的密码。</summary>
    [SerializeField, Header("密码输入框")] private TMP_InputField _iptPasd;
    /// <summary>再次输入的密码，由控制器比较两次输入是否一致。</summary>
    [SerializeField, Header("确认密码输入框")] private TMP_InputField _iptSurePasd;

    /// <summary>提交账号、邮箱、验证码、密码、确认密码，参数严格按此顺序传出。</summary>
    public Action<string, string, string, string, string> RegisterRequested;

    /// <summary>请求向输入的邮箱发送验证码，回调参数为邮箱文本。</summary>
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

    /// <summary>读取 inputField 当前文本；控件未配置时返回空字符串。</summary>
    private static string GetText(TMP_InputField inputField)
    {
        if (inputField == null)
        {
            return string.Empty;
        }
        return inputField.text;
    }
}
