/// <summary>内功防御变动运行基因，按根实例层数提供固定值或百分比修正。</summary>
public class DefInternalDeltaGene : AttributeGeneBase
{
    /// <summary>此节点只修正内功防御变动，不会改变其他属性。</summary>
    public override GeneAttributeType Attribute { get { return GeneAttributeType.DefInternalDelta; } }
}
