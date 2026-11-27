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
    /// <summary>最近和联系人页签的两块浅色选中底板，顺序与录像相同。</summary>
    [SerializeField] private GameObject[] selectedTabs;
    /// <summary>切页时切换深色或浅色文字的页签标题。</summary>
    [SerializeField] private Text[] tabLabels;
    /// <summary>联系人折叠列表、筛选和本机分组选择器。</summary>
    [SerializeField] internal FriendsDirectory directory;
    /// <summary>显示货币余额与右侧导航的共用页头。</summary>
    [SerializeField] internal SocialPanelHeader header;
    /// <summary>左下打开按昵称添加好友的弹层。</summary>
    [SerializeField] private Button openAdd;
    /// <summary>添加好友弹层。</summary>
    [SerializeField] internal GameObject addDialog;
    /// <summary>原添加弹层内的推荐行、筛选和本区搜索视图。</summary>
    [SerializeField] internal AddFriendsPanel discovery;
    /// <summary>添加弹层内输入为空时显示的提示。</summary>
    [SerializeField] internal Text addStatus;
    /// <summary>放弃本次添加并收起弹层。</summary>
    [SerializeField] private Button cancelAdd;
    /// <summary>将当前联系人加入或移出特别关注。</summary>
    [SerializeField] internal Button star;
    /// <summary>根据个人关注状态显示操作名。</summary>
    [SerializeField] internal Text starLabel;
    /// <summary>已有会话但暂无消息时展示的侠客空态。</summary>
    [SerializeField] private GameObject noHistory;
    /// <summary>同区角色号或昵称搜索框。</summary>
    [SerializeField] internal ChatInputField nickname;
    /// <summary>按当前输入搜索可见玩家。</summary>
    [SerializeField] internal Button add;
    /// <summary>选中联系人的资料和聊天栏。</summary>
    [SerializeField] internal GameObject conversation;
    /// <summary>尚未选择联系人的 RO 波利空态。</summary>
    [SerializeField] internal GameObject empty;
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
    /// <summary>最近页签上的实际未读数量徽标。</summary>
    [SerializeField] internal Text recentUnread;
    /// <summary>当前聊天草稿。</summary>
    [SerializeField] internal ChatInputField input;
    /// <summary>发送一条私聊文字。</summary>
    [SerializeField] internal Button send;
    /// <summary>沿用项目按住录音、滑出取消的组件。</summary>
    [SerializeField] internal ChatRecordObj record;
    /// <summary>会话记录的滚动位置。</summary>
    [SerializeField] private ScrollRect chatScroll;
    /// <summary>右列复用的消息模板。</summary>
    [SerializeField] private FriendChatTemplate chatTemplate;
    /// <summary>网络或输入失败的当前提示。</summary>
    [SerializeField] internal Text status;
    /// <summary>当前面板已实例化的历史消息行。</summary>
    private readonly List<FriendChatTemplate> messages = new List<FriendChatTemplate>();
    /// <summary>打开时重新查询当前玩家好友。</summary>
    internal event Action Opened;

    /// <summary>初始化预制体上的关闭操作。</summary>
    private void Awake()
    {
        close.onClick.AddListener(CloseWindow);
        emoji.onClick.AddListener(() => emojis.gameObject.SetActive(!emojis.gameObject.activeSelf));
        openAdd.onClick.AddListener(() => { addStatus.text = ""; addDialog.SetActive(true); });
        cancelAdd.onClick.AddListener(() => addDialog.SetActive(false));
    }
    /// <summary>恢复布局并交控制器读取会话。</summary>
    private void OnEnable() { addDialog.SetActive(false); emojis.gameObject.SetActive(false); OnRectTransformDimensionsChange(); Opened?.Invoke(); }
    /// <summary>小屏幕按比例完整显示好友框。</summary>
    private void OnRectTransformDimensionsChange()
    {
        if (frame == null) return;
        // 功能层的可用 Canvas 尺寸。
        Rect area = ((RectTransform)transform).rect;
        frame.localScale = Vector3.one * Mathf.Min(area.width / frame.sizeDelta.x, area.height / frame.sizeDelta.y);
    }
    /// <summary>使最近和联系人两个页签的选中颜色与当前目录一致。</summary><param name="recentOnly">显示最近联系人时为真。</param>
    internal void SetPage(bool recentOnly)
    {
        selectedTabs[0].SetActive(recentOnly);
        selectedTabs[1].SetActive(!recentOnly);
        // RO 蓝色高亮页签使用白字，浅色页签沿用好友姓名的蓝色。
        Color selectedColor = Color.white;
        // 未选页签的文字颜色，和群聊预制体中相同状态一致。
        Color normalColor = new Color32(79, 107, 182, 255);
        if (recentOnly)
        {
            tabLabels[0].color = selectedColor;
            tabLabels[1].color = normalColor;
        }
        else
        {
            tabLabels[0].color = normalColor;
            tabLabels[1].color = selectedColor;
        }
    }
    /// <summary>复用右侧消息行，打开新会话时定位到最新消息。</summary><param name="history">当前联系人的有序本机记录。</param><param name="onPlay">播放该行语音。</param><param name="jump">选择新联系人时为真。</param>
    internal void ShowChat(ReadPrivateMessageRes history, Action<AudioChat> onPlay, bool jump)
    {
        // 用户在阅读旧消息时保留当前位置。
        bool bottom = jump || chatScroll.verticalNormalizedPosition < .02f;
        // 本地缓存中应显示的消息数。
        int count = history.PrivateChatList.Count;
        noHistory.SetActive(count == 0);
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
