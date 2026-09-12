using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 服务器列表窗口，负责列表实例和选中状态显示。
/// </summary>
public class ServerListWindow : WindowBase
{
    /// <summary>当前选中服务器名称文本。</summary>
    [SerializeField, Header("服务器名称")] private TMP_Text _texServerName;
    /// <summary>服务器条目父节点。</summary>
    [SerializeField, Header("Item父对象")] private Transform _itemParentTrans;

    /// <summary>当前生成的服务器条目。</summary>
    private readonly List<GameObject> _serverItems = new List<GameObject>();

    /// <summary>当前选中的服务器编号。</summary>
    private int _selectedServerId;

    /// <summary>列表刷新版本，用于丢弃上一次异步加载回调。</summary>
    private int _listVersion;

    /// <summary>关闭列表事件。</summary>
    public Action CloseRequested;

    /// <summary>确认选择事件。</summary>
    public Action<int> ConfirmRequested;

    /// <summary>使用控制器提供的服务器展示列表刷新窗口。</summary>
    public override void ReFreshUI(object obj)
    {
        List<GameServerViewData> serverList = obj as List<GameServerViewData>;
        if (serverList == null)
        {
            return;
        }

        GenerateServerListItems(serverList);
    }

    /// <summary>
    /// 更新服务器列表；重复刷新前先清理旧条目。
    /// </summary>
    private void GenerateServerListItems(List<GameServerViewData> gameServers)
    {
        ClearServerItems();
        int listVersion = ++_listVersion;
        ResourceMgr.Instance.LoadPrefabAsync("UIPrefabs/ServerLiistItemWidget", prefab =>
        {
            if (listVersion != _listVersion)
            {
                if (prefab != null)
                {
                    Destroy(prefab);
                }
                return;
            }
            if (prefab == null)
            {
                return;
            }

            // ResourceMgr 已创建首个实例，先用它显示第一条，其余条目继续加载同一预制体。
            if (gameServers.Count == 0)
            {
                Destroy(prefab);
                return;
            }

            AddServerItem(prefab, gameServers[0]);
            for (int index = 1; index < gameServers.Count; index++)
            {
                GameServerViewData server = gameServers[index];
                ResourceMgr.Instance.LoadPrefabAsync("UIPrefabs/ServerLiistItemWidget", itemObject =>
                {
                    if (listVersion != _listVersion)
                    {
                        if (itemObject != null)
                        {
                            Destroy(itemObject);
                        }
                        return;
                    }
                    AddServerItem(itemObject, server);
                });
            }
        });
    }

    /// <summary>
    /// 初始化单个服务器条目并注册点击事件。
    /// </summary>
    private void AddServerItem(GameObject itemObject, GameServerViewData gameServer)
    {
        if (itemObject == null || _itemParentTrans == null || gameServer == null)
        {
            if (itemObject != null)
            {
                Destroy(itemObject);
            }
            return;
        }

        itemObject.transform.SetParent(_itemParentTrans, false);
        _serverItems.Add(itemObject);
        GameServerItem item = itemObject.GetComponent<GameServerItem>();
        if (item != null)
        {
            item.RefreshUI(gameServer);
            item.ONItemClickCB += OnItemClicked;
            item.ONItemDoubleClickCB += OnItemConfirmed;
        }
    }

    /// <summary>
    /// 销毁上次刷新生成的服务器条目。
    /// </summary>
    private void ClearServerItems()
    {
        _selectedServerId = 0;
        for (int index = 0; index < _serverItems.Count; index++)
        {
            GameObject serverItem = _serverItems[index];
            if (serverItem != null)
            {
                Destroy(serverItem);
            }
        }
        _serverItems.Clear();
        SetServerName(string.Empty);
    }

    /// <summary>更新当前选中的服务器编号和名称。</summary>
    private void OnItemClicked(int serverId)
    {
        _selectedServerId = serverId;
        GameServerItem selectedItem = null;
        for (int index = 0; index < _serverItems.Count; index++)
        {
            GameObject itemObject = _serverItems[index];
            if (itemObject == null)
            {
                continue;
            }
            GameServerItem candidate = itemObject.GetComponent<GameServerItem>();
            if (candidate != null && candidate.ServerId == serverId)
            {
                selectedItem = candidate;
                break;
            }
        }
        if (selectedItem != null)
        {
            SetServerName(selectedItem.ServerName);
        }
    }

    /// <summary>双击条目时向控制器提交当前服务器。</summary>
    private void OnItemConfirmed(int serverId)
    {
        _selectedServerId = serverId;
        if (ConfirmRequested != null)
        {
            ConfirmRequested.Invoke(serverId);
        }
    }

    /// <summary>设置当前服务器名称文本。</summary>
    public void SetServerName(string serverName)
    {
        if (_texServerName != null)
        {
            _texServerName.text = serverName;
        }
    }

    /// <summary>Inspector 绑定入口：关闭服务器列表。</summary>
    public void OnCloseBtnClicked()
    {
        if (CloseRequested != null)
        {
            CloseRequested.Invoke();
        }
    }

    /// <summary>Inspector 绑定入口：确认当前服务器编号。</summary>
    public void OnConfirmBtnClicked()
    {
        if (ConfirmRequested != null)
        {
            ConfirmRequested.Invoke(_selectedServerId);
        }
    }
}
