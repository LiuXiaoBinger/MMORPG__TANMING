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
    /// <summary>实例词条分区中的正文。</summary>
    [SerializeField] private TMP_Text _traitsText;
    /// <summary>只滚动中间属性、词条和特技，顶部和底部按钮保持固定。</summary>
    [SerializeField] private ScrollRect _scroll;
    /// <summary>预制体中按槽0、1、2排列的特技说明。</summary>
    [SerializeField] private TMP_Text[] _specialSkillTexts;
    /// <summary>对应三个特技槽的启用按钮，引用保存在正式预制体。</summary>
    [SerializeField] private Button[] _activateButtons;
    /// <summary>未穿戴时显示的穿戴按钮。</summary>
    [SerializeField] private Button _wearButton;
    /// <summary>已穿戴时显示的卸下按钮。</summary>
    [SerializeField] private Button _takeOffButton;
    /// <summary>只关闭本窗口的按钮。</summary>
    [SerializeField] private Button _closeButton;
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
    /// <summary>三个特技行末的详情入口按钮，按槽位顺序对应三个特技说明。</summary>
    [SerializeField] private Button[] _specialSkillDetailButtons;
    /// <summary>外观独立行，无实例数据时显示中性占位。</summary>
    [SerializeField] private TMP_Text _appearanceRowText;
    /// <summary>祭甲与甲片独立行，无实例数据时整行中性占位。</summary>
    [SerializeField] private TMP_Text _garnishRowText;
    /// <summary>套装总阶与件数独立行，无实例数据时整行中性占位。</summary>
    [SerializeField] private TMP_Text _setRowText;
    /// <summary>套装行末尾的查看入口，只提示暂未开放，不请求套装数据。</summary>
    [SerializeField] private Button _setViewButton;
    /// <summary>外观行末尾的查看入口，只提示暂未开放，不请求外观数据。</summary>
    [SerializeField] private Button _appearanceViewButton;
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
    /// <summary>说明层关闭按钮。</summary>
    [SerializeField] private Button _infoLayerCloseButton;
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
    /// <summary>三个特技槽各自的完整说明，供每行末尾的详情入口按槽位取用。</summary>
    private readonly string[] _specialSkillDetails = new string[3];

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
        _closeButton.onClick.AddListener(CloseWindow);
        _activateButtons[0].onClick.AddListener(OnActivateFirst);
        _activateButtons[1].onClick.AddListener(OnActivateSecond);
        _activateButtons[2].onClick.AddListener(OnActivateThird);
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
        _strengthenButton.onClick.AddListener(OnMainButtonFirst);
        _inscribeButton.onClick.AddListener(OnMainButtonSecond);
        _forgeButton.onClick.AddListener(OnMainButtonThird);
        _moreButton.onClick.AddListener(OnMoreClicked);
        _footerExpandButton.onClick.AddListener(OnFooterExpandClicked);
        _infoLayerCloseButton.onClick.AddListener(OnInfoLayerClosed);
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
        _traitsText.text = data.Traits;
        _headerMetaText.text = data.MetaText;
        _gemDropText.text = data.GemDropText;
        _footerText.text = data.FooterText;
        _footerExtraText.text = data.FooterExtraText;
        _infoLayerText.text = data.InfoLayerText;
        _equipmentInfoText = data.InfoLayerText;
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
        // 三个固定槽按服务器保存顺序渲染，空槽按钮停用；index 是特技槽下标。
        for (int index = 0; index < 3; index++)
        {
            // 首行只显示槽位、名称、等级与状态；完整说明留给详情层，不在行内换行溢出。
            _specialSkillTexts[index].text = data.SpecialSkillHeadlines[index];
            _activateButtons[index].interactable = data.CanActivateSpecialSkill[index];
        }
        // 打开新装备时把新增交互状态复位，避免上一次的展开状态残留。
        // gemSlotCount 是当前装备真实宝石孔数量，SetFoldableRows 按它显示空槽图标。
        int gemSlotCount = data.GemSlotCount;
        _gemSlotCount = gemSlotCount;
        CacheSpecialSkillDetails(data);
        ApplyGemSlots();
        // 正文分区已改为滚动区内的自然排列，不再有总折叠与固定高度空壳。
        // 每次打开（含关闭重开同武器）都从展开态复位，避免上次折叠状态残留。
        _forgeRowCollapsed = false;
        ApplyForgeRowState();
        SetButtonLabel(_footerExpandButton, FoldExpandText);
        SetButtonLabel(_moreButton, MoreText);
        _gemDropPanel.gameObject.SetActive(false);
        _moreMenu.gameObject.SetActive(false);
        _footerExtra.gameObject.SetActive(false);
        _infoLayer.gameObject.SetActive(false);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_scroll.content);
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
    /// <summary>转交第一个保存特技槽的启用请求。</summary>
    private void OnActivateFirst() { SpecialSkillRequested?.Invoke(0); }
    /// <summary>转交第二个保存特技槽的启用请求。</summary>
    private void OnActivateSecond() { SpecialSkillRequested?.Invoke(1); }
    /// <summary>转交第三个保存特技槽的启用请求。</summary>
    private void OnActivateThird() { SpecialSkillRequested?.Invoke(2); }
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
        _infoLayerText.text = _equipmentInfoText;
        _infoLayer.gameObject.SetActive(true);
    }

    /// <summary>特技行末尾的详情入口在说明层展示该槽自己的完整说明，不共用首个槽。</summary>
    /// <param name="index">被点击的特技槽下标，范围0至2。</param>
    private void OnSpecialSkillDetailClicked(int index)
    {
        // text 取本槽缓存下来的完整说明；标题行只有两行摘要，不能当详情。
        string text = _specialSkillDetails[index];
        if (string.IsNullOrEmpty(text))
        {
            text = _specialSkillTexts[index].text;
        }
        _infoLayerText.text = text;
        _infoLayer.gameObject.SetActive(true);
    }

    /// <summary>把三个特技槽的详情正文缓存下来，供各行末尾入口按槽位取用。</summary>
    /// <param name="data">控制器生成的纯展示数据。</param>
    private void CacheSpecialSkillDetails(EquipmentDetailViewData data)
    {
        // index 是特技槽下标，三槽说明互相独立。
        for (int index = 0; index < 3; index++)
        {
            _specialSkillDetails[index] = data.SpecialSkillTexts[index];
        }
    }

    /// <summary>按真实宝石孔数量显示空槽图标，其余隐藏；正文其余行常驻可见。</summary>
    private void ApplyGemSlots()
    {
        // index 是空槽图标下标，只显示下标小于真实孔数的那些。
        for (int index = 0; index < _gemSlots.Length; index++)
        {
            _gemSlots[index].gameObject.SetActive(index < _gemSlotCount);
        }
        if (_gemDropPanel != null) _gemDropPanel.gameObject.SetActive(false);
    }

    /// <summary>淬锋与铭缀行折叠按钮只切换该行显隐，不改其它分区。</summary>
    private void OnForgeFoldClicked()
    {
        // 折叠状态自己保存：正文只清透明不隐藏，横向布局里的占位仍保留，按钮不会被挤到行首。
        _forgeRowCollapsed = !_forgeRowCollapsed;
        ApplyForgeRowState();
    }

    /// <summary>按当前折叠状态刷新淬锋正文可见性与按钮标签，正文始终保留布局占位。</summary>
    private void ApplyForgeRowState()
    {
        _forgeRowText.gameObject.SetActive(true);
        if (_forgeRowCollapsed)
        {
            _forgeRowText.alpha = 0f;
            SetButtonLabel(_forgeFoldButton, FoldExpandText);
        }
        else
        {
            _forgeRowText.alpha = 1f;
            SetButtonLabel(_forgeFoldButton, FoldCollapseText);
        }
    }

    /// <summary>镶嵌下拉真实切换占位容器显隐。</summary>
    private void OnGemDropClicked()
    {
        // visible 是切换后镶嵌下拉是否显示，显隐本身就是点击反馈。
        bool visible = !_gemDropPanel.gameObject.activeSelf;
        _gemDropPanel.gameObject.SetActive(visible);
    }

    /// <summary>套装行末的查看入口只提示暂未开放，不请求套装数据、不发协议。</summary>
    private void OnSetViewClicked()
    {
        TipsMgr.Instance.ShowSystemTips("套装：暂未开放");
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
