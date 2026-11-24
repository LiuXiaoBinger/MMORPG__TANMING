using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 武学面板窗口：按「分类 + 子页」组织界面。
/// 出战页把技能铺到人物周围（武功 8 个普通节点 + 1 个绝学大节点，轻功左侧 3 个节点 + 中央大图标），
/// 列表页按行展示技能；右侧详情始终显示当前选中的技能，秘笈分类仍是空壳页。
/// </summary>
public class SkillInfoWindow : WindowBase
{
    /// <summary>武功出战页的节点数量，前 8 个为普通节点，第 9 个为绝学大节点。</summary>
    private const int BattleNodeCount = 9;

    /// <summary>轻功出战页左侧的节点数量。</summary>
    private const int LightNodeCount = 3;

    /// <summary>武功分类编号。</summary>
    private const int CategorySkill = 0;

    /// <summary>轻功分类编号，同时包含滑行与轻功两类技能。</summary>
    private const int CategoryLight = 1;

    /// <summary>秘笈分类编号，包含出战和目录子页。</summary>
    private const int CategorySecret = 2;

    /// <summary>子页编号：出战页。</summary>
    private const int PageBattle = 0;

    /// <summary>子页编号：列表页。</summary>
    private const int PageList = 1;

    /// <summary>节点没有可展示技能时使用的两字标签。</summary>
    private const string EmptyNodeTag = "未习得";

    /// <summary>绝学技能的两字标签，用于把绝学固定放到大节点上。</summary>
    private const string UltimateTag = "绝学";

    /// <summary>技能表 Type=1 为普通攻击，不属于技能，不参与节点与列表展示。</summary>
    private const int BasicAttackType = 1;

    /// <summary>绝学升一级需要攒满的心得值，与铜币升级是两套独立机制。</summary>
    private const int UltimateExpMax = 100;

    /// <summary>使用一颗心得丹增加的心得值。</summary>
    private const int ExpItemGain = 10;

    /// <summary>技能未提供升级消耗时使用的默认铜币价格。</summary>
    private const int DefaultUpgradeCost = 40;

    /// <summary>浅色详情卡上标记可升级状态的松绿色。</summary>
    private static readonly Color UpgradeGreen = new Color(0.14f, 0.44f, 0.28f, 1f);

    /// <summary>浅色详情卡上标记升级条件不足的灰蓝色。</summary>
    private static readonly Color UpgradeGray = new Color(0.34f, 0.42f, 0.51f, 1f);

    /// <summary>已装备普通技能的雾蓝色外圈。</summary>
    private static readonly Color NodeRingNormal = new Color(0.45f, 0.67f, 0.88f, 1f);

    /// <summary>绝学大节点的金色外圈。</summary>
    private static readonly Color NodeRingUltimate = new Color(0.98f, 0.78f, 0.42f, 1f);

    /// <summary>未装备槽位的灰蓝色外圈。</summary>
    private static readonly Color NodeRingEmpty = new Color(0.31f, 0.41f, 0.54f, 1f);

    /// <summary>当前选择的技能节点使用冰蓝高光。</summary>
    private static readonly Color NodeRingSelected = new Color(0.88f, 0.96f, 1f, 1f);

    /// <summary>当前子页签或技能分类的蓝色底纹。</summary>
    private static readonly Color TabOnColor = new Color(0.25f, 0.47f, 0.76f, 1f);

    /// <summary>未选子页签的浅蓝底纹。</summary>
    private static readonly Color TabOffColor = new Color(0.87f, 0.93f, 0.99f, 1f);

    /// <summary>选中页签蓝底上的白色文字和图标。</summary>
    private static readonly Color TabOnTextColor = new Color(0.98f, 0.99f, 1f, 1f);

    /// <summary>未选页签浅底上的深蓝文字和图标。</summary>
    private static readonly Color TabOffTextColor = new Color(0.14f, 0.26f, 0.39f, 1f);

    /// <summary>当前职业名称文本。</summary>
    [SerializeField, Header("职业")] private TMP_Text _texJob;

    /// <summary>标题栏关闭按钮。</summary>
    [SerializeField, Header("关闭按钮")] private Button _btnClose;

    /// <summary>按 1920×1080 设计坐标摆放内容的节点，尺寸变化时缩放，屏幕背景独立铺满。</summary>
    [SerializeField, Header("布局")] private RectTransform _panelLayout;

    /// <summary>包含立绘、轨道与节点的舞台，配置查看时整体移到右侧。</summary>
    [SerializeField, Header("技能舞台")] private RectTransform _stage;

    /// <summary>出战槽位沿着立绘排列的透底轨道，列表页隐藏。</summary>
    [SerializeField, Header("技能轨道")] private GameObject _portraitHalo;

    /// <summary>显示槽位对应关系的配置入口，同一按钮也用于退出查看。</summary>
    [SerializeField, Header("配置按钮")] private Button _btnConfig;

    /// <summary>轮换查看一组、二组普通武功的按钮，轻功不分组。</summary>
    [SerializeField, Header("翻页按钮")] private Button _btnFlip;

    /// <summary>出战页右侧说明卡的显隐按钮。</summary>
    [SerializeField, Header("描述按钮")] private Button _btnDescription;

    /// <summary>配置入口的标签，进入槽位查看后改为返回。</summary>
    [SerializeField, Header("配置文字")] private TMP_Text _configLabel;

    /// <summary>标记当前普通武功组的文字，与舞台两侧的组号对应。</summary>
    [SerializeField, Header("翻页文字")] private TMP_Text _flipLabel;

    /// <summary>说明入口根据正文显隐显示“描述”或“显示描述”。</summary>
    [SerializeField, Header("描述工具文字")] private TMP_Text _descriptionLabel;

    /// <summary>普通列表与配置查看共用的列表标题，用于区分武功、轻功及可选条目。</summary>
    [SerializeField, Header("列表标题")] private TMP_Text _listHeader;

    /// <summary>仅在配置查看时出现的只读操作说明。</summary>
    [SerializeField, Header("配置说明")] private GameObject _configHint;

    /// <summary>配置查看右侧的出战槽标题，常态下不盖住立绘。</summary>
    [SerializeField, Header("配置槽位标题")] private GameObject _configStageTitle;

    /// <summary>两个子页签按钮：出战 / 列表。</summary>
    [SerializeField, Header("子页签按钮")] private Button[] _tabButtons;

    /// <summary>子页签文字。</summary>
    [SerializeField, Header("子页签文字")] private TMP_Text[] _tabLabels;

    /// <summary>子页签左侧的剪影图标。</summary>
    [SerializeField, Header("子页签图标")] private Image[] _tabIcons;

    /// <summary>右侧竖排导航按钮：武功、轻功、秘笈、自创、双门派、失传武学。</summary>
    [SerializeField, Header("导航按钮")] private Button[] _navButtons;

    /// <summary>竖排导航文字。</summary>
    [SerializeField, Header("导航文字")] private TMP_Text[] _navLabels;

    /// <summary>有分类图标的导航项，前三项分别显示武功、轻功和秘笈图标。</summary>
    [SerializeField, Header("导航图标")] private Image[] _navIcons;

    /// <summary>魔剑士立绘，出战和列表使用同一资源；秘笈页不显示。</summary>
    [SerializeField, Header("门派立绘")] private GameObject _portraitRoot;

    /// <summary>武功出战页的节点容器。</summary>
    [SerializeField, Header("武功节点容器")] private GameObject _battleRoot;

    /// <summary>武功出战页顶部的工具行（配置 / 翻页 / 描述）。</summary>
    [SerializeField, Header("工具行")] private GameObject _toolRoot;

    /// <summary>武功出战页的技能组编号徽章。</summary>
    [SerializeField, Header("技能组徽章")] private GameObject _badgeRoot;

    /// <summary>轻功出战页容器。</summary>
    [SerializeField, Header("轻功页容器")] private GameObject _lightRoot;

    /// <summary>列表页容器。</summary>
    [SerializeField, Header("列表页容器")] private GameObject _listRoot;

