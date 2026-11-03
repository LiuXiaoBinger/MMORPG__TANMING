using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>聊天频道编号；从 0 连续排列，同时用作频道状态与未读数组下标。</summary>
public enum ChatChannel
{
    // 世界频道：默认的公开聊天频道。
    World,
    // 队伍频道。
    Team,
    // 公会频道。
    Guild,
    // 私聊频道。
    Private,
    // 附近频道。
    Nearby,
    // 观战频道。
    Watch,
    // 职业频道。
    Profession,
    // 系统提示消息频道。
    System
}

// 一条聊天消息的数据模型；只保存数据，不负责显示 UI。
[Serializable]
public sealed class ChatMessage
{
    // 消息所属频道。
    public ChatChannel Channel;
    // 发送者名称。
    public string Sender;
    // 消息正文；TextArea 仅让 Inspector 中的编辑框以多行显示。
    [TextArea] public string Text;
    // true 表示本地玩家发送，用于选择自己的消息预制体。
    public bool IsLocalPlayer;
    // 语音消息可同时显示文字；纯文字消息会隐藏语音节点。
    public bool HasVoice;
    // 语音消息保存的音频资源引用，当前窗口只保存，不负责播放。
    public AudioClip VoiceClip;
    // 在本地生成这条消息时记录的系统时间，用 HH:mm 显示，不是服务器时间。
    public DateTime SentAt;
}

/// <summary>
/// 聊天演示窗口：保存消息、按当前频道显示最近一段记录，并处理输入、未读提示和频道显隐。
/// 基础控件和消息模板由 Inspector 提供；本类不发送网络协议，语音入口只保存音频引用并显示语音节点。
/// </summary>
public sealed class ChatWindows : MonoBehaviour
{

    #region 字段、事件与属性
    // 所有频道合计保存的消息上限，至少按 1 处理；超过时删除最早记录，实际显示行数另受 pooledMessageRows 限制。
    [SerializeField] private int maxVisibleMessages = 1000;

    // 本地玩家发送消息时显示的名字。
    [SerializeField] private string localPlayerName = "You";

    // 是否允许在输入框内按 Enter / 小键盘 Enter 发送消息。
    [SerializeField] private bool submitWithReturn = true;

    // 当前选中频道，本地发送和消息过滤都读取它。
    [SerializeField] private ChatChannel currentChannel = ChatChannel.World;

    // 所有频道按加入顺序保存的消息；超过缓存上限时从头删除，Messages 属性提供读取入口。
    private readonly List<ChatMessage> messages = new List<ChatMessage>();

    // 以下引用必须从 Chat 预制体 Inspector 拖拽赋值，避免运行时依赖对象名称。
    [Header("Chat UI References")]
    // 优先使用的 TMP 输入框，读取消息和恢复输入焦点都先尝试它。
    [SerializeField] private TMP_InputField inputField;

    // 兼容旧预制体的 UGUI 输入框，未配置 TMP 时作为输入来源。
    [SerializeField] private InputField legacyInputField;

    // 读取输入框并添加本地消息的发送按钮。
    [SerializeField] private Button sendButton;

    // 消息行实例的父节点，其父节点会作为滚动视口。
    [SerializeField] private RectTransform content;

    // 消息列表的滚动组件，ConfigureChatList 会改为视口上的组件。
    [SerializeField] private ScrollRect scrollRect;

    // 消息行模板由预制体 Inspector 外部拖拽赋值，避免运行时按名称查找。
    [SerializeField] private RectTransform otherMessageTemplate;

    // 本地玩家消息行模板，用于创建右对齐的消息行。
    [SerializeField] private RectTransform playerMessageTemplate;

    // 系统频道消息行模板，优先于本地/他人模板选择。
    [SerializeField] private RectTransform systemMessageTemplate;

    // 当前频道最多显示的最近消息数，同时限制三种消息行池的合计数量，至少按 1 处理。
    [SerializeField] private int pooledMessageRows = 30;

    // 已成功绑定的频道 Toggle，用来判断 Start 是否需要再次尝试绑定。
    private readonly List<Toggle> channelToggles = new List<Toggle>();

    // 频道到选择 Toggle 的对应表，切换频道时更新各项选中图形。
    private readonly Dictionary<ChatChannel, Toggle> channelToggleMap = new Dictionary<ChatChannel, Toggle>();

    // 可重复绑定他人消息的行实例，不是消息数据本身。
    private readonly List<RectTransform> pooledOtherRows = new List<RectTransform>();

    // 可重复绑定本地玩家消息的行实例。
    private readonly List<RectTransform> pooledPlayerRows = new List<RectTransform>();

    // 可重复绑定系统频道消息的行实例。
    private readonly List<RectTransform> pooledSystemRows = new List<RectTransform>();

    // 最近一次 RefreshVisibleMessages 筛选出的当前频道数据，行池从其末尾取显示条目。
    private readonly List<ChatMessage> visibleMessages = new List<ChatMessage>();

    // 各频道最后一次应用的可见状态，用来识别设置 Toggle 是否发生变化。
    private readonly bool[] channelVisibilityStates = new bool[Enum.GetValues(typeof(ChatChannel)).Length];

    // 各频道是否至少应用过一次显隐，用来区分首次初始化和状态未变化。
    private readonly bool[] channelVisibilityStateInitialized = new bool[Enum.GetValues(typeof(ChatChannel)).Length];

    // 行池正在刷新时阻止滚动回调再次进入同一刷新流程。
    private bool poolRefreshInProgress;

    // 本轮填充系统消息时即将使用的行池下标，每次刷新从 0 开始。
    private int systemPoolIndex;

    // 每个频道各自保存未读消息数量，切换频道时不会互相覆盖。
    private readonly int[] unreadMessageCounts = new int[Enum.GetValues(typeof(ChatChannel)).Length];

    // 预制体中的“新消息提示”节点及其文字、按钮。
    [SerializeField] private RectTransform newMessageHint;

    // TMP 版本的新消息提示文字，显示当前频道累计条数。
    [SerializeField] private TMP_Text newMessageHintText;

    // 旧 UGUI 版本的新消息提示文字；存在时也同步设置。
    [SerializeField] private Text legacyNewMessageHintText;

    // 点击后清除当前频道未读，并把当前显示列表滚到底部。
    [SerializeField] private Button newMessageHintButton;

    // 切换聊天设置面板显隐的按钮。
    [SerializeField] private Button settingButton;

    // 频道显隐设置所在的面板对象。
    [SerializeField] private GameObject chatSettingPanel;

    [Header("Input Mode")]
    // 切到语音输入外观并清除 UI 选中焦点的按钮，不启动录音。
    [SerializeField] private Button voiceButton;

    // 切回文字输入并激活输入框的按钮。
    [SerializeField] private Button keyboardButton;

    // 文字输入区域，与 audioPanel 互斥显示。
    [SerializeField] private GameObject textInputPanel;

