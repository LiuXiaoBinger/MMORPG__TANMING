using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>RO FriendsPanel 的双栏好友与私聊视图，由 MainView 在功能层注册。</summary>
public sealed class FriendsPanel : WindowBase
{
    /// <summary>原比例缩放的好友窗口主体。</summary>
    [SerializeField] private RectTransform frame;
    /// <summary>关闭面板并回到上一级。</summary>
    [SerializeField] private Button close;
    /// <summary>切到全部好友页。</summary>
    [SerializeField] internal Button friends;
    /// <summary>只看有聊天记录的联系人。</summary>
    [SerializeField] internal Button recent;
    /// <summary>当前联系人页签的底部选中线。</summary>
    [SerializeField] internal RectTransform underline;
    /// <summary>同区角色完整昵称输入框。</summary>
    [SerializeField] internal ChatInputField nickname;
    /// <summary>确认按昵称添加。</summary>
    [SerializeField] internal Button add;
    /// <summary>选中联系人的资料和聊天栏。</summary>
    [SerializeField] internal GameObject conversation;
    /// <summary>尚未选择联系人的 RO 波利空态。</summary>
    [SerializeField] internal GameObject empty;
    /// <summary>当前页没有联系人时的文字。</summary>
    [SerializeField] internal Text noFriends;
    /// <summary>当前正在对话的角色昵称。</summary>
    [SerializeField] internal Text title;
    /// <summary>删除自己的关系或回加对方。</summary>
    [SerializeField] internal Button relation;
    /// <summary>关系按钮的当前动作。</summary>
    [SerializeField] internal Text relationLabel;
    /// <summary>从好友面板打开同一主城的聊天室。</summary>
    [SerializeField] internal Button rooms;
    /// <summary>展开基础包表情选单。</summary>
    [SerializeField] internal Button emoji;
    /// <summary>按 RO 编号选取与还原表情。</summary>
    [SerializeField] internal EmojiPackageTemplate emojis;
    /// <summary>群邀请和未读群消息的红点。</summary>
    [SerializeField] internal GameObject roomHint;
    /// <summary>当前聊天草稿。</summary>
    [SerializeField] internal ChatInputField input;
    /// <summary>发送一条私聊文字。</summary>
    [SerializeField] internal Button send;
    /// <summary>沿用项目按住录音、滑出取消的组件。</summary>
    [SerializeField] internal ChatRecordObj record;
    /// <summary>会话记录的滚动位置。</summary>
    [SerializeField] private ScrollRect chatScroll;
    /// <summary>左列复用的联系人模板。</summary>
    [SerializeField] private FriendsItemTemplate friendTemplate;
    /// <summary>右列复用的消息模板。</summary>
    [SerializeField] private FriendChatTemplate chatTemplate;
    /// <summary>网络或输入失败的当前提示。</summary>
    [SerializeField] internal Text status;
    /// <summary>当前面板已实例化的联系人行。</summary>
    private readonly List<FriendsItemTemplate> contacts = new List<FriendsItemTemplate>();
    /// <summary>当前面板已实例化的历史消息行。</summary>
    private readonly List<FriendChatTemplate> messages = new List<FriendChatTemplate>();
    /// <summary>打开时重新查询当前玩家好友。</summary>
    internal event Action Opened;

    /// <summary>初始化预制体上的关闭操作。</summary>
    private void Awake() { close.onClick.AddListener(CloseWindow); emoji.onClick.AddListener(() => emojis.gameObject.SetActive(!emojis.gameObject.activeSelf)); }
    /// <summary>恢复布局并交控制器读取会话。</summary>
    private void OnEnable() { emojis.gameObject.SetActive(false); OnRectTransformDimensionsChange(); Opened?.Invoke(); }
    /// <summary>小屏幕按比例完整显示好友框。</summary>
    private void OnRectTransformDimensionsChange()
    {
        if (frame == null) return;
        // 功能层的可用 Canvas 尺寸。
        Rect area = ((RectTransform)transform).rect;
        frame.localScale = Vector3.one * Mathf.Min(1f, area.width / frame.sizeDelta.x, area.height / frame.sizeDelta.y);
    }
    /// <summary>按当前好友或最近列表的行数准备原生滚动区域。</summary><param name="count">当前页联系人个数。</param>
    internal List<FriendsItemTemplate> FriendRows(int count)
    {
        while (contacts.Count < count) contacts.Add(Instantiate(friendTemplate, friendTemplate.transform.parent));
        // 按实际联系人条数显示或收起行。
        for (int i = 0; i < contacts.Count; i++) contacts[i].gameObject.SetActive(i < count);
        noFriends.gameObject.SetActive(count == 0); return contacts;
    }
    /// <summary>复用右侧消息行，打开新会话时定位到最新消息。</summary><param name="history">当前联系人的有序本机记录。</param><param name="onPlay">播放该行语音。</param><param name="jump">选择新联系人时为真。</param>
    internal void ShowChat(ReadPrivateMessageRes history, Action<AudioChat> onPlay, bool jump)
    {
        // 用户在阅读旧消息时保留当前位置。
        bool bottom = jump || chatScroll.verticalNormalizedPosition < .02f;
        // 本地缓存中应显示的消息数。
        int count = history.PrivateChatList.Count;
        while (messages.Count < count) messages.Add(Instantiate(chatTemplate, chatTemplate.transform.parent));
        // 将本会话有序消息绑定到复用行。
        for (int i = 0; i < messages.Count; i++)
        {
            messages[i].gameObject.SetActive(i < count);
            if (i < count) messages[i].Bind(history.PrivateChatList[i], onPlay, emojis);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatScroll.content);
        if (bottom) chatScroll.verticalNormalizedPosition = 0;
    }
}