    /// <summary>列表页的内容节点，列表条目挂在这里。</summary>
    [SerializeField, Header("列表内容")] private Transform _listContent;

    /// <summary>列表条目模板，默认隐藏，运行时复制。</summary>
    [SerializeField, Header("列表条目模板")] private GameObject _listItemTemplate;

    /// <summary>秘笈空壳页。</summary>
    [SerializeField, Header("秘笈页")] private GameObject _emptyPage;
    /// <summary>包含出战、目录和培养控件的正式秘笈预制体。</summary>
    [SerializeField] private MartialManualPanel _manualPrefab;
    /// <summary>随武学窗口复用的秘笈页实例。</summary>
    private MartialManualPanel _manualPanel;
    /// <summary>当前页面产生的秘笈操作，由技能控制器发送。</summary>
    public event System.Action<ManualRequest> ManualRequested;

    /// <summary>背包培养按钮直达当前书籍，在一次页面刷新中打开秘笈列表和培养窗口。</summary>
    /// <param name="bookId">背包物品对应的书目编号。</param>
    public void OpenManualTraining(int bookId)
    {
        _category = CategorySecret;
        _page = PageList;
        _viewingSlots = false;
        RefreshTabs();
        ApplyPage();
        _manualPanel.OpenBookTraining(bookId);
    }

    /// <summary>把服务器秘笈状态交给仍可见的页面。</summary><param name="reply">当前角色的权威进度。</param>
    public void ApplyManual(ManualReply reply)
    {
        if (_manualPanel != null && _manualPanel.isActiveAndEnabled) _manualPanel.Apply(reply);
    }

    /// <summary>角色切换时丢弃现有角色的秘笈视图。</summary>
    public void ClearManual()
    {
        if (_manualPanel != null) _manualPanel.gameObject.SetActive(false);
    }


    /// <summary>右侧技能详情面板。</summary>
    [SerializeField, Header("详情面板")] private GameObject _detailPanel;

    /// <summary>与详情同位置的细框，必须和详情卡一起显隐。</summary>
    [SerializeField, Header("详情边框")] private GameObject _detailBorder;

    /// <summary>武功页九个节点的点击按钮。</summary>
    [SerializeField, Header("节点按钮")] private Button[] _nodeButtons;

    /// <summary>武功页九个节点图标。</summary>
    [SerializeField, Header("节点图标")] private Image[] _nodeIcons;

    /// <summary>武功页九个节点外圈。</summary>
    [SerializeField, Header("节点外圈")] private Image[] _nodeRings;

    /// <summary>武功页九个节点光晕。</summary>
    [SerializeField, Header("节点光晕")] private Image[] _nodeGlows;

    /// <summary>武功页九个节点下方的两字标签。</summary>
    [SerializeField, Header("节点标签")] private TMP_Text[] _nodeTags;

    /// <summary>轻功页左侧三个节点按钮。</summary>
    [SerializeField, Header("轻功节点按钮")] private Button[] _lightButtons;

    /// <summary>轻功页左侧三个节点图标。</summary>
    [SerializeField, Header("轻功节点图标")] private Image[] _lightIcons;

    /// <summary>轻功页左侧三个节点外圈。</summary>
    [SerializeField, Header("轻功节点外圈")] private Image[] _lightRings;

    /// <summary>轻功页左侧三个节点光晕。</summary>
    [SerializeField, Header("轻功节点光晕")] private Image[] _lightGlows;

    /// <summary>轻功页中央的大图标。</summary>
    [SerializeField, Header("轻功大图标")] private Image _lightBigIcon;

    /// <summary>轻功页中央大图标的外圈。</summary>
    [SerializeField, Header("轻功大图标外圈")] private Image _lightBigRing;

    /// <summary>详情面板左上角的技能图标。</summary>
    [SerializeField, Header("详情图标")] private Image _detailIcon;

    /// <summary>详情面板技能名称。</summary>
    [SerializeField, Header("详情名称")] private TMP_Text _detailName;

    /// <summary>详情面板技能等级。</summary>
    [SerializeField, Header("详情等级")] private TMP_Text _detailLevel;

    /// <summary>详情面板上的两字定位标签。</summary>
    [SerializeField, Header("详情标签")] private TMP_Text _detailTag;

    /// <summary>详情面板的技能说明正文。</summary>
    [SerializeField, Header("详情描述")] private TMP_Text _detailDesc;

    /// <summary>详情面板的使用条件。</summary>
    [SerializeField, Header("详情条件")] private TMP_Text _detailCond;

    /// <summary>控制器最近一次下发的完整技能快照。</summary>
    private readonly List<SkillViewData> _allSkills = new List<SkillViewData>();

    /// <summary>武功出战页按节点顺序排列的技能，空位为 null，末位为绝学。</summary>
    private readonly List<SkillViewData> _battleSkills = new List<SkillViewData>(BattleNodeCount);

    /// <summary>轻功出战页按节点顺序排列的技能，空位为 null。</summary>
    private readonly List<SkillViewData> _lightSkills = new List<SkillViewData>(LightNodeCount);

    /// <summary>自窗口首次打开列表起按需创建的条目缓存，页签切换时复用已创建控件。</summary>
    private readonly List<SkillListItemWidget> _listItems = new List<SkillListItemWidget>();

    /// <summary>列表页当前展示的技能，顺序与条目下标一致。</summary>
    private readonly List<SkillViewData> _listSkills = new List<SkillViewData>();

    /// <summary>列表页当前选中的条目下标。</summary>
    private int _selectedListItem = -1;

    /// <summary>出战页是否正在查看技能所属槽位，查看时不改动实际装备。</summary>
    private bool _viewingSlots;

    /// <summary>武功出战页是否显示图标下的短标签，轻功出战页则用于控制右侧详情。</summary>
    private bool _showDescription = true;

    /// <summary>每组普通武功的展示槽位数，槽位 8 是单列的绝学。</summary>
    private const int NormalSlotsPerGroup = 4;

    /// <summary>当前分类编号。</summary>
    private int _category;

    /// <summary>当前子页编号。</summary>
    private int _page;

    /// <summary>当前选中的武功节点下标。</summary>
    private int _selectedNode;

    /// <summary>当前选中的轻功节点下标。</summary>
    private int _selectedLightNode;

    /// <summary>详情面板当前展示的技能编号，用于丢弃过期的图标回调。</summary>
    private int _detailSkillId;

    /// <summary>按钮事件是否已绑定，避免窗口反复激活时重复挂监听。</summary>
    private bool _buttonsBound;

    /// <summary>升级相关节点是否已在运行时按路径查找并绑定。</summary>
    private bool _upgradeRefsBound;

    /// <summary>描述区滚动容器，切换技能时回到顶部。</summary>
    private ScrollRect _descScroll;

    /// <summary>普通技能升级区（铜币 + 升级按钮），等级门槛已移到描述滚动区。</summary>
    private GameObject _normalUpgradeRoot;

    /// <summary>绝学升级区（心得进度 + 心得丹），与普通技能区互斥显示。</summary>
    private GameObject _ultimateUpgradeRoot;

    /// <summary>描述滚动内容末行的绿色「下一级(角色N级可提升)」。</summary>
    private TMP_Text _nextLevelText;

    /// <summary>普通技能单级升级消耗的铜币数字。</summary>
    private TMP_Text _costText;

    /// <summary>普通技能单级升级按钮。</summary>
    private Button _upgradeButton;

    /// <summary>绝学「心得 X/100」文字。</summary>
    private TMP_Text _expText;

    /// <summary>绝学心得进度条填充。</summary>
    private Image _expFill;

    /// <summary>心得丹按钮上的数量文字。</summary>
    private TMP_Text _expItemText;

    /// <summary>心得丹道具按钮。</summary>
    private Button _expItemButton;

    /// <summary>点击心得丹后显示的浮层提示。</summary>
    private GameObject _expItemHint;

    /// <summary>绝学心得攒满后的升级按钮。</summary>
    private Button _ultimateButton;

