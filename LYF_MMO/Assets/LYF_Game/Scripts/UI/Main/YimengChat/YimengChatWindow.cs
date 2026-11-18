using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>管理江湖聊天的可编辑预制体控件、频道和消息展示。</summary>
public sealed class YimengChatWindow : WindowBase
{
    /// <summary>完整聊天右缘返回速览的收起箭头。</summary>
    [SerializeField] private Button _collapse;
    /// <summary>完整聊天的背景与操作区，进入速览时整体隐藏。</summary>
    [SerializeField] private GameObject _fullChat;
    /// <summary>完整聊天右上角切到只读速览的入口。</summary>
    [SerializeField] private Button _showCompactFeed;
    /// <summary>与完整聊天互斥的左下角近期消息面板。</summary>
    [SerializeField] private YimengChatCompactFeed _compactFeed;
    /// <summary>侧栏内全部频道，按预制体中的顺序列出。</summary>
    [SerializeField] private YimengChatChannelTab[] _channels;
    /// <summary>常用页发言菜单，包含项目已接通和仅展示的频道。</summary>
    [SerializeField] private YimengChatChannelTab[] _sendChoices;
    /// <summary>第一次打开常用页时使用的单服发言目标，对应现有世界频道。</summary>
    [SerializeField] private YimengChatChannelTab _defaultSendChoice;
    /// <summary>消息区的可滚动视口和内容定位。</summary>
    [SerializeField] private ScrollRect _scroll;
    /// <summary>消息列表的可滚动内容容器。</summary>
    [SerializeField] private RectTransform _content;
    /// <summary>普通、系统和语音消息共用的预制体行。</summary>
    [SerializeField] private YimengChatMessageRow _rowPrefab;
    /// <summary>玩家主动锁定阅读位置或恢复自动滚动的按钮。</summary>
    [SerializeField] private Button _scrollMode;
    /// <summary>锁屏状态的按钮文字。</summary>
    [SerializeField] private TMP_Text _scrollModeLabel;
    /// <summary>锁屏状态的图标，可由两张对应样式的图片切换。</summary>
    [SerializeField] private Image _scrollModeIcon;
    /// <summary>浏览旧消息时表示将保持阅读位置的图标。</summary>
    [SerializeField] private Sprite _lockedIcon;
    /// <summary>跟随最新消息时表示滚动模式的图标。</summary>
    [SerializeField] private Sprite _rollingIcon;
    /// <summary>消息区底部的未读条，点击回到最新消息。</summary>
    [SerializeField] private Button _unread;
    /// <summary>未读条显示待查看消息数量。</summary>
    [SerializeField] private TMP_Text _unreadLabel;
    /// <summary>消息区最下方的发言编辑条和发送按钮。</summary>
    [SerializeField] private GameObject _composer;
    /// <summary>普通频道显示的顶部操作栏。</summary>
    [SerializeField] private GameObject _normalTools;
    /// <summary>普通操作栏里可按频道单独显隐的组队按钮。</summary>
    [SerializeField] private GameObject _teamTool;
    /// <summary>普通操作栏里的窗口化按钮；系统频道隐藏这个入口。</summary>
    [SerializeField] private GameObject _windowTool;
    /// <summary>常用页工具栏里打开撩一下设置的入口。</summary>
    [SerializeField] private GameObject _teaseTool;
    /// <summary>招募显示的组队、筛选和副本入口。</summary>
    [SerializeField] private GameObject _recruitTools;
    /// <summary>队伍和帮派频道下方的实时语音外观区域。</summary>
    [SerializeField] private GameObject _groupVoice;
    /// <summary>左下方定制入口与麦克风入口的父对象。</summary>
    [SerializeField] private GameObject _leftComposer;
    /// <summary>当前文本的编辑框，采用现有项目的中文字体。</summary>
    [SerializeField] private TMP_InputField _input;
    /// <summary>底部文字发言目标的标识。</summary>
    [SerializeField] private TMP_Text _sendCaption;
    /// <summary>常用页发言标签的边框、箭头和点击区域，固定频道页隐藏这部分。</summary>
    [SerializeField] private GameObject _sendPickerButton;
    /// <summary>文本模式的发送按钮。</summary>
    [SerializeField] private Button _send;
    /// <summary>用于在文字和已有按住录音模式间切换。</summary>
    [SerializeField] private Button _mic;
    /// <summary>完整聊天录音模式的现有长按控件。</summary>
    [SerializeField] private ChatRecordObj _record;
    /// <summary>速览顶部的麦克风，和完整聊天共用发言频道及录音回调。</summary>
    [SerializeField] private ChatRecordObj _compactRecord;
    /// <summary>设置层的所有分页及显隐容器。</summary>
    [SerializeField] private YimengChatPages _settings;
    /// <summary>完整聊天齿轮的点击边界，保留它自己的设置开关行为。</summary>
    [SerializeField] private RectTransform _settingsTrigger;
    /// <summary>设置右侧独立的频道管理区域，打开时与设置页一起参与点外判断。</summary>
    [SerializeField] private RectTransform _channelManagementPanel;
    /// <summary>底栏笑脸打开的表情选择面板，提供可发送项及广播标记的查找。</summary>
    [SerializeField] private YimengChatEmojiPicker _palette;
    /// <summary>完整聊天底栏笑脸的点击边界，关闭表情时交给按钮自身切换。</summary>
    [SerializeField] private RectTransform _paletteTrigger;
    /// <summary>速览右侧的表情面板挂点，复用完整版弹窗的尺寸与内部布局。</summary>
    [SerializeField] private RectTransform _compactPaletteDock;
    /// <summary>底部发言目标选择菜单。</summary>
    [SerializeField] private GameObject _channelPicker;
    /// <summary>所属根画布，提供弹窗屏幕坐标换算所需的渲染模式和实际相机。</summary>
    private Canvas _rootCanvas;

