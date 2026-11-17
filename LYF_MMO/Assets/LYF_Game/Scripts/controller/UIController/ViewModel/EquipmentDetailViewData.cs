/// <summary>装备详情展示数据，已由控制器合并静态配置与服务器实例字段。</summary>
public sealed class EquipmentDetailViewData
{
    /// <summary>装备名称及强化等级的展示正文。</summary>
    public string Name;
    /// <summary>装备图标在既有UISprites目录中的资源路径。</summary>
    public string IconPath;
    /// <summary>中文装备部位，固定显示在标题下方。</summary>
    public string PartName;
    /// <summary>基础属性名称，每行对应AttributeValues中的同一行数值。</summary>
    public string Attributes;
    /// <summary>基础属性展示值，保留配置的百分数语义，不参与计算。</summary>
    public string AttributeValues;
    /// <summary>三个保存特技槽的名称、等级、状态与效果说明。</summary>
    public string[] SpecialSkillTexts = new string[3];
    /// <summary>三个特技槽的独立说明卡正文，按标题、当前层效果、下一层效果、同名装备说明分区。</summary>
    public string[] SpecialSkillCards = new string[3];
    /// <summary>三个特技侧卡的标题，逐槽保存特技名与当前展示等级。</summary>
    public string[] SpecialSkillCardTitles = new string[3];
    /// <summary>三个特技侧卡的只读状态，区分当前生效、已选未穿、未使用与未镶嵌。</summary>
    public string[] SpecialSkillCardStates = new string[3];
    /// <summary>三个特技槽的首行摘要，逐行显示槽位、名称、等级与启用状态。</summary>
    public string[] SpecialSkillHeadlines = new string[3];
    /// <summary>槽内没有任何特技族 ID 时为空槽，用于区分「空槽」与「已选但不可切换」的显示。</summary>
    public bool[] IsSpecialSkillEmpty = new bool[3];
    /// <summary>该槽已开孔且不是当前启用槽时允许提交切换；未开孔或已启用时为 false。</summary>
    public bool[] CanActivateSpecialSkill = new bool[3];
    /// <summary>该槽正好是下一个可开启的孔时允许提交开孔请求，其余槽位为 false。</summary>
    public bool[] CanOpenSpecialSkill = new bool[3];
    /// <summary>服务器下发的已开孔数量，取值 0 到 3，与六个宝石孔无关。</summary>
    public int OpenedSpecialSkillCount;
    /// <summary>开孔费用说明文本，与服务端扣费使用同一个常量，避免两边价格不一致。</summary>
    public string SpecialSkillOpenCostText;
    /// <summary>服务器当前是否将该实例放在穿戴栏。</summary>
    public bool IsWorn;
    /// <summary>头部元信息两行：装备状态与部位一行，需求等级与占位一行，宽度 270 内不换行。</summary>
    public string MetaText;
    /// <summary>底栏交易状态与耐久，两者都没有真实字段，固定中性占位。</summary>
    public string FooterText;
    /// <summary>信息按钮说明层正文，展示装备静态说明与获取途径。</summary>
    public string InfoLayerText;
    public string InscribeRowText;
    public string WashRowText;
    public string GemInfoText;
    /// <summary>按六个固定套装部位排列的穿戴状态，每行两个部位。</summary>
    public string SetExpandText;
    public string ForgeExpandText;
    /// <summary>属性说明卡正文：逐行写当前属性名与数值，末行标注基础/强化/进阶贡献来源。</summary>
    public string AttributeCardText;
    /// <summary>底栏展开后的单行短说明，只放交易与耐久的中性占位，不重复说明层正文。</summary>
    public string FooterExtraText;
    /// <summary>镶嵌下拉展开后的空槽中性占位文本。</summary>
    public string GemDropText;
    /// <summary>静态配置里的宝石孔数量，用于决定空槽图标显示几个。</summary>
    public int GemSlotCount;
    /// <summary>强化与套装摘要独立行，单行短文本，强化等级与上限来自真实数据。</summary>
    public string EnhanceRowText;
    /// <summary>淬锋与铭缀独立折叠行，单行短文本，无实例数据统一中性占位。</summary>
    public string ForgeRowText;
    /// <summary>特技独立行的短标题，只写特技编号，完整说明由特技卡与说明层承载。</summary>
    public string FeatureRowText;
    /// <summary>外观独立行，单行短文本，不重复其它分区内容。</summary>
    public string AppearanceRowText;
    /// <summary>祭甲与甲片独立行，没有实例字段时整行中性占位。</summary>
    public string GarnishRowText;
    /// <summary>套装摘要，显示角色已穿戴的指定部位件数，分母固定为六。</summary>
    public string SetRowText;
    /// <summary>基础属性左列名称，三行一列，行号与左列数值一致。</summary>
    public string AttributeLeftNames;
    /// <summary>基础属性左列数值，保留配置的百分数语义，不参与计算。</summary>
    public string AttributeLeftValues;
    /// <summary>基础属性右列名称，三行一列，行号与右列数值一致。</summary>
    public string AttributeRightNames;
    /// <summary>基础属性右列数值，保留配置的百分数语义，不参与计算。</summary>
    public string AttributeRightValues;
    /// <summary>图标左上角状态角标文字，只由服务器穿戴状态派生为装备中或未装备。</summary>
    public string WornBadgeText;
    /// <summary>头部右上品质行，没有真实品质字段时用中性占位。</summary>
    public string HeaderPartText;
    /// <summary>头部右上绑定位，没有真实绑定字段时用中性占位。</summary>
    public string HeaderBindText;
}
