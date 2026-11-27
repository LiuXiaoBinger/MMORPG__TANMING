using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>装备养成窗口内的孔位选择和三栏合成视图；实际扣料、扣币和发货等待服务器。</summary>
public sealed class GemWorkshopPanel : MonoBehaviour
{
    /// <summary>装备孔的选择宝石弹层，关闭后回到原装备。</summary>
    [SerializeField] private GameObject _picker;
    /// <summary>当前打开的选择宝石标题与孔号。</summary>
    [SerializeField] private Text _pickerTitle;
    /// <summary>分组、实际拥有等级和快捷操作所在的滚动容器。</summary>
    [SerializeField] private RectTransform _choices;
    /// <summary>关闭选择宝石弹层。</summary>
    [SerializeField] private Button _closePicker;
    /// <summary>共享的三栏宝石合成页面。</summary>
    [SerializeField] private GameObject _synthesis;
    /// <summary>回到进入合成前的镶嵌或选材页面。</summary>
    [SerializeField] private Button _closeSynthesis;
    /// <summary>合成窗口显示玩家当前真实铜币余额。</summary>
    [SerializeField] private Text _wallet;
    /// <summary>所选的目标宝石名称和等级。</summary>
    [SerializeField] private Text _title;
    /// <summary>合成目标在物品表中的属性。</summary>
    [SerializeField] private Text _attribute;
    /// <summary>打开目标宝石品种与等级选择。</summary>
    [SerializeField] private Button _target;
    /// <summary>空目标时显示的加号。</summary>
    [SerializeField] private GameObject _plus;
    /// <summary>合成目标按等级着色的宝石图。</summary>
    [SerializeField] private Image _targetArt;
    /// <summary>目标已选时显示在图标角落，提示可点图更换目标。</summary>
    [SerializeField] private GameObject _switch;
    /// <summary>提交按钮的文字在材料不足时使用深色以便阅读。</summary>
    [SerializeField] private Text _composeLabel;
    /// <summary>中央进度圆中来源于背包的绿色份额。</summary>
    [SerializeField] private Image _bagFill;
    /// <summary>中央进度圆中计入商会的总份额，背包层覆盖其下段。</summary>
    [SerializeField] private Image _shopFill;
    /// <summary>选中原料折算份数与目标所需份数。</summary>
    [SerializeField] private Text _progress;
    /// <summary>已有合成目标时显示成功率。</summary>
    [SerializeField] private Text _chance;
    /// <summary>本次选材需要的商会铜币总价。</summary>
    [SerializeField] private Text _cost;
    /// <summary>提交当前材料组合的合成按钮。</summary>
    [SerializeField] private Button _compose;
    /// <summary>背包里没有此类宝石时的空状态。</summary>
    [SerializeField] private GameObject _emptyBag;
    /// <summary>按等级排列的真实背包材料。</summary>
    [SerializeField] private RectTransform _ownedRows;
    /// <summary>一级至三级商会宝石的列表。</summary>
    [SerializeField] private RectTransform _marketRows;
    /// <summary>切换合成目标时打开的品种与等级面板。</summary>
    [SerializeField] private GameObject _targets;
    /// <summary>供玩家选择目标品种的滚动列表。</summary>
    [SerializeField] private RectTransform _families;
    /// <summary>按二至七级排列的可合成目标按钮。</summary>
    [SerializeField] private Button[] _grades;
    /// <summary>选择目标时正在浏览的品种。</summary>
    [SerializeField] private Text _targetFamily;
    /// <summary>取消选择，返回原有合成材料组合。</summary>
    [SerializeField] private Button _closeTargets;
    /// <summary>从空孔弹层进入的单颗商会购买面板。</summary>
    [SerializeField] private GameObject _buyPanel;
    /// <summary>取消直接购买并返回对应孔。</summary>
    [SerializeField] private Button _closeBuy;
    /// <summary>展示三个可购买等级和剩余额度。</summary>
    [SerializeField] private RectTransform _buyRows;
    /// <summary>隐藏的清单行模板，按实际品种和库存复用。</summary>
    [SerializeField] private GemWorkshopLine _line;
    /// <summary>所属养成窗口提供当前装备、背包查询和协议入口。</summary>
    private EquipmentWorkshopWindow _owner;
    /// <summary>打开选择弹层的孔号和实际装备部位。</summary>
    private int _slot;
    /// <summary>当前空孔所属部位，决定能列出的品种。</summary>
    private EquipType _part;
    /// <summary>选定装备的宝石镶嵌等级上限。</summary>
    private int _socketCap;
    /// <summary>展开的品种；负一代表当前全部收起。</summary>
    private int _expanded = -1;
    /// <summary>合成或便捷购买正在浏览的宝石品种。</summary>
    private int _family = -1;
    /// <summary>当前目标等级，打开空页面时为零。</summary>
    private int _grade;
    /// <summary>选择目标对话框中的临时品种，取消时保留原目标。</summary>
    private int _browsing;
    /// <summary>按等级记录当前手选的背包材料颗数。</summary>
    private readonly int[] _bag = new int[EquipmentGemRules.ComposeCap];
    /// <summary>按等级记录本单所选的商会材料颗数。</summary>
    private readonly int[] _market = new int[EquipmentGemRules.MarketGrades];
    /// <summary>由服务端返回的三个商品当前剩余额度。</summary>
    private readonly int[] _remaining = new int[EquipmentGemRules.MarketGrades];
    /// <summary>已收到最新商会报价的品种，切换品种即清除。</summary>
    private int _quoted = -1;
    /// <summary>当前有购买或合成正在提交，收到回包后再次允许提交。</summary>
    private bool _pending;
    /// <summary>选择目标时先用包内材料，首次取得服务器额度时再补齐差额。</summary>
    private bool _autoMarket;
    /// <summary>控制器从角色虚拟物品栏读到的可用铜币。</summary>
    private long _copper;

