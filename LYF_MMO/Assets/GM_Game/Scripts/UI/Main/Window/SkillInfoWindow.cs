using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 武学面板窗口：把控制器给出的技能快照铺到门派立绘周围的环形节点上，
/// 右侧展示选中技能的名称、两字定位标签与使用条件；秘笈页当前只提供空壳界面。
/// </summary>
public class SkillInfoWindow : WindowBase
{
    /// <summary>环形技能节点数量，最后一个为绝学大节点。</summary>
    private const int NodeCount = 6;

    /// <summary>武功分类编号，展示普攻、主动技能与滑行。</summary>
    private const int CategorySkill = 0;

    /// <summary>轻功分类编号，只展示技能表 Type 为 4 的轻功。</summary>
    private const int CategoryLight = 1;

    /// <summary>秘笈分类编号，当前仅显示空壳页。</summary>
    private const int CategorySecret = 2;

    /// <summary>节点没有可展示技能时使用的两字标签。</summary>
    private const string EmptyNodeTag = "未习得";

    /// <summary>节点外圈常态颜色，深蓝呼应技能图标的冷色调。</summary>
    private static readonly Color NodeRingNormal = new Color(0.42f, 0.63f, 0.92f, 1f);

    /// <summary>绝学节点的金色外圈，用于和普通技能区分。</summary>
    private static readonly Color NodeRingUltimate = new Color(0.98f, 0.78f, 0.42f, 1f);

    /// <summary>节点没有技能时的暗色外圈。</summary>
    private static readonly Color NodeRingEmpty = new Color(0.24f, 0.31f, 0.42f, 1f);

    /// <summary>当前选中节点的亮色外圈。</summary>
    private static readonly Color NodeRingSelected = new Color(0.85f, 0.96f, 1f, 1f);

    /// <summary>当前职业名称文本，来自角色信息回包。</summary>
    [SerializeField, Header("职业")] private TMP_Text _texJob;

    /// <summary>顶部右侧的技能点数文本。</summary>
    [SerializeField, Header("技能升级点")] private TMP_Text _texPoint;

    /// <summary>标题栏关闭按钮。</summary>
    [SerializeField, Header("关闭按钮")] private Button _btnClose;

    /// <summary>顶部三个页签按钮：出战武功、轻功、秘笈。</summary>
    [SerializeField, Header("页签按钮")] private Button[] _tabButtons;

    /// <summary>页签按钮上的文字，用于切换选中配色。</summary>
    [SerializeField, Header("页签文字")] private TMP_Text[] _tabLabels;

    /// <summary>右侧竖排导航按钮：武功、轻功、秘笈、自创、双门派、失传武学。</summary>
    [SerializeField, Header("导航按钮")] private Button[] _navButtons;

    /// <summary>竖排导航按钮上的文字。</summary>
    [SerializeField, Header("导航文字")] private TMP_Text[] _navLabels;

    /// <summary>门派立绘节点，秘笈空壳页隐藏，避免压在空壳面板下面。</summary>
    [SerializeField, Header("门派立绘")] private GameObject _portraitRoot;

    /// <summary>技能节点容器，秘笈页隐藏。</summary>
    [SerializeField, Header("节点容器")] private GameObject _nodesRoot;

    /// <summary>右侧技能详情面板，秘笈页隐藏。</summary>
    [SerializeField, Header("详情面板")] private GameObject _detailPanel;

    /// <summary>秘笈空壳页，只在秘笈分类下显示。</summary>
    [SerializeField, Header("秘笈空壳页")] private GameObject _emptyPage;

    /// <summary>六个技能节点的点击按钮。</summary>
    [SerializeField, Header("节点按钮")] private Button[] _nodeButtons;

    /// <summary>六个技能节点的图标。</summary>
    [SerializeField, Header("节点图标")] private Image[] _nodeIcons;

    /// <summary>六个技能节点的外圈。</summary>
    [SerializeField, Header("节点外圈")] private Image[] _nodeRings;

    /// <summary>六个技能节点的光晕。</summary>
    [SerializeField, Header("节点光晕")] private Image[] _nodeGlows;

    /// <summary>六个技能节点下方的两字标签。</summary>
    [SerializeField, Header("节点标签")] private TMP_Text[] _nodeTags;

    /// <summary>详情面板左上角的技能图标。</summary>
    [SerializeField, Header("详情图标")] private Image _detailIcon;

    /// <summary>详情面板技能名称。</summary>
    [SerializeField, Header("详情名称")] private TMP_Text _detailName;

    /// <summary>详情面板技能等级。</summary>
    [SerializeField, Header("详情等级")] private TMP_Text _detailLevel;

    /// <summary>详情面板右上角的两字定位标签。</summary>
    [SerializeField, Header("详情标签")] private TMP_Text _detailTag;

