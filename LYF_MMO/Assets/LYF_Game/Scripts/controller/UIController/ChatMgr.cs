using System;
using Google.Protobuf;

/// <summary>沿 RO 的 ChatMgr 收发世界、当前场景及队伍的文字和语音消息。</summary>
public sealed class ChatMgr : IDisposable
{
    /// <summary>主界面真正显示的 RO 风格聊天窗口。</summary>
    private readonly ChatWindows _view;
    /// <summary>随主界面创建一次并绑定网络回包及输入。</summary><param name="view">MainView 注册的聊天窗口。</param>
    public ChatMgr(ChatWindows view)
    {
        _view = view;
        _view.SendRequested += SendChatMsg;
        _view.VoiceReady += SendAudioMsg;
        SocketDispatcher.Instance.AddEventHandler(NetDefine.RpcC2M_SendChatMsg, OnSendChatMsg);
        SocketDispatcher.Instance.AddEventHandler(NetDefine.PtcT2C_ChattingMsgNtf, OnChattingMsgNtf);
        _view.BindChatAudio();
    }

    /// <summary>将 UI 频道映射为 RO 的服务端编号。</summary><param name="channel">玩家选中的页签。</param><param name="type">对应的协议频道。</param>
    private static bool ChannelType(ChatChannel channel, out ChatMsgType type)
    {
        switch (channel)
        {
            case ChatChannel.World: type = ChatMsgType.ChatTypeWorld; return true;
            case ChatChannel.Nearby: type = ChatMsgType.ChatTypeCurscene; return true;
            case ChatChannel.Team: type = ChatMsgType.ChatTypeTeam; return true;
            default: type = ChatMsgType.ChatTypePrivate; return false;
        }
    }

    /// <summary>提交普通文本，自己发送的行同样等待服务器广播。</summary><param name="channel">目标频道。</param><param name="text">玩家正文。</param>
    private void SendChatMsg(ChatChannel channel, string text)
    {
        Send(channel, new ChatMsg { Medium = ChatMediumType.Text, MsgContent = text });
    }

    /// <summary>上传完成后才将录音 ID 发布到录制时选择的频道。</summary><param name="channel">开始录制时的频道。</param><param name="audio">服务端签发的录音引用。</param>
    private void SendAudioMsg(ChatChannel channel, AudioChat audio)
    {
        Send(channel, new ChatMsg { Medium = ChatMediumType.Audio, Audio = audio });
    }

    /// <summary>两种媒介共用 RO 风格的发送 RPC。</summary><param name="channel">界面频道。</param><param name="msg">待发的强类型消息。</param>
    private static void Send(ChatChannel channel, ChatMsg msg)
    {
        // 支持的频道来自协议显式映射，不能误投到其他页签。
        ChatMsgType type;
        if (!ChannelType(channel, out type)) { TipsMgr.Instance.ShowSystemTips("该频道尚未开放"); return; }
        msg.MsgHead = new ChatMsgHead { MsgType = type };
        if (!MNetClient.Instance.Send(NetDefine.RpcC2M_SendChatMsg, new SendChatMsgArg { Msg = msg }.ToByteString()))
            TipsMgr.Instance.ShowSystemTips("聊天连接不可用");
    }

    /// <summary>处理请求结果，失败原因由服务端给出。</summary><param name="data">SendChatMsgRes 回包。</param>
    private void OnSendChatMsg(ByteString data)
    {
        // 此 RPC 只处理发送状态，正文依赖独立广播。
        SendChatMsgRes res = SendChatMsgRes.Parser.ParseFrom(data);
        if (res.Result == CmdCode.Succeed) return;
        // 冷却拒绝时附上服务端剩余秒数。
        string reason = res.Reason;
        if (res.ResidueChatCd > 0) reason += "，请在 " + res.ResidueChatCd + " 秒后再发";
        TipsMgr.Instance.ShowSystemTips(reason);
    }

    /// <summary>按作者区分自己与他人的消息行，使用 MS 的消息时间。</summary><param name="data">ChatMsg 广播。</param>
    private void OnChattingMsgNtf(ByteString data)
    {
        // 只展示与现有三个页签匹配的服务端通知。
        ChatMsg msg = ChatMsg.Parser.ParseFrom(data);
        if (msg.MsgHead == null) return;
        // UI 使用既有枚举，协议使用 RO 编号。
        ChatChannel channel;
        switch (msg.MsgHead.MsgType)
        {
            case ChatMsgType.ChatTypeWorld: channel = ChatChannel.World; break;
            case ChatMsgType.ChatTypeCurscene: channel = ChatChannel.Nearby; break;
            case ChatMsgType.ChatTypeTeam: channel = ChatChannel.Team; break;
            default: return;
        }
        // 主角可能在切场期间短暂不存在。
        PlayerEntity self = null;
        if (Global.Instance.SceneWorld != null) self = Global.Instance.SceneWorld.LocalRole;
        _view.AppendChattingMsg(msg, channel, self != null && (ulong)self.GetID() == msg.MsgHead.SenderUid);
    }

    /// <summary>注销所有回包及输入，避免重新登录时一条消息展示两次。</summary>
    public void Dispose()
    {
        _view.SendRequested -= SendChatMsg;
        _view.VoiceReady -= SendAudioMsg;
        _view.UnbindChatAudio();
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.RpcC2M_SendChatMsg, OnSendChatMsg);
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.PtcT2C_ChattingMsgNtf, OnChattingMsgNtf);
    }
}
