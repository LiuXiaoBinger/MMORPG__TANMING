using Google.Protobuf;

/// <summary>把 RO 聊天室公开操作送到本区持久化的 ChatRoomManager，并通过已有角色宿主定向投递。监听框架要求实现 IContainer。</summary>
internal sealed class ChatRoomService : IContainer
{
    /// <summary>本区成员和群消息存取链路。</summary>
    private readonly NetClient center;
    /// <summary>既有跨 GS 投递和语音归属验证。</summary>
    private readonly MasterTeamService teams;
    /// <summary>本逻辑区的资料边界。</summary>
    private readonly int realmId;
    /// <summary>按部署连接好友群所在的中心库。</summary><param name="realm">本区服务部署。</param><param name="teams">当前区服社交路由。</param>
    internal ChatRoomService(RealmTopology realm, MasterTeamService teams)
    {
        this.teams = teams;
        // 单节点使用与现有社交服务同一默认连接。
        string host = NetDefine.IPHost; int port = NetDefine.CenterServerPort; realmId = 1;
        if (realm != null) { host = realm.CenterHost; port = realm.CenterPort; realmId = realm.RealmId; }
        center = new NetClient(host, port, ClientType.MasterServer);
        center.RegistCommand(ChatRoomCode.Operation, this); center.StartConnect();
    }
    /// <summary>无需在 MS 初始化持久群状态。</summary>
    public void OnInit() { }
    /// <summary>只从 GS 查询到的角色号推导操作身份。</summary><param name="link">来源 GS。</param><param name="packet">真实角色及客户端公开协议。</param>
    public void OnServerCommand(BaseLink link, BasePackage packet)
    {
        // RoleId 只取从 GS 会话查出的值。
        MasterTeamRolePacket request = MasterTeamRolePacket.Parser.ParseFrom(packet.Data);
        // GS 包内的群操作原文。
        BasePackage body = request.Packet;
        // 传往中心库的操作人、逻辑区和回包地址。
        var call = new ChatRoomCall { GameLink = ((Session)link).SessionID, RoleId = (int)request.RoleId, RealmId = realmId, Command = body.ProtoCode };
        switch (call.Command)
        {
            case ChatRoomCode.GetChatRoomInfo: ChatRoomInfoArg.Parser.ParseFrom(body.Data); break;
            case ChatRoomCode.CreateChatRoom: call.Setting = CreateChatRoomArg.Parser.ParseFrom(body.Data).Setting; break;
            case ChatRoomCode.InviteRoomMember:
                // 正式受邀角色取自好友选择，中心库还会确认关系。
                InviteRoomMemberArg invite = InviteRoomMemberArg.Parser.ParseFrom(body.Data);
                call.RoomUid = invite.RoomUid; call.Target = invite.RoleUid; break;
            case ChatRoomCode.ApplyJoinRoom: call.RoomUid = ApplyJoinRoomArg.Parser.ParseFrom(body.Data).RoomUid; break;
            case ChatRoomCode.LeaveRoom: call.RoomUid = LeaveRoomArg.Parser.ParseFrom(body.Data).RoomUid; break;
            case ChatRoomCode.DissolveRoom: call.RoomUid = DissolveRoomArg.Parser.ParseFrom(body.Data).RoomUid; break;
            case ChatRoomCode.PullChat:
                // 零边界表示当前最新页，非零读取更早的一页。
                ChatRoomInfoArg pull = ChatRoomInfoArg.Parser.ParseFrom(body.Data);
                call.RoomUid = pull.RoomUid; call.BeforeSquence = pull.BeforeSquence; break;
            case ChatRoomCode.SendChatMsg:
                // 新建公开正文，撇开发件人可能填入的昵称与时间。
                RoomChatMsg msg = RoomChatMsg.Parser.ParseFrom(body.Data);
                call.RoomUid = msg.RoomUid;
                call.Msg = new RoomChatMsg { Content = msg.Content.Trim(), MediumType = msg.MediumType };
                if (msg.MediumType == ChatMediumType.Audio)
                {
                    call.Msg.Content = ""; call.Msg.Audio = msg.Audio;
                    if (msg.Audio == null || FriendCode.TextLength(msg.Audio.Text) > 70 || !teams.PreparePrivateAudio(call.GameLink, request.RoleId, msg.Audio))
                        call.Error = "未找到本角色的有效录音";
                }
                else if (msg.MediumType != ChatMediumType.Text || call.Msg.Content.Length == 0 || FriendCode.TextLength(call.Msg.Content) > ChatRoomCode.RoomChatMaxNum)
                    call.Error = "群消息为空或超过 60 个计数单位";
                break;
            default: return;
        }
        if (call.Error.Length == 0 && !center.Send(packet, ChatRoomCode.Operation, call.ToByteString())) call.Error = "群聊存储暂不可用";
        if (call.Error.Length > 0) ReplyError(call);
    }
    /// <summary>只向已入库的真实成员投递，未接受邀请的角色不参与消息分发。</summary><param name="link">本区角色库连接。</param><param name="packet">权威存储和成员清单。</param>
    public void OnClientCommand(BaseLink link, BasePackage packet)
    {
        // 群关系事务提交完成后才下发变更。
        ChatRoomCall call = ChatRoomCall.Parser.ParseFrom(packet.Data);
        if (call.Error.Length > 0) { ReplyError(call); return; }
        // 本次已完成群操作的真实角色。
        ulong actor = (ulong)call.RoleId;
        if (call.List != null) { teams.SendToContact(actor, call.GameLink, ChatRoomCode.GetChatRoomInfo, call.List.ToByteString()); return; }
        if (call.History != null) { teams.SendToContact(actor, call.GameLink, ChatRoomCode.PullChat, call.History.ToByteString()); return; }
        // 遍历中心库在同一事务中取得的接收者。
        for (int i = 0; i < call.NotifyRoles.Count; i++)
        {
            // 待邀请成员只会出现在关系通知的接收清单内。
            ulong target = (ulong)call.NotifyRoles[i];
            if (call.Command == ChatRoomCode.SendChatMsg) teams.SendToContact(target, call.GameLink, ChatRoomCode.RoomChatNtf, call.Msg.ToByteString());
            else
            {
                // 群关系通知不携带消息正文。
                var change = new RoomChangeNtf { RoomUid = call.RoomUid };
                if (target == actor) change.Command = call.Command;
                teams.SendToContact(target, call.GameLink, ChatRoomCode.RoomInfoNtf, change.ToByteString());
            }
        }
    }
    /// <summary>仅向发起角色返回拒绝原因。</summary><param name="call">含角色和本次业务错误的回执。</param>
    private void ReplyError(ChatRoomCall call)
    { teams.SendToContact((ulong)call.RoleId, call.GameLink, ChatRoomCode.Error, new FriendError { Error = call.Error }.ToByteString()); }
}