    /// <summary>详情面板的技能说明正文。</summary>
    [SerializeField, Header("详情描述")] private TMP_Text _detailDesc;

    /// <summary>详情面板的使用条件，包含调息时间、效果范围与消耗内力。</summary>
    [SerializeField, Header("详情条件")] private TMP_Text _detailCond;

    /// <summary>控制器最近一次下发的完整技能快照，切换分类时从中筛选。</summary>
    private readonly List<SkillViewData> _allSkills = new List<SkillViewData>();

    /// <summary>当前分类下按节点顺序排列的技能，空位为 null。</summary>
    private readonly List<SkillViewData> _categorySkills = new List<SkillViewData>(NodeCount);

    /// <summary>当前选中的节点下标，范围 0 到 NodeCount-1。</summary>
    private int _selectedNode;

    /// <summary>当前分类编号，取值对应 CategorySkill、CategoryLight、CategorySecret。</summary>
    private int _category;

    /// <summary>按钮事件是否已绑定，避免窗口反复激活时重复挂监听。</summary>
    private bool _buttonsBound;

    /// <summary>窗口首次激活时绑定关闭、页签、导航与节点按钮。</summary>
    private void Awake()
    {
        BindButtons();
    }

    /// <summary>把交互按钮的点击事件挂到本窗口方法上，只执行一次。</summary>
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
        // 顶部页签与竖排导航指向同一组分类，两者保持同步高亮。
        for (int index = 0; index < _tabButtons.Length; index++)
        {
            // 当前页签的分类编号，闭包中不能直接捕获循环变量。
            int category = index;
            if (_tabButtons[index] != null)
            {
                _tabButtons[index].onClick.AddListener(delegate { SelectCategory(category); });
            }
        }
        for (int index = 0; index < _navButtons.Length; index++)
        {
            // 当前导航项对应的分类编号，前三项与页签一致。
            int category = index;
            if (_navButtons[index] == null)
            {
                continue;
            }
            // 自创、双门派、失传武学当前没有数据，按钮保持不可点状态。
            if (index >= CategorySecret + 1)
            {
                _navButtons[index].interactable = false;
                continue;
            }
            _navButtons[index].onClick.AddListener(delegate { SelectCategory(category); });
        }
        for (int index = 0; index < _nodeButtons.Length; index++)
        {
            // 当前节点的下标，用于点击后切换右侧详情。
            int nodeIndex = index;
            if (_nodeButtons[index] != null)
            {
                _nodeButtons[index].onClick.AddListener(delegate { SelectNode(nodeIndex); });
            }
        }
    }

    /// <summary>接收控制器下发的技能快照，按当前分类重新铺节点。</summary>
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
        ApplyCategory(_category);
    }

    /// <summary>把职业名与技能点数显示在标题栏，不修改角色技能数据。</summary>
    /// <param name="jobName">当前职业显示名，例如“剑 修”。</param>
    /// <param name="availablePoints">顶部右侧显示的数值，当前协议沿用角色等级。</param>
    public void SetHeader(string jobName, int availablePoints)
    {
        if (_texJob != null)
        {
            _texJob.text = jobName;
        }
        if (_texPoint != null)
        {
            _texPoint.text = "剩余技能点：" + availablePoints;
        }
    }

    /// <summary>切换分类并重铺节点；页签与竖排导航共用该入口。</summary>
    /// <param name="category">目标分类编号，0 武功、1 轻功、2 秘笈。</param>
    public void SelectCategory(int category)
    {
        if (category < CategorySkill || category > CategorySecret)
        {
            return;
        }
        ApplyCategory(category);
    }

    /// <summary>按分类筛选技能、刷新页签与导航高亮，并决定节点区还是秘笈空壳页可见。</summary>
    /// <param name="category">目标分类编号。</param>
    private void ApplyCategory(int category)
    {
        _category = category;
        _selectedNode = 0;
        RefreshCategoryVisual(category);
        if (category == CategorySecret)
        {
            if (_portraitRoot != null)
            {
                _portraitRoot.SetActive(false);
            }
            if (_nodesRoot != null)
            {
                _nodesRoot.SetActive(false);
            }
            if (_detailPanel != null)
            {
                _detailPanel.SetActive(false);
            }
            if (_emptyPage != null)
            {
                _emptyPage.SetActive(true);
            }
            return;
        }
        if (_portraitRoot != null)
        {
            _portraitRoot.SetActive(true);
        }
        if (_nodesRoot != null)
        {
            _nodesRoot.SetActive(true);
        }
        if (_detailPanel != null)
        {
            _detailPanel.SetActive(true);
        }
        if (_emptyPage != null)
        {
            _emptyPage.SetActive(false);
        }
        CollectCategorySkills(category);
        ApplyNodes();
        SelectNode(0);
    }

    /// <summary>同步页签与竖排导航的选中配色，让当前分类在两侧同时可见。</summary>
    /// <param name="category">当前选中的分类编号。</param>
    private void RefreshCategoryVisual(int category)
    {
        for (int index = 0; index < _tabLabels.Length; index++)
        {
            if (_tabLabels[index] == null)
            {
                continue;
            }
            // 页签选中时用白字压在蓝色条上，未选中时压到灰蓝字。
            if (index == category)
            {
                _tabLabels[index].color = new Color(1f, 1f, 1f, 1f);
            }
            else
            {
                _tabLabels[index].color = new Color(0.62f, 0.7f, 0.82f, 1f);
            }
            // 页签底板由预制体上的 Image 提供，选中时换成蓝色底便于和未选中区分。
            Image tabImage = null;
            if (_tabButtons[index] != null)
            {
                tabImage = _tabButtons[index].GetComponent<Image>();
            }
            if (tabImage != null)
            {
                if (index == category)
                {
                    tabImage.color = new Color(0.28f, 0.46f, 0.74f, 1f);
                }
                else
                {
                    tabImage.color = new Color(0.13f, 0.17f, 0.24f, 0.95f);
                }
            }
        }
        for (int index = 0; index < _navLabels.Length; index++)
        {
            if (_navLabels[index] == null)
            {
                continue;
            }
            // 竖排导航选中项为浅底深字，未选中为深底浅字。
            if (index == category)
            {
                _navLabels[index].color = new Color(0.1f, 0.17f, 0.28f, 1f);
                if (_navButtons[index] != null)
                {
                    // 选中项统一使用浅色底板，便于一眼看出当前分类。
                    Image navImage = _navButtons[index].GetComponent<Image>();
                    if (navImage != null)
                    {
                        navImage.color = new Color(0.9f, 0.95f, 1f, 1f);
                    }
                }
            }
            else
            {
                if (index <= CategorySecret)
                {
                    _navLabels[index].color = new Color(0.66f, 0.72f, 0.82f, 1f);
                }
                else
                {
                    _navLabels[index].color = new Color(0.42f, 0.47f, 0.56f, 1f);
                }
                if (_navButtons[index] != null)
                {
                    Image navImage = _navButtons[index].GetComponent<Image>();
                    if (navImage != null)
                    {
                        navImage.color = new Color(0.1f, 0.13f, 0.19f, 0.9f);
                    }
                }
            }
        }
    }

    /// <summary>按分类把技能快照筛成节点顺序：前五个为普通节点，第六个为绝学节点。</summary>
    /// <param name="category">目标分类编号，只处理武功与轻功。</param>
    private void CollectCategorySkills(int category)
    {
        _categorySkills.Clear();
        for (int index = 0; index < NodeCount; index++)
        {
            _categorySkills.Add(null);
        }
        // 当前分类下的有效技能按原顺序暂存，超过节点数量时只保留前六个。
        List<SkillViewData> matched = new List<SkillViewData>(NodeCount);
        for (int index = 0; index < _allSkills.Count; index++)
        {
            // 本次判断的技能展示数据，空值来自尚未装载的条目。
            SkillViewData skill = _allSkills[index];
            if (skill == null)
            {
                continue;
            }
            // 轻功页只收 Type=4，武功页收普攻、主动技能与滑行。
            bool isLightSkill = skill.SkillType == 4;
            bool wanted = false;
            if (category == CategoryLight)
            {
                wanted = isLightSkill;
            }
            else
            {
                wanted = !isLightSkill;
            }
            if (wanted && matched.Count < NodeCount)
            {
                matched.Add(skill);
            }
        }
        for (int index = 0; index < matched.Count; index++)
        {
            _categorySkills[index] = matched[index];
        }
    }

    /// <summary>把当前分类的技能铺到六个节点上，空位显示未习得状态。</summary>
    private void ApplyNodes()
    {
        for (int index = 0; index < NodeCount; index++)
        {
            // 当前节点对应的技能，null 表示该槽位没有可展示技能。
            SkillViewData skill = _categorySkills[index];
            bool isUltimate = index == NodeCount - 1;
            if (_nodeTags[index] != null)
            {
                if (skill != null && skill.Level > 0)
                {
                    _nodeTags[index].text = skill.Tag;
                }
                else
                {
                    _nodeTags[index].text = EmptyNodeTag;
                }
                if (isUltimate)
                {
                    // 绝学节点标签用金色，和普通技能的冷白区分。
                    _nodeTags[index].color = new Color(1f, 0.86f, 0.55f, 1f);
                }
                else
                {
                    _nodeTags[index].color = new Color(0.92f, 0.96f, 1f, 1f);
                }
            }
            if (_nodeButtons[index] != null)
            {
                _nodeButtons[index].interactable = skill != null;
            }
            if (_nodeIcons[index] != null)
            {
                _nodeIcons[index].enabled = skill != null;
            }
            if (_nodeRings[index] != null)
            {
                if (skill == null)
                {
                    _nodeRings[index].color = NodeRingEmpty;
                }
                else if (isUltimate)
                {
                    _nodeRings[index].color = NodeRingUltimate;
                }
                else
                {
                    _nodeRings[index].color = NodeRingNormal;
                }
            }
            if (_nodeGlows[index] != null)
            {
                _nodeGlows[index].enabled = false;
            }
            if (skill != null && !string.IsNullOrEmpty(skill.IconPath))
            {
                // 节点下标，异步回调只回填对应槽位，避免切换分类后串图。
                int nodeIndex = index;
                ResourceMgr.Instance.LoadSpriteAsync(skill.IconPath, sprite =>
                {
                    if (_nodeIcons[nodeIndex] == null || sprite == null)
                    {
                        return;
                    }
                    if (nodeIndex >= _categorySkills.Count)
                    {
                        return;
                    }
                    // 回调时槽位可能已被新分类覆盖，技能编号一致才回填。
                    if (_categorySkills[nodeIndex] != null &&
                        _categorySkills[nodeIndex].SkillId == skill.SkillId)
                    {
                        _nodeIcons[nodeIndex].sprite = sprite;
                    }
                });
            }
        }
    }

    /// <summary>选中指定节点，右侧详情随之切换。</summary>
    /// <param name="nodeIndex">节点下标，超出范围时忽略。</param>
    public void SelectNode(int nodeIndex)
    {
        if (nodeIndex < 0 || nodeIndex >= NodeCount)
        {
            return;
        }
        _selectedNode = nodeIndex;
        // 选中高亮：当前节点提亮外圈并打开光晕，其余恢复常态。
        for (int index = 0; index < NodeCount; index++)
        {
            if (_nodeRings[index] == null)
            {
                continue;
            }
            // 该节点对应的技能，用于区分绝学金圈与普通蓝圈。
            SkillViewData skill = _categorySkills[index];
            if (index == nodeIndex)
            {
                _nodeRings[index].color = NodeRingSelected;
            }
            else if (skill == null)
            {
                _nodeRings[index].color = NodeRingEmpty;
            }
            else if (index == NodeCount - 1)
            {
                _nodeRings[index].color = NodeRingUltimate;
            }
            else
            {
                _nodeRings[index].color = NodeRingNormal;
            }
            if (_nodeGlows[index] != null)
            {
                _nodeGlows[index].enabled = index == nodeIndex;
            }
        }
        ShowDetail(_categorySkills[nodeIndex]);
    }

    /// <summary>把技能数据写到右侧详情面板；空数据时展示未习得提示。</summary>
    /// <param name="skill">选中的技能展示数据，没有可展示技能时为空。</param>
    private void ShowDetail(SkillViewData skill)
    {
        if (skill == null)
        {
            ShowEmptyDetail();
            return;
        }
        if (_detailName != null)
        {
            _detailName.text = skill.Name;
        }
        if (_detailLevel != null)
        {
            if (skill.Level > 0)
            {
                _detailLevel.text = "Lv." + skill.Level;
            }
            else
            {
                _detailLevel.text = "未习得";
            }
        }
        if (_detailTag != null)
        {
            _detailTag.text = skill.Tag;
        }
        if (_detailDesc != null)
        {
            _detailDesc.text = skill.Description;
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
        if (_detailIcon != null)
        {
            _detailIcon.enabled = true;
            if (!string.IsNullOrEmpty(skill.IconPath))
            {
                // 当前详情技能编号，异步图标回来时用于拒绝过期结果。
                int skillId = skill.SkillId;
                ResourceMgr.Instance.LoadSpriteAsync(skill.IconPath, sprite =>
                {
                    if (_detailIcon == null || sprite == null)
                    {
                        return;
                    }
                    // 快照已切换时不再覆盖详情图标。
                    if (_categorySkills.Count > _selectedNode &&
                        _categorySkills[_selectedNode] != null &&
                        _categorySkills[_selectedNode].SkillId == skillId)
                    {
                        _detailIcon.sprite = sprite;
                    }
                });
            }
        }
    }

    /// <summary>节点为空时清空详情文本，避免残留上一个技能的内容。</summary>
    private void ShowEmptyDetail()
    {
        if (_detailName != null)
        {
            _detailName.text = "未习得";
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
            _detailDesc.text = "该槽位尚未习得技能，请先在技能列表中选择要装配的技能。";
        }
        if (_detailCond != null)
        {
            _detailCond.text = string.Empty;
        }
        if (_detailIcon != null)
        {
            _detailIcon.enabled = false;
        }
    }
}
