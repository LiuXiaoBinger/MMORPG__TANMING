using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>金装作坊左栏的一行，可显示分类、成品或一个独立装备实例。</summary>
public sealed class GoldWorkshopRow : MonoBehaviour
{
    /// <summary>整行的点击区域。</summary>
    [SerializeField] private Button _button;
    /// <summary>分类、成品或已装备的物品名。</summary>
    [SerializeField] private Text _name;
    /// <summary>该部位或实例的等级及阶数。</summary>
    [SerializeField] private Text _detail;
    /// <summary>成品或装备的实际 RO 图标。</summary>
    [SerializeField] private Image _icon;
    /// <summary>当前选中实例的蓝色底条。</summary>
    [SerializeField] private GameObject _highlight;

    /// <summary>复用行并把点击目标换为本次列表的分类或实例。</summary>
    /// <param name="name">行标题。</param>
    /// <param name="detail">等级、阶数或分类说明。</param>
    /// <param name="sprite">物品图标，折叠标题留空。</param>
    /// <param name="selected">是否为中央预览的条目。</param>
    /// <param name="choose">点击时选择本行对应的分类或实例。</param>
    public void Bind(string name, string detail, Sprite sprite, bool selected, Action choose)
    {
        _name.text = name;
        _detail.text = detail;
        _icon.sprite = sprite;
        _icon.enabled = sprite != null;
        _highlight.SetActive(selected);
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(() => choose());
        gameObject.SetActive(true);
    }
}
