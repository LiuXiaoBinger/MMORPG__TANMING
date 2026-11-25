using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Market = TradeMarketCatalog.Market;
using Shelf = TradeMarketCatalog.Shelf;

/// <summary>主城交易窗口，呈现本服与全服目录、玩家包裹及风雷拍场的浏览流程。</summary>
public sealed class TradeMarketWindow : WindowBase
{
    /// <summary>按录屏 1328×748 参考尺寸排版的内容层。</summary>
    [SerializeField] private RectTransform _layout;
    /// <summary>设计配置的分类、物品编号和原图索引。</summary>
    [SerializeField] private TradeMarketCatalog _catalog;
    /// <summary>当前打开窗口所使用的搜索、关注和求购入口设置。</summary>
    private TradeMarketVisibility _visibility;
    /// <summary>已应用一级与二级显示开关的有序货架。</summary>
    private List<Shelf> _visibleShelves;
    /// <summary>右侧六个市场按钮，顺序对应 Market。</summary>
    [SerializeField] private TradeMarketChoice[] _markets;
    /// <summary>六个右侧市场印记。</summary>
    [SerializeField] private Sprite[] _marketArt;
    /// <summary>四种顶部操作的按钮位置。</summary>
    [SerializeField] private TradeMarketChoice[] _tabs;
    /// <summary>购买、出售、求购、公示、排行及关注的操作印记。</summary>
    [SerializeField] private Sprite[] _tabArt;
    /// <summary>顶部操作条与交易记录入口。</summary>
    [SerializeField] private GameObject _toolbar;
    /// <summary>按铜币、银两、银票和元宝排列的余额。</summary>
    [SerializeField] private Text[] _wallet;
    /// <summary>带左侧分类和中部商品的浏览页。</summary>
    [SerializeField] private GameObject _browse;
    /// <summary>拍卖挂单全宽显示时收起的分类栏。</summary>
    [SerializeField] private GameObject _sidebar;
    /// <summary>目录与挂单共用的主区域，藏拍深一层时占满横向空间。</summary>
    [SerializeField] private RectTransform _shelfPanel;
    /// <summary>搜索本频道全部物品的输入框。</summary>
    [SerializeField] private InputField _search;
    /// <summary>左侧独立滚动的分类列表。</summary>
    [SerializeField] private ScrollRect _categories;
    /// <summary>一级与二级分类复用的正式模板。</summary>
    [SerializeField] private TradeMarketChoice _categoryTemplate;
    /// <summary>商品双列滚动区域。</summary>
    [SerializeField] private ScrollRect _goods;
    /// <summary>控制两列卡片宽度与间距。</summary>
    [SerializeField] private GridLayoutGroup _goodsGrid;
    /// <summary>带关注和求购入口的正式商品模板。</summary>
    [SerializeField] private TradeMarketCard _cardTemplate;
    /// <summary>无货品与无关注时的插画及说明。</summary>
    [SerializeField] private GameObject _empty;
    /// <summary>与当前浏览状态相符的空态文案。</summary>
    [SerializeField] private Text _emptyLabel;
    /// <summary>商品等级筛选按钮。</summary>
    [SerializeField] private GameObject _levelButton;
    /// <summary>当前等级区间。</summary>
    [SerializeField] private Text _levelText;
    /// <summary>可点选的等级筛选菜单。</summary>
    [SerializeField] private GameObject _levelMenu;
    /// <summary>跨服目录开关。</summary>
    [SerializeField] private GameObject _crossButton;
    /// <summary>跨服开关文字旁的选中标记。</summary>
    [SerializeField] private GameObject _crossMark;
    /// <summary>点击商品后显示该商品的挂单列表。</summary>
    [SerializeField] private GameObject _listings;
    /// <summary>挂单页最上方可点开说明的商品。</summary>
    [SerializeField] private TradeMarketCard _listedCard;
    /// <summary>挂单页的实际空状态。</summary>
    [SerializeField] private Text _listedEmpty;
    /// <summary>挂单页底部的单据入口标题。</summary>
    [SerializeField] private Text _draftLabel;
    /// <summary>拍卖挂单页独有的批量出价和排序区域。</summary>
    [SerializeField] private GameObject _auctionTools;
    /// <summary>当前按价格或时间排列拍品。</summary>
    [SerializeField] private Text _auctionSort;
    /// <summary>摆摊与集市出售页，左边摊位右边包裹。</summary>
    [SerializeField] private GameObject _sell;
    /// <summary>包裹中的真实物品按四列陈列。</summary>
    [SerializeField] private ScrollRect _bag;
    /// <summary>按包裹宽度设置四列方格。</summary>
    [SerializeField] private GridLayoutGroup _bagGrid;
    /// <summary>包裹物品的正方形格子模板。</summary>
    [SerializeField] private TradeMarketCard _bagTemplate;
    /// <summary>包裹没有匹配物品时提示。</summary>
    [SerializeField] private GameObject _bagEmpty;
    /// <summary>当前包裹筛选条件。</summary>
    [SerializeField] private Text _bagFilterLabel;
    /// <summary>默认、装备、消耗和材料的筛选列表。</summary>
    [SerializeField] private GameObject _bagFilterMenu;
    /// <summary>风雷藏拍的三幅圆形拍场入口。</summary>
    [SerializeField] private GameObject _venues;
    /// <summary>从拍场目录返回地区选择。</summary>
    [SerializeField] private GameObject _venueBack;
    /// <summary>公示、信誉及个人记录的独立空页。</summary>
    [SerializeField] private GameObject _standalone;
    /// <summary>当前独立页的说明。</summary>
    [SerializeField] private Text _standaloneText;
    /// <summary>商品详情、求购与竞拍的同层弹窗。</summary>
    [SerializeField] private TradeMarketDialog _dialog;

