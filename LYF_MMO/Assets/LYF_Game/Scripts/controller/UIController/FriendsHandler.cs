using System.Collections.Generic;
using UnityEngine;

/// <summary>对应 RO FriendsHandler，管理页签、联系人选择、未读提示，以及打开中的一对一会话。</summary>
public sealed class FriendsHandler
{
    /// <summary>好友服务的请求和推送入口。</summary>
    private readonly FriendMgr mgr = new FriendMgr();
    /// <summary>当前玩家的本地历史和已确认游标。</summary>
    private readonly ChatDataMgr chat = new ChatDataMgr();
    /// <summary>由 MainView 管理的原生好友窗口。</summary>
    private readonly FriendsPanel panel;
    /// <summary>主界面功能菜单“好友”的红点。</summary>
    private readonly GameObject hint;
    /// <summary>公共聊天与好友私聊共享的录放音组件。</summary>
    private readonly ChatAudioMgr audio;
    /// <summary>正在主城操作的玩家编号。</summary>
    private int role;
    /// <summary>当前已打开的对端编号，零表示停留在好友列表。</summary>
    private ulong selected;
    /// <summary>在开始录音时保存目标，即使上传期间切换会话也不改变收件人。</summary>
    private ulong recordingPeer;
    /// <summary>为真时左侧只展示已有会话的联系人。</summary>
    private bool recentOnly;

