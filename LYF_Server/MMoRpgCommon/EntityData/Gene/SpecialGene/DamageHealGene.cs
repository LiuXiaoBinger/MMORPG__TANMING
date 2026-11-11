/// <summary>按本次实际正伤害的百分点即时回复攻击者生命；由攻击命中事件携带真实伤害执行。</summary>
public class DamageHealGene : InstantResourceGeneBase
{
    /// <summary>本次伤害转化比例，Value 为百分点，60 表示回复本次伤害的 60%。</summary>
    public override GeneEffectType EffectType { get { return GeneEffectType.DamageHeal; } }
    /// <summary>转换比例不按生命上限换算，数值语义固定为本次伤害的百分点。</summary>
    public override bool CallsOnAttach { get { return false; } }
    /// <summary>只在攻击命中事件到达时执行，挂载自身不回复。</summary>
    public override bool WaitsForEvent { get { return true; } }

    /// <summary>作为独立根节点时没有携带伤害的事件，不能凭空换算治疗量。</summary>
    /// <param name="role">当前角色。</param>
    /// <param name="args">未被事件使用；伤害只能由 ActivateFromEvent 传入。</param>
    /// <returns>始终返回 0，表示本次未回复。</returns>
    public override int OnCall(IGeneOwner role, GeneTriggerArgs args = default(GeneTriggerArgs)) { return 0; }

    /// <summary>用事件携带的实际扣血值换算治疗量，零伤害不回复也不消耗冷却。</summary>
    /// <param name="role">收到命中事件的原角色，也是实际回复对象。</param>
    /// <param name="args">包含本次实际伤害的攻击命中事件。</param>
    /// <param name="target">上层事件选中的容器；本节点固定在自身结算，不使用它。</param>
    /// <returns>实际回复了正生命返回 1，否则返回 0。</returns>
    internal override int ActivateFromEvent(IGeneOwner role, GeneTriggerArgs args, GeneRuntime target)
    {
        if (args.Damage <= 0) return 0;
        int amount = (int)(args.Damage * (double)(Value * StackCount) / 100);
        // 治疗量仍走角色治疗增益与受疗修正，与即时治疗基因保持一致。
        amount = System.Math.Max(0, (int)(amount * (1 + role.AttrComp.HealBonus / 100)
            * role.AttrComp.CalculateAttribute(GeneAttributeType.HealingReceived, 100) / 100));
        if (amount <= 0) return 0;
        role.AttrComp.ChangeHp(amount, FirerId);
        return 1;
    }
}
