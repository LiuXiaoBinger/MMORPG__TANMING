/// <summary>属性基因的公共行为；具体属性类固定属性语义，数值和组合树属于本次运行节点。</summary>
public abstract class AttributeGeneBase : GeneBase
{
    /// <summary>当前具体属性类负责修正的属性。</summary>
    public abstract GeneAttributeType Attribute { get; }
    /// <summary>所有具体属性节点都采用属性修正分类。</summary>
    public override GeneEffectType EffectType { get { return GeneEffectType.Attribute; } }

}
