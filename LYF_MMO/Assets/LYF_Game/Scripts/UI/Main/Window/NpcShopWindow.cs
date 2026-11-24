using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>NPC 商店的双列货架、分类与选购详情，货币和交易数据由 ShopCtrl 提供。</summary>
public class NpcShopWindow : WindowBase
{
    /// <summary>商店打开期间通知控制器处理次数推送。</summary>
    public event Action FrameUpdated;
    /// <summary>玩家按下购买时通知控制器商品及份数。</summary>
    public event Action<ShopGoodsViewData, int> BuyRequested;

    /// <summary>按设计尺寸等比适配屏幕的月纹面板。</summary>
    [SerializeField] private RectTransform _panel;
    /// <summary>当前交互商人的名称。</summary>
    [SerializeField] private Text _title;
    /// <summary>按名称过滤当前页商品的输入框。</summary>
    [SerializeField] private InputField _search;
    /// <summary>当前筛选结果件数。</summary>
    [SerializeField] private Text _count;
    /// <summary>货架滚动区域，切页和更换搜索词时回到顶端。</summary>
    [SerializeField] private ScrollRect _scroll;
    /// <summary>存于预制体的隐藏商品格模板。</summary>
    [SerializeField] private NpcShopGoodsCard _template;
    /// <summary>空商店和搜索无匹配时使用的说明。</summary>
    [SerializeField] private Text _empty;
    /// <summary>仅混售商店显示的右侧分类栏。</summary>
    [SerializeField] private RectTransform _tabs;
    /// <summary>进入杂货货架的页签。</summary>
    [SerializeField] private Button _sundriesTab;
    /// <summary>进入装备货架的页签。</summary>
    [SerializeField] private Button _equipmentTab;
    /// <summary>杂货页签选中时的浅色签面。</summary>
    [SerializeField] private GameObject _sundriesActive;
    /// <summary>装备页签选中时的浅色签面。</summary>
    [SerializeField] private GameObject _equipmentActive;
    /// <summary>右侧购买详情的容器，双栏时占用空出的页签区。</summary>
    [SerializeField] private RectTransform _detail;
    /// <summary>详情顶部的商品图标。</summary>
    [SerializeField] private Image _heroIcon;
    /// <summary>详情内选中商品的名称。</summary>
    [SerializeField] private Text _name;
    /// <summary>所选商品自己的限购说明。</summary>
    [SerializeField] private Text _limit;
    /// <summary>角色包裹中含绑定在内的已有数量。</summary>
    [SerializeField] private Text _owned;
    /// <summary>物品配置给出的详细说明。</summary>
    [SerializeField] private Text _description;
    /// <summary>商品购买后绑定时显示的限制提示。</summary>
    [SerializeField] private Text _binding;
    /// <summary>窗口顶部所选商品对应的钱袋图标。</summary>
    [SerializeField] private Image _walletIcon;
    /// <summary>角色所持该种货币的名称和余额。</summary>
    [SerializeField] private Text _wallet;
    /// <summary>结算金额旁的同币种图标。</summary>
    [SerializeField] private Image _costIcon;
    /// <summary>双栏结算区显示持有量与所需金额。</summary>
    [SerializeField] private Text _cost;
    /// <summary>有分类栏时在购买按钮内显示的支付图标。</summary>
    [SerializeField] private Image _buttonIcon;
    /// <summary>当前购买份数。</summary>
    [SerializeField] private Text _quantityText;
    /// <summary>将购买份数恢复为一份。</summary>
    [SerializeField] private Button _first;
    /// <summary>将购买份数减一。</summary>
    [SerializeField] private Button _minus;
    /// <summary>将购买份数加一。</summary>
    [SerializeField] private Button _plus;
    /// <summary>选取余额与限购允许的最大份数。</summary>
    [SerializeField] private Button _max;
    /// <summary>对当前商品开启多份购买，再点恢复单份。</summary>
    [SerializeField] private Button _batch;
    /// <summary>购买份数大于一时显示在菱形框内的标记。</summary>
    [SerializeField] private GameObject _batchMark;
    /// <summary>提交当前购买数量的主按钮。</summary>
    [SerializeField] private Button _buy;
    /// <summary>购买、售罄、货币不足或已选总价的按钮文字。</summary>
    [SerializeField] private Text _buyText;
    /// <summary>右上角的关闭按钮。</summary>
    [SerializeField] private Button _close;

