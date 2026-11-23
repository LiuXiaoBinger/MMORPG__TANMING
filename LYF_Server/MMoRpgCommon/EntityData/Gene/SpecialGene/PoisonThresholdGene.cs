/// <summary>毒经受击触发时在攻击者身上按来源累计中毒；槽一承接满层效果，槽二可填周期伤害。</summary>
public sealed class PoisonThresholdGene : GeneBase
{
    /// <summary>同一施加者在同一目标上需要成功施毒的次数。</summary>
    public int Threshold { get; set; }
    /// <summary>仅在父事件触发时执行此次施毒。</summary>
    public override GeneEffectType EffectType { get { return GeneEffectType.PoisonThreshold; } }
    /// <summary>直接挂载不改变资源，必须经过真实受击事件。</summary>
    /// <param name="role">持有此节点的角色。</param><param name="args">当前战斗事件。</param>
    public override int OnCall(IGeneOwner role, GeneTriggerArgs args = default(GeneTriggerArgs)) { return 0; }
    /// <summary>按已成功的概率判定增加一层，满层后重新计数。</summary>
    /// <param name="role">受击的施毒者。</param><param name="args">原受击事件。</param><param name="target">发动攻击的对方。</param>
    internal override int ActivateFromEvent(IGeneOwner role, GeneTriggerArgs args, GeneRuntime target)
    {
        // 计数标记不携带关联树，避免未达次数时提前施加惩罚。
        StateGene marker = new StateGene { GeneID = GeneID, DisplayName = DisplayName, AllowsStacking = true,
            DurationSeconds = DurationSeconds, MaxStack = Threshold };
        GeneBase active = target.AddGene(marker, SourceId, 0, FirerId);
        if (Children.Count > 1) Children[1].ActivateFromEvent(role, args, target);
        if (active.StackCount >= Threshold)
        {
            target.RemoveGene(active);
            if (Children.Count > 0) Children[0].ActivateFromEvent(role, args, target);
        }
        return 1;
    }
}