    /// <summary>报告文字编辑的焦点状态，供游戏输入控制器暂停或恢复操作。</summary>
    public event Action<bool> InputFocusChanged;
    /// <summary>编辑区确认的当前频道及文字，由控制器发出正式请求。</summary>
    public event Action<ChatChannel, string> TextRequested;
    /// <summary>开始在当前发言频道录音；返回设备是否成功开始。</summary>
    public event Func<ChatChannel, bool> VoiceStartRequested;
    /// <summary>结束长按录音，参数表示是否提交成语音留言。</summary>
    public event Action<bool> VoiceEndRequested;
    /// <summary>请求播放消息中的正式语音引用。</summary>
    public event Action<AudioChat> VoicePlayRequested;

    /// <summary>只读引用旧窗口维护的聊天记录，保持唯一记录来源。</summary>
    private IReadOnlyList<ChatMessage> _history = Array.Empty<ChatMessage>();
    /// <summary>当前频道的游戏对象实例，随频道或窗口显隐重建。</summary>
    private readonly List<YimengChatMessageRow> _rows = new List<YimengChatMessageRow>();
    /// <summary>左栏正在阅读的频道。</summary>
    private YimengChatChannelTab _viewed;
    /// <summary>常用页记住的发言选项，切换到固定频道时继续保留。</summary>
    private YimengChatChannelTab _frequentDestination;
    /// <summary>是否由用户锁定消息列表的位置。</summary>
    private bool _locked;
    /// <summary>挂起自动归底时到达的当前频道消息数。</summary>
    private int _pending;
    /// <summary>输入框已通知外部暂停角色输入。</summary>
    private bool _inputHasFocus;

    /// <summary>常用页使用菜单选中的目标，独立频道页按当前页发言。</summary>
    private YimengChatChannelTab PostingTarget
    {
        get
        {
            if (_viewed.ShowAllHistory) return _frequentDestination;
            return _viewed;
        }
    }