    /// <summary>绑定已序列化的按钮和宿主窗口。</summary>
    /// <param name="owner">装备详情正常入口注册的养成窗口。</param>
    public void Init(EquipmentWorkshopWindow owner)
    {
        _owner = owner;
        _closePicker.onClick.AddListener(() => _picker.SetActive(false));
        _closeSynthesis.onClick.AddListener(() => { _synthesis.SetActive(false); _targets.SetActive(false); Refresh(); });
        _closeBuy.onClick.AddListener(() => { _buyPanel.SetActive(false); Refresh(); });
        _closeTargets.onClick.AddListener(() => _targets.SetActive(false));
        _target.onClick.AddListener(OpenTargets);
        _compose.onClick.AddListener(Compose);
        for (int index = 0; index < _grades.Length; index++)
        {
            // 每个按钮捕获其自身的二至七级目标。
            int grade = index + 2;
            _grades[index].onClick.AddListener(() => SetTarget(_browsing, grade));
        }
        Hide();
    }

    /// <summary>切换源装备或分页时收起当前养成窗口的全部弹层。</summary>
    public void Hide()
    {
        _picker.SetActive(false);
        _buyPanel.SetActive(false);
        _targets.SetActive(false);
        _synthesis.SetActive(false);
    }

    /// <summary>成功镶嵌当前孔后回到装备展示。</summary>
    /// <param name="slot">收到服务器装备增量的已填充孔下标。</param>
    public void Filled(int slot)
    {
        if (_picker.activeSelf && _slot == slot) _picker.SetActive(false);
    }

    /// <summary>显示该孔允许的所有品种，有库存和零库存均占一行。</summary>
    /// <param name="part">当前装备部位。</param>
    /// <param name="slot">左起零基孔号。</param>
    /// <param name="cap">装备表中的最高镶嵌等级。</param>
    public void OpenSocket(EquipType part, int slot, int cap)
    {
        _part = part;
        _slot = slot;
        _socketCap = cap;
        _expanded = -1;
        _pickerTitle.text = "选择宝石  ·  第" + (slot + 1) + "孔";
        _picker.SetActive(true);
        DrawPicker();
        _choices.GetComponentInParent<ScrollRect>().verticalNormalizedPosition = 1f;
    }

