/// <summary>按毫秒间隔产生法力恢复的具体运行节点。</summary>
public class PeriodicManaGene : TimeBuffGeneBase
{
    /// <summary>周期结果交由宿主执行法力恢复。</summary>
    public override GeneEffectType EffectType { get { return GeneEffectType.PeriodicMana; } }
}