    /// <summary>窗口实例内复用的左侧按钮。</summary>
    private readonly List<TradeMarketChoice> _rows = new List<TradeMarketChoice>();
    /// <summary>中央可复用商品卡片。</summary>
    private readonly List<TradeMarketCard> _cards = new List<TradeMarketCard>();
    /// <summary>包裹页复用的格子。</summary>
    private readonly List<TradeMarketCard> _bagCards = new List<TradeMarketCard>();
    /// <summary>当前角色在各市场本次会话的关注。</summary>
    private readonly Dictionary<Market, HashSet<int>> _favorites = new Dictionary<Market, HashSet<int>>();
    /// <summary>打开窗口的角色背包，随角色切换替换。</summary>
    private RoItemComponent _inventory;
    /// <summary>当前右侧入口。</summary>
    private Market _market;
    /// <summary>当前顶栏页签下标。</summary>
    private int _tab;
    /// <summary>当前拍场，负一表示尚在选地区。</summary>
    private int _venue;
    /// <summary>实际正在浏览的二级或单层货架。</summary>
    private Shelf _shelf;
    /// <summary>左侧展开的一级标题。</summary>
    private string _expanded;
    /// <summary>是否停留在关注或我的求购列表。</summary>
    private bool _personal;
    /// <summary>是否合并全服同分类的目录。</summary>
    private bool _cross;
    /// <summary>当前选择的等级筛选序号。</summary>
    private int _level;
    /// <summary>玩家包裹筛选，零为所有正常背包。</summary>
    private int _bagFilter;
    /// <summary>正查看挂单的物品，为空时显示目录。</summary>
    private ItemConfigBase _listedItem;
    /// <summary>进入挂单前的滚动位置，返回时定位到同一批商品。</summary>
    private float _catalogPosition;

    /// <summary>浏览交互只发出查询意图；主控制器负责网络请求。</summary>
    public event Action<StallQuery> StallRequested;
    /// <summary>当前本服目录的真实在售价格与数量，按物品配置索引。</summary>
    private readonly Dictionary<int, StallQuote> _quotes = new Dictionary<int, StallQuote>();
    /// <summary>行情尚在装载或服务不可用时显示的状态。</summary>
    private string _quoteState;
    /// <summary>当前单品或我的摊位的下一页游标，零时点击刷新首页。</summary>
    private long _nextOffer;
    /// <summary>公开挂单页的行文案，使用原面板范围。</summary>
    [SerializeField] private Text _offerLines;
    /// <summary>空挂单时展示的图标和提示。</summary>
    [SerializeField] private GameObject _offerEmptyState;
    /// <summary>自己的寄存记录，可浏览隐藏分类的剩货。</summary>
    [SerializeField] private Text _ownLines;
    /// <summary>其它市场出售页沿用的空摊位底图。</summary>
    [SerializeField] private GameObject _stallSlots;
    /// <summary>个人摊位翻页按钮文字，末页可回到首页。</summary>
    [SerializeField] private Text _ownPager;
    /// <summary>出售页的实际翻页按钮。</summary>
    [SerializeField] private Button _ownPageButton;
    /// <summary>摆摊标题，由服务端读取时去掉旧版固定的空位计数。</summary>
    [SerializeField] private Text _stallHeading;
    /// <summary>其他市场的固定页号，实时摆摊使用游标分页。</summary>
    [SerializeField] private GameObject _legacyPage;

    /// <summary>由主控制器绑定当前本地角色，关注随角色分开。</summary>
    /// <param name="items">角色的真实背包及虚拟货币组件。</param>
    public void BindInventory(RoItemComponent items)
    {
        if (items == _inventory) return;
        if (_inventory != null) _inventory.KnapsackChangedDetailed -= OnBagChanged;
        _inventory = items;
        _favorites.Clear();
        if (isActiveAndEnabled && _inventory != null) _inventory.KnapsackChangedDetailed += OnBagChanged;
    }

    /// <summary>打开时读取余额，从本服摆摊目录开始浏览。</summary>
    private void OnEnable()
    {
        _visibility = _catalog.ReadVisibility();
        _visibleShelves = _visibility.VisibleShelves(_catalog.Shelves);
        if (_inventory != null) _inventory.KnapsackChangedDetailed += OnBagChanged;
        Resize();
        RefreshWallet();
        SelectMarket((int)Market.Stall);
    }

    /// <summary>关闭窗口时停止观察背包。</summary>
    private void OnDisable()
    {
        if (_inventory != null) _inventory.KnapsackChangedDetailed -= OnBagChanged;
    }

    /// <summary>分辨率变化后重新计算参考单位和可视列宽。</summary>
    private void OnRectTransformDimensionsChange()
    {
        if (_layout != null) Resize();
    }