    /// <summary>打开空目标，或从已展开的品种直接预览二级合成。</summary>
    /// <param name="family">来自选择宝石行的品种；从装备页进入传负一。</param>
    /// <param name="targetGrade">默认预览二级，从背包进入时预览所点宝石的下一级。</param>
    public void OpenSynthesis(int family, int targetGrade = 2)
    {
        _family = family;
        _grade = 0;
        _quoted = -1;
        _targets.SetActive(false);
        _synthesis.SetActive(true);
        if (family >= 0) SetTarget(family, targetGrade);
        else DrawRecipe();
    }

    /// <summary>将最新铜币同步到窗口，预览不足时禁用提交。</summary>
    /// <param name="balance">角色当前虚拟铜币余额。</param>
    public void Money(long balance)
    {
        _copper = balance;
        _wallet.text = balance.ToString("N0");
        Refresh();
    }

    /// <summary>背包增量只重绘当前可见面板，选材数量随实际库存缩减。</summary>
    public void Refresh()
    {
        if (_buyPanel.activeSelf) DrawBuy();
        else if (_synthesis.activeSelf) DrawRecipe();
        else if (_picker.activeSelf) DrawPicker();
    }

    /// <summary>接收商会额度和交易回执，成功时清除已结算材料。</summary>
    /// <param name="reply">原网关返回的权威装备业务结果。</param>
    public void Receive(EquipOperationRet reply)
    {
        // 通过目标编号判断返回结果对应的品种，避免切换品种时串用额度。
        int family;
        int grade;
        bool same = EquipmentGemRules.TryRead(reply.GemItemId, out family, out grade) && family == _family;
        if (reply.Operation != EquipOperation.GemMarket) _pending = false;
        if (same && reply.MarketRemaining.Count == _remaining.Length)
        {
            _quoted = family;
            for (int index = 0; index < _remaining.Length; index++) _remaining[index] = reply.MarketRemaining[index];
            if (_autoMarket && _synthesis.activeSelf) AutoMarket();
        }
        if (reply.Operation == EquipOperation.ComposeGem && reply.CmdCode == CmdCode.Succeed && same)
        {
            Array.Clear(_bag, 0, _bag.Length);
            Array.Clear(_market, 0, _market.Length);
        }
        Refresh();
    }

    /// <summary>按模板数量复用滚动行。</summary>
    /// <param name="host">只存放数据行的内容节点。</param>
    /// <param name="index">此次绘制中从零计数的行下标。</param>
    private GemWorkshopLine Row(RectTransform host, int index)
    {
        if (index == host.childCount) return Instantiate(_line, host);
        return host.GetChild(index).GetComponent<GemWorkshopLine>();
    }

    /// <summary>收起上次显示、本次已无数据的多余行。</summary>
    /// <param name="host">被刷新的列表。</param>
    /// <param name="used">本次绘制的有效行数。</param>
    private static void End(RectTransform host, int used)
    {
        for (int index = used; index < host.childCount; index++) host.GetChild(index).gameObject.SetActive(false);
    }

    /// <summary>由真实物品表读取等级名称和属性简称。</summary>
    /// <param name="family">从零开始的宝石品种编号。</param>
    /// <param name="grade">从一开始的物品等级。</param>
    private static cfg.ItemInfo Item(int family, int grade)
    {
        return LubanMgr.Instance.GetItemInfoById(EquipmentGemRules.FirstItemId(family) + grade - 1);
    }

    /// <summary>组名去掉一级前缀，保留物品表的品种名。</summary>
    /// <param name="family">物品表中的宝石品种。</param>
    private static string Name(int family) { return Item(family, 1).Name.Substring(2); }

