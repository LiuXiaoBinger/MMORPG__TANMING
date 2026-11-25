using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>交易窗口内的商品说明、求购、上架和出价单，编辑期间只修改本地草稿。</summary>
public sealed class TradeMarketDialog : MonoBehaviour
{
    /// <summary>决定窗口标题、价格行和下方操作的单据种类。</summary>
    public enum Form
    {
        Detail, // 展示物品属性和来源。
        Wanted, // 调整求购天数、单价和份数。
        Sell, // 根据包裹中这一格的数量填写上架单。
        Bid // 展示拍品及出价输入。
    }

    /// <summary>消息和物品单据共用的顶栏。</summary>
    [SerializeField] private Text _title;
    /// <summary>包含物品图标和名称的区域。</summary>
    [SerializeField] private GameObject _itemPanel;
    /// <summary>当前商品的真实图标。</summary>
    [SerializeField] private Image _icon;
    /// <summary>商品配置中的中文名。</summary>
    [SerializeField] private Text _itemName;
    /// <summary>商品描述或当前展开的获取途径。</summary>
    [SerializeField] private Text _description;
    /// <summary>数量、天数和价格所在的下半区。</summary>
    [SerializeField] private GameObject _terms;
    /// <summary>有份数的单据显示此行。</summary>
    [SerializeField] private GameObject _quantityRow;
    /// <summary>当前拟发布或出售的数量。</summary>
    [SerializeField] private Text _quantityText;
    /// <summary>求购单的持续天数行。</summary>
    [SerializeField] private GameObject _daysRow;
    /// <summary>天数选择浮层。</summary>
    [SerializeField] private GameObject _daysMenu;
    /// <summary>当前选中的持续天数。</summary>
    [SerializeField] private Text _daysText;
    /// <summary>按单据种类显示的价格标签。</summary>
    [SerializeField] private Text _priceCaption;
    /// <summary>当前拟定的单价或出价。</summary>
    [SerializeField] private Text _priceText;
    /// <summary>银两或银票精灵的显示位置。</summary>
    [SerializeField] private Image _money;
    /// <summary>摆摊使用的银两图标。</summary>
    [SerializeField] private Sprite _silver;
    /// <summary>集市及藏拍使用的银票图标。</summary>
    [SerializeField] private Sprite _ticket;
    /// <summary>提交按钮的说明，服务器未接入时保持不可提交。</summary>
    [SerializeField] private Text _submitText;
    /// <summary>展示本单金额和当前能否提交。</summary>
    [SerializeField] private Text _total;
    /// <summary>无交易记录、无公示或无拍品时的提示。</summary>
    [SerializeField] private GameObject _messagePanel;
    /// <summary>消息页正文。</summary>
    [SerializeField] private Text _messageText;
    /// <summary>藏拍专用面板，承载拍品、剩余时间和我的出价。</summary>
    [SerializeField] private GameObject _bidPanel;
    /// <summary>拍品预览使用的真实图标。</summary>
    [SerializeField] private Image _bidIcon;
    /// <summary>拍品名称与配置所需等级。</summary>
    [SerializeField] private Text _bidItem;
    /// <summary>当前草稿的银票出价。</summary>
    [SerializeField] private Text _bidPrice;
    /// <summary>当前查看的表配置，为来源按钮提供说明。</summary>
    private ItemConfigBase _item;
    /// <summary>当前单据类型。</summary>
    private Form _form;
    /// <summary>当前草稿的份数。</summary>
    private int _quantity;
    /// <summary>当前背包堆叠允许填写的份数，求购按界面上限。</summary>
    private int _maximum;
    /// <summary>当前草稿的一份价格。</summary>
    private int _price;

    /// <summary>当前出售草稿的实时参考价说明，切换单据时清空。</summary>
    private string _stallQuote;

    /// <summary>更新当前出售草稿的市场提示，保留玩家正在编辑的价格。</summary>
    /// <param name="itemId">商品编号，零表示全市场断线。</param><param name="quote">交易服的报价或状态说明。</param>
    public void ShowStallQuote(int itemId, string quote)
    {
        if (!gameObject.activeSelf || !_terms.activeSelf || _form != Form.Sell) return;
        if (itemId != 0 && _item.ItemId != itemId) return;
        _stallQuote = quote;
        RefreshCost();
    }