    /// <summary>底部「铜币总数 + 升级全部」条。</summary>
    private GameObject _currencyBar;

    /// <summary>底部铜币总数文字。</summary>
    private TMP_Text _currencyText;

    /// <summary>升级全部按钮。</summary>
    private Button _upgradeAllButton;

    /// <summary>九个武功节点右上角的绿色可升级角标。</summary>
    private readonly TMP_Text[] _nodeFlags = new TMP_Text[BattleNodeCount];

    /// <summary>本地模拟的技能等级增量，键为技能编号；真实等级由服务端快照给出。</summary>
    private readonly Dictionary<int, int> _levelOverrides = new Dictionary<int, int>();

    /// <summary>本地模拟的铜币余额，未接货币协议。</summary>
    private int _currency = 224516;
    /// <summary>正式成长技能的货币余额，取自当前本地角色的权威同步。</summary>
    private long _growthCopper;
    /// <summary>用于显示升级可用性的当前角色等级。</summary>
    private int _growthRoleLevel;
    /// <summary>请求控制器向服务端提升一次当前学习等级。</summary>
    public event System.Action<SkillViewData> UpgradeRequested;
    /// <summary>打开面板时由控制器补入最新的权威角色成长信息。</summary>
    public event System.Action GrowthContextRequested;

    /// <summary>沿用窗口管理器的显示入口，开启时向控制器请求刷新。</summary>
    /// <param name="isShow">本次是否显示当前技能窗口。</param>
    /// <param name="obj">窗口原有数据，由基类按原逻辑处理。</param>
    public override void Show(bool isShow = true, object obj = null)
    {
        base.Show(isShow, obj);
        if (isShow) GrowthContextRequested?.Invoke();
    }

    /// <summary>接收当前角色等级与铜币，供下一次技能快照刷新展示。</summary>
    /// <param name="roleLevel">当前角色的服务端等级。</param>
    /// <param name="copper">虚拟背包中的铜币点数。</param>
    public void SetGrowthContext(int roleLevel, long copper)
    {
        _growthRoleLevel = roleLevel;
        _growthCopper = copper;
    }

    /// <summary>本地模拟的绝学心得值。</summary>
    private int _ultimateExp;

    /// <summary>本地模拟的心得丹持有数量。</summary>
    private int _expItemCount = 10;

    /// <summary>详情面板当前展示的技能，升级按钮针对它生效。</summary>
    private SkillViewData _currentSkill;

    /// <summary>窗口首次激活时绑定按钮事件。</summary>
    private void Awake()
    {
        FitLayout();
        BindButtons();
    }

    /// <summary>画布尺寸变化时重新放置内容，宽屏保留完整控件，平时不做每帧计算。</summary>
    private void OnRectTransformDimensionsChange()
    {
        FitLayout();
    }

    /// <summary>按窗口可用宽高等比缩放固定设计布局，屏幕背景不受影响。</summary>
    private void FitLayout()
    {
        if (_panelLayout == null)
        {
            return;
        }
        // 当前 Canvas 单位下窗口可用宽高，以及预制体标定的设计宽高。
        Rect frame = ((RectTransform)transform).rect;
        Rect design = _panelLayout.rect;
        // 取较小的一边缩放比，避免超宽屏下控件从上下边缘裁掉。
        float scale = Mathf.Min(frame.width / design.width, frame.height / design.height);
        _panelLayout.localScale = new Vector3(scale, scale, 1f);
    }

    /// <summary>把关闭按钮、子页签、竖排导航与节点的点击事件挂到本窗口方法上，只执行一次。</summary>
    private void BindButtons()
    {
        if (_buttonsBound)
        {
            return;
        }
        _buttonsBound = true;
        _btnConfig.onClick.AddListener(ToggleSlotView);
        _btnFlip.onClick.AddListener(FlipBattleGroup);
        _btnDescription.onClick.AddListener(ToggleDescription);
        if (_btnClose != null)
        {
            _btnClose.onClick.AddListener(CloseWindow);
        }
        // 子页签数量固定为 2，对应出战页与列表页。
        for (int index = 0; index < _tabButtons.Length; index++)
        {
            // 当前子页编号，闭包中不能直接捕获循环变量。
            int page = index;
            if (_tabButtons[index] != null)
            {
                _tabButtons[index].onClick.AddListener(delegate { SelectPage(page); });
            }
        }
        // 竖排导航前三项与分类一一对应，后三项当前没有数据。
        for (int index = 0; index < _navButtons.Length; index++)
        {
            // 当前导航项对应的分类编号。
            int category = index;
            if (_navButtons[index] == null)
            {
                continue;
            }
            if (index > CategorySecret)
            {
                _navButtons[index].interactable = false;
                continue;
            }
            _navButtons[index].onClick.AddListener(delegate { SelectCategory(category); });
        }
        for (int index = 0; index < _nodeButtons.Length; index++)
        {
            // 当前武功节点下标。
            int nodeIndex = index;
            if (_nodeButtons[index] != null)
            {
                _nodeButtons[index].onClick.AddListener(delegate { SelectBattleNode(nodeIndex); });
            }
        }
        for (int index = 0; index < _lightButtons.Length; index++)
        {
            // 当前轻功节点下标。
            int nodeIndex = index;
            if (_lightButtons[index] != null)
            {
                _lightButtons[index].onClick.AddListener(delegate { SelectLightNode(nodeIndex); });
            }
        }
        EnsureUpgradeRefs();
    }

    /// <summary>接收控制器下发的技能快照，按当前分类与子页重铺界面。</summary>
    /// <param name="obj">控制器生成的强类型技能展示列表。</param>
    public override void ReFreshUI(object obj)
    {
        // WindowBase 的 object 签名属于 UI 框架边界，此处立即转换为强类型技能列表。
        List<SkillViewData> skills = obj as List<SkillViewData>;
        if (skills == null)
        {
            return;
        }
        _allSkills.Clear();
        for (int index = 0; index < skills.Count; index++)
        {
            _allSkills.Add(skills[index]);
        }
        BindButtons();
        RefreshTabs();
        ApplyPage();
    }

    /// <summary>把职业名显示在标题栏，不修改角色技能数据。</summary>
    /// <param name="jobName">当前职业显示名。</param>
    public void SetHeader(string jobName)
    {
        if (_texJob != null)
        {
            _texJob.text = jobName;
        }
    }

    /// <summary>切换分类，并把子页复位到出战页。</summary>
    /// <param name="category">目标分类编号，0 武功、1 轻功、2 秘笈。</param>
    public void SelectCategory(int category)
    {
        if (category < CategorySkill || category > CategorySecret)
        {
            return;
        }
        _category = category;
        _page = PageBattle;
        _viewingSlots = false;
        _showDescription = true;
        RefreshTabs();
        ApplyPage();
    }

    /// <summary>切换子页（出战 / 列表）。</summary>
    /// <param name="page">目标子页编号。</param>
    public void SelectPage(int page)
    {
        if (page < PageBattle || page > PageList)
        {
            return;
        }
        _page = page;
        _viewingSlots = false;
        _showDescription = true;
        RefreshTabs();
        ApplyPage();
    }

