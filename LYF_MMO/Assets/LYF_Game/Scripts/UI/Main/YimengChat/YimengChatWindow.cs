using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>管理江湖聊天的可编辑预制体控件、频道和消息展示。</summary>
public sealed class YimengChatWindow : WindowBase
{
    /// <summary>关闭聊天时在主界面底部出现的独立入口。</summary>
    [SerializeField] private Button _launcher;
    /// <summary>聊天右缘的收起箭头。</summary>
    [SerializeField] private Button _collapse;
    /// <summary>侧栏内全部频道，按预制体中的顺序列出。</summary>
    [SerializeField] private YimengChatChannelTab[] _channels;
    /// <summary>可切换为发言目标的三个已有频道。</summary>
    [SerializeField] private YimengChatChannelTab[] _sendChoices;
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
    /// <summary>文本模式的发送按钮。</summary>
    [SerializeField] private Button _send;
    /// <summary>用于在文字和已有按住录音模式间切换。</summary>
    [SerializeField] private Button _mic;
    /// <summary>录音模式的现有长按控件。</summary>
    [SerializeField] private ChatRecordObj _record;
    /// <summary>设置层的所有分页及显隐容器。</summary>
    [SerializeField] private YimengChatPages _settings;
    /// <summary>表情与玩具面板的整体容器。</summary>
    [SerializeField] private GameObject _palette;
    /// <summary>底部发言目标选择菜单。</summary>
    [SerializeField] private GameObject _channelPicker;

    /// <summary>向控制器发出通过主界面窗口路由切换的请求。</summary>
    public event Action ToggleRequested;
    /// <summary>向控制器报告新版是否正显示，供旧窗口切换呈现和点击。</summary>
    public event Action<bool> VisibilityChanged;
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
    /// <summary>底栏正在发消息的频道选项，与左栏阅读态独立。</summary>
    private YimengChatChannelTab _destination;
    /// <summary>是否由用户锁定消息列表的位置。</summary>
    private bool _locked;
    /// <summary>挂起自动归底时到达的当前频道消息数。</summary>
    private int _pending;
    /// <summary>输入框已通知外部暂停角色输入。</summary>
    private bool _inputHasFocus;

