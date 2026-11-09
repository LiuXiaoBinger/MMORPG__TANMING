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
    /// <summary>三个实例词条的独立说明，空词条显示暂无额外词条。</summary>
    public string Traits;
    /// <summary>三个保存特技槽的名称、等级、状态与效果说明。</summary>
    public string[] SpecialSkillTexts = new string[3];
    /// <summary>槽有特技且不是当前启用槽时允许提交切换。</summary>
    public bool[] CanActivateSpecialSkill = new bool[3];
    /// <summary>服务器当前是否将该实例放在穿戴栏。</summary>
    public bool IsWorn;
}