    /// <summary>按分类设置子页签文字，并按当前子页刷新高亮。</summary>
    private void RefreshTabs()
    {
        // 子页签文字随分类变化。
        string battleText = "出战武功";
        string listText = "武功列表";
        if (_category == CategoryLight)
        {
            battleText = "出战轻功";
            listText = "轻功列表";
        }
        else if (_category == CategorySecret)
        {
            battleText = "出战秘笈";
            listText = "秘笈列表";
        }
        if (_tabLabels != null && _tabLabels.Length > 0 && _tabLabels[0] != null)
        {
            _tabLabels[0].text = battleText;
        }
        if (_tabLabels != null && _tabLabels.Length > 1 && _tabLabels[1] != null)
        {
            _tabLabels[1].text = listText;
        }
        for (int index = 0; index < _tabButtons.Length; index++)
        {
            if (_tabButtons[index] == null)
            {
                continue;
            }
            _tabButtons[index].gameObject.SetActive(true);
            // 当前子页高亮：浅底深字，其余深底浅字。
            bool selected = index == _page;
            Image tabImage = _tabButtons[index].GetComponent<Image>();
            if (tabImage != null)
            {
                if (selected)
                {
                    tabImage.color = TabOnColor;
                }
                else
                {
                    tabImage.color = TabOffColor;
                }
            }
            if (_tabLabels != null && index < _tabLabels.Length && _tabLabels[index] != null)
            {
                if (selected)
                {
                    _tabLabels[index].color = TabOnTextColor;
                }
                else
                {
                    _tabLabels[index].color = TabOffTextColor;
                }
            }
            if (_tabIcons != null && index < _tabIcons.Length && _tabIcons[index] != null)
            {
                if (selected)
                {
                    _tabIcons[index].color = TabOnTextColor;
                }
                else
                {
                    _tabIcons[index].color = TabOffTextColor;
                }
            }
        }
        // 右侧分类与横向页签统一使用蓝色选中底纹，未开放项保持低亮。
        for (int index = 0; index < _navLabels.Length; index++)
        {
            // 当前条目的文字与图标公用一种前景色。
            Color foreground = TabOffTextColor;
            // 当前条目的底色。
            Color background = new Color(0.84f, 0.88f, 0.93f, 0.94f);
            if (index == _category)
            {
                foreground = TabOnTextColor;
                background = TabOnColor;
            }
            else if (index > CategorySecret)
            {
                foreground = new Color(0.32f, 0.40f, 0.49f, 1f);
            }
            _navLabels[index].color = foreground;
            _navButtons[index].image.color = background;
            if (index < _navIcons.Length)
            {
                _navIcons[index].color = foreground;
            }
        }
    }

    /// <summary>按当前分类刷新版式，并把同一技能快照映射到出战节点或列表。</summary>
    private void ApplyPage()
    {
        RefreshPageLayout();
        if (_category == CategorySecret)
        {
            if (!isActiveAndEnabled) return;
            if (_manualPanel == null)
            {
                _manualPanel = Instantiate(_manualPrefab, _panelLayout, false);
                _manualPanel.Requested += request => ManualRequested?.Invoke(request);
            }
            _manualPanel.Open(_page == PageList, _growthRoleLevel);
            return;
        }
        ClearManual();
        if (_page == PageList)
        {
            FillList();
            return;
        }
        if (_category == CategoryLight)
        {
            FillLightNodes();
        }
        else
        {
            FillBattleNodes();
        }
        if (_viewingSlots)
        {
            FillList();
        }
    }

    /// <summary>切换预制体里已摆好的页面区块，不创建或销毁控件。</summary>
    private void RefreshPageLayout()
    {
        // 秘笈由窗口持有的独立页面展示。
        bool content = _category != CategorySecret;
        // 配置查看与出战页共用相同的槽位和角色舞台。
        bool battle = content && _page == PageBattle;
        // 只在轻功分类显示三个轻功槽。
        bool light = _category == CategoryLight;
        // 右侧详情在配置视图收起，轻功出战页还可用描述按钮收起。
        bool detail = content && !_viewingSlots;
        if (battle && light && !_showDescription)
        {
            detail = false;
        }
        SetActive(_stage.gameObject, content);
        SetActive(_emptyPage, false);
        SetActive(_portraitRoot, content);
        SetActive(_portraitHalo, battle);
        SetActive(_battleRoot, battle && !light);
        SetActive(_badgeRoot, battle && !light);
        SetActive(_lightRoot, battle && light);
        SetActive(_toolRoot, battle);
        SetActive(_listRoot, content && (!battle || _viewingSlots));
        SetActive(_detailPanel, detail);
        SetActive(_detailBorder, detail);
        SetActive(_currencyBar, battle && !_viewingSlots);
        SetActive(_configHint, _viewingSlots);
        SetActive(_configStageTitle, _viewingSlots);
        SetActive(_btnFlip.gameObject, !light);
        SetActive(_btnDescription.gameObject, !_viewingSlots);
        _configLabel.text = "配置";
        _descriptionLabel.text = "描述";
        _listHeader.text = "武功";
        if (light)
        {
            _listHeader.text = "轻功";
        }
        if (_viewingSlots)
        {
            _configLabel.text = "返回出战";
            _listHeader.text += "选择";
        }
        if (!_showDescription)
        {
            _descriptionLabel.text = "显示描述";
        }
        if (battle && !light)
        {
            // index 对应预制体绑定的武功槽位，标签包含技能类型和空槽状态。
            for (int index = 0; index < _nodeTags.Length; index++)
            {
                _nodeTags[index].enabled = _showDescription;
            }
        }
        SetPortraitPosition(battle, light);
    }

    /// <summary>打开或关闭只读的出战槽位查看，不修改快捷键或技能装备。</summary>
    private void ToggleSlotView()
    {
        _viewingSlots = !_viewingSlots;
        RefreshPageLayout();
        if (_viewingSlots)
        {
            FillList();
        }
        else if (_category == CategoryLight)
        {
            SelectLightNode(_selectedLightNode);
        }
        else
        {
            SelectBattleNode(_selectedNode);
        }
    }

    /// <summary>从已显示的另一组普通武功中选首个可用技能；绝学不参与分组。</summary>
    private void FlipBattleGroup()
    {
        // 武功索引 0–3 为一组，4–7 为二组，选中绝学时直接返回一组。
        int start = 0;
        if (_selectedNode < NormalSlotsPerGroup)
        {
            start = NormalSlotsPerGroup;
        }
        for (int index = start; index < start + NormalSlotsPerGroup && index < _battleSkills.Count; index++)
        {
            if (_battleSkills[index] != null)
            {
                SelectBattleNode(index);
                return;
            }
        }
    }

    /// <summary>切换武功图标下方的短标签，轻功出战页沿用详情展开操作。</summary>
    private void ToggleDescription()
    {
        _showDescription = !_showDescription;
        RefreshPageLayout();
        if (_category == CategoryLight && _showDescription)
        {
            ResetScroll(_descScroll);
        }
    }

    /// <summary>按开关统一处理空引用，避免调用点重复判空。</summary>
    /// <param name="target">目标节点，可为空。</param>
    /// <param name="active">是否启用。</param>
    private static void SetActive(GameObject target, bool active)
    {
        if (target == null)
        {
            return;
        }
        target.SetActive(active);
    }

    /// <summary>出战时立绘居于节点之间，列表居中，配置查看时整组移动到右侧。</summary>
    /// <param name="battleLayout">出战或配置查看为 true；技能列表为 false。</param>
    /// <param name="light">在轻功页为 true，立绘右移为中央大图标让位。</param>
    private void SetPortraitPosition(bool battleLayout, bool light)
    {
        // 配置页整体平移舞台和工具行，留出左栏标题；普通页沿用预制体的居中原点。
        Vector2 stageOffset = Vector2.zero;
        if (_viewingSlots)
        {
            stageOffset = new Vector2(670f, 0f);
        }
        _stage.anchoredPosition = stageOffset;
        ((RectTransform)_toolRoot.transform).anchoredPosition = stageOffset;
        // 立绘 RectTransform 的宽高和位置均为布局节点的设计单位。
        RectTransform portrait = (RectTransform)_portraitRoot.transform;
        if (!battleLayout)
        {
            portrait.sizeDelta = new Vector2(364f, 536f);
            portrait.anchoredPosition = new Vector2(-237f, -67f);
            return;
        }
        portrait.sizeDelta = new Vector2(435f, 616f);
        portrait.anchoredPosition = new Vector2(-458f, -67f);
        if (light)
        {
            portrait.anchoredPosition = new Vector2(-270f, -67f);
        }
    }