    /// <summary>显示参考图已给出的基础石点数，其他等级沿用配置里的属性名。</summary>
    /// <param name="family">五类基础石在品种表中排在最前面。</param>
    /// <param name="grade">所展示的实际等级。</param>
    public static string AttributeText(int family, int grade)
    {
        // 这些基础石的一级至四级分别为 5、10、15、25 点。
        string attribute = Item(family, grade).Attribute;
        if (family < 5 && grade <= 3) return attribute + "+" + (grade * 5);
        if (family < 5 && grade == 4) return attribute + "+25";
        return "增加" + attribute;
    }

    /// <summary>重绘空孔允许的品种，并仅展开一个品种的持有等级。</summary>
    private void DrawPicker()
    {
        // cursor 依次写入品种、等级、两个快捷入口。
        int cursor = 0;
        for (int family = 0; family < EquipmentGemRules.FamilyCount; family++)
        {
            if (!EquipmentGemRules.AllowsInSocket(_part, _slot, family)) continue;
            int current = family; // 捕获本行品种，点击后在原位展开。
            int first = EquipmentGemRules.FirstItemId(family); // 用连续等级编号累计实际背包库存。
            int total = 0;
            for (int grade = 1; grade <= _socketCap && grade <= EquipmentGemRules.GradeCount; grade++)
                total += _owner.Owned(first + grade - 1);
            string arrow = "▼";
            if (_expanded == family) arrow = "▲";
            Action expand = () => { if (_expanded == current) _expanded = -1; else _expanded = current; DrawPicker(); };
            Row(_choices, cursor++).Bind(_owner.GradeArt(family, 1), Name(family) + "（" + total + "）",
                "增加" + Item(family, 1).Attribute, "", arrow, expand, expand);
            if (_expanded != family) continue;
            for (int grade = 1; grade <= _socketCap && grade <= EquipmentGemRules.GradeCount; grade++)
            {
                int itemId = first + grade - 1; // 唯一确定这一等级，发给服务器镶嵌。
                int count = _owner.Owned(itemId); // 只列出背包中有货的等级。
                if (count < 1) continue;
                Row(_choices, cursor++).Bind(_owner.GradeArt(family, grade), Item(family, grade).Name,
                    AttributeText(family, grade), count.ToString(), "镶嵌", null, () => _owner.Socket(_slot, itemId));
            }
            Row(_choices, cursor++).BindShortcuts(
                () => OpenSynthesis(current), () => OpenBuy(current));
        }
        End(_choices, cursor);
    }

    /// <summary>打开全部品种与二至七级目标，尚未选择的目标不会消耗材料。</summary>
    private void OpenTargets()
    {
        _browsing = _family;
        if (_browsing < 0) _browsing = 0;
        _targets.SetActive(true);
        _targetFamily.text = Name(_browsing);
        for (int index = 0; index < EquipmentGemRules.FamilyCount; index++)
        {
            int current = index; // 选择品种后由右侧等级按钮确定目标。
            Action pick = () => { _browsing = current; _targetFamily.text = Name(current); };
            Row(_families, index).Bind(_owner.GradeArt(index, 1), Name(index), "增加" + Item(index, 1).Attribute, "", "", pick, null);
        }
        _families.GetComponentInParent<ScrollRect>().verticalNormalizedPosition = 1f;
    }

    /// <summary>选择目标后优先用背包中可折算的高级材料，再预览需商会补充的差额。</summary>
    /// <param name="family">目标品种。</param>
    /// <param name="grade">二至七级的目标等级。</param>
    private void SetTarget(int family, int grade)
    {
        _targets.SetActive(false);
        _family = family;
        _grade = grade;
        Array.Clear(_bag, 0, _bag.Length);
        Array.Clear(_market, 0, _market.Length);
        int left = EquipmentGemRules.Units(grade); // 距离合成一颗目标还差的一级份数。
        for (int level = grade - 1; level >= 1; level--)
        {
            int units = EquipmentGemRules.Units(level); // 一颗此级材料折算的份数。
            _bag[level - 1] = Math.Min(left / units, _owner.Owned(EquipmentGemRules.FirstItemId(family) + level - 1));
            left -= _bag[level - 1] * units;
        }
        _autoMarket = true;
        Quote();
        DrawRecipe();
    }