    /// <summary>把好友窗口、红点和现有语音通道接到同一个管理器。</summary><param name="panel">主城好友窗。</param><param name="hint">入口红点。</param><param name="audio">主城已绑定的录音和播放组件。</param>
    internal FriendsHandler(FriendsPanel panel, GameObject hint, ChatAudioMgr audio)
    {
        this.panel = panel; this.hint = hint; this.audio = audio;
        mgr.Changed += Refresh; mgr.PrivateChat += OnPrivateChat; mgr.History += OnReadPrivateMessage; mgr.Failed += ShowError;
        panel.emojis.Selected += SendEmoji; panel.Opened += Open; panel.friends.onClick.AddListener(OnFriends); panel.recent.onClick.AddListener(OnRecent);
        panel.add.onClick.AddListener(AddFriend); panel.relation.onClick.AddListener(ChangeRelation); panel.send.onClick.AddListener(SendText); panel.input.onSubmit.AddListener(SubmitText);
        if (audio != null)
        {
            panel.record.RecordStarted += StartRecord; panel.record.RecordEnded += audio.FinishRecord; audio.Ready += OnAudio;
        }
    }
    /// <summary>退出主城时移除网络、窗口及录音回调。</summary>
    internal void Dispose()
    {
        mgr.Dispose(); panel.emojis.Selected -= SendEmoji; panel.Opened -= Open; panel.friends.onClick.RemoveListener(OnFriends); panel.recent.onClick.RemoveListener(OnRecent);
        panel.add.onClick.RemoveListener(AddFriend); panel.relation.onClick.RemoveListener(ChangeRelation); panel.send.onClick.RemoveListener(SendText); panel.input.onSubmit.RemoveListener(SubmitText);
        if (audio != null)
        {
            panel.record.RecordStarted -= StartRecord; panel.record.RecordEnded -= audio.FinishRecord; audio.Ready -= OnAudio;
        }
    }
    /// <summary>清除前一角色的窗口内容，并从角色库恢复当前未读数。</summary><param name="id">登录角色编号，离场为零。</param>
    internal void BindRole(int id)
    {
        if (id == role) return;
        role = id; selected = 0; recordingPeer = 0; recentOnly = false;
        chat.BindRole(Global.Instance?.LoginInfo?.GameServer?.ServerId ?? 0, id);
        mgr.ClearData(); panel.input.text = ""; panel.nickname.text = ""; panel.status.text = ""; Refresh();
        if (role > 0) mgr.RequestGetFriendInfo();
    }
    /// <summary>打开好友页时刷新关系，只在已明确选中会话时补读。</summary>
    private void Open()
    {
        panel.status.text = ""; Refresh();
        if (role == 0) return;
        mgr.RequestGetFriendInfo();
        if (selected != 0) SelectFriend(selected);
    }
    /// <summary>显示所有联系人。</summary>
    private void OnFriends() { recentOnly = false; Refresh(); }
    /// <summary>按最近聊天时间过滤并排序。</summary>
    private void OnRecent() { recentOnly = true; Refresh(); }
    /// <summary>返回当前选择对应的有效角色资料。</summary>
    private GiftFriendInfo SelectedFriend()
    { return mgr.Friends.Find(friend => (ulong)friend.RoleId == selected); }
    /// <summary>读取服务端最近时间和红点。</summary><param name="uid">被查询的联系人。</param>
    private UnReadMessageCountInfo Summary(int uid)
    {
        // 找到这个角色的服务端摘要。
        for (int i = 0; i < mgr.Unread.UnreadCounts.Count; i++)
            if (mgr.Unread.UnreadCounts[i].Uid == (ulong)uid) return mgr.Unread.UnreadCounts[i];
        return null;
    }
    /// <summary>先比较聊天时间，同秒时按昵称稳定排列。</summary><param name="left">左侧联系人。</param><param name="right">右侧联系人。</param>
    private int CompareRecent(GiftFriendInfo left, GiftFriendInfo right)
    {
        // 无记录的联系人排在有记录的联系人之后。
        ulong l = Summary(left.RoleId)?.LastChatTime ?? 0;
        // 另一个联系人的最新服务端聊天时间。
        ulong r = Summary(right.RoleId)?.LastChatTime ?? 0;
        if (l != r) return r.CompareTo(l);
        return string.CompareOrdinal(left.Name, right.Name);
    }
    /// <summary>更新红点和左侧列表，不切换正在编辑的会话。</summary>
    private void Refresh()
    {
        hint.SetActive(roomUnread || mgr.Unread.TotalUnreadCount > 0);
        if (!panel.isActiveAndEnabled) return;
        // 当前已被双方删除的会话退出编辑状态。
        GiftFriendInfo active = SelectedFriend();
        if (active == null && selected != 0) { selected = 0; panel.input.text = ""; }
        panel.conversation.SetActive(active != null); panel.empty.SetActive(active == null);
        if (active != null)
        {
            panel.title.text = active.Name;
            if (active.Added) panel.relationLabel.text = "删除好友"; else panel.relationLabel.text = "加为好友";
        }
        // 当前页签只改变左侧可见名单。
        var visible = new List<GiftFriendInfo>(mgr.Friends);
        if (recentOnly) visible.RemoveAll(friend => Summary(friend.RoleId) == null);
        visible.Sort(CompareRecent);
        if (recentOnly) { panel.underline.anchoredPosition = new Vector2(71, -19); panel.noFriends.text = "暂无最近聊天"; }
        else { panel.underline.anchoredPosition = new Vector2(-71, -19); panel.noFriends.text = "暂无好友"; }
        // 当前页可见联系人对应的复用行。
        var rows = panel.FriendRows(visible.Count);
        // 把可见名单逐一绑定到对应行。
        for (int i = 0; i < visible.Count; i++)
        {
            // 行显示的红点只取该玩家自己的未读。
            GiftFriendInfo contact = visible[i];
            rows[i].Bind(contact, Summary(contact.RoleId)?.Count ?? 0, selected == (ulong)contact.RoleId, SelectFriend);
        }
    }
    /// <summary>玩家点选联系人后才对其会话标记已读。</summary><param name="uid">该行的真实角色号。</param>
    private void SelectFriend(ulong uid)
    {
        if (selected != uid) panel.input.text = "";
        selected = uid; panel.status.text = ""; Refresh();
        // 先显示本地历史，随后用已确认序号请求缺口。
        ReadPrivateMessageRes history = chat.Get(uid);
        panel.ShowChat(history, Play, true); mgr.RequestReadPrivateMessage(uid, history.Squence);
    }
    /// <summary>按完整昵称寻找并添加本区好友。</summary>
    private void AddFriend()
    {
        // 输入为空时保留输入框，直接显示原因。
        string name = panel.nickname.text.Trim();
        if (name.Length == 0) { ShowError("请输入好友角色名"); return; }
        recentOnly = false; panel.status.text = ""; mgr.RequestAddFriend(name, 0);
    }
    /// <summary>删除自己的关系，或回加已主动添加自己的角色。</summary>
    private void ChangeRelation()
    {
        // 当前服务端列表决定按钮实际含义。
        GiftFriendInfo friend = SelectedFriend();
        if (friend == null) return;
        panel.status.text = "";
        if (friend.Added) mgr.RequestDeleteFriend(selected); else mgr.RequestAddFriend("", selected);
    }
    /// <summary>按回车或移动键盘的发送键提交，与发送按钮走同一入口。</summary><param name="draft">UGUI 刚提交的草稿，内容由输入框持有。</param>
    private void SubmitText(string draft) { SendText(); }
    /// <summary>提交文字后以服务器定向回显作为正式聊天行。</summary>
    private void SendText()
    {
        // 不发送没有可见正文的输入。
        string text = panel.input.text.Trim();
        if (selected == 0 || text.Length == 0) return;
        if (FriendCode.TextLength(text) > FriendCode.FriendMaxNum) { ShowError("消息超过 100 个计数单位"); return; }
        panel.status.text = "";
        if (mgr.RequestSendPrivateChatMsg(new PrivateChatInfo { Uid = selected, Content = text, MediumType = ChatMediumType.Text })) panel.input.text = "";
    }
    /// <summary>把选择的完整表情标记保存为 RO 的文字媒介。</summary><param name="marker">表情包中当前选定的原始编号标记。</param>
    private void SendEmoji(string marker)
    {
        if (selected != 0) mgr.RequestSendPrivateChatMsg(new PrivateChatInfo { Uid = selected, Content = marker, MediumType = ChatMediumType.Text });
    }
    /// <summary>当前长期群是否有消息或邀请。</summary>
    private bool roomUnread;
    /// <summary>主菜单红点汇总本人私聊及群聊。</summary><param name="unread">至少一个群有未读消息或待接邀请。</param>
    internal void SetRoomHint(bool unread)
    {
        roomUnread = unread; panel.roomHint.SetActive(unread); hint.SetActive(roomUnread || mgr.Unread.TotalUnreadCount > 0);
    }
    /// <summary>向房间邀请面板提供本角色已取得的好友名单。</summary>
    internal List<GiftFriendInfo> Contacts => mgr.Friends;
    /// <summary>长按时将通过麦克风权限检查的收件人固定为当前对端。</summary>
    private bool StartRecord()
    {
        if (selected == 0 || !audio.StartRecord(ChatChannel.Private)) return false;
        recordingPeer = selected; return true;
    }
    /// <summary>上传成功后向开始录音时的目标发出真实音频 ID。</summary><param name="channel">录音发起的频道。</param><param name="clip">服务器签发的引用。</param>
    private void OnAudio(ChatChannel channel, AudioChat clip)
    {
        if (channel != ChatChannel.Private || recordingPeer == 0) return;
        mgr.RequestSendPrivateChatMsg(new PrivateChatInfo { Uid = recordingPeer, MediumType = ChatMediumType.Audio, Audio = clip });
        recordingPeer = 0;
    }
    /// <summary>按 audio_id 走已有分块下载播放器。</summary><param name="clip">点击的语音条。</param>
    private void Play(AudioChat clip) { if (audio != null) audio.PlayRecordedFile(clip); }
    /// <summary>实时消息先写入对应存档，只有当前可见对话才立即补读。</summary><param name="row">来自角色服的单条正式消息。</param>
    private void OnPrivateChat(PrivateDetailChatInfo row)
    {
        if (role == 0) return;
        chat.AddMessage(row);
        // 本人回显也应即时出现在最近联系人的第一组。
        UnReadMessageCountInfo summary = Summary((int)row.ChatUid);
        if (summary == null) { summary = new UnReadMessageCountInfo { Uid = row.ChatUid }; mgr.Unread.UnreadCounts.Add(summary); }
        if (row.ChatTime > summary.LastChatTime) summary.LastChatTime = row.ChatTime;
        Refresh();
        if (!panel.isActiveAndEnabled || selected != row.ChatUid) return;
        // 刚合并在线通知的对端本地历史。
        ReadPrivateMessageRes history = chat.Get(selected);
        panel.ShowChat(history, Play, false); mgr.RequestReadPrivateMessage(selected, history.Squence);
    }
    /// <summary>只为当前仍打开的对话继续翻页，其他返回页按实际对端写回本地。</summary><param name="page">中心角色库按序号返回的一页。</param>
    private void OnReadPrivateMessage(ReadPrivateMessageRes page)
    {
        if (role == 0) return;
        chat.ReadPrivateMessage(page);
        if (!panel.isActiveAndEnabled || page.Uid != selected) return;
        panel.ShowChat(chat.Get(selected), Play, false);
        if (page.HasMore) mgr.RequestReadPrivateMessage(selected, page.Squence);
    }
    /// <summary>将输入和服务端错误显示在当前窗口。</summary><param name="message">可直接展示的原因。</param>
    private void ShowError(string message) { panel.status.text = message; }
}
