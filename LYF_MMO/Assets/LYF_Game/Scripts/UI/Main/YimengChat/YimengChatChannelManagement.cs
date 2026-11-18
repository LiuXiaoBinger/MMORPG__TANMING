using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>管理接收消息页中的频道排序、显隐草稿及本次主界面会话的应用。</summary>
public sealed class YimengChatChannelManagement : MonoBehaviour
{
    /// <summary>接收消息页外侧展开的管理面板，包含滚动列表和固定底栏。</summary>
    [SerializeField] private GameObject _panel;
    /// <summary>右侧独立滚动区，内容容器也是频道管理行的父节点。</summary>
    [SerializeField] private ScrollRect _scroll;
    /// <summary>将草稿应用到新版聊天左栏的按钮。</summary>
    [SerializeField] private Button _apply;
    /// <summary>预制体中的管理行，每行引用新版左栏的一个实际条目。</summary>
    [SerializeField] private YimengChatChannelManagementRow[] _rows;
    /// <summary>当前聊天窗口，应用后负责处理原阅读频道已被隐藏的情况。</summary>
    [SerializeField] private YimengChatWindow _window;
    /// <summary>管理面板的当前草稿顺序，未应用时与大厅左栏的顺序独立。</summary>
    private readonly List<YimengChatChannelManagementRow> _draft = new List<YimengChatChannelManagementRow>();

    /// <summary>首次打开接收消息页时绑定已序列化的控件。</summary>
    private void Awake()
    {
        _apply.onClick.AddListener(Apply);
        // 按预制体提供的行集合完成一次眼睛和拖动事件绑定。
        for (int index = 0; index < _rows.Length; index++)
        {
            _rows[index].Bind(this);
            _draft.Add(_rows[index]);
        }
    }

    /// <summary>接收消息页切出或设置关闭时收起右面板，下次打开重新读取左栏状态。</summary>
    private void OnDisable()
    {
        _panel.SetActive(false);
    }

    /// <summary>由管理频道标题和图标共用，展开时从实际左栏读取当前顺序和显隐。</summary>
    public void Toggle()
    {
        if (_panel.activeSelf)
        {
            _panel.SetActive(false);
            return;
        }

        // 比较的是两个实际左栏条目的层级下标，不沿用未提交的草稿顺序。
        _draft.Sort((left, right) => left.SidebarItem.transform.GetSiblingIndex().CompareTo(right.SidebarItem.transform.GetSiblingIndex()));
        // 逐行同步当前会话已应用的视觉状态。
        for (int index = 0; index < _draft.Count; index++)
        {
            // 当前草稿下标对应的管理行与实际左栏条目。
            YimengChatChannelManagementRow row = _draft[index];
            row.Row.SetSiblingIndex(index);
            row.SetVisible(row.SidebarItem.activeSelf);
        }

        _apply.interactable = true;
        _panel.SetActive(true);
        _scroll.StopMovement();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_scroll.content);
        _scroll.verticalNormalizedPosition = 1f;
    }

    /// <summary>切换一个管理行的草稿可见性，仅在草稿仍保留设置入口时允许应用。</summary>
    /// <param name="row">触发眼睛按钮的实际管理行。</param>
    internal void ToggleVisibility(YimengChatChannelManagementRow row)
    {
        row.SetVisible(!row.Visible);
        _apply.interactable = false;
        // 声演坊和招募没有设置入口，必须留一个能重新打开频道管理的页签。
        for (int index = 0; index < _draft.Count; index++)
        {
            // 当前检查的行需要可见，并对应带齿轮设置的频道页签。
            YimengChatChannelManagementRow candidate = _draft[index];
            // 仅普通和系统页签会保留齿轮设置，招募页使用独立工具栏。
            if (candidate.Visible && candidate.SidebarItem.TryGetComponent(out YimengChatChannelTab tab) && !tab.Recruit)
            {
                _apply.interactable = true;
                return;
            }
        }
    }

    /// <summary>按指针越过管理行的中心线来更新右侧草稿顺序。</summary>
    /// <param name="row">玩家当前抓住的管理行。</param>
    /// <param name="eventData">EventSystem 给出的屏幕坐标和进行 UI 换算的相机。</param>
    internal void MoveRow(YimengChatChannelManagementRow row, PointerEventData eventData)
    {
        // 指针在滚动内容坐标中的位置，纵轴用于判断前后顺序。
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_scroll.content, eventData.position, eventData.pressEventCamera, out Vector2 pointer))
            return;

        // 被拖动的行在当前草稿中的下标。
        int from = row.Row.GetSiblingIndex();
        // 指针越过其他行后应插入的草稿下标。
        int target = from;
        // 由上向下比较每行中心，判断本次拖动越过的最后一个目标位置。
        for (int index = 0; index < _draft.Count; index++)
        {
            if (index == from) continue;
            // 目标行的矩形与换算到滚动内容坐标的中心纵坐标。
            RectTransform item = _draft[index].Row;
            float center = _scroll.content.InverseTransformPoint(item.TransformPoint(item.rect.center)).y;
            if (index < from && pointer.y > center)
            {
                target = index;
                break;
            }
            if (index > from && pointer.y < center) target = index;
        }

        if (target == from) return;
        _draft.RemoveAt(from);
        _draft.Insert(target, row);
        row.Row.SetSiblingIndex(target);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_scroll.content);
    }

    /// <summary>把草稿按当前顺序同步到新版左栏，并处理阅读频道的切换。</summary>
    private void Apply()
    {
        if (!_apply.interactable) return;
        // 应用时才改动正式左栏节点，现有收发和频道标识仍由各自的页签保存。
        for (int index = 0; index < _draft.Count; index++)
        {
            // 草稿中的管理行持有将被更新的左栏对象。
            YimengChatChannelManagementRow row = _draft[index];
            row.SidebarItem.transform.SetSiblingIndex(index);
            row.SidebarItem.SetActive(row.Visible);
        }
        _panel.SetActive(false);
        _window.RefreshManagedSidebar();
    }
}
