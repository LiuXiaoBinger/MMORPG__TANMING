using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>装备详情子窗口，只展示数据并通知控制器提交穿脱和特技选择。</summary>
public sealed class EquipmentDetailWindow : WindowBase
{
    #region 既有正式控件
    /// <summary>显示装备名和强化等级的标题。</summary>
    [SerializeField] private TMP_Text _nameText;
    /// <summary>固定顶部的装备图标，使用现有资源加载入口。</summary>
    [SerializeField] private Image _icon;
    /// <summary>固定顶部的中文部位名称。</summary>
    [SerializeField] private TMP_Text _partText;
    /// <summary>基础属性左列名称，与右列数值逐行对齐。</summary>
    [SerializeField] private TMP_Text _attributesText;
    /// <summary>基础属性右列数值，采用与左列相同的行距。</summary>
    [SerializeField] private TMP_Text _attributeValuesText;
    /// <summary>只滚动中间属性、养成和特技，顶部和底部按钮保持固定。</summary>
    [SerializeField] private ScrollRect _scroll;
    /// <summary>预制体中按槽0、1、2排列的单行特技摘要。</summary>
    [SerializeField] private TMP_Text[] _specialSkillTexts;
    /// <summary>未穿戴时显示的穿戴按钮。</summary>
    [SerializeField] private Button _wearButton;
    /// <summary>已穿戴时显示的卸下按钮。</summary>
    [SerializeField] private Button _takeOffButton;
    #endregion

    #region 头部元信息与图标按钮
    /// <summary>头部元信息汇总：装备状态、部位品质、评分、需求等级、职业与绑定。</summary>
    [SerializeField] private TMP_Text _headerMetaText;
    /// <summary>头部笔形编辑按钮，只给界面反馈，不修改装备数据。</summary>
    [SerializeField] private Button _editButton;
    /// <summary>头部信息按钮，打开窗口内说明层。</summary>
    [SerializeField] private Button _infoButton;
    #endregion

    #region 正文附加分区
    /// <summary>镶嵌下拉按钮。</summary>
    [SerializeField] private Button _gemDropButton;
    /// <summary>镶嵌下拉占位容器。</summary>
    [SerializeField] private GameObject _gemDropPanel;
    /// <summary>镶嵌下拉占位正文。</summary>
    [SerializeField] private TMP_Text _gemDropText;
    /// <summary>强化与套装摘要独立行，与总说明分离呈现。</summary>
    [SerializeField] private TMP_Text _enhanceRowText;
    /// <summary>六个空槽图标，按真实宝石孔数量决定显示个数，其余隐藏。</summary>
    [SerializeField] private GameObject[] _gemSlots;
    /// <summary>淬锋与铭缀独立折叠行。</summary>
    [SerializeField] private TMP_Text _forgeRowText;
    /// <summary>淬锋与铭缀行的折叠按钮，切换该行显隐。</summary>
    [SerializeField] private Button _forgeFoldButton;
    /// <summary>三个特技行末的单个图标入口，按保存槽顺序打开对应侧卡。</summary>
    [SerializeField] private Button[] _specialSkillDetailButtons;
    /// <summary>外观独立行，无实例数据时显示中性占位。</summary>
    [SerializeField] private TMP_Text _appearanceRowText;
    /// <summary>祭甲与甲片独立行，无实例数据时整行中性占位。</summary>
    [SerializeField] private TMP_Text _garnishRowText;
    /// <summary>显示角色在六个指定套装部位的当前穿戴件数。</summary>
    [SerializeField] private TMP_Text _setRowText;
    /// <summary>套装行末的按钮，用于展开或收起六个部位的穿戴状态。</summary>
    [SerializeField] private Button _setViewButton;
    /// <summary>外观行末尾的查看入口，只提示暂未开放，不请求外观数据。</summary>
    [SerializeField] private Button _appearanceViewButton;
    /// <summary>铭缀独立行的摘要文本，无数据时显示中性空态。</summary>
    [SerializeField] private TMP_Text _inscribeRowValue;
    /// <summary>洗练独立行的摘要文本，无数据时显示中性空态。</summary>
    [SerializeField] private TMP_Text _washRowValue;
    /// <summary>铭缀独立行末的查看入口，只给未开放提示。</summary>
    [SerializeField] private Button _inscribeRowButton;
    /// <summary>洗练独立行末的查看入口，只给未开放提示。</summary>
    [SerializeField] private Button _washRowButton;
    /// <summary>镶嵌展开区，折叠时整体隐藏并让父布局回收高度。</summary>
    [SerializeField] private GameObject _gemExpandPanel;
    /// <summary>镶嵌展开区的孔位说明文本。</summary>
    [SerializeField] private TMP_Text _gemInfoText;
    /// <summary>套装展开区，折叠时整体隐藏并让父布局回收高度。</summary>
    [SerializeField] private GameObject _setExpandPanel;
    /// <summary>套装展开区正文，每行显示两个部位及其穿戴状态。</summary>
    [SerializeField] private TMP_Text _setExpandText;
    /// <summary>淬锋展开区，折叠时整体隐藏并让父布局回收高度。</summary>
    [SerializeField] private GameObject _forgeExpandPanel;
    /// <summary>淬锋展开区正文。</summary>
    [SerializeField] private TMP_Text _forgeExpandText;
    /// <summary>镶嵌展开区的六个持久孔位格，容量固定，按真实孔数启用前若干个。</summary>
    [SerializeField] private GameObject[] _gemCells;
    /// <summary>六个孔位格所在的网格容器，格子坐标与网格高度都按它的实际宽度推导。</summary>
    [SerializeField] private RectTransform _gemSlotGrid;
    #endregion

