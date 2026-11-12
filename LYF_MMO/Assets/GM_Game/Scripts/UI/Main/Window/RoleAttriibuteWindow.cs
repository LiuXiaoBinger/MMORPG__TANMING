using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 角色装备窗口，只负责装备数据到固定槽位的布局映射。
/// </summary>
public class RoleAttriibuteWindow : WindowBase
{
    /// <summary>窗口的实际显隐通知，控制器据此订阅或解除基因列表事件。</summary>
    public event Action<bool> GeneVisibilityChanged;
    /// <summary>正式预制体中的基因滚动内容容器。</summary>
    [SerializeField] private RectTransform _geneContent;
    /// <summary>正式预制体中默认隐藏的完整基因行模板。</summary>
    [SerializeField] private RoleGeneItemWidget _geneRowTemplate;
    /// <summary>正式预制体中的无生效基因提示。</summary>
    [SerializeField] private TMP_Text _geneEmptyText;
    /// <summary>本窗口已创建的行，列表变化时按索引复用，窗口销毁时随层级释放。</summary>
    private readonly List<RoleGeneItemWidget> geneRows = new List<RoleGeneItemWidget>();
    /// <summary>当前可见且有有限寿命的行；永久基因不参加时间刷新。</summary>
    private readonly List<RoleGeneItemWidget> timedGeneRows = new List<RoleGeneItemWidget>();
    /// <summary>距上一次倒计时显示刷新的秒数。</summary>
    private float geneDisplayElapsed;

    /// <summary>真实窗口打开时通知控制器读取当前节点，重开也能显示最新列表。</summary>
    private void OnEnable()
    {
        // 页签与高级行的绑定只做一次，重开窗口沿用已有行。
        InitializeAdvancedPanel();
        // 整行可见性在首次打开时收集行根，重开沿用缓存。
        InitializeRowVisibility();
        RefreshAfterLayout();
        GeneVisibilityChanged?.Invoke(true);
    }

    /// <summary>隐藏时停止订阅及时间刷新，并释放对旧角色节点的引用。</summary>
    private void OnDisable()
    {
        GeneVisibilityChanged?.Invoke(false);
        timedGeneRows.Clear();
        // 窗口对象可以复用，行本身保留但不再持有角色数据。
        for (int index = 0; index < geneRows.Count; index++) geneRows[index].Clear();
    }

    /// <summary>可见时每四分之一秒检查有限节点的显示秒数，属性列表不逐帧计算。</summary>
    private void Update()
    {
        if (timedGeneRows.Count == 0) return;
        geneDisplayElapsed += Time.unscaledDeltaTime;
        if (geneDisplayElapsed < 0.25f) return;
        geneDisplayElapsed = 0;
        // 每行仅当秒数改变时才写 TMP 文本。
        for (int index = 0; index < timedGeneRows.Count; index++) timedGeneRows[index].RefreshTime();
    }

    /// <summary>只在列表、层数或次数变化时更新当前可见行，滚动区域负责容纳全部根。</summary>
    /// <param name="items">控制器提供的当前完整展示列表，元素引用真实节点。</param>
    public void RenderGenes(IReadOnlyList<RoleGeneViewData> items)
    {
        timedGeneRows.Clear();
        geneDisplayElapsed = 0;
        _geneEmptyText.gameObject.SetActive(items.Count == 0);
        // 正式行模板的全部控件引用在预制体中绑定，运行时只实例化列表行。
        for (int index = 0; index < items.Count; index++)
        {
            if (index == geneRows.Count) geneRows.Add(Instantiate(_geneRowTemplate, _geneContent));
            // 同索引复用当前窗口的展示行，不给业务节点增加生命周期。
            RoleGeneItemWidget row = geneRows[index];
            row.gameObject.SetActive(true);
            row.Bind(items[index]);
            if (row.HasCountdown) timedGeneRows.Add(row);
        }
        // 删除项对应的旧行立即隐藏，避免同数量替换或卸装留下旧文字。
        for (int index = items.Count; index < geneRows.Count; index++)
        {
            geneRows[index].Clear();
            geneRows[index].gameObject.SetActive(false);
        }
    }

    /// <summary>已穿戴装备格点击事件，携带装备实例 UID。</summary>
    public event Action<long> EquipmentSelected;

