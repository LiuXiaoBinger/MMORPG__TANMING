using System.Collections;
using System.Collections.Generic;
using Google.Protobuf;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

public class CreateRoleCtrl : CtrlBase
{
    private CreateRoleView _createRoleView;
    public CreateRoleCtrl(UIBase view) : base(view)
    {
        _createRoleView = view as CreateRoleView;
        _createRoleView.InitView();
        RegisCommand();
       

    }
    private void RegisCommand()
    {
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_CreateRoleCode,CreateRoleHandle);
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_StartGameCode,StartGame);
        _createRoleView.RegisCreateRoleBtnClick(OnCreateRoleBtnClicked);
        _createRoleView.RegisStartGameBtnClick(OnStartGameBtnClicked);
    }

    private void StartGame(ByteString data)
    {
        StartGameRet ret = StartGameRet.Parser.ParseFrom(data.ToByteArray());
        if (ret != null && ret.CmdCode == CmdCode.Succeed)
        {
            Debug.Log("开始游戏成功 ...");
            
            //1.缓存主角数据
            Global.Instance.mainRoleInfo = ret.MainRoleInfo;
            //2.加载场景
            SceneMgr.Instance.LoadSceneMode(SceneType.Scene_MainCity, () =>
            {
                //隐藏创建角色界面
                UIRoot.Instance.CreateRoleCtrl.ShowView(false);
                //初始化主城ui
                UIRoot.Instance.InitMainCtrl();
            });
        }
    }

    private void OnStartGameBtnClicked(int roleId)
    {
        StartGameReq req = new StartGameReq(){RoleId = roleId};
        NetSocketMgr.Client.SendData(NetDefine.CMD_StartGameCode,req.ToByteString());
    }

    private void OnCreateRoleBtnClicked(string name,int jobId)
    {
        //验证服务器是否创建成功
        CreateRoleReq req = new CreateRoleReq()
        {
            Nickname = name,
            AccountId = Global.Instance.LoginInfo.AccountId,
            GameServerId = Global.Instance.LoginInfo.GameServer.ServerId,
            JobId = jobId,//目前只有一个角色剑修
        };
        NetSocketMgr.Client.SendData(NetDefine.CMD_CreateRoleCode,req.ToByteString());
       
    }

    private void CreateRoleHandle(ByteString data)
    {
        CreateRoleRet ret = CreateRoleRet.Parser.ParseFrom(data.ToByteArray());
        if (ret != null&&ret.CmdCode==CmdCode.Succeed)
        {
            Debug.Log("角色创建成功");
           ShowWindow(WindowType.SelectRoleWindow,ret);
        }
        else
        {
            Debug.Log("CreateRole Error角色创建失败");
            TipsMgr.Instance.ShowSystemTips("角色创建失败");
        }
    }
    
}
