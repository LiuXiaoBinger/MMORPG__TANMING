using System;
using System.Collections;
using System.Collections.Generic;
using Google.Protobuf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
/**
* Title: 注册窗口
* Descrpiton:
*/

public class RegistWindow : WindowBase
{
    [SerializeField, Header("账号输入框")] private TMP_InputField _iptAcct;
    [SerializeField, Header("邮箱输入框")] private TMP_InputField _iptMobile;
    [SerializeField, Header("验证码输入框")] private TMP_InputField _iptVerify;
    [SerializeField, Header("密码输入框")] private TMP_InputField _iptPasd;
    [SerializeField, Header("确认密码输入框")] private TMP_InputField _iptSurePasd;

    public void OnRegistBtnClicked()
    {
        //1.判断输入框是否为空。
        if (string.IsNullOrEmpty(_iptAcct.text)) {
            Debug.Log("账号为空..");
            TipsMgr.Instance.ShowSystemTips("请输入账号..");
            return;
        }

        if (string.IsNullOrEmpty(_iptMobile.text)) {
            Debug.Log("邮箱为空..");
            TipsMgr.Instance.ShowSystemTips("手机号为空..");
            return;
        }

        if (string.IsNullOrEmpty(_iptVerify.text)) {
            TipsMgr.Instance.ShowSystemTips("请输入验证码..");
            Debug.Log("验证码为空..");
            return;
        }

        if (string.IsNullOrEmpty(_iptPasd.text)) {
            Debug.Log("密码为空..");
            return;
        }

        if (string.IsNullOrEmpty(_iptSurePasd.text)) {
            Debug.Log("确认密码为空..");
            return;
        }

        //2.验证账号，手机号码，密码的合法性。

        //3.判断密码和确认是否一致，
        if (!_iptPasd.text.Equals(_iptSurePasd.text))
        {
            Debug.Log("两次密码不一致");
            return;
        }
        //4，开始注册
        //TODO
        //Debug.Log("注册成功..");
        //Show(false);
        
        RegistReq req = new RegistReq()
        {
            UserName =  _iptAcct.text,
            Email =  _iptMobile.text,
            Varify =  _iptVerify.text,
            Password = _iptPasd.text,
        };
        NetSocketMgr.Client.SendData(NetDefine.CMD_RegistCode,req.ToByteString());
    }
    public Action<string> verifyCodeBtnClickedAction;
    public void OnVerifyCodeBtnClicked() {

       // Debug.Log("获取验证码成功..");
        if (string.IsNullOrEmpty(_iptMobile.text)) {
            Debug.Log("邮箱为空..");
            TipsMgr.Instance.ShowSystemTips("手机号为空..");
            return;
        }
        verifyCodeBtnClickedAction?.Invoke(_iptMobile.text);
        
    }
    
    public void OnBackBtnClicked() 
    {
        UIRoot.Instance.LoginViewCtrl.ShowWindow(WindowType.LoginWindow);        
    }
}
