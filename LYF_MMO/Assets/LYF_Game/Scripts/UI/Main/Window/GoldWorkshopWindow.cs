using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>魔剑士金装作坊。合成选成品，进阶和升级按穿戴栏、背包的真实 UID 选择装备。</summary>
public sealed class GoldWorkshopWindow : WindowBase
{
    /// <summary>与背包和交易界面共用的物品插画目录。</summary>
    [SerializeField] private TradeMarketCatalog _art;
    /// <summary>右上关闭作坊的按钮。</summary>
    [SerializeField] private Button _close;
    /// <summary>右栏依次为合成、进阶、升级。</summary>
    [SerializeField] private Button[] _tabs;
    /// <summary>三页各自的选中色条。</summary>
    [SerializeField] private GameObject[] _tabHighlights;
    /// <summary>顶部铜钱、银两、银票和元宝的库存文本。</summary>
    [SerializeField] private Text[] _wallets;
    /// <summary>左栏的两种分类或来源按钮。</summary>
    [SerializeField] private Button[] _filters;
    /// <summary>合成显示套装和部位，成长显示已装备和背包。</summary>
    [SerializeField] private Text[] _filterLabels;
    /// <summary>左栏实际可滚动的列表。</summary>
    [SerializeField] private ScrollRect _scroll;
    /// <summary>隐藏的列表行原型。</summary>
    [SerializeField] private GoldWorkshopRow _template;
    /// <summary>穿戴栏或背包没有金装时的提示。</summary>
    [SerializeField] private Text _empty;
    /// <summary>铁砧上显示的当前成品图标及边框。</summary>
    [SerializeField] private GameObject _hero;
    /// <summary>铁砧中央的金装原画。</summary>
    [SerializeField] private Image _heroIcon;
    /// <summary>选中成品或实例的配置名称。</summary>
    [SerializeField] private Text _title;
    /// <summary>显示当前进度及本步的目标等级或阶数。</summary>
    [SerializeField] private Text _progress;
    /// <summary>切换成品属性面板的预览按钮。</summary>
    [SerializeField] private Button _preview;
    /// <summary>盖在铁砧上的基础属性预览卡。</summary>
    [SerializeField] private GameObject _previewCard;
    /// <summary>收起属性预览的按钮。</summary>
    [SerializeField] private Button _closePreview;
    /// <summary>预览卡使用真实配置基础属性及单位。</summary>
    [SerializeField] private Text _attributes;
    /// <summary>进阶和升级时列在铁砧两侧的当前、目标属性卡。</summary>
    [SerializeField] private GameObject[] _comparison;
    /// <summary>使用同一份真实基础属性，等待策划提供成长表。</summary>
    [SerializeField] private Text[] _comparisonValues;
    /// <summary>目标属性卡标题显示本次要去的等级或阶数。</summary>
    [SerializeField] private Text _targetTitle;
    /// <summary>最多三种实物材料的卡片根节点。</summary>
    [SerializeField] private GameObject[] _materialSlots;
    /// <summary>材料实物图标，与卡片顺序相同。</summary>
    [SerializeField] private Image[] _materialIcons;
    /// <summary>材料真实名称。</summary>
    [SerializeField] private Text[] _materialNames;
    /// <summary>每种材料的持有数与本步需求数。</summary>
    [SerializeField] private Text[] _counts;
    /// <summary>本次操作的铜钱工费。</summary>
    [SerializeField] private Text _fee;
    /// <summary>当前操作可提交时点亮的确认按钮。</summary>
    [SerializeField] private Button _confirm;
    /// <summary>随所属操作显示合成、进阶或升级。</summary>
    [SerializeField] private Text _confirmLabel;
    /// <summary>缺方、余额不足或真实结算回执。</summary>
    [SerializeField] private Text _notice;
    /// <summary>按来源读取玩家当前的金装实例。</summary>
    public Func<bool, List<EquipBase>> Equipment;
    /// <summary>读取角色各包中的材料总量或虚拟钱包余额。</summary>
    public Func<int, long> Owned;
    /// <summary>经现有装备协议发送本次选择，断线返回假。</summary>
    public Func<EquipOperationReq, bool> Submit;
    /// <summary>按右栏顺序选择的作坊操作。</summary>
    private static readonly EquipOperation[] Operations = { EquipOperation.ForgeGold, EquipOperation.AdvanceGold, EquipOperation.UpgradeGold };
    /// <summary>与右栏顺序相同的按钮文案。</summary>
    private static readonly string[] Captions = { "合成", "进阶", "升级" };
    /// <summary>连续八件金装的部位分类名。</summary>
    private static readonly string[] Parts = { "主武器", "副武器", "头部", "上衣", "腰带", "下装", "护腕", "鞋履" };
    /// <summary>已生成的左栏行，刷新数据时复用。</summary>
    private readonly List<GoldWorkshopRow> _rows = new List<GoldWorkshopRow>();
    /// <summary>本次绘制已绑定的行数。</summary>
    private int _used;
    /// <summary>当前页，0合成、1进阶、2升级。</summary>
    private int _page;
    /// <summary>合成时表示按部位，成长时表示装备背包。</summary>
    private bool _secondFilter;
    /// <summary>展开的部位下标，按套装时0表示展开、-1表示收起。</summary>
    private int _group;
    /// <summary>当前选定的成品配置编号。</summary>
    private int _id;
    /// <summary>选定装备的原始 UID，合成目录中为零。</summary>
    private long _uid;
    /// <summary>当前行对应的最新实例，合成或空列表时为空。</summary>
    private EquipBase _equip;
    /// <summary>目前在界面中展示的单步配方。</summary>
    private GoldWorkshopRecipe _recipe;
    /// <summary>已发出一次操作直至收到服务器回包，防止连续点击多做一件。</summary>
    private bool _submitting;

