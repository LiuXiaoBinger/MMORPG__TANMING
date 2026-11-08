/// <summary>最大生命值运行基因，按根实例层数提供固定值或百分比修正。</summary>
public class MaxHPGene : AttributeGeneBase
{
    /// <summary>此节点只修正最大生命值，不会改变其他属性。</summary>
    public override GeneAttributeType Attribute { get { return GeneAttributeType.MaxHp; } }
}
