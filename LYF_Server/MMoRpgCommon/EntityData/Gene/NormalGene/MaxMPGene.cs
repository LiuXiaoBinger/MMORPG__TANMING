/// <summary>最大法力值运行基因，按根实例层数提供固定值或百分比修正。</summary>
public class MaxMPGene : AttributeGeneBase
{
    /// <summary>此节点只修正最大法力值，不会改变其他属性。</summary>
    public override GeneAttributeType Attribute { get { return GeneAttributeType.MaxMp; } }
}
