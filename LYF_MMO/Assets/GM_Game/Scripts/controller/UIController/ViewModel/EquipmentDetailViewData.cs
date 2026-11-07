/// <summary>装备详情展示数据，已由控制器合并静态配置与服务器实例字段。</summary>
public sealed class EquipmentDetailViewData
{
    /// <summary>装备名称及强化等级的展示正文。</summary>
    public string Name;
    /// <summary>六项基础基因和三项实例词条的说明，保留各自来源。</summary>
    public string Attributes;
    /// <summary>三个保存特技槽的名称、等级、状态与效果说明。</summary>
    public string[] SpecialSkillTexts = new string[3];
    /// <summary>槽有特技且不是当前启用槽时允许提交切换。</summary>
    public bool[] CanActivateSpecialSkill = new bool[3];
    /// <summary>服务器当前是否将该实例放在穿戴栏。</summary>
    public bool IsWorn;
}