    /// <summary>已实例化且可重复绑定的货架格，按原始商品顺序保存。</summary>
    private readonly List<NpcShopGoodsCard> _cards = new List<NpcShopGoodsCard>();
    /// <summary>本次实际陈列的格数，超出部分保留给后续打开复用。</summary>
    private int _goodsCount;
    /// <summary>当前展示详情及结算的格子，空页时为空。</summary>
    private NpcShopGoodsCard _selected;
    /// <summary>当前商品的购买份数，切换商品时从一份开始。</summary>
    private int _quantity = 1;
    /// <summary>当前商店同时出售杂货与装备时启用分类栏。</summary>
    private bool _hasTabs;
    /// <summary>有分类栏时当前页是否展示装备。</summary>
    private bool _equipmentPage;

    /// <summary>把预制体上的所有交互接到当前窗口；购买事件由控制器订阅。</summary>
    private void Awake()
    {
        _close.onClick.AddListener(CloseWindow);
        _search.onValueChanged.AddListener(value => FilterGoods(true));
        _sundriesTab.onClick.AddListener(() => ChangePage(false));
        _equipmentTab.onClick.AddListener(() => ChangePage(true));
        _first.onClick.AddListener(() => SetQuantity(1));
        _minus.onClick.AddListener(() => SetQuantity(_quantity - 1));
        _plus.onClick.AddListener(() => SetQuantity(_quantity + 1));
        _max.onClick.AddListener(() => SetQuantity(_selected.Goods.MaxPurchaseCount));
        _batch.onClick.AddListener(ToggleBatch);
        _buy.onClick.AddListener(() => BuyRequested?.Invoke(_selected.Goods, _quantity));
    }

    /// <summary>窗口从隐藏状态打开时，按功能层的实际尺寸设置初始比例。</summary>
    private void OnEnable()
    {
        OnRectTransformDimensionsChange();
    }

    /// <summary>分辨率改变时适配货架与详情的整体比例。</summary>
    private void OnRectTransformDimensionsChange()
    {
        if (_panel == null) return;
        // 窗口根节点覆盖功能层；固定设计面板等比缩放，使宽屏和常规屏都能显示完整购买按钮。
        Rect screen = ((RectTransform)transform).rect;
        float scale = Mathf.Min(screen.width * 0.92f / _panel.sizeDelta.x,
            screen.height * 0.88f / _panel.sizeDelta.y);
        _panel.localScale = new Vector3(scale, scale, 1f);
    }

    /// <summary>向控制器提供原有的可见窗口帧通知。</summary>
    private void Update()
    {
        FrameUpdated?.Invoke();
    }