    #region 头部角标与双列属性
    /// <summary>图标左上角状态角标，只显示装备中或未装备。</summary>
    [SerializeField] private TMP_Text _wornBadgeText;
    /// <summary>头部右上部位与品质行，品质无真实字段时用中性占位。</summary>
    [SerializeField] private TMP_Text _headerPartText;
    /// <summary>头部右上绑定位，绑定无真实字段时用中性占位。</summary>
    [SerializeField] private TMP_Text _headerBindText;
    /// <summary>基础属性右列名称，与左侧 _attributesText 行号一一对应。</summary>
    [SerializeField] private TMP_Text _attrRightNamesText;
    /// <summary>基础属性右列数值，保留配置的百分数语义。</summary>
    [SerializeField] private TMP_Text _attrRightValuesText;
    #endregion

    #region 右侧主按钮与更多菜单
    /// <summary>右侧强化主按钮，点击只给暂未开放提示。</summary>
    [SerializeField] private Button _strengthenButton;
    /// <summary>右侧铭缀主按钮，点击只给暂未开放提示。</summary>
    [SerializeField] private Button _inscribeButton;
    /// <summary>右侧熔锻主按钮，点击只给暂未开放提示。</summary>
    [SerializeField] private Button _forgeButton;
    /// <summary>右侧更多按钮，切换下拉菜单显隐。</summary>
    [SerializeField] private Button _moreButton;
    /// <summary>更多下拉菜单容器，初始隐藏。</summary>
    [SerializeField] private GameObject _moreMenu;
    /// <summary>更多菜单四个按钮，顺序为淬锋、进阶、重铸、属性转换。</summary>
    [SerializeField] private Button[] _moreMenuButtons;
    #endregion

    #region 底栏与说明层
    /// <summary>底栏交易状态与耐久正文。</summary>
    [SerializeField] private TMP_Text _footerText;
    /// <summary>底栏展开按钮。</summary>
    [SerializeField] private Button _footerExpandButton;
    /// <summary>底栏展开后的附加容器。</summary>
    [SerializeField] private GameObject _footerExtra;
    /// <summary>底栏展开后的中性占位正文。</summary>
    [SerializeField] private TMP_Text _footerExtraText;
    /// <summary>窗口内说明层容器，初始隐藏。</summary>
    [SerializeField] private GameObject _infoLayer;
    /// <summary>说明层正文。</summary>
    [SerializeField] private TMP_Text _infoLayerText;
    /// <summary>说明卡正文滚动区，卡高按内容收紧时它同步缩短，底部按钮跟随卡底。</summary>
    [SerializeField] private RectTransform _infoLayerScroll;
    /// <summary>说明层关闭按钮。</summary>
    [SerializeField] private Button _infoLayerCloseButton;
    /// <summary>说明卡标题，打开属性卡或特技卡时按内容改写。</summary>
    [SerializeField] private TMP_Text _infoLayerTitleText;
    /// <summary>特技侧卡右上角的使用入口，提交当前正在阅读的槽位。</summary>
    [SerializeField] private Button _cardUseButton;
    /// <summary>特技侧卡右上角的只读状态，区分当前生效、已选未穿和空槽。</summary>
    [SerializeField] private TMP_Text _cardStateText;
    /// <summary>说明卡底部的兑换入口，只给暂未开放提示。</summary>
    [SerializeField] private Button _cardDeductButton;
    /// <summary>说明卡底部的拓源入口，只给暂未开放提示。</summary>
    [SerializeField] private Button _cardSourceButton;
    /// <summary>说明卡底部的特技卸下入口，只给暂未开放提示。</summary>
    [SerializeField] private Button _cardRemoveButton;
    /// <summary>基础属性分区行末的查看按钮，打开属性说明卡。</summary>
    [SerializeField] private Button _attrViewButton;
    /// <summary>图标外框的蓝青展示光效，不代表未知的装备品质。</summary>
    [SerializeField] private Image _iconGlow;
    #endregion

    /// <summary>玩家请求穿戴当前详情装备。</summary>
    public event Action WearRequested;
    /// <summary>玩家请求卸下当前详情装备。</summary>
    public event Action TakeOffRequested;
    /// <summary>玩家请求启用特技槽，参数范围0至2。</summary>
    public event Action<int> SpecialSkillRequested;