    /// <summary>将装备格点击转交控制器打开详情。</summary>
    /// <param name="itemUid">被点击的已穿戴装备唯一编号。</param>
    private void OnEquipmentSelected(long itemUid)
    {
        EquipmentSelected?.Invoke(itemUid);
    }
    /// <summary>从装备区域寻找预制体固定槽位，建立部位到控件的映射。</summary>
    private void Start()
    {
        InitializeEquipSlots();
    }

    /// <summary>关闭角色属性，并沿I键关闭流程恢复打开前的角色镜头。</summary>
    public override void CloseWindow()
    {
        base.CloseWindow();
        CameraMgr.Instance.RoleAttrWindowAngle(this);
    }

    /// <summary>装备部位节点的共同父对象，子节点名按 EquipType 查找。</summary>
    [SerializeField, Header("穿戴装备父组件")] private Transform _content;

    /// <summary>原角色信息区的昵称文本。</summary>
    [SerializeField] private TMP_Text _nicknameText;
    /// <summary>原职业区域中的职业名文本。</summary>
    [SerializeField] private TMP_Text _jobNameText;
    /// <summary>原生命条上的当前值与上限文本。</summary>
    [SerializeField] private TMP_Text _hpText;
    /// <summary>原法力条上的当前值与上限文本。</summary>
    [SerializeField] private TMP_Text _mpText;
    /// <summary>原角色生命条，值使用0至1比例。</summary>
    [SerializeField] private Slider _hpSlider;
    /// <summary>原角色法力条，值使用0至1比例。</summary>
    [SerializeField] private Slider _mpSlider;
    /// <summary>按 RoleAttributeViewData 约定顺序保存原16个属性数值文本。</summary>
    [SerializeField] private TMP_Text[] _attributeValues;

    // ============================================================
    // 基础/高级/生活三页签切换：页签、高级滚动区与六组折叠头都由预制体序列化引用提供。
    // ============================================================
    /// <summary>切回基础属性的页签按钮。</summary>
    [SerializeField] private Button _tabBase;
    /// <summary>切到高级属性的页签按钮。</summary>
    [SerializeField] private Button _tabAdvanced;
    /// <summary>切到生活属性的页签按钮。</summary>
    [SerializeField] private Button _tabLife;
    /// <summary>基础页签背景图，切换时改配色。</summary>
    [SerializeField] private Image _tabBaseImage;
    /// <summary>高级页签背景图，切换时改配色。</summary>
    [SerializeField] private Image _tabAdvancedImage;
    /// <summary>生活页签背景图，切换时改配色。</summary>
    [SerializeField] private Image _tabLifeImage;
    /// <summary>基础页签文字，切换时改配色。</summary>
    [SerializeField] private TMP_Text _tabBaseLabel;
    /// <summary>高级页签文字，切换时改配色。</summary>
    [SerializeField] private TMP_Text _tabAdvancedLabel;
    /// <summary>生活页签文字，切换时改配色。</summary>
    [SerializeField] private TMP_Text _tabLifeLabel;
    /// <summary>基础属性滚动区，高级与生活模式下整体隐藏。</summary>
    [SerializeField] private GameObject _baseScroll;
    /// <summary>高级属性滚动区，预制体里默认隐藏。</summary>
    [SerializeField] private GameObject _advancedScroll;
    /// <summary>生活属性容器，预制体里默认隐藏；当前只放占位提示。</summary>
    [SerializeField] private GameObject _lifePanel;
    /// <summary>六个高级分组根节点，每个内部固定含 Header 与 Rows 两个子节点。</summary>
    [SerializeField] private Transform[] _advancedGroups;
    /// <summary>六个分组的折叠按钮，与 _advancedGroups 同序。</summary>
    [SerializeField] private Button[] _advancedHeaders;
    /// <summary>六个分组的折叠指示文字，与 _advancedGroups 同序。</summary>
    [SerializeField] private TMP_Text[] _advancedArrows;
    /// <summary>预制体中的高级属性行模板，运行时按静态属性表实例化。</summary>
    [SerializeField] private RectTransform _advancedRowTemplate;