    /// <summary>把武功分类的技能铺到 8 个普通节点与 1 个绝学大节点上。</summary>
    private void FillBattleNodes()
    {
        _battleSkills.Clear();
        for (int index = 0; index < BattleNodeCount; index++)
        {
            _battleSkills.Add(null);
        }
        // 普通节点已填充的数量，绝学单独放到最后一个大节点。
        int normalCount = 0;
        // 扫描到的绝学技能，只保留第一条。
        SkillViewData ultimate = null;
        for (int index = 0; index < _allSkills.Count; index++)
        {
            // 本次判断的技能展示数据。
            SkillViewData skill = _allSkills[index];
            if (skill == null || IsLightSkill(skill))
            {
                continue;
            }
            // 普通攻击不是技能，不占技能节点。
            if (skill.SkillType == BasicAttackType)
            {
                continue;
            }
            if (skill.Tag == UltimateTag)
            {
                if (ultimate == null)
                {
                    ultimate = skill;
                }
                continue;
            }
            if (normalCount >= BattleNodeCount - 1)
            {
                continue;
            }
            _battleSkills[normalCount] = skill;
            normalCount++;
        }
        _battleSkills[BattleNodeCount - 1] = ultimate;
        _selectedNode = 0;
        ApplyBattleNodeVisuals();
        SelectBattleNode(0);
    }

    /// <summary>把轻功分类（含滑行）的技能铺到左侧 3 个节点，并同步中央大图标。</summary>
    private void FillLightNodes()
    {
        _lightSkills.Clear();
        for (int index = 0; index < LightNodeCount; index++)
        {
            _lightSkills.Add(null);
        }
        // 已填充的轻功节点数量。
        int count = 0;
        for (int index = 0; index < _allSkills.Count; index++)
        {
            // 本次判断的技能展示数据。
            SkillViewData skill = _allSkills[index];
            if (skill == null || !IsLightSkill(skill))
            {
                continue;
            }
            if (count >= LightNodeCount)
            {
                continue;
            }
            _lightSkills[count] = skill;
            count++;
        }
        _selectedLightNode = 0;
        ApplyLightNodeVisuals();
        SelectLightNode(0);
    }

    /// <summary>刷新武功页九个节点的图标、外圈与两字标签。</summary>
    private void ApplyBattleNodeVisuals()
    {
        for (int index = 0; index < BattleNodeCount; index++)
        {
            // 当前槽位的技能，null 表示该位置没有技能。
            SkillViewData skill = _battleSkills[index];
            // 绝学槽保持独有的外圈配色。
            bool isUltimateSlot = index == BattleNodeCount - 1;
            UpdateNodeVisual(_nodeButtons, _nodeIcons, _nodeRings, _nodeGlows, _nodeTags,
                index, skill, isUltimateSlot, false);
            if (skill != null)
            {
                LoadNodeIcon(_nodeIcons, index, skill);
            }
        }
    }

    /// <summary>刷新轻功页三个节点的图标与外圈。</summary>
    private void ApplyLightNodeVisuals()
    {
        for (int index = 0; index < LightNodeCount; index++)
        {
            // 当前槽位的技能，null 表示该位置没有技能。
            SkillViewData skill = _lightSkills[index];
            UpdateNodeVisual(_lightButtons, _lightIcons, _lightRings, _lightGlows, null,
                index, skill, false, false);
            if (skill != null)
            {
                LoadNodeIcon(_lightIcons, index, skill);
            }
        }
    }

    /// <summary>把一条技能数据写进指定节点，空技能时显示未习得状态。</summary>
    /// <param name="buttons">该组节点的按钮数组。</param>
    /// <param name="icons">该组节点的图标数组。</param>
    /// <param name="rings">该组节点的外圈数组。</param>
    /// <param name="glows">该组节点的光晕数组。</param>
    /// <param name="tags">该组节点的标签数组，轻功页没有标签时传 null。</param>
    /// <param name="index">节点下标。</param>
    /// <param name="skill">该节点对应技能，可为空。</param>
    /// <param name="isUltimateSlot">该节点是否为绝学大节点。</param>
    /// <param name="selected">该节点当前是否被选中。</param>
    private void UpdateNodeVisual(Button[] buttons, Image[] icons, Image[] rings, Image[] glows,
        TMP_Text[] tags, int index, SkillViewData skill, bool isUltimateSlot, bool selected)
    {
        if (buttons != null && index < buttons.Length && buttons[index] != null)
        {
            buttons[index].interactable = skill != null;
        }
        if (icons != null && index < icons.Length && icons[index] != null)
        {
            icons[index].enabled = skill != null && icons[index].sprite != null;
        }
        if (rings != null && index < rings.Length && rings[index] != null)
        {
            if (selected)
            {
                rings[index].color = NodeRingSelected;
            }
            else if (skill == null)
            {
                rings[index].color = NodeRingEmpty;
            }
            else if (isUltimateSlot)
            {
                rings[index].color = NodeRingUltimate;
            }
            else
            {
                rings[index].color = NodeRingNormal;
            }
        }
        if (glows != null && index < glows.Length && glows[index] != null)
        {
            glows[index].enabled = selected;
        }
        if (tags != null && index < tags.Length && tags[index] != null)
        {
            if (skill == null)
            {
                tags[index].text = EmptyNodeTag;
            }
            else
            {
                tags[index].text = skill.Tag;
            }
            if (isUltimateSlot)
            {
                tags[index].color = new Color(1f, 0.86f, 0.55f, 1f);
            }
            else
            {
                tags[index].color = new Color(0.92f, 0.96f, 1f, 1f);
            }
        }
    }

    /// <summary>异步加载节点图标，回调前确认槽位仍存在。</summary>
    /// <param name="icons">节点图标数组。</param>
    /// <param name="index">节点下标。</param>
    /// <param name="skill">该节点对应技能。</param>
    private void LoadNodeIcon(Image[] icons, int index, SkillViewData skill)
    {
        if (icons == null || index >= icons.Length || icons[index] == null)
        {
            return;
        }
        if (string.IsNullOrEmpty(skill.IconPath))
        {
            return;
        }
        // 图标回来之前先隐藏，避免 Image 在没有 sprite 时画出一个白色方块。
        if (icons[index].sprite == null)
        {
            icons[index].enabled = false;
        }
        ResourceMgr.Instance.LoadSpriteAsync(skill.IconPath, sprite =>
        {
            if (icons[index] == null || sprite == null)
            {
                return;
            }
            icons[index].sprite = sprite;
            icons[index].enabled = true;
        });
    }

    /// <summary>选中武功页的指定节点，右侧详情随之切换。</summary>
    /// <param name="nodeIndex">节点下标，超出范围时忽略。</param>
    public void SelectBattleNode(int nodeIndex)
    {
        if (nodeIndex < 0 || nodeIndex >= _battleSkills.Count)
        {
            return;
        }
        _selectedNode = nodeIndex;
        for (int index = 0; index < BattleNodeCount; index++)
        {
            // 该节点对应的技能，用于保持绝学金圈。
            SkillViewData skill = _battleSkills[index];
            bool isUltimateSlot = index == BattleNodeCount - 1;
            UpdateNodeVisual(_nodeButtons, _nodeIcons, _nodeRings, _nodeGlows, _nodeTags,
                index, skill, isUltimateSlot, index == nodeIndex);
        }
        // 当前组的可见名称，与左右组号码保持一致。
        _flipLabel.text = "翻页 · 1";
        if (nodeIndex >= NormalSlotsPerGroup && nodeIndex < BattleNodeCount - 1)
        {
            _flipLabel.text = "翻页 · 2";
        }
        // 选中的技能实例，列表与槽位都沿用控制器快照中的同一引用。
        SkillViewData selected = _battleSkills[nodeIndex];
        if (_viewingSlots)
        {
            SelectListRow(_listSkills.IndexOf(selected));
            return;
        }
        ShowDetail(selected);
    }