    /// <summary>展开态下折叠按钮的提示词，指向「点了会收起」。</summary>
    private const string FoldCollapseText = "收起";
    /// <summary>折叠态下折叠按钮的提示词，指向「点了会展开」。</summary>
    private const string FoldExpandText = "展开";
    /// <summary>外框闪动起点为清晰的蓝色，透明度0.95，确保浅蓝底上可见。</summary>
    private static readonly Color GlowBlue = new Color(0.25f, 0.55f, 1f, 0.95f);
    /// <summary>外框闪动终点为青色，透明度1，不影响武器图标本身的颜色。</summary>
    private static readonly Color GlowCyan = new Color(0.18f, 1f, 0.76f, 1f);
    /// <summary>外框每秒走过的单程相位，一次蓝青蓝往返约2.7秒。</summary>
    private const float GlowPulseSpeed = 0.75f;
    /// <summary>详情窗口在屏幕上的最小显示缩放，1366 下按它补偿，保证正文可读。</summary>
    private const float MinScreenScale = 0.9f;
    /// <summary>说明卡最大高度，与预制体默认卡高一致，特技卡到顶后在卡内滚动。</summary>
    private const float CardMaxHeight = 452f;
    /// <summary>说明卡最小高度，短属性卡收紧后仍保留标题、正文与按钮三块。</summary>
    private const float CardMinHeight = 150f;
    /// <summary>说明卡固定宽度，与预制体卡宽一致。</summary>
    private const float CardWidth = 316f;
    /// <summary>说明卡正文宽度，等于卡宽减去左右内边距。</summary>
    private const float CardBodyWidth = 300f;
    /// <summary>说明卡顶部标题区高度，正文滚动区从这里往下开始。</summary>
    private const float CardTopArea = 42f;
    /// <summary>说明卡底部三按钮区高度，按钮固定贴在卡底内边距之上。</summary>
    private const float CardButtonArea = 34f;
    /// <summary>说明卡底部留白，无按钮时只留这一条边距。</summary>
    private const float CardBottomGap = 8f;
    /// <summary>说明卡底部按钮自身高度，用于把按钮行贴到卡底。</summary>
    private const float CardButtonHeight = 26f;
    /// <summary>镶嵌孔位格边长，与预制体 GemCell 尺寸一致。</summary>
    private const float GemCellSize = 32f;
    /// <summary>镶嵌孔位格行距，等于格高加行间空隙。</summary>
    private const float GemCellRowStep = 34f;
    /// <summary>可启用特技的标题字色，与正文一致。</summary>
    private static readonly Color ActiveSkillTitleColor = new Color(0.07f, 0.19f, 0.33f, 1f);
    /// <summary>空槽标题字色，压暗表示不可启用。</summary>
    private static readonly Color DisabledSkillTitleColor = new Color(0.55f, 0.62f, 0.70f, 1f);
    /// <summary>附加分区展开高度，容纳两行新增信息后由 340 扩到 400。</summary>
    private const float ExpandedSectionHeight = 400f;
    /// <summary>附加分区折叠高度，只保留标题行。</summary>
    private const float CollapsedSectionHeight = 40f;
    /// <summary>更多按钮在菜单收起时的原始名称。</summary>
    private const string MoreText = "更多";
    /// <summary>四个更多菜单项的准确名称，与用户展开菜单截图一致。</summary>
    private static readonly string[] MoreMenuNames = { "淬锋", "进阶", "重铸", "属性转换" };
    /// <summary>三个右侧主按钮的准确名称，与用户展开菜单截图一致。</summary>
    private static readonly string[] MainButtonNames = { "强化", "铭缀", "熔锻" };
    /// <summary>当前装备真实宝石孔数量，供折叠复位时恢复空槽显示个数。</summary>
    private int _gemSlotCount;
    /// <summary>淬锋行是否处于折叠态：正文清透明但保留占位，按钮不离开行末。</summary>
    private bool _forgeRowCollapsed;
    /// <summary>当前装备的静态说明与获取途径，供信息按钮还原说明层正文。</summary>
    private string _equipmentInfoText;
    /// <summary>属性说明卡正文，打开属性卡时写入说明层。</summary>
    private string _attributeCardText;
    /// <summary>三个特技槽各自的完整说明，供每行末尾的详情入口按槽位取用。</summary>
    private readonly string[] _specialSkillDetails = new string[3];
    /// <summary>三个特技槽各自的说明卡正文，已按标题、当前层、下一层、同名说明分区。</summary>
    private readonly string[] _specialSkillCards = new string[3];
    /// <summary>三个特技槽的侧卡标题，包含真实特技名与等级。</summary>
    private readonly string[] _specialSkillCardTitles = new string[3];
    /// <summary>三个特技槽的只读状态，由控制器根据服务器选择和穿戴状态生成。</summary>
    private readonly string[] _specialSkillCardStates = new string[3];
    /// <summary>各槽是否可使用，缓存当前Render数据，打开侧卡时决定使用入口显隐。</summary>
    private readonly bool[] _canUseSpecialSkills = new bool[3];
    /// <summary>当前特技侧卡对应的保存槽下标，范围0至2，仅用于原有选择事件。</summary>
    private int _currentCardSkillSlot;
    /// <summary>当前开窗的光效起点，使用不受游戏暂停影响的秒数。</summary>
    private float _glowStartedAt;

    /// <summary>把状态词写进按钮自带 Label；Label 已铺满父 Rect，长词也不会溢出按钮背景。</summary>
    /// <param name="button">需要改写文案的扩展按钮。</param>
    /// <param name="text">收起或展开的状态词。</param>
    private static void SetButtonLabel(Button button, string text)
    {
        // label 是按钮子节点上的 Label，扩展按钮均为一层 Label 子节点结构。
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = text;
    }

