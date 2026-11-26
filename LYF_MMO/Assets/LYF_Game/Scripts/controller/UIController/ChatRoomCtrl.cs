using System;
using System.Collections.Generic;

/// <summary>对应 RO ChatRoomCtrl，驱动长期群的成员操作、点开补读、向前翻页与三种消息展示。</summary>
public sealed class ChatRoomCtrl
{
    /// <summary>本角色的群协议管理器。</summary>
    private readonly ChatRoomMgr mgr = new ChatRoomMgr();
    /// <summary>主城功能层注册的群聊窗口。</summary>
    private readonly ChatRoomPanel panel;
    /// <summary>提供同一份真实好友名单及功能入口红点。</summary>
    private readonly FriendsHandler friends;
    /// <summary>与好友、公聊共享的正式录音播放器。</summary>
    private readonly ChatAudioMgr audio;
    /// <summary>当前打开群的已加载记录，按群序号排列。</summary>
    private readonly List<RoomChatMsg> messages = new List<RoomChatMsg>();
    /// <summary>当前主角编号，用来区分本人气泡。</summary>
    private int role;
    /// <summary>玩家主动选择的群号。</summary>
    private ulong selected;
    /// <summary>本次录音开始时明确选择的群号。</summary>
    private ulong recordingRoom;
    /// <summary>建群时当前选中的 RO 容量档位。</summary>
    private int capacity = 5;
    /// <summary>目录更新后要打开的刚刚创建或接受的群。</summary>
    private ulong newlyJoined;
    /// <summary>绑定主城窗口、联系人和群语音。</summary><param name="panel">群聊子窗。</param><param name="friends">好友和红点控制器。</param><param name="audio">主城现有语音组件。</param>
    internal ChatRoomCtrl(ChatRoomPanel panel, FriendsHandler friends, ChatAudioMgr audio)
    {
        this.panel = panel; this.friends = friends; this.audio = audio;
        mgr.Changed += Refresh; mgr.RoomChange += OnChange; mgr.Chat += OnChat; mgr.History += OnHistory; mgr.Failed += ShowError;
        panel.Opened += Open; panel.create.onClick.AddListener(Create); panel.capacity.onClick.AddListener(CycleCapacity);
        panel.invite.onClick.AddListener(Invite); panel.accept.onClick.AddListener(Join); panel.decline.onClick.AddListener(Leave);
        panel.leave.onClick.AddListener(Leave); panel.dissolve.onClick.AddListener(AskDissolve); panel.confirm.onClick.AddListener(Dissolve);
        panel.send.onClick.AddListener(SendText); panel.input.onSubmit.AddListener(SubmitText); panel.older.onClick.AddListener(Older); panel.emojis.Selected += SendEmoji;
        if (audio != null) { panel.record.RecordStarted += StartRecord; panel.record.RecordEnded += audio.FinishRecord; audio.Ready += OnAudio; }
    }
    /// <summary>结束主城和网络订阅。</summary>
    internal void Dispose()
    {
        mgr.Dispose(); panel.Opened -= Open; panel.create.onClick.RemoveListener(Create); panel.capacity.onClick.RemoveListener(CycleCapacity);
        panel.invite.onClick.RemoveListener(Invite); panel.accept.onClick.RemoveListener(Join); panel.decline.onClick.RemoveListener(Leave);
        panel.leave.onClick.RemoveListener(Leave); panel.dissolve.onClick.RemoveListener(AskDissolve); panel.confirm.onClick.RemoveListener(Dissolve);
        panel.send.onClick.RemoveListener(SendText); panel.input.onSubmit.RemoveListener(SubmitText); panel.older.onClick.RemoveListener(Older); panel.emojis.Selected -= SendEmoji;
        if (audio != null) { panel.record.RecordStarted -= StartRecord; panel.record.RecordEnded -= audio.FinishRecord; audio.Ready -= OnAudio; }
    }
    /// <summary>主角切换后清除群窗口和上一角色的录音目标。</summary><param name="id">新主角，零表示离场。</param>
    internal void BindRole(int id)
    {
        if (role == id) return;
        role = id; selected = 0; recordingRoom = 0; newlyJoined = 0; messages.Clear(); mgr.Rooms.Rooms.Clear();
        panel.groupName.text = ""; panel.input.text = ""; panel.status.text = ""; Refresh();
        if (id > 0) mgr.RequestGetChatRoomInfo();
    }
    /// <summary>重新打开时恢复目录和所选群的最新消息。</summary>
    private void Open()
    {
        panel.status.text = ""; Refresh();
        if (role == 0) return;
        mgr.RequestGetChatRoomInfo();
        if (Selected() != null) Select(selected);
    }
    /// <summary>查找当前群的权威成员关系。</summary>
    private ChatRoomInfo Selected()
    {
        // 按群编号遍历本人目录。
        for (int i = 0; i < mgr.Rooms.Rooms.Count; i++) if (mgr.Rooms.Rooms[i].RoomUid == selected) return mgr.Rooms.Rooms[i];
        return null;
    }
    /// <summary>计算全部群未读或待接受邀请，并刷新当前角色可以使用的动作。</summary>
    private void Refresh()
    {
        // 每个邀请均作为一个待处理群入口提示。
        bool hint = false;
        // 按本人已加入或受邀的群检查提示。
        for (int i = 0; i < mgr.Rooms.Rooms.Count; i++)
        {
            // 当前正在检查未读和邀请状态的群。
            ChatRoomInfo entry = mgr.Rooms.Rooms[i];
            if (entry.Invited || entry.UnreadCount > 0) { hint = true; break; }
        }
        friends.SetRoomHint(hint);
        if (!panel.isActiveAndEnabled) return;
        if (newlyJoined != 0)
        {
            // 建群或接受成功后自动进入该群。
            ulong id = newlyJoined; newlyJoined = 0; Select(id); return;
        }
        // 当前可见群及本人的成员资格。
        ChatRoomInfo room = Selected();
        if (room == null) { selected = 0; panel.confirmPanel.SetActive(false); panel.invitePanel.SetActive(false); }
        panel.ShowRooms(mgr.Rooms, selected, Select);
        panel.conversation.SetActive(room != null && !room.Invited);
        panel.invitation.SetActive(room != null && room.Invited);
        panel.empty.SetActive(room == null);
        if (room == null) { panel.title.text = ""; panel.members.text = ""; return; }
        panel.title.text = room.RoomSetting.RoomName;
        if (room.Invited) { panel.members.text = "你收到了这个群的邀请"; return; }
        // 只显示服务端返回的群成员昵称。
        var names = new List<string>();
        // 按群成员顺序汇总昵称。
        for (int i = 0; i < room.RoomMember.Info.Count; i++) names.Add(room.RoomMember.Info[i].Name);
        panel.members.text = names.Count + " / " + room.RoomSetting.RoomCapacity + " 人   " + string.Join("、", names);
        panel.dissolve.gameObject.SetActive(room.RoomCaptainUid == (ulong)role);
    }
    /// <summary>手动或建群后选择目标，此时才将该群最新一页记为已读。</summary><param name="uid">真实群编号。</param>
    private void Select(ulong uid)
    {
        selected = uid; messages.Clear(); panel.status.text = ""; panel.input.text = "";
        panel.invitePanel.SetActive(false); panel.confirmPanel.SetActive(false); panel.older.gameObject.SetActive(false);
        Refresh(); panel.ShowMessages(messages, role, Play, true, false);
        // 切换后从目录重新确认可读资格。
        ChatRoomInfo room = Selected(); if (room != null && !room.Invited) mgr.RequestPullChat(uid, 0);
    }
    /// <summary>按 RO 的人数表循环切换。</summary>
    private void CycleCapacity() { capacity += 5; if (capacity > 20) capacity = 5; panel.capacityLabel.text = capacity + " 人"; }
    /// <summary>以当前输入的名字创建长期群。</summary>
    private void Create() { panel.status.text = ""; mgr.RequestCreatChatRoom(panel.groupName.text.Trim(), capacity); }
    /// <summary>列出尚未加入该群的好友。</summary>
    private void Invite()
    {
        // 只有已选群才需要生成邀请名单。
        ChatRoomInfo room = Selected();
        if (room == null) return;
        // 先复制本人的好友，再排除已经在群的角色。
        var available = new List<GiftFriendInfo>(friends.Contacts);
        // 筛掉已是群成员的好友。
        for (int i = 0; i < room.RoomMember.Info.Count; i++)
        {
            // 本轮排除的群成员角色号。
            int member = room.RoomMember.Info[i].RoleId;
            available.RemoveAll(contact => contact.RoleId == member);
        }
        panel.ShowInvites(available, uid => { panel.invitePanel.SetActive(false); mgr.RequestInviteRoomMember(selected, uid); });
    }
    /// <summary>主动接受所选群邀请。</summary>
    private void Join() { mgr.RequestApplyJoinRoom(selected); }
    /// <summary>退出所选群或拒绝邀请。</summary>
    private void Leave() { mgr.RequestLeaveRoom(selected); }
    /// <summary>展示群主所选的解散对象和后果。</summary>
    private void AskDissolve() { panel.confirmText.text = "解散「" + panel.title.text + "」并删除群消息？"; panel.confirmPanel.SetActive(true); }
    /// <summary>群主确认后请求服务端解散。</summary>
    private void Dissolve() { panel.confirmPanel.SetActive(false); mgr.RequestDissolveRoom(selected); }
    /// <summary>加载当前第一条消息之前的一页。</summary>
    private void Older() { if (messages.Count > 0) mgr.RequestPullChat(selected, messages[0].Squence); }
    /// <summary>按回车或移动键盘的发送键提交，与发送按钮走同一入口。</summary><param name="draft">UGUI 刚提交的草稿，内容由输入框持有。</param>
    private void SubmitText(string draft) { SendText(); }
    /// <summary>发送当前群草稿，超限时保留输入。</summary>
    private void SendText()
    {
        // 去除边缘空白后的群聊草稿。
        string content = panel.input.text.Trim();
        if (content.Length == 0) return;
        if (FriendCode.TextLength(content) > ChatRoomCode.RoomChatMaxNum) { ShowError("群消息超过 60 个计数单位"); return; }
        if (mgr.RequestSendChatMsg(new RoomChatMsg { RoomUid = selected, Content = content, MediumType = ChatMediumType.Text })) panel.input.text = "";
    }
    /// <summary>RO 表情以原始标记存储，再由每端的原图显示。</summary><param name="marker">选择的基础包编号。</param>
    private void SendEmoji(string marker) { mgr.RequestSendChatMsg(new RoomChatMsg { RoomUid = selected, Content = marker, MediumType = ChatMediumType.Text }); }
    /// <summary>将录音固定于按下时的群。</summary>
    private bool StartRecord()
    {
        if (selected == 0 || !audio.StartRecord(ChatChannel.ChatRoom)) return false;
        recordingRoom = selected; return true;
    }
    /// <summary>只处理从群聊按钮录制的媒体。</summary><param name="channel">录音起始频道。</param><param name="clip">真实上传的音频。</param>
    private void OnAudio(ChatChannel channel, AudioChat clip)
    {
        if (channel != ChatChannel.ChatRoom || recordingRoom == 0) return;
        mgr.RequestSendChatMsg(new RoomChatMsg { RoomUid = recordingRoom, Audio = clip, MediumType = ChatMediumType.Audio }); recordingRoom = 0;
    }
    /// <summary>按群记录中的音频编号下载播放。</summary><param name="clip">该条录音。</param>
    private void Play(AudioChat clip) { if (audio != null) audio.PlayRecordedFile(clip); }
    /// <summary>按实际变动种类更新目录选中项与邀请反馈。</summary><param name="change">发生变更的群及操作。</param>
    private void OnChange(RoomChangeNtf change)
    {
        if (change.Command == ChatRoomCode.CreateChatRoom)
        {
            panel.createDialog.SetActive(false);
            panel.groupName.text = "";
        }
        if (change.Command == ChatRoomCode.CreateChatRoom || change.Command == ChatRoomCode.ApplyJoinRoom) newlyJoined = change.RoomUid;
        if (change.Command == ChatRoomCode.InviteRoomMember) panel.status.text = "已发送群邀请";
    }
    /// <summary>只补读屏幕上已打开的群，后台群单独刷新其未读数。</summary><param name="msg">实时群消息。</param>
    private void OnChat(RoomChatMsg msg)
    {
        if (panel.isActiveAndEnabled && msg.RoomUid == selected) mgr.RequestPullChat(selected, 0);
        else mgr.RequestGetChatRoomInfo();
    }
    /// <summary>按消息序号合并重叠页；先前打开的其他群回包不会改动当前窗口。</summary><param name="page">已按序排列的群历史。</param>
    private void OnHistory(PullRoomChatRes page)
    {
        if (page.RoomUid != selected || !panel.isActiveAndEnabled) return;
        // 接到头部时显示更早入口；更新尾页时保留已读到的较早页。
        bool first = messages.Count == 0;
        if (first || page.BeforeSquence > 0) panel.older.gameObject.SetActive(page.HasOlder);
        // 按该页权威序号合并消息。
        for (int i = 0; i < page.Messages.Count; i++)
        {
            // 本批次待合并的一条正式群消息。
            RoomChatMsg row = page.Messages[i];
            // 列表中不早于这条消息的第一个位置。
            int pos = messages.FindIndex(item => item.Squence >= row.Squence);
            if (pos < 0) messages.Add(row);
            else if (messages[pos].Squence != row.Squence) messages.Insert(pos, row);
        }
        panel.ShowMessages(messages, role, Play, first, page.BeforeSquence > 0);
        if (page.BeforeSquence == 0) mgr.RequestGetChatRoomInfo();
    }
    /// <summary>返回操作失败原因。</summary><param name="text">面向玩家的提示。</param>
    private void ShowError(string text)
    {
        panel.status.text = text;
        if (panel.createDialog.activeSelf) panel.createStatus.text = text;
    }
}
