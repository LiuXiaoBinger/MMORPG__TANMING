/// <summary>治疗增益百分比基因，装备词条对该属性的增量记录。</summary>
public class HealBonusGene : GeneBase
{
    /// <summary>本词条提供的治疗增益百分比增量；这里只保存数据，不在此计算角色最终属性。</summary>
    public float Value { get; set; }
}
