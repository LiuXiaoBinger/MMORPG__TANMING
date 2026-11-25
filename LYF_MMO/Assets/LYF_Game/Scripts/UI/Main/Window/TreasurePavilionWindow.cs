using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>主城珍宝阁窗口，负责四个频道的陈列、筛选和操作意图。</summary>
public sealed class TreasurePavilionWindow : WindowBase
{
    /// <summary>设计尺寸画布，随可用面积等比缩放。</summary>
    [SerializeField] private RectTransform layout;
    /// <summary>随窗口打包的四频道陈列配置。</summary>
    [SerializeField] private TextAsset catalogFile;
    /// <summary>项目物品编号到背包图标的映射。</summary>
    [SerializeField] private TradeMarketCatalog items;
    /// <summary>铜钱、银两、银票和元宝的统一项目图标。</summary>
    [SerializeField] private Sprite[] walletIcons;
    /// <summary>顶部四个角色钱包的余额文本。</summary>
    [SerializeField] private Text[] balances;
    /// <summary>仅加载该窗口引用的 RO 商城插画切片。</summary>
    [SerializeField] private Sprite[] shopArt;
    /// <summary>右侧频道当前选中的高亮标记。</summary>
    [SerializeField] private GameObject[] navMarks;
    /// <summary>顶部复用的页签按钮。</summary>
    [SerializeField] private PavilionTile[] tabs;
    /// <summary>商城搜索与过滤工具栏。</summary>
    [SerializeField] private GameObject filters;
    /// <summary>当前展开的筛选选项浮层。</summary>
    [SerializeField] private GameObject filterPopup;
    /// <summary>浮层可选择的货币或类型。</summary>
    [SerializeField] private PavilionTile[] filterChoices;
    /// <summary>用于按商品名称查找的输入框。</summary>
    [SerializeField] private InputField search;
    /// <summary>选中的货币过滤条件。</summary>
    [SerializeField] private Text currencyLabel;
    /// <summary>选中的物品类型过滤条件。</summary>
    [SerializeField] private Text typeLabel;
    /// <summary>显示或隐藏高级商品的条件。</summary>
    [SerializeField] private Text levelLabel;
    /// <summary>独立的绑定元宝余额。</summary>
    [SerializeField] private Text boundBalance;
    /// <summary>当前选中商品的详情面板。</summary>
    [SerializeField] private GameObject detail;
    /// <summary>详情物品插画。</summary>
    [SerializeField] private Image detailArt;
    /// <summary>当前商品名。</summary>
    [SerializeField] private Text detailName;
    /// <summary>限购或背包持有数量。</summary>
    [SerializeField] private Text limitText;
    /// <summary>物品说明和获取途径。</summary>
    [SerializeField] private Text description;
    /// <summary>详情中当前选择的份数。</summary>
    [SerializeField] private Text quantityText;
    /// <summary>当前数量对应的总价。</summary>
    [SerializeField] private Text total;
    /// <summary>结算按钮内的币种。</summary>
    [SerializeField] private Image totalCoin;
    /// <summary>减一操作，最小数量时置灰。</summary>
    [SerializeField] private Button minus;
    /// <summary>加一操作，到上限时置灰。</summary>
    [SerializeField] private Button plus;
    /// <summary>商品、充值和返利共用的纵向滚动视口。</summary>
    [SerializeField] private ScrollRect goods;
    /// <summary>商会一级二级分类滚动视口。</summary>
    [SerializeField] private ScrollRect categories;
    /// <summary>两列商城和商会列表共用的商品卡片。</summary>
    [SerializeField] private PavilionTile productTemplate;
    /// <summary>商会目录按钮。</summary>
    [SerializeField] private PavilionTile categoryTemplate;
    /// <summary>充值礼包模板。</summary>
    [SerializeField] private PavilionTile packTemplate;
    /// <summary>返利进度与奖励模板。</summary>
    [SerializeField] private PavilionTile rewardTemplate;
    /// <summary>当前目录或背包没有商品时的提示。</summary>
    [SerializeField] private Text empty;
    /// <summary>未接入业务和点击提示。</summary>
    [SerializeField] private Text feedback;
    /// <summary>累计或年度返利表头。</summary>
    [SerializeField] private Text rewardHeading;
    /// <summary>随返利页整体显示的门槛、奖品和状态列标题。</summary>
    [SerializeField] private GameObject rewardHeader;
    /// <summary>商城或商会的购买请求，由后续业务控制器订阅。</summary>
    public event Action<PavilionEntry.Order> PurchaseRequested;
    /// <summary>从当前包裹堆叠选择的商会出售请求。</summary>
    public event Action<PavilionEntry.Order> SaleRequested;
    /// <summary>请求打开指定元宝面值的充值渠道。</summary>
    public event Action<int> RechargeRequested;
    /// <summary>请求领取返利，参数依次为年度标记和门槛。</summary>
    public event Action<bool, int> RebateRequested;
    /// <summary>预留的活动入口。</summary>
    public event Action ActivityRequested;

