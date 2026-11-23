using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>从装备详情打开的五页养成界面，负责分页、装备选择与只读属性展示。</summary>
public sealed class EquipmentWorkshopWindow : WindowBase
{
    /// <summary>顺序与预制体右侧导航和页面引用一一对应。</summary>
    public enum Page
    {
        Strengthen, // 强化进度、属性和材料栏。
        Socket, // 宝石方案和镶嵌孔。
        Refine, // 洗炼方案与萃石栏。
        Inherit, // 装备及材料位和三项继承。
        Upgrade // 当前装备的属性和升级状态。
    }

    /// <summary>右上角花形返回按钮。</summary>
    [SerializeField] private Button _close;
    /// <summary>展开装备来源选择的按钮。</summary>
    [SerializeField] private Button _filter;
    /// <summary>显示当前查看的装备来源。</summary>
    [SerializeField] private Text _filterLabel;
    /// <summary>叠在列表上方的两个来源选项。</summary>
    [SerializeField] private GameObject _filterMenu;
    /// <summary>切换到已装备列表的选项。</summary>
    [SerializeField] private Button _worn;
    /// <summary>切换到背包装备列表的选项。</summary>
    [SerializeField] private Button _bag;
    /// <summary>按 Page 枚举顺序排列的五个纵向分页按钮。</summary>
    [SerializeField] private Button[] _tabs;
    /// <summary>选中分页时点亮的浅色签纸。</summary>
    [SerializeField] private GameObject[] _tabPapers;
    /// <summary>各页签可着色的图形与标题，依次为图标、文字。</summary>
    [SerializeField] private Graphic[] _tabInk;
    /// <summary>五页互斥显示的内容节点。</summary>
    [SerializeField] private GameObject[] _pages;
    /// <summary>顶部铜币、银两、银票和元宝余额。</summary>
    [SerializeField] private Text[] _money;
    /// <summary>可滚动的装备列表。</summary>
    [SerializeField] private ScrollRect _scroll;
    /// <summary>预制体内用于生成可重复行的隐藏模板。</summary>
    [SerializeField] private EquipmentWorkshopRow _rowTemplate;
    /// <summary>当前容器没有装备时居中显示的说明。</summary>
    [SerializeField] private GameObject _empty;
    /// <summary>铁砧上方的装备纹框和当前装备图标。</summary>
    [SerializeField] private GameObject _hero;
    /// <summary>已选择装备的大图，五页共用。</summary>
    [SerializeField] private Image _heroIcon;
    /// <summary>镶嵌、洗炼、升级三页的装备名称。</summary>
    [SerializeField] private Text[] _itemLabels;
    /// <summary>强化页和镶嵌页的强化等级及上限。</summary>
    [SerializeField] private Text[] _strengthLabels;
    /// <summary>按当前强化等级与上限比例填充的水平进度条。</summary>
    [SerializeField] private Image _progress;
    /// <summary>进度条中心显示的实际数值。</summary>
    [SerializeField] private Text _progressLabel;
    /// <summary>强化、升级两页的基础属性列表。</summary>
    [SerializeField] private Text[] _attributeLists;
    /// <summary>镶嵌页的六个可见孔位，按配置孔数显示。</summary>
    [SerializeField] private GameObject[] _sockets;
    /// <summary>一级强化石的物品 ID，后四级按等级顺序相邻。</summary>
    [SerializeField] private int _firstStoneId;
    /// <summary>强化材料栏从一级到五级的持有数量。</summary>
    [SerializeField] private Text[] _stoneCounts;
    /// <summary>点空孔展开的宝石查看面板。</summary>
    [SerializeField] private GameObject _gemPanel;
    /// <summary>收起宝石面板，回到当前装备的孔位。</summary>
    [SerializeField] private Button _gemClose;
    /// <summary>按月光石、天河石、青金石、寿山石、孔雀石排列的品种按钮。</summary>
    [SerializeField] private Button[] _gemFamilies;
    /// <summary>与品种按钮对应的选中底色。</summary>
    [SerializeField] private GameObject[] _gemHighlights;
    /// <summary>每个品种依次为绿、蓝、紫、橙棕四张图，与背包的物品图标共用。</summary>
    [SerializeField] private Sprite[] _gemArt;
    /// <summary>各品种的透明轮廓图，供已安装的六个孔独立绘制宝石。</summary>
    [SerializeField] private Sprite[] _socketArt;
    /// <summary>六个孔各自承载透明宝石本体的图层。</summary>
    [SerializeField] private Image[] _socketIcons;
    /// <summary>空孔的加号，安装成功后隐藏。</summary>
    [SerializeField] private GameObject[] _socketPluses;
    /// <summary>各孔下方独立显示的属性名与实际等级。</summary>
    [SerializeField] private Text[] _socketLabels;
    /// <summary>按一级至十级排列的选材按钮，由库存和该装备的等级上限决定可用性。</summary>
    [SerializeField] private Button[] _gradeButtons;
    /// <summary>从一级至十级排列的宝石格图标。</summary>
    [SerializeField] private Image[] _gemGradeIcons;
    /// <summary>当前品种各级宝石的背包总数。</summary>
    [SerializeField] private Text[] _gemCounts;
    /// <summary>本次打开后选择的宝石品种序号。</summary>
    private int _gemFamily;
    /// <summary>此次打开选材面板的左起孔位下标。</summary>
    private int _selectedSocket;
    /// <summary>当前装备允许镶嵌的最高宝石等级，直接来自装备表。</summary>
    private int _maxGemGrade;
    /// <summary>当前列表中选定的装备实例；空列表返回零。</summary>
    public long SelectedItemUid
    {
        get { if (_selected == null) return 0; return _selected.Entry.ItemUid; }
    }
    /// <summary>列表已实例化的行，在切换来源时复用。</summary>
    private readonly List<EquipmentWorkshopRow> _rows = new List<EquipmentWorkshopRow>();
    /// <summary>当前选中的实际行，空列表时为 null。</summary>
    private EquipmentWorkshopRow _selected;
    /// <summary>当前导航页，切换装备时沿用。</summary>
    private Page _page;
    /// <summary>请求控制器读取已穿戴或背包中的装备。</summary>
    public event Action<bool> SourceSelected;
    /// <summary>通知控制器读取指定装备 UID 的详细属性。</summary>
    public event Action<long> EquipmentSelected;
    /// <summary>请求控制器按首个物品 ID 和连续等级数读取角色库存。</summary>
    public event Action<int, int> StockRequested;
    /// <summary>请求控制器发送装备实例、孔下标和消耗宝石的配置编号。</summary>
    public event Action<long, int, int> SocketRequested;