    /// <summary>页签选中态的背景配色，饱和蓝 #3F7CCF。</summary>
    private static readonly Color TabSelectedColor = new Color(0.247f, 0.486f, 0.812f, 1f);
    /// <summary>页签未选中态的背景配色，浅蓝 #DFEBF8。</summary>
    private static readonly Color TabNormalColor = new Color(0.874f, 0.922f, 0.973f, 1f);
    /// <summary>页签选中态的文字配色，选中蓝底用白字。</summary>
    private static readonly Color TabSelectedTextColor = new Color(1f, 1f, 1f, 1f);
    /// <summary>页签未选中态的文字配色，浅蓝底用深蓝字 #1E4169。</summary>
    private static readonly Color TabNormalTextColor = new Color(0.118f, 0.255f, 0.412f, 1f);
    /// <summary>分组展开时的指示符号。</summary>
    private const string GroupExpandedMark = "-";
    /// <summary>分组收起时的指示符号。</summary>
    private const string GroupCollapsedMark = "+";

    /// <summary>左侧属性区的三种展示模式，三个页签互斥切换。</summary>
    private enum AttributePanelMode
    {
        /// <summary>16 行基础属性。</summary>
        Base = 0,
        /// <summary>六组 44 项高级属性。</summary>
        Advanced = 1,
        /// <summary>生活属性占位页，当前没有权威数据。</summary>
        Life = 2,
    }

    /// <summary>当前停留的页签，默认基础页。</summary>
    private AttributePanelMode attributeMode;
    /// <summary>各分组是否展开，长度与 _advancedGroups 一致。</summary>
    private bool[] groupExpanded;
    /// <summary>高级属性行缓存，顺序与 RoleAttributeViewData.AdvancedEntries 一致。</summary>
    private readonly List<AdvancedRowWidget> advancedRows = new List<AdvancedRowWidget>();
    /// <summary>行模板是否已经实例化，避免每次显示重复创建。</summary>
    private bool advancedBuilt;

    /// <summary>高级属性行的两列文本缓存，刷新时只改文字。</summary>
    private sealed class AdvancedRowWidget
    {
        /// <summary>左侧属性名称。</summary>
        public TMP_Text Label;
        /// <summary>右侧数值，右对齐。</summary>
        public TMP_Text Value;
    }

    /// <summary>原属性面板中渲染控制器提供的最终属性和资源。</summary>
    /// <param name="data">服务器最终属性转换成的展示值，不含客户端概率推算。</param>
    public void RenderAttributes(RoleAttributeViewData data)
    {
        _nicknameText.text = data.Nickname;
        _jobNameText.text = data.JobName;
        _hpText.text = data.HpText;
        _mpText.text = data.MpText;
        _hpSlider.value = data.HpRatio;
        _mpSlider.value = data.MpRatio;
        // 原16个固定属性行与纯展示数组同序，不创建额外属性控件。
        for (int index = 0; index < _attributeValues.Length; index++)
        {
            _attributeValues[index].text = data.AttributeValues[index];
        }
        // 高级页签的 44 行只在数值变化时改文字，行本身常驻。
        RenderAdvancedValues(data.AdvancedValues);
    }

    /// <summary>首次显示时实例化高级行并绑定页签与折叠头，重复调用直接返回。</summary>
    private void InitializeAdvancedPanel()
    {
        if (advancedBuilt)
        {
            return;
        }
        if (_advancedRowTemplate == null || _advancedGroups == null)
        {
            return;
        }
        advancedBuilt = true;
        // 首次打开时六组默认收起，直接能看见六个标题；bool 数组默认值就是 false。
        groupExpanded = new bool[_advancedGroups.Length];
        BuildAdvancedRows();
        if (_tabBase != null)
        {
            _tabBase.onClick.AddListener(ShowBaseAttributes);
        }
        if (_tabAdvanced != null)
        {
            _tabAdvanced.onClick.AddListener(ShowAdvancedAttributes);
        }
        if (_tabLife != null)
        {
            _tabLife.onClick.AddListener(ShowLifeAttributes);
        }
        // 折叠头按下标绑定，不为六组各写一个方法。
        for (int index = 0; index < _advancedHeaders.Length; index++)
        {
            if (_advancedHeaders[index] == null)
            {
                continue;
            }
            // 闭包需要固定的分组下标，不能直接捕获循环变量。
            int groupIndex = index;
            _advancedHeaders[index].onClick.AddListener(delegate { ToggleGroup(groupIndex); });
        }
        ApplyAttributeMode();
    }

