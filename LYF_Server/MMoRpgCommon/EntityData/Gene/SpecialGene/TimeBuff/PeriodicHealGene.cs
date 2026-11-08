/// <summary>按毫秒间隔产生生命恢复的具体运行节点。</summary>
public class PeriodicHealGene : TimeBuffGeneBase
{
    /// <summary>周期结果交由宿主执行生命恢复。</summary>
    public override GeneEffectType EffectType { get { return GeneEffectType.PeriodicHeal; } }
}