    /// <summary>向服务端读取当前品种的个人限购余额。</summary>
    private void Quote()
    {
        _quoted = -1;
        _owner.SendTrade(new EquipOperationReq { Operation = EquipOperation.GemMarket, GemItemId = EquipmentGemRules.FirstItemId(_family) });
    }

    /// <summary>得到商会库存后自动补足预选材料；仅点合成时才按显示的价格扣款。</summary>
    private void AutoMarket()
    {
        _autoMarket = false;
        int left = EquipmentGemRules.Units(_grade) - Total(_bag); // 仅补足该目标的差额。
        for (int index = 0; index < _market.Length && index + 1 < _grade; index++)
        {
            int unit = EquipmentGemRules.Units(index + 1); // 低档商品优先，价格按份等值。
            _market[index] = Math.Min(_remaining[index], left / unit);
            left -= _market[index] * unit;
        }
    }

    /// <summary>计算一组材料折合的一级份数。</summary>
    /// <param name="counts">按等级排列的已选数量。</param>
    private static int Total(int[] counts)
    {
        int total = 0; // 单目标最高需要六十四份。
        for (int index = 0; index < counts.Length; index++) total += counts[index] * EquipmentGemRules.Units(index + 1);
        return total;
    }

    /// <summary>渲染背包、目标进度和三个商会等级；所选份数及铜币均足够才允许提交。</summary>
    private void DrawRecipe()
    {
        bool selected = _grade > 0; // 空目标时两栏均无对应品种。
        _plus.SetActive(!selected);
        _switch.SetActive(selected);
        _composeLabel.color = new Color32(71, 88, 106, 255);
        _targetArt.enabled = selected;
        _target.interactable = !_pending;
        _title.text = "设定合成目标";
        _attribute.text = "";
        _progress.text = "";
        _chance.text = "";
        _cost.text = "";
        _bagFill.fillAmount = 0;
        _shopFill.fillAmount = 0;
        _compose.interactable = false;
        if (!selected)
        {
            _emptyBag.SetActive(true);
            End(_ownedRows, 0);
            End(_marketRows, 0);
            return;
        }
        _title.text = Item(_family, _grade).Name;
        _attribute.text = AttributeText(_family, _grade);
        _targetArt.sprite = _owner.GradeArt(_family, _grade);
        int used = 0; // 左列只展示实际拥有的该品种等级，包括不能充当本次材料的高级石。
        int first = EquipmentGemRules.FirstItemId(_family); // 紧邻编号分别指向一至十级。
        for (int grade = 1; grade <= EquipmentGemRules.GradeCount; grade++)
        {
            int owned = _owner.Owned(first + grade - 1);
            if (grade <= _bag.Length) _bag[grade - 1] = Math.Min(_bag[grade - 1], owned);
            if (owned > 0) MaterialRow(_ownedRows, used++, grade, owned, false);
        }
        End(_ownedRows, used);
        _emptyBag.SetActive(used == 0);
        for (int index = 0; index < _market.Length; index++)
        {
            if (_quoted == _family) _market[index] = Math.Min(_remaining[index], _market[index]);
            MaterialRow(_marketRows, index, index + 1, _remaining[index], true);
        }
        int bagUnits = Total(_bag); // 绿色进度对应背包的投入。
        int marketUnits = Total(_market); // 金色部分对应商会投入。
        int needed = EquipmentGemRules.Units(_grade);
        long price = (long)marketUnits * EquipmentGemRules.MarketPrice(1);
        _bagFill.fillAmount = (float)bagUnits / needed;
        _shopFill.fillAmount = (float)(bagUnits + marketUnits) / needed;
        _progress.text = (bagUnits + marketUnits) + " / " + needed;
        _cost.text = "花费  " + price.ToString("N0") + " 铜币";
        _chance.text = "成功率 100%";
        _compose.interactable = !_pending && bagUnits + marketUnits == needed && _copper >= price;
        if (marketUnits > 0 && _quoted != _family) _compose.interactable = false;
        if (_compose.interactable) _composeLabel.color = Color.white;
    }

