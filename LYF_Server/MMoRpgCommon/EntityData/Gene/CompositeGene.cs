/// <summary>只组合子基因的节点，自身负责根寿命与场景约束，不额外贡献效果。</summary>
public class CompositeGene : GeneBase
{
    /// <summary>纯组合类型固定为 Composite。</summary>
    public override GeneEffectType EffectType { get { return GeneEffectType.Composite; } }
    /// <summary>活跃列表只加入组合子项，保留当前节点作为它们的祖先。</summary>
    public override bool HasOwnEffect { get { return false; } }
}
