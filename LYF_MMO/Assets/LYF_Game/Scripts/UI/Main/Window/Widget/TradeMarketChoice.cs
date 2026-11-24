using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>交易窗口的分类、频道或工具标签，保持整块点击与纸纹选中效果一致。</summary>
public sealed class TradeMarketChoice : MonoBehaviour
{
    /// <summary>该标签的实际点击范围。</summary>
    [SerializeField] private Button _button;
    /// <summary>普通底色与选中纸纹共用的图像。</summary>
    [SerializeField] private Image _fill;
    /// <summary>当前标签旁的市场或操作图标。</summary>
    [SerializeField] private Image _symbol;
    /// <summary>显示分类或页签名称。</summary>
    [SerializeField] private Text _caption;
    /// <summary>可展开一级分类的方向提示。</summary>
    [SerializeField] private Image _arrow;
    /// <summary>选中时的纸纹资源。</summary>
    [SerializeField] private Sprite _paper;
    /// <summary>该类标签未选中时的底色。</summary>
    [SerializeField] private Color _idleFill;
    /// <summary>该类标签未选中时的文字和图标颜色。</summary>
    [SerializeField] private Color _idleInk;

    /// <summary>设置文字、选中态和本次点击目标。</summary>
    /// <param name="label">玩家看到的标签。</param>
    /// <param name="selected">是否对应当前频道或分类。</param>
    /// <param name="action">玩家点击后执行的页面操作。</param>
    /// <param name="icon">带图标的固定标签所用的精灵。</param>
    /// <param name="expanded">可展开分类的当前状态，普通标签留空。</param>
    /// <param name="child">二级分类使用深色窄行。</param>
    public void Bind(string label, bool selected, Action action, Sprite icon = null, bool? expanded = null, bool child = false)
    {
        _caption.text = label;
        _fill.sprite = null;
        _fill.color = _idleFill;
        // 未选中、选中及子分类的图文色随同一个标签一起切换。
        Color ink = _idleInk;
        if (child) _fill.color = new Color32(15, 23, 29, 215);
        if (selected)
        {
            _fill.sprite = _paper;
            _fill.color = Color.white;
            ink = new Color32(19, 26, 30, 255);
            if (child)
            {
                _fill.color = new Color32(101, 114, 122, 255);
                ink = Color.white;
            }
        }
        _caption.color = ink;
        if (_symbol != null)
        {
            _symbol.sprite = icon;
            _symbol.enabled = icon != null;
            _symbol.color = ink;
        }
        if (_arrow != null)
        {
            _arrow.enabled = expanded.HasValue;
            _arrow.rectTransform.localRotation = Quaternion.identity;
            if (expanded == true) _arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            _arrow.color = ink;
        }
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(() => action());
    }
}