    // 语音模式展示区域，当前只负责显隐。
    [SerializeField] private GameObject audioPanel;

    [Header("Channel Tabs")]
    // 世界频道标签，选中时显示世界消息。
    [SerializeField] private Toggle worldChannelTab;

    // 公会频道标签，选中时显示公会消息。
    [SerializeField] private Toggle guildChannelTab;

    // 队伍频道标签，选中时显示队伍消息。
    [SerializeField] private Toggle teamChannelTab;

    // 附近频道标签，当前不在此处按世界距离过滤内容。
    [SerializeField] private Toggle nearbyChannelTab;

    // 私聊频道标签，当前不在此处选择聊天对象。
    [SerializeField] private Toggle privateChannelTab;

    // 观战频道标签，仅按消息的频道编号筛选。
    [SerializeField] private Toggle watchChannelTab;

    // 职业频道标签，当前不在此处校验发送者职业。
    [SerializeField] private Toggle professionChannelTab;

    // 系统频道标签，使用独立的系统消息模板。
    [SerializeField] private Toggle systemChannelTab;

    [Header("Channel Visibility Settings")]
    // 设置面板中决定世界频道标签是否显示的开关。
    [SerializeField] private Toggle worldVisibilityToggle;

    // 设置面板中决定公会频道标签是否显示的开关。
    [SerializeField] private Toggle guildVisibilityToggle;

    // 设置面板中决定队伍频道标签是否显示的开关。
    [SerializeField] private Toggle teamVisibilityToggle;

    // 设置面板中决定附近频道标签是否显示的开关。
    [SerializeField] private Toggle nearbyVisibilityToggle;

    // 设置面板中决定系统频道标签是否显示的开关。
    [SerializeField] private Toggle systemVisibilityToggle;

    // 小于该值时认为滚动条已经到达底部，避免浮点误差导致提示无法清除。
    private const float BottomScrollThreshold = 0.01f;

    // PlayerPrefs 键前缀，后接频道枚举名，分别保存各频道显隐。
    private const string ChannelVisibilityPreferencePrefix = "Chat.ChannelVisible.";

    // 记录上一帧 Enter 是否按下，避免长按时连续发送。
    private bool returnWasPressed;

    // 以只读列表接口提供历史记录；条目仍是原 ChatMessage 引用，其字段没有被封装成只读。
    public IReadOnlyList<ChatMessage> Messages => messages;

    // 返回当前频道，演示控制器据此决定模拟消息加入哪个频道。
    public ChatChannel CurrentChannel => currentChannel;

    // 本地消息加入列表后触发；需要联网时由订阅者处理，本类不会自行发包。
    public event Action<ChatMessage> MessageSent;

    // 接收到外部消息后触发，可供其他系统监听。
    public event Action<ChatMessage> MessageReceived;

    // 聊天设置修改频道可见性后触发，供折叠聊天入口同步未读角标。
    public event Action<ChatChannel, bool> ChannelVisibilityChanged;
    #endregion

    #region 生命周期
    /// <summary>准备聊天控件和列表，绑定发送、回车、频道、显隐设置与滚动事件。</summary>
    private void Awake()
    {
        // 为已配置控件注册操作，准备列表遮罩、滚动组件和现有布局。
        CacheReferences();
        ConfigureChatList();

        if (sendButton != null)
        {
            // 点击发送按钮时，读取输入框并发送消息。
            sendButton.onClick.AddListener(SendInputMessage);
        }

        if (inputField != null)
        {
            // 输入框结束编辑时，检查是否由 Enter 引起并尝试发送。
            inputField.onEndEdit.AddListener(SubmitOnEndEdit);
        }
        if (legacyInputField != null)
        {
            legacyInputField.onEndEdit.AddListener(SubmitOnLegacyEndEdit);
        }

        BindChannelToggles();
        BindChannelVisibilitySettings();
        if (scrollRect != null)
        {
            scrollRect.onValueChanged.AddListener(OnScrollChanged);
        }
    }

    /// <summary>Awake 未绑定到任何频道 Toggle 时再尝试一次。</summary>
    private void Start()
    {
        // 某些编辑器驱动的运行模式会延迟加载预制体子节点，因此在 Start 再尝试绑定一次频道按钮。
        if (channelToggles.Count == 0)
        {
            BindChannelToggles();
        }
    }

    /// <summary>移除显式保存方法引用的 UI 监听；消息数据和行对象随组件所在对象生命周期处理。</summary>
    private void OnDestroy()
    {
        // 移除这里登记的按钮、输入框和滚动监听；频道 Toggle 的匿名监听未在本方法中移除。
        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(SendInputMessage);
        }

        if (inputField != null)
        {
            inputField.onEndEdit.RemoveListener(SubmitOnEndEdit);
        }
        if (legacyInputField != null)
        {
            legacyInputField.onEndEdit.RemoveListener(SubmitOnLegacyEndEdit);
        }

        if (scrollRect != null)
        {
            scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
        }

        if (newMessageHintButton != null)
        {
            newMessageHintButton.onClick.RemoveListener(OnNewMessageHintClicked);
        }

