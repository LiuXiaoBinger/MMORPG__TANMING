/// <summary>按毫秒间隔产生生命恢复的具体运行节点。</summary>
public class PeriodicHealGene : TimeBuffGeneBase
{
    /// <summary>直接执行本节点的资源效果，不创建中转结果。</summary>
    /// <param name="role">实际承受效果的角色或怪物。</param>
    /// <param name="args">本资源效果不依赖额外事件参数。</param>
    /// <returns>完成资源操作后返回 1。</returns>
    public override int OnCall(IGeneOwner role, GeneTriggerArgs args = default(GeneTriggerArgs))
    {
        // 治疗数值使用当前最大生命，并计入角色治疗增益和受疗修正。
        int amount = ResolveAmount(role.AttrComp.MaxHp);
        amount = System.Math.Max(0, (int)(amount * (1 + role.AttrComp.HealBonus / 100)
            * role.AttrComp.CalculateAttribute(GeneAttributeType.HealingReceived, 100) / 100));
        role.AttrComp.ChangeHp(amount, FirerId);
        return 1;
    }

    /// <summary>周期结果交由宿主执行生命恢复。</summary>
    public override GeneEffectType EffectType { get { return GeneEffectType.PeriodicHeal; } }
}