    /// <summary>主视图持有窗口后绑定一次正式控件。</summary>
    public override void InitWindow()
    {
        _rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
        _collapse.onClick.AddListener(ShowCompactFeed);
        _showCompactFeed.onClick.AddListener(ShowCompactFeed);
        _compactFeed.Init(() => _compactFeed.ToggleHeight(_history, _palette), RestoreFullChat,
            TogglePalette, ToggleCompactFilter, () => _compactFeed.Refresh(_history, _palette));
        _send.onClick.AddListener(SubmitText);
        _palette.Init(SendEmoji);
        _input.onSubmit.AddListener(OnInputSubmit);
        _input.onSelect.AddListener(OnInputSelect);
        _input.onEndEdit.AddListener(OnInputEndEdit);
        _input.onDeselect.AddListener(OnInputDeselect);
        _mic.onClick.AddListener(ToggleVoice);
        _record.RecordStarted += OnRecordStart;
        _record.RecordEnded += OnRecordFinish;
        _compactRecord.RecordStarted += OnRecordStart;
        _compactRecord.RecordEnded += OnRecordFinish;
        _scrollMode.onClick.AddListener(ToggleScrollMode);
        _unread.onClick.AddListener(GoToLatest);
        // 遍历侧栏按钮，不依赖名称查找频道。
        for (int i = 0; i < _channels.Length; i++)
        {
            // 捕获具体频道项，避免回调读取到变化后的循环索引。
            YimengChatChannelTab tab = _channels[i];
            tab.Button.onClick.AddListener(() => SelectChannel(tab));
        }
        // 逐项绑定常用页菜单，未接入的选项仍可切换标签。
        for (int i = 0; i < _sendChoices.Length; i++)
        {
            // 捕获本行的发言选项和绑定的频道能力。
            YimengChatChannelTab destination = _sendChoices[i];
            destination.Button.onClick.AddListener(() => SelectSendChannel(destination));
        }
        _viewed = _channels[0];
        _frequentDestination = _defaultSendChoice;
    }

    /// <summary>借用旧聊天列表的数据源，打开时由窗口按频道渲染。</summary>
    /// <param name="history">由旧聊天窗口更新的正式消息集合。</param>
    public void UseHistory(IReadOnlyList<ChatMessage> history)
    {
        _history = history;
    }

    /// <summary>点击工具栏齿轮时开关设置，默认显示接收消息分页。</summary>
    public void ToggleSettings()
    {
        // 当前关闭的面板在点击后打开，已开的在同一入口收起。
        bool show = !_settings.gameObject.activeSelf;
        ClosePopups();
        if (show)
        {
            _settings.gameObject.SetActive(true);
            _settings.SelectPage(0);
        }
    }

    /// <summary>显示录屏中撩一下的设置标签页。</summary>
    public void OpenTease()
    {
        ClosePopups();
        _settings.gameObject.SetActive(true);
        _settings.SelectPage(3);
    }

    /// <summary>在当前显示模式下打开原表情面板，该面板默认显示可发的系统表情。</summary>
    public void TogglePalette()
    {
        // 保存本次点击希望触发的显隐动作。
        bool show = !_palette.gameObject.activeSelf;
        ClosePopups();
        if (!show) return;
        // 完整聊天沿用原坐标，速览将同一面板放到消息区右侧。
        Transform paletteParent = transform;
        if (_compactFeed.gameObject.activeSelf) paletteParent = _compactPaletteDock;
        _palette.transform.SetParent(paletteParent, false);
        _palette.Open();
    }

    /// <summary>仅常用页能开关发言目标菜单。</summary>
    public void ToggleSendPicker()
    {
        if (!_viewed.ShowAllHistory) return;
        // 保存本次点击希望触发的显隐动作。
        bool show = !_channelPicker.activeSelf;
        ClosePopups();
        _channelPicker.SetActive(show);
    }

    /// <summary>关闭设置、表情和频道切换等本窗口的临时界面。</summary>
    public void ClosePopups()
    {
        _settings.gameObject.SetActive(false);
        _palette.gameObject.SetActive(false);
        _channelPicker.SetActive(false);
        _compactFeed.CloseFilter();
    }

    /// <summary>监听弹窗打开期间的指针按下，点外仅收起当前弹层。</summary>
    private void Update()
    {
        // 设置和右侧频道管理共用设置层的显隐。
        bool fullPopupOpen = _settings.isActiveAndEnabled || _palette.isActiveAndEnabled || _channelPicker.activeInHierarchy;
        if (!fullPopupOpen && !_compactFeed.FilterBounds.gameObject.activeInHierarchy) return;

        // 优先取本帧开始的触控，坐标单位为屏幕像素。
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
        {
            CloseOnOutsidePress(touchscreen.primaryTouch.position.ReadValue());
            return;
        }
        // 鼠标只在左键按下时检查，抬起仍由原按钮或拖动控件处理。
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            CloseOnOutsidePress(mouse.position.ReadValue());
    }

