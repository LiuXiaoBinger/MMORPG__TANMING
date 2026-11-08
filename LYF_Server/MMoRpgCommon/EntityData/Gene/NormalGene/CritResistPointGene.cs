/// <summary>暴击抵抗运行基因，按根实例层数提供固定值或百分比修正。</summary>
public class CritResistPointGene : AttributeGeneBase
{
    /// <summary>此节点只修正暴击抵抗，不会改变其他属性。</summary>
    public override GeneAttributeType Attribute { get { return GeneAttributeType.CritResistPoint; } }
}
