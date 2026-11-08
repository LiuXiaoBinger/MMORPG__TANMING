/// <summary>无咎点数运行基因，按根实例层数提供固定值或百分比修正。</summary>
public class WuGuGene : AttributeGeneBase
{
    /// <summary>此节点只修正无咎点数，不会改变其他属性。</summary>
    public override GeneAttributeType Attribute { get { return GeneAttributeType.WuGu; } }
}
