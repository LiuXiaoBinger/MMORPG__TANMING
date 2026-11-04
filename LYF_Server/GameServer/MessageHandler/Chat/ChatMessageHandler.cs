using System.Collections.Generic;
using Google.Protobuf;

/// <summary>按现有 GS 调度框架处理聊天命令，提供会话身份和场景在线玩家，频道规则由 MS 决定。</summary>
public sealed class ChatMessageHandler : IGameMessageHandler
{
    /// <summary>注册客户端发言及队伍服回执、频道通知。</summary>
    /// <param name="server">接受 Gate 的监听端。</param><param name="messages">游戏逻辑线程分发器。</param>
    public static void Register(NetServer server, ClientMessageManager messages)
    {
        // 同一个无状态接入器按来源区分请求与回包。
        ChatMessageHandler handler = new ChatMessageHandler();
        // 文字、语音内容发布和实际文件传输都必须经过已登录的网关会话。
        ushort[] requests = { NetDefine.RpcC2M_SendChatMsg, NetDefine.RpcC2M_UploadChatAudio, NetDefine.RpcC2M_DownloadChatAudio };
        for (int i = 0; i < requests.Length; i++)
        {
            server.RegistCommand(requests[i], messages);
            messages.Register(new GameMessageHandlerKey(GameCommandSource.GateServer, requests[i]), handler, CenterMessageRoute.Internal);
        }
        // 内网回包在现有 Master 连接上接收。
        ushort[] codes = { NetDefine.RpcC2M_SendChatMsg, NetDefine.PtcM2T_ChatBroadCastM2T, NetDefine.PtcM2G_ChatBroadCastM2Gs,
            NetDefine.RpcC2M_UploadChatAudio, NetDefine.RpcC2M_DownloadChatAudio };
        for (int i = 0; i < codes.Length; i++)
        {
            messages.Register(new GameMessageHandlerKey(GameCommandSource.MasterServer, codes[i]), handler, CenterMessageRoute.Internal);
        }
    }

    /// <summary>区分原始准入请求和 MS 批准的通知。</summary>
    /// <param name="source">服务间来源。</param><param name="code">协议编号。</param><returns>本处理器是否接管。</returns>
    public bool CanHandle(GameCommandSource source, ushort code)
    {
        // 三个客户端 RPC 的请求和回执使用相同协议号，由真实来源区分。
        bool rpc = code == NetDefine.RpcC2M_SendChatMsg || code == NetDefine.RpcC2M_UploadChatAudio || code == NetDefine.RpcC2M_DownloadChatAudio;
        if (source == GameCommandSource.GateServer) return rpc;
        if (source != GameCommandSource.MasterServer) return false;
        return rpc || code == NetDefine.PtcM2T_ChatBroadCastM2T || code == NetDefine.PtcM2G_ChatBroadCastM2Gs;
    }