    #region 初始化与注册
    /// <summary>注册正式预制体中的固定按钮，窗口生命周期内执行一次。</summary>
    public override void InitWindow()
    {
        _wearButton.onClick.AddListener(OnWearRequested);
        _takeOffButton.onClick.AddListener(OnTakeOffRequested);
        _cardUseButton.onClick.AddListener(OnCardUseClicked);
        _editButton.onClick.AddListener(OnEditClicked);
        _infoButton.onClick.AddListener(OnInfoClicked);
        _gemDropButton.onClick.AddListener(OnGemDropClicked);
        _setViewButton.onClick.AddListener(OnSetViewClicked);
        _appearanceViewButton.onClick.AddListener(OnAppearanceViewClicked);
        // index 是特技槽下标，每行末尾的详情入口只展示本槽完整说明。
        for (int index = 0; index < _specialSkillDetailButtons.Length; index++)
        {
            int slot = index;
            _specialSkillDetailButtons[index].onClick.AddListener(() => OnSpecialSkillDetailClicked(slot));
        }
        _forgeFoldButton.onClick.AddListener(OnForgeFoldClicked);
        _inscribeRowButton.onClick.AddListener(OnInscribeRowClicked);
        _washRowButton.onClick.AddListener(OnWashRowClicked);
        _strengthenButton.onClick.AddListener(OnMainButtonFirst);
        _inscribeButton.onClick.AddListener(OnMainButtonSecond);
        _forgeButton.onClick.AddListener(OnMainButtonThird);
        _moreButton.onClick.AddListener(OnMoreClicked);
        _footerExpandButton.onClick.AddListener(OnFooterExpandClicked);
        _infoLayerCloseButton.onClick.AddListener(OnInfoLayerClosed);
        _cardDeductButton.onClick.AddListener(OnCardDeductClicked);
        _cardSourceButton.onClick.AddListener(OnCardSourceClicked);
        _cardRemoveButton.onClick.AddListener(OnCardRemoveClicked);
        _attrViewButton.onClick.AddListener(OnAttributeViewClicked);
        // index 是更多菜单按钮下标，逐一绑定；slot 是闭包内固定下来的当前下标。
        for (int index = 0; index < _moreMenuButtons.Length; index++)
        {
            int slot = index;
            _moreMenuButtons[index].onClick.AddListener(() => OnMoreMenuClicked(slot));
        }
    }
    #endregion

