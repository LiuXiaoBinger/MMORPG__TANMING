using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>在主界面左下角从正式历史提取少量最近消息。</summary>
public sealed class YimengChatCompactFeed : MonoBehaviour
{
    /// <summary>首次打开时最多显示的消息条数。</summary>
    private const int CompactRows = 3;
    /// <summary>含标题栏及三条消息的面板高度，单位为 Canvas 像素。</summary>
    private const float CompactHeight = 154f;
    /// <summary>预制体内每增加一条消息所需的高度，单位为 Canvas 像素。</summary>
    private const float RowHeight = 30f;

    /// <summary>控制速览在三条和全部预设行之间切换。</summary>
    [SerializeField] private Button _more;
    /// <summary>随当前高度切换「更多」和「收起」。</summary>
    [SerializeField] private TMP_Text _moreCaption;
    /// <summary>在同一窗口恢复完整聊天的按钮。</summary>
    [SerializeField] private Button _expand;
    /// <summary>开关速览旁的频道筛选面板。</summary>
    [SerializeField] private Button _settings;
    /// <summary>速览右侧的频道筛选面板，离开速览时收起。</summary>
    [SerializeField] private GameObject _filterPanel;
    /// <summary>收起筛选面板并保留当前选择的按钮。</summary>
    [SerializeField] private Button _closeFilter;
    /// <summary>按 ChatChannel 的连续枚举值排列的频道开关，勾选值随本窗口实例保留。</summary>
    [SerializeField] private Toggle[] _channelFilters;
    /// <summary>交给主界面路由切回旧版聊天。</summary>
    [SerializeField] private Button _legacy;
    /// <summary>无频道选择或没有匹配记录时显示的提示。</summary>
    [SerializeField] private TMP_Text _empty;
    /// <summary>从上往下排列的频道标签，与正文槽位同序。</summary>
    [SerializeField] private TMP_Text[] _channels;
    /// <summary>从上往下排列的单行消息摘要，使用省略号处理过长内容。</summary>
    [SerializeField] private TMP_Text[] _summaries;
    /// <summary>本次打开是否已加高速览区域。</summary>
    private bool _tall;

    /// <summary>绑定由主聊天窗口持有的数据和切换动作。</summary>
    /// <param name="toggleHeight">使用最新正式历史调整本面板的高度。</param>
    /// <param name="expand">恢复当前频道的完整聊天。</param>
    /// <param name="refresh">频道选择改变后按最新的正式历史重绘速览。</param>
    /// <param name="showLegacy">沿主界面窗口路由关闭新版。</param>
    internal void Init(Action toggleHeight, Action expand, Action refresh, Action showLegacy)
    {
        _more.onClick.AddListener(() => toggleHeight());
        _expand.onClick.AddListener(() => expand());
        _settings.onClick.AddListener(ToggleFilter);
        _closeFilter.onClick.AddListener(CloseFilter);
        _legacy.onClick.AddListener(() => showLegacy());

        // 按 ChatChannel 编号绑定各开关，回调直接读取控件已保存的新勾选值。
        for (int i = 0; i < _channelFilters.Length; i++)
            _channelFilters[i].onValueChanged.AddListener(_ => refresh());
    }

    /// <summary>以短面板打开，立即显示已收到的最近消息。</summary>
    /// <param name="history">旧聊天窗口保存的按时间顺序排列的消息。</param>
    /// <param name="emojiPicker">用于识别正式表情正文的预制体表情面板。</param>
    internal void Open(IReadOnlyList<ChatMessage> history, YimengChatEmojiPicker emojiPicker)
    {
        SetHeight(false);
        gameObject.SetActive(true);
        Refresh(history, emojiPicker);
    }

    /// <summary>调整高度后按当前行数重新取最近记录。</summary>
    /// <param name="history">旧聊天窗口持有的实时记录引用。</param>
    /// <param name="emojiPicker">用于获取表情名称的面板绑定。</param>
    internal void ToggleHeight(IReadOnlyList<ChatMessage> history, YimengChatEmojiPicker emojiPicker)
    {
        SetHeight(!_tall);
        Refresh(history, emojiPicker);
    }

