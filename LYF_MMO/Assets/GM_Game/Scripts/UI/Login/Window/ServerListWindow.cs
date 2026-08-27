using System;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.Collections;
using TMPro;
using UnityEngine;
using YooAsset;

/**
 * Title:服务器列表窗口
 * Description:
 */


public class ServerListWindow : WindowBase {

    [SerializeField, Header("服务器名称")] private TMP_Text _texServerName;

    [SerializeField, Header("Item父对象")] private Transform _itemParentTrans;

    private RepeatedField<GameServer> _gameServers =null;
    public override void ReFreshUI(object obj)
    {
        GetServerListRet ret =obj as GetServerListRet;
        if (ret != null && ret.GameServers != null && ret.GameServers.Count > 0)
        {
            if(_gameServers!=null && _gameServers.SequenceEqual(ret.GameServers))
            {
                return;
            }

            if (Global.Instance.LoginInfo != null)
            {
                SetServerName(Global.Instance.LoginInfo.GameServer.ServerName);
            }
            _gameServers = ret.GameServers;
            GenerateServerListItem();
        }
        
    }

    private void Start()
    {
        //GanerateServerListItem();
    }

    private async Task GenerateServerListItem()
    {
        AssetOperationHandle handle = Global.Instance.YooPackage.LoadAssetAsync
            ("Assets/GM_Game/Prefabs/UIPrefabs/ServerLiistItemWidget");
        await handle.Task;
        foreach (var gameServer in _gameServers)
        {
            GameObject obj = handle.InstantiateSync();
            obj.transform.parent=_itemParentTrans;
            obj.transform.localScale = Vector3.one;
            obj.transform.localPosition = Vector3.zero;
            GameServerItem itemcop = obj.GetComponent<GameServerItem>();
            if (itemcop != null)
            {
                itemcop.RefreshUI(gameServer);
                itemcop.ONItemClickCB += OnItemClicked;
            }
        }
        
    }
    private GameServer _gameServer;
    public void SetServerName(string str)
    {
        _texServerName.text = str;
    }
    /// <summary>
    /// 服务器列表item点击事件
    /// </summary>
    /// <param name="obj"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void OnItemClicked(GameServer gameServer)
    {
        _gameServer = gameServer;
        SetServerName(gameServer.ServerName);
    }


    public void OnCloseBtnClicked() {
        UIRoot.Instance.LoginViewCtrl.ShowWindow(WindowType.GameServerWindow);

    }

    public void OnConfirmBtnClicked()
    {
        UIRoot.Instance.LoginViewCtrl.ShowWindow(WindowType.GameServerWindow,_gameServer);
    }



}