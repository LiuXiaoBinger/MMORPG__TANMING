using System;
using System.Collections.Generic;
using Google.Protobuf;

/// <summary>借助组队冒烟的真实 GS→MS 连接和网关套接字验证聊天分发及分块媒体。</summary>
public static class ChatAuthoritySmoke
{
    /// <summary>聊天断言数量，失败会中断当前测试进程。</summary>
    private static int _checks;
    /// <summary>与正式 GS 通信的隔离网关端点。</summary>
    private static LoopbackLink _gate;

    /// <summary>使用同一真实队伍及两个圈外角色核对各频道的实际收件人。</summary>
    /// <param name="gate">正式协议注册的 GS 回环连接。</param><param name="owner">51 号会话的队长。</param>
    /// <param name="member">52 号会话的队员。</param><param name="other">53 号会话的另一实例角色。</param>
    /// <param name="far">54 号会话的另一地图角色。</param>
    public static void Run(LoopbackLink gate, OnlineRole owner, OnlineRole member, OnlineRole other, OnlineRole far)
    {
        _gate = gate;
        // 记录原地图，完成测试后继续既有副本回归。
        int map = owner.mainRoleInfo.BaseInfo.MapId;
        owner.mainRoleInfo.BaseInfo.MapId = 7; member.mainRoleInfo.BaseInfo.MapId = 7;
        other.mainRoleInfo.BaseInfo.MapId = 7; far.mainRoleInfo.BaseInfo.MapId = 8;
        other.mainRoleInfo.BaseInfo.InstanceId = 222;
        member.mainRoleInfo.BaseInfo.Pos = "900_0_900";

        CheckSend(51, Text(ChatMsgType.ChatTypeWorld, " 世界 "), true, 51, 52, 53, 54);
        CheckSend(51, Text(ChatMsgType.ChatTypeWorld, "过快"), false);
        CheckSend(51, Text(ChatMsgType.ChatTypeCurscene, "同场景可见"), true, 51, 52);
        CheckSend(52, Text(ChatMsgType.ChatTypeTeam, "队员发言"), true, 51, 52);
        CheckSend(53, Text(ChatMsgType.ChatTypeTeam, "无队伍"), false);
        CheckSend(53, Text(ChatMsgType.ChatTypePrivate, "未开放"), false);
        CheckSend(53, Text(ChatMsgType.ChatTypeWorld, new string('中', 38)), false);
        CheckSend(53, Text(ChatMsgType.ChatTypeWorld, "  "), false);
        CheckSend(54, Text(ChatMsgType.ChatTypeWorld, new string('中', 37)), true, 51, 52, 53, 54);
        CheckAudio();
        // 假会话无法借用聊天正文里填写的真实角色号发言。
        _gate.SendFromGate(99, NetDefine.RpcC2M_SendChatMsg, new SendChatMsgArg { Msg = Text(ChatMsgType.ChatTypeWorld, "假会话") });
        Expect(CountChat(Collect(400)) == 0, "未登录会话没有广播或成功回执");
        owner.mainRoleInfo.BaseInfo.MapId = map;
        member.mainRoleInfo.BaseInfo.MapId = map;
        member.mainRoleInfo.BaseInfo.Pos = "0_0_0";
        Console.WriteLine("CHAT_AUTHORITY_SMOKE: PASS checks=" + _checks);
    }

    /// <summary>构造带伪造身份及时间的文本，验证生产链路替换它们。</summary>
    /// <param name="channel">测试频道。</param><param name="body">原始正文。</param>
    private static ChatMsg Text(ChatMsgType channel, string body)
    {
        return new ChatMsg { MsgHead = new ChatMsgHead { MsgType = channel, SenderUid = 123, MsgTime = 1 },
            Medium = ChatMediumType.Text, MsgContent = body, SenderName = "伪造昵称" };
    }

    /// <summary>只收集本次时间窗内发到测试网关的帧，同时驱动 GS 命令队列。</summary><param name="milliseconds">收件窗口长度。</param>
    private static List<DecodedFrame> Collect(int milliseconds)
    {
        // 允许实际 TCP 读取与 GS 主循环跨线程交替执行。
        DateTime stop = DateTime.UtcNow.AddMilliseconds(milliseconds);
        // 记录所有目标，才可以判断没有越过频道边界或重复下发。
        List<DecodedFrame> all = new List<DecodedFrame>();
        do { GameGlobal.Instance.ProcessNetMessage(); all.AddRange(_gate.WaitWindow(1)); } while (DateTime.UtcNow < stop);
        return all;
    }

