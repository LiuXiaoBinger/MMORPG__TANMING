using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>养成列表中的一件装备，绑定实例、图标、页签摘要和选中描边。</summary>
public sealed class EquipmentWorkshopRow : MonoBehaviour
{
    /// <summary>覆盖整行的点击区域。</summary>
    [SerializeField] private Button _button;
    /// <summary>异步加载背包中这件装备的图标。</summary>
    [SerializeField] private Image _icon;
    /// <summary>浅色选中底板，叠在常态行底板与图标之间。</summary>
    [SerializeField] private GameObject _highlight;
    /// <summary>装备表中的名称。</summary>
    [SerializeField] private Text _name;
    /// <summary>强化等级或穿戴需求等级，镶嵌页仅列装备名。</summary>
    [SerializeField] private Text _subtitle;
    /// <summary>点击时通知所属窗口切换到本行。</summary>
    private Action<EquipmentWorkshopRow> _select;
    /// <summary>行当前展示的装备实例摘要。</summary>
    public EquipmentViewData Entry { get; private set; }

    /// <summary>此行首次激活时注册按钮，重复绑定数据时沿用监听。</summary>
    private void Awake() { _button.onClick.AddListener(SelectRow); }

    /// <summary>把点击传回窗口，由窗口统一更新行高亮和中间内容。</summary>
    private void SelectRow() { _select(this); }

    /// <summary>将装备实例和列表选择事件绑定到这一行。</summary>
    /// <param name="entry">控制器从背包格或穿戴栏中读取的装备摘要。</param>
    /// <param name="select">所属窗口接收当前行的回调。</param>
    public void Bind(EquipmentViewData entry, Action<EquipmentWorkshopRow> select)
    {
        Entry = entry;
        _select = select;
        _name.text = entry.Name;
        _icon.enabled = false;
        // sprite 为资源系统返回的实际装备图；复用过的行只接受当前条目的图标。
        ResourceMgr.Instance.LoadSpriteAsync(entry.IconPath, sprite =>
        {
            if (Entry != entry) return;
            _icon.sprite = sprite;
            _icon.enabled = sprite != null;
        });
    }

    /// <summary>刷新页签对应的摘要与当前行的选中高亮。</summary>
    /// <param name="page">窗口右侧当前高亮的养成页签。</param>
    /// <param name="selected">这行是否与中央展示的实例相同。</param>
    public void Display(EquipmentWorkshopWindow.Page page, bool selected)
    {
        _highlight.SetActive(selected);
        _subtitle.gameObject.SetActive(page != EquipmentWorkshopWindow.Page.Socket);
        if (page == EquipmentWorkshopWindow.Page.Strengthen)
        {
            _subtitle.color = new Color32(29, 112, 108, 255);
            _subtitle.text = "强化" + Entry.StrengthenLevel;
        }
        else
        {
            _subtitle.color = new Color32(127, 86, 32, 255);
            _subtitle.text = LevelText;
        }
    }

    /// <summary>真实穿戴需求的展示文案。配置为零时显示无等级要求。</summary>
    public string LevelText
    {
        get
        {
            if (Entry.RequiredLevel > 0) return Entry.RequiredLevel + "级可用";
            return "无等级要求";
        }
    }
}
