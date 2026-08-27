using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UI;

/**
* Title:
* Descrpiton:
*/

public class GameServerItem : MonoBehaviour
{
    [SerializeField,Header("服务器名称")]private TMP_Text _textSeverName;
    [SerializeField,Header("服务器状态")]private Image _imageRun;
    
    private GameServer _gameServer;
    public Action<GameServer> ONItemClickCB;
    public void RefreshUI(GameServer gameServer)
    {
        _gameServer = gameServer;
        Color color = Color.white;
        if (gameServer.RunState == 1)
        {
            color = Color.red;
        }else if(gameServer.RunState==2)
        {
            color = Color.yellow;
            
        }else if (gameServer.RunState == 3)
        {
            color = Color.green;
        }
        _imageRun.color = color;
        string str = "";
        if (gameServer.IsNew == 1)
        {
            str = "(新服)";
        }
        
        _textSeverName.text = gameServer.ServerName+str;
    }

    private int _clickCount = 0;
    public void OnItemClick()
    {
        ++_clickCount;
        
        if(_clickCount>=2)
        {
            _clickCount = 0;
            UIRoot.Instance.LoginViewCtrl.ShowWindow(WindowType.GameServerWindow,_gameServer);
        }
        ONItemClickCB?.Invoke(_gameServer);
    }

    private void ResetClickCount()
    {
        Observable.Timer(TimeSpan.FromMilliseconds(300)).Subscribe(_ =>
        {
            _clickCount = 0;
        });
    }
    
}