    /// <summary>统计聊天回执和正文通知，排除原组队用例的其他帧。</summary><param name="all">测试连接收到的全部帧。</param>
    private static int CountChat(List<DecodedFrame> all)
    {
        // 此窗口实际聊天帧总数。
        int count = 0;
        for (int i = 0; i < all.Count; i++)
            if (all[i].ProtoCode == NetDefine.RpcC2M_SendChatMsg || all[i].ProtoCode == NetDefine.PtcT2C_ChattingMsgNtf) count++;
        return count;
    }

    /// <summary>检查回执只到发送者、每个收件人仅获一条权威广播。</summary><param name="sender">真实登录会话号。</param>
    /// <param name="msg">待发数据。</param><param name="ok">是否应当成功。</param><param name="targets">所有预期收到消息的会话。</param>
    private static void CheckSend(int sender, ChatMsg msg, bool ok, params int[] targets)
    {
        _gate.SendFromGate(sender, NetDefine.RpcC2M_SendChatMsg, new SendChatMsgArg { Msg = msg });
        // 通知与 RPC 回执可以连续出现在同一次 TCP 读取中。
        List<DecodedFrame> all = Collect(620);
        // 独立验证所有目标以及不存在重复通知。
        List<int> actual = new List<int>();
        // 请求结果只应发回一次，失败时也必须送到客户端。
        int replies = 0;
        for (int i = 0; i < all.Count; i++)
        {
            // 回环工具已用正式帧头、CRC 和 Protobuf 解析原包。
            DecodedFrame frame = all[i];
            if (frame.ProtoCode == NetDefine.RpcC2M_SendChatMsg)
            {
                replies++;
                if (msg.Medium == ChatMediumType.Audio) CheckGateFrame(frame.Package);
                // 回执本身没有正文，不触发本地回显。
                SendChatMsgRes res = SendChatMsgRes.Parser.ParseFrom(frame.Package.Data);
                Expect(frame.UnitySessionId == sender && (res.Result == CmdCode.Succeed) == ok, "请求结果及回执收件人");
                if (!ok) Expect(res.Reason.Length > 0, "拒绝发言返回具体原因");
            }
            if (frame.ProtoCode != NetDefine.PtcT2C_ChattingMsgNtf) continue;
            actual.Add(frame.UnitySessionId);
            if (msg.Medium == ChatMediumType.Audio && actual.Count == 1) CheckGateFrame(frame.Package);
            // 真实的发言人和服务器时间必须取代客户端自报值。
            ChatMsg notice = ChatMsg.Parser.ParseFrom(frame.Package.Data);
            Expect(notice.MsgHead.SenderUid == (ulong)(5950 + sender) && notice.SenderName != "伪造昵称" && notice.MsgHead.MsgTime > 1, "权威作者及时间");
            Expect(notice.MsgHead.MsgType == msg.MsgHead.MsgType && notice.Medium == msg.Medium, "媒介与频道");
            if (msg.Medium == ChatMediumType.Text) Expect(notice.MsgContent == msg.MsgContent.Trim(), "保留文字正文");
            else Expect(notice.Audio.AudioId == msg.Audio.AudioId && notice.Audio.Duration == 3f && notice.Audio.Text == "", "录音 ID、实际三秒时长及空转写");
        }
        actual.Sort();
        Expect(replies == 1 && string.Join(",", actual) == string.Join(",", targets), "回执次数与完整收件范围 " + msg.MsgHead.MsgType);
    }

    /// <summary>通过正式网关回包校验和真实客户端套接字确认错误与成功均保留原协议。</summary>
    /// <param name="packet">从 GS 发回的实际业务帧。</param>
    private static void CheckGateFrame(BasePackage packet)
    {
        // 该回环端点代表一个客户端连接，与上游 GS 连接独立。
        LoopbackLink client = new LoopbackLink(false);
        try
        {
            GateClientResponseForwarder.ForwardToUnity(SessionMgr.Instance.GetSession(client.SessionId), packet);
            // 网关转发时不应丢失媒体数据、认证失败原因或请求的文件 ID。
            List<DecodedFrame> frames = client.WaitWindow(220);
            Expect(frames.Count == 1 && frames[0].ProtoCode == packet.ProtoCode && frames[0].Package.Data.Equals(packet.Data), "真实网关套接字保留聊天协议和载荷");
        }
        finally { client.Close(); }
    }

    /// <summary>等待指定媒体回包，可区分分块请求与抄送。</summary><param name="session">玩家会话号。</param>
    /// <param name="code">媒体 RPC 编号。</param><param name="arg">对应请求。</param>
    private static ByteString Media(int session, ushort code, IMessage arg)
    {
        _gate.SendFromGate(session, code, arg);
        // 独立服务、GS 主循环和网关均需处理当前块。
        DateTime deadline = DateTime.UtcNow.AddSeconds(5);
        do
        {
            // 每次保留已到达的完整业务帧。
            List<DecodedFrame> all = Collect(130);
            for (int i = 0; i < all.Count; i++)
                if (all[i].UnitySessionId == session && all[i].ProtoCode == code)
                {
                    CheckGateFrame(all[i].Package);
                    return all[i].Package.Data;
                }
        } while (DateTime.UtcNow < deadline);
        throw new InvalidOperationException("未收到媒体回包 " + code);
    }

