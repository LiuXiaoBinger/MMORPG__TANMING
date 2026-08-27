using System;
using System.Collections;
using System.Collections.Generic;
using Google.Protobuf;
using TMPro;
using UnityEngine;
using YooAsset;

/**
* Title:登录服务器窗口
* Descrpiton:
*/

public class GameServerWindow : WindowBase
{
    [SerializeField, Header("服务器状态")] private TMP_Text _texRunState;
    [SerializeField, Header("服务器名字")] private TMP_Text _texServerName;
    private  GameServer _gameServer;
    public override void ReFreshUI(object obj)
    {
        GameServer gameServer = obj as GameServer;
        if (gameServer != null)
        {
            _gameServer =  gameServer;
            Color color = Color.white;
            string runtate = "";
            if (gameServer.RunState == 1)
            {
                color = Color.red;
                runtate = "爆满";
            }else if(gameServer.RunState==2)
            {
                color = Color.yellow;
                runtate = "拥挤";
            }else if (gameServer.RunState == 3)
            {
                color = Color.green;
                runtate = "正常";
            }
            _texRunState.color = color;
            _texRunState.text = runtate;
            
            
            string str = "";
            if (gameServer.IsNew == 1)
            {
                str = "(新服)";
            }
        
            _texServerName.text = gameServer.ServerName+str;
        }
    }

    public void OnGotoServerListBtnClicked()
    {
        //UIRoot.Instance.LoginViewCtrl.ShowWindow(WindowType.ServerListWindow);
        GetServerListReq req = new GetServerListReq()
        {
            ServerId = 0,
        };
        NetSocketMgr.Client.SendData(NetDefine.CMD_GetServerListCode,req.ToByteString());
    }

    public Action<GameServer> gameServerBtnClickedAction;
    public void OnGameServerBtnClicked()
    {
        gameServerBtnClickedAction.Invoke(_gameServer);
       
    }
}