    /// <summary>仅刷新固定展示槽位，最近一条放在可见列表底部。</summary>
    /// <param name="history">按到达顺序保存的现有聊天记录。</param>
    /// <param name="emojiPicker">与完整聊天共用的表情格集合。</param>
    internal void Refresh(IReadOnlyList<ChatMessage> history, YimengChatEmojiPicker emojiPicker)
    {
        // 短版最多取三条匹配记录，加高时使用预制体提供的全部行槽。
        int capacity = CompactRows;
        if (_tall) capacity = _summaries.Length;

        // 从最新记录回溯，找到本次要显示的最早匹配位置。
        int start = history.Count;
        // 已找到的匹配条数，用于决定有多少行可见。
        int count = 0;
        while (start > 0 && count < capacity)
        {
            start--;
            if (_channelFilters[(int)history[start].Channel].isOn) count++;
        }

        // 从最早匹配位置向后填充行槽，最新的可见消息排在底部。
        for (int i = 0; i < _summaries.Length; i++)
        {
            // 未分配记录的槽位释放视觉空间。
            bool visible = i < count;
            _channels[i].gameObject.SetActive(visible);
            _summaries[i].gameObject.SetActive(visible);
            if (!visible) continue;

            while (!_channelFilters[(int)history[start].Channel].isOn) start++;
            // 读取同一份只读记录里的原消息，速览只保留显示文字。
            ChatMessage message = history[start++];
            _channels[i].text = YimengChatMessageRow.ChannelCaption(message.Channel);
            _channels[i].color = YimengChatMessageRow.ChannelColor(message.Channel);
            _summaries[i].text = FormatSummary(message, emojiPicker);
        }

        _empty.gameObject.SetActive(count == 0);
        if (count != 0) return;

        _empty.text = "未选择频道";
        // 用首个勾选项区分全部关闭与已选频道还没有消息。
        for (int i = 0; i < _channelFilters.Length; i++)
        {
            if (!_channelFilters[i].isOn) continue;
            _empty.text = "所选频道暂无消息";
            if (history.Count == 0) _empty.text = "暂无消息";
            break;
        }
    }

    /// <summary>离开速览后收起面板，下次进入时继续使用现有频道选择。</summary>
    private void OnDisable()
    {
        CloseFilter();
    }

    /// <summary>在速览旁开关频道筛选，并保持速览当前高度。</summary>
    private void ToggleFilter()
    {
        _filterPanel.SetActive(!_filterPanel.activeSelf);
    }

    /// <summary>关闭筛选面板，保留本窗口内各频道的勾选状态。</summary>
    private void CloseFilter()
    {
        _filterPanel.SetActive(false);
    }

    /// <summary>保持面板底边固定，向上增加能容纳其余消息的高度。</summary>
    /// <param name="tall">为 true 时展开预设的全部消息行。</param>
    private void SetHeight(bool tall)
    {
        _tall = tall;
        // 基础高度包含标题和三条消息，上拉时只增加行占用的距离。
        float height = CompactHeight;
        _moreCaption.text = "更多";
        if (tall)
        {
            height += (_summaries.Length - CompactRows) * RowHeight;
            _moreCaption.text = "收起";
        }
        ((RectTransform)transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }

    /// <summary>生成单行摘要，语音显示时长，表情显示其名称。</summary>
    /// <param name="message">正式历史中的系统或玩家消息。</param>
    /// <param name="emojiPicker">将完整标记映射到表情格的窗口控件。</param>
    private static string FormatSummary(ChatMessage message, YimengChatEmojiPicker emojiPicker)
    {
        // 速览将真实正文中的换行折成空格，避免遮住下一行。
        string body = (message.Text ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');
        if (message.HasVoice && message.Audio != null)
            body = "语音 · " + Mathf.CeilToInt(message.Audio.Duration) + " 秒";
        else if (message.Channel != ChatChannel.System)
        {
            // 与完整聊天一样匹配整条原文，保留普通文字中的标记片段。
            YimengChatEmojiTile emoji = emojiPicker.Find(message.Text);
            if (emoji != null) body = "表情 · " + emoji.Caption;
        }
        if (message.Channel != ChatChannel.System && !string.IsNullOrEmpty(message.Sender))
            return message.Sender + "  " + body;
        return body;
    }
}
