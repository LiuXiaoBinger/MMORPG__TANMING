/// <summary>只组合子基因的节点，自身负责根寿命与场景约束，不额外贡献效果。</summary>
public class CompositeGene : GeneBase
{
    /// <summary>纯组合类型固定为 Composite。</summary>
    public override GeneEffectType EffectType { get { return GeneEffectType.Composite; } }
    /// <summary>活跃列表只加入组合子项，保留当前节点作为它们的祖先。</summary>
    public override bool HasOwnEffect { get { return false; } }
    /// <summary>组合本身不重复改数值，关联的具体节点分别执行各自的 OnCall。</summary>
    /// <param name="role">组合所属角色。</param>
    /// <param name="args">组合本身不消费事件。</param>
    /// <returns>组合本体无额外效果，返回 0。</returns>
    public override int OnCall(IGeneOwner role, GeneTriggerArgs args = default(GeneTriggerArgs)) { return 0; }
}