    /// <summary>在实际画布坐标下判断当前弹窗及其开关，保留原点击的传递。</summary>
    /// <param name="position">本次指针按下时的屏幕像素坐标。</param>
    private void CloseOnOutsidePress(Vector2 position)
    {
        // 覆盖画布以屏幕坐标为准，相机画布使用项目绑定的 UI 相机。
        Camera eventCamera = null;
        if (_rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay) eventCamera = _rootCanvas.worldCamera;

        if (_settings.isActiveAndEnabled)
        {
            // 管理弹窗往右伸出设置边界，只在它打开时算作设置内部。
            bool insideSettings = ContainsPress((RectTransform)_settings.transform, position, eventCamera) ||
                ContainsPress(_channelManagementPanel, position, eventCamera);
            if (insideSettings || ContainsPress(_settingsTrigger, position, eventCamera) ||
                ContainsPress((RectTransform)_teaseTool.transform, position, eventCamera)) return;
        }
        else if (_palette.isActiveAndEnabled)
        {
            if (ContainsPress((RectTransform)_palette.transform, position, eventCamera) ||
                ContainsPress(_paletteTrigger, position, eventCamera) ||
                ContainsPress(_compactFeed.EmojiTrigger, position, eventCamera)) return;
        }
        else if (_channelPicker.activeInHierarchy)
        {
            if (ContainsPress((RectTransform)_channelPicker.transform, position, eventCamera) ||
                ContainsPress((RectTransform)_sendPickerButton.transform, position, eventCamera)) return;
        }
        else if (ContainsPress(_compactFeed.FilterBounds, position, eventCamera) ||
            ContainsPress(_compactFeed.FilterTrigger, position, eventCamera)) return;

        ClosePopups();
    }

    /// <summary>判定有效控件是否包含当前屏幕位置，未显示的另一模式入口不占命中范围。</summary>
    /// <param name="bounds">预制体中弹层或其开关的实际矩形。</param>
    /// <param name="position">指针屏幕坐标，单位为像素。</param>
    /// <param name="eventCamera">相机画布使用的 UI 相机，覆盖画布传空。</param>
    private static bool ContainsPress(RectTransform bounds, Vector2 position, Camera eventCamera)
    {
        return bounds.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(bounds, position, eventCamera);
    }