    /// <summary>绑定商店数据。购买后的刷新保留页签、筛选、选中项和可用份数。</summary>
    /// <param name="title">商人的显示名称。</param>
    /// <param name="goods">控制器按 GoodsId 排好的当前商店商品。</param>
    /// <param name="opening">新开商店时重置上次浏览状态。</param>
    public void SetGoods(string title, List<ShopGoodsViewData> goods, bool opening)
    {
        _title.text = title;
        // 保存商品编号而非复用格的位置，列表变化后仍可定位同件商品。
        int selectedId = -1;
        if (!opening && _selected != null) selectedId = _selected.Goods.GoodsId;
        if (opening)
        {
            _equipmentPage = false;
            _search.SetTextWithoutNotify(string.Empty);
            _quantity = 1;
        }
        // 只为真实存在的商品类型显示页签。
        bool hasSundries = false;
        bool hasEquipment = false;
        _goodsCount = goods.Count;
        _selected = null;
        for (int index = 0; index < goods.Count; index++)
        {
            // 预制体模板在货架外，克隆按商品顺序进入 GridLayoutGroup。
            if (index == _cards.Count)
            {
                NpcShopGoodsCard copy = Instantiate(_template, _scroll.content, false);
                copy.Selected += SelectCard;
                copy.IconsChanged += ShowIcons;
                _cards.Add(copy);
            }
            // 每格在一次数据刷新中只绑定一次，两类商品复用同一模板。
            NpcShopGoodsCard card = _cards[index];
            card.Bind(goods[index]);
            card.SetSelected(false);
            if (goods[index].IsEquipment) hasEquipment = true;
            else hasSundries = true;
            if (goods[index].GoodsId == selectedId) _selected = card;
        }
        for (int index = goods.Count; index < _cards.Count; index++)
            _cards[index].gameObject.SetActive(false);
        _hasTabs = hasEquipment && hasSundries;
        _tabs.gameObject.SetActive(_hasTabs);
        // 页签贴右排列；无页签商店的详情从相同左边界展开。
        Vector2 right = _detail.offsetMax;
        right.x = -44f;
        if (_hasTabs) right.x -= _tabs.rect.width + 20f;
        _detail.offsetMax = right;
        // 分类商店在购买按钮内显示总价，数量栏下移占用原本独立的总价行。
        RectTransform quantityBar = (RectTransform)_quantityText.transform.parent;
        float bottom = 159f;
        if (_hasTabs) bottom = 95f;
        quantityBar.anchoredPosition = new Vector2(quantityBar.anchoredPosition.x, bottom);
        _cost.transform.parent.gameObject.SetActive(!_hasTabs);
        FilterGoods(opening);
    }

    /// <summary>切换分类并从该页首件匹配商品开始显示。</summary>
    /// <param name="equipment">选择装备页时为真，选择杂货页时为假。</param>
    private void ChangePage(bool equipment)
    {
        _equipmentPage = equipment;
        _search.SetTextWithoutNotify(string.Empty);
        FilterGoods(true);
    }