    /// <summary>按受信连接取得身份或投递消息。</summary><param name="context">保留真实会话来源的已注册消息。</param>
    public void OnCall(GameMessageHandlerContext context)
    {
        // 回执沿真实的网关和客户端会话返回。
        BasePackage packet = context.Package;
        if (context.Source == GameCommandSource.GateServer)
        {
            // 发言人由网关连接和玩家会话确认。
            OnlineRole role = RoleManager.Instance.GetOnlineRoleBySession(packet.GateSessionId, packet.UnitySessionId);
            if (role == null || role.State != OnlineRoleState.Online) return;
            if (packet.ProtoCode != NetDefine.RpcC2M_SendChatMsg)
            {
                // 只改变内网寻址，媒体块不进入游戏逻辑或频道广播。
                MasterTeamRolePacket media = new MasterTeamRolePacket { RoleId = role.GetID(), Packet = packet };
                if (!MasterTeamLink.Instance.SendToMaster(new BasePackage { ProtoCode = NetDefine.PtcG2M_ChatAudioTransfer, Data = media.ToByteString() }))
                {
                    if (packet.ProtoCode == NetDefine.RpcC2M_UploadChatAudio)
                        role.SendToClient(packet.ProtoCode, new UploadChatAudioRes { Result = CmdCode.ServerError }.ToByteString());
                    else
                    {
                        // 保留本次请求的文件与偏移，客户端才能在对应气泡提示连接失败。
                        DownloadChatAudioArg download = DownloadChatAudioArg.Parser.ParseFrom(packet.Data);
                        role.SendToClient(packet.ProtoCode, new DownloadChatAudioRes { Result = CmdCode.ServerError,
                            AudioId = download.AudioId, Offset = download.Offset }.ToByteString());
                    }
                }
                return;
            }
            // 丢弃客户端自报角色和昵称，服务端记录为准。
            SendChatMsgArg arg = SendChatMsgArg.Parser.ParseFrom(packet.Data);
            if (arg.Msg != null && arg.Msg.MsgHead != null)
            {
                arg.Msg.MsgHead.SenderUid = role.GetID();
                arg.Msg.SenderName = role.mainRoleInfo.BaseInfo.Nickname;
            }
            packet.Data = arg.ToByteString();
            if (!MasterTeamLink.Instance.SendToMaster(packet))
            {
                role.SendToClient(NetDefine.RpcC2M_SendChatMsg,
                    new SendChatMsgRes { Result = CmdCode.ServerError, Reason = "聊天服务未连接" }.ToByteString());
            }
            return;
        }
        if (packet.ProtoCode != NetDefine.PtcM2T_ChatBroadCastM2T && packet.ProtoCode != NetDefine.PtcM2G_ChatBroadCastM2Gs)
        {
            // 发送结果与正文广播是不同的客户端入口。
            OnlineRole source = RoleManager.Instance.GetOnlineRoleBySession(packet.GateSessionId, packet.UnitySessionId);
            if (source != null) source.SendToClient(packet.ProtoCode, packet.Data);
            return;
        }
        Broadcast(ChatMsgWithLists.Parser.ParseFrom(packet.Data));
    }

    /// <summary>队伍按 MS 成员投递，世界按在线分区投递，附近使用同一地图及副本实例。</summary>
    /// <param name="notice">权威消息和可选的目标角色号列表。</param>
    private static void Broadcast(ChatMsgWithLists notice)
    {
        // 同一频段广播时只序列化一次。
        ByteString payload = notice.ChatMsg.ToByteString();
        // MS 批准的真实频道。
        ChatMsgType channel = notice.ChatMsg.MsgHead.MsgType;
        if (channel == ChatMsgType.ChatTypeTeam)
        {
            for (int i = 0; i < notice.ReceiverList.Count; i++)
            {
                // MS 已选定的成员可能刚好离线。
                OnlineRole member = RoleManager.Instance.GetOnlineRole((int)notice.ReceiverList[i]);
                if (member != null && member.State == OnlineRoleState.Online) member.SendToClient(NetDefine.PtcT2C_ChattingMsgNtf, payload);
            }
            return;
        }
        // 附近频道由场景持有者筛选，世界频道无需发送者仍留在原场景。
        OnlineRole sender = RoleManager.Instance.GetOnlineRole((int)notice.ChatMsg.MsgHead.SenderUid);
        if (channel == ChatMsgType.ChatTypeCurscene && sender == null) return;
        // 在线索引快照只复制引用，不复制角色实体。
        List<OnlineRole> online = new List<OnlineRole>(RoleManager.Instance.GetOnlineRoleMap().Values);
        for (int i = 0; i < online.Count; i++)
        {
            // 只向当前已登录的角色会话发通知。
            OnlineRole target = online[i];
            if (target.State != OnlineRoleState.Online) continue;
            if (channel == ChatMsgType.ChatTypeCurscene)
            {
                if (target.mainRoleInfo.BaseInfo.MapId != sender.mainRoleInfo.BaseInfo.MapId) continue;
                if (target.mainRoleInfo.BaseInfo.InstanceId != sender.mainRoleInfo.BaseInfo.InstanceId) continue;
            }
            target.SendToClient(NetDefine.PtcT2C_ChattingMsgNtf, payload);
        }
    }
}
