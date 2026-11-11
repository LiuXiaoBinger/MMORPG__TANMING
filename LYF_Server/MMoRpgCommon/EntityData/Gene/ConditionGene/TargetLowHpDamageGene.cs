/// <summary>目标生命低于阈值时提高本次伤害的常驻节点，对应「对残血目标增伤」。</summary>
public class TargetLowHpDamageGene : ConditionDamageGeneBase
{
    /// <summary>条件增伤分类。</summary>
    public override GeneEffectType EffectType { get { return GeneEffectType.TargetLowHpDamage; } }

    /// <summary>目标扣血前生命比例严格低于 Condition 时，返回配置的增伤百分点。</summary>
    /// <param name="context">结算前的真实双方资源。</param>
    /// <returns>满足条件返回按层数换算的百分点，否则返回零。</returns>
    public override double ResolveBonus(GeneDamageContext context)
    {
        if (context.TargetHpPercent >= Condition) return 0;
        return Value * StackCount;
    }
}