    /// <summary>绑定正式预制体上的导航、分类、成品预览和确认按钮。</summary>
    public override void InitWindow()
    {
        _close.onClick.AddListener(CloseWindow);
        _preview.onClick.AddListener(() => _previewCard.SetActive(!_previewCard.activeSelf));
        _closePreview.onClick.AddListener(() => _previewCard.SetActive(false));
        _confirm.onClick.AddListener(Confirm);
        // index 是右侧按钮在三个操作中的固定位置。
        for (int index = 0; index < _tabs.Length; index++)
        {
            // 捕获独立的页签序号。
            int page = index;
            _tabs[index].onClick.AddListener(() => Open(page, false, 0));
        }
        _filters[0].onClick.AddListener(() => Filter(false));
        _filters[1].onClick.AddListener(() => Filter(true));
    }

    /// <summary>从主城或装备详情切换到指定操作，并定位页面对应的成品或实例。</summary>
    /// <param name="page">0合成、1进阶、2升级。</param>
    /// <param name="bag">成长时是否从背包选取。</param>
    /// <param name="uid">需要预选的实例 UID，零时选列表第一件。</param>
    public void Open(int page, bool bag, long uid)
    {
        _page = page;
        _secondFilter = bag;
        _uid = uid;
        _id = GoldWorkshopRecipe.FirstItem + 1;
        _group = 0;
        _previewCard.SetActive(false);
        _scroll.verticalNormalizedPosition = 1;
        Refresh(true);
    }

    /// <summary>切换套装或部位分类，或切换已装备与背包来源。</summary>
    /// <param name="second">是否选择右边的筛选项。</param>
    private void Filter(bool second)
    {
        _secondFilter = second;
        _group = 0;
        if (_page == 0 && second) _group = _id - GoldWorkshopRecipe.FirstItem;
        _uid = 0;
        _previewCard.SetActive(false);
        _scroll.verticalNormalizedPosition = 1;
        Refresh(true);
    }

    /// <summary>按当前角色的数据刷新库存和目标，列表只在物品或来源变化时重绘。</summary>
    /// <param name="listChanged">是否需要重新查找当前容器的实例。</param>
    public void Refresh(bool listChanged)
    {
        if (listChanged)
        {
            _used = 0;
            _equip = null;
            if (_page == 0) MakeCatalog();
            else MakeInventory();
            // index 指向上一次比当前多出的可复用行。
            for (int index = _used; index < _rows.Count; index++) _rows[index].gameObject.SetActive(false);
        }
        // index 对应当前高亮页签。
        for (int index = 0; index < _tabs.Length; index++) _tabHighlights[index].SetActive(index == _page);
        _filterLabels[0].text = "套装分类"; _filterLabels[1].text = "部位分类";
        if (_page != 0) { _filterLabels[0].text = "已装备"; _filterLabels[1].text = "背包装备"; }
        _filters[0].interactable = _secondFilter;
        _filters[1].interactable = !_secondFilter;
        // 钱包顺序为铜钱、银两、银票和元宝。
        int[] money = { 101, 102, 106, 103 };
        for (int index = 0; index < money.Length; index++) _wallets[index].text = Count(money[index]).ToString("N0");
        DrawRecipe();
    }

