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

    /// <summary>秘笈分类编号，当前只有空壳页。</summary>
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

    /// <summary>技能升级文案的绿色，与参考实机一致。</summary>
    private static readonly Color UpgradeGreen = new Color(0.42f, 0.88f, 0.55f, 1f);

    /// <summary>技能升级文案未达成时的灰色。</summary>
    private static readonly Color UpgradeGray = new Color(0.5f, 0.56f, 0.62f, 1f);

    /// <summary>节点外圈常态颜色。</summary>
    private static readonly Color NodeRingNormal = new Color(0.42f, 0.63f, 0.92f, 1f);

    /// <summary>绝学大节点的金色外圈。</summary>
    private static readonly Color NodeRingUltimate = new Color(0.98f, 0.78f, 0.42f, 1f);

    /// <summary>空槽位的暗色外圈。</summary>
    private static readonly Color NodeRingEmpty = new Color(0.24f, 0.31f, 0.42f, 1f);

    /// <summary>当前选中节点的亮色外圈。</summary>
    private static readonly Color NodeRingSelected = new Color(0.85f, 0.96f, 1f, 1f);

    /// <summary>子页签选中时的底板颜色，深蓝底配金色文字，避免大面积纯白抢视线。</summary>
    private static readonly Color TabOnColor = new Color(0.17f, 0.32f, 0.54f, 1f);

    /// <summary>子页签未选中时的底板颜色，与选中态保持同一组块面、只降明度。</summary>
    private static readonly Color TabOffColor = new Color(0.13f, 0.19f, 0.29f, 0.95f);

    /// <summary>子页签选中时的文字与图标颜色，暖金白。</summary>
    private static readonly Color TabOnTextColor = new Color(1f, 0.92f, 0.74f, 1f);

    /// <summary>子页签未选中时的文字与图标颜色，灰蓝。</summary>
    private static readonly Color TabOffTextColor = new Color(0.58f, 0.66f, 0.78f, 1f);

    /// <summary>当前职业名称文本。</summary>
    [SerializeField, Header("职业")] private TMP_Text _texJob;

    /// <summary>标题栏关闭按钮。</summary>
    [SerializeField, Header("关闭按钮")] private Button _btnClose;

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

    /// <summary>门派立绘节点，秘笈页与列表页隐藏。</summary>
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

    /// <summary>右侧技能详情面板。</summary>
    [SerializeField, Header("详情面板")] private GameObject _detailPanel;

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

    /// <summary>列表页已创建的条目，刷新时整批销毁。</summary>
    private readonly List<GameObject> _listItems = new List<GameObject>();

    /// <summary>列表页当前展示的技能，顺序与条目下标一致。</summary>
    private readonly List<SkillViewData> _listSkills = new List<SkillViewData>();

    /// <summary>列表页当前选中的条目下标。</summary>
    private int _selectedListItem;

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

    /// <summary>普通技能升级区（下一级提示 + 铜币 + 升级按钮）。</summary>
    private GameObject _normalUpgradeRoot;

    /// <summary>绝学升级区（心得进度 + 心得丹），与普通技能区互斥显示。</summary>
    private GameObject _ultimateUpgradeRoot;

    /// <summary>普通技能「下一级(角色N级可提升)」。</summary>
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

    /// <summary>本地模拟的绝学心得值。</summary>
    private int _ultimateExp;

    /// <summary>本地模拟的心得丹持有数量。</summary>
    private int _expItemCount = 10;

    /// <summary>详情面板当前展示的技能，升级按钮针对它生效。</summary>
    private SkillViewData _currentSkill;

    /// <summary>窗口首次激活时绑定按钮事件。</summary>
    private void Awake()
    {
        BindButtons();
    }

    /// <summary>把关闭按钮、子页签、竖排导航与节点的点击事件挂到本窗口方法上，只执行一次。</summary>
    private void BindButtons()
    {
        if (_buttonsBound)
        {
            return;
        }
        _buttonsBound = true;
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
        // 秘笈分类只有空壳页，不接受子页切换。
        if (_category == CategorySecret)
        {
            return;
        }
        _page = page;
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
            battleText = "秘笈";
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
            // 秘笈分类只保留一个子页签，避免出现两个同样内容的入口。
            if (_category == CategorySecret && index > 0)
            {
                _tabButtons[index].gameObject.SetActive(false);
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
        // 竖排导航：当前分类用 RO 蓝底金字，其余压暗，避免整块纯白抢主视觉。
        for (int index = 0; index < _navLabels.Length; index++)
        {
            if (_navLabels[index] == null)
            {
                continue;
            }
            bool current = index == _category;
            if (current)
            {
                _navLabels[index].color = new Color(1f, 0.92f, 0.74f, 1f);
            }
            else if (index <= CategorySecret)
            {
                _navLabels[index].color = new Color(0.66f, 0.72f, 0.82f, 1f);
            }
            else
            {
                _navLabels[index].color = new Color(0.42f, 0.47f, 0.56f, 1f);
            }
            if (_navButtons[index] == null)
            {
                continue;
            }
            Image navImage = _navButtons[index].GetComponent<Image>();
            if (navImage == null)
            {
                continue;
            }
            if (current)
            {
                navImage.color = new Color(0.17f, 0.32f, 0.54f, 1f);
            }
            else
            {
                navImage.color = new Color(0.08f, 0.11f, 0.17f, 0.92f);
            }
        }
    }

    /// <summary>按当前分类与子页切换可见容器，并刷新对应内容。</summary>
    private void ApplyPage()
    {
        // 秘笈分类统一走空壳页，不需要立绘、节点与详情。
        if (_category == CategorySecret)
        {
            SetActive(_portraitRoot, false);
            SetActive(_battleRoot, false);
            SetActive(_toolRoot, false);
            SetActive(_badgeRoot, false);
            SetActive(_lightRoot, false);
            SetActive(_listRoot, false);
            SetActive(_detailPanel, false);
            SetActive(_emptyPage, true);
            SetActive(_currencyBar, false);
            return;
        }
        SetActive(_emptyPage, false);
        SetActive(_detailPanel, true);
        // 铜币条只在出战页出现，列表页底部留给滚动列表。
        SetActive(_currencyBar, _page == PageBattle);
        // 列表页：左侧铺满技能列表，隐藏立绘，避免半张脸被详情面板切掉。
        if (_page == PageList)
        {
            SetActive(_portraitRoot, false);
            SetActive(_battleRoot, false);
            SetActive(_toolRoot, false);
            SetActive(_badgeRoot, false);
            SetActive(_lightRoot, false);
            SetActive(_listRoot, true);
            FillList();
            return;
        }
        SetActive(_listRoot, false);
        SetActive(_portraitRoot, true);
        SetPortraitPosition(true);
        // 轻功出战页：左侧 3 个节点 + 中央大图标，没有工具行与技能组徽章。
        if (_category == CategoryLight)
        {
            SetActive(_battleRoot, false);
            SetActive(_toolRoot, false);
            SetActive(_badgeRoot, false);
            SetActive(_lightRoot, true);
            FillLightNodes();
            return;
        }
        SetActive(_battleRoot, true);
        SetActive(_toolRoot, true);
        SetActive(_badgeRoot, true);
        SetActive(_lightRoot, false);
        FillBattleNodes();
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

    /// <summary>按页面摆放立绘：出战页靠左给节点留位，列表页挪到列表与详情之间。</summary>
    /// <param name="battleLayout">true 表示出战页位置。</param>
    private void SetPortraitPosition(bool battleLayout)
    {
        if (_portraitRoot == null)
        {
            return;
        }
        RectTransform rect = _portraitRoot.transform as RectTransform;
        if (rect == null)
        {
            return;
        }
        if (battleLayout)
        {
            rect.anchoredPosition = new Vector2(-430f, -70f);
        }
        else
        {
            rect.anchoredPosition = new Vector2(-180f, -90f);
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
            bool isUltimateSlot = index == BattleNodeCount - 1;
            UpdateNodeVisual(_nodeButtons, _nodeIcons, _nodeRings, _nodeGlows, _nodeTags,
                index, skill, isUltimateSlot, false);
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
            icons[index].enabled = skill != null;
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
        if (skill == null)
        {
            return;
        }
        LoadNodeIcon(icons, index, skill);
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
        if (nodeIndex < 0 || nodeIndex >= BattleNodeCount)
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
        ShowDetail(_battleSkills[nodeIndex]);
    }

    /// <summary>选中轻功页的指定节点，并同步中央大图标。</summary>
    /// <param name="nodeIndex">节点下标，超出范围时忽略。</param>
    public void SelectLightNode(int nodeIndex)
    {
        if (nodeIndex < 0 || nodeIndex >= LightNodeCount)
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
        ShowDetail(selected);
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

    /// <summary>按当前分类重建列表页条目。</summary>
    private void FillList()
    {
        for (int index = 0; index < _listItems.Count; index++)
        {
            if (_listItems[index] != null)
            {
                Destroy(_listItems[index]);
            }
        }
        _listItems.Clear();
        if (_listContent == null || _listItemTemplate == null)
        {
            return;
        }
        // 当前分类下要展示的技能，按快照顺序收集后再建条目。
        List<SkillViewData> visible = new List<SkillViewData>();
        for (int index = 0; index < _allSkills.Count; index++)
        {
            // 本次判断的技能展示数据。
            SkillViewData skill = _allSkills[index];
            if (skill == null)
            {
                continue;
            }
            // 普通攻击不是技能，列表里也不与技能混排。
            if (skill.SkillType == BasicAttackType)
            {
                continue;
            }
            // 轻功列表只收滑行与轻功，武功列表收其余技能。
            bool isLight = IsLightSkill(skill);
            bool wanted = false;
            if (_category == CategoryLight)
            {
                wanted = isLight;
            }
            else
            {
                wanted = !isLight;
            }
            if (wanted)
            {
                visible.Add(skill);
            }
        }
        for (int index = 0; index < visible.Count; index++)
        {
            GameObject item = Instantiate(_listItemTemplate, _listContent);
            item.SetActive(true);
            item.transform.SetAsLastSibling();
            SkillListItemWidget widget = item.GetComponent<SkillListItemWidget>();
            if (widget != null)
            {
                widget.SetData(visible[index]);
                widget.SetSelected(index == 0);
            }
            // 每条目绑定自己的下标，点击时只刷新详情与选中态。
            Button button = item.GetComponent<Button>();
            if (button != null)
            {
                int itemIndex = index;
                button.onClick.AddListener(delegate { SelectListItem(itemIndex); });
            }
            _listItems.Add(item);
        }
        _listSkills.Clear();
        for (int index = 0; index < visible.Count; index++)
        {
            _listSkills.Add(visible[index]);
        }
        _selectedListItem = 0;
        // 内容高度按条目数显式撑开；VerticalLayoutGroup 没有 ContentSizeFitter 时滚动才生效。
        RectTransform contentRect = _listContent as RectTransform;
        if (contentRect != null)
        {
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x,
                visible.Count * 118f);
        }
        // 重建条目后把列表滚回顶部，避免停在上一次的滚动位置。
        ScrollRect scrollRect = _listContent.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
        if (visible.Count > 0)
        {
            ShowDetail(visible[0]);
        }
        else
        {
            ShowEmptyDetail();
        }
    }

    /// <summary>选中列表页的指定条目，同步高亮与右侧详情。</summary>
    /// <param name="index">条目下标，超出范围时忽略。</param>
    public void SelectListItem(int index)
    {
        if (index < 0 || index >= _listSkills.Count)
        {
            return;
        }
        _selectedListItem = index;
        for (int itemIndex = 0; itemIndex < _listItems.Count; itemIndex++)
        {
            if (_listItems[itemIndex] == null)
            {
                continue;
            }
            SkillListItemWidget widget = _listItems[itemIndex].GetComponent<SkillListItemWidget>();
            if (widget != null)
            {
                widget.SetSelected(itemIndex == index);
            }
        }
        ShowDetail(_listSkills[index]);
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
        RefreshUpgradeBlock(skill);
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
            _detailCond.text = "调息时间：" + skill.Cooldown.ToString("0.0") + " 秒\n" +
                rangeText + "\n消耗内力：" + skill.MpCost;
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
        Transform detail = transform.Find("DetailPanel");
        if (detail == null)
        {
            return;
        }
        Transform normal = detail.Find("NormalUpgradeRoot");
        Transform ultimate = detail.Find("UltimateUpgradeRoot");
        Transform currency = transform.Find("CurrencyBar");
        // 三块容器必须同时存在，避免构建中途把监听器挂重。
        if (normal == null || ultimate == null || currency == null)
        {
            return;
        }
        Transform scroll = detail.Find("DescScroll");
        _descScroll = scroll != null ? scroll.GetComponent<ScrollRect>() : null;
        _normalUpgradeRoot = normal.gameObject;
        _ultimateUpgradeRoot = ultimate.gameObject;
        _currencyBar = currency.gameObject;
        _nextLevelText = ChildText(normal, "Txt_NextLevel");
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
        Transform nodeRoot = transform.Find("NodeRoot");
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
            return;
        }
        bool ultimate = skill.Tag == UltimateTag;
        SetActive(_normalUpgradeRoot, !ultimate);
        SetActive(_ultimateUpgradeRoot, ultimate);
        if (_currencyText != null)
        {
            _currencyText.text = _currency.ToString();
        }
        // 换技能后描述回到顶部，长文本靠右侧滚动条查看；先强制排版再复位，避免内容高度尚未重算。
        if (_descScroll != null)
        {
            Canvas.ForceUpdateCanvases();
            _descScroll.verticalNormalizedPosition = 1f;
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
    }

    /// <summary>普通技能：显示角色等级门槛、单级铜币消耗与余额是否足够。</summary>
    private void RefreshNormalBlock(SkillViewData skill)
    {
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
                if (skill.Tag == UltimateTag)
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

    /// <summary>点击右下角升级按钮：扣铜币并把当前普通技能提升一级。</summary>
    private void OnUpgradeClicked()
    {
        SkillViewData skill = _currentSkill;
        if (skill == null || skill.Tag == UltimateTag)
        {
            return;
        }
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

    /// <summary>把纯文本描述转成 TMP 富文本：数值橙色、词条青蓝、下一级绿色。</summary>
    private static string BuildRichDescription(string raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return string.Empty;
        }
        string text = Regex.Replace(raw, @"(\d+\.?\d*%|\+\d+|\d+点|\d+秒|\d+名)",
            "<color=#FFB46B>$1</color>");
        text = Regex.Replace(text, @"(?m)^([\u4e00-\u9fa5]{2,4})[：:]",
            "<color=#7FD2F0>$1：</color>");
        text = Regex.Replace(text, @"(?m)^(下一级.*)$", "<color=#6BE39A>$1</color>");
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