    /// <summary>选中轻功页的指定节点，并同步中央大图标。</summary>
    /// <param name="nodeIndex">节点下标，超出范围时忽略。</param>
    public void SelectLightNode(int nodeIndex)
    {
        if (nodeIndex < 0 || nodeIndex >= _lightSkills.Count)
        {
            return;
        }
        _selectedLightNode = nodeIndex;
        for (int index = 0; index < LightNodeCount; index++)
        {
            // 该节点对应的技能。
            SkillViewData skill = _lightSkills[index];
            UpdateNodeVisual(_lightButtons, _lightIcons, _lightRings, _lightGlows, null,
                index, skill, false, index == nodeIndex);
        }
        // 当前选中的轻功技能，用于中央大图标与右侧详情。
        SkillViewData selected = _lightSkills[nodeIndex];
        if (_viewingSlots)
        {
            SelectListRow(_listSkills.IndexOf(selected));
        }
        else
        {
            ShowDetail(selected);
        }
        if (_lightBigIcon == null)
        {
            return;
        }
        _lightBigIcon.enabled = selected != null;
        if (selected == null || string.IsNullOrEmpty(selected.IconPath))
        {
            return;
        }
        // 大图标同样先隐藏，等图标加载完再显示。
        if (_lightBigIcon.sprite == null)
        {
            _lightBigIcon.enabled = false;
        }
        ResourceMgr.Instance.LoadSpriteAsync(selected.IconPath, sprite =>
        {
            if (_lightBigIcon != null && sprite != null)
            {
                _lightBigIcon.sprite = sprite;
                _lightBigIcon.enabled = true;
            }
        });
    }

    /// <summary>按分类整理武功或轻功，复用已有条目，只有条目数量增长时创建控件。</summary>
    private void FillList()
    {
        _listSkills.Clear();
        // 当前列表只收与分类一致的技能，普攻不占武学条目。
        bool light = _category == CategoryLight;
        for (int index = 0; index < _allSkills.Count; index++)
        {
            // 控制器下发的原始技能展示数据。
            SkillViewData skill = _allSkills[index];
            if (skill == null || skill.SkillType == BasicAttackType)
            {
                continue;
            }
            if (IsLightSkill(skill) == light)
            {
                _listSkills.Add(skill);
            }
        }
        for (int index = 0; index < _listSkills.Count; index++)
        {
            if (index >= _listItems.Count)
            {
                // 新控件常驻在池中的下标，监听器只在创建时绑定一次。
                int rowIndex = index;
                // 只克隆预制体里绑定好的行；不在脚本拼图标和文字。
                GameObject item = Instantiate(_listItemTemplate, _listContent);
                item.GetComponent<Button>().onClick.AddListener(delegate { SelectListItem(rowIndex); });
                _listItems.Add(item.GetComponent<SkillListItemWidget>());
            }
            // 复用同一视图，离开列表页面时随父节点一起隐藏。
            SkillListItemWidget row = _listItems[index];
            // 本行展示的技能快照。
            SkillViewData skill = _listSkills[index];
            row.SetData(skill, GetSkillLevel(skill));
            row.SetSelected(false);
            row.gameObject.SetActive(true);
        }
        for (int index = _listSkills.Count; index < _listItems.Count; index++)
        {
            _listItems[index].gameObject.SetActive(false);
        }
        _selectedListItem = -1;
        // 列表面板上的 ScrollRect 控件，开启后从顶部开始浏览。
        ScrollRect scroll = _listContent.GetComponentInParent<ScrollRect>();
        ResetScroll(scroll);
        if (_viewingSlots)
        {
            // 配置查看默认高亮当前出战槽位，不改变玩家上一次在节点里的选择。
            SkillViewData selected = null;
            if (light)
            {
                if (_selectedLightNode < _lightSkills.Count)
                {
                    selected = _lightSkills[_selectedLightNode];
                }
            }
            else if (_selectedNode < _battleSkills.Count)
            {
                selected = _battleSkills[_selectedNode];
            }
            SelectListRow(_listSkills.IndexOf(selected));
        }
        else if (_listSkills.Count > 0)
        {
            SelectListItem(0);
        }
        else
        {
            ShowEmptyDetail();
        }
    }

    /// <summary>选中列表条目，普通列表切换详情，配置查看定位对应的实际槽位。</summary>
    /// <param name="index">可见列表中的条目下标。</param>
    public void SelectListItem(int index)
    {
        if (index < 0 || index >= _listSkills.Count)
        {
            return;
        }
        SelectListRow(index);
        // 列表和槽位共享同一份技能快照，不新建出战配置。
        SkillViewData skill = _listSkills[index];
        if (!_viewingSlots)
        {
            ShowDetail(skill);
            return;
        }
        if (_category == CategoryLight)
        {
            SelectLightNode(_lightSkills.IndexOf(skill));
        }
        else
        {
            SelectBattleNode(_battleSkills.IndexOf(skill));
        }
    }

    /// <summary>只重绘旧行和新行的高亮；配置下一旦选中未上场技能，槽位不变。</summary>
    /// <param name="index">当前行下标，-1 表示未选中可见条目。</param>
    private void SelectListRow(int index)
    {
        if (_selectedListItem >= 0)
        {
            _listItems[_selectedListItem].SetSelected(false);
        }
        _selectedListItem = index;
        if (_selectedListItem >= 0)
        {
            _listItems[_selectedListItem].SetSelected(true);
        }
    }