    /// <summary>发送三秒合成 PCM，覆盖多块、所有权、时长、转写空接口与接收者逐字节还原。</summary>
    private static void CheckAudio()
    {
        // 确定性非零内容，长度大于协议单帧的十六位上限。
        byte[] pcm = new byte[NetDefine.ChatAudioBytesPerSecond * 3];
        for (int i = 0; i < pcm.Length; i++) pcm[i] = (byte)((i * 37 + 23) & 255);
        // 首块之后直接跳偏移必须失败。
        UploadChatAudioArg bad = new UploadChatAudioArg { Offset = 17, Chunk = ByteString.CopyFrom(pcm, 0, 2), Complete = true };
        Expect(UploadChatAudioRes.Parser.ParseFrom(Media(52, NetDefine.RpcC2M_UploadChatAudio, bad)).Result != CmdCode.Succeed, "拒绝错误的起始偏移");
        // 尾块之前不发媒体 ID。
        AudioChat uploaded = null;
        for (int offset = 0; offset < pcm.Length; offset += NetDefine.ChatAudioChunkBytes)
        {
            // 与客户端使用完全相同的分块尺寸。
            int count = Math.Min(NetDefine.ChatAudioChunkBytes, pcm.Length - offset);
            // 当前偏移和尾片标志均通过真实线路传给 MS。
            UploadChatAudioArg arg = new UploadChatAudioArg { Offset = offset, Complete = offset + count == pcm.Length, Chunk = ByteString.CopyFrom(pcm, offset, count) };
            // 从正式回包获得进度或最终编号。
            UploadChatAudioRes res = UploadChatAudioRes.Parser.ParseFrom(Media(52, NetDefine.RpcC2M_UploadChatAudio, arg));
            Expect(res.Result == CmdCode.Succeed && res.NextOffset == offset + count, "上传连续确认");
            if (!arg.Complete) Expect(res.Audio == null, "中间块不生成 ID");
            else uploaded = res.Audio;
        }
        Expect(uploaded != null && uploaded.AudioId.Length == 32, "服务端生成文件 ID");
        // 故意谎报时长，广播必须恢复文件真实长度。
        ChatMsg voice = Text(ChatMsgType.ChatTypeWorld, "");
        voice.Medium = ChatMediumType.Audio; voice.Audio = new AudioChat { AudioId = uploaded.AudioId, Duration = 99 };
        CheckSend(51, voice, false);
        CheckSend(52, voice, true, 51, 52, 53, 54);
        voice.MsgHead.MsgType = ChatMsgType.ChatTypeCurscene;
        CheckSend(52, voice, true, 51, 52);
        voice.MsgHead.MsgType = ChatMsgType.ChatTypeTeam;
        CheckSend(52, voice, true, 51, 52);
        // 另一名角色在相同消息的 ID 上逐块下载。
        byte[] restored = new byte[pcm.Length];
        for (int offset = 0; offset < restored.Length;)
        {
            // 来自第二个真实会话的下一块。
            DownloadChatAudioRes chunk = DownloadChatAudioRes.Parser.ParseFrom(Media(53, NetDefine.RpcC2M_DownloadChatAudio,
                new DownloadChatAudioArg { AudioId = uploaded.AudioId, Offset = offset }));
            Expect(chunk.Result == CmdCode.Succeed && chunk.TotalBytes == pcm.Length && chunk.Offset == offset, "下载偏移、总长");
            Expect(chunk.Chunk.Length > 0 && chunk.AudioId == uploaded.AudioId, "同一文件的非空分块");
            chunk.Chunk.CopyTo(restored, offset); offset += chunk.Chunk.Length;
        }
        Expect(ByteString.CopyFrom(restored).Equals(ByteString.CopyFrom(pcm)), "跨会话还原 96000 字节 PCM");
        Expect(DownloadChatAudioRes.Parser.ParseFrom(Media(53, NetDefine.RpcC2M_DownloadChatAudio,
            new DownloadChatAudioArg { AudioId = "../无效" })).Result != CmdCode.Succeed, "拒绝无效文件 ID");
    }

    /// <summary>记录断言次数，失败时把具体检查交给回归驱动输出。</summary><param name="pass">实际是否符合预期。</param><param name="what">检查内容。</param>
    private static void Expect(bool pass, string what)
    {
        _checks++;
        if (!pass) throw new InvalidOperationException("聊天断言失败：" + what);
    }
}