    /// <summary>按屏幕较短边缩放，额外宽高交给锚点分配。</summary>
    private void Resize()
    {
        // 根节点尺寸已经过主城 CanvasScaler 换算。
        Vector2 size = ((RectTransform)transform).rect.size;
        // 较短边约束字号和触控热区，超宽屏仍保留右侧导航。
        float scale = Mathf.Min(size.x / 1328f, size.y / 748f);
        if (scale <= 0f) return;
        _layout.localScale = new Vector3(scale, scale, 1f);
        _layout.sizeDelta = size / scale;
        FitGrid(_goods, _goodsGrid, 2, 100f);
        FitGrid(_bag, _bagGrid, 4, 0f);
    }

    /// <summary>按实际视口宽度均分商品卡或包裹格。</summary>
    /// <param name="scroll">当前滚动区。</param>
    /// <param name="grid">该区域的格子布局。</param>
    /// <param name="columns">可见列数。</param>
    /// <param name="height">固定卡高，零表示正方形。</param>
    private static void FitGrid(ScrollRect scroll, GridLayoutGroup grid, int columns, float height)
    {
        // 布局的内边距和格间距不计入单格宽度。
        float width = (scroll.viewport.rect.width - grid.padding.horizontal - (columns - 1) * grid.spacing.x) / columns;
        if (height == 0f) height = width;
        grid.cellSize = new Vector2(Mathf.Max(1f, Mathf.Floor(width)), Mathf.Max(1f, height));
    }

    /// <summary>背包变化只刷新真实余额和当前打开的出售页。</summary>
    /// <param name="change">当前角色发出的背包变更。</param>
    private void OnBagChanged(KnapsackChangedEventArgs change)
    {
        RefreshWallet();
        if (_sell.activeSelf) RefreshBag();
    }

    /// <summary>使用与角色包裹一致的四种虚拟货币编号。</summary>
    private void RefreshWallet()
    {
        // 数组顺序与录屏顶栏图标一一对应。
        int[] ids = { (int)ItemEnum.ZENY, (int)ItemEnum.ROMONEY, (int)ItemEnum.SILVER_TICKET, (int)ItemEnum.DIAMOND };
        // i 依次对应四个图标及其虚拟物品编号。
        for (int i = 0; i < ids.Length; i++)
        {
            _wallet[i].text = "—";
            if (_inventory != null) _wallet[i].text = _inventory.GetVirtualItemCount(ids[i]).ToString("N0");
        }
    }

    /// <summary>切换右侧频道，并重置上个频道的悬浮菜单。</summary>
    /// <param name="index">从摆摊到信誉的频道索引。</param>
    public void SelectMarket(int index)
    {
        _market = (Market)index;
        _venue = -1;
        // i 是右侧频道按钮在预制体中的显示顺序。
        for (int i = 0; i < _markets.Length; i++)
        {
            // 点击时使用该按钮自己的索引，回调不引用循环游标。
            int target = i;
            _markets[i].Bind(MarketNames[i], i == index, () => SelectMarket(target), _marketArt[i]);
        }
        if (!_favorites.ContainsKey(_market)) _favorites.Add(_market, new HashSet<int>());
        SelectTab(0);
    }

    /// <summary>按录屏顺序排列的右栏标题。</summary>
    private static readonly string[] MarketNames = { "摆摊", "集市", "全服集市", "神厨菜品", "风雷藏拍", "信誉" };
    /// <summary>装备等级选择器显示的区间。</summary>
    private static readonly string[] LevelNames = { "全部等级", "1–69级", "70–109级", "110级以上" };
    /// <summary>包裹筛选顺序，索引同时对应下拉按钮。</summary>
    private static readonly string[] BagNames = { "默认", "装备", "消耗品", "材料" };

    /// <summary>在进入风雷藏拍后选择一处拍场。</summary>
    /// <param name="venue">南海、云巅或九州的入口序号。</param>
    public void SelectVenue(int venue)
    {
        _venue = venue;
        SelectTab(0);
    }