    /// <summary>解析一次的本地目录。</summary>
    private PavilionEntry.Catalog catalog;
    /// <summary>打开窗口的当前角色背包。</summary>
    private RoItemComponent bag;
    /// <summary>角色等级筛选使用的实际等级。</summary>
    private int roleLevel;
    /// <summary>外部提供的绑定元宝余额，未知时留空。</summary>
    private long? boundAmount;
    /// <summary>普通与年度返利的外部累计值。</summary>
    private int?[] progress = new int?[2];
    /// <summary>普通与年度已领取返利的档位。</summary>
    private HashSet<int>[] claimed = { new HashSet<int>(), new HashSet<int>() };
    /// <summary>当前角色在本次会话收藏的目录键。</summary>
    private readonly HashSet<string> favorites = new HashSet<string>();
    /// <summary>可复用商品卡。</summary>
    private readonly List<PavilionTile> cards = new List<PavilionTile>();
    /// <summary>可复用充值档位。</summary>
    private readonly List<PavilionTile> packs = new List<PavilionTile>();
    /// <summary>可复用返利行。</summary>
    private readonly List<PavilionTile> rewards = new List<PavilionTile>();
    /// <summary>可复用分类行。</summary>
    private readonly List<PavilionTile> shelves = new List<PavilionTile>();
    /// <summary>筛选并排序后的当前页条目。</summary>
    private readonly List<PavilionEntry> visible = new List<PavilionEntry>();
    /// <summary>详情对应的条目。</summary>
    private PavilionEntry selected;
    /// <summary>本次操作份数。</summary>
    private int quantity = 1;
    /// <summary>侧栏顺序为充值、活动、商城、绑元商城、商会。</summary>
    private int channel = 2;
    /// <summary>当前顶部页签下标。</summary>
    private int tab;
    /// <summary>商会选中的一级分类。</summary>
    private int group;
    /// <summary>商会选中的子分类。</summary>
    private int leaf;
    /// <summary>商会选中一级目录是否展开。</summary>
    private bool expanded;
    /// <summary>启用角色等级过滤。</summary>
    private bool lowerOnly;
    /// <summary>详情当前是否展开获取途径。</summary>
    private bool showSource;
    /// <summary>空串表示全部币种。</summary>
    private string currency = "";
    /// <summary>空串表示全部类型。</summary>
    private string kind = "";
    /// <summary>与四种钱包图标顺序一致的名称。</summary>
    private static readonly string[] WalletNames = { "铜钱", "银两", "银票", "元宝" };
    /// <summary>四种虚拟物品表编号。</summary>
    private static readonly int[] WalletIds = { 101, 102, 106, 103 };
    /// <summary>充值频道顶部页签。</summary>
    private static readonly string[] RechargeTabs = { "充值", "返利", "年度返利" };
    /// <summary>商会的两个交易方向。</summary>
    private static readonly string[] ChamberTabs = { "购买", "出售" };

    /// <summary>主城入口传入当前角色，换角色时清空个人页状态。</summary>
    /// <param name="inventory">当前本地角色背包。</param>
    /// <param name="level">角色等级。</param>
    public void BindRole(RoItemComponent inventory, int level)
    {
        if (bag != inventory)
        {
            favorites.Clear();
            boundAmount = null;
            Array.Clear(progress, 0, progress.Length);
            claimed[0].Clear();
            claimed[1].Clear();
        }
        bag = inventory;
        roleLevel = level;
    }