    /// <summary>在同一遮罩内显示标题与空态说明。</summary>
    /// <param name="heading">来源页面的标题。</param>
    /// <param name="message">当前无记录的原因或规则正文。</param>
    public void Message(string heading, string message)
    {
        _title.text = heading;
        _messageText.text = message;
        _itemPanel.SetActive(false);
        _terms.SetActive(false);
        _daysMenu.SetActive(false);
        _messagePanel.SetActive(true);
        _bidPanel.SetActive(false);
        gameObject.SetActive(true);
    }

    /// <summary>按当前商品打开物品说明或可编辑的单据。</summary>
    /// <param name="form">详情、求购、上架或竞拍。</param>
    /// <param name="item">当前选中的真实物品。</param>
    /// <param name="icon">商品卡片使用的图标。</param>
    /// <param name="maximum">单据份数上限。</param>
    /// <param name="ticket">此入口是否使用银票计价。</param>
    public void Open(Form form, ItemConfigBase item, Sprite icon, int maximum, bool ticket)
    {
        _form = form;
        _stallQuote = null;
        _item = item;
        _maximum = Mathf.Max(1, maximum);
        _quantity = 1;
        _price = 1;
        _icon.sprite = icon;
        _icon.enabled = icon != null;
        _itemName.text = item.ItemName;
        _description.text = item.Description;
        _itemPanel.SetActive(form != Form.Bid);
        _messagePanel.SetActive(false);
        _terms.SetActive(form == Form.Sell || form == Form.Wanted);
        _bidPanel.SetActive(form == Form.Bid);
        if (form == Form.Bid)
        {
            _bidIcon.sprite = icon;
            _bidIcon.enabled = icon != null;
            _bidItem.text = item.ItemName + "\n等级要求：" + item.NeedLevel + "级";
        }
        _daysRow.SetActive(form == Form.Wanted);
        _quantityRow.SetActive(form != Form.Bid);
        SetDays(1);
        _priceCaption.text = "价格";
        _money.sprite = _silver;
        if (ticket) _money.sprite = _ticket;
        switch (form)
        {
            case Form.Detail: _title.text = "物品简介"; break;
            case Form.Wanted: _title.text = "求购"; _submitText.text = "发布求购"; break;
            case Form.Sell: _title.text = "出售"; _submitText.text = "上架"; break;
            case Form.Bid: _title.text = "风雷藏拍"; _submitText.text = "加价"; _priceCaption.text = "我的出价"; break;
        }
        RefreshCost();
        gameObject.SetActive(true);
    }

    /// <summary>切换至选中物品在配置表中的获取途径。</summary>
    public void ToggleSource()
    {
        // 有来源的配置直接呈现来源，无来源时给出实际缺省状态。
        string source = _item.Source;
        if (string.IsNullOrEmpty(source)) source = "暂无获取途径";
        if (_description.text == source) _description.text = _item.Description;
        else _description.text = source;
    }

    /// <summary>展开或收起持续天数的选项。</summary>
    public void ToggleDays() => _daysMenu.SetActive(!_daysMenu.activeSelf);

    /// <summary>使用用户点选的发布天数并收起列表。</summary>
    /// <param name="days">录屏样式选择器中的天数。</param>
    public void SetDays(int days)
    {
        _daysText.text = days + "天";
        _daysMenu.SetActive(false);
    }

    /// <summary>将草稿份数限制在当前货物的可选范围内。</summary>
    /// <param name="delta">每次按钮操作增加或减少一份。</param>
    public void ChangeQuantity(int delta)
    {
        _quantity = Mathf.Clamp(_quantity + delta, 1, _maximum);
        RefreshCost();
    }

    /// <summary>按一单位调整拟填价格。</summary>
    /// <param name="delta">正数加价，负数减价。</param>
    public void ChangePrice(int delta)
    {
        _price = (int)Math.Max(1L, Math.Min(int.MaxValue, (long)_price + delta));
        RefreshCost();
    }

    /// <summary>显示用户当前填写的合计及挂单状态。</summary>
    private void RefreshCost()
    {
        _quantityText.text = _quantity.ToString();
        _priceText.text = _price.ToString();
        _bidPrice.text = _price.ToString();
        _total.text = "合计  " + ((long)_quantity * _price).ToString("N0") + "　 ·　 玩家交易尚未开放";
        if (_form == Form.Sell && !string.IsNullOrEmpty(_stallQuote))
            _total.text = _stallQuote + "\n草稿合计  " + ((long)_quantity * _price).ToString("N0");
        if (_form == Form.Bid) _total.text = "当前暂无有效拍卖挂单";
    }

    /// <summary>返回原先浏览的位置。</summary>
    public void Close() => gameObject.SetActive(false);
}
