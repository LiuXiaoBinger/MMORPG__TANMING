using System;
using Google.Protobuf;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>把江湖聊天窗口接到现有主界面、聊天历史和语音管理器。</summary>
public sealed class YimengChatCtrl : IDisposable
{
    /// <summary>新版窗口，组件和消息行来自江湖聊天预制体。</summary>
    private readonly YimengChatWindow _view;
    /// <summary>旧版聊天继续处理正式回包，也是聊天记录的唯一来源。</summary>
    private readonly ChatWindows _legacy;
    /// <summary>旧窗口已经启用的录音、上传、下载与回放入口。</summary>
    private readonly ChatAudioMgr _audio;
    /// <summary>主控制器已有的角色输入暂停和恢复回调。</summary>
    private readonly Action<bool> _inputFocusChanged;

    /// <summary>复用既有聊天数据和语音，接好窗口路由与收发事件后打开默认速览。</summary>
    /// <param name="view">由 MainView 加载并初始化的江湖聊天子窗口。</param>
    /// <param name="legacy">由既有 ChatMgr 绑定的正式聊天窗口。</param>
    /// <param name="toggleWindow">由 MainCtrl 提供的新版窗口切换方法。</param>
    /// <param name="inputFocusChanged">主界面暂停或恢复角色操作的统一入口。</param>
    public YimengChatCtrl(YimengChatWindow view, ChatWindows legacy, Action toggleWindow, Action<bool> inputFocusChanged)
    {
        _view = view;
        _legacy = legacy;
        _audio = legacy.GetComponent<ChatAudioMgr>();
        _inputFocusChanged = inputFocusChanged;

        view.UseHistory(legacy.Messages);
        view.InputFocusChanged += _inputFocusChanged;
        view.TextRequested += SendText;
        view.VoiceStartRequested += _audio.StartRecord;
        view.VoiceEndRequested += _audio.FinishRecord;
        view.VoicePlayRequested += _audio.PlayRecordedFile;
        legacy.MessageSent += view.OnMessage;
        legacy.MessageReceived += view.OnMessage;
        HideLegacyPresentation();
        // 预制体中的新版窗口初始隐藏，通过主界面既有路由打开左下角速览。
        toggleWindow();
    }

    /// <summary>结束新窗口并解绑回调，旧聊天的呈现随主界面实例一直隐藏。</summary>
    public void Dispose()
    {
        _view.CloseWindow();
        _legacy.MessageSent -= _view.OnMessage;
        _legacy.MessageReceived -= _view.OnMessage;
        _view.InputFocusChanged -= _inputFocusChanged;
        _view.TextRequested -= SendText;
        _view.VoiceStartRequested -= _audio.StartRecord;
        _view.VoiceEndRequested -= _audio.FinishRecord;
        _view.VoicePlayRequested -= _audio.PlayRecordedFile;
    }

    /// <summary>关闭旧聊天的显示和点击，保持消息接收和语音组件运行。</summary>
    private void HideLegacyPresentation()
    {
        // 若主界面重建前曾选中旧聊天输入，先释放其选中状态。
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null)
        {
            // 当前事件系统的选中对象，只在它属于旧聊天时清除。
            GameObject selected = eventSystem.currentSelectedGameObject;
            if (selected != null && selected.transform.IsChildOf(_legacy.transform))
                eventSystem.SetSelectedGameObject(null);
        }
        // 复用主界面实例的显隐层，防止程序关闭新版时重新露出旧入口。
        CanvasGroup legacyPresentation = _legacy.GetComponent<CanvasGroup>();
        if (legacyPresentation == null) legacyPresentation = _legacy.gameObject.AddComponent<CanvasGroup>();
        legacyPresentation.alpha = 0f;
        legacyPresentation.interactable = false;
        legacyPresentation.blocksRaycasts = false;
        legacyPresentation.enabled = true;
    }

    /// <summary>通过既有客户端 RPC 发言；旧版 ChatMgr 统一接收状态和正式广播。</summary>
    /// <param name="channel">新版输入栏选择的发言目标，与旧窗口当前页独立。</param>
    /// <param name="text">已经去掉首尾空白的非空正文。</param>
    private static void SendText(ChatChannel channel, string text)
    {
        // 服务端协议使用的频道编号只支持项目已开放的三类发言。
        ChatMsgType type;
        switch (channel)
        {
            case ChatChannel.World: type = ChatMsgType.ChatTypeWorld; break;
            case ChatChannel.Nearby: type = ChatMsgType.ChatTypeCurscene; break;
            case ChatChannel.Team: type = ChatMsgType.ChatTypeTeam; break;
            default:
                TipsMgr.Instance.ShowSystemTips("该频道尚未开放");
                return;
        }

        // 与已有聊天共用消息的发送入口，正文只在接到广播后加入历史。
        SendChatMsgArg request = new SendChatMsgArg
        {
            Msg = new ChatMsg
            {
                MsgHead = new ChatMsgHead { MsgType = type },
                Medium = ChatMediumType.Text,
                MsgContent = text
            }
        };
        if (!MNetClient.Instance.Send(NetDefine.RpcC2M_SendChatMsg, request.ToByteString()))
            TipsMgr.Instance.ShowSystemTips("聊天连接不可用");
    }
}
