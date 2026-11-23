using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>宝石清单与材料栏共用的预制行，主行点击与右侧操作独立接收鼠标。</summary>
public sealed class GemWorkshopLine : MonoBehaviour
{
    /// <summary>占据整行的选择按钮，接收展开或添加操作。</summary>
    [SerializeField] private Button _body;
    /// <summary>品质底色随等级变化的宝石原图。</summary>
    [SerializeField] private Image _icon;
    /// <summary>宝石品种或带等级的完整名称。</summary>
    [SerializeField] private Text _name;
    /// <summary>品种属性、材料来源或商会单价。</summary>
    [SerializeField] private Text _detail;
    /// <summary>右下角显示已选和实际拥有或限购数量。</summary>
    [SerializeField] private Text _count;
    /// <summary>该行展开、镶嵌或减少材料的独立按钮。</summary>
    [SerializeField] private Button _action;
    /// <summary>右侧按钮随行用途切换的短标签。</summary>
    [SerializeField] private Text _actionLabel;

    /// <summary>展开品种底部的一对快捷按钮，普通材料行隐藏。</summary>
    [SerializeField] private GameObject _shortcuts;
    /// <summary>进入该品种的合成目标。</summary>
    [SerializeField] private Button _quickCompose;
    /// <summary>展开该品种的一至三级商会商品。</summary>
    [SerializeField] private Button _quickBuy;

    /// <summary>切换为两个等宽的独立快捷操作。</summary>
    /// <param name="compose">带入当前品种进入合成界面。</param>
    /// <param name="buy">预览该品种的三档可购宝石。</param>
    public void BindShortcuts(Action compose, Action buy)
    {
        _shortcuts.SetActive(true);
        _quickCompose.onClick.RemoveAllListeners();
        _quickBuy.onClick.RemoveAllListeners();
        _quickCompose.onClick.AddListener(() => compose());
        _quickBuy.onClick.AddListener(() => buy());
        gameObject.SetActive(true);
    }

    /// <summary>填入当前数据和两种点击行为，复用时覆盖上一行监听。</summary>
    /// <param name="art">本品种对应等级的物品图。</param>
    /// <param name="title">宝石名称。</param>
    /// <param name="detail">属性、价格或所在容器。</param>
    /// <param name="count">右下角数量文字。</param>
    /// <param name="action">行尾操作名，空字符串隐藏按钮。</param>
    /// <param name="select">点击整行后的行为。</param>
    /// <param name="press">点击行尾控件后的行为。</param>
    public void Bind(Sprite art, string title, string detail, string count, string action, Action select, Action press)
    {
        _shortcuts.SetActive(false);
        _icon.sprite = art;
        _name.text = title;
        _detail.text = detail;
        _count.text = count;
        _body.onClick.RemoveAllListeners();
        if (select != null) _body.onClick.AddListener(() => select());
        _body.interactable = select != null;
        _action.onClick.RemoveAllListeners();
        _action.gameObject.SetActive(action.Length > 0);
        _actionLabel.text = action;
        _action.interactable = press != null;
        if (press != null) _action.onClick.AddListener(() => press());
        gameObject.SetActive(true);
    }
}
