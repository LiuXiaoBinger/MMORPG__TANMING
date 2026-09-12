using System;
using TMPro;
using UnityEngine;

/// <summary>
/// 游戏服务器窗口，只显示服务器状态并抛出选择事件。
/// </summary>
public class GameServerWindow : WindowBase
{
    /// <summary>服务器状态文本。</summary>
    [SerializeField, Header("服务器状态")] private TMP_Text _texRunState;
    /// <summary>服务器名称文本。</summary>
    [SerializeField, Header("服务器名字")] private TMP_Text _texServerName;

    /// <summary>当前显示的服务器编号。</summary>
    private int _serverId;

    /// <summary>选择当前服务器事件，保留旧字段名兼容已有代码。</summary>
    public Action<int> gameServerBtnClickedAction;

    /// <summary>请求服务器列表事件。</summary>
    public Action ServerListRequested;

    /// <summary>使用控制器提供的展示数据刷新服务器状态。</summary>
    public override void ReFreshUI(object obj)
    {
        GameServerViewData viewData = obj as GameServerViewData;
        if (viewData == null)
        {
            return;
        }

        _serverId = viewData.ServerId;
        Color color = Color.white;
        string runState = string.Empty;
        if (viewData.RunState == 1)
        {
            color = Color.red;
            runState = "爆满";
        }
        else if (viewData.RunState == 2)
        {
            color = Color.yellow;
            runState = "拥挤";
        }
        else if (viewData.RunState == 3)
        {
            color = Color.green;
            runState = "正常";
        }

        if (_texRunState != null)
        {
            _texRunState.color = color;
            _texRunState.text = runState;
        }

        if (_texServerName != null)
        {
            string newServerText = string.Empty;
            if (viewData.IsNew)
            {
                newServerText = "(新服)";
            }
            _texServerName.text = viewData.ServerName + newServerText;
        }
    }

    /// <summary>Inspector 绑定入口：请求打开服务器列表。</summary>
    public void OnGotoServerListBtnClicked()
    {
        if (ServerListRequested != null)
        {
            ServerListRequested.Invoke();
        }
    }

    /// <summary>Inspector 绑定入口：提交当前服务器编号。</summary>
    public void OnGameServerBtnClicked()
    {
        if (gameServerBtnClickedAction != null && _serverId > 0)
        {
            gameServerBtnClickedAction.Invoke(_serverId);
        }
    }
}
