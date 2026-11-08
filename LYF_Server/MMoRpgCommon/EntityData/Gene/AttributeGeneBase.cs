/// <summary>属性基因的公共行为；具体属性类固定属性语义，数值和组合树属于本次运行节点。</summary>
public abstract class AttributeGeneBase : GeneBase
{
    /// <summary>当前具体属性类负责修正的属性。</summary>
    public abstract GeneAttributeType Attribute { get; }
    /// <summary>所有具体属性节点都采用属性修正分类。</summary>
    public override GeneEffectType EffectType { get { return GeneEffectType.Attribute; } }

    /// <summary>只对本类型负责的属性累计修正，同类百分比按加法合并。</summary>
    /// <param name="attribute">本次查询的属性。</param>
    /// <param name="stacks">根实例的当前层数。</param>
    /// <param name="flat">固定修正的累计点数。</param>
    /// <param name="percent">百分比修正的累计百分数。</param>
    public override void AccumulateAttribute(GeneAttributeType attribute, int stacks, ref double flat, ref double percent)
    {
        if (Attribute != attribute) return;
        if (ValueType == 1) percent += Value * stacks;
        else flat += Value * stacks;
    }
}
