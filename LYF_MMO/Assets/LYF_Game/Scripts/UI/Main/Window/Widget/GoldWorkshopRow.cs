using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>金装作坊左栏的一行，可显示分类、成品或一个独立装备实例。</summary>
public sealed class GoldWorkshopRow : MonoBehaviour
{
    /// <summary>整行的点击区域。</summary>
    [SerializeField] private Button _button;
    /// <summary>有装备图标、部位和等级的物品行。</summary>
    [SerializeField] private GameObject _equipmentLine;
    /// <summary>较矮的折叠分类行，随展开状态显示原版箭头。</summary>
    [SerializeField] private GameObject _categoryLine;
    /// <summary>套装名或部位分类名。</summary>
    [SerializeField] private Text _categoryName;
    /// <summary>收起时向右，展开时向下的分类箭头。</summary>
    [SerializeField] private RectTransform _categoryArrow;
    /// <summary>成品或已拥有的装备名。</summary>
    [SerializeField] private Text _name;
    /// <summary>该部位或实例的等级及阶数。</summary>
    [SerializeField] private Text _detail;
    /// <summary>成品或装备的实际 RO 图标。</summary>
    [SerializeField] private Image _icon;
    /// <summary>当前选中装备的 RO 蓝色选框。</summary>
    [SerializeField] private GameObject _highlight;

    /// <summary>将复用行呈现为 RO 装备格，并绑定本次选择目标。</summary>
    /// <param name="name">行标题。</param>
    /// <param name="detail">装备部位、等级或阶数。</param>
    /// <param name="sprite">装备的物品图标。</param>
    /// <param name="selected">是否为中央预览的条目。</param>
    /// <param name="choose">点击时选择本行对应的装备。</param>
    public void Bind(string name, string detail, Sprite sprite, bool selected, Action choose)
    {
        _equipmentLine.SetActive(true);
        _categoryLine.SetActive(false);
        _name.text = name;
        _detail.text = detail;
        _icon.sprite = sprite;
        _icon.enabled = sprite != null;
        _highlight.SetActive(selected);
        BindClick(choose);
    }

    /// <summary>将复用行切换成紧凑分类标题，旋转箭头表示展开方向。</summary>
    /// <param name="caption">要显示的套装或部位名。</param>
    /// <param name="expanded">此分类下的装备是否正在显示。</param>
    /// <param name="choose">点击时展开或收起此分类。</param>
    public void BindCategory(string caption, bool expanded, Action choose)
    {
        _equipmentLine.SetActive(false);
        _categoryLine.SetActive(true);
        _categoryName.text = caption;
        _categoryArrow.localRotation = Quaternion.identity;
        if (expanded) _categoryArrow.localRotation = Quaternion.Euler(0f, 0f, -90f);
        BindClick(choose);
    }

    /// <summary>把当前行的点击替换为这次绘制对应的回调。</summary>
    /// <param name="choose">选择装备或展开分类的操作。</param>
    private void BindClick(Action choose)
    {
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(() => choose());
        gameObject.SetActive(true);
    }
}