    /// <summary>一行材料支持点击添加一颗、单独点减号撤回一颗。</summary>
    /// <param name="host">背包或商会的列表容器。</param>
    /// <param name="row">此行在可见列表中的下标。</param>
    /// <param name="grade">此行宝石的实际等级。</param>
    /// <param name="stock">真实库存或服务器返回的商会余额度。</param>
    /// <param name="shop">是否使用商会价格与额度。</param>
    private void MaterialRow(RectTransform host, int row, int grade, int stock, bool shop)
    {
        int[] counts = _bag; // 扣除和增加只改变对应来源的草稿。
        string detail = "包裹中";
        if (shop) { counts = _market; detail = EquipmentGemRules.MarketPrice(grade).ToString("N0") + " 铜币"; }
        int chosen = 0;
        if (grade <= counts.Length) chosen = counts[grade - 1];
        string amount = chosen + "/" + stock;
        if (shop && _quoted != _family) amount = "—";
        Action add = null;
        Action minus = null;
        string label = "";
        int room = EquipmentGemRules.Units(_grade) - Total(_bag) - Total(_market); // 剩余可填的一级份数。
        if (grade < _grade && chosen < stock && room >= EquipmentGemRules.Units(grade))
        {
            if ((!shop || _quoted == _family) && !_pending)
                add = () => { _autoMarket = false; counts[grade - 1]++; DrawRecipe(); };
        }
        if (chosen > 0)
        {
            label = "−";
            if (!_pending) minus = () => { _autoMarket = false; counts[grade - 1]--; DrawRecipe(); };
        }
        Row(host, row).Bind(_owner.GradeArt(_family, grade), Item(_family, grade).Name, detail, amount, label, add, minus);
    }

    /// <summary>将选中的颗数提交给服务端，预览期间不改背包。</summary>
    private void Compose()
    {
        // 发送目标配置编号与两列原料颗数，价格、成功率和结果都由服务端确定。
        EquipOperationReq req = new EquipOperationReq { Operation = EquipOperation.ComposeGem,
            GemItemId = EquipmentGemRules.FirstItemId(_family) + _grade - 1 };
        req.BagGemCounts.Add(_bag);
        req.MarketGemCounts.Add(_market);
        _pending = _owner.SendTrade(req);
        DrawRecipe();
    }

    /// <summary>从品种处打开便捷购买，显示每颗价格并等待玩家点具体档位。</summary>
    /// <param name="family">准备购买的宝石品种。</param>
    private void OpenBuy(int family)
    {
        _family = family;
        _autoMarket = false;
        _buyPanel.SetActive(true);
        Quote();
        DrawBuy();
    }

    /// <summary>在对应等级点购买只发一颗的购买请求，成功以后由背包增量刷新。</summary>
    private void DrawBuy()
    {
        for (int index = 0; index < _remaining.Length; index++)
        {
            int grade = index + 1; // 商会只出售三个低级档位。
            int price = EquipmentGemRules.MarketPrice(grade);
            string stock = "—";
            Action buy = null;
            if (_quoted == _family)
            {
                stock = "剩余 " + _remaining[index];
                if (_remaining[index] > 0 && _copper >= price && !_pending)
                    buy = () => { _pending = _owner.SendTrade(new EquipOperationReq { Operation = EquipOperation.BuyGem,
                        GemItemId = EquipmentGemRules.FirstItemId(_family) + grade - 1 }); DrawBuy(); };
            }
            Row(_buyRows, index).Bind(_owner.GradeArt(_family, grade), Item(_family, grade).Name,
                price.ToString("N0") + " 铜币", stock, "购买", null, buy);
        }
    }
}
