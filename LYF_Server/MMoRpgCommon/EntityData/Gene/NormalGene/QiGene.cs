/// <summary>基础气属性运行基因，按根实例层数提供固定值或百分比修正。</summary>
public class QiGene : AttributeGeneBase
{
    /// <summary>此节点只修正基础气属性，不会改变其他属性。</summary>
    public override GeneAttributeType Attribute { get { return GeneAttributeType.Qi; } }
}
