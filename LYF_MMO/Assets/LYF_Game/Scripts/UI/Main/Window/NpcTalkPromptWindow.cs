using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>主 HUD 的交谈列表，每位附近人物占一行，超过可见高度后支持滑动。</summary>
public sealed class NpcTalkPromptWindow : WindowBase
{
    /// <summary>主界面右侧的滚动区域，少于四行时按实际人数收起。</summary>
    [SerializeField] private ScrollRect _scroll;
    /// <summary>按人物进入范围的顺序排列按钮，已有按钮保留位置。</summary>
    [SerializeField] private VerticalLayoutGroup _list;
    /// <summary>预制体内默认隐藏的 RO 按钮，决定行高、字体、留白和背景。</summary>
    [SerializeField] private NpcTalkPromptRow _template;
    /// <summary>附近人物离开后留用其按钮，避免巡逻往返时重复实例化。</summary>
    private readonly List<NpcTalkPromptRow> _rows = new List<NpcTalkPromptRow>();
    /// <summary>上次更新的可见人数，人数变化时才调整滚动窗口尺寸。</summary>
    private int _visibleCount;
    /// <summary>把实际点到这一行的 NPC 交给主控制器。</summary>
    internal event Action<NpcEntity> TalkRequested;

    /// <summary>同步附近可交谈者，已在范围中的人继续使用原按钮。</summary>
    /// <param name="speakers">本轮可见的实际人物；空列表或空值时收起整窗。</param>
    internal void SetSpeakers(List<NpcEntity> speakers)
    {
        // 先收回离开的行，使同一轮新增的说话者能够直接复用按钮。
        for (int index = 0; index < _rows.Count; index++)
        {
            // 界面上实际绑定的 NPC 实体，空值表示该行正在等待复用。
            NpcTalkPromptRow row = _rows[index];
            if (row.Speaker != null && (speakers == null || !speakers.Contains(row.Speaker))) row.Bind(null);
        }
        // 单次距离检查中符合条件的人数；无需为没有角色的登录、换图状态创建空列表。
        int count = 0;
        if (speakers != null)
        {
            count = speakers.Count;
            for (int index = 0; index < count; index++) AddSpeaker(speakers[index]);
        }
        if (_visibleCount != count)
        {
            // 按模板真实行高和排版间距计算，四人以上可滚动查看其余所有人。
            int visibleRows = Mathf.Min(4, count);
            float height = visibleRows * ((RectTransform)_template.transform).rect.height +
                Mathf.Max(0, visibleRows - 1) * _list.spacing;
            ((RectTransform)transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            _scroll.vertical = count > visibleRows;
            if (!_scroll.vertical)
            {
                _scroll.StopMovement();
                _scroll.content.anchoredPosition = Vector2.zero;
            }
            _visibleCount = count;
        }
        if (gameObject.activeSelf != (count > 0)) Show(count > 0);
    }

    /// <summary>保持已有人的原行，仅为新出现的人绑定空行或复制正式模板。</summary>
    /// <param name="speaker">通过距离与视线检查的场景人物。</param>
    private void AddSpeaker(NpcEntity speaker)
    {
        // 复用第一个空闲按钮，同时检查该人是否已经在列表中。
        NpcTalkPromptRow available = null;
        for (int index = 0; index < _rows.Count; index++)
        {
            // 正在显示或等待复用的按钮实例。
            NpcTalkPromptRow row = _rows[index];
            if (row.Speaker == speaker) return;
            if (available == null && row.Speaker == null) available = row;
        }
        if (available == null)
        {
            available = Instantiate(_template, _list.transform, false);
            available.TalkRequested += OnRowRequested;
            _rows.Add(available);
        }
        available.transform.SetAsLastSibling();
        available.Bind(speaker);
    }

    /// <summary>把被点击行的人物直接传给导航入口。</summary>
    /// <param name="speaker">被点击行上显示姓名的场景人物。</param>
    private void OnRowRequested(NpcEntity speaker) => TalkRequested?.Invoke(speaker);
}
