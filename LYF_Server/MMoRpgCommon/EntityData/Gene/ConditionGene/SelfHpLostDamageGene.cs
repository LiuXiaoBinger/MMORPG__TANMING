/// <summary>自身每损失一段生命就提高本次伤害的常驻节点，对应「低血增伤」。</summary>
public class SelfHpLostDamageGene : ConditionDamageGeneBase
{
    /// <summary>条件增伤分类。</summary>
    public override GeneEffectType EffectType { get { return GeneEffectType.SelfHpLostDamage; } }

    /// <summary>按攻击者已损失生命折算档数，每档 Condition 个百分点返回一份增伤。</summary>
    /// <param name="context">结算前的真实双方资源。</param>
    /// <returns>整数档数乘以配置百分点，未损失生命时返回零。</returns>
    public override double ResolveBonus(GeneDamageContext context)
    {
        if (Condition <= 0) return 0;
        // 只按完整档数计算，剩余不足一档的生命不产生额外加成。
        int steps = (int)(context.AttackerLostHpPercent / Condition);
        if (steps <= 0) return 0;
        return Value * StackCount * steps;
    }
}
