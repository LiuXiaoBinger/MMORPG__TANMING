using System;
using System.Collections;
using System.Collections.Generic;
using Google.Protobuf;
using UnityEngine;
using YooAsset;

/**
* Title:登录模块控制器
* Descrpiton:
*/

public class LoginCtrl : CtrlBase
{
   private Loginview _loginview;
   private int _serverId;

   public LoginCtrl(UIBase view) : base(view)
   {
       _loginview = view as Loginview;
       _loginview.InitView();
       RegisCommand();
       

   }
   private void RegisCommand()
   {
       SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_RegistCode,OnRegistHandle);
       SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_LoginCode,OnLoginHandle);
       SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_GetServerListCode,OnGetServerListHandle);
       SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_LoginGameServerCode,OnLoginGameServerHandle);
       
       //注册点击事件
       _loginview.RegisGameServerBtnClicked(OnGameServerBtnClicked);
       _loginview.RegisVerifyCodeBtnClicked(OnVerifyCodeBtnClicked);
   }

   private void OnGameServerBtnClicked(GameServer server)
   {
       _serverId = server.ServerId;
       //
       NetSocketMgr.Instance.ConnectServer(server.IpHost, server.Prot,OnConnSucced,OnConnFilded);
     
   }

   private void OnConnFilded()
   {
       TipsMgr.Instance.ShowSystemTips("登录服务器失败");
   }

   private void OnConnSucced()
   {
       Debug.Log("连接服务端成功");
       //服务器请求
       LoginGameServerReq req = new LoginGameServerReq()
       {
           AccountId = Global.Instance.LoginInfo.AccountId,
           GameServerId = _serverId,
       };
       NetSocketMgr.Client.SendData(NetDefine.CMD_LoginGameServerCode,req.ToByteString());
     
   }

   private void OnVerifyCodeBtnClicked(string emil)
   {
       RegistVarifyReq req = new RegistVarifyReq()
       {
           
           Email =  emil
          
       };
       NetSocketMgr.Client.SendData(NetDefine.CMD_RegistVarifyCode,req.ToByteString());
   }

   private void OnLoginGameServerHandle(ByteString data)
   {
       LoginGameServerRet ret =LoginGameServerRet.Parser.ParseFrom(data);
       if (ret != null && ret.CmdCode == CmdCode.Succeed)
       {
           Debug.Log("登录游戏服务器返回数据"+ret.ToString());
           //打开创建角色场景Scene_CreaRole
           SceneMgr.Instance.LoadSceneMode(SceneType.Scene_CreateRole, () =>
           {
               UIRoot.Instance.LoginViewCtrl.ShowView(false);
               //1.是否已经创建角色
               if (ret.CreateRoleInfo != null)
               {
                   UIRoot.Instance.CreateRoleCtrl.ShowWindow(WindowType.SelectRoleWindow,ret.CreateRoleInfo);
               }
               else
               {
                   //2.还未创建角色，跳转创建角色界面
                   UIRoot.Instance.CreateRoleCtrl.ShowWindow(WindowType.CreateRoleWindow);
               }
           });
       }
   }

   private void OnGetServerListHandle(ByteString data)
   {
       GetServerListRet ret =GetServerListRet.Parser.ParseFrom(data);
       if (ret != null && ret.CmdCode == CmdCode.Succeed)
       {
           Debug.Log("获取服务器数据成功：："+ret.ToString());
           ShowWindow(WindowType.ServerListWindow,ret);
           
       }
       else
       {
           Debug.Log("获取服务器数据失败：："+ret.ToString());
       }
   }

   //登录请求结果返回数据
   private void OnLoginHandle(ByteString data)
   {
       LoginRet ret =LoginRet.Parser.ParseFrom(data);
       if (ret != null & ret.CmdCode == CmdCode.Succeed)
       {
           Debug.Log("登录成功...."+ret.ToString());
           TipsMgr.Instance.ShowSystemTips("登录成功...");
           Global.Instance.LoginInfo = ret;
           ShowWindow(WindowType.GameServerWindow,ret.GameServer);
       }
       else
       {
           Debug.Log("登录失败....");
           TipsMgr.Instance.ShowSystemTips("登录失败...");
       }
   }

   /// <summary>
   /// 处理服务端返回回来的注册结果
   /// </summary>
   /// <param name="data"></param>
   /// <exception cref="NotImplementedException"></exception>
   private void OnRegistHandle(ByteString data)
   {
       RegistRet ret = RegistRet.Parser.ParseFrom(data);
       if (ret != null && ret.CmdCode == CmdCode.Succeed)
       {
           Debug.Log("注册成功....");
           TipsMgr.Instance.ShowSystemTips("注册成功...请登录");
           ShowWindow(WindowType.LoginWindow);
       }
       else if (ret != null &&CmdCode.VarifyError==ret.CmdCode)
       {
           Debug.Log("验证码不对....");
           TipsMgr.Instance.ShowSystemTips("验证码不对...");
       }
       else
       {
           Debug.Log("注册失败...."+ret.ToString());
           TipsMgr.Instance.ShowSystemTips("注册失败...");
       }
       
   }
}

   
