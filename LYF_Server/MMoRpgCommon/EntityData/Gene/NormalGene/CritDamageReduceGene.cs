/// <summary>暴击伤害减免百分比基因，装备词条对该属性的增量记录。</summary>
public class CritDamageReduceGene : GeneBase
{
    /// <summary>本词条提供的暴击伤害减免百分比增量；这里只保存数据，不在此计算角色最终属性。</summary>
    public float Value { get; set; }
}