    /// <summary>接收业务方提供的只读绑定元宝余额。</summary>
    /// <param name="amount">绑定元宝余额，为空时显示未知。</param>
    public void SetBoundBalance(long? amount)
    {
        boundAmount = amount;
        RefreshWallet();
    }

    /// <summary>更新外部提供的充值累计与已领取档位。</summary>
    /// <param name="annual">是否年度累计。</param>
    /// <param name="amount">已确认的累计充值额。</param>
    /// <param name="thresholds">已领取的档位门槛。</param>
    public void SetRebateProgress(bool annual, int amount, int[] thresholds)
    {
        // 普通和年度累计独立存储。
        int index = Convert.ToInt32(annual);
        progress[index] = amount;
        claimed[index].Clear();
        if (thresholds != null) claimed[index].UnionWith(thresholds);
        if (gameObject.activeInHierarchy && channel == 0 && tab > 0) DrawGoods();
    }

    /// <summary>显示时读取陈列文件并回到录屏中的商城首页。</summary>
    /// <param name="isShow">本次显隐目标。</param>
    /// <param name="obj">框架显隐接口预留的刷新参数。</param>
    public override void Show(bool isShow = true, object obj = null)
    {
        base.Show(isShow, obj);
        if (!isShow) return;
        if (catalog == null)
        {
            catalog = JsonUtility.FromJson<PavilionEntry.Catalog>(catalogFile.text);
            search.onValueChanged.AddListener(OnSearch);
        }
        Fit();
        OpenChannel(2);
    }

    /// <summary>窗口尺寸或分辨率变化时重算设计画布缩放。</summary>
    private void OnRectTransformDimensionsChange() => Fit();

    /// <summary>以设计宽高等比适配当前主城窗口。</summary>
    private void Fit()
    {
        if (layout == null) return;
        // 当前父层用于珍宝阁的逻辑尺寸。
        Rect area = ((RectTransform)transform).rect;
        layout.localScale = Vector3.one * Mathf.Min(area.width / 1600f, area.height / 900f);
    }

    /// <summary>打开右侧频道，活动只通知订阅方。</summary>
    /// <param name="index">右侧频道编号。</param>
    public void OpenChannel(int index)
    {
        if (index == 1)
        {
            if (ActivityRequested != null) ActivityRequested();
            else feedback.text = "活动暂未开放";
            return;
        }
        channel = index;
        group = 0;
        leaf = 0;
        expanded = true;
        for (int i = 0; i < navMarks.Length; i++) navMarks[i].SetActive(i == index);
        OpenTab(0);
    }