    /// <summary>切换操作页，按市场呈现购买、出售、求购、公示或拍卖记录。</summary>
    /// <param name="index">当前市场中被选中的页签位置。</param>
    public void SelectTab(int index)
    {
        _tab = index;
        _personal = false;
        _cross = false;
        _level = 0;
        _bagFilter = 0;
        _listedItem = null;
        _nextOffer = 0;
        _shelf = null;
        _expanded = null;
        _search.SetTextWithoutNotify(string.Empty);
        _dialog.Close();
        _levelMenu.SetActive(false);
        _bagFilterMenu.SetActive(false);
        // 根据当前市场替换顶部标签，顺序与录像一致。
        string[] names = { "购买", "出售", "求购" };
        // 页签印记按当前市场的操作顺序选自公用图集。
        int[] symbols = { 0, 1, 2 };
        if (_market == Market.Local) { names = new[] { "购买", "出售", "公示" }; symbols = new[] { 0, 1, 3 }; }
        if (_market == Market.Global) { names = new[] { "购买", "出售", "公示", "关注排行榜" }; symbols = new[] { 0, 1, 3, 3 }; }
        if (_market == Market.Auction) { names = new[] { "珍品藏拍", "我的关注", "我的持标", "我的上架" }; symbols = new[] { 4, 5, 0, 1 }; }
        if (_market == Market.Kitchen) { names = new[] { "购买", "出售" }; symbols = new[] { 0, 1 }; }
        // i 遍历四个预留按钮，未使用的按钮收起。
        for (int i = 0; i < _tabs.Length; i++)
        {
            _tabs[i].gameObject.SetActive(i < names.Length);
            if (i >= names.Length) continue;
            // 复制页签位置供点击回调使用。
            int target = i;
            _tabs[i].Bind(names[i], index == i, () => SelectTab(target), _tabArt[symbols[i]]);
        }
        // 拍场入口没有顶栏，个人持标、公示等页按实际数据呈现空态。
        bool auction = _market == Market.Auction;
        // 尚未选择区域时只显示三个拍场入口。
        bool venues = auction && _venue < 0;
        // 出售标签在普通市场进入摊位和包裹。
        bool selling = !auction && _market != Market.Credit && index == 1;
        // 个人持标、排行和信誉共用记录空页。
        bool standalone = _market == Market.Credit || (auction && index >= 2);
        if ((_market == Market.Local || _market == Market.Global) && index >= 2) standalone = true;
        _toolbar.SetActive(!venues && _market != Market.Credit);
        _sell.SetActive(selling);
        _venues.SetActive(venues);
        _venueBack.SetActive(auction && !venues);
        _standalone.SetActive(standalone);
        _browse.SetActive(!venues && !selling && !standalone);
        _listings.SetActive(false);
        SetAuctionListLayout(false);
        if (standalone)
        {
            _standaloneText.text = "暂无公示商品";
            if (_market == Market.Credit) _standaloneText.text = "信誉　·　暂无信誉记录";
            if (_market == Market.Global && index == 3) _standaloneText.text = "暂无关注排行";
            if (auction && index == 2) _standaloneText.text = "暂无持标记录";
            if (auction && index == 3) _standaloneText.text = "暂无上架拍品";
        }
        // 本服出售页独立读取角色的全部寄存物品，分类隐藏配置仅影响公共目录。
        bool own = selling && _market == Market.Stall;
        _ownLines.gameObject.SetActive(own);
        _ownPageButton.gameObject.SetActive(own);
        if (own) _stallHeading.text = "我的摊位";
        else _stallHeading.text = "我的摊位  0/8";
        _stallSlots.SetActive(!own);
        if (own)
        {
            _ownLines.text = "正在读取我的摊位…";
            _ownPager.text = "刷新";
            RequestOffers(0);
        }
        if (_market == Market.Stall && index == 0)
        {
            _quotes.Clear();
            _quoteState = "读取中";
            StallRequested?.Invoke(new StallQuery { View = StallView.StallSummary });
        }
        if (selling) RefreshBag();
        if (!_browse.activeSelf) return;
        _search.gameObject.SetActive(!auction && _visibility.showSearch);
        // 拍场目录没有搜索栏，首个分类贴近左栏顶端。
        RectTransform categoryArea = (RectTransform)_categories.transform;
        // 预留搜索框的参考高度，单位为界面像素。
        float top = 93f;
        if (!_search.gameObject.activeSelf) top = 10f;
        categoryArea.offsetMax = new Vector2(categoryArea.offsetMax.x, -top);
        _personal = auction && index == 1;
        // 选中此频道第一个真实货架，保证首屏可立即浏览物品。
        for (int i = 0; i < _visibleShelves.Count; i++)
        {
            if (!Here(_visibleShelves[i])) continue;
            _shelf = _visibleShelves[i];
            _expanded = _shelf.Group;
            break;
        }
        _categories.verticalNormalizedPosition = 1f;
        BuildCategories();
        RefreshGoods();
    }

    /// <summary>判断一个货架是否属于当前市场及拍场。</summary>
    /// <param name="shelf">目录中待显示的货架。</param>
    private bool Here(Shelf shelf) => shelf.Market == _market && (_market != Market.Auction || shelf.Venue == _venue);

