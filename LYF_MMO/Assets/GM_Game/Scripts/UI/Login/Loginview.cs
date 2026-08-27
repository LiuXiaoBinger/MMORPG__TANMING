using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
* Title:登录视图
* Descrpiton:登录模块所有window的管理视图类
*/

public class Loginview : UIBase
{
    [SerializeField,Header("登录窗口")] private LoginWindow _loginWindow;
    [SerializeField,Header("注册窗口")] private RegistWindow _registWindow;
    [SerializeField,Header("服务器窗口")] private GameServerWindow _gameServerWindow;
    [SerializeField, Header("服务器列表窗口")] private ServerListWindow _serverListWindow;
    
    public override void InitView()
    { 
        base.InitView();
        windowDic.Add(WindowType.LoginWindow, _loginWindow);
        windowDic.Add(WindowType.RegistWindow, _registWindow);
        windowDic.Add(WindowType.GameServerWindow, _gameServerWindow);
        windowDic.Add(WindowType.ServerListWindow, _serverListWindow);
    }
   
    public void RegisGameServerBtnClicked(Action<GameServer> func)
    {
        _gameServerWindow.gameServerBtnClickedAction = func;
    }
    public void RegisVerifyCodeBtnClicked(Action<string> func)
    {
        _registWindow.verifyCodeBtnClickedAction= func;
    }
}