    /// <summary>选择上方分类并重置不再适用的查询条件。</summary>
    /// <param name="index">当前频道内的页签序号。</param>
    public void OpenTab(int index)
    {
        tab = index;
        currency = "";
        kind = "";
        lowerOnly = false;
        filterPopup.SetActive(false);
        search.SetTextWithoutNotify("");
        search.gameObject.SetActive(false);
        feedback.text = "";
        // 当前频道拥有的页签标题。
        string[] titles = catalog.mall;
        if (channel == 0) titles = RechargeTabs;
        if (channel == 3) titles = catalog.bound;
        if (channel == 4) titles = ChamberTabs;
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].gameObject.SetActive(i < titles.Length);
            if (i >= titles.Length) continue;
            tabs[i].title.text = titles[i];
            tabs[i].selection.SetActive(i == tab);
            // 保留本按钮的页签序号供点击使用。
            int target = i;
            tabs[i].OnPick(() => OpenTab(target));
        }
        filters.SetActive(channel == 2 || channel == 3);
        boundBalance.gameObject.SetActive(channel == 3);
        categories.gameObject.SetActive(channel == 4 && tab == 0);
        rewardHeader.SetActive(channel == 0 && tab > 0);
        rewardHeading.text = "累计充值";
        if (tab == 2) rewardHeading.text = "今年累计充值";
        RefreshWallet();
        DrawShelves();
        DrawGoods();
        goods.verticalNormalizedPosition = 1;
    }

    /// <summary>钱包只读取角色持有的虚拟物品。</summary>
    private void RefreshWallet()
    {
        for (int i = 0; i < balances.Length; i++)
        {
            balances[i].text = "—";
            if (bag != null) balances[i].text = bag.GetVirtualItemCount(WalletIds[i]).ToString("N0");
        }
        boundBalance.text = "绑定元宝   —";
        if (boundAmount.HasValue) boundBalance.text = "绑定元宝   " + boundAmount.Value.ToString("N0");
    }

    /// <summary>切换商品搜索输入。</summary>
    public void ToggleSearch()
    {
        filterPopup.SetActive(false);
        search.gameObject.SetActive(!search.gameObject.activeSelf);
        if (search.gameObject.activeSelf) search.ActivateInputField();
        else search.text = "";
    }

    /// <summary>关键字改变时从列表顶部展示匹配项。</summary>
    /// <param name="value">输入框当前文字。</param>
    private void OnSearch(string value)
    {
        DrawGoods();
        goods.verticalNormalizedPosition = 1;
    }

    /// <summary>展开币种或分类筛选，可从类型筛选中查看关注。</summary>
    /// <param name="filter">零表示货币，一表示类型。</param>
    public void OpenFilter(int filter)
    {
        // 从当前货架收集真实可用的筛选值。
        List<string> options = new List<string> { "" };
        if (filter == 1) options.Add("已关注");
        for (int i = 0; i < catalog.products.Length; i++)
        {
            // 只有当前频道和货架的值进入选单。
            PavilionEntry row = catalog.products[i];
            if (!OnShelf(row)) continue;
            // 按触发的筛选器取对应字段。
            string value = row.currency;
            if (filter == 1) value = row.kind;
            if (!options.Contains(value)) options.Add(value);
        }
        filterPopup.SetActive(true);
        for (int i = 0; i < filterChoices.Length; i++)
        {
            filterChoices[i].gameObject.SetActive(i < options.Count);
            if (i >= options.Count) continue;
            // 每行保留自身的筛选值。
            string value = options[i];
            filterChoices[i].title.text = value;
            if (i == 0)
            {
                filterChoices[i].title.text = "全部货币";
                if (filter == 1) filterChoices[i].title.text = "全部类型";
            }
            filterChoices[i].OnPick(() =>
            {
                if (filter == 0) currency = value;
                else kind = value;
                filterPopup.SetActive(false);
                OnSearch(search.text);
            });
        }
    }

    /// <summary>切换是否仅显示角色可用等级的商品。</summary>
    public void ToggleLevel()
    {
        lowerOnly = !lowerOnly;
        OnSearch(search.text);
    }

    /// <summary>商品是否属于当前选中的陈列货架。</summary>
    /// <param name="row">目录中的陈列项。</param>
    private bool OnShelf(PavilionEntry row)
    {
        if (channel == 2) return row.channel == "商城" && row.group == catalog.mall[tab];
        if (channel == 3) return row.channel == "绑元商城" && row.group == catalog.bound[tab];
        if (row.channel != "商会" || row.group != catalog.chamber[group].name) return false;
        // 直接目录没有子分类。
        string[] children = catalog.chamber[group].children;
        return children.Length == 0 || row.shelf == children[leaf];
    }

    /// <summary>重绘商会目录，选中的一级类目可以展开或收起。</summary>
    private void DrawShelves()
    {
        if (!categories.gameObject.activeSelf) return;
        // 下一条目录行在滚动内容中的索引。
        int count = 0;
        for (int i = 0; i < catalog.chamber.Length; i++)
        {
            // 当前一级类目的稳定序号及其目录。
            int parent = i;
            // 当前要显示的一级与二级目录。
            PavilionEntry.Shelf shelf = catalog.chamber[i];
            // 右侧展开提示符。
            string arrow = "";
            if (shelf.children.Length > 0) arrow = "  ▾";
            DrawCategory(count++, shelf.name + arrow, i == group, () =>
            {
                if (group == parent) expanded = !expanded;
                else { group = parent; leaf = 0; expanded = true; }
                DrawShelves();
                OnSearch("");
            });
            if (group != i || !expanded) continue;
            for (int j = 0; j < shelf.children.Length; j++)
            {
                // 子分类序号供本行点击使用。
                int child = j;
                DrawCategory(count++, "   " + shelf.children[j], leaf == j, () =>
                {
                    leaf = child;
                    DrawShelves();
                    OnSearch("");
                });
            }
        }
        HideAfter(shelves, count);
        categories.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, count * 62);
    }

    /// <summary>填充商会一行分类。</summary>
    /// <param name="index">纵向行号。</param>
    /// <param name="caption">分类名。</param>
    /// <param name="active">是否当前分类。</param>
    /// <param name="action">点击后的目录切换。</param>
    private void DrawCategory(int index, string caption, bool active, Action action)
    {
        // 当前复用的目录按钮。
        PavilionTile row = Take(shelves, categoryTemplate, categories.content, index, 0, index * 62, 222, 56);
        row.title.text = caption;
        row.selection.SetActive(active);
        row.OnPick(action);
    }

    /// <summary>取得一个模板实例并设置其左上角坐标与尺寸。</summary>
    /// <param name="pool">相同布局的复用列表。</param>
    /// <param name="template">预制体内禁用的模板。</param>
    /// <param name="parent">滚动内容。</param>
    /// <param name="index">复用下标。</param>
    /// <param name="x">左边距。</param>
    /// <param name="y">上边距。</param>
    /// <param name="width">卡片宽度。</param>
    /// <param name="height">卡片高度。</param>
    private static PavilionTile Take(List<PavilionTile> pool, PavilionTile template, RectTransform parent, int index, int x, int y, int width, int height)
    {
        if (pool.Count == index) pool.Add(Instantiate(template, parent, false));
        // 当前下标已经有实例，可直接更新内容。
        PavilionTile tile = pool[index];
        tile.gameObject.SetActive(true);
        // 模板已采用左上锚点。
        RectTransform rect = (RectTransform)tile.transform;
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
        return tile;
    }

    /// <summary>隐藏这一轮之后多余的模板实例。</summary>
    /// <param name="pool">待回收的列表。</param>
    /// <param name="count">本次实际显示的行数。</param>
    private static void HideAfter(List<PavilionTile> pool, int count)
    {
        for (int i = count; i < pool.Count; i++) pool[i].gameObject.SetActive(false);
    }

    /// <summary>切换货架、输入或页签后，重排当前频道商品及详情。</summary>
    private void DrawGoods()
    {
        // 商会购买页为左目录、中间列表、右详情。
        bool chamber = channel == 4 && tab == 0;
        // 充值独占内容区，收藏与数量属于商品卡片。
        RectTransform region = (RectTransform)goods.transform;
        region.anchoredPosition = new Vector2(30, -216);
        region.sizeDelta = new Vector2(860, 593);
        if (channel == 0)
        {
            region.anchoredPosition = new Vector2(30, -165);
            region.sizeDelta = new Vector2(1344, 650);
            if (tab > 0) { region.anchoredPosition = new Vector2(30, -216); region.sizeDelta = new Vector2(1344, 592); }
        }
        // 商会没有商城工具栏，详情与单列货架一起从页签下方开始。
        RectTransform detailRect = (RectTransform)detail.transform;
        detailRect.anchoredPosition = new Vector2(909, -218);
        if (channel == 4)
        {
            detailRect.anchoredPosition = new Vector2(909, -167);
            region.anchoredPosition = new Vector2(30, -167);
            region.sizeDelta = new Vector2(860, 645);
            if (chamber) { region.anchoredPosition = new Vector2(266, -167); region.sizeDelta = new Vector2(622, 645); }
        }
        HideAfter(packs, 0);
        HideAfter(rewards, 0);
        HideAfter(cards, 0);
        detail.SetActive(false);
        empty.gameObject.SetActive(false);
        visible.Clear();
        selected = null;
        if (channel == 0)
        {
            DrawRecharge();
            return;
        }
        currencyLabel.text = "全部货币 ▾";
        if (currency.Length > 0) currencyLabel.text = currency + " ▾";
        typeLabel.text = "全部类型 ▾";
        if (kind.Length > 0) typeLabel.text = kind + " ▾";
        levelLabel.text = "□  隐藏高于自身等级的商品";
        if (lowerOnly) levelLabel.text = "✓  隐藏高于自身等级的商品";
        if (channel == 4 && tab == 1) ReadSaleRows();
        else
        {
            for (int i = 0; i < catalog.products.Length; i++)
            {
                // 目录行与货币、类型、等级、名称均匹配时才陈列。
                PavilionEntry row = catalog.products[i];
                if (!OnShelf(row)) continue;
                if (currency.Length > 0 && row.currency != currency) continue;
                if (kind == "已关注" && !favorites.Contains(row.key)) continue;
                if (kind.Length > 0 && kind != "已关注" && row.kind != kind) continue;
                if (lowerOnly && roleLevel > 0 && row.level > roleLevel) continue;
                if (row.name.IndexOf(search.text, StringComparison.OrdinalIgnoreCase) < 0) continue;
                visible.Add(row);
            }
        }
        // 两列货架与商会单列使用同一种可伸缩商品卡模板。
        int columns = 2;
        if (chamber) columns = 1;
        // 当前区域一列所占的宽度。
        int pitch = (int)region.sizeDelta.x / columns;
        for (int i = 0; i < visible.Count; i++)
        {
            // 捕获对应条目而非循环索引。
            PavilionEntry row = visible[i];
            // 纵向滚动内容中本次复用的商品卡。
            PavilionTile card = Take(cards, productTemplate, goods.content, i, i % columns * pitch, i / columns * 119, pitch - 9, 110);
            card.title.text = row.name;
            card.note.text = row.discount;
            if (channel == 4 && row.limit > 0) card.note.text = "日限 " + row.limit;
            if (row.stack != 0) card.note.text = "持有 " + row.available;
            card.price.text = row.price.ToString("N0") + " " + row.currency;
            PavilionTile.SetPicture(card.picture, Illustration(row));
            PavilionTile.SetPicture(card.coin, Coin(row.currency));
            card.bound.SetActive(channel == 3);
            card.favorite.gameObject.SetActive(channel == 2 || channel == 3);
            card.favorite.image.sprite = card.stars[Convert.ToInt32(favorites.Contains(row.key))];
            card.favorite.onClick.RemoveAllListeners();
            card.favorite.onClick.AddListener(() =>
            {
                if (!favorites.Remove(row.key)) favorites.Add(row.key);
                card.favorite.image.sprite = card.stars[Convert.ToInt32(favorites.Contains(row.key))];
                if (kind == "已关注") DrawGoods();
            });
            card.OnPick(() => SelectProduct(row));
        }
        goods.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ((visible.Count + columns - 1) / columns) * 119);
        if (visible.Count > 0) SelectProduct(visible[0]);
        else
        {
            empty.gameObject.SetActive(true);
            empty.text = "此货架暂无符合条件的商品";
            if (channel == 4 && tab == 1) empty.text = "包裹中暂无可出售的物品";
        }
    }

    /// <summary>只读取可出售的真实堆叠，成交接口保留独立堆叠身份。</summary>
    private void ReadSaleRows()
    {
        for (int i = 0; i < 4; i++)
        {
            // 四个物品包不含货币和穿戴中的装备。
            RoItemContainer container = bag?.GetContainer((KnapsackType)i);
            if (container == null) continue;
            // 按包格号展示。
            List<ItemBase> stacks = new List<ItemBase>(container.GetItemMap().Values);
            stacks.Sort((left, right) => left.GetBagIndex().CompareTo(right.GetBagIndex()));
            for (int j = 0; j < stacks.Count; j++)
            {
                // 真实堆叠及其策划回收价格。
                ItemBase stack = stacks[j];
                // 配置中读取名称、售价和币种。
                ItemConfigBase item = LubanMgr.Instance.GetItemConfigById(stack.GetItemID());
                if (item == null || item.SellPrice <= 0 || stack.GetItemCount() <= 0) continue;
                // 当前物品表的出售货币类型 1 对应普通铜钱。
                int wallet = Array.IndexOf(WalletIds, item.SellCurrencyType);
                if (item.SellCurrencyType == 1) wallet = 0;
                if (wallet < 0) continue;
                visible.Add(new PavilionEntry { key = item.ItemId.ToString(), channel = "商会", name = item.ItemName,
                    stack = stack.GetItemUID(), art = item.ItemId, price = item.SellPrice, currency = WalletNames[wallet],
                    available = (int)Math.Min(int.MaxValue, stack.GetItemCount()), description = item.Description });
            }
        }
    }

    /// <summary>取得商品插画或项目背包图标。</summary>
    /// <param name="row">商品配置。</param>
    private Sprite Illustration(PavilionEntry row)
    {
        if (!string.IsNullOrEmpty(row.picture)) return FindArt(row.picture);
        return ItemIcon(row.art);
    }

    /// <summary>根据物品编号查找钱包或商品目录中已有的图标。</summary>
    /// <param name="id">真实项目物品编号。</param>
    private Sprite ItemIcon(int id)
    {
        // 钱包中的货币独立于交易货架目录。
        int wallet = Array.IndexOf(WalletIds, id);
        if (wallet >= 0) return walletIcons[wallet];
        return items.GetIcon(id);
    }

    /// <summary>查找预制体已绑定的 RO 精灵。</summary>
    /// <param name="name">完整且稳定的图集内切片名。</param>
    private Sprite FindArt(string name)
    {
        for (int i = 0; i < shopArt.Length; i++) if (shopArt[i].name == name) return shopArt[i];
        return null;
    }

    /// <summary>取对应的钱包图标，绑定元宝与普通元宝用价格文字区分。</summary>
    /// <param name="name">商品标价货币。</param>
    private Sprite Coin(string name)
    {
        if (name == "绑定元宝") return walletIcons[3];
        // 其他录屏专属币种只展示名称。
        int index = Array.IndexOf(WalletNames, name);
        if (index >= 0) return walletIcons[index];
        return null;
    }

    /// <summary>切换详情，并把购买数量归一。</summary>
    /// <param name="row">点选的商品或实际背包堆叠。</param>
    private void SelectProduct(PavilionEntry row)
    {
        selected = row;
        quantity = 1;
        showSource = false;
        feedback.text = "";
        detail.SetActive(true);
        detailName.text = row.name;
        description.text = row.description;
        limitText.text = "单价   " + row.price.ToString("N0") + " " + row.currency;
        if (row.limit > 0) limitText.text = "每日限购   " + row.limit;
        if (row.stack != 0) limitText.text = "包裹中数量   " + row.available;
        PavilionTile.SetPicture(detailArt, Illustration(row));
        for (int i = 0; i < visible.Count; i++) cards[i].selection.SetActive(visible[i] == row);
        ChangeQuantity(0);
    }

    /// <summary>切换详情的商品说明与获取位置。</summary>
    public void ToggleSource()
    {
        if (selected == null) return;
        showSource = !showSource;
        description.text = selected.description;
        if (!showSource) return;
        description.text = "获取途径    " + selected.channel + "  /  " + selected.group;
        if (!string.IsNullOrEmpty(selected.shelf)) description.text += "  /  " + selected.shelf;
        if (selected.stack != 0) description.text = "物品来源    当前角色包裹";
    }

    /// <summary>增减份数，更新总额与边界按钮。</summary>
    /// <param name="step">份数增量，零表示仅刷新。</param>
    public void ChangeQuantity(int step)
    {
        if (selected == null) return;
        // 有日限时使用目录额度，有堆叠时使用当前持有量。
        int max = int.MaxValue;
        if (selected.limit > 0) max = selected.limit;
        if (selected.stack != 0) max = selected.available;
        quantity = (int)Math.Max(1L, Math.Min(max, (long)quantity + step));
        quantityText.text = quantity.ToString();
        minus.interactable = quantity > 1;
        plus.interactable = quantity < max;
        // 使用长整数避免多件物品的标价相乘溢出。
        long amount = (long)selected.price * quantity;
        total.text = "购买   " + amount.ToString("N0") + " " + selected.currency;
        if (selected.stack != 0) total.text = "出售   " + amount.ToString("N0") + " " + selected.currency;
        PavilionTile.SetPicture(totalCoin, Coin(selected.currency));
    }

    /// <summary>只发出所选商品和数量的强类型购买或出售意图。</summary>
    public void SubmitOrder()
    {
        if (selected == null) return;
        // 点击这一刻的稳定键、价格与堆叠号交给订阅方。
        PavilionEntry.Order order = new PavilionEntry.Order { Key = selected.key, Channel = selected.channel,
            Stack = selected.stack, Quantity = quantity, Currency = selected.currency, UnitPrice = selected.price };
        if (selected.stack != 0)
        {
            if (SaleRequested != null) SaleRequested(order);
            else feedback.text = "商会出售暂未开放";
        }
        else if (PurchaseRequested != null) PurchaseRequested(order);
        else feedback.text = "购买暂未开放";
    }

    /// <summary>充值页使用六档礼包，返利按独立的年度或累计进度显示。</summary>
    private void DrawRecharge()
    {
        if (tab == 0)
        {
            for (int i = 0; i < catalog.packs.Length; i++)
            {
                // 此档的录屏标价与附赠信息。
                PavilionEntry.Pack pack = catalog.packs[i];
                // 面值按三列两行排列。
                PavilionTile card = Take(packs, packTemplate, goods.content, i, i % 3 * 450, i / 3 * 322, 430, 305);
                card.title.text = pack.amount + " 元宝";
                card.note.text = "赠 " + pack.bonus + " 元宝";
                if (pack.envelopes > 0) card.note.text += "  ·  红包 ×" + pack.envelopes;
                card.price.text = "¥ " + pack.yuan;
                PavilionTile.SetPicture(card.picture, FindArt("UI_Shopping_Icon_0" + (i + 1) + ".png"));
                card.OnPick(() =>
                {
                    if (RechargeRequested != null) RechargeRequested(pack.amount);
                    else feedback.text = "充值渠道暂未开放";
                });
            }
            goods.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ((catalog.packs.Length + 2) / 3) * 322);
            return;
        }
        // 每一页只显示与该累计周期匹配的返利档位。
        int count = 0;
        // 业务数据中普通为 0，年度为 1。
        int period = tab - 1;
        for (int i = 0; i < catalog.rewards.Length; i++)
        {
            // 当前周期的一条档位记录。
            PavilionEntry.Reward reward = catalog.rewards[i];
            if (reward.annual != (period == 1)) continue;
            // 同一行关联累计门槛、奖品和状态。
            PavilionTile row = Take(rewards, rewardTemplate, goods.content, count, 0, count * 122, 1326, 112);
            count++;
            row.title.text = "— / " + reward.threshold;
            row.price.text = "待同步";
            if (progress[period].HasValue)
            {
                row.title.text = progress[period].Value + " / " + reward.threshold;
                row.price.text = "未达成";
                if (progress[period].Value >= reward.threshold) row.price.text = "可领取";
                if (claimed[period].Contains(reward.threshold)) row.price.text = "已领取";
            }
            for (int j = 0; j < row.prizes.Length; j++)
            {
                row.prizes[j].transform.parent.gameObject.SetActive(j < reward.icons.Length);
                if (j >= reward.icons.Length) continue;
                PavilionTile.SetPicture(row.prizes[j], ItemIcon(reward.icons[j]));
                row.counts[j].text = "×" + reward.counts[j];
            }
            row.OnPick(() =>
            {
                if (!progress[period].HasValue) { feedback.text = "返利进度暂未同步"; return; }
                if (claimed[period].Contains(reward.threshold)) { feedback.text = "本档奖励已领取"; return; }
                if (progress[period].Value < reward.threshold) { feedback.text = "尚未达到本档额度"; return; }
                if (RebateRequested != null) RebateRequested(reward.annual, reward.threshold);
                else feedback.text = "返利领取暂未开放";
            });
        }
        goods.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, count * 122);
    }
}