    /// <summary>绘制套装或部位折叠目录，其中包括可预览的待补配方。</summary>
    private void MakeCatalog()
    {
        if (!_secondFilter)
        {
            Header(0, "辉银套装");
            if (_group < 0) return;
        }
        // slot 是辉银套装内的部位顺序。
        for (int slot = 0; slot < Parts.Length; slot++)
        {
            if (_secondFilter) { Header(slot, Parts[slot]); if (_group != slot) continue; }
            // 本行捕获自己的成品编号。
            int id = GoldWorkshopRecipe.FirstItem + slot;
            Row().Bind(Name(id), Parts[slot] + "  ·  60级", _art.GetIcon(id), _id == id,
                () => { _id = id; Refresh(true); });
        }
    }

    /// <summary>增加可折叠的分类标题。</summary>
    /// <param name="index">展开时要记录的分类编号。</param>
    /// <param name="caption">套装或装备部位名。</param>
    private void Header(int index, string caption)
    {
        // 向下、向右的标志跟随分类展开状态。
        string mark = "▸  ";
        if (_group == index) mark = "▾  ";
        Row().Bind(mark + caption, "", null, false, () =>
        {
            if (_group == index) _group = -1;
            else _group = index;
            Refresh(true);
        });
    }

    /// <summary>读取当前来源的真实金装实例，按 UID 区分同名同部位装备。</summary>
    private void MakeInventory()
    {
        // 控制器已经按实际格位对当前来源排序。
        List<EquipBase> list = Equipment?.Invoke(!_secondFilter);
        _id = 0;
        if (list == null || list.Count == 0) { _uid = 0; return; }
        _equip = list[0];
        // index 对应真实格位顺序，优先保留上次选中的 UID。
        for (int index = 0; index < list.Count; index++)
            if (list[index].GetItemUID() == _uid) { _equip = list[index]; break; }
        _uid = _equip.GetItemUID();
        _id = _equip.GetItemID();
        // index 指向接下来要显示的一个物品实例。
        for (int index = 0; index < list.Count; index++)
        {
            // 每个行按钮保存具体实例，不会误点同部位的另一件。
            EquipBase entry = list[index];
            Row().Bind(Name(entry.GetItemID()), GoldWorkshopRecipe.Level(entry.GoldLevelSteps) + "级  ·  " + (entry.GoldTierSteps + 1) + "阶",
                _art.GetIcon(entry.GetItemID()), entry == _equip, () => { _uid = entry.GetItemUID(); Refresh(true); });
        }
    }

    /// <summary>取得下一条可复用的列表行。</summary>
    private GoldWorkshopRow Row()
    {
        if (_used == _rows.Count) _rows.Add(Instantiate(_template, _scroll.content));
        return _rows[_used++];
    }

    /// <summary>读取某一类真实库存，未绑定角色时显示零。</summary>
    /// <param name="id">材料或货币编号。</param>
    private long Count(int id) { return Owned?.Invoke(id) ?? 0L; }

    /// <summary>成品从装备表取名，材料从道具表取名。</summary>
    /// <param name="id">有效的作坊物品编号。</param>
    private static string Name(int id)
    {
        if (GoldWorkshopRecipe.IsGold(id)) return LubanMgr.Instance.GetEquipInfoById(id).EquipName;
        return LubanMgr.Instance.GetItemInfoById(id).Name;
    }

