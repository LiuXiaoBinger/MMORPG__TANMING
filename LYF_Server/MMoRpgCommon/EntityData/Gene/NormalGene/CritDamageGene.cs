/// <summary>暴击伤害百分数运行基因，按根实例层数提供固定值或百分比修正。</summary>
public class CritDamageGene : AttributeGeneBase
{
    /// <summary>此节点只修正暴击伤害百分数，不会改变其他属性。</summary>
    public override GeneAttributeType Attribute { get { return GeneAttributeType.CritDamage; } }
}
