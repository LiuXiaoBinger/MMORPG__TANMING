/// <summary>按伤害来源真实类别（首领或普通怪）降低本次受到伤害的常驻节点，对应「罗汉」类减伤。</summary>
public class AttackerKindReduceGene : ConditionReduceGeneBase
{
    /// <summary>条件减伤分类。</summary>
    public override GeneEffectType EffectType { get { return GeneEffectType.AttackerKindReduce; } }

    /// <summary>Condition 为 1 只对首领来源生效，为 2 只对普通怪来源生效，其他值不生效。</summary>
    /// <param name="attackerIsBoss">伤害来源是否为真实首领实体。</param>
    /// <returns>类别匹配返回按层数换算的减免百分点，否则返回零。</returns>
    public override double ResolveReduction(bool attackerIsBoss)
    {
        if (Condition == 1 && !attackerIsBoss) return 0;
        if (Condition == 2 && attackerIsBoss) return 0;
        if (Condition != 1 && Condition != 2) return 0;
        return Value * StackCount;
    }
}
