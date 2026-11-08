/// <summary>持续伤害基类，复用周期推进并产生生命伤害脉冲；实际扣血由宿主完成。</summary>
public abstract class DamageBuffGeneBase : TimeBuffGeneBase
{
    /// <summary>持续伤害按生命上限或固定生命点结算。</summary>
    public override GeneEffectType EffectType { get { return GeneEffectType.PeriodicDamage; } }
}
