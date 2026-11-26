using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>对应 RO ChatRoomPanel，在主城功能层显示持久群目录、成员、邀请和群消息。</summary>
public sealed class ChatRoomPanel : WindowBase
{
    /// <summary>按设计尺寸缩放的 RO 群聊主体。</summary>
    [SerializeField] private RectTransform frame;
    /// <summary>返回好友面板的关闭入口。</summary>
    [SerializeField] private Button close;
    /// <summary>新群的昵称输入框。</summary>
    [SerializeField] internal ChatInputField groupName;
    /// <summary>切换 RO 的四个人数档位。</summary>
    [SerializeField] internal Button capacity;
    /// <summary>当前建群人数档位。</summary>
    [SerializeField] internal Text capacityLabel;
    /// <summary>创建新群。</summary>
    [SerializeField] internal Button create;
    /// <summary>无群或尚未选择群的提示。</summary>
    [SerializeField] internal Text empty;
    /// <summary>展示当前群名和成员数量。</summary>
    [SerializeField] internal Text title;
    /// <summary>群成员真实昵称。</summary>
    [SerializeField] internal Text members;
    /// <summary>右栏已入群的消息与操作。</summary>
    [SerializeField] internal GameObject conversation;
    /// <summary>尚未接受邀请的操作栏。</summary>
    [SerializeField] internal GameObject invitation;
    /// <summary>接受加入这个群。</summary>
    [SerializeField] internal Button accept;
    /// <summary>拒绝本次群邀请。</summary>
    [SerializeField] internal Button decline;
    /// <summary>从本角色的好友中选择受邀者。</summary>
    [SerializeField] internal Button invite;
    /// <summary>选择好友的浮层。</summary>
    [SerializeField] internal GameObject invitePanel;
    /// <summary>收起好友选择浮层。</summary>
    [SerializeField] private Button cancelInvite;
    /// <summary>邀请名单中尚无好友时的提示。</summary>
    [SerializeField] internal GameObject noContacts;
    /// <summary>离开当前群聊。</summary>
    [SerializeField] internal Button leave;
    /// <summary>群主的解散入口。</summary>
    [SerializeField] internal Button dissolve;
    /// <summary>解散前显示群名及不可恢复的影响。</summary>
    [SerializeField] internal GameObject confirmPanel;
    /// <summary>确认解散当前群。</summary>
    [SerializeField] internal Button confirm;
    /// <summary>取消解散。</summary>
    [SerializeField] private Button cancel;
    /// <summary>确认时展示的目标群名称。</summary>
    [SerializeField] internal Text confirmText;
    /// <summary>群消息草稿。</summary>
    [SerializeField] internal ChatInputField input;
    /// <summary>发送当前文字。</summary>
    [SerializeField] internal Button send;
    /// <summary>打开群聊表情包。</summary>
    [SerializeField] private Button emoji;
    /// <summary>当前窗口使用的 RO 表情包。</summary>
    [SerializeField] internal EmojiPackageTemplate emojis;
    /// <summary>群聊长按录音、移出取消。</summary>
    [SerializeField] internal ChatRecordObj record;
    /// <summary>向前加载更早一页。</summary>
    [SerializeField] internal Button older;
    /// <summary>结果或失败原因。</summary>
    [SerializeField] internal Text status;
    /// <summary>群聊消息滚动区。</summary>
    [SerializeField] private ScrollRect scroll;
    /// <summary>左栏群目录的列表行。</summary>
    [SerializeField] private FriendsItemTemplate roomTemplate;
    /// <summary>好友邀请选单的列表行。</summary>
    [SerializeField] private FriendsItemTemplate inviteTemplate;
    /// <summary>群发言的气泡模板。</summary>
    [SerializeField] private FriendChatTemplate template;
    /// <summary>已实例化的目录行。</summary>
    private readonly List<FriendsItemTemplate> rooms = new List<FriendsItemTemplate>();
    /// <summary>已实例化的好友选择行。</summary>
    private readonly List<FriendsItemTemplate> friends = new List<FriendsItemTemplate>();
    /// <summary>受本窗管理的消息行。</summary>
    private readonly List<FriendChatTemplate> rows = new List<FriendChatTemplate>();
    /// <summary>玩家点关闭时回到好友页。</summary>
    internal event Action BackToFriends;
    /// <summary>显示时通知控制器查询最新群目录。</summary>
    internal event Action Opened;
    /// <summary>只绑定视图自身的展开与收起动作。</summary>
    private void Awake()
    {
        close.onClick.AddListener(() => { CloseWindow(); BackToFriends?.Invoke(); }); cancelInvite.onClick.AddListener(() => invitePanel.SetActive(false));
        cancel.onClick.AddListener(() => confirmPanel.SetActive(false));
        emoji.onClick.AddListener(() => emojis.gameObject.SetActive(!emojis.gameObject.activeSelf));
    }
    /// <summary>每次打开时先收起弹层。</summary>
    private void OnEnable() { invitePanel.SetActive(false); confirmPanel.SetActive(false); emojis.gameObject.SetActive(false); OnRectTransformDimensionsChange(); Opened?.Invoke(); }
    /// <summary>适应小屏画布的可用宽高。</summary>
    private void OnRectTransformDimensionsChange()
    {
        if (frame == null) return;
        // 计算含所有按钮在内的整体缩放。
        Rect area = ((RectTransform)transform).rect;
        frame.localScale = Vector3.one * Mathf.Min(1f, area.width / frame.sizeDelta.x, area.height / frame.sizeDelta.y);
    }
    /// <summary>按当前目录逐项展示群名、人数、邀请及未读。</summary><param name="list">已加入和受邀的目录。</param><param name="selected">选中群。</param><param name="choose">选择群回调。</param>
    internal void ShowRooms(ChatRoomList list, ulong selected, Action<ulong> choose)
    {
        while (rooms.Count < list.Rooms.Count) rooms.Add(Instantiate(roomTemplate, roomTemplate.transform.parent));
        // 用本次目录覆盖现有的群行。
        for (int i = 0; i < rooms.Count; i++)
        {
            rooms[i].gameObject.SetActive(i < list.Rooms.Count);
            if (i >= list.Rooms.Count) continue;
            // 待接受的邀请显示独立关系说明。
            ChatRoomInfo item = list.Rooms[i];
            // 默认列出群成员人数，待处理邀请使用单独文字。
            string detail = item.RoomMember.Info.Count + " / " + item.RoomSetting.RoomCapacity + " 人";
            if (item.Invited) detail = "邀请你加入";
            rooms[i].BindRoom(item.RoomUid, item.RoomSetting.RoomName, detail, item.UnreadCount, selected == item.RoomUid, choose);
        }
    }
    /// <summary>选择当前角色好友作为邀请对象。</summary><param name="list">与赠礼共用的真实好友名单。</param><param name="choose">提交实名邀请。</param>
    internal void ShowInvites(List<GiftFriendInfo> list, Action<ulong> choose)
    {
        invitePanel.SetActive(true); noContacts.SetActive(list.Count == 0);
        while (friends.Count < list.Count) friends.Add(Instantiate(inviteTemplate, inviteTemplate.transform.parent));
        // 展示好友选单里实际可邀请的角色。
        for (int i = 0; i < friends.Count; i++)
        {
            friends[i].gameObject.SetActive(i < list.Count);
            if (i < list.Count) friends[i].Bind(list[i], 0, false, choose);
        }
    }
    /// <summary>展示按序群历史，向前插入旧页时保持用户的阅读位置。</summary><param name="list">当前群已加载的有序消息。</param><param name="role">本角色编号。</param><param name="play">复用录音播放器。</param><param name="jump">是否主动定位到底部。</param><param name="prepend">是否正向前插入一页。</param>
    internal void ShowMessages(List<RoomChatMsg> list, int role, Action<AudioChat> play, bool jump, bool prepend)
    {
        // 插入更早记录前记下视口位置和内容原高度。
        float oldHeight = scroll.content.rect.height;
        // 追加旧页前内容相对视口的位移。
        Vector2 position = scroll.content.anchoredPosition;
        // 先前在末尾时跟随新消息。
        bool bottom = jump || scroll.verticalNormalizedPosition < .02f;
        while (rows.Count < list.Count) rows.Add(Instantiate(template, template.transform.parent));
        // 按当前群消息顺序填入复用气泡。
        for (int i = 0; i < rows.Count; i++)
        {
            rows[i].gameObject.SetActive(i < list.Count);
            if (i >= list.Count) continue;
            // 群行也沿用 RO 的左右气泡与表情图集。
            RoomChatMsg msg = list[i];
            // RO 规定本人的行靠右，其他成员的行靠左。
            int side = 2;
            if (msg.SenderUid == (ulong)role) side = 1;
            rows[i].Bind(new PrivateDetailChatInfo { Content = msg.Content, ChatTime = msg.ChatTime, Audio = msg.Audio, WhoType = side }, play, emojis, msg.SenderName);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
        if (prepend) scroll.content.anchoredPosition = position + new Vector2(0, scroll.content.rect.height - oldHeight);
        else if (bottom) scroll.verticalNormalizedPosition = 0;
    }
}
