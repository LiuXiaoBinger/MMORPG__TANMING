/// <summary>当前生命值基因，装备词条对该属性的增量记录。</summary>
public class CurrHPGene : GeneBase
{
    /// <summary>本词条提供的当前生命值增量；这里只保存数据，不在此计算角色最终属性。</summary>
    public int Value { get; set; }
}
