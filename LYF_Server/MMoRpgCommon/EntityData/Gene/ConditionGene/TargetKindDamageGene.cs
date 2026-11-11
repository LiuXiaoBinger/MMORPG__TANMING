/// <summary>按目标真实类别（首领或普通怪）提高本次伤害的常驻节点。</summary>
public class TargetKindDamageGene : ConditionDamageGeneBase
{
    /// <summary>条件增伤分类。</summary>
    public override GeneEffectType EffectType { get { return GeneEffectType.TargetKindDamage; } }

    /// <summary>Condition 为 1 只对首领生效，为 2 只对非首领目标生效，其他值不生效。</summary>
    /// <param name="context">结算前的真实双方资源与目标类别。</param>
    /// <returns>类别匹配返回按层数换算的百分点，否则返回零。</returns>
    public override double ResolveBonus(GeneDamageContext context)
    {
        if (Condition == 1 && !context.TargetIsBoss) return 0;
        if (Condition == 2 && context.TargetIsBoss) return 0;
        if (Condition != 1 && Condition != 2) return 0;
        return Value * StackCount;
    }
}
