using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>交易窗口中可整块点击的频道、操作页签和商品分类。</summary>
public sealed class TradeMarketChoice : MonoBehaviour
{
    /// <summary>该标签的实际点击范围。</summary>
    [SerializeField] private Button _button;
    /// <summary>绘制当前页签状态的九宫格底板。</summary>
    [SerializeField] private Image _fill;
    /// <summary>当前标签旁的市场或操作图标。</summary>
    [SerializeField] private Image _symbol;
    /// <summary>显示分类或页签名称。</summary>
    [SerializeField] private Text _caption;
    /// <summary>可展开一级分类的方向提示。</summary>
    [SerializeField] private Image _arrow;
    /// <summary>此类页签未选中时的浅色底板。</summary>
    [SerializeField] private Sprite _idlePlate;
    /// <summary>此类页签选中时的蓝色底板。</summary>
    [SerializeField] private Sprite _selectedPlate;

    /// <summary>设置文字、选中态和本次点击目标。</summary>
    /// <param name="label">玩家看到的标签。</param>
    /// <param name="selected">是否对应当前频道或分类。</param>
    /// <param name="action">玩家点击后执行的页面操作。</param>
    /// <param name="icon">带图标的固定标签所用的精灵。</param>
    /// <param name="expanded">可展开分类的当前状态，普通标签留空。</param>
    /// <param name="child">二级分类在未选中时使用浅蓝底，以区分层级。</param>
    public void Bind(string label, bool selected, Action action, Sprite icon = null, bool? expanded = null, bool child = false)
    {
        _caption.text = label;
        _fill.sprite = _idlePlate;
        _fill.color = Color.white;
        // 浅底上使用深蓝字，选中后切换为白字。
        Color ink = new Color32(57, 84, 123, 255);
        if (child)
        {
            _fill.color = new Color32(241, 247, 253, 255);
            if (selected)
            {
                _fill.color = new Color32(182, 217, 255, 255);
                ink = new Color32(36, 76, 122, 255);
            }
        }
        else if (selected)
        {
            _fill.sprite = _selectedPlate;
            ink = Color.white;
        }
        _caption.color = ink;
        if (_symbol != null)
        {
            _symbol.sprite = icon;
            _symbol.enabled = icon != null;
        }
        if (_arrow != null)
        {
            // 一级分类与二级货架共用行模板，缩进只作用于左侧分类标题。
            RectTransform captionArea = (RectTransform)_caption.transform.parent;
            // 相对行左缘的设计像素，同一行复用时恢复所属层级的缩进。
            float inset = 18f;
            if (child) inset = 38f;
            captionArea.offsetMin = new Vector2(inset, captionArea.offsetMin.y);
            _arrow.enabled = expanded.HasValue;
            _arrow.rectTransform.localRotation = Quaternion.identity;
            if (expanded == true) _arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);
        }
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(() => action());
    }
}