    /// <summary>按静态属性表把固定行模板实例化到所属分组，并把名称写入左列。</summary>
    private void BuildAdvancedRows()
    {
        // 静态表当前已经填到第几项，六组按配置项数顺序切分。
        int entryIndex = 0;
        for (int group = 0; group < _advancedGroups.Length; group++)
        {
            Transform rowsRoot = _advancedGroups[group].Find("Rows");
            if (rowsRoot == null)
            {
                continue;
            }
            // 当前分组应包含的属性项数，越界时按表尾截断。
            int size = RoleAttributeViewData.AdvancedGroupSizes[group];
            for (int offset = 0; offset < size; offset++)
            {
                if (entryIndex >= RoleAttributeViewData.AdvancedEntries.Length)
                {
                    break;
                }
                AdvancedRowWidget row = CreateAdvancedRow(rowsRoot, entryIndex);
                row.Label.text = RoleAttributeViewData.AdvancedEntries[entryIndex].Label;
                entryIndex++;
            }
            rowsRoot.gameObject.SetActive(groupExpanded[group]);
            ApplyGroupArrow(group);
        }
    }

    /// <summary>取已缓存的行，或在分组内新建一行并缓存两列文本。</summary>
    /// <param name="rowsRoot">该分组固定命名的行容器。</param>
    /// <param name="entryIndex">静态属性表中的下标，同时作为行缓存下标。</param>
    private AdvancedRowWidget CreateAdvancedRow(Transform rowsRoot, int entryIndex)
    {
        if (entryIndex < advancedRows.Count)
        {
            return advancedRows[entryIndex];
        }
        // 行模板在预制体里默认隐藏，实例化后单独打开。
        RectTransform root = Instantiate(_advancedRowTemplate, rowsRoot);
        root.gameObject.SetActive(true);
        AdvancedRowWidget row = new AdvancedRowWidget();
        // 行根与透明组同物体，登记到高级页列表供整行刷新复用。
        advancedRowGroups.Add(ResolveRowGroup(root));
        row.Label = FindRowText(root, "name");
        row.Value = FindRowText(root, "arrtval");
        advancedRows.Add(row);
        return row;
    }

    /// <summary>按节点名取行内文本，预制体未提供时返回 null 由调用方跳过。</summary>
    private static TMP_Text FindRowText(Transform rowRoot, string nodeName)
    {
        Transform node = rowRoot.Find(nodeName);
        if (node == null)
        {
            return null;
        }
        return node.GetComponent<TMP_Text>();
    }

    /// <summary>把控制器给出的 44 项正文写入高级行，缺项显示空串。</summary>
    /// <param name="values">与静态属性表同序的已格式化正文。</param>
    public void RenderAdvancedValues(string[] values)
    {
        InitializeAdvancedPanel();
        for (int index = 0; index < advancedRows.Count; index++)
        {
            if (advancedRows[index].Value == null)
            {
                continue;
            }
            if (values == null || index >= values.Length)
            {
                advancedRows[index].Value.text = string.Empty;
                continue;
            }
            advancedRows[index].Value.text = values[index];
        }
    }

    /// <summary>基础页签点击：切回 16 行基础属性。</summary>
    private void ShowBaseAttributes()
    {
        attributeMode = AttributePanelMode.Base;
        ApplyAttributeMode();
    }

    /// <summary>高级页签点击：切到六组 44 项高级属性。</summary>
    private void ShowAdvancedAttributes()
    {
        attributeMode = AttributePanelMode.Advanced;
        ApplyAttributeMode();
    }

    /// <summary>生活页签点击：切到生活占位页，当前没有可信数值来源。</summary>
    private void ShowLifeAttributes()
    {
        attributeMode = AttributePanelMode.Life;
        ApplyAttributeMode();
    }

