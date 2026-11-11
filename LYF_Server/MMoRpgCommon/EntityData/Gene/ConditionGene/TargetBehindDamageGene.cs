/// <summary>从目标背后命中时提高本次伤害的常驻节点；背面判定由权威结算方按真实朝向给出。</summary>
public class TargetBehindDamageGene : ConditionDamageGeneBase
{
    /// <summary>条件增伤分类。</summary>
    public override GeneEffectType EffectType { get { return GeneEffectType.TargetBehindDamage; } }

    /// <summary>只在本次结算判定为背面命中时给出增伤，正面与侧面命中不加成。</summary>
    /// <param name="context">结算前的真实双方资源与背面判定。</param>
    /// <returns>背面命中返回按层数换算的百分点，否则返回零。</returns>
    public override double ResolveBonus(GeneDamageContext context)
    {
        if (!context.TargetBehind) return 0;
        return Value * StackCount;
    }
}