    #region 界面刷新
    /// <summary>刷新服务器已确认的装备详情，不提前修改穿戴或特技状态。</summary>
    /// <param name="data">控制器根据最新实例和配置生成的纯展示数据。</param>
    public void Render(EquipmentDetailViewData data)
    {
        // name 是装备名的最终显示文本：空名保持空串，非空则在分隔点换行，两行都完整显示，长名不再让最后一字落单。
        string name = string.Empty;
        if (data.Name != null)
        {
            name = data.Name.Replace("·", "·\n");
        }
        _nameText.text = name;
        _partText.text = data.PartName;
        _attributesText.text = data.Attributes;
        _attributeValuesText.text = data.AttributeValues;
        _headerMetaText.text = data.MetaText;
        _gemDropText.text = data.GemDropText;
        // 铭缀、洗练、镶嵌、套装、淬锋五个分区的正文只做展示，文本全部来自控制器。
        _inscribeRowValue.text = data.InscribeRowText;
        _washRowValue.text = data.WashRowText;
        _gemInfoText.text = data.GemInfoText;
        _setExpandText.text = data.SetExpandText;
        _forgeExpandText.text = data.ForgeExpandText;
        _footerText.text = data.FooterText;
        _footerExtraText.text = data.FooterExtraText;
        _infoLayerText.text = data.InfoLayerText;
        _equipmentInfoText = data.InfoLayerText;
        // 属性卡正文由控制器按真实属性生成，窗口只负责换文本。
        _attributeCardText = data.AttributeCardText;
        _enhanceRowText.text = data.EnhanceRowText;
        _forgeRowText.text = data.ForgeRowText;
        _appearanceRowText.text = data.AppearanceRowText;
        _garnishRowText.text = data.GarnishRowText;
        _setRowText.text = data.SetRowText;
        // 角标只由服务器穿戴状态派生；右上部位品质与绑定各自独立，避免挤在已改窄的元信息行。
        _wornBadgeText.text = data.WornBadgeText;
        _headerPartText.text = data.HeaderPartText;
        _headerBindText.text = data.HeaderBindText;
        // 右列名称与数值独立赋值，行序与左列基础属性保持一致。
        _attrRightNamesText.text = data.AttributeRightNames;
        _attrRightValuesText.text = data.AttributeRightValues;
        _icon.sprite = null;
        // 图标仅用于展示，装备身份和所有操作仍由控制器持有。
        ResourceMgr.Instance.LoadSpriteAsync(data.IconPath, sprite => _icon.sprite = sprite);
        _wearButton.gameObject.SetActive(!data.IsWorn);
        _takeOffButton.gameObject.SetActive(data.IsWorn);
        // 三个保存槽只显示一行摘要和一个查看图标，空槽使用次级色但仍可查看空态说明。
        for (int index = 0; index < 3; index++)
        {
            // 首行只显示槽位、名称、等级与状态；完整说明留给详情层，不在行内换行溢出。
            _specialSkillTexts[index].text = data.SpecialSkillHeadlines[index];
            // isEmptySlot 表示该槽没有任何特技族 ID，只有真正空槽才用浅灰禁用色。
            bool isEmptySlot = data.IsSpecialSkillEmpty[index];
            if (isEmptySlot)
            {
                _specialSkillTexts[index].color = DisabledSkillTitleColor;
            }
            else
            {
                _specialSkillTexts[index].color = ActiveSkillTitleColor;
            }
        }
        // 打开新装备时把新增交互状态复位，避免上一次的展开状态残留。
        // gemSlotCount 是当前装备真实宝石孔数量，SetFoldableRows 按它显示空槽图标。
        int gemSlotCount = data.GemSlotCount;
        _gemSlotCount = gemSlotCount;
        CacheSpecialSkillDetails(data);
        ApplyGemSlots();
        // 正文分区已改为滚动区内的自然排列，不再有总折叠与固定高度空壳。
        // 三个展开区统一回到折叠态，淬锋折叠标志也由 ApplyExpandPanels 里同步复位。
        ApplyExpandPanels();
        SetButtonLabel(_footerExpandButton, FoldExpandText);
        SetButtonLabel(_moreButton, MoreText);
        _gemDropPanel.gameObject.SetActive(false);
        _moreMenu.gameObject.SetActive(false);
        // 附加分区默认收起，底栏只保留固定一行交易与耐久，不开窗就重复显示说明正文。
        _footerExtra.gameObject.SetActive(false);
        SetButtonLabel(_footerExpandButton, FoldExpandText);
        _infoLayer.gameObject.SetActive(false);
        // 每次打开从蓝色起点开始，在预制体内的外框上做实际颜色变化。
        _glowStartedAt = Time.unscaledTime;
        ApplyGlowColor(0f);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_scroll.content);
        // 本窗口的显示尺度在每次刷新时按根画布缩放补偿，1366 下正文不会被缩到 10 像素。
        ApplyDisplayScale();
    }

    /// <summary>窗口启用时检测左键外部点击并更新外框颜色，隐藏后不再执行。</summary>
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (CloseOnOutsideClick(Input.mousePosition))
            {
                return;
            }
        }
        if (_iconGlow == null)
        {
            return;
        }
        // phase 是 0 到 1 来回往复的呼吸相位，用不受暂停影响的时间驱动。
        float phase = Mathf.PingPong((Time.unscaledTime - _glowStartedAt) * GlowPulseSpeed, 1f);
        ApplyGlowColor(phase);
    }

    /// <summary>外部点击按说明卡、更多菜单、主详情的显示层级，每次只收起最上层。</summary>
    /// <param name="screenPosition">左键按下的屏幕像素坐标，原点位于屏幕左下角。</param>
    /// <returns>只有主详情已关闭时为true；收起小窗后保留主详情并继续更新光效。</returns>
    private bool CloseOnOutsideClick(Vector2 screenPosition)
    {
        // canvas 是所属根画布，使用其实际相机适配项目的UI缩放与两种画布模式。
        Canvas canvas = GetComponentInParent<Canvas>().rootCanvas;
        // eventCamera 在屏幕覆盖模式下保持空，其余模式使用画布的UI相机。
        Camera eventCamera = null;
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            eventCamera = canvas.worldCamera;
        }
        if (_infoLayer.activeSelf)
        {
            // insideCard 表示点击仍在最上层说明卡内，卡内交互不收起任何窗口。
            bool insideCard = RectTransformUtility.RectangleContainsScreenPoint(
                _infoLayer.transform as RectTransform, screenPosition, eventCamera);
            if (insideCard)
            {
                return false;
            }
            OnInfoLayerClosed();
            return false;
        }
        if (_moreMenu.activeSelf)
        {
            // insideMenu 表示点击落在已展开的更多菜单内，保留菜单项的原有点击入口。
            bool insideMenu = RectTransformUtility.RectangleContainsScreenPoint(
                _moreMenu.transform as RectTransform, screenPosition, eventCamera);
            // insideMoreButton 保留按钮自己的切换，避免按下时先收起、抬起后又重新打开菜单。
            bool insideMoreButton = RectTransformUtility.RectangleContainsScreenPoint(
                _moreButton.transform as RectTransform, screenPosition, eventCamera);
            if (insideMenu || insideMoreButton)
            {
                return false;
            }
            OnMoreClicked();
            return false;
        }
        // insideMain 表示点击落在固定头尾、滚动正文或右侧操作区内。
        bool insideMain = RectTransformUtility.RectangleContainsScreenPoint(
            transform as RectTransform, screenPosition, eventCamera);
        if (insideMain)
        {
            return false;
        }
        CloseWindow();
        return true;
    }

    /// <summary>窗口隐藏或关闭时把外框恢复为蓝色起点。</summary>
    private void OnDisable()
    {
        ApplyGlowColor(0f);
    }

    /// <summary>在蓝色和青色之间更新展示框，中央镂空由预制体Image保留武器原色。</summary>
    /// <param name="phase">单程闪动相位，范围0至1。</param>
    private void ApplyGlowColor(float phase)
    {
        if (_iconGlow == null)
        {
            return;
        }
        _iconGlow.color = Color.Lerp(GlowBlue, GlowCyan, phase);
    }

    /// <summary>每次打开详情从正文顶部阅读，不改变已经绑定的装备或按钮事件。</summary>
    private void OnEnable()
    {
        _scroll.verticalNormalizedPosition = 1f;
    }
    #endregion

    #region 业务按钮（保持既有三个事件语义）
    /// <summary>转交穿戴按钮事件。</summary>
    private void OnWearRequested() { WearRequested?.Invoke(); }
    /// <summary>转交卸下按钮事件。</summary>
    private void OnTakeOffRequested() { TakeOffRequested?.Invoke(); }
    /// <summary>侧卡的使用按钮沿用原事件，交给控制器选择当前阅读的特技槽。</summary>
    private void OnCardUseClicked() { SpecialSkillRequested?.Invoke(_currentCardSkillSlot); }
    #endregion

    #region 新增界面的可见反馈
    /// <summary>编辑按钮只提示准确名称与暂未开放，不改装备数据。</summary>
    private void OnEditClicked()
    {
        TipsMgr.Instance.ShowSystemTips("编辑：暂未开放");
    }

    /// <summary>信息按钮打开说明层并还原当前装备的静态说明与获取途径。</summary>
    private void OnInfoClicked()
    {
        _infoLayerTitleText.text = "装备说明";
        _infoLayerText.text = _equipmentInfoText;
        ApplyCardSkillButtons(false);
        _infoLayer.gameObject.SetActive(true);
        // 普通信息卡没有底部三按钮，按正文高度收紧到卡底只留一条内边距。
        ApplyCardLayout(false);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_infoLayer.transform as RectTransform);
    }

    /// <summary>基础属性行末的查看入口打开属性说明卡，只展示真实属性与贡献来源。</summary>
    private void OnAttributeViewClicked()
    {
        _infoLayerTitleText.text = "属性说明";
        _infoLayerText.text = _attributeCardText;
        // 属性卡不提供特技相关入口，底部三个按钮整体隐藏。
        ApplyCardSkillButtons(false);
        _infoLayer.gameObject.SetActive(true);
        // 属性条目少时卡高收到最小高度，不再固定 452 留出大片空白。
        ApplyCardLayout(false);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_infoLayer.transform as RectTransform);
    }

    /// <summary>按正文实际高度收紧或撑开说明卡，正文滚动区与底部按钮都贴在卡内。</summary>
    /// <param name="withSkillButtons">true 表示特技卡，底部保留兑换、拓源与特技卸下三按钮。</param>
    private void ApplyCardLayout(bool withSkillButtons)
    {
        // bottomArea 是卡底固定占位：有按钮时容纳按钮行，无按钮时只留一条留白。
        float bottomArea = CardBottomGap;
        if (withSkillButtons) bottomArea = CardButtonArea;
        // bodyMax 是正文可占的最大高度，卡总高不会超过预制体给的 452。
        float bodyMax = CardMaxHeight - CardTopArea - bottomArea;
        // preferred 是正文按卡宽换行后的首选高度，短属性卡远小于最大高度。
        float preferred = _infoLayerText.GetPreferredValues(_infoLayerText.text, CardBodyWidth, 0f).y;
        // bodyHeight 是正文实际占用高度，过长时封顶并在卡内滚动。
        float bodyHeight = preferred;
        if (bodyHeight > bodyMax) bodyHeight = bodyMax;
        if (bodyHeight < 0f) bodyHeight = 0f;
        // cardHeight 是收紧后的卡高，下限保证标题、正文与关闭按钮三块不重叠。
        float cardHeight = CardTopArea + bodyHeight + bottomArea;
        if (cardHeight < CardMinHeight) cardHeight = CardMinHeight;
        if (cardHeight > CardMaxHeight) cardHeight = CardMaxHeight;
        RectTransform cardRect = _infoLayer.transform as RectTransform;
        if (cardRect != null) cardRect.sizeDelta = new Vector2(CardWidth, cardHeight);
        if (_infoLayerScroll != null)
        {
            // scrollHeight 是正文区高度，卡高收紧后同步缩短，底部不会留出空白。
            float scrollHeight = cardHeight - CardTopArea - bottomArea;
            _infoLayerScroll.sizeDelta = new Vector2(CardBodyWidth, scrollHeight);
        }
        // buttonY 是三按钮行的顶部纵坐标，按卡高从卡底内边距倒推，不会落在卡外。
        float buttonY = CardBottomGap + CardButtonHeight - cardHeight;
        SetCardButtonY(_cardDeductButton, buttonY);
        SetCardButtonY(_cardSourceButton, buttonY);
        SetCardButtonY(_cardRemoveButton, buttonY);
    }

    /// <summary>只改说明卡底部按钮的纵坐标，横向排布保持预制体原值。</summary>
    /// <param name="button">说明卡底部的任一个入口按钮。</param>
    /// <param name="y">相对卡顶的纵坐标，按卡高倒推得到。</param>
    private static void SetCardButtonY(Button button, float y)
    {
        RectTransform rect = button.transform as RectTransform;
        if (rect == null) return;
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
    }

    /// <summary>按是否特技卡切换底部三个特技入口的显隐。</summary>
    /// <param name="visible">true 表示当前是特技卡，显示兑换、拓源与特技卸下。</param>
    private void ApplyCardSkillButtons(bool visible)
    {
        _cardDeductButton.gameObject.SetActive(visible);
        _cardSourceButton.gameObject.SetActive(visible);
        _cardRemoveButton.gameObject.SetActive(visible);
        // 使用按钮和状态只在具体特技侧卡打开时按槽位设置，普通说明卡统一隐藏。
        _cardUseButton.gameObject.SetActive(false);
        _cardStateText.gameObject.SetActive(false);
    }

    /// <summary>兑换入口只提示暂未开放，不请求任何养成数据。</summary>
    private void OnCardDeductClicked() { TipsMgr.Instance.ShowSystemTips("兑换：暂未开放"); }
    /// <summary>拓源入口只提示暂未开放，不请求任何养成数据。</summary>
    private void OnCardSourceClicked() { TipsMgr.Instance.ShowSystemTips("拓源：暂未开放"); }
    /// <summary>特技卸下入口只提示暂未开放，不实际卸下特技。</summary>
    private void OnCardRemoveClicked() { TipsMgr.Instance.ShowSystemTips("特技卸下：暂未开放"); }

    /// <summary>特技行末尾的详情入口在说明层展示该槽自己的完整说明，不共用首个槽。</summary>
    /// <param name="index">被点击的特技槽下标，范围0至2。</param>
    private void OnSpecialSkillDetailClicked(int index)
    {
        // card 是本槽说明卡正文，标题与当前层/下一层分区都由控制器按真实配置生成。
        string card = _specialSkillCards[index];
        if (string.IsNullOrEmpty(card))
        {
            // 控制器没给出分区正文时退回到原来的完整说明，避免说明层空白。
            card = _specialSkillDetails[index];
        }
        if (string.IsNullOrEmpty(card))
        {
            card = _specialSkillTexts[index].text;
        }
        _currentCardSkillSlot = index;
        _infoLayerTitleText.text = _specialSkillCardTitles[index];
        _infoLayerText.text = card;
        // 特技卡保留兑换、拓源与特技卸下三个真实入口，均只给未开放提示。
        ApplyCardSkillButtons(true);
        _cardUseButton.gameObject.SetActive(_canUseSpecialSkills[index]);
        _cardStateText.text = _specialSkillCardStates[index];
        _cardStateText.gameObject.SetActive(!_canUseSpecialSkills[index]);
        _infoLayer.gameObject.SetActive(true);
        // 特技卡正文按内容撑高到 452 上限，超出部分在卡内滚动，三按钮固定贴卡底。
        ApplyCardLayout(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_infoLayer.transform as RectTransform);
    }

    /// <summary>把三个特技槽的详情正文缓存下来，供各行末尾入口按槽位取用。</summary>
    /// <param name="data">控制器生成的纯展示数据。</param>
    private void CacheSpecialSkillDetails(EquipmentDetailViewData data)
    {
        // index 是特技槽下标，三槽说明互相独立。
        for (int index = 0; index < 3; index++)
        {
            _specialSkillDetails[index] = data.SpecialSkillTexts[index];
            // 说明卡正文单独缓存，空串时点击入口会退回完整说明，不会出现空白卡。
            _specialSkillCards[index] = data.SpecialSkillCards[index];
            _specialSkillCardTitles[index] = data.SpecialSkillCardTitles[index];
            _specialSkillCardStates[index] = data.SpecialSkillCardStates[index];
            _canUseSpecialSkills[index] = data.CanActivateSpecialSkill[index];
        }
    }

    /// <summary>按真实宝石孔数量显示空槽图标与展开区孔位格，其余隐藏。</summary>
    private void ApplyGemSlots()
    {
        // index 是空槽图标下标，只显示下标小于真实孔数的那些。
        for (int index = 0; index < _gemSlots.Length; index++)
        {
            _gemSlots[index].gameObject.SetActive(index < _gemSlotCount);
        }
        if (_gemDropPanel != null) _gemDropPanel.gameObject.SetActive(false);
        // index 是展开区孔位格下标，容量固定 6 个，只有真实孔位对应的格子才启用。
        for (int index = 0; index < _gemCells.Length; index++)
        {
            _gemCells[index].SetActive(index < _gemSlotCount);
        }
        // rows 是真实孔数按两列排布后的可见行数，两孔只占一行，零孔时不占高度。
        int rows = (_gemSlotCount + 1) / 2;
        LayoutGemCells(rows);
    }

    /// <summary>按可见行数排布孔位格并回收网格高度，两列间距取网格可用宽度的一半。</summary>
    /// <param name="rows">当前可见行数，由真实孔数按两列推导。</param>
    private void LayoutGemCells(int rows)
    {
        if (_gemSlotGrid == null) return;
        // gridElement 是网格容器的布局项，高度由行数推导，父纵向组据此回收空位。
        LayoutElement gridElement = _gemSlotGrid.GetComponent<LayoutElement>();
        if (gridElement != null)
        {
            gridElement.preferredHeight = rows * GemCellRowStep + 4f;
        }
        // columnOffset 是第二列的左边距，取网格宽度的一半，两列不会挤在左上角。
        float columnOffset = _gemSlotGrid.rect.width * 0.5f;
        if (columnOffset < GemCellSize * 3f)
        {
            // 网格尚未完成布局时宽度可能是默认值或 0，此时用主栏半宽兜底，避免两列重叠。
            columnOffset = 152f;
        }
        // index 是孔位格下标，只对已启用且属于真实孔位的格子排坐标。
        for (int index = 0; index < _gemCells.Length; index++)
        {
            if (!_gemCells[index].activeSelf) continue;
            // column 是列序 0 或 1，row 是行序，格子从左上角向右向下排。
            int column = index % 2;
            int row = index / 2;
            RectTransform cellRect = _gemCells[index].transform as RectTransform;
            if (cellRect == null) continue;
            // x 是格子左边界：第一列贴左内边距，第二列右移半栏宽。
            float x = 8f;
            if (column == 1) x = columnOffset;
            cellRect.anchoredPosition = new Vector2(x, -(2f + row * GemCellRowStep));
        }
    }

    /// <summary>按根画布实际缩放补偿本窗口显示尺度：1920 保持原观感，1366 放大到可读。</summary>
    private void ApplyDisplayScale()
    {
        // rootCanvas 是本窗口所属根画布，scaleFactor 由全局 CanvasScaler 按屏幕分辨率算出。
        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null) return;
        // canvasScale 是当前屏幕下的真实缩放，1920 下为 1，1366 下约 0.71。
        float canvasScale = rootCanvas.scaleFactor;
        // windowScale 是只作用于详情窗口的局部缩放，下限为 1，1920 下不放大。
        float windowScale = 1f;
        if (canvasScale > 0.0001f && canvasScale < MinScreenScale)
        {
            // 补偿到最小屏幕缩放，主窗口与左侧说明卡同属本 root，组合仍在 1366 屏内。
            windowScale = MinScreenScale / canvasScale;
        }
        transform.localScale = new Vector3(windowScale, windowScale, 1f);
    }

    /// <summary>淬锋行折叠按钮切换行下方属性正文区，摘要行始终同行可见。</summary>
    private void OnForgeFoldClicked()
    {
        // 折叠只隐藏下方正文区，摘要行与按钮位置不动，父布局按内容高度回收空位。
        _forgeRowCollapsed = !_forgeRowCollapsed;
        _forgeExpandPanel.SetActive(!_forgeRowCollapsed);
        if (_forgeRowCollapsed)
        {
            SetButtonLabel(_forgeFoldButton, FoldExpandText);
        }
        else
        {
            SetButtonLabel(_forgeFoldButton, FoldCollapseText);
        }
    }

    /// <summary>默认折叠全部分区展开区，切装备或重开窗口时回到初始状态。</summary>
    private void ApplyExpandPanels()
    {
        // 淬锋折叠标志与面板显隐必须同源：默认折叠时标志为 true，首次点击才能真正展开。
        _forgeRowCollapsed = true;
        _gemExpandPanel.SetActive(false);
        _setExpandPanel.SetActive(false);
        _forgeExpandPanel.SetActive(false);
        SetButtonLabel(_gemDropButton, FoldExpandText);
        SetButtonLabel(_setViewButton, FoldExpandText);
        SetButtonLabel(_forgeFoldButton, FoldExpandText);
    }

    /// <summary>镶嵌摘要行末按钮展开或收起两列孔位区，折叠时隐藏内容并回收高度。</summary>
    private void OnGemDropClicked()
    {
        // visible 是切换后镶嵌展开区是否显示，按钮文案随真显隐状态改写。
        bool visible = !_gemExpandPanel.activeSelf;
        _gemExpandPanel.SetActive(visible);
        if (visible)
        {
            SetButtonLabel(_gemDropButton, FoldCollapseText);
        }
        else
        {
            SetButtonLabel(_gemDropButton, FoldExpandText);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(_gemExpandPanel.transform.parent as RectTransform);
        // 展开后按真实半栏宽重排两列孔位格，隐藏态下拿不到网格宽度。
        if (visible) ApplyGemSlots();
    }

    /// <summary>切换套装行下方六个部位穿戴状态的显隐。</summary>
    private void OnSetViewClicked()
    {
        // visible 是切换后套装展开区是否显示，按钮文案与真显隐一致。
        bool visible = !_setExpandPanel.activeSelf;
        _setExpandPanel.SetActive(visible);
        if (visible)
        {
            SetButtonLabel(_setViewButton, FoldCollapseText);
        }
        else
        {
            SetButtonLabel(_setViewButton, FoldExpandText);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(_setExpandPanel.transform.parent as RectTransform);
    }

    /// <summary>铭缀行末查看入口只给未开放提示，不请求铭缀数据。</summary>
    private void OnInscribeRowClicked()
    {
        TipsMgr.Instance.ShowSystemTips("铭缀：暂未开放");
    }

    /// <summary>洗练行末查看入口只给未开放提示，不请求洗练数据。</summary>
    private void OnWashRowClicked()
    {
        TipsMgr.Instance.ShowSystemTips("洗练：暂未开放");
    }

    /// <summary>外观行末的查看入口只提示暂未开放，不请求外观数据、不发协议。</summary>
    private void OnAppearanceViewClicked()
    {
        TipsMgr.Instance.ShowSystemTips("外观：暂未开放");
    }

    /// <summary>三个右侧主按钮共用提示，按下标取准确名称。</summary>
    private void OnMainButtonFirst() { ShowUnavailable(0); }
    /// <summary>铭缀主按钮提示。</summary>
    private void OnMainButtonSecond() { ShowUnavailable(1); }
    /// <summary>熔锻主按钮提示。</summary>
    private void OnMainButtonThird() { ShowUnavailable(2); }

    /// <summary>按主按钮下标显示准确名称与暂未开放提示。</summary>
    /// <param name="index">主按钮下标，范围0至2。</param>
    private void ShowUnavailable(int index)
    {
        TipsMgr.Instance.ShowSystemTips(MainButtonNames[index] + "：暂未开放");
    }

    /// <summary>更多按钮真实切换下拉菜单显隐并改写按钮标签。</summary>
    private void OnMoreClicked()
    {
        // visible 是切换后更多菜单是否显示，打开提示收起、关闭回到原始名称「更多」。
        bool visible = !_moreMenu.gameObject.activeSelf;
        _moreMenu.gameObject.SetActive(visible);
        if (visible)
        {
            SetButtonLabel(_moreButton, FoldCollapseText);
        }
        else
        {
            SetButtonLabel(_moreButton, MoreText);
        }
    }

    /// <summary>更多菜单项按准确名称提示暂未开放，不发业务协议。</summary>
    /// <param name="index">菜单项下标，范围0至3。</param>
    private void OnMoreMenuClicked(int index)
    {
        TipsMgr.Instance.ShowSystemTips(MoreMenuNames[index] + "：暂未开放");
    }

    /// <summary>底栏展开真实切换附加容器显隐并改写按钮标签。</summary>
    private void OnFooterExpandClicked()
    {
        // visible 是切换后底栏附加容器是否显示，按钮标签随状态改写。
        bool visible = !_footerExtra.gameObject.activeSelf;
        _footerExtra.gameObject.SetActive(visible);
        if (visible)
        {
            SetButtonLabel(_footerExpandButton, FoldCollapseText);
        }
        else
        {
            SetButtonLabel(_footerExpandButton, FoldExpandText);
        }
    }

    /// <summary>说明层关闭按钮隐藏本层，不关闭整个详情窗口。</summary>
    private void OnInfoLayerClosed()
    {
        _infoLayer.gameObject.SetActive(false);
    }
    #endregion
}