    /// <summary>按当前页签切换三个内容区的显隐与页签配色，同一时刻只显示一个。</summary>
    private void ApplyAttributeMode()
    {
        // 三个内容区共用同一块矩形，互斥显示避免叠字。
        bool showBase = attributeMode == AttributePanelMode.Base;
        // 高级滚动区只在高级页签下打开，内部保留六组折叠状态。
        bool showAdvanced = attributeMode == AttributePanelMode.Advanced;
        if (_baseScroll != null)
        {
            _baseScroll.SetActive(showBase);
        }
        if (_advancedScroll != null)
        {
            _advancedScroll.SetActive(showAdvanced);
        }
        if (_lifePanel != null)
        {
            _lifePanel.SetActive(attributeMode == AttributePanelMode.Life);
        }
        ApplyTabStyle(_tabBaseImage, _tabBaseLabel, showBase);
        ApplyTabStyle(_tabAdvancedImage, _tabAdvancedLabel, showAdvanced);
        ApplyTabStyle(_tabLifeImage, _tabLifeLabel, attributeMode == AttributePanelMode.Life);
        // 页签切换后内容区尺寸已变，重新排版再同步整行可见性。
        RefreshAfterLayout();
    }

    /// <summary>选中页签用高亮底加浅色字，未选中用暗底加灰字。</summary>
    /// <param name="background">页签背景图，可为空。</param>
    /// <param name="label">页签文字，可为空。</param>
    /// <param name="selected">是否处于选中态。</param>
    private static void ApplyTabStyle(Image background, TMP_Text label, bool selected)
    {
        if (selected)
        {
            if (background != null)
            {
                background.color = TabSelectedColor;
            }
            if (label != null)
            {
                label.color = TabSelectedTextColor;
            }
            return;
        }
        if (background != null)
        {
            background.color = TabNormalColor;
        }
        if (label != null)
        {
            label.color = TabNormalTextColor;
        }
    }

    /// <summary>折叠或展开单个分组，只改该组的行容器与指示符号。</summary>
    /// <param name="groupIndex">分组下标，越界时忽略。</param>
    private void ToggleGroup(int groupIndex)
    {
        if (groupExpanded == null || groupIndex < 0)
        {
            return;
        }
        if (groupIndex >= groupExpanded.Length)
        {
            return;
        }
        groupExpanded[groupIndex] = !groupExpanded[groupIndex];
        ApplyGroupState(groupIndex);
    }

    /// <summary>按缓存状态刷新单个分组的行容器与指示符号。</summary>
    private void ApplyGroupState(int groupIndex)
    {
        if (_advancedGroups == null || groupIndex >= _advancedGroups.Length)
        {
            return;
        }
        Transform rowsRoot = _advancedGroups[groupIndex].Find("Rows");
        if (rowsRoot != null)
        {
            rowsRoot.gameObject.SetActive(groupExpanded[groupIndex]);
        }
        ApplyGroupArrow(groupIndex);
        // 展开或收起改变内容高度，排版后重新判定整行可见性。
        RefreshAfterLayout();
    }

    /// <summary>只刷新折叠指示符号，展开显示减号、收起显示加号。</summary>
    private void ApplyGroupArrow(int groupIndex)
    {
        if (_advancedArrows == null || groupIndex >= _advancedArrows.Length)
        {
            return;
        }
        if (_advancedArrows[groupIndex] == null)
        {
            return;
        }
        if (groupExpanded[groupIndex])
        {
            _advancedArrows[groupIndex].text = GroupExpandedMark;
            return;
        }
        _advancedArrows[groupIndex].text = GroupCollapsedMark;
    }

    /// <summary>已经找到并初始化的固定装备槽，刷新时先全部清空再填入当前穿戴。</summary>
    private readonly Dictionary<EquipType, EquipSlotWidget> _equipSlots =
        new Dictionary<EquipType, EquipSlotWidget>();

    /// <summary>用 obj 中的装备展示列表重填固定部位槽；未穿戴部位保持空槽。</summary>
    public override void ReFreshUI(object obj)
    {
        InitializeEquipSlots();
        // 控制器提供的当前穿戴列表。
        List<EquipmentViewData> equipments = obj as List<EquipmentViewData>;
        if (equipments == null)
        {
            return;
        }

        // 已登记部位的快照，用来先清掉上一次展示。
        List<EquipType> slotTypes = new List<EquipType>(_equipSlots.Keys);
        for (int index = 0; index < slotTypes.Count; index++)
        {
            _equipSlots[slotTypes[index]].Clear();
        }
        for (int index = 0; index < equipments.Count; index++)
        {
            // 当前待显示的装备摘要。
            EquipmentViewData equipment = equipments[index];
            // 与该装备部位对应的预制体槽位。
            EquipSlotWidget slot;
            if (equipment != null && _equipSlots.TryGetValue(equipment.EquipType, out slot))
            {
                slot.RefreshUI(equipment);
            }
        }
    }

