using Google.Protobuf;

/// <summary>对应 RO RoleFriend，处理私聊、好友关系和定向在线通知。服务端网络注册要求实现 IContainer。</summary>
internal sealed class RoleFriend : IContainer
{
    /// <summary>本逻辑区的角色库入口。</summary>
    private readonly NetClient center;
    /// <summary>复用现有语音上传记录与在线角色的 GS 路由。</summary>
    private readonly MasterTeamService teams;
    /// <summary>供中心服限定好友角色资料的区号。</summary>
    private readonly int realmId;

    /// <summary>建立到本区中心服的好友存取连接。</summary><param name="realm">当前游戏区配置，单机使用默认端口。</param><param name="teams">当前区服的社交服务。</param>
    internal RoleFriend(RealmTopology realm, MasterTeamService teams)
    {
        this.teams = teams;
        // 单机测试沿用角色库的默认地址。
        string host = NetDefine.IPHost;
        // 按当前区服部署连接角色库。
        int port = NetDefine.CenterServerPort;
        realmId = 1;
        if (realm != null) { host = realm.CenterHost; port = realm.CenterPort; realmId = realm.RealmId; }
        center = new NetClient(host, port, ClientType.MasterServer);
        center.RegistCommand(FriendCode.Operation, this);
        center.StartConnect();
    }

    /// <summary>好友按角色操作时从库中获取。</summary>
    public void OnInit() { }

    /// <summary>将 GS 查出的发起角色和客户端提交的目标合并成内部调用。</summary><param name="link">来源 GS。</param><param name="packet">带可信角色号的原始包。</param>
    public void OnServerCommand(BaseLink link, BasePackage packet)
    {
        // GS 通过玩家会话验证过的角色和公开协议。
        MasterTeamRolePacket request = MasterTeamRolePacket.Parser.ParseFrom(packet.Data);
        // 发起人的好友操作及序列化参数。
        BasePackage body = request.Packet;
        // 附有角色身份和来源 GS 的可信存取调用。
        var call = new FriendCall { Command = body.ProtoCode, RealmId = realmId, RoleId = (int)request.RoleId, GameLink = ((Session)link).SessionID };
        switch (call.Command)
        {
            case FriendCode.GetFriendInfo: GetFriendInfoArg.Parser.ParseFrom(body.Data); break;
            case FriendCode.AddFriend:
                // 输入昵称是主动添加，指定 UID 是好友列表回加。
                AddFriendArg add = AddFriendArg.Parser.ParseFrom(body.Data);
                call.Peer = add.Uid; call.Name = add.Name.Trim(); break;
            case FriendCode.DeleteFriend: call.Peer = DeleteFriendArg.Parser.ParseFrom(body.Data).Uid; break;
            case FriendCode.ReadPrivateMessage:
                // squence 是角色已确认写入本地历史的最后一条。
                ReadPrivateMessageArg read = ReadPrivateMessageArg.Parser.ParseFrom(body.Data);
                call.Peer = read.Uid; call.Squence = read.Squence; break;
            case FriendCode.SendPrivateChatMsg:
                call.Chat = PrivateChatInfo.Parser.ParseFrom(body.Data);
                call.Peer = call.Chat.Uid;
                OnPrivateChat(call);
                break;
            default: return;
        }
        if (call.Error.Length == 0 && !center.Send(packet, FriendCode.Operation, call.ToByteString())) call.Error = "好友存储暂不可用";
        if (call.Error.Length > 0) ReplyError(call);
    }

    /// <summary>核实文字计数与语音真实上传者，使用 RO 同一套消息媒介编号。</summary><param name="call">发件人、目标和待受理的媒介。</param>
    private void OnPrivateChat(FriendCall call)
    {
        // 实际保存的正文与客户端展示的媒介严格对应。
        PrivateChatInfo info = call.Chat;
        if (info.MediumType == ChatMediumType.Text)
        {
            info.Content = info.Content.Trim(); info.Audio = null;
            if (info.Content.Length == 0 || FriendCode.TextLength(info.Content) > FriendCode.FriendMaxNum) call.Error = "消息为空或超过 100 个计数单位";
            return;
        }
        if (info.MediumType != ChatMediumType.Audio || info.Audio == null) { call.Error = "不支持的聊天内容"; return; }
        info.Content = "";
        if (FriendCode.TextLength(info.Audio.Text) > FriendCode.FriendMaxNum || !teams.PreparePrivateAudio(call.GameLink, (ulong)call.RoleId, info.Audio))
            call.Error = "未找到本角色的有效录音";
    }

    /// <summary>持久化完成才分发正文、未读以及关系变动。</summary><param name="link">角色库内部连接。</param><param name="packet">存储回执。</param>
    public void OnClientCommand(BaseLink link, BasePackage packet)
    {
        // 回执使用与请求相同的角色和连接编号。
        FriendCall call = FriendCall.Parser.ParseFrom(packet.Data);
        if (call.Error.Length > 0) { ReplyError(call); return; }
        // 给发起人和对端各自发送对应其视角的数据。
        ulong owner = (ulong)call.RoleId;
        if (call.Command == FriendCode.SendPrivateChatMsg)
        {
            teams.SendToContact(owner, call.GameLink, FriendCode.PrivateChatNtf, call.Posted.ToByteString());
            OnPassivePrivateChat(call, owner);
        }
        else if (call.Command == FriendCode.ReadPrivateMessage)
        {
            teams.SendToContact(owner, call.GameLink, FriendCode.ReadPrivateMessage, call.History.ToByteString());
            teams.SendToContact(owner, call.GameLink, FriendCode.UnReadMessageCountNtf, call.Unread.ToByteString());
        }
        else
        {
            teams.SendToContact(owner, call.GameLink, FriendCode.GetFriendInfo, call.Info.ToByteString());
            if (call.Peer > 0) teams.SendToContact(call.Peer, call.GameLink, FriendCode.FriendChangedNtf, ByteString.Empty);
        }
    }

    /// <summary>将收件人的对端和左右气泡方向换为发件人，并只通知该收件人。</summary><param name="call">已保存的消息。</param><param name="sender">可信发件人。</param>
    private void OnPassivePrivateChat(FriendCall call, ulong sender)
    {
        // 发件方的包已经序列化，可复用这一行切换为收件方视角。
        call.Posted.ChatUid = sender; call.Posted.WhoType = 2;
        teams.SendToContact(call.Peer, call.GameLink, FriendCode.PrivateChatNtf, call.Posted.ToByteString());
        teams.SendToContact(call.Peer, call.GameLink, FriendCode.UnReadMessageCountNtf, call.Unread.ToByteString());
    }

    /// <summary>仅向实际操作人返回业务失败原因。</summary><param name="call">含失败原因和角色号的请求。</param>
    private void ReplyError(FriendCall call)
    { teams.SendToContact((ulong)call.RoleId, call.GameLink, FriendCode.Error, new FriendError { Error = call.Error }.ToByteString()); }
}