    /// <summary>主视图持有窗口后绑定一次正式控件。</summary>
    public override void InitWindow()
    {
        _launcher.onClick.AddListener(RequestToggle);
        _collapse.onClick.AddListener(RequestToggle);
        _send.onClick.AddListener(SubmitText);
        _input.onSubmit.AddListener(OnInputSubmit);
        _input.onSelect.AddListener(OnInputSelect);
        _input.onEndEdit.AddListener(OnInputEndEdit);
        _input.onDeselect.AddListener(OnInputDeselect);
        _mic.onClick.AddListener(ToggleVoice);
        _record.RecordStarted += OnRecordStart;
        _record.RecordEnded += OnRecordFinish;
        _scrollMode.onClick.AddListener(ToggleScrollMode);
        _unread.onClick.AddListener(GoToLatest);
        // 遍历侧栏按钮，不依赖名称查找频道。
        for (int i = 0; i < _channels.Length; i++)
        {
            // 捕获具体频道项，避免回调读取到变化后的循环索引。
            YimengChatChannelTab tab = _channels[i];
            tab.Button.onClick.AddListener(() => SelectChannel(tab));
        }
        // 遍历真实可发言的目标按钮，菜单独立于正在阅读的频道。
        for (int i = 0; i < _sendChoices.Length; i++)
        {
            // 捕获发言菜单中对应的已有频道。
            YimengChatChannelTab destination = _sendChoices[i];
            destination.Button.onClick.AddListener(() => SelectSendChannel(destination));
        }
        _viewed = _channels[0];
        _destination = _sendChoices[0];
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

    /// <summary>定制入口打开常用频道样式的设置分页。</summary>
    public void OpenCustomization()
    {
        ClosePopups();
        _settings.gameObject.SetActive(true);
        _settings.SelectPage(2);
    }

    /// <summary>显示录屏中撩一下的设置标签页。</summary>
    public void OpenTease()
    {
        ClosePopups();
        _settings.gameObject.SetActive(true);
        _settings.SelectPage(3);
    }

    /// <summary>开关表情与玩具预览面板。</summary>
    public void TogglePalette()
    {
        // 保存本次点击希望触发的显隐动作。
        bool show = !_palette.activeSelf;
        ClosePopups();
        _palette.SetActive(show);
    }

    /// <summary>开关真实可发言的频道菜单。</summary>
    public void ToggleSendPicker()
    {
        // 保存本次点击希望触发的显隐动作。
        bool show = !_channelPicker.activeSelf;
        ClosePopups();
        _channelPicker.SetActive(show);
    }

    /// <summary>关闭设置、表情和频道切换等本窗口的临时界面。</summary>
    public void ClosePopups()
    {
        _settings.gameObject.SetActive(false);
        _palette.SetActive(false);
        _channelPicker.SetActive(false);
    }

    /// <summary>将目前显示的频道追加收到的真实消息。</summary>
    /// <param name="message">已存入旧列表的本条聊天消息。</param>
    public void OnMessage(ChatMessage message)
    {
        if (!isActiveAndEnabled) return;
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

    /// <summary>实际显示时恢复窗口状态及当前频道历史，然后告知控制器。</summary>
    private void OnEnable()
    {
        _launcher.gameObject.SetActive(false);
        ApplyChannel();
        VisibilityChanged?.Invoke(true);
    }

    /// <summary>收起时还原入口并释放编辑焦点、临时弹窗和旧窗口遮挡状态。</summary>
    private void OnDisable()
    {
        if (_inputHasFocus)
        {
            _input.DeactivateInputField();
            SetInputFocus(false);
        }
        SetVoiceMode(false);
        ClosePopups();
        _launcher.gameObject.SetActive(true);
        VisibilityChanged?.Invoke(false);
    }

    /// <summary>交由主界面统一切换窗口，不直接修改旧窗口。</summary>
    public void RequestToggle()
    {
        ToggleRequested?.Invoke();
    }

    /// <summary>切换阅读频道及其配套工具栏和输入状态。</summary>
    /// <param name="tab">点击的左栏频道选项。</param>
    private void SelectChannel(YimengChatChannelTab tab)
    {
        _viewed = tab;
        if (!tab.ShowAllHistory) _destination = tab;
        ApplyChannel();
    }

    /// <summary>更换发言目标，保留左栏阅读频道。</summary>
    /// <param name="destination">选择的真实发言目标。</param>
    private void SelectSendChannel(YimengChatChannelTab destination)
    {
        _destination = destination;
        SetVoiceMode(false);
        RefreshDestination();
        ClosePopups();
    }

    /// <summary>用阅读频道同步页面控件和记录。</summary>
    private void ApplyChannel()
    {
        // 遍历侧栏把选中态和用于过滤的阅读频道同步。
        for (int i = 0; i < _channels.Length; i++)
            _channels[i].SetSelected(_channels[i] == _viewed);
        ClosePopups();
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
        _sendCaption.text = "[" + _destination.Caption + "]";
        _sendCaption.color = YimengChatMessageRow.ChannelColor(_destination.Channel);
        _input.interactable = _destination.CanPost;
        _send.interactable = _destination.CanPost;
        _mic.interactable = _destination.CanPost;
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
        row.Bind(message, _scroll.viewport.rect.width, PlayVoice);
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
        if (!_destination.CanPost || text.Length == 0) return;
        TextRequested?.Invoke(_destination.Channel, text);
        _input.text = string.Empty;
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
        if (!_destination.CanPost) return false;
        return VoiceStartRequested?.Invoke(_destination.Channel) == true;
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