        if (settingButton != null)
        {
            settingButton.onClick.RemoveListener(ToggleChatSettings);
        }
        if (voiceButton != null)
        {
            voiceButton.onClick.RemoveListener(ShowVoiceInput);
        }
        if (keyboardButton != null)
        {
            keyboardButton.onClick.RemoveListener(ShowTextInput);
        }

    }

    /// <summary>同步频道设置；文字输入获焦且启用回车发送时，检测 Enter 的按下变化。</summary>
    private void Update()
    {
        // 设置面板来自嵌套预制体。即使其 onValueChanged 监听在某些加载顺序下丢失，
        // 这里也会根据 Toggle 的真实状态立即同步频道标签和玩家设置。
        SynchronizeChannelVisibilitySettings();

        // 只有启用回车发送、输入框存在且正在输入时，才检查按键。
        if (!submitWithReturn || !IsInputFocused())
        {
            return;
        }

        // 当前是否按住主键盘或小键盘 Enter，与上一帧比较后只处理新按下。
        bool returnPressed = Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter);
        // 只在按下的第一帧发送，防止按住 Enter 重复发送。
        if (returnPressed && !returnWasPressed)
        {
            SendInputMessage();
        }

        returnWasPressed = returnPressed;
    }
    #endregion

    #region 界面显示与刷新
    /// <summary>隐藏三种行池中的已有对象，保留实例供后续消息重用。</summary>
    private void HidePooledRows()
    {
        foreach (RectTransform row in pooledOtherRows)
        {
            if (row != null) row.gameObject.SetActive(false);
        }

        foreach (RectTransform row in pooledPlayerRows)
        {
            if (row != null) row.gameObject.SetActive(false);
        }

        foreach (RectTransform row in pooledSystemRows)
        {
            if (row != null) row.gameObject.SetActive(false);
        }
    }

    /// <summary>显示语音模式区域，并清除 EventSystem 当前选中对象。</summary>
    private void ShowVoiceInput()
    {
        SetVoiceInputMode(true);
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    /// <summary>显示文字区域并恢复输入框焦点。</summary>
    private void ShowTextInput()
    {
        SetVoiceInputMode(false);
        ActivateInputField();
    }

    /// <summary>记住当前归一化滚动位置，再重建当前频道的显示数据。</summary>
    /// <param name="scrollToBottom">true 刷新后滚底，false 恢复原归一化位置。</param>
    private void RefreshVisibleMessages(bool scrollToBottom)
    {
        // 0 表示底部、1 表示顶部；没有滚动组件时暂取顶部值。
        float previousPosition = scrollRect == null ? 1f : scrollRect.verticalNormalizedPosition;
        RefreshVisibleMessages(scrollToBottom, previousPosition);
    }

    /// <summary>从历史筛选当前频道，重绑最近消息行，再安排下一帧调整滚动位置。</summary>
    /// <param name="scrollToBottom">是否将列表滚到底部。</param>
    /// <param name="previousPosition">不滚底时要恢复的归一化位置。</param>
    private void RefreshVisibleMessages(bool scrollToBottom, float previousPosition)
    {
        if (content == null || scrollRect == null)
        {
            return;
        }

        visibleMessages.Clear();
        foreach (ChatMessage message in messages)
        {
            if (message.Channel == currentChannel)
            {
                visibleMessages.Add(message);
            }
        }

        RefreshPoolForScroll();
        if (scrollToBottom)
        {
            StartCoroutine(ScrollToLatestMessage());
        }
        else
        {
            StartCoroutine(RestoreScrollPosition(previousPosition));
        }
    }

    /// <summary>显示缓存中最近 pooledMessageRows 条消息，按系统、本地、他人模板复用行；不按滚动位置翻页。</summary>
    private void RefreshPoolForScroll()
    {
        if (content == null || scrollRect == null || poolRefreshInProgress)
        {
            return;
        }

        poolRefreshInProgress = true;
        // 本次最多显示的消息数，配置小于 1 时仍允许显示 1 条。
        int poolSize = Mathf.Max(1, pooledMessageRows);
        // 当前频道显示缓存里的消息总数，可能多于实际创建的行数。
        int totalCount = visibleMessages.Count;
        // 始终取当前频道最后 poolSize 条消息，不按滚动位置换成更早的记录；较早数据只在缓存上限内保留。
        int firstIndex = Mathf.Max(0, totalCount - poolSize);
        // 显示区间的结束下标，不包含该下标本身。
        int lastIndex = totalCount;

        // 当前显示区间需要的他人消息行数。
        int otherRequired = 0;
        // 当前显示区间需要的本地玩家消息行数。
        int playerRequired = 0;
        // 当前显示区间需要的系统消息行数。
        int systemRequired = 0;
        for (int index = firstIndex; index < lastIndex; index++)
        {
            if (visibleMessages[index].Channel == ChatChannel.System) systemRequired++;
            else if (visibleMessages[index].IsLocalPlayer) playerRequired++;
            else otherRequired++;
        }

        EnsurePoolRows(pooledOtherRows, otherMessageTemplate, otherRequired);
        EnsurePoolRows(pooledPlayerRows, playerMessageTemplate, playerRequired);
        EnsurePoolRows(pooledSystemRows, systemMessageTemplate, systemRequired);
        TrimPoolRowsToCapacity(otherRequired, playerRequired, systemRequired);
        HidePooledRows();

        // 本轮下一条他人消息使用的行池下标。
        int otherIndex = 0;
        // 本轮下一条本地消息使用的行池下标。
        int playerIndex = 0;
        systemPoolIndex = 0;
        for (int index = firstIndex; index < lastIndex; index++)
        {
            // 当前待显示的缓存消息，原对象引用不会被行池复制。
            ChatMessage message = visibleMessages[index];
            // 根据频道和发送者选出的复用行，填文字后移动到显示顺序末尾。
            RectTransform row;
            if (message.Channel == ChatChannel.System)
                row = pooledSystemRows[systemPoolIndex++];
            else if (message.IsLocalPlayer)
                row = pooledPlayerRows[playerIndex++];
            else
                row = pooledOtherRows[otherIndex++];
            row.gameObject.SetActive(true);
            row.transform.SetAsLastSibling();
            SetTemplateText(row.transform, message);
        }

        poolRefreshInProgress = false;
    }

    /// <summary>逐帧检查世界、公会、队伍、附近、系统五个可配置频道开关。</summary>
    private void SynchronizeChannelVisibilitySettings()
    {
        SynchronizeChannelVisibilitySetting(worldVisibilityToggle, ChatChannel.World);
        SynchronizeChannelVisibilitySetting(guildVisibilityToggle, ChatChannel.Guild);
        SynchronizeChannelVisibilitySetting(teamVisibilityToggle, ChatChannel.Team);
        SynchronizeChannelVisibilitySetting(nearbyVisibilityToggle, ChatChannel.Nearby);
        SynchronizeChannelVisibilitySetting(systemVisibilityToggle, ChatChannel.System);
    }

    /// <summary>设置开关与上次应用状态不同，或尚未初始化时，更新标签并保存选择。</summary>
    /// <param name="visibilityToggle">设置面板中的显示开关，未配置时跳过。</param>
    /// <param name="channel">该开关控制的频道。</param>
    private void SynchronizeChannelVisibilitySetting(Toggle visibilityToggle, ChatChannel channel)
    {
        if (visibilityToggle == null)
        {
            return;
        }

        int index = (int)channel;
        if (!channelVisibilityStateInitialized[index] || channelVisibilityStates[index] != visibilityToggle.isOn)
        {
            ApplyChannelVisibility(channel, visibilityToggle.isOn, true);
        }
    }

    /// <summary>让选中图形所在对象跟随 Toggle 状态，并启用图形组件。</summary>
    /// <param name="toggle">要更新选中外观的频道标签。</param>
    private static void UpdateChannelToggleGraphic(Toggle toggle)
    {
        if (toggle == null || toggle.graphic == null)
        {
            return;
        }

        toggle.graphic.gameObject.SetActive(toggle.isOn);
        toggle.graphic.enabled = true;
    }

    /// <summary>根据当前频道的未读数更新提示节点的显示和文案。</summary>
    private void UpdateNewMessageHint()
    {
        if (newMessageHint == null)
        {
            return;
        }

        int index = (int)currentChannel;
        // 当前频道已累计的未读数；非法下标按 0 显示。
        int unreadCount = index >= 0 && index < unreadMessageCounts.Length
            ? unreadMessageCounts[index]
            : 0;
        // 有未读且列表未到底部时，才显示新消息提示。
        bool shouldShow = unreadCount > 0 && !IsAtBottom();
        newMessageHint.gameObject.SetActive(shouldShow);
        if (shouldShow && newMessageHintText != null)
        {
            newMessageHintText.text = unreadCount + "条新消息↓";
        }
        if (shouldShow && legacyNewMessageHintText != null)
        {
            legacyNewMessageHintText.text = unreadCount + "条新消息↓";
        }
    }
    #endregion

    #region 交互响应与网络请求与消息
    /// <summary>滚动变化时重绑最近消息行，到底后清除当前频道未读，再刷新提示。</summary>
    /// <param name="_">ScrollRect 传入的归一化位置，本方法直接读取组件状态而不使用该值。</param>
    private void OnScrollChanged(Vector2 _)
    {
        RefreshPoolForScroll();
        if (IsAtBottom())
        {
            ClearUnreadCount(currentChannel);
        }
        UpdateNewMessageHint();
    }

    /// <summary>设置开关回调，将频道标签显隐变化保存到本地。</summary>
    /// <param name="channel">受影响频道。</param>
    /// <param name="isVisible">是否显示该频道标签。</param>
    private void OnChannelVisibilityChanged(ChatChannel channel, bool isVisible)
    {
        ApplyChannelVisibility(channel, isVisible, true);
    }

    /// <summary>点击提示后滚到当前频道底部，并清除当前频道未读数。</summary>
    private void OnNewMessageHintClicked()
    {
        if (scrollRect == null)
        {
            return;
        }

        ClearUnreadCount(currentChannel);
        StartCoroutine(ScrollToLatestMessage());
    }

    /// <summary>结束编辑时确认焦点仍属于输入框且本帧按下 Enter，再提交输入。</summary>
    /// <param name="_">输入框传来的文本，当前重新读取输入框内容，不使用这个参数。</param>
    private void SubmitOnEndEdit(string _)
    {
        // onEndEdit 也可能由失焦触发，因此仍需确认是当前输入框且按下了 Enter。
        if (!submitWithReturn || EventSystem.current == null)
        {
            return;
        }

        // EventSystem 当前焦点对象，用来区分回车结束编辑与普通失焦。
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        // 优先采用 TMP 输入框，没有时使用旧 UGUI 输入框。
        GameObject inputObject = inputField != null ? inputField.gameObject : legacyInputField != null ? legacyInputField.gameObject : null;
        if (selected == inputObject && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            SendInputMessage();
        }
    }

    /// <summary>旧 UGUI 输入框结束编辑时复用相同的回车判断。</summary>
    /// <param name="_">旧输入框传入的文本。</param>
    private void SubmitOnLegacyEndEdit(string _)
    {
        SubmitOnEndEdit(_);
    }

    /// <summary>读取并修剪输入，非空时加入本地消息，然后清空输入并恢复焦点。</summary>
    public void SendInputMessage()
    {
        // 没有找到输入框时无法发送。
        if (inputField == null && legacyInputField == null)
        {
            return;
        }

        // 去掉首尾空白，空消息不发送。
        string text = GetInputText();
        text = text == null ? string.Empty : text.Trim();
        if (text.Length == 0)
        {
            return;
        }

        // 生成本地消息后清空输入框，并继续保持输入焦点。
        SendLocalMessage(text);
        SetInputText(string.Empty);
        ActivateInputField();
    }

    /// <summary>向当前频道加入本地消息并通知 MessageSent 订阅者；本类不直接访问服务器。</summary>
    /// <param name="text">待发送正文，空白文本忽略，其他文本去掉首尾空白。</param>
    public void SendLocalMessage(string text)
    {
        // 供其他脚本直接调用，用于发送本地玩家消息。
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        // 本地消息加入当前选中的频道，世界频道只是组件的初始默认值。
        ChatMessage message = new ChatMessage
        {
            Channel = currentChannel,
            Sender = localPlayerName,
            Text = text.Trim(),
            IsLocalPlayer = true,
            SentAt = DateTime.Now
        };

        // 同时写入数据列表、创建 UI，并通知外部订阅者。
        AddMessage(message, true);
        MessageSent?.Invoke(message);
    }
    #endregion

    #region 创建与移除
    /// <summary>以 System 为发送者，复用接收入口加入系统频道消息。</summary>
    /// <param name="text">系统提示正文。</param>
    public void AddSystemMessage(string text)
    {
        // 系统消息只是 ReceiveMessage 的快捷写法。
        ReceiveMessage("System", text, ChatChannel.System);
    }

    /// <summary>保存一条消息并限制历史总数，再按频道、滚动位置决定刷新还是累计未读。</summary>
    /// <param name="message">已经填好频道和发送者等字段的消息对象。</param>
    /// <param name="scrollToBottom">本地发送传 true，外部接收传 false。</param>
    private void AddMessage(ChatMessage message, bool scrollToBottom)
    {
        // 先存储消息数据。
        messages.Add(message);
        // 先从全部频道的历史列表删除最早记录；显示行由后续刷新重新绑定，不在这里逐行销毁。
        while (messages.Count > Mathf.Max(1, maxVisibleMessages))
        {
            messages.RemoveAt(0);
        }

        // 本地发送请求滚底；收到当前频道消息时，若用户在看上方记录则只累计未读，不立即重建显示列表。
        if (message.Channel == currentChannel && content != null && scrollRect != null)
        {
            // 刷新前的归一化位置，外部消息需要保持用户当前位置。
            float previousPosition = scrollRect.verticalNormalizedPosition;
            
            bool wasAtBottom = IsAtBottom();//判断当前滚动条是否到底部
            if (!scrollToBottom && !wasAtBottom)
            {
                IncrementUnreadCount(message.Channel);
            }
            else
            {
                RefreshVisibleMessages(scrollToBottom, previousPosition);
            }
           
        }
        else if (!message.IsLocalPlayer)
        {
            // 非当前频道的消息不会刷新列表，但必须保留到该频道自己的未读计数中。
            IncrementUnreadCount(message.Channel);
        }

        UpdateNewMessageHint();
    }

    /// <summary>池数量超过本轮需求时移除并销毁最后一行，否则返回 false。</summary>
    /// <param name="pool">需要缩减的某一种消息行池。</param>
    /// <param name="required">本轮仍需保留的行数。</param>
    private bool RemoveUnusedPoolRow(List<RectTransform> pool, int required)
    {
        if (pool.Count <= required)
        {
            return false;
        }

        // 只移除末尾未被本轮需求占用的行，避免改动正在使用的前部行。
        int last = pool.Count - 1;
        // 从池中拿出的末尾对象，移除记录后销毁它的 GameObject。
        RectTransform row = pool[last];
        pool.RemoveAt(last);
        if (row != null) Destroy(row.gameObject);
        return true;
    }
    #endregion

    #region 查询与判定
    /// <summary>返回频道标签对象的 activeSelf；不检查祖先面板是否激活，也不等同于 Toggle.isOn。</summary>
    /// <param name="channel">需要检查标签显隐的频道。</param>
    public bool IsChannelTabVisible(ChatChannel channel)
    {
        Toggle tab = GetChannelTab(channel);
        return tab != null && tab.gameObject.activeSelf;
    }

    /// <summary>把固定前缀与频道枚举名拼成 PlayerPrefs 键。</summary>
    /// <param name="channel">要保存或读取显隐的频道。</param>
    private static string GetChannelVisibilityPreferenceKey(ChatChannel channel)
    {
        return ChannelVisibilityPreferencePrefix + channel;
    }

    /// <summary>返回 Inspector 中对应频道的标签引用；未知频道返回 null。</summary>
    /// <param name="channel">目标频道编号。</param>
    private Toggle GetChannelTab(ChatChannel channel)
    {
        switch (channel)
        {
            case ChatChannel.World: return worldChannelTab;
            case ChatChannel.Team: return teamChannelTab;
            case ChatChannel.Guild: return guildChannelTab;
            case ChatChannel.Nearby: return nearbyChannelTab;
            case ChatChannel.Private: return privateChannelTab;
            case ChatChannel.Watch: return watchChannelTab;
            case ChatChannel.Profession: return professionChannelTab;
            case ChatChannel.System: return systemChannelTab;
            default: return null;
        }
    }

    /// <summary>按枚举顺序寻找第一个 activeSelf 为 true 的频道标签。</summary>
    /// <param name="channel">找到时输出可见频道；找不到时保留当前频道。</param>
    private bool TryGetFirstVisibleChannel(out ChatChannel channel)
    {
        foreach (ChatChannel candidate in Enum.GetValues(typeof(ChatChannel)))
        {
            Toggle tab = GetChannelTab(candidate);
            if (tab != null && tab.gameObject.activeSelf)
            {
                channel = candidate;
                return true;
            }
        }

        channel = currentChannel;
        return false;
    }

    /// <summary>判断当前滚动条是否已经位于底部。</summary>
    private bool IsAtBottom()
    {
        return scrollRect == null || scrollRect.verticalNormalizedPosition <= 
            BottomScrollThreshold;
    }

    /// <summary>在指定行及其所有子节点中，按对象名称找第一个 TMP 文本。</summary>
    /// <param name="root">搜索起点，包含未激活后代。</param>
    /// <param name="objectName">需要匹配的对象名称。</param>
    private TMP_Text FindTMPText(Transform root, string objectName)
    {
        // 包括未激活的子物体一起查找，便于使用隐藏的模板节点。
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name == objectName)
            {
                return text;
            }
        }

        return null;
    }

    /// <summary>在指定行及其所有子节点中，按名称找旧 UGUI Text，兼容未迁移模板。</summary>
    /// <param name="root">搜索起点，包含未激活后代。</param>
    /// <param name="objectName">需要匹配的对象名称。</param>
    private Text FindLegacyText(Transform root, string objectName)
    {
        foreach (Text text in root.GetComponentsInChildren<Text>(true))
        {
            if (text.name == objectName)
            {
                return text;
            }
        }

        return null;
    }

    /// <summary>按名称查指定节点及其后代，包含未激活对象，返回第一个匹配项。</summary>
    /// <param name="root">搜索起点。</param>
    /// <param name="objectName">目标对象名称。</param>
    private Transform FindNamedTransform(Transform root, string objectName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName)
            {
                return child;
            }
        }

        return null;
    }

    /// <summary>优先读取 TMP，其次读取旧 UGUI 输入框；两者都没有时返回空串。</summary>
    private string GetInputText()
    {
        return inputField != null ? inputField.text : legacyInputField != null ? legacyInputField.text : string.Empty;
    }

    /// <summary>任意一个已配置输入框获得焦点时返回 true。</summary>
    private bool IsInputFocused()
    {
        return (inputField != null && inputField.isFocused) || (legacyInputField != null && legacyInputField.isFocused);
    }

    /// <summary>保留的按名称查组件辅助方法，当前初始化流程没有调用它。</summary>
    /// <typeparam name="T">所需 Unity 组件类型，限定为 Component。</typeparam>
    /// <param name="objectName">组件所在 RectTransform 的对象名称。</param>
    private T FindComponent<T>(string objectName) where T : Component
    {
        // 先按名称找到节点，再从该节点获取指定类型的组件。
        RectTransform target = FindTransform(objectName);
        return target == null ? null : target.GetComponent<T>();
    }

    /// <summary>在当前窗口及其全部后代里找同名 RectTransform，包含未激活对象。</summary>
    /// <param name="objectName">目标对象名称。</param>
    private RectTransform FindTransform(string objectName)
    {
        // 在当前对象及其所有子节点中，按名称查找 RectTransform；true 表示包含未激活节点。
        foreach (RectTransform rectTransform in GetComponentsInChildren<RectTransform>(true))
        {
            if (rectTransform.name == objectName)
            {
                return rectTransform;
            }
        }

        return null;
    }
    #endregion

    #region 核心逻辑
    /// <summary>把外部文本加入指定频道并发出 MessageReceived；正文为空白时忽略。</summary>
    /// <param name="sender">发送者名称，null 转为空串。</param>
    /// <param name="text">收到的正文，去掉首尾空白后保存。</param>
    /// <param name="channel">接收频道，调用方未指定时使用世界频道。</param>
    public void ReceiveMessage(string sender, string text, ChatChannel channel = ChatChannel.World)
    {
        // 供网络层或其他脚本调用，用于显示收到的消息。
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        // 收到的消息默认不是本地玩家发送的。
        ChatMessage message = new ChatMessage
        {
            Channel = channel,
            Sender = sender ?? string.Empty,
            Text = text.Trim(),
            IsLocalPlayer = false,
            SentAt = DateTime.Now
        };

        AddMessage(message, false);
        MessageReceived?.Invoke(message);
    }

    /// <summary>接收带可选文字的语音消息，保存音频引用并显示语音节点，不播放音频。</summary>
    /// <param name="sender">发送者名称，null 转为空串。</param>
    /// <param name="text">可为空的配套正文。</param>
    /// <param name="voiceClip">语音资源；正文与音频都为空时忽略消息。</param>
    /// <param name="channel">消息所属频道，默认世界频道。</param>
    public void ReceiveVoiceMessage(string sender, string text, AudioClip voiceClip, ChatChannel channel = ChatChannel.World)
    {
        if (string.IsNullOrWhiteSpace(text) && voiceClip == null)
        {
            return;
        }

        ChatMessage message = new ChatMessage
        {
            Channel = channel,
            Sender = sender ?? string.Empty,
            Text = text == null ? string.Empty : text.Trim(),
            IsLocalPlayer = false,
            HasVoice = voiceClip != null,
            VoiceClip = voiceClip,
            SentAt = DateTime.Now
        };

        AddMessage(message, false);
        MessageReceived?.Invoke(message);
    }

    /// <summary>设置当前频道并刷新到列表底部；即使选中同一频道也重新刷新。</summary>
    /// <param name="channel">要选中的频道，不在此方法检查标签是否可见。</param>
    public void SelectChannel(ChatChannel channel)
    {
        if (currentChannel == channel)
        {
            SetSelectedChannelToggle(channel);
            RefreshVisibleMessages(true);
            return;
        }

        currentChannel = channel;
        SetSelectedChannelToggle(channel);
        RefreshVisibleMessages(true);
    }

    /// <summary>清空历史和未读数并隐藏行池；保留行实例和当前 visibleMessages 缓存。</summary>
    public void ClearMessages()
    {
        // 清空历史和未读，隐藏池中的行但保留实例；当前没有在此清空 visibleMessages 显示缓存。
        messages.Clear();
        Array.Clear(unreadMessageCounts, 0, unreadMessageCounts.Length);
        if (content == null)
        {
            return;
        }

        HidePooledRows();
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0f);
        UpdateNewMessageHint();
    }

    /// <summary>等一帧布局后把当前显示列表滚到底部，刷新行池并清除当前频道未读；不重建显示数据缓存。</summary>
    private IEnumerator ScrollToLatestMessage()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
        RefreshPoolForScroll();
        ClearUnreadCount(currentChannel);
        UpdateNewMessageHint();
    }

    /// <summary>等一帧后恢复归一化滚动位置，刷新行池并同步未读提示。</summary>
    /// <param name="normalizedPosition">待恢复的位置，限制到 0～1，0 为底部。</param>
    private IEnumerator RestoreScrollPosition(float normalizedPosition)
    {
        yield return null;
        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalizedPosition);
        RefreshPoolForScroll();
        if (IsAtBottom())
        {
            ClearUnreadCount(currentChannel);
        }
        UpdateNewMessageHint();
    }

    /// <summary>按需克隆模板补足某类行池；没有模板时直接返回，单池不超过配置上限。</summary>
    /// <param name="pool">接收新实例的行池。</param>
    /// <param name="template">该类消息的 RectTransform 模板。</param>
    /// <param name="required">本轮显示需要的行数。</param>
    private void EnsurePoolRows(List<RectTransform> pool, RectTransform template, int required)
    {
        if (template == null)
        {
            return;
        }

        int capacity = Mathf.Max(1, pooledMessageRows);
        while (pool.Count < required && pool.Count < capacity)
        {
            // 新克隆的行挂在 Content 下，先隐藏，绑定消息后再显示。
            RectTransform row = Instantiate(template, content, false);
            row.name = "PooledMessageRow_" + pool.Count;
            row.gameObject.SetActive(false);
            // 保留模板原有的自适应高度，让布局系统根据文字内容计算行高。
            ContentSizeFitter fitter = row.GetComponent<ContentSizeFitter>();
            if (fitter != null) fitter.enabled = true;
            pool.Add(row);
        }
    }

    /// <summary>三类池合计超上限时，依次从他人、本地、系统池删除超过本轮需求的末尾行。</summary>
    /// <param name="otherRequired">本轮必须保留的他人行数。</param>
    /// <param name="playerRequired">本轮必须保留的本地行数。</param>
    /// <param name="systemRequired">本轮必须保留的系统行数。</param>
    private void TrimPoolRowsToCapacity(int otherRequired, int playerRequired, int systemRequired)
    {
        int capacity = Mathf.Max(1, pooledMessageRows);
        while (pooledOtherRows.Count + pooledPlayerRows.Count + pooledSystemRows.Count > capacity)
        {
            if (RemoveUnusedPoolRow(pooledOtherRows, otherRequired)) continue;
            if (RemoveUnusedPoolRow(pooledPlayerRows, playerRequired)) continue;
            if (RemoveUnusedPoolRow(pooledSystemRows, systemRequired)) continue;
            break;
        }
    }

    /// <summary>给已配置的提示、设置和输入模式按钮注册事件，默认显示文字输入，并报告缺失引用。</summary>
    private void CacheReferences()
    {
        if (newMessageHintButton != null)
        {
            newMessageHintButton.onClick.AddListener(OnNewMessageHintClicked);
        }
        if (settingButton != null)
        {
            settingButton.onClick.AddListener(ToggleChatSettings);
        }
        if (voiceButton != null)
        {
            voiceButton.onClick.AddListener(ShowVoiceInput);
        }
        if (keyboardButton != null)
        {
            keyboardButton.onClick.AddListener(ShowTextInput);
        }

        // 默认使用文字输入；语音面板只负责模式切换，不实现录音。
        SetVoiceInputMode(false);

        if (inputField == null && legacyInputField == null)
        {
            Debug.LogWarning("ChatWindows requires an Input Field reference in the Inspector.", this);
        }
        if (sendButton == null)
        {
            Debug.LogWarning("ChatWindows requires a Send Button reference in the Inspector.", this);
        }
        if (content == null)
        {
            Debug.LogWarning("ChatWindows requires a Content reference in the Inspector.", this);
        }
        if (newMessageHint == null)
        {
            Debug.LogWarning("ChatWindows requires a New Message Hint reference in the Inspector.", this);
        }
        if (settingButton == null || chatSettingPanel == null)
        {
            Debug.LogWarning("ChatWindows requires Setting Button and Chat Setting Panel references in the Inspector.", this);
        }
    }

    /// <summary>设置面板存在时切换其 activeSelf。</summary>
    private void ToggleChatSettings()
    {
        if (chatSettingPanel != null)
        {
            chatSettingPanel.SetActive(!chatSettingPanel.activeSelf);
        }
    }

    /// <summary>只切换文字与语音区域的显隐，不创建录音或发送流程。</summary>
    /// <param name="showVoiceInput">true 显示语音区域，false 显示文字区域。</param>
    private void SetVoiceInputMode(bool showVoiceInput)
    {
        if (textInputPanel != null)
        {
            textInputPanel.SetActive(!showVoiceInput);
        }
        if (audioPanel != null)
        {
            audioPanel.SetActive(showVoiceInput);
        }
    }

    /// <summary>启用已有布局，以 Content 父节点作为视口，补齐遮罩与滚动组件并设置纵向滚动。</summary>
    private void ConfigureChatList()
    {
        // 下方统一设置嵌套 Canvas 相机的旧代码已停用，当前保留预制体原相机配置。
        /*foreach (Canvas chatCanvas in GetComponentsInChildren<Canvas>(true))
        {
            if (chatCanvas.renderMode == RenderMode.WorldSpace)
            {
                chatCanvas.worldCamera = Camera.main;
            }
        }*/

        if (content == null)
        {
            // 没有配置 Content 引用就不能生成聊天行，记录警告后结束布局准备。
            Debug.LogWarning("ChatController could not find ChatLineGroupContent.", this);
            return;
        }

        // 消息内容必须处于列表同级提示层之上，避免顶部提示遮住刚发送的文字。
        content.SetAsLastSibling();

        // Content 的父节点作为 ScrollRect 的可视区域（Viewport）。
        RectTransform viewport = content.parent as RectTransform;
        if (viewport == null)
        {
            return;
        }

        // 保留预制体原有的布局系统，避免对象池改变消息行的尺寸和对齐方式。
        LayoutGroup layoutGroup = content.GetComponent<LayoutGroup>();
        if (layoutGroup != null)
        {
            layoutGroup.enabled = true;
        }

        // 只启用预制体已有的自适应高度组件，没有时不另建。
        ContentSizeFitter contentFitter = content.GetComponent<ContentSizeFitter>();
        if (contentFitter != null)
        {
            contentFitter.enabled = true;
        }

        // 防止超出可视区域的消息文字显示在聊天窗口外面。
        if (viewport.GetComponent<RectMask2D>() == null)
        {
            viewport.gameObject.AddComponent<RectMask2D>();
        }

        // 下方补建透明滚动接收面的旧代码已停用；当前依赖预制体原有的可射线检测图形。
        /*Image viewportGraphic = viewport.GetComponent<Image>();
        if (viewportGraphic == null)
        {
            viewportGraphic = viewport.gameObject.AddComponent<Image>();
            viewportGraphic.color = Color.clear;
        }
        viewportGraphic.raycastTarget = true;*/

        // 获取或创建滚动组件，并把 Content 和 Viewport 绑定进去。
        scrollRect = viewport.GetComponent<ScrollRect>();
        if (scrollRect == null)
        {
            scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
        }

        // 聊天仅需要纵向滚动；Clamped 表示不能拖动到内容边界之外。
        scrollRect.viewport = viewport;
        scrollRect.content = content;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        // 设置每次滚轮输入带来的滚动量，并保留滑动惯性。
        scrollRect.scrollSensitivity = 40f;
        scrollRect.inertia = true;

        // 没有 GraphicRaycaster 时补上，使 UI 能接收鼠标和触摸事件。
        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    /// <summary>重建频道引用表，为存在的标签注册选择回调，再同步当前选中状态。</summary>
    private void BindChannelToggles()
    {
        channelToggles.Clear();
        channelToggleMap.Clear();
        BindChannelToggle(worldChannelTab, ChatChannel.World);
        BindChannelToggle(teamChannelTab, ChatChannel.Team);
        BindChannelToggle(guildChannelTab, ChatChannel.Guild);
        BindChannelToggle(nearbyChannelTab, ChatChannel.Nearby);
        BindChannelToggle(systemChannelTab, ChatChannel.System);
        BindChannelToggle(watchChannelTab, ChatChannel.Watch);
        BindChannelToggle(privateChannelTab, ChatChannel.Private);
        BindChannelToggle(professionChannelTab, ChatChannel.Profession);
        SetSelectedChannelToggle(currentChannel);
    }

    /// <summary>保存单个频道标签并注册变化回调；只有打开且允许交互时才切换频道。</summary>
    /// <param name="toggle">Inspector 配置的频道标签，缺失时跳过。</param>
    /// <param name="channel">此标签对应的频道。</param>
    private void BindChannelToggle(Toggle toggle, ChatChannel channel)
    {
        if (toggle == null)
        {
            return;
        }

        channelToggleMap[channel] = toggle;
        PrepareChannelToggleGraphic(toggle);
        // 回调保存的当前标签引用，触发时用来更新选中外观和判断是否可交互。
        Toggle capturedToggle = toggle;
        toggle.onValueChanged.AddListener(isOn =>
        {
            UpdateChannelToggleGraphic(capturedToggle);
            if (isOn && capturedToggle.interactable)
            {
                SelectChannel(channel);
            }
        });
        channelToggles.Add(toggle);
    }

    /// <summary>恢复并绑定五个可配置频道的显隐开关。</summary>
    private void BindChannelVisibilitySettings()
    {
        BindChannelVisibilitySetting(worldVisibilityToggle, ChatChannel.World);
        BindChannelVisibilitySetting(guildVisibilityToggle, ChatChannel.Guild);
        BindChannelVisibilitySetting(teamVisibilityToggle, ChatChannel.Team);
        BindChannelVisibilitySetting(nearbyVisibilityToggle, ChatChannel.Nearby);
        BindChannelVisibilitySetting(systemVisibilityToggle, ChatChannel.System);
    }

    /// <summary>恢复单频道 PlayerPrefs 选择，先无事件地更新开关，再监听之后的变化。</summary>
    /// <param name="visibilityToggle">该频道的设置开关。</param>
    /// <param name="channel">该开关控制的频道。</param>
    private void BindChannelVisibilitySetting(Toggle visibilityToggle, ChatChannel channel)
    {
        if (visibilityToggle == null)
        {
            return;
        }

        // 本次绑定的设置开关引用，初始化时不触发它的其他监听。
        Toggle capturedToggle = visibilityToggle;
        // 首次进入游戏时所有可配置频道默认显示；之后恢复玩家上次的选择。
        bool isVisible = PlayerPrefs.GetInt(GetChannelVisibilityPreferenceKey(channel), 1) == 1;
        // 初始化时不触发其他监听，避免加载顺序影响频道标签状态。
        capturedToggle.SetIsOnWithoutNotify(isVisible);
        ApplyChannelVisibility(channel, isVisible, false);
        capturedToggle.onValueChanged.AddListener(isOn => OnChannelVisibilityChanged(channel, isOn));
    }

    /// <summary>状态变化时更新缓存、按需保存 PlayerPrefs、修改频道标签并通知折叠入口。</summary>
    /// <param name="channel">要应用显隐的有效频道编号。</param>
    /// <param name="isVisible">是否显示标签。</param>
    /// <param name="savePreference">用户操作传 true，启动恢复状态时传 false。</param>
    private void ApplyChannelVisibility(ChatChannel channel, bool isVisible, bool savePreference)
    {
        int index = (int)channel;
        if (channelVisibilityStateInitialized[index] && channelVisibilityStates[index] == isVisible)
        {
            return;
        }

        channelVisibilityStateInitialized[index] = true;
        channelVisibilityStates[index] = isVisible;
        if (savePreference)
        {
            PlayerPrefs.SetInt(GetChannelVisibilityPreferenceKey(channel), isVisible ? 1 : 0);
            PlayerPrefs.Save();
        }

        SetChannelTabVisible(channel, isVisible);
        ChannelVisibilityChanged?.Invoke(channel, isVisible);
    }

    /// <summary>修改标签显隐；隐藏当前频道后，尝试按枚举顺序切到第一个仍显示的频道。</summary>
    /// <param name="channel">目标频道。</param>
    /// <param name="visible">标签是否显示。</param>
    private void SetChannelTabVisible(ChatChannel channel, bool visible)
    {
        Toggle tab = GetChannelTab(channel);
        if (tab == null)
        {
            return;
        }

        tab.gameObject.SetActive(visible);
        if (!visible && currentChannel == channel)
        {
            // 当前频道被隐藏后使用的替代频道；找不到时不改变当前频道。
            ChatChannel fallback;
            if (TryGetFirstVisibleChannel(out fallback))
            {
                SelectChannel(fallback);
            }
        }
    }

    /// <summary>不触发 Toggle 事件地同步全部频道选中状态，再更新图形显隐。</summary>
    /// <param name="channel">唯一应处于 isOn 状态的频道。</param>
    private void SetSelectedChannelToggle(ChatChannel channel)
    {
        foreach (KeyValuePair<ChatChannel, Toggle> pair in channelToggleMap)
        {
            Toggle toggle = pair.Value;
            if (toggle == null)
            {
                continue;
            }

            toggle.SetIsOnWithoutNotify(pair.Key == channel);
            UpdateChannelToggleGraphic(toggle);
        }
    }

    /// <summary>准备标签选中图形，禁止它阻挡鼠标射线，并使所在对象随选中状态显示。</summary>
    /// <param name="toggle">需要准备外观的频道标签。</param>
    private static void PrepareChannelToggleGraphic(Toggle toggle)
    {
        if (toggle == null || toggle.graphic == null)
        {
            return;
        }

        // 选中图形所在对象还包含文字，所以整个对象要随 Toggle 显隐，不能只开关 Image 组件。
        toggle.graphic.gameObject.SetActive(toggle.isOn);
        toggle.graphic.raycastTarget = false;
        UpdateChannelToggleGraphic(toggle);
    }

    /// <summary>给复用行填正文、发送者和时间，优先用 TMP、缺失时兼容旧 Text，并设置语音节点显隐。</summary>
    /// <param name="row">本轮选中的复用消息行根节点。</param>
    /// <param name="message">要显示的消息，保持原数据引用不变。</param>
    private void SetTemplateText(Transform row, ChatMessage message)
    {
        // 消息模板的 Bubble 默认关闭。仅激活行根节点不会激活其中的文字，
        // 因此要先确保 TextMessage 到消息行之间的整条父级路径均处于启用状态。
        row.gameObject.SetActive(true);

        // 优先使用 TMP 文本；保留 legacy 文本兜底，兼容尚未迁移的旧模板。
        TMP_Text messageText = FindTMPText(row, "TextMessage");
        Text legacyMessageText = FindLegacyText(row, "TextMessage");
        // TextMessage 是聊天行预制体的固定 TMP 文本节点。
        if (messageText == null && legacyMessageText == null)
        {
            Transform messageTransform = FindNamedTransform(row, "TextMessage");
            if (messageTransform != null)
            {
                messageText = messageTransform.gameObject.GetComponent<TMP_Text>();
            }
        }
        if (messageText == null && legacyMessageText == null)
        {
            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);
            messageText = texts.Length > 0 ? texts[0] : null;
        }

        if (messageText != null)
        {
            ActivatePathToRow(messageText.transform, row);
            messageText.text = message.Text;
            messageText.alignment = message.IsLocalPlayer ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
        }
        else if (legacyMessageText != null)
        {
            ActivatePathToRow(legacyMessageText.transform, row);
            legacyMessageText.text = message.Text;
            legacyMessageText.alignment = message.IsLocalPlayer ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
        }

        // TextPlayerName 为预制体中显示发送者名字的文字节点。
        TMP_Text senderText = FindTMPText(row, "TextPlayerName");
        Text legacySenderText = FindLegacyText(row, "TextPlayerName");
        if (senderText != null)
        {
            senderText.text = message.Sender;
            senderText.gameObject.SetActive(!string.IsNullOrEmpty(message.Sender));
        }
        else if (legacySenderText != null)
        {
            legacySenderText.text = message.Sender;
            legacySenderText.gameObject.SetActive(!string.IsNullOrEmpty(message.Sender));
        }

        // TextCurrentTime 为预制体中显示发送时间的文字节点。
        TMP_Text timeText = FindTMPText(row, "TextCurrentTime");
        Text legacyTimeText = FindLegacyText(row, "TextCurrentTime");
        if (timeText != null)
        {
            timeText.text = message.SentAt.ToString("HH:mm");
        }
        else if (legacyTimeText != null)
        {
            legacyTimeText.text = message.SentAt.ToString("HH:mm");
        }

        // 按 HasVoice 决定 AudioObj 显隐；此处不读取 VoiceClip，也不播放或录制音频。
        Transform audioObject = FindNamedTransform(row, "AudioObj");
        if (audioObject != null)
        {
            audioObject.gameObject.SetActive(message.HasVoice);
        }
    }

    /// <summary>从文字节点沿父级逐层激活，到消息行根节点后停止。</summary>
    /// <param name="target">需要显示的文字节点。</param>
    /// <param name="row">预期的消息行祖先；未遇到时会继续到层级根。</param>
    private static void ActivatePathToRow(Transform target, Transform row)
    {
        for (Transform current = target; current != null; current = current.parent)
        {
            current.gameObject.SetActive(true);
            if (current == row)
            {
                break;
            }
        }
    }

    /// <summary>频道下标有效时把未读数加 1；这里没有额外裁剪计数范围。</summary>
    /// <param name="channel">需要累计未读的频道。</param>
    private void IncrementUnreadCount(ChatChannel channel)
    {
        int index = (int)channel;
        if (index >= 0 && index < unreadMessageCounts.Length)
        {
            unreadMessageCounts[index]++;
        }
    }

    /// <summary>清除指定频道的未读数量。</summary>
    /// <param name="channel">需要清零的频道，其他频道计数保留。</param>
    private void ClearUnreadCount(ChatChannel channel)
    {
        int index = (int)channel;
        if (index >= 0 && index < unreadMessageCounts.Length)
        {
            unreadMessageCounts[index] = 0;
        }
    }

    /// <summary>给所有已配置的输入框写入同一文本。</summary>
    /// <param name="value">新文本，发送完成后传空串。</param>
    private void SetInputText(string value)
    {
        if (inputField != null) inputField.text = value;
        if (legacyInputField != null) legacyInputField.text = value;
    }

    /// <summary>优先激活 TMP 输入框；没有 TMP 时激活旧 UGUI 输入框。</summary>
    private void ActivateInputField()
    {
        if (inputField != null) inputField.ActivateInputField();
        else if (legacyInputField != null) legacyInputField.ActivateInputField();
    }

    /// <summary>保留的整行消息格式化辅助方法，当前没有调用。</summary>
    /// <param name="message">需要拼成单段文本的消息。</param>
    private string FormatMessage(ChatMessage message)
    {
        // 保留的整行文字格式化方式；当前消息模板填充流程没有调用它。
        if (message.Channel == ChatChannel.System)
        {
            return "[System] " + message.Text;
        }

        // 普通消息前缀使用频道枚举名，再拼接发送者和正文。
        string channel = message.Channel.ToString();
        return "[" + channel + "] " + message.Sender + ": " + message.Text;
    }
    #endregion
}
