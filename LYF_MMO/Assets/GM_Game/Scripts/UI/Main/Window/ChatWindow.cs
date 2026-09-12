using System;
using TMPro;
using UnityEngine;

/// <summary>
/// 聊天窗口，只采集输入并显示控制器确认的聊天条目。
/// </summary>
public class ChatWindow : WindowBase
{
    /// <summary>聊天消息条目父节点。</summary>
    [SerializeField, Header("消息展示框")] private Transform _content;
    /// <summary>聊天频道选择框。</summary>
    [SerializeField, Header("发送消息频道选择")] private TMP_Dropdown _dropdown;
    /// <summary>聊天文本输入框。</summary>
    [SerializeField, Header("发送消息的内容")] private TMP_InputField _iptChat;

    /// <summary>输入焦点变化事件，true 表示正在输入。</summary>
    public event Action<bool> InputFocusChanged;

    /// <summary>聊天发送事件。</summary>
    public event Action<string, string> SendRequested;

    /// <summary>注册输入框焦点监听。</summary>
    private void Start()
    {
        if (_iptChat == null)
        {
            return;
        }
        _iptChat.onSelect.AddListener(OnInputSelected);
        _iptChat.onDeselect.AddListener(OnInputDeselected);
    }

    /// <summary>销毁时注销输入框焦点监听。</summary>
    private void OnDestroy()
    {
        if (_iptChat != null)
        {
            _iptChat.onSelect.RemoveListener(OnInputSelected);
            _iptChat.onDeselect.RemoveListener(OnInputDeselected);
        }
    }

    /// <summary>通知控制器聊天输入获得焦点。</summary>
    private void OnInputSelected(string value)
    {
        if (InputFocusChanged != null)
        {
            InputFocusChanged.Invoke(true);
        }
    }

    /// <summary>通知控制器聊天输入失去焦点。</summary>
    private void OnInputDeselected(string value)
    {
        if (InputFocusChanged != null)
        {
            InputFocusChanged.Invoke(false);
        }
    }

    /// <summary>Inspector 绑定入口：提交当前频道和消息文本。</summary>
    public void OnSendBrnClicked()
    {
        string channel = string.Empty;
        string message = string.Empty;
        if (_dropdown != null && _dropdown.value >= 0 && _dropdown.value < _dropdown.options.Count)
        {
            TMP_Dropdown.OptionData option = _dropdown.options[_dropdown.value];
            if (option != null)
            {
                channel = option.text;
            }
        }
        if (_iptChat != null)
        {
            message = _iptChat.text;
            // 发送点击立即消费本次输入，异步消息条目创建不得覆盖后续新输入。
            _iptChat.text = string.Empty;
        }
        if (SendRequested != null)
        {
            SendRequested.Invoke(channel, message);
        }
    }

    /// <summary>生成并显示一条聊天消息。</summary>
    public void AppendMessage(string channel, string nickname, string message)
    {
        ResourceMgr.Instance.LoadPrefabAsync("UIPrefabs/ChatItemWidget", itemObject =>
        {
            if (itemObject == null || _content == null)
            {
                return;
            }
            itemObject.transform.SetParent(_content, false);
            ChatItemWidget widget = itemObject.GetComponent<ChatItemWidget>();
            if (widget != null)
            {
                widget.RefreshUI(channel, nickname, message);
            }
        });
    }
}
