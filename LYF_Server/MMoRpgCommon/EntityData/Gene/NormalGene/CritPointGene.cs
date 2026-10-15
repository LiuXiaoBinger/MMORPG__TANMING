/// <summary>暴击属性值基因，装备词条对该属性的增量记录。</summary>
public class CritPointGene : GeneBase
{
    /// <summary>该词条的属性数值，语义与 RoleBaseAttrEntity 对应字段一致。</summary>
    public int Value { get; set; }
}