    /// <summary>首次按枚举查找已有部位节点，必要时给槽位补组件；建立映射后不重复初始化。</summary>
    private void InitializeEquipSlots()
    {
        if (_content == null || _equipSlots.Count > 0)
        {
            return;
        }
        // 所有装备部位枚举，预制体未提供的部位会跳过。
        EquipType[] equipTypes = (EquipType[])Enum.GetValues(typeof(EquipType));
        for (int index = 0; index < equipTypes.Length; index++)
        {
            // 当前处理的装备部位。
            EquipType equipType = equipTypes[index];
            // 按部位名称定位外层节点。
            Transform equipRoot = _content.Find(GetEquipRootName(equipType));
            if (equipRoot == null)
            {
                continue;
            }
            // 该部位下固定命名的装备格子节点。
            Transform slotRoot = equipRoot.Find("EquipSlotWidget");
            if (slotRoot == null)
            {
                continue;
            }
            // 槽位展示组件，旧预制体未挂载时在此补上。
            EquipSlotWidget slot = slotRoot.GetComponent<EquipSlotWidget>();
            if (slot == null)
            {
                slot = slotRoot.gameObject.AddComponent<EquipSlotWidget>();
            }
            slot.Initialize();
            slot.EquipmentSelected += OnEquipmentSelected;
            slot.Clear();
            _equipSlots[equipType] = slot;
        }
    }

    /// <summary>把 equipType 转成预制体节点名，腰部沿用现有 WaistWaist 拼写。</summary>
    private static string GetEquipRootName(EquipType equipType)
    {
        if (equipType == EquipType.WaistWaist)
        {
            return "WaistWaist";
        }
        return equipType.ToString();
    }

    // ============================================================
    // 整行可见性：只显示完全落在视口内的行，半行整组透明并禁射线，
    // 不 SetActive，避免布局重排把滚动位置带跑。
    // ============================================================
    /// <summary>行边界的本地坐标容差，覆盖 0.01 缩放的离屏场景。</summary>
    private const float RowEdgeTolerance = 0.02f;
    /// <summary>基础页每行的透明组，顺序与滚动内容子节点一致。</summary>
    private readonly List<CanvasGroup> baseRowGroups = new List<CanvasGroup>();
    /// <summary>高级页每行与每组标题的透明组，按创建登记顺序刷新。</summary>
    private readonly List<CanvasGroup> advancedRowGroups = new List<CanvasGroup>();
    /// <summary>基础滚动区，滚动变化时按视口同步整行可见性。</summary>
    private ScrollRect baseScrollRect;
    /// <summary>高级滚动区，与基础页同法处理。</summary>
    private ScrollRect advancedScrollRect;
    /// <summary>行根世界角点缓存，复用同一数组避免每次刷新分配。</summary>
    private readonly Vector3[] rowCorners = new Vector3[4];
    /// <summary>行可见性是否已初始化。</summary>
    private bool rowVisibilityBuilt;

    /// <summary>首次收集两个滚动区的行透明组并订阅滚动变化。</summary>
    private void InitializeRowVisibility()
    {
        if (rowVisibilityBuilt)
        {
            return;
        }
        rowVisibilityBuilt = true;
        baseScrollRect = _baseScroll.GetComponent<ScrollRect>();
        advancedScrollRect = _advancedScroll.GetComponent<ScrollRect>();
        CollectBaseRowGroups();
        CollectAdvancedHeaderGroups();
        baseScrollRect.onValueChanged.AddListener(OnAttributeScrolled);
        advancedScrollRect.onValueChanged.AddListener(OnAttributeScrolled);
    }

    /// <summary>从基础滚动内容子节点收集行透明组，每行只补一次。</summary>
    private void CollectBaseRowGroups()
    {
        baseRowGroups.Clear();
        Transform content = baseScrollRect.content;
        for (int index = 0; index < content.childCount; index++)
        {
            // 基础行按内容子节点顺序排列，与 _attributeValues 同序。
            RectTransform row = content.GetChild(index) as RectTransform;
            baseRowGroups.Add(ResolveRowGroup(row));
        }
    }