    /// <summary>将目前显示的频道追加收到的真实消息。</summary>
    /// <param name="message">已存入旧列表的本条聊天消息。</param>
    public void OnMessage(ChatMessage message)
    {
        if (!isActiveAndEnabled) return;
        if (_compactFeed.gameObject.activeSelf)
        {
            _compactFeed.Refresh(_history, _palette);
            return;
        }
        // 非当前频道的消息仍可能使历史达到上限，但不会改变本频道的自动滚动状态。
        bool inChannel = _viewed.Includes(message.Channel);
        // 有效阅读范围内由本人发出的消息，可以在自动滚动模式下直接定位。
        bool fromSelfInChannel = message.IsLocalPlayer && inChannel;
        // 自动滚动只在未锁定且原本处于底部时发生，自己的消息也可直接定位。
        bool follow = !_locked && (_scroll.verticalNormalizedPosition <= 0.02f || fromSelfInChannel);
        // 消息区上沿的当前阅读位置，加入新行后在锁屏期间继续使用。
        float oldY = _content.anchoredPosition.y;
        if (_rows.Count > 0)
        {
            // 首行仍在旧版历史中时保留；超过历史上限后才从本视图移除。
            bool stillStored = false;
            // 逐条比对正式列表引用，新版不单独保存一份历史。
            for (int i = 0; i < _history.Count; i++)
            {
                if (_rows[0].Message != _history[i]) continue;
                stillStored = true;
                break;
            }
            if (!stillStored)
            {
                // 已删除的行在内容顶端，扣减其高度以保留后续已读文字的位置。
                oldY = Mathf.Max(0, oldY - ((RectTransform)_rows[0].transform).rect.height);
                RemoveOldestRow();
            }
        }
        if (inChannel) AddRow(message);
        if (follow) GoToLatest();
        else
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            _content.anchoredPosition = new Vector2(_content.anchoredPosition.x, oldY);
            if (inChannel)
            {
                _pending++;
                RefreshUnread();
            }
        }
    }

    /// <summary>主界面打开此窗口时直接显示短档速览。</summary>
    private void OnEnable()
    {
        ShowCompactFeed();
    }

    /// <summary>退出窗口时释放编辑焦点并收起录音控件、临时弹窗。</summary>
    private void OnDisable()
    {
        ReleaseInputFocus();
        SetVoiceMode(false);
        ClosePopups();
    }

    /// <summary>完整聊天原窗口化和外置按钮将聊天收回左下角速览。</summary>
    public void RequestToggle()
    {
        ShowCompactFeed();
    }

    /// <summary>留在同一个聊天窗口中，交还游戏输入并在左下角展示近期消息。</summary>
    private void ShowCompactFeed()
    {
        ReleaseInputFocus();
        SetVoiceMode(false);
        ClosePopups();
        _fullChat.SetActive(false);
        _compactFeed.Open(_history, _palette);
    }

    /// <summary>按当前阅读频道初始化完整聊天的工具栏、发言目标与消息行。</summary>
    private void RestoreFullChat()
    {
        _compactFeed.gameObject.SetActive(false);
        _fullChat.SetActive(true);
        ApplyChannel();
    }

    /// <summary>速览右侧只显示当前选中的筛选或表情面板。</summary>
    private void ToggleCompactFilter()
    {
        _palette.gameObject.SetActive(false);
        _compactFeed.ToggleFilter();
    }

    /// <summary>切换阅读频道及其配套工具栏和输入状态。</summary>
    /// <param name="tab">点击的左栏频道选项。</param>
    private void SelectChannel(YimengChatChannelTab tab)
    {
        _viewed = tab;
        ApplyChannel();
    }

    /// <summary>应用频道管理后，原阅读频道隐藏时切到左栏首个仍可见的实际页签。</summary>
    internal void RefreshManagedSidebar()
    {
        if (_viewed.gameObject.activeSelf) return;
        // 直接读取应用后的左栏子节点顺序，跳过声演坊和已隐藏的页签。
        Transform sidebar = _viewed.transform.parent;
        // 按目前左栏显示顺序寻找可用的替代页签。
        for (int index = 0; index < sidebar.childCount; index++)
        {
            // 当前子节点包含实际频道页签时才可能被选中。
            Transform item = sidebar.GetChild(index);
            if (!item.gameObject.activeSelf) continue;
            // 第一个仍可见的频道用于刷新消息和固定发言状态。
            if (!item.TryGetComponent(out YimengChatChannelTab tab)) continue;
            _viewed = tab;
            ApplyChannel(false);
            return;
        }
    }

    /// <summary>更换发言目标，保留左栏阅读频道。</summary>
    /// <param name="destination">在常用菜单选择的发言目标，尚未接入的频道只更新显示。</param>
    private void SelectSendChannel(YimengChatChannelTab destination)
    {
        _frequentDestination = destination;
        SetVoiceMode(false);
        RefreshDestination();
        ClosePopups();
    }

    /// <summary>用阅读频道同步页面控件和记录。</summary>
    /// <param name="dismissPopups">正常切页时收起弹层，管理频道应用后保留接收消息页。</param>
    private void ApplyChannel(bool dismissPopups = true)
    {
        // 遍历侧栏把选中态和用于过滤的阅读频道同步。
        for (int i = 0; i < _channels.Length; i++)
            _channels[i].SetSelected(_channels[i] == _viewed);
        if (dismissPopups) ClosePopups();
        SetVoiceMode(false);
        _normalTools.SetActive(!_viewed.Recruit);
        _teamTool.SetActive(!_viewed.ReadOnly);
        _windowTool.SetActive(!_viewed.ReadOnly && !_viewed.ShowAllHistory);
        _teaseTool.SetActive(_viewed.ShowAllHistory);
        _recruitTools.SetActive(_viewed.Recruit);
        _groupVoice.SetActive(_viewed.GroupVoice);
        _composer.SetActive(!_viewed.ReadOnly);
        _leftComposer.SetActive(!_viewed.ReadOnly);
        // 可读区域底部随输入条和帮派、队伍语音栏让出空间。
        float bottom = 0.108f;
        if (_viewed.ReadOnly) bottom = 0.012f;
        else if (_viewed.GroupVoice) bottom = 0.183f;
        // 视口本身采用百分比锚点，适应 16:9 与 16:10 的游戏画面。
        RectTransform scrollRect = (RectTransform)_scroll.transform;
        scrollRect.anchorMin = new Vector2(scrollRect.anchorMin.x, bottom);
        RefreshDestination();
        RebuildRows();
    }

    /// <summary>更新发言目标及控件可用状态。</summary>
    private void RefreshDestination()
    {
        // 本页当前有效目标，用来同步标签、文字和语音控件。
        YimengChatChannelTab destination = PostingTarget;
        _sendPickerButton.SetActive(_viewed.ShowAllHistory);
        _sendCaption.text = "[" + destination.Caption + "]";
        _sendCaption.color = YimengChatMessageRow.ChannelColor(destination.Channel);
        if (_viewed.ShowAllHistory)
            _sendCaption.color = destination.Button.GetComponentInChildren<TMP_Text>(true).color;
        _input.interactable = destination.CanPost;
        _send.interactable = destination.CanPost;
        _mic.interactable = destination.CanPost;
        _palette.SetCanPost(destination.CanPost);
    }

    /// <summary>从正式记录重建本频道的可见行。</summary>
    private void RebuildRows()
    {
        // 从尾部倒序停用旧列表中的渲染节点，以便布局立即刷新。
        for (int i = _rows.Count - 1; i >= 0; i--)
        {
            _rows[i].gameObject.SetActive(false);
            Destroy(_rows[i].gameObject);
        }
        _rows.Clear();
        Canvas.ForceUpdateCanvases();
        // 从最早到最新渲染符合当前频道的记录。
        for (int i = 0; i < _history.Count; i++)
        {
            // 聊天记录由旧窗口保存，这里只为当前可见频道生成视图对象。
            ChatMessage message = _history[i];
            if (_viewed.Includes(message.Channel)) AddRow(message);
        }
        GoToLatest();
    }

    /// <summary>从已保存的预制模板实例化一个消息展示行。</summary>
    /// <param name="message">要绑定的当前频道聊天记录。</param>
    private void AddRow(ChatMessage message)
    {
        // 行仅向列表渲染，消息规格及音频由已有聊天数据携带。
        YimengChatMessageRow row = Instantiate(_rowPrefab, _content, false);
        row.Bind(message, _palette.Find(message.Text), PlayVoice);
        _rows.Add(row);
    }

    /// <summary>将已从旧聊天历史淘汰的最早行从视图移除。</summary>
    private void RemoveOldestRow()
    {
        _rows[0].gameObject.SetActive(false);
        Destroy(_rows[0].gameObject);
        _rows.RemoveAt(0);
    }

    /// <summary>锁定当前阅读位置或重新启用滚动。</summary>
    private void ToggleScrollMode()
    {
        _locked = !_locked;
        if (_locked)
        {
            _scrollModeLabel.text = "锁屏";
            _scrollModeIcon.sprite = _lockedIcon;
        }
        else
        {
            _scrollModeLabel.text = "滚动";
            _scrollModeIcon.sprite = _rollingIcon;
            GoToLatest();
        }
    }

    /// <summary>把滚动区定位到当前频道的最后一条消息。</summary>
    private void GoToLatest()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        _scroll.StopMovement();
        _scroll.verticalNormalizedPosition = 0f;
        _pending = 0;
        RefreshUnread();
    }

    /// <summary>按未读数量更新消息区底部的提示。</summary>
    private void RefreshUnread()
    {
        _unread.gameObject.SetActive(_pending > 0);
        _unreadLabel.text = "你有" + _pending + "条新消息";
    }

    /// <summary>转交回车确认的 TMP 单行输入。</summary>
    /// <param name="value">TMP 当前输入内容；发送按钮共用输入框里的最终值。</param>
    private void OnInputSubmit(string value)
    {
        SubmitText();
    }

    /// <summary>发送输入框中去掉首尾空白后的文字。</summary>
    private void SubmitText()
    {
        // 保留正文中间空白，项目服务端负责最终长度和冷却校验。
        string text = _input.text.Trim();
        // 发言时读取当前页的目标，常用页和固定频道使用各自的选择。
        YimengChatChannelTab destination = PostingTarget;
        if (!destination.CanPost || text.Length == 0) return;
        TextRequested?.Invoke(destination.Channel, text);
        _input.text = string.Empty;
    }

    /// <summary>通过现有文字发言发送完整表情标记，消息行在正式广播后显示图像。</summary>
    /// <param name="marker">表情格配置的完整正文，旧聊天按文字显示。</param>
    private void SendEmoji(string marker)
    {
        // 和文字、语音统一使用当前页发言目标，不改动编辑框已有草稿。
        YimengChatChannelTab destination = PostingTarget;
        if (!destination.CanPost) return;
        TextRequested?.Invoke(destination.Channel, marker);
        _palette.gameObject.SetActive(false);
    }

    /// <summary>TMP 输入进入编辑状态时交出角色操作焦点。</summary>
    /// <param name="value">获得焦点时输入框中的已有草稿。</param>
    private void OnInputSelect(string value)
    {
        SetInputFocus(true);
    }

    /// <summary>按 Enter 或 Esc 结束 TMP 编辑时恢复角色控制并释放原控件的选中状态。</summary>
    /// <param name="value">本次编辑结束时 TMP 保留的文字。</param>
    private void OnInputEndEdit(string value)
    {
        SetInputFocus(false);
        // 鼠标切换选中对象时，EventSystem 已开始处理失焦，无需再切换一次选中对象。
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null || eventSystem.alreadySelecting) return;
        if (eventSystem.currentSelectedGameObject == _input.gameObject)
            eventSystem.SetSelectedGameObject(null);
    }

    /// <summary>从输入框切换到其他控件时恢复角色控制，即使编辑尚未激活。</summary>
    /// <param name="value">离开编辑时输入框中的现有草稿。</param>
    private void OnInputDeselect(string value)
    {
        SetInputFocus(false);
    }

    /// <summary>窗口隐藏或进入速览时结束编辑并恢复游戏操作，保留文本草稿。</summary>
    private void ReleaseInputFocus()
    {
        if (!_inputHasFocus) return;
        _input.DeactivateInputField();
        SetInputFocus(false);
    }

    /// <summary>仅在焦点状态改变时向游戏输入控制器转发。</summary>
    /// <param name="focused">输入框当前是否实际处在编辑态。</param>
    private void SetInputFocus(bool focused)
    {
        if (_inputHasFocus == focused) return;
        _inputHasFocus = focused;
        InputFocusChanged?.Invoke(focused);
    }

    /// <summary>在文本编辑和长按录音模式之间切换。</summary>
    private void ToggleVoice()
    {
        SetVoiceMode(!_record.gameObject.activeSelf);
    }

    /// <summary>控制长按录音和文本编辑的互斥显示。</summary>
    /// <param name="voice">true 时显示长按录音，否则使用文字输入。</param>
    private void SetVoiceMode(bool voice)
    {
        if (voice) _input.DeactivateInputField();
        _input.gameObject.SetActive(!voice);
        _send.gameObject.SetActive(!voice);
        _record.gameObject.SetActive(voice);
    }

    /// <summary>开始在当前发言目标进行长按录音。</summary>
    private bool OnRecordStart()
    {
        // 录音和文字使用相同的发言目标，避免切页后投递到之前的菜单频道。
        YimengChatChannelTab destination = PostingTarget;
        if (!destination.CanPost) return false;
        return VoiceStartRequested?.Invoke(destination.Channel) == true;
    }

    /// <summary>结束本次长按录音。</summary>
    /// <param name="send">抬起时是否要发布已录好的语音。</param>
    private void OnRecordFinish(bool send)
    {
        VoiceEndRequested?.Invoke(send);
    }

    /// <summary>把点击的语音消息交给控制器回放。</summary>
    /// <param name="audio">该消息在服务器签发的语音引用和时长。</param>
    private void PlayVoice(AudioChat audio)
    {
        VoicePlayRequested?.Invoke(audio);
    }
}
