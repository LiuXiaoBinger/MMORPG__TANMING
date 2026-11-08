/// <summary>在根寿命内持续提供动作限制状态的运行节点。</summary>
public class StateGene : GeneBase
{
    /// <summary>此节点提供的眩晕、定身、沉默或霸体位。</summary>
    public GeneState States { get; set; }
    /// <summary>状态查询由此具体节点实现。</summary>
    public override GeneEffectType EffectType { get { return GeneEffectType.State; } }

    /// <summary>返回此节点贡献的原始状态；霸体冲突由宿主合并后统一处理。</summary>
    /// <returns>工厂复制的持续状态位。</returns>
    public override GeneState GetStates() { return States; }
}