    /// <summary>根据当前展开项产生一级与二级按钮。</summary>
    private void BuildCategories()
    {
        // 覆盖可复用按钮时从第一行开始。
        int count = 0;
        // 普通浏览与求购分别使用文件内对应的个人列表开关。
        bool showPersonal = _visibility.showFavorites;
        if (WantedPage) showPersonal = _visibility.showMyRequests;
        if (_market != Market.Auction && showPersonal)
        {
            // 摆摊求购入口第一行显示本人的求购记录。
            string label = "关注";
            if (WantedPage) label = "我的求购";
            AddCategory(ref count, label, _personal, () => SelectShelf(null));
        }
        // 同一组的一级行只出现一次。
        string lastGroup = null;
        // i 按正式目录顺序逐个访问货架。
        for (int i = 0; i < _visibleShelves.Count; i++)
        {
            // 保留配置对象供各行的点击回调引用。
            Shelf shelf = _visibleShelves[i];
            if (!Here(shelf)) continue;
            // 有二级货架的组在点击一级时展开或收起。
            bool children = !string.IsNullOrEmpty(shelf.Leaf);
            if (lastGroup != shelf.Group)
            {
                lastGroup = shelf.Group;
                // 已展开组的箭头向上，其余有二级项的组向下。
                bool? expanded = null;
                if (children) expanded = _expanded == shelf.Group;
                AddCategory(ref count, shelf.Group, !_personal && _shelf?.Group == shelf.Group, () => ToggleGroup(shelf), expanded);
            }
            if (children && _expanded == shelf.Group)
                AddCategory(ref count, shelf.Leaf, !_personal && _shelf == shelf, () => SelectShelf(shelf), null, true);
        }
        // i 从已使用行数开始，收起上个分类的多余按钮。
        for (int i = count; i < _rows.Count; i++) _rows[i].gameObject.SetActive(false);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_categories.content);
    }

    /// <summary>复用一行左栏按钮并绑定其目标。</summary>
    /// <param name="count">本次目录已显示的行数。</param>
    /// <param name="title">按钮显示名称。</param>
    /// <param name="selected">当前选中项或其所属一级组。</param>
    /// <param name="click">点击后选择或展开该行。</param>
    /// <param name="expanded">一级组的展开状态，普通按钮留空。</param>
    /// <param name="child">是否为缩进的二级项。</param>
    private void AddCategory(ref int count, string title, bool selected, Action click, bool? expanded = null, bool child = false)
    {
        if (count == _rows.Count) _rows.Add(Instantiate(_categoryTemplate, _categories.content));
        // 此行本次使用的样式由一级或二级位置决定。
        TradeMarketChoice row = _rows[count++];
        row.gameObject.SetActive(true);
        row.Bind(title, selected, click, null, expanded, child);
    }

    /// <summary>点当前已展开的一级可收回二级列表。</summary>
    /// <param name="first">本组首个货架。</param>
    private void ToggleGroup(Shelf first)
    {
        if (_expanded == first.Group && !string.IsNullOrEmpty(first.Leaf))
        {
            _expanded = null;
            BuildCategories();
            return;
        }
        SelectShelf(first);
    }

    /// <summary>显示一个实际货架或个人列表。</summary>
    /// <param name="shelf">选中的分类，为空时显示关注或本人的求购。</param>
    private void SelectShelf(Shelf shelf)
    {
        _personal = shelf == null;
        if (_market == Market.Auction && _tab == 1) _personal = true;
        _shelf = shelf;
        _expanded = shelf?.Group;
        _listedItem = null;
        _level = 0;
        _search.SetTextWithoutNotify(string.Empty);
        _levelMenu.SetActive(false);
        BuildCategories();
        RefreshGoods();
    }

    /// <summary>是否显示摆摊求购侧。</summary>
    private bool WantedPage => _market == Market.Stall && _tab == 2;

    /// <summary>编辑搜索词时跨当前市场所有货架匹配物品名。</summary>
    /// <param name="query">输入框刚更新的内容。</param>
    public void Search(string query)
    {
        _listedItem = null;
        _levelMenu.SetActive(false);
        RefreshGoods();
    }

    /// <summary>按当前目录、名称、关注与等级筛选实际物品配置。</summary>
    private void RefreshGoods()
    {
        _listings.SetActive(_listedItem != null);
        if (_listedItem != null) return;
        SetAuctionListLayout(false);
        _goods.gameObject.SetActive(true);
        // 同一配置在多种交叉目录出现时仍只占一个商品格。
        HashSet<int> shown = new HashSet<int>();
        // 本轮筛选实际占用的商品格数。
        int count = 0;
        // 首尾空白不参与物品名称匹配。
        string query = _search.text.Trim();
        // 有输入时允许跨当前市场的分类搜索。
        bool searching = query.Length > 0;
        // i 遍历可参与本轮筛选的货架。
        for (int i = 0; i < _visibleShelves.Count; i++)
        {
            // 同分类全服货架在玩家主动查看全服时纳入。
            Shelf shelf = _visibleShelves[i];
            // 用户勾选查看全服后，加入全服市场内的同名分类。
            bool adjacent = _cross && shelf.Market == Market.Global && shelf.Group == _shelf?.Group;
            if (!Here(shelf) && !adjacent) continue;
            if (!searching && !_personal && !adjacent)
            {
                if (shelf != _shelf) continue;
            }
            if (_market == Market.Auction && _personal && _shelf != null)
            {
                if (shelf != _shelf) continue;
            }
            // j 依陈列顺序访问本货架关联的物品编号。
            for (int j = 0; j < shelf.Items.Length; j++)
            {
                // 目录仅保存配置编号，名称与描述始终以当前项目表为准。
                int id = shelf.Items[j];
                if (!shown.Add(id)) continue;
                if (_personal && !searching)
                {
                    if (WantedPage || !_favorites[_market].Contains(id)) continue;
                }
                // 读取项目内当前版本的物品名称和等级条件。
                ItemConfigBase item = LubanMgr.Instance.GetItemConfigById(id);
                if (item == null || !MatchesLevel(item.NeedLevel)) continue;
                if (searching && item.ItemName.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (count == _cards.Count) _cards.Add(Instantiate(_cardTemplate, _goods.content));
                // 同一张卡在摆摊页显示真实行情，其余市场沿用各自的展示。
                TradeMarketCard card = _cards[count++];
                card.gameObject.SetActive(true);
                BindCard(card, item, false);
            }
        }
        // i 从首个空闲商品格起收回旧页的剩余卡片。
        for (int i = count; i < _cards.Count; i++) _cards[i].gameObject.SetActive(false);
        _empty.SetActive(count == 0);
        _emptyLabel.text = "暂无此类商品";
        if (_personal) _emptyLabel.text = "您还没有关注任何商品哦";
        if (_personal && WantedPage) _emptyLabel.text = "暂无求购记录";
        if (searching) _emptyLabel.text = "没有找到匹配的商品";
        _levelButton.SetActive(_market != Market.Auction && !_personal);
        _levelText.text = LevelNames[_level];
        _crossButton.SetActive(_market == Market.Stall && !_personal);
        _crossMark.SetActive(_cross);
        FitGrid(_goods, _goodsGrid, 2, 100f);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_goods.content);
        _goods.StopMovement();
        _goods.verticalNormalizedPosition = 1f;
    }

    /// <summary>判断物品所需等级是否位于当前区间。</summary>
    /// <param name="level">表中规定的使用等级。</param>
    private bool MatchesLevel(int level)
    {
        if (_level == 1) return level < 70;
        if (_level == 2) return level >= 70 && level < 110;
        if (_level == 3) return level >= 110;
        return true;
    }

    /// <summary>配置一张目录卡或挂单页的商品标题卡。</summary>
    /// <param name="card">即将显示的预制体实例。</param>
    /// <param name="item">项目内的真实商品配置。</param>
    /// <param name="detail">点击标题卡时直接打开商品说明。</param>
    private void BindCard(TradeMarketCard card, ItemConfigBase item, bool detail)
    {
        // 求购页显示求购数量，买入侧展示在售状态。
        string state = "在售：—";
        if (_market == Market.Stall && _tab == 0)
        {
            // 行情只含有余货的物品，成功装载后缺项即代表零件在售。
            StallQuote quote;
            if (_quotes.TryGetValue(item.ItemId, out quote))
                state = "在售 " + quote.Quantity + "  ·  " + quote.Price + CurrencyName(quote.CurrencyId) + "起";
            else if (_quoteState.Length == 0) state = "在售 0";
            else state = _quoteState;
        }
        if (WantedPage) state = "求购：—";
        // 材料与藏拍货物的单据按钮使用不同的标题。
        string action = "求购";
        // 仅有单据入口的卡片才绑定这一点击回调。
        Action order = null;
        if (_market == Market.Stall && item.ItemMainType == ItemMainType.ItemType_Material)
            order = () => OpenForm(TradeMarketDialog.Form.Wanted, item, 99);
        if (_market == Market.Auction)
        {
            action = "出价";
            order = () => OpenForm(TradeMarketDialog.Form.Bid, item, 1);
        }
        if (detail) order = null;
        card.Bind(item, _catalog.GetIcon(item.ItemId), state, () =>
        {
            if (detail) OpenForm(TradeMarketDialog.Form.Detail, item, 1);
            else ShowListings(item);
        }, _favorites[_market].Contains(item.ItemId), () => ToggleFavorite(card, item, detail), action, order);
    }

    /// <summary>更新当前市场的会话关注，保留普通列表的滚动位置。</summary>
    /// <param name="card">玩家刚点击的卡片。</param>
    /// <param name="item">要增删关注的商品。</param>
    /// <param name="detail">点击位置是否在挂单页。</param>
    private void ToggleFavorite(TradeMarketCard card, ItemConfigBase item, bool detail)
    {
        if (!_favorites[_market].Add(item.ItemId)) _favorites[_market].Remove(item.ItemId);
        if (_personal && !detail) RefreshGoods();
        else BindCard(card, item, detail);
    }

    /// <summary>进入商品的挂单层，标题可继续打开物品简介。</summary>
    /// <param name="item">当前点击的商品配置。</param>
    private void ShowListings(ItemConfigBase item)
    {
        _catalogPosition = _goods.verticalNormalizedPosition;
        _listedItem = item;
        _empty.SetActive(false);
        _goods.gameObject.SetActive(false);
        _levelButton.SetActive(false);
        _crossButton.SetActive(false);
        _levelMenu.SetActive(false);
        _listings.SetActive(true);
        SetAuctionListLayout(_market == Market.Auction);
        BindCard(_listedCard, item, true);
        _legacyPage.SetActive(_market != Market.Stall || _tab != 0);
        _offerEmptyState.SetActive(true);
        _offerLines.gameObject.SetActive(false);
        _listedEmpty.text = "暂无在售挂单";
        _draftLabel.text = "物品简介";
        if (_market == Market.Stall) _draftLabel.text = "求购";
        if (_market == Market.Stall && _tab == 0)
        {
            _nextOffer = 0;
            _listedEmpty.text = "正在读取挂单…";
            _draftLabel.text = "刷新";
            RequestOffers(0);
        }
        if (WantedPage) _listedEmpty.text = "暂无求购挂单";
        if (_market == Market.Auction) { _listedEmpty.text = "暂时无任何拍卖商品"; _draftLabel.text = "查看出价"; }
    }

    /// <summary>读取货币配置名称，未装入对应配置时保留币种编号。</summary><param name="id">挂单的实际货币物品编号。</param>
    private static string CurrencyName(int id)
    {
        // 货币也由统一物品表提供名称。
        ItemConfigBase currency = LubanMgr.Instance.GetItemConfigById(id);
        if (currency != null) return currency.ItemName;
        return "币种" + id;
    }

    /// <summary>翻阅自己的寄存记录，最后一页点击可刷新首页。</summary>
    public void NextOwnOffers() { RequestOffers(_nextOffer); }

    /// <summary>发起当前具体挂单页面的读取。</summary><param name="after">上页最后一条记录的挂牌号，零为首页。</param>
    private void RequestOffers(long after)
    {
        // 个人摊位不携带卖家号，服务端从角色路由取得。
        StallQuery query = new StallQuery { View = StallView.StallOwn, AfterId = after };
        if (_listedItem != null) { query.View = StallView.StallOffers; query.ItemId = _listedItem.ItemId; }
        StallRequested?.Invoke(query);
    }

    /// <summary>让当前摆摊页面展示已提交的真实数据或市场离线信息。</summary><param name="page">由交易服或网关返回的当前页。</param>
    public void ApplyStall(StallResult page)
    {
        if (!isActiveAndEnabled || _market != Market.Stall) return;
        // 没有关联查询的回包表示整个市场断开，清除旧报价。
        if (page.Query == null)
        {
            _quotes.Clear();
            _quoteState = page.Reason;
            _nextOffer = 0;
            if (_tab == 1) { _ownLines.text = page.Reason; _ownPager.text = "刷新"; }
            if (_tab == 0 && _listedItem != null) { ShowOfferText(page.Reason, false); _draftLabel.text = "刷新"; }
            if (_tab == 0 && _listedItem == null) RefreshGoods();
            return;
        }
        if (page.Query.View == StallView.StallSummary)
        {
            if (_tab != 0) return;
            if (page.Query.AfterId == 0) _quotes.Clear();
            _quoteState = page.Reason;
            for (int i = 0; i < page.Quotes.Count; i++) _quotes[page.Quotes[i].ItemId] = page.Quotes[i];
            if (page.NextId != 0)
            {
                StallRequested?.Invoke(new StallQuery { View = StallView.StallSummary, AfterId = page.NextId });
                return;
            }
            if (_listedItem == null) RefreshGoods();
            else BindCard(_listedCard, _listedItem, true);
            return;
        }
        // 其他商品或其他页面的迟到结果不替换正在浏览的内容。
        bool own = page.Query.View == StallView.StallOwn;
        if (own && _tab != 1) return;
        if (!own && (_listedItem == null || page.Query.ItemId != _listedItem.ItemId)) return;
        _nextOffer = page.NextId;
        // 每页最多三条记录，分页控件在末页提供刷新。
        string action = "刷新";
        if (_nextOffer != 0) action = "下一页";
        // 本页的服务提示或多行商品详情。
        string caption = page.Reason;
        if (caption.Length == 0 && page.Offers.Count == 0) caption = "暂无在售挂单";
        if (own && page.Reason.Length == 0 && page.Offers.Count == 0) caption = "暂无寄存物品";
        for (int i = 0; i < page.Offers.Count; i++)
        {
            // 自己的摊位显示物品名称和过期余货，公开页按真实挂牌显示单价。
            StallOffer row = page.Offers[i];
            // 寄存物品的本地显示配置。
            ItemConfigBase item = LubanMgr.Instance.GetItemConfigById(row.ItemId);
            // 公开页显示挂牌号，卖家页优先显示物品名。
            string label = "挂单 " + row.ListingId;
            if (own && item != null) label = item.ItemName;
            if (row.Status == 4) label += "（已到期）";
            if (row.Status == 3) label += "（已撤）";
            if (i > 0) caption += "\n\n";
            caption += label + "　×" + row.Quantity + "　" + row.Price + CurrencyName(row.CurrencyId) + "/件";
        }
        if (own) { _ownLines.text = caption; _ownPager.text = action; }
        else { ShowOfferText(caption, page.Offers.Count > 0); _draftLabel.text = action; }
    }

    /// <summary>挂单有数据时用原区域展示列表，空页和错误沿用原提示图。</summary>
    /// <param name="caption">该页挂单或状态文字。</param><param name="hasRows">本次是否返回真实记录。</param>
    private void ShowOfferText(string caption, bool hasRows)
    {
        _offerEmptyState.SetActive(!hasRows);
        _offerLines.gameObject.SetActive(hasRows);
        if (hasRows) _offerLines.text = caption;
        else _listedEmpty.text = caption;
    }

    /// <summary>藏拍挂单页使用整幅货架；其它页面留出分类栏。</summary>
    /// <param name="wide">当前是否进入藏拍挂单层。</param>
    private void SetAuctionListLayout(bool wide)
    {
        _sidebar.SetActive(!wide);
        _auctionTools.SetActive(wide);
        // 常规目录左边界避让分类栏，藏拍浏览使用整个内容宽度。
        float left = 0.278f;
        if (wide) left = 0f;
        _shelfPanel.anchorMin = new Vector2(left, _shelfPanel.anchorMin.y);
        if (wide) _auctionSort.text = "全部商品 · 价格排序";
    }

    /// <summary>切换拍品列表的价格或时间排序条件。</summary>
    public void ToggleAuctionSort()
    {
        if (_auctionSort.text == "全部商品 · 价格排序") _auctionSort.text = "全部商品 · 时间排序";
        else _auctionSort.text = "全部商品 · 价格排序";
    }

    /// <summary>回到刚才浏览的分类。</summary>
    public void BackToCatalog()
    {
        _listedItem = null;
        RefreshGoods();
        _goods.verticalNormalizedPosition = _catalogPosition;
    }

    /// <summary>从挂单页打开该市场支持的单据或说明。</summary>
    public void OpenListedForm()
    {
        if (_market == Market.Stall && _tab == 0) { RequestOffers(_nextOffer); return; }
        if (_market == Market.Stall) OpenForm(TradeMarketDialog.Form.Wanted, _listedItem, 99);
        else if (_market == Market.Auction) OpenForm(TradeMarketDialog.Form.Bid, _listedItem, 1);
        else OpenForm(TradeMarketDialog.Form.Detail, _listedItem, 1);
    }

    /// <summary>把同一配置及当前实际图标交给窗口内弹层。</summary>
    /// <param name="form">本次查看或编辑的单据。</param>
    /// <param name="item">选中的商品或背包格。</param>
    /// <param name="maximum">当前能填写的最大份数。</param>
    private void OpenForm(TradeMarketDialog.Form form, ItemConfigBase item, int maximum)
    {
        _dialog.Open(form, item, _catalog.GetIcon(item.ItemId), maximum, _market != Market.Stall);
    }

    /// <summary>展开目录的等级选择列表。</summary>
    public void ToggleLevels() => _levelMenu.SetActive(!_levelMenu.activeSelf);

    /// <summary>应用选中的等级范围。</summary>
    /// <param name="index">等级菜单的区间序号。</param>
    public void SelectLevel(int index)
    {
        _level = index;
        _levelMenu.SetActive(false);
        RefreshGoods();
    }

    /// <summary>切换是否浏览全服同组的配置。</summary>
    public void ToggleCross()
    {
        _cross = !_cross;
        RefreshGoods();
    }

    /// <summary>显示当前角色可见的背包内容，保留每一堆的真实数量。</summary>
    private void RefreshBag()
    {
        _bagFilterLabel.text = BagNames[_bagFilter];
        // 只读取普通、装备、消耗和材料四个包，不陈列穿戴中物品及虚拟货币。
        int count = 0;
        // i 对应四个真实背包的 KnapsackType 序号。
        for (int i = 0; i < 4; i++)
        {
            if (_bagFilter != 0 && i != _bagFilter) continue;
            // 当前角色的这一个包裹提供物品实例及格位。
            RoItemContainer container = _inventory?.GetContainer((KnapsackType)i);
            if (container == null) continue;
            // 同一包按格序陈列，分开的堆叠分别保留。
            List<ItemBase> entries = new List<ItemBase>(container.GetItemMap().Values);
            entries.Sort((left, right) => left.GetBagIndex().CompareTo(right.GetBagIndex()));
            // j 是按背包格位排序后的物品堆叠序号。
            for (int j = 0; j < entries.Count; j++)
            {
                // 背包的真实堆叠和配置用于显示及本地上架草稿。
                ItemBase stack = entries[j];
                // 用该堆叠的配置查找实际图标和名称。
                ItemConfigBase item = LubanMgr.Instance.GetItemConfigById(stack.GetItemID());
                if (item == null) continue;
                if (count == _bagCards.Count) _bagCards.Add(Instantiate(_bagTemplate, _bag.content));
                // 当前堆叠占用的可复用展示格。
                TradeMarketCard card = _bagCards[count++];
                // 草稿允许的份数取当前这一堆，在界面整数范围内显示。
                int amount = (int)Math.Min(int.MaxValue, stack.GetItemCount());
                card.gameObject.SetActive(true);
                card.Bind(item, _catalog.GetIcon(item.ItemId), amount.ToString(), () => OpenForm(TradeMarketDialog.Form.Sell, item, amount), false, null, "", null);
            }
        }
        // i 从首个未用格起收起上一次筛选的多余包裹格。
        for (int i = count; i < _bagCards.Count; i++) _bagCards[i].gameObject.SetActive(false);
        _bagEmpty.SetActive(count == 0);
        FitGrid(_bag, _bagGrid, 4, 0f);
    }

    /// <summary>展开右侧包裹的分类菜单。</summary>
    public void ToggleBagFilters() => _bagFilterMenu.SetActive(!_bagFilterMenu.activeSelf);

    /// <summary>根据玩家选择的背包类型刷新物品格。</summary>
    /// <param name="index">默认、装备、消耗品或材料的序号。</param>
    public void FilterBag(int index)
    {
        _bagFilter = index;
        _bagFilterMenu.SetActive(false);
        RefreshBag();
        _bag.verticalNormalizedPosition = 1f;
    }

    /// <summary>查看交易记录、上架说明或顶部积分入口。</summary>
    /// <param name="index">零为记录，一为上架规则，二为积分。</param>
    public void ShowInfo(int index)
    {
        if (index == 0) _dialog.Message("交易记录", "暂无交易记录");
        if (index == 1) _dialog.Message("上架说明", "从包裹中选择物品，可查看上架单。当前尚未开放玩家挂单。");
        if (index == 2) _dialog.Message("积分", "暂无交易积分记录");
    }

    /// <summary>按 Esc 先退弹层、下拉或挂单页，最后离开交易窗口。</summary>
    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (_dialog.gameObject.activeSelf) _dialog.Close();
        else if (_levelMenu.activeSelf) _levelMenu.SetActive(false);
        else if (_bagFilterMenu.activeSelf) _bagFilterMenu.SetActive(false);
        else if (_listedItem != null) BackToCatalog();
        else if (_market == Market.Auction && _venue >= 0) SelectMarket((int)Market.Auction);
        else CloseWindow();
    }
}
