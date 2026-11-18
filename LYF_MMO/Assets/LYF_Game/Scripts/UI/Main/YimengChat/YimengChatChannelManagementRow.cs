using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>表示频道管理的一行，通过 Unity EventSystem 要求的 IDragHandler 接收右侧手柄的指针拖动。</summary>
public sealed class YimengChatChannelManagementRow : MonoBehaviour, IDragHandler
{
    /// <summary>本行在管理列表中的矩形，包含眼睛、名称和拖动手柄。</summary>
    [SerializeField] private RectTransform _row;
    /// <summary>与本行一一对应的新版左栏真实条目。</summary>
    [SerializeField] private GameObject _sidebarItem;
    /// <summary>切换本行草稿显隐的眼睛按钮。</summary>
    [SerializeField] private Button _eye;
    /// <summary>根据草稿可见状态显示睁眼或划线眼睛的图像。</summary>
    [SerializeField] private Image _eyeIcon;
    /// <summary>频道当前设为可见时显示的睁眼图标。</summary>
    [SerializeField] private Sprite _visibleIcon;
    /// <summary>频道当前设为隐藏时显示的划线图标。</summary>
    [SerializeField] private Sprite _hiddenIcon;
    /// <summary>本行归属的频道管理面板，接收点击和拖动操作。</summary>
    private YimengChatChannelManagement _management;

    /// <summary>本行在管理列表中参加布局的矩形。</summary>
    internal RectTransform Row => _row;
    /// <summary>被控制显隐和排序的实际左栏对象。</summary>
    internal GameObject SidebarItem => _sidebarItem;
    /// <summary>管理面板当前草稿是否将本频道设为可见。</summary>
    internal bool Visible { get; private set; }

    /// <summary>把眼睛和拖动操作接到同一个管理面板。</summary>
    /// <param name="management">本行所属的频道管理组件。</param>
    internal void Bind(YimengChatChannelManagement management)
    {
        _management = management;
        _eye.onClick.AddListener(ToggleVisibility);
    }

    /// <summary>更改草稿可见状态并显示对应眼睛图标。</summary>
    /// <param name="visible">应用后是否显示左侧频道条目。</param>
    internal void SetVisible(bool visible)
    {
        Visible = visible;
        if (visible)
            _eyeIcon.sprite = _visibleIcon;
        else
            _eyeIcon.sprite = _hiddenIcon;
    }

    /// <summary>将 Unity 手柄拖动的指针位置交给管理面板计算草稿顺序。</summary>
    /// <param name="eventData">EventSystem 当前拖动的指针数据。</param>
    public void OnDrag(PointerEventData eventData)
    {
        _management.MoveRow(this, eventData);
    }

    /// <summary>通过眼睛按钮切换草稿状态。</summary>
    private void ToggleVisibility()
    {
        _management.ToggleVisibility(this);
    }
}
