using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单个游戏服务器条目，只显示服务器展示数据并抛出点击事件。
/// </summary>
public class GameServerItem : MonoBehaviour
{
    /// <summary>服务器名称文本。</summary>
    [SerializeField, Header("服务器名称")] private TMP_Text _textSeverName;
    /// <summary>服务器状态图标。</summary>
    [SerializeField, Header("服务器状态")] private Image _imageRun;

    /// <summary>当前条目对应的服务器编号。</summary>
    private int _serverId;
    /// <summary>当前条目显示的服务器名称。</summary>
    private string _serverName = string.Empty;
    /// <summary>服务器条目单击事件。</summary>
    public Action<int> ONItemClickCB;
    /// <summary>服务器条目双击事件。</summary>
    public Action<int> ONItemDoubleClickCB;

    /// <summary>上一次点击的未缩放时间。</summary>
    private float _lastClickTime = -1f;

    /// <summary>获取当前条目的服务器编号。</summary>
    public int ServerId
    {
        get { return _serverId; }
    }

    /// <summary>获取当前条目的服务器名称。</summary>
    public string ServerName
    {
        get { return _serverName; }
    }

    /// <summary>使用控制器提供的展示数据刷新条目。</summary>
    public void RefreshUI(GameServerViewData viewData)
    {
        if (viewData == null)
        {
            return;
        }
        _serverId = viewData.ServerId;
        _serverName = viewData.ServerName;
        Color color = Color.white;
        if (viewData.RunState == 1)
        {
            color = Color.red;
        }
        else if (viewData.RunState == 2)
        {
            color = Color.yellow;
        }
        else if (viewData.RunState == 3)
        {
            color = Color.green;
        }
        if (_imageRun != null)
        {
            _imageRun.color = color;
        }

        string newServerText = string.Empty;
        if (viewData.IsNew)
        {
            newServerText = "(新服)";
        }
        if (_textSeverName != null)
        {
            _textSeverName.text = viewData.ServerName + newServerText;
        }
    }

    /// <summary>上报服务器条目的单击和双击事件。</summary>
    public void OnItemClick()
    {
        if (ONItemClickCB != null)
        {
            ONItemClickCB.Invoke(_serverId);
        }
        if (_lastClickTime >= 0f && Time.unscaledTime - _lastClickTime <= 0.3f)
        {
            _lastClickTime = -1f;
            if (ONItemDoubleClickCB != null)
            {
                ONItemDoubleClickCB.Invoke(_serverId);
            }
            return;
        }
        _lastClickTime = Time.unscaledTime;
    }
}
