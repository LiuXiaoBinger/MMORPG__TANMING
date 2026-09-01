using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


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
    // TextArea 会让 Unity Inspector 中的文本输入框显示为多行。
    [TextArea] public string Text;
    // true 表示本地玩家发送，用于选择自己的消息预制体。
    public bool IsLocalPlayer;
    // 语音消息可同时显示文字；纯文字消息会隐藏语音节点。
    public bool HasVoice;
    public AudioClip VoiceClip;
    // 消息创建时间。
    public DateTime SentAt;
}

/// <summary>
/// 聊天窗口控制器。
/// 运行时只查找聊天窗口内部的基础控件；消息行模板由 Inspector 引用提供。
/// </summary>
public sealed class ChatWindows : MonoBehaviour
{
    // 最多保留并显示多少条消息。超过时会删除最早的一条。
    [SerializeField] private int maxVisibleMessages = 1000;
    // 本地玩家发送消息时显示的名字。
    [SerializeField] private string localPlayerName = "You";
    // 是否允许在输入框内按 Enter / 小键盘 Enter 发送消息。
    [SerializeField] private bool submitWithReturn = true;
    [SerializeField] private ChatChannel currentChannel = ChatChannel.World;

    // 聊天消息的数据列表。这里的内容可供其他脚本读取。
    private readonly List<ChatMessage> messages = new List<ChatMessage>();
    // 以下引用必须从 Chat 预制体 Inspector 拖拽赋值，避免运行时依赖对象名称。
    [Header("Chat UI References")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private InputField legacyInputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private RectTransform content;
    [SerializeField] private ScrollRect scrollRect;
    // 消息行模板由预制体 Inspector 外部拖拽赋值，避免运行时按名称查找。
    [SerializeField] private RectTransform otherMessageTemplate;
    [SerializeField] private RectTransform playerMessageTemplate;
    [SerializeField] private RectTransform systemMessageTemplate;
    [SerializeField] private int pooledMessageRows = 30;
    private readonly List<Toggle> channelToggles = new List<Toggle>();
    private readonly Dictionary<ChatChannel, Toggle> channelToggleMap = new Dictionary<ChatChannel, Toggle>();
    private readonly List<RectTransform> pooledOtherRows = new List<RectTransform>();
    private readonly List<RectTransform> pooledPlayerRows = new List<RectTransform>();
    private readonly List<RectTransform> pooledSystemRows = new List<RectTransform>();
    private readonly List<ChatMessage> visibleMessages = new List<ChatMessage>();
    // 记录设置面板 Toggle 的最后一次状态。用于兜底同步嵌套预制体中的 Toggle 事件。
    private readonly bool[] channelVisibilityStates = new bool[Enum.GetValues(typeof(ChatChannel)).Length];
    private readonly bool[] channelVisibilityStateInitialized = new bool[Enum.GetValues(typeof(ChatChannel)).Length];
    private bool poolRefreshInProgress;
    private int systemPoolIndex;
    // 每个频道各自保存未读消息数量，切换频道时不会互相覆盖。
    private readonly int[] unreadMessageCounts = new int[Enum.GetValues(typeof(ChatChannel)).Length];
    // 预制体中的“新消息提示”节点及其文字、按钮。
    [SerializeField] private RectTransform newMessageHint;
    [SerializeField] private TMP_Text newMessageHintText;
    [SerializeField] private Text legacyNewMessageHintText;
    [SerializeField] private Button newMessageHintButton;
    [SerializeField] private Button settingButton;
    [SerializeField] private GameObject chatSettingPanel;
    [Header("Input Mode")]
    [SerializeField] private Button voiceButton;
    [SerializeField] private Button keyboardButton;
    [SerializeField] private GameObject textInputPanel;
    [SerializeField] private GameObject audioPanel;
    [Header("Channel Tabs")]
    [SerializeField] private Toggle worldChannelTab;
    [SerializeField] private Toggle guildChannelTab;
    [SerializeField] private Toggle teamChannelTab;
    [SerializeField] private Toggle nearbyChannelTab;
    [SerializeField] private Toggle privateChannelTab;
    [SerializeField] private Toggle watchChannelTab;
    [SerializeField] private Toggle professionChannelTab;
    [SerializeField] private Toggle systemChannelTab;
    [Header("Channel Visibility Settings")]
    [SerializeField] private Toggle worldVisibilityToggle;
    [SerializeField] private Toggle guildVisibilityToggle;
    [SerializeField] private Toggle teamVisibilityToggle;
    [SerializeField] private Toggle nearbyVisibilityToggle;
    [SerializeField] private Toggle systemVisibilityToggle;
    // 小于该值时认为滚动条已经到达底部，避免浮点误差导致提示无法清除。
    private const float BottomScrollThreshold = 0.01f;
    private const string ChannelVisibilityPreferencePrefix = "Chat.ChannelVisible.";
    // 记录上一帧 Enter 是否按下，避免长按时连续发送。
    private bool returnWasPressed;

    // 对外只读地提供消息列表，外部脚本不能直接增删该列表。
    public IReadOnlyList<ChatMessage> Messages => messages;
    public ChatChannel CurrentChannel => currentChannel;
    // 本地玩家消息发送后触发，可在其他脚本中订阅并发送给服务器。
    public event Action<ChatMessage> MessageSent;
    // 接收到外部消息后触发，可供其他系统监听。
    public event Action<ChatMessage> MessageReceived;
    // 聊天设置修改频道可见性后触发，供折叠聊天入口同步未读角标。
    public event Action<ChatChannel, bool> ChannelVisibilityChanged;

    /// <summary>返回频道标签是否在聊天设置中处于启用状态。</summary>
    public bool IsChannelTabVisible(ChatChannel channel)
    {
        Toggle tab = GetChannelTab(channel);
        return tab != null && tab.gameObject.activeSelf;
    }

    private void Awake()
    {
        // 缓存预制体中的 UI 引用，并为消息列表补全必要的布局组件。
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

    private void Start()
    {
        // 某些编辑器驱动的运行模式会延迟加载预制体子节点，因此在 Start 再尝试绑定一次频道按钮。
        if (channelToggles.Count == 0)
        {
            BindChannelToggles();
        }
    }

    private void OnDestroy()
    {
        // 移除监听，避免对象销毁后仍保留无效回调。
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

        bool returnPressed = Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter);
        // 只在按下的第一帧发送，防止按住 Enter 重复发送。
        if (returnPressed && !returnWasPressed)
        {
            SendInputMessage();
        }

        returnWasPressed = returnPressed;
    }

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

    public void SendLocalMessage(string text)
    {
        // 供其他脚本直接调用，用于发送本地玩家消息。
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        // 本地消息默认发送到世界频道。
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

    public void AddSystemMessage(string text)
    {
        // 系统消息只是 ReceiveMessage 的快捷写法。
        ReceiveMessage("System", text, ChatChannel.System);
    }

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

    public void ClearMessages()
    {
        // 同时清除数据列表和消息对象池中的 UI。
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

    private void AddMessage(ChatMessage message, bool scrollToBottom)
    {
        // 先存储消息数据。
        messages.Add(message);
        // 数据超过上限时，始终删除最早的消息和对应的第一行 UI。
        while (messages.Count > Mathf.Max(1, maxVisibleMessages))
        {
            messages.RemoveAt(0);
        }

        // 本地发送才滚动到底部；他人和系统消息保持用户当前滚动位置。
        if (message.Channel == currentChannel && content != null && scrollRect != null)
        {
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

    private IEnumerator ScrollToLatestMessage()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
        RefreshPoolForScroll();
        ClearUnreadCount(currentChannel);
        UpdateNewMessageHint();
    }

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

    private void RefreshVisibleMessages(bool scrollToBottom)
    {
        float previousPosition = scrollRect == null ? 1f : scrollRect.verticalNormalizedPosition;
        RefreshVisibleMessages(scrollToBottom, previousPosition);
    }

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

    private void OnScrollChanged(Vector2 _)
    {
        RefreshPoolForScroll();
        if (IsAtBottom())
        {
            ClearUnreadCount(currentChannel);
        }
        UpdateNewMessageHint();
    }

    private void RefreshPoolForScroll()
    {
        if (content == null || scrollRect == null || poolRefreshInProgress)
        {
            return;
        }

        poolRefreshInProgress = true;
        int poolSize = Mathf.Max(1, pooledMessageRows);
        int totalCount = visibleMessages.Count;
        // 保留原有布局系统，只激活最近的一段消息；完整历史仍保存在 messages 中。
        int firstIndex = Mathf.Max(0, totalCount - poolSize);
        int lastIndex = totalCount;

        int otherRequired = 0;
        int playerRequired = 0;
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

        int otherIndex = 0;
        int playerIndex = 0;
        systemPoolIndex = 0;
        for (int index = firstIndex; index < lastIndex; index++)
        {
            ChatMessage message = visibleMessages[index];
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

    private void EnsurePoolRows(List<RectTransform> pool, RectTransform template, int required)
    {
        if (template == null)
        {
            return;
        }

        int capacity = Mathf.Max(1, pooledMessageRows);
        while (pool.Count < required && pool.Count < capacity)
        {
            RectTransform row = Instantiate(template, content, false);
            row.name = "PooledMessageRow_" + pool.Count;
            row.gameObject.SetActive(false);
            // 保留模板原有的自适应高度，让布局系统根据文字内容计算行高。
            ContentSizeFitter fitter = row.GetComponent<ContentSizeFitter>();
            if (fitter != null) fitter.enabled = true;
            pool.Add(row);
        }
    }

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

    private bool RemoveUnusedPoolRow(List<RectTransform> pool, int required)
    {
        if (pool.Count <= required)
        {
            return false;
        }

        int last = pool.Count - 1;
        RectTransform row = pool[last];
        pool.RemoveAt(last);
        if (row != null) Destroy(row.gameObject);
        return true;
    }

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

    private void ToggleChatSettings()
    {
        if (chatSettingPanel != null)
        {
            chatSettingPanel.SetActive(!chatSettingPanel.activeSelf);
        }
    }

    private void ShowVoiceInput()
    {
        SetVoiceInputMode(true);
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void ShowTextInput()
    {
        SetVoiceInputMode(false);
        ActivateInputField();
    }

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

    private void ConfigureChatList()
    {
        // 预制体内有嵌套 Canvas。它们都必须使用同一台相机，鼠标事件才能正确
        // 从 Game 窗口射到聊天控件。
        /*foreach (Canvas chatCanvas in GetComponentsInChildren<Canvas>(true))
        {
            if (chatCanvas.renderMode == RenderMode.WorldSpace)
            {
                chatCanvas.worldCamera = Camera.main;
            }
        }*/

        if (content == null)
        {
            // Content 不存在时无法生成聊天行，输出警告方便检查预制体命名。
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

        // ScrollRect 只能接收鼠标滚轮事件的前提是指针下方有可射线检测的 Graphic。
        // 聊天列表本身没有背景图时，补一个完全透明的 Image 作为滚动接收面。
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
        // Unity 2023 的 ScrollRect 直接提供鼠标滚轮灵敏度属性。
        scrollRect.scrollSensitivity = 40f;
        scrollRect.inertia = true;

        // 没有 GraphicRaycaster 时补上，使 UI 能接收鼠标和触摸事件。
        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }
    }



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

    private void BindChannelToggle(Toggle toggle, ChatChannel channel)
    {
        if (toggle == null)
        {
            return;
        }

        channelToggleMap[channel] = toggle;
        PrepareChannelToggleGraphic(toggle);
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

    private void BindChannelVisibilitySettings()
    {
        BindChannelVisibilitySetting(worldVisibilityToggle, ChatChannel.World);
        BindChannelVisibilitySetting(guildVisibilityToggle, ChatChannel.Guild);
        BindChannelVisibilitySetting(teamVisibilityToggle, ChatChannel.Team);
        BindChannelVisibilitySetting(nearbyVisibilityToggle, ChatChannel.Nearby);
        BindChannelVisibilitySetting(systemVisibilityToggle, ChatChannel.System);
    }

    private void BindChannelVisibilitySetting(Toggle visibilityToggle, ChatChannel channel)
    {
        if (visibilityToggle == null)
        {
            return;
        }

        Toggle capturedToggle = visibilityToggle;
        // 首次进入游戏时所有可配置频道默认显示；之后恢复玩家上次的选择。
        bool isVisible = PlayerPrefs.GetInt(GetChannelVisibilityPreferenceKey(channel), 1) == 1;
        // 初始化时不触发其他监听，避免加载顺序影响频道标签状态。
        capturedToggle.SetIsOnWithoutNotify(isVisible);
        ApplyChannelVisibility(channel, isVisible, false);
        capturedToggle.onValueChanged.AddListener(isOn => OnChannelVisibilityChanged(channel, isOn));
    }

    private void OnChannelVisibilityChanged(ChatChannel channel, bool isVisible)
    {
        ApplyChannelVisibility(channel, isVisible, true);
    }

    private void SynchronizeChannelVisibilitySettings()
    {
        SynchronizeChannelVisibilitySetting(worldVisibilityToggle, ChatChannel.World);
        SynchronizeChannelVisibilitySetting(guildVisibilityToggle, ChatChannel.Guild);
        SynchronizeChannelVisibilitySetting(teamVisibilityToggle, ChatChannel.Team);
        SynchronizeChannelVisibilitySetting(nearbyVisibilityToggle, ChatChannel.Nearby);
        SynchronizeChannelVisibilitySetting(systemVisibilityToggle, ChatChannel.System);
    }

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

    private static string GetChannelVisibilityPreferenceKey(ChatChannel channel)
    {
        return ChannelVisibilityPreferencePrefix + channel;
    }

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
            ChatChannel fallback;
            if (TryGetFirstVisibleChannel(out fallback))
            {
                SelectChannel(fallback);
            }
        }
    }

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

    private static void PrepareChannelToggleGraphic(Toggle toggle)
    {
        if (toggle == null || toggle.graphic == null)
        {
            return;
        }

        // The selected-state object also contains text, so its parent must follow
        // the Toggle state instead of only enabling the Image component.
        toggle.graphic.gameObject.SetActive(toggle.isOn);
        toggle.graphic.raycastTarget = false;
        UpdateChannelToggleGraphic(toggle);
    }

    private static void UpdateChannelToggleGraphic(Toggle toggle)
    {
        if (toggle == null || toggle.graphic == null)
        {
            return;
        }

        toggle.graphic.gameObject.SetActive(toggle.isOn);
        toggle.graphic.enabled = true;
    }

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

        // 文字消息只显示文本；只有带语音资源的消息才显示 AudioObj。
        Transform audioObject = FindNamedTransform(row, "AudioObj");
        if (audioObject != null)
        {
            audioObject.gameObject.SetActive(message.HasVoice);
        }
    }

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

    /// <summary>判断当前滚动条是否已经位于底部。</summary>
    private bool IsAtBottom()
    {
        return scrollRect == null || scrollRect.verticalNormalizedPosition <= 
            BottomScrollThreshold;
    }

    /// <summary>增加指定频道的未读数量，并限制为非负值。</summary>
    private void IncrementUnreadCount(ChatChannel channel)
    {
        int index = (int)channel;
        if (index >= 0 && index < unreadMessageCounts.Length)
        {
            unreadMessageCounts[index]++;
        }
    }

    /// <summary>清除指定频道的未读数量。</summary>
    private void ClearUnreadCount(ChatChannel channel)
    {
        int index = (int)channel;
        if (index >= 0 && index < unreadMessageCounts.Length)
        {
            unreadMessageCounts[index] = 0;
        }
    }

    /// <summary>根据当前频道的未读数更新提示节点的显示和文案。</summary>
    private void UpdateNewMessageHint()
    {
        if (newMessageHint == null)
        {
            return;
        }

        int index = (int)currentChannel;
        int unreadCount = index >= 0 && index < unreadMessageCounts.Length
            ? unreadMessageCounts[index]
            : 0;
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

    private void SubmitOnEndEdit(string _)
    {
        // onEndEdit 也可能由失焦触发，因此仍需确认是当前输入框且按下了 Enter。
        if (!submitWithReturn || EventSystem.current == null)
        {
            return;
        }

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        GameObject inputObject = inputField != null ? inputField.gameObject : legacyInputField != null ? legacyInputField.gameObject : null;
        if (selected == inputObject && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            SendInputMessage();
        }
    }

    private void SubmitOnLegacyEndEdit(string _)
    {
        SubmitOnEndEdit(_);
    }

    private string GetInputText()
    {
        return inputField != null ? inputField.text : legacyInputField != null ? legacyInputField.text : string.Empty;
    }

    private void SetInputText(string value)
    {
        if (inputField != null) inputField.text = value;
        if (legacyInputField != null) legacyInputField.text = value;
    }

    private void ActivateInputField()
    {
        if (inputField != null) inputField.ActivateInputField();
        else if (legacyInputField != null) legacyInputField.ActivateInputField();
    }

    private bool IsInputFocused()
    {
        return (inputField != null && inputField.isFocused) || (legacyInputField != null && legacyInputField.isFocused);
    }

    private string FormatMessage(ChatMessage message)
    {
        // 保底文字行需要自行把频道、发送者和消息内容拼接成一段文字。
        if (message.Channel == ChatChannel.System)
        {
            return "[System] " + message.Text;
        }

        string channel = message.Channel.ToString();
        return "[" + channel + "] " + message.Sender + ": " + message.Text;
    }

    private T FindComponent<T>(string objectName) where T : Component
    {
        // 先按名称找到节点，再从该节点获取指定类型的组件。
        RectTransform target = FindTransform(objectName);
        return target == null ? null : target.GetComponent<T>();
    }

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
}