    /// <summary>将预制体入口绑定到界面交互。</summary>
    public override void InitWindow()
    {
        _close.onClick.AddListener(CloseWindow);
        _filter.onClick.AddListener(() => _filterMenu.SetActive(!_filterMenu.activeSelf));
        _worn.onClick.AddListener(() => SourceSelected?.Invoke(true));
        _bag.onClick.AddListener(() => SourceSelected?.Invoke(false));
        _gemClose.onClick.AddListener(() => _gemPanel.SetActive(false));
        // index 对应一个可点击的装备孔位。
        for (int index = 0; index < _sockets.Length; index++)
        {
            // 按钮属于装备上的这个孔，捕获后续请求所需的下标。
            int slot = index;
            _sockets[index].GetComponent<Button>().onClick.AddListener(() => OpenGemPanel(slot));
        }
        // 每格选定一个独立物品，只有服务器确认后才会更新装备孔。
        for (int index = 0; index < _gradeButtons.Length; index++)
        {
            // 一级与品种起始 ID 相同，后续等级依次递增。
            int offset = index;
            _gradeButtons[index].onClick.AddListener(() =>
                SocketRequested?.Invoke(SelectedItemUid, _selectedSocket, EquipmentGemRules.FirstItemId(_gemFamily) + offset));
        }
        // index 对应预制体中一个宝石品种的按钮。
        for (int index = 0; index < _gemFamilies.Length; index++)
        {
            // 捕获当前按钮的品种序号，以后点击时沿用。
            int family = index;
            _gemFamilies[index].onClick.AddListener(() => ShowGemFamily(family));
        }
        // index 是对应 Page 的固定按钮序号。
        for (int index = 0; index < _tabs.Length; index++)
        {
            // 此按钮始终对应同一页。
            Page page = (Page)index;
            _tabs[index].onClick.AddListener(() => SelectPage(page));
        }
    }

