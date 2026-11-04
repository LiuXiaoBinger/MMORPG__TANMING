using System;
using System.Collections.Generic;
using Google.Protobuf;

/// <summary>MS 的频道聊天管理器，沿 RO 的 SendChatMsg 请求、回执和广播通知分离处理。</summary>
internal sealed class CChatMgr
{
    /// <summary>当前分区连接，用来回送请求结果与分发命令。</summary>
    private readonly BaseLink _game;
    /// <summary>同一连接对应的权威队伍集合。</summary>
    private readonly TeamManager _teams;
    /// <summary>用于确认语音引用确实由当前角色上传。</summary>
    private readonly ChatAudioRecordMgr _audio;
    /// <summary>角色在各频道上次获准发言的 Unix 秒时间戳，角色下线后移除。</summary>
    private readonly Dictionary<ulong, Dictionary<ChatMsgType, long>> _lastSent = new Dictionary<ulong, Dictionary<ChatMsgType, long>>();

    /// <summary>为一个游戏分区绑定其聊天和队伍权威。</summary>
    /// <param name="game">分区 GS 连接。</param><param name="teams">同分区的真实队伍名单。</param>
    /// <param name="audio">本分区已完成上传的录音。</param>
    internal CChatMgr(BaseLink game, TeamManager teams, ChatAudioRecordMgr audio) { _game = game; _teams = teams; _audio = audio; }

    /// <summary>角色离开游戏时释放其发言冷却记录。</summary>
    /// <param name="uid">下线角色号。</param>
    internal void OnRoleOffline(ulong uid) { _lastSent.Remove(uid); }

    /// <summary>受理来自已认证 GS 的发言，单独回复发起会话，并下发真实聊天行。</summary>
    /// <param name="request">保留 GS 确认的网关及玩家会话寻址。</param>
    internal void SendMsg(BasePackage request)
    {
        // 客户端正文已经过 GS 身份替换。
        ChatMsg msg = SendChatMsgArg.Parser.ParseFrom(request.Data).Msg;
        // 发送结果与通知分别编码，自身也依赖通知显示消息。
        SendChatMsgRes result = new SendChatMsgRes();
        // 队伍有显式收件名单，世界与场景频道由 GS 的在线实体索引投递。
        ChatMsgWithLists broadcast = new ChatMsgWithLists();
        if (msg == null || msg.MsgHead == null) result.Reason = "聊天消息缺少频道";
        else Validate(msg, result, broadcast);
        if (result.Reason.Length != 0) result.Result = CmdCode.ReqParamError;
        _game.Send(new BasePackage { ProtoCode = NetDefine.RpcC2M_SendChatMsg, GateSessionId = request.GateSessionId,
            UnitySessionId = request.UnitySessionId, Data = result.ToByteString() });
        if (result.Result != CmdCode.Succeed) return;
        broadcast.ChatMsg = msg;
        if (msg.MsgHead.MsgType == ChatMsgType.ChatTypeCurscene)
        {
            _game.Send(NetDefine.PtcM2G_ChatBroadCastM2Gs, broadcast.ToByteString());
            return;
        }
        _game.Send(NetDefine.PtcM2T_ChatBroadCastM2T, broadcast.ToByteString());
    }

    /// <summary>检查媒介和频道，限定收件范围，并记录本次获准发言的时间。</summary>
    /// <param name="msg">待分发的消息。</param><param name="result">拒绝原因或可见冷却。</param><param name="broadcast">队伍收件人列表。</param>
    private void Validate(ChatMsg msg, SendChatMsgRes result, ChatMsgWithLists broadcast)
    {
        // 本次角色和频道来自 GS 的已认证角色、原始选项。
        ulong uid = msg.MsgHead.SenderUid;
        // 不接受保留的私聊或未接入的其他频道。
        ChatMsgType channel = msg.MsgHead.MsgType;
        // RO 参考配置的基础发言间隔，单位为秒。
        int cooldown;
        switch (channel)
        {
            case ChatMsgType.ChatTypeWorld: cooldown = 10; break;
            case ChatMsgType.ChatTypeCurscene: cooldown = 5; break;
            case ChatMsgType.ChatTypeTeam:
                if (!_teams.GetChatMembers(uid, broadcast))
                {
                    result.Reason = "请先加入队伍";
                    return;
                }
                cooldown = 2;
                break;
            default: result.Reason = "该频道不能发言"; return;
        }
        if (msg.Medium == ChatMediumType.Text)
        {
            msg.MsgContent = msg.MsgContent.Trim();
            if (msg.MsgContent.Length == 0 || TextLength(msg.MsgContent) > 74) { result.Reason = "聊天内容为空或超出 74 个计数单位"; return; }
            msg.Audio = null;
        }
        else if (msg.Medium == ChatMediumType.Audio)
        {
            // 只有此角色上传并由服务端确认的录音才能发布。
            AudioChat audio = msg.Audio;
            if (audio == null || !_audio.PrepareMessage(uid, audio)) { result.Reason = "语音文件尚未上传"; return; }
            if (!(audio.Duration > 0 && audio.Duration <= 30) || TextLength(audio.Text) > 70) { result.Reason = "语音长度或转写超出限制"; return; }
            msg.MsgContent = string.Empty;
        }
        else { result.Reason = "未知聊天媒介"; return; }
        // 使用服务端时间，而非信任客户端填写的发送时间。
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        // 同一个角色各频道独立计算冷却。
        Dictionary<ChatMsgType, long> times;
        if (!_lastSent.TryGetValue(uid, out times))
        {
            times = new Dictionary<ChatMsgType, long>();
            _lastSent.Add(uid, times);
        }
        // 只有在实际发过该频道时才计算剩余时间。
        long last;
        if (times.TryGetValue(channel, out last) && last + cooldown > now)
        {
            result.ResidueChatCd = last + cooldown - now;
            result.Reason = "发言过快";
            return;
        }
        times[channel] = now;
        result.ResidueChatCd = cooldown;
        msg.MsgHead.MsgTime = (ulong)now;
    }

    /// <summary>按 ASCII 计 1、非 ASCII 码点计 2 的频道长度单位计数。</summary>
    /// <param name="text">文字或转写。</param><returns>消息占用的频道长度单位。</returns>
    private static int TextLength(string text)
    {
        // 多字节码点按一个字符处理，包括补充平面的表情。
        int units = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] <= 127) units++;
            else { units += 2; if (char.IsSurrogatePair(text, i)) i++; }
        }
        return units;
    }
}