    /// <summary>只重算当前滚动内容，然后回到顶部，不请求重建整座界面。</summary>
    /// <param name="scroll">描述或列表负责裁剪和输入的滚动控件，未绑定则跳过。</param>
    private static void ResetScroll(ScrollRect scroll)
    {
        if (scroll == null || !scroll.isActiveAndEnabled)
        {
            return;
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
        scroll.StopMovement();
        scroll.verticalNormalizedPosition = 1f;
    }

    /// <summary>判断技能是否属于轻功分类，滑行与轻功都算。</summary>
    /// <param name="skill">待判断的技能展示数据。</param>
    /// <returns>技能表 Type 为 3（滑行）或 4（轻功）时返回 true。</returns>
    private static bool IsLightSkill(SkillViewData skill)
    {
        return skill.SkillType == 3 || skill.SkillType == 4;
    }

    /// <summary>把技能数据写到右侧详情面板；空数据时展示未习得提示。</summary>
    /// <param name="skill">选中的技能展示数据，可为空。</param>
    private void ShowDetail(SkillViewData skill)
    {
        if (skill == null)
        {
            _detailSkillId = 0;
            ShowEmptyDetail();
            return;
        }
        _detailSkillId = skill.SkillId;
        _currentSkill = skill;
        if (_detailName != null)
        {
            _detailName.text = skill.Name;
        }
        if (_detailLevel != null)
        {
            if (skill.Level > 0)
            {
                _detailLevel.text = "(" + GetSkillLevel(skill) + "级)";
                _detailLevel.color = UpgradeGreen;
            }
            else
            {
                _detailLevel.text = "未习得";
                _detailLevel.color = UpgradeGray;
            }
        }
        if (_detailTag != null)
        {
            _detailTag.text = skill.Tag;
        }
        if (_detailDesc != null)
        {
            _detailDesc.text = BuildRichDescription(skill.Description);
        }
        if (_detailCond != null)
        {
            // 效果范围为米；配置为 0 的技能属于自身范围，不显示具体距离。
            string rangeText = "效果范围：自身范围";
            if (skill.Range > 0)
            {
                rangeText = "效果范围：" + skill.Range + " 米";
            }
            // 真气技能显示独立消耗项，读同一等级行取得的实际消耗方式。
            string costText = "消耗内力：" + skill.MpCost;
            if (skill.HasGrowthConfig) costText = "消耗魔力：" + skill.MpCost;
            if (skill.TrueQiCost < 0) costText = "消耗真气：全部剩余真气";
            if (skill.TrueQiCost > 0) costText = "消耗真气：" + skill.TrueQiCost;
            _detailCond.text = "调息时间：" + skill.Cooldown.ToString("0.###") + " 秒\n" +
                rangeText + "\n" + costText;
        }
        RefreshUpgradeBlock(skill);
        if (_page == PageList && _selectedListItem >= 0)
        {
            _listItems[_selectedListItem].SetLevel(GetSkillLevel(skill), skill.Tag);
        }
        if (_detailIcon == null)
        {
            return;
        }
        _detailIcon.enabled = true;
        if (string.IsNullOrEmpty(skill.IconPath))
        {
            return;
        }
        // 详情图标同样先隐藏，避免加载期间出现白块。
        if (_detailIcon.sprite == null)
        {
            _detailIcon.enabled = false;
        }
        // 本次请求对应的技能编号，回调时与当前详情比对。
        int skillId = skill.SkillId;
        ResourceMgr.Instance.LoadSpriteAsync(skill.IconPath, sprite =>
        {
            if (_detailIcon == null || sprite == null)
            {
                return;
            }
            if (_detailSkillId == skillId)
            {
                _detailIcon.sprite = sprite;
                _detailIcon.enabled = true;
            }
        });
    }

    /// <summary>没有可展示技能时清空详情，避免残留上一条技能内容。</summary>
    private void ShowEmptyDetail()
    {
        _currentSkill = null;
        RefreshUpgradeBlock(null);
        if (_detailName != null)
        {
            _detailName.text = EmptyNodeTag;
        }
        if (_detailLevel != null)
        {
            _detailLevel.text = string.Empty;
        }
        if (_detailTag != null)
        {
            _detailTag.text = string.Empty;
        }
        if (_detailDesc != null)
        {
            _detailDesc.text = "该槽位尚未习得技能。";
        }
        if (_detailCond != null)
        {
            _detailCond.text = string.Empty;
        }
        if (_detailIcon != null)
        {
            _detailIcon.enabled = false;
        }
        if (_lightBigIcon != null)
        {
            _lightBigIcon.enabled = false;
        }
    }

    #region 技能升级与绝学心得

    /// <summary>按路径把升级区控件找出来并挂事件；资源未构建时直接返回，下次调用会重试。</summary>
    private void EnsureUpgradeRefs()
    {
        if (_upgradeRefsBound)
        {
            return;
        }
        // 详情卡与货币条都在等比布局内部。
        Transform detail = _detailPanel.transform;
        if (detail == null)
        {
            return;
        }
        Transform normal = detail.Find("NormalUpgradeRoot");
        Transform ultimate = detail.Find("UltimateUpgradeRoot");
        // 出战页底部货币条。
        Transform currency = _panelLayout.Find("CurrencyBar");
        // 三块容器必须同时存在，避免构建中途把监听器挂重。
        if (normal == null || ultimate == null || currency == null)
        {
            return;
        }
        // 仅说明正文滚动，升级区作为外层常驻操作。
        _descScroll = detail.Find("DescScroll").GetComponent<ScrollRect>();
        _normalUpgradeRoot = normal.gameObject;
        _ultimateUpgradeRoot = ultimate.gameObject;
        _currencyBar = currency.gameObject;
        // 等级门槛已收进描述滚动内容，按完整路径从详情面板查找。
        _nextLevelText = ChildText(detail, "DescScroll/Viewport/Content/Txt_NextLevel");
        _costText = ChildText(normal, "Txt_Cost");
        _upgradeButton = ChildButton(normal, "Btn_Upgrade");
        _expText = ChildText(ultimate, "Txt_Exp");
        _expFill = ChildImage(ultimate, "ExpBarBg/ExpBarFill");
        _expItemText = ChildText(ultimate, "Btn_ExpItem/Txt_ExpItem");
        _expItemButton = ChildButton(ultimate, "Btn_ExpItem");
        _expItemHint = ChildGo(ultimate, "ExpItemHint");
        _ultimateButton = ChildButton(ultimate, "Btn_UltimateUpgrade");
        _currencyText = ChildText(currency, "Txt_Currency");
        _upgradeAllButton = ChildButton(currency, "Btn_UpgradeAll");
        if (_upgradeButton != null)
        {
            _upgradeButton.onClick.AddListener(OnUpgradeClicked);
        }
        if (_expItemButton != null)
        {
            _expItemButton.onClick.AddListener(OnExpItemClicked);
        }
        if (_ultimateButton != null)
        {
            _ultimateButton.onClick.AddListener(OnUltimateUpgradeClicked);
        }
        if (_upgradeAllButton != null)
        {
            _upgradeAllButton.onClick.AddListener(OnUpgradeAllClicked);
        }
        // 九个武功槽在舞台内部，使用已绑定的容器。
        Transform nodeRoot = _battleRoot.transform;
        if (nodeRoot != null)
        {
            for (int index = 0; index < BattleNodeCount; index++)
            {
                _nodeFlags[index] = ChildText(nodeRoot, "Node_" + index + "/UpgradeFlag");
            }
        }
        SetActive(_expItemHint, false);
        _upgradeRefsBound = true;
    }

    /// <summary>按当前技能切换「铜币升级」或「绝学心得」两套互斥 UI，并刷新全部数字。</summary>
    /// <param name="skill">详情当前技能，为空时隐藏两套升级区。</param>
    private void RefreshUpgradeBlock(SkillViewData skill)
    {
        EnsureUpgradeRefs();
        if (skill == null)
        {
            SetActive(_normalUpgradeRoot, false);
            SetActive(_ultimateUpgradeRoot, false);
            SetNextLevelActive(false);
            return;
        }
        // 有权威成长配置的武功统一显示真实铜币费用，绝学标签仍用于武功分类。
        bool ultimate = skill.Tag == UltimateTag && !skill.HasGrowthConfig;
        // 正式成长技能没有下一行时只显示满级说明。
        bool hasUpgrade = !skill.HasGrowthConfig || (skill.Level > 0 && skill.NextRoleLevel > 0);
        SetActive(_normalUpgradeRoot, !ultimate && hasUpgrade);
        SetActive(_ultimateUpgradeRoot, ultimate);
        // 等级门槛在滚动内容里，不随升级区隐藏，按技能类型单独显隐。
        // 描述正文若已带「下一级」行，就不再追加第二条，避免滚动区出现两行一样的提示。
        bool descHasNextLevel = !string.IsNullOrEmpty(skill.Description)
            && skill.Description.Contains("下一级");
        SetNextLevelActive(!ultimate && !descHasNextLevel && !skill.HasGrowthConfig);
        if (_currencyText != null)
        {
            if (skill.HasGrowthConfig) _currencyText.text = _growthCopper.ToString();
            else _currencyText.text = _currency.ToString();
        }
        if (ultimate)
        {
            RefreshUltimateBlock();
        }
        else
        {
            RefreshNormalBlock(skill);
        }
        RefreshNodeFlags();
        ResetScroll(_descScroll);
    }

    /// <summary>控制描述滚动内容里「下一级」行的显隐，绝学心得页不显示这一行。</summary>
    /// <param name="visible">是否显示等级门槛行。</param>
    private void SetNextLevelActive(bool visible)
    {
        if (_nextLevelText == null)
        {
            return;
        }
        SetActive(_nextLevelText.gameObject, visible);
    }

    /// <summary>普通技能：显示角色等级门槛、单级铜币消耗与余额是否足够。</summary>
    private void RefreshNormalBlock(SkillViewData skill)
    {
        if (skill.HasGrowthConfig)
        {
            if (_costText != null) _costText.text = skill.UpgradeCost.ToString();
            if (_upgradeButton != null) _upgradeButton.interactable = CanUpgradeGrowth(skill);
            return;
        }
        // 参考实机为「2级 → 角色3级可提升」；本地升级后门槛同步递增，不显示过期等级。
        int levelGap = GetSkillLevel(skill) - skill.Level;
        int requiredRoleLevel = skill.NextRoleLevel > 0
            ? skill.NextRoleLevel + levelGap
            : GetSkillLevel(skill) + 1;
        int cost = GetUpgradeCost(skill);
        if (_nextLevelText != null)
        {
            _nextLevelText.text = "下一级(角色" + requiredRoleLevel + "级可提升)";
            _nextLevelText.color = UpgradeGreen;
        }
        if (_costText != null)
        {
            _costText.text = cost.ToString();
        }
        if (_upgradeButton != null)
        {
            _upgradeButton.interactable = _currency >= cost;
        }
    }

    /// <summary>用已显示的下一等级门槛和当前余额决定正式升级按钮的可用性。</summary>
    /// <param name="skill">已配置逐级数值的已学技能。</param>
    /// <returns>存在下一行且满足展示条件时为真，最终结果由服务器确认。</returns>
    private bool CanUpgradeGrowth(SkillViewData skill)
    {
        // 角色和铜币条件只用于引导点击，权威处理会重新读同一等级行。
        bool hasNextLevel = skill.Level > 0 && skill.NextRoleLevel > 0;
        return hasNextLevel && _growthRoleLevel >= skill.NextRoleLevel && _growthCopper >= skill.UpgradeCost;
    }

    /// <summary>绝学：显示心得进度、道具余量与「攒满才可升级」状态。</summary>
    private void RefreshUltimateBlock()
    {
        if (_expText != null)
        {
            _expText.text = "绝学升级：心得 " + _ultimateExp + "/" + UltimateExpMax;
        }
        if (_expFill != null)
        {
            // 无 sprite 的 Image 走 Filled 时部分版本会忽略 fillAmount，改用锚点宽度控制进度。
            _expFill.type = Image.Type.Simple;
            RectTransform fillRect = _expFill.rectTransform;
            fillRect.anchorMax = new Vector2((float)_ultimateExp / UltimateExpMax, 1f);
        }
        if (_expItemText != null)
        {
            _expItemText.text = "心得丹 ×" + _expItemCount;
        }
        if (_expItemButton != null)
        {
            _expItemButton.interactable = _expItemCount > 0 && _ultimateExp < UltimateExpMax;
        }
        if (_ultimateButton != null)
        {
            _ultimateButton.interactable = _ultimateExp >= UltimateExpMax;
        }
    }

    /// <summary>刷新九个武功节点右上角的绿色可升级角标。</summary>
    private void RefreshNodeFlags()
    {
        for (int index = 0; index < _nodeFlags.Length; index++)
        {
            if (_nodeFlags[index] == null)
            {
                continue;
            }
            // 该节点对应技能，列表不足时视为空槽。
            SkillViewData skill = index < _battleSkills.Count ? _battleSkills[index] : null;
            bool canUpgrade = false;
            if (skill != null && skill.Level > 0)
            {
                if (skill.HasGrowthConfig)
                {
                    canUpgrade = CanUpgradeGrowth(skill);
                }
                else if (skill.Tag == UltimateTag)
                {
                    canUpgrade = _ultimateExp >= UltimateExpMax;
                }
                else
                {
                    canUpgrade = _currency >= GetUpgradeCost(skill);
                }
            }
            _nodeFlags[index].gameObject.SetActive(canUpgrade);
        }
    }

    /// <summary>点击右下角升级按钮，正式成长武功交由服务端扣币确认。</summary>
    private void OnUpgradeClicked()
    {
        SkillViewData skill = _currentSkill;
        if (skill == null)
        {
            return;
        }
        if (skill.HasGrowthConfig)
        {
            if (CanUpgradeGrowth(skill)) UpgradeRequested?.Invoke(skill);
            return;
        }
        if (skill.Tag == UltimateTag) return;
        int cost = GetUpgradeCost(skill);
        if (_currency < cost)
        {
            return;
        }
        _currency -= cost;
        AddSkillLevel(skill.SkillId, 1);
        ShowDetail(skill);
    }

    /// <summary>点击「升级全部」：按铜币余额依次把所有可升级的普通技能各升一级，绝学不参与。</summary>
    private void OnUpgradeAllClicked()
    {
        for (int index = 0; index < _allSkills.Count; index++)
        {
            SkillViewData skill = _allSkills[index];
            if (skill == null || skill.Level <= 0)
            {
                continue;
            }
            // 普攻不算技能，绝学走心得制，都不吃铜币。
            if (skill.SkillType == BasicAttackType || skill.Tag == UltimateTag)
            {
                continue;
            }
            if (skill.HasGrowthConfig)
            {
                if (CanUpgradeGrowth(skill)) UpgradeRequested?.Invoke(skill);
                continue;
            }
            int cost = GetUpgradeCost(skill);
            if (_currency < cost)
            {
                continue;
            }
            _currency -= cost;
            AddSkillLevel(skill.SkillId, 1);
        }
        if (_currentSkill != null)
        {
            ShowDetail(_currentSkill);
        }
    }

    /// <summary>点击心得丹：消耗一颗道具增加心得，并弹出使用说明浮层。</summary>
    private void OnExpItemClicked()
    {
        if (_expItemCount <= 0 || _ultimateExp >= UltimateExpMax)
        {
            return;
        }
        _expItemCount--;
        _ultimateExp = Mathf.Min(UltimateExpMax, _ultimateExp + ExpItemGain);
        SetActive(_expItemHint, true);
        CancelInvoke("HideExpItemHint");
        Invoke("HideExpItemHint", 4f);
        if (_currentSkill != null)
        {
            ShowDetail(_currentSkill);
        }
    }

    /// <summary>心得攒满后点击绝学升级按钮：清空心得并把绝学提升一级。</summary>
    private void OnUltimateUpgradeClicked()
    {
        if (_ultimateExp < UltimateExpMax || _currentSkill == null)
        {
            return;
        }
        _ultimateExp = 0;
        AddSkillLevel(_currentSkill.SkillId, 1);
        ShowDetail(_currentSkill);
    }

    /// <summary>自动收起心得丹浮层。</summary>
    private void HideExpItemHint()
    {
        SetActive(_expItemHint, false);
    }

    /// <summary>取技能当前等级，叠加本地模拟的升级增量。</summary>
    private int GetSkillLevel(SkillViewData skill)
    {
        if (skill.HasGrowthConfig) return skill.Level;
        int extra;
        if (_levelOverrides.TryGetValue(skill.SkillId, out extra))
        {
            return skill.Level + extra;
        }
        return skill.Level;
    }

    /// <summary>取单级升级消耗，配置缺失时回退到默认铜币价格。</summary>
    private static int GetUpgradeCost(SkillViewData skill)
    {
        return skill.UpgradeCost > 0 ? skill.UpgradeCost : DefaultUpgradeCost;
    }

    /// <summary>记录一次本地模拟升级，不改动服务端下发的只读快照。</summary>
    private void AddSkillLevel(int skillId, int delta)
    {
        int current;
        _levelOverrides.TryGetValue(skillId, out current);
        _levelOverrides[skillId] = current + delta;
    }

    /// <summary>把纯文本描述转成 TMP 富文本：数值橙、词条蓝、下一级绿，均按深蓝底调过明度。</summary>
    private static string BuildRichDescription(string raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return string.Empty;
        }
        string text = Regex.Replace(raw, @"(\d+\.?\d*%|\+\d+|\d+点|\d+秒|\d+名)",
            "<color=#FFB067>$1</color>");
        text = Regex.Replace(text, @"(?m)^([\u4e00-\u9fa5]{2,4})[：:]",
            "<color=#8CC6F2>$1：</color>");
        text = Regex.Replace(text, @"(?m)^(下一级.*)$", "<color=#6BE08A>$1</color>");
        return text;
    }

    /// <summary>按相对路径取子物体。</summary>
    private static GameObject ChildGo(Transform root, string path)
    {
        Transform found = root.Find(path);
        return found != null ? found.gameObject : null;
    }

    /// <summary>按相对路径取 TMP 文本。</summary>
    private static TMP_Text ChildText(Transform root, string path)
    {
        Transform found = root.Find(path);
        return found != null ? found.GetComponent<TMP_Text>() : null;
    }

    /// <summary>按相对路径取 Image。</summary>
    private static Image ChildImage(Transform root, string path)
    {
        Transform found = root.Find(path);
        return found != null ? found.GetComponent<Image>() : null;
    }

    /// <summary>按相对路径取 Button。</summary>
    private static Button ChildButton(Transform root, string path)
    {
        Transform found = root.Find(path);
        return found != null ? found.GetComponent<Button>() : null;
    }

    #endregion
}