    /// <summary>将四项真实账户余额显示在标题栏。</summary>
    /// <param name="copper">铜币数量。</param>
    /// <param name="silver">银两数量。</param>
    /// <param name="ticket">银票数量。</param>
    /// <param name="yuanbao">元宝数量。</param>
    public void RenderMoney(long copper, long silver, long ticket, long yuanbao)
    {
        _money[0].text = copper.ToString();
        _money[1].text = silver.ToString();
        _money[2].text = ticket.ToString();
        _money[3].text = yuanbao.ToString();
    }

    /// <summary>刷新来源、列表与选中条目，初次打开时将入口装备滚动到视野内。</summary>
    /// <param name="entries">当前来源下按实际格位排序的装备。</param>
    /// <param name="worn">当前来源是否为已穿戴栏。</param>
    /// <param name="selectedUid">入口选中的装备实例，切换来源时为零。</param>
    public void RenderItems(List<EquipmentViewData> entries, bool worn, long selectedUid)
    {
        _filterLabel.text = "背包装备";
        if (worn) _filterLabel.text = "已装备";
        _filterMenu.SetActive(false);
        _selected = null;
        // index 对应当前数据及其复用行，剩余旧行在下一次循环中隐藏。
        for (int index = 0; index < entries.Count; index++)
        {
            if (index == _rows.Count) _rows.Add(Instantiate(_rowTemplate, _scroll.content));
            // 当前实例行，存下 UID 后才允许玩家点击。
            EquipmentWorkshopRow row = _rows[index];
            row.Bind(entries[index], SelectRow);
            row.gameObject.SetActive(true);
            if (index == 0 || entries[index].ItemUid == selectedUid) _selected = row;
        }
        // index 指向旧列表中本次不再需要的行。
        for (int index = entries.Count; index < _rows.Count; index++) _rows[index].gameObject.SetActive(false);
        _empty.SetActive(_selected == null);
        _hero.SetActive(_selected != null);
        SelectPage(_page);
        _scroll.StopMovement();
        _scroll.verticalNormalizedPosition = 1f;
        if (_selected == null) return;
        SelectRow(_selected);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_scroll.content);
        // 超出遮罩的实际内容高度，用于定位从背包或穿戴栏点进来的那一件。
        float overflow = _scroll.content.rect.height - _scroll.viewport.rect.height;
        if (overflow > 0f)
        {
            // 布局组件按行的轴心定位，先换算到上边沿，避免首行被裁掉半行。
            RectTransform rowRect = (RectTransform)_selected.transform;
            // 相对列表顶端的纵坐标，第一行的上沿为零。
            float rowTop = rowRect.anchoredPosition.y + (1f - rowRect.pivot.y) * rowRect.rect.height;
            _scroll.verticalNormalizedPosition = Mathf.Clamp01(1f + rowTop / overflow);
        }
    }

    /// <summary>切换中央页面和右栏选中颜色，并保持当前装备。</summary>
    /// <param name="page">需要展示的养成页。</param>
    public void SelectPage(Page page)
    {
        _page = page;
        _filterMenu.SetActive(false);
        _gemPanel.SetActive(false);
        // index 为固定的右侧页签和内容下标。
        for (int index = 0; index < _tabs.Length; index++)
        {
            // 高亮底板与中央内容指向同一页。
            bool active = index == (int)page;
            _tabPapers[index].SetActive(active);
            _pages[index].SetActive(active && _selected != null);
            // RO 蓝色选中条上用白色，浅底页签上用蓝灰色。
            Color32 ink = new Color32(65, 89, 128, 255);
            if (active) ink = Color.white;
            _tabInk[index * 2].color = ink;
            _tabInk[index * 2 + 1].color = ink;
        }
        // index 遍历已创建行，只更新当前容器可见的摘要。
        for (int index = 0; index < _rows.Count; index++)
        {
            if (_rows[index].gameObject.activeSelf) _rows[index].Display(page, _rows[index] == _selected);
        }
        RefreshStock();
    }

    /// <summary>从装备空孔打开宝石面板，先展示第一种宝石。</summary>
    /// <param name="slot">当前装备可以镶嵌的空孔下标。</param>
    private void OpenGemPanel(int slot)
    {
        _selectedSocket = slot;
        _gemPanel.SetActive(true);
        // 按当前装备部位过滤品种，选择首个匹配项。
        int first = -1;
        for (int family = 0; family < _gemFamilies.Length; family++)
        {
            // 服务器同样按实际部位验证该品种。
            bool allowed = EquipmentGemRules.Allows(_selected.Entry.EquipType, family);
            _gemFamilies[family].gameObject.SetActive(allowed);
            if (allowed && first < 0) first = family;
        }
        if (first >= 0) ShowGemFamily(first);
    }

    /// <summary>显示一个品种的一至十级原图和每级库存。</summary>
    /// <param name="family">所选按钮在品种列表中的序号，从零开始。</param>
    private void ShowGemFamily(int family)
    {
        _gemFamily = family;
        // index 依次指向各品种的选中底色。
        for (int index = 0; index < _gemHighlights.Length; index++)
            _gemHighlights[index].SetActive(index == family);
        // index 是等级减一，每两级换底色，七级至十级共用橙棕底。
        for (int index = 0; index < _gemGradeIcons.Length; index++)
        {
            // 当前品种的四种品质图按等级从低到高排列。
            int band = Mathf.Min(index / 2, 3);
            _gemGradeIcons[index].sprite = _gemArt[family * 4 + band];
        }
        RefreshStock();
    }

    /// <summary>查询屏幕上正在展示的一组材料，同步计入背包内所有堆叠。</summary>
    /// <param name="changedItemId">增量变化的物品编号，零表示进入页面时读取整组。</param>
    public void RefreshStock(int changedItemId = 0)
    {
        if (_selected == null) return;
        // 默认读取强化石栏的起始物品编号。
        int firstId = _firstStoneId;
        // 需要查询的等级数量与当前格数一致。
        int levels = _stoneCounts.Length;
        if (_gemPanel.activeSelf)
        {
            firstId = EquipmentGemRules.FirstItemId(_gemFamily);
            levels = _gemCounts.Length;
        }
        else if (_page != Page.Strengthen) return;
        if (changedItemId != 0 && (changedItemId < firstId || changedItemId >= firstId + levels)) return;
        StockRequested?.Invoke(firstId, levels);
    }

    /// <summary>将控制器查到的库存写回一个等级格。</summary>
    /// <param name="index">所查等级在当前组中的索引，一级为零。</param>
    /// <param name="count">角色实际持有的总件数，包含绑定与非绑定堆叠。</param>
    public void RenderStockCount(int index, int count)
    {
        if (_gemPanel.activeSelf)
        {
            _gemCounts[index].text = count.ToString();
            _gradeButtons[index].interactable = count > 0 && index < _maxGemGrade;
        }
        else _stoneCounts[index].text = count.ToString();
    }

    /// <summary>从服务器同步的装备实例绘制各孔，品种图透明，物品格的等级底板只在背包及选材面板使用。</summary>
    /// <param name="equip">当前选中装备的最新实例。</param>
    /// <param name="cap">该装备允许的最高宝石等级。</param>
    public void RenderGems(EquipBase equip, int cap)
    {
        _maxGemGrade = cap;
        for (int slot = 0; slot < _sockets.Length; slot++)
        {
            // 配置编号同时记录宝石品种与等级。
            int gemId = equip.GemItemIds[slot];
            // 找到对应透明原图及真实物品的属性简称。
            int family;
            int grade;
            bool filled = EquipmentGemRules.TryRead(gemId, out family, out grade);
            _socketIcons[slot].enabled = filled;
            _socketPluses[slot].SetActive(!filled);
            _sockets[slot].GetComponent<Button>().interactable = !filled;
            _socketLabels[slot].text = "";
            if (!filled) continue;
            _socketIcons[slot].sprite = GetGemArt(family);
            // 显示物品表中的属性和实际等级；空孔不占文字空间。
            cfg.ItemInfo gem = LubanMgr.Instance.GetItemInfoById(gemId);
            if (gem != null) _socketLabels[slot].text = gem.Attribute + " · " + grade + "级";
            if (slot == _selectedSocket) _gemPanel.SetActive(false);
        }
    }

    /// <summary>取装备详情与镶嵌页共用的透明宝石图，同品种的十个等级使用同一张。</summary>
    /// <param name="family">已由物品编号解析出的合法宝石品种。</param>
    public Sprite GetGemArt(int family) { return _socketArt[family]; }

    /// <summary>按所选行更新主图、名称和当前页属性。</summary>
    /// <param name="row">装备列表中被点击或首次定位到的行。</param>
    private void SelectRow(EquipmentWorkshopRow row)
    {
        if (_selected != row) _gemPanel.SetActive(false);
        if (_selected != null) _selected.Display(_page, false);
        _selected = row;
        row.Display(_page, true);
        // 捕获本次装备摘要，防止切换后旧的异步图标覆盖当前选择。
        EquipmentViewData entry = row.Entry;
        _heroIcon.enabled = false;
        ResourceMgr.Instance.LoadSpriteAsync(entry.IconPath, sprite =>
        {
            if (_selected == null || _selected.Entry != entry) return;
            _heroIcon.sprite = sprite;
            _heroIcon.enabled = sprite != null;
        });
        _itemLabels[0].text = entry.Name;
        _itemLabels[1].text = entry.Name;
        if (entry.RequiredLevel > 0) _itemLabels[1].text += "（" + row.LevelText + "）";
        _itemLabels[2].text = _itemLabels[1].text;
        EquipmentSelected?.Invoke(entry.ItemUid);
    }

    /// <summary>把当前装备的属性、强化等级和宝石孔数同步到对应页面。</summary>
    /// <param name="detail">与详情窗同源的装备属性和孔数。</param>
    /// <param name="level">实例当前的强化等级。</param>
    /// <param name="limit">装备表设置的强化等级上限。</param>
    public void RenderDetail(EquipmentDetailViewData detail, int level, int limit)
    {
        // 纯色进度条的右锚点直接表示占满轨道的比例，零级时宽度为零。
        float fraction = 0f;
        if (limit > 0) fraction = Mathf.Clamp01((float)level / limit);
        _progress.rectTransform.anchorMax = new Vector2(fraction, 1f);
        _progressLabel.text = level + " / " + limit;
        // index 对应强化、镶嵌两页需要显示的强化等级标题。
        for (int index = 0; index < _strengthLabels.Length; index++)
            _strengthLabels[index].text = "强化 " + level + "/" + limit;
        // index 对应需要重复显示同一装备基础属性的页面。
        for (int index = 0; index < _attributeLists.Length; index++)
            _attributeLists[index].text = detail.BaseAttributeText;
        // index 对应从左到右的宝石孔，展示数量由装备配置决定。
        for (int index = 0; index < _sockets.Length; index++)
            _sockets[index].SetActive(index < detail.GemSlotCount);
    }
}