    /// <summary>按当前实例生成目标、预览、材料和底部工费。</summary>
    private void DrawRecipe()
    {
        // 当前列不含可操作的实例时保留导航和来源切换。
        bool selected = _id != 0;
        _hero.SetActive(selected);
        _preview.gameObject.SetActive(selected);
        _empty.gameObject.SetActive(!selected);
        _confirmLabel.text = Captions[_page];
        _confirm.interactable = false;
        _notice.text = "";
        _comparison[0].SetActive(selected && _page != 0);
        _comparison[1].SetActive(false);
        if (!selected)
        {
            _previewCard.SetActive(false);
            _title.text = ""; _progress.text = ""; _fee.text = "—"; _recipe = null;
            _empty.text = "当前没有已穿戴的辉银金装";
            if (_secondFilter) _empty.text = "装备背包中还没有辉银金装";
        }
        else
        {
            // 实例已升的十级档数，合成页从60级开始。
            int levels = _equip?.GoldLevelSteps ?? 0;
            // 实例已升的阶数，合成页从一阶开始。
            int tiers = _equip?.GoldTierSteps ?? 0;
            _recipe = GoldWorkshopRecipe.Get(Operations[_page], _id, levels, tiers);
            _heroIcon.sprite = _art.GetIcon(_id);
            _title.text = Name(_id);
            _progress.text = GoldWorkshopRecipe.Level(levels) + "级  ·  " + (tiers + 1) + "阶";
            if (_recipe.Unavailable.Length == 0 && _page == 1) _progress.text = (tiers + 1) + " 阶   →   " + _recipe.Target + " 阶";
            if (_recipe.Unavailable.Length == 0 && _page == 2) _progress.text = GoldWorkshopRecipe.Level(levels) + " 级   →   " + _recipe.Target + " 级";
            _attributes.text = Attributes(_id);
            if (_page != 0)
            {
                _comparisonValues[0].text = _attributes.text;
                _comparisonValues[1].text = _attributes.text;
                _comparison[1].SetActive(_recipe.Unavailable.Length == 0);
                if (_page == 1) _targetTitle.text = _recipe.Target + "阶属性";
                else _targetTitle.text = _recipe.Target + "级属性";
            }
            _fee.text = _recipe.Copper.ToString("N0");
            _notice.text = _recipe.Unavailable;
            if (_notice.text.Length > 0) _fee.text = "—";
        }
        // 凑齐每一项并有足够铜钱才允许点击提交。
        bool ready = selected && _recipe.Unavailable.Length == 0;
        // index 为最多三个材料格的序号。
        for (int index = 0; index < _materialSlots.Length; index++)
        {
            // 每次切换配方时隐藏不使用的格子。
            bool visible = selected && index < _recipe.Materials.Count;
            _materialSlots[index].SetActive(visible);
            if (!visible) continue;
            // 该格的图标和分母来自与服务端共用的配方。
            ItemDesc mat = _recipe.Materials[index];
            // 分子为当前角色实际库存。
            long stock = Count(mat.ItemID);
            _materialIcons[index].sprite = _art.GetIcon(mat.ItemID);
            _materialNames[index].text = Name(mat.ItemID);
            _counts[index].text = stock + " / " + mat.ItemCount;
            _counts[index].color = new Color32(39, 100, 86, 255);
            if (stock < mat.ItemCount)
            {
                _counts[index].color = new Color32(164, 59, 48, 255);
                ready = false;
                if (_recipe.Unavailable.Length == 0) _notice.text = "所需材料不足";
            }
        }
        if (ready && Count(101) < _recipe.Copper) { ready = false; _notice.text = "铜钱不足"; }
        _confirm.interactable = ready && !_submitting;
    }

    /// <summary>生成当前基础属性，无成长表时各阶各级保持配置值。</summary>
    /// <param name="id">正在预览的成品编号。</param>
    private static string Attributes(int id)
    {
        // 魔剑士金装沿用物品配置的六个基因槽位。
        cfg.EquipInfo info = LubanMgr.Instance.GetEquipInfoById(id);
        // 属性名称通过配置基因编号取得。
        int[] ids = { info.GeneId0, info.GeneId1, info.GeneId2, info.GeneId3, info.GeneId4, info.GeneId5 };
        // 与各槽位相对应的真实基础值。
        int[] values = { info.GeneValue0, info.GeneValue1, info.GeneValue2, info.GeneValue3, info.GeneValue4, info.GeneValue5 };
        // 每行一个属性和值，百分比依照基因值类型显示。
        StringBuilder lines = new StringBuilder();
        for (int index = 0; index < ids.Length; index++)
        {
            if (ids[index] == 0) continue;
            // 当前槽位的名称、单位等基础数据。
            cfg.Gene gene = LubanMgr.Instance.GetGeneInfoById(ids[index]);
            if (gene == null) continue;
            lines.Append(KnapsackCtrl.ShortGeneName(gene.EffectDesc)).Append("    ").Append(values[index]);
            if (gene.ValueType == 1) lines.Append('%');
            lines.AppendLine();
        }
        return lines.ToString().TrimEnd();
    }

    /// <summary>只发送目标和实例，材料与货币由服务器一次结算。</summary>
    private void Confirm()
    {
        if (!_confirm.interactable) return;
        // 客户端不发送价格或自算库存。
        EquipOperationReq request = new EquipOperationReq { Operation = Operations[_page], GoldItemId = _id, ItemUid = _uid, GoldTarget = _recipe.Target };
        _submitting = Submit?.Invoke(request) == true;
        _confirm.interactable = !_submitting;
        if (_submitting) _notice.text = "正在" + Captions[_page] + "…";
        else _notice.text = "未连接服务器";
    }

    /// <summary>收到服务端回执后恢复按钮，并显示本次操作的实际结果。</summary>
    /// <param name="reply">角色服发回的装备事务结果。</param>
    public void Finish(EquipOperationRet reply)
    {
        _submitting = false;
        if (!isActiveAndEnabled) return;
        Refresh(true);
        _notice.text = reply.GoldMessage;
        if (_notice.text.Length == 0) _notice.text = "作坊操作失败";
    }

    /// <summary>切换角色或退出窗口时丢弃旧会话的待提交状态。</summary>
    public override void CloseWindow() { _submitting = false; base.CloseWindow(); }
}