    /// <summary>把六个折叠组头登记进高级页列表，避免标题被视口切成半截。</summary>
    private void CollectAdvancedHeaderGroups()
    {
        for (int index = 0; index < _advancedGroups.Length; index++)
        {
            // 组头固定在分组根下的 Header 节点。
            Transform header = _advancedGroups[index].Find("Header");
            if (header == null)
            {
                continue;
            }
            advancedRowGroups.Add(ResolveRowGroup(header as RectTransform));
        }
    }

    /// <summary>滚轮、拖拽与惯性都走滚动回调，每次变化同步一次当前页。</summary>
    private void OnAttributeScrolled(Vector2 position)
    {
        RefreshRowVisibility();
    }

    /// <summary>只刷新当前活动页的整行显隐，隐藏页不测量。</summary>
    private void RefreshRowVisibility()
    {
        if (!rowVisibilityBuilt)
        {
            return;
        }
        if (attributeMode == AttributePanelMode.Advanced)
        {
            ApplyRowVisibility(advancedRowGroups, advancedScrollRect.viewport);
            return;
        }
        ApplyRowVisibility(baseRowGroups, baseScrollRect.viewport);
    }

    /// <summary>把行角点转到视口本地坐标，完全落在视口上下边内才显示。</summary>
    private void ApplyRowVisibility(List<CanvasGroup> groups, RectTransform viewport)
    {
        // 视口本地坐标下的可用上下边界，向外放宽容差，
        // 恰贴上下边的完整首末行仍判为可见。
        float top = viewport.rect.yMax + RowEdgeTolerance;
        float bottom = viewport.rect.yMin - RowEdgeTolerance;
        Matrix4x4 worldToLocal = viewport.worldToLocalMatrix;
        for (int index = 0; index < groups.Count; index++)
        {
            CanvasGroup group = groups[index];
            // 透明组与行根同物体，直接取矩形即可。
            RectTransform row = group.GetComponent<RectTransform>();
            if (!row.gameObject.activeInHierarchy)
            {
                continue;
            }
            row.GetWorldCorners(rowCorners);
            // 第 0 角点先换到视口本地空间，同时作为上下边初值，
            // 后续比较都在同一坐标系内进行。
            float firstLocalY = worldToLocal.MultiplyPoint3x4(rowCorners[0]).y;
            float rowTop = firstLocalY;
            float rowBottom = firstLocalY;
            for (int corner = 1; corner < 4; corner++)
            {
                float localY = worldToLocal.MultiplyPoint3x4(rowCorners[corner]).y;
                if (localY > rowTop)
                {
                    rowTop = localY;
                }
                if (localY < rowBottom)
                {
                    rowBottom = localY;
                }
            }
            bool fullyInside = rowTop <= top;
            if (fullyInside)
            {
                fullyInside = rowBottom >= bottom;
            }
            ApplyRowState(group, fullyInside);
        }
    }

    /// <summary>完整行恢复正常显示，半行整组透明并关闭射线。</summary>
    private static void ApplyRowState(CanvasGroup group, bool fullyInside)
    {
        if (fullyInside)
        {
            group.alpha = 1f;
            group.blocksRaycasts = true;
            group.interactable = true;
            return;
        }
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
    }

    /// <summary>取行的透明组，缺失时补一个只用于显隐的组件。</summary>
    private static CanvasGroup ResolveRowGroup(RectTransform row)
    {
        CanvasGroup group = row.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = row.gameObject.AddComponent<CanvasGroup>();
        }
        return group;
    }

    /// <summary>页签或折叠变化后只重建当前活动内容，再同步整行可见性。</summary>
    private void RefreshAfterLayout()
    {
        if (!rowVisibilityBuilt)
        {
            return;
        }
        if (attributeMode == AttributePanelMode.Advanced)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(advancedScrollRect.content);
            ApplyRowVisibility(advancedRowGroups, advancedScrollRect.viewport);
            return;
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(baseScrollRect.content);
        ApplyRowVisibility(baseRowGroups, baseScrollRect.viewport);
    }
}