    /// <summary>组合商品名和分类条件，保留仍可见的选中项。</summary>
    /// <param name="scrollToTop">搜索、切页或首次打开时回到货架顶部。</param>
    private void FilterGoods(bool scrollToTop)
    {
        // 首个匹配商品用于切页或旧选择被筛除时的默认详情。
        NpcShopGoodsCard firstMatch = null;
        int visible = 0;
        // 当前页未筛选的货物总数，短货架无需占用一行搜索栏。
        int pageCount = 0;
        string keyword = _search.text.Trim();
        for (int index = 0; index < _goodsCount; index++)
        {
            // 一个商品只在当前分类与名称筛选同时命中时参与布局。
            NpcShopGoodsCard card = _cards[index];
            bool pageMatch = !_hasTabs || card.Goods.IsEquipment == _equipmentPage;
            if (pageMatch) pageCount++;
            bool match = pageMatch && card.Goods.ProductName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
            card.gameObject.SetActive(match);
            if (!match) continue;
            visible++;
            if (firstMatch == null) firstMatch = card;
        }
        _sundriesActive.SetActive(!_equipmentPage);
        _equipmentActive.SetActive(_equipmentPage);
        // 两列四行可直接浏览时，分类商店从详情图标的高度开始陈列。
        bool showSearch = !_hasTabs || pageCount > 8 || keyword.Length > 0;
        _search.gameObject.SetActive(showSearch);
        _count.gameObject.SetActive(showSearch);
        RectTransform shelf = (RectTransform)_scroll.transform;
        float top = 124f;
        if (showSearch) top = 198f;
        shelf.anchoredPosition = new Vector2(shelf.anchoredPosition.x, -top);
        shelf.sizeDelta = new Vector2(shelf.sizeDelta.x, 861f - top);
        _count.text = "共 " + visible + " 件";
        _empty.gameObject.SetActive(firstMatch == null);
        _empty.text = "暂时没有商品";
        if (keyword.Length > 0) _empty.text = "未找到匹配的商品";
        if (_selected == null || !_selected.gameObject.activeSelf) SelectCard(firstMatch);
        else SelectCard(_selected);
        if (scrollToTop)
        {
            _scroll.StopMovement();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_scroll.content);
            _scroll.verticalNormalizedPosition = 1f;
        }
    }

    /// <summary>换选商品后刷新详情，只移动高亮且不直接发出购买。</summary>
    /// <param name="card">玩家点击或筛选后默认显示的格，空页传空。</param>
    private void SelectCard(NpcShopGoodsCard card)
    {
        if (_selected != card)
        {
            if (_selected != null) _selected.SetSelected(false);
            _quantity = 1;
        }
        _selected = card;
        _detail.gameObject.SetActive(card != null);
        _batch.interactable = card != null && card.Goods.MaxPurchaseCount > 1;
        if (card == null)
        {
            _batchMark.SetActive(false);
            _wallet.text = string.Empty;
            _walletIcon.enabled = false;
            return;
        }
        card.SetSelected(true);
        // 展示数据与当前卡片共用同一实例，余额、绑定与限购均跟随选择。
        ShopGoodsViewData goods = card.Goods;
        _name.text = goods.ProductName;
        _limit.text = goods.LimitText;
        _owned.text = "包裹中数量　" + goods.OwnedCount.ToString("N0");
        _description.text = goods.Description;
        _binding.gameObject.SetActive(goods.BindAfterPurchase);
        // 分类商店的按钮留给总价，余额不足时在钱袋旁说明原因。
        string walletName = goods.CurrencyName;
        if (_hasTabs && goods.Balance < goods.Price) walletName += "不足";
        _wallet.text = walletName + "　" + goods.Balance.ToString("N0");
        SetQuantity(_quantity);
        ShowIcons(card);
    }

    /// <summary>详情图与三处价格图取自已绑定卡片，晚到的其他货物图不写入当前详情。</summary>
    /// <param name="card">选择变化或完成图标加载的货架格。</param>
    private void ShowIcons(NpcShopGoodsCard card)
    {
        if (_selected != card) return;
        _heroIcon.sprite = card.ProductSprite;
        _heroIcon.enabled = card.ProductSprite != null;
        _walletIcon.sprite = _costIcon.sprite = _buttonIcon.sprite = card.CurrencySprite;
        _walletIcon.enabled = card.CurrencySprite != null;
        _costIcon.enabled = _walletIcon.enabled;
        _buttonIcon.gameObject.SetActive(_hasTabs && _walletIcon.enabled && card.Goods.RemainingPurchaseCount > 0);
    }

    /// <summary>在当前商品的一份与最大可购份数之间切换。</summary>
    private void ToggleBatch()
    {
        if (_quantity > 1) SetQuantity(1);
        else SetQuantity(_selected.Goods.MaxPurchaseCount);
    }

    /// <summary>按可购上限更新数量、总价、余额提示和按钮可用状态。</summary>
    /// <param name="quantity">玩家希望选择的份数，展示数量至少为一。</param>
    private void SetQuantity(int quantity)
    {
        // 服务端按单价乘以份数扣款，页面展示相同金额。
        ShopGoodsViewData goods = _selected.Goods;
        _quantity = Mathf.Clamp(quantity, 1, Mathf.Max(1, goods.MaxPurchaseCount));
        long total = (long)goods.Price * _quantity;
        _quantityText.text = _quantity.ToString("N0");
        _first.interactable = _minus.interactable = _quantity > 1;
        _max.interactable = _plus.interactable = _quantity < goods.MaxPurchaseCount;
        _batchMark.SetActive(_quantity > 1);
        _cost.text = goods.Balance.ToString("N0") + " / " + total.ToString("N0");
        _buy.interactable = goods.MaxPurchaseCount > 0;
        _buyText.text = "购买";
        if (_hasTabs) _buyText.text = total.ToString("N0");
        if (!_buy.interactable)
        {
            _buyText.text = goods.CurrencyName + "不足";
            if (_hasTabs) _buyText.text = total.ToString("N0");
            if (!goods.IsUnlimitedPurchase && goods.RemainingPurchaseCount == 0) _buyText.text = "已售罄";
        }
    }
}
