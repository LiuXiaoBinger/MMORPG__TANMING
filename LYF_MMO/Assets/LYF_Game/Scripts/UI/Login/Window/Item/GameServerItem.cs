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
    /// <summary>每次点击都会触发的选择事件，参数为本条目的服务器编号。</summary>
    public Action<int> ONItemClickCB;
    /// <summary>两次点击间隔不超过 0.3 秒时追加触发的确认事件，参数为服务器编号。</summary>
    public Action<int> ONItemDoubleClickCB;

    /// <summary>上次点击时的 Time.unscaledTime 秒数；-1 表示当前没有等待组成双击的首击。</summary>
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
        // 浅色底上的状态标记按爆满、拥挤、正常显示砖红、赭黄、深绿，未识别状态显示灰色。
        Color color = new Color32(84, 94, 95, 255);
        if (viewData.RunState == 1)
        {
            color = new Color32(157, 60, 52, 255);
        }
        else if (viewData.RunState == 2)
        {
            color = new Color32(143, 92, 28, 255);
        }
        else if (viewData.RunState == 3)
        {
            color = new Color32(28, 108, 81, 255);
        }
        if (_imageRun != null)
        {
            _imageRun.color = color;
        }

        // 新服名称后缀，不改变用于查询的原始服务器名。
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

    /// <summary>先上报本次单击，再按未缩放时间判断是否追加双击；双击成功后重置计时。</summary>
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
