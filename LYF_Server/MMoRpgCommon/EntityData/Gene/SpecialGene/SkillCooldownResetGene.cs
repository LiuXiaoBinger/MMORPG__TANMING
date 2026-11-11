/// <summary>成功施法后刷新本次技能自身冷却的即时节点，对应「行云流水」类特技。</summary>
public class SkillCooldownResetGene : GeneBase
{
    /// <summary>本节点只清冷却，不施加任何属性或状态。</summary>
    public override GeneEffectType EffectType { get { return GeneEffectType.SkillCooldownReset; } }
    /// <summary>挂载自身不刷新冷却，必须有真实提交成功的施法事件。</summary>
    public override bool CallsOnAttach { get { return false; } }
    /// <summary>只在施法事件到达时执行。</summary>
    public override bool WaitsForEvent { get { return true; } }
    /// <summary>清冷却即时完成，不需要在角色身上保留节点寿命。</summary>
    public override bool IsInstant { get { return true; } }

    /// <summary>独立挂载时没有正在提交的技能，不能凭空清某个技能冷却。</summary>
    /// <param name="role">当前角色。</param>
    /// <param name="args">未被使用；冷却刷新回调只能由 ActivateFromEvent 传入。</param>
    /// <returns>始终返回 0，表示本次未刷新。</returns>
    public override int OnCall(IGeneOwner role, GeneTriggerArgs args = default(GeneTriggerArgs)) { return 0; }

    /// <summary>用事件携带的本次技能编号与冷却刷新回调清除该技能自己的剩余冷却。</summary>
    /// <param name="role">成功提交施法的原角色。</param>
    /// <param name="args">包含本次技能编号与冷却刷新回调的施法事件。</param>
    /// <param name="target">上层事件选中的容器；刷冷却不写目标，忽略它。</param>
    /// <returns>确实清除了正剩余冷却返回 1，否则返回 0。</returns>
    internal override int ActivateFromEvent(IGeneOwner role, GeneTriggerArgs args, GeneRuntime target)
    {
        if (args.SkillId <= 0) return 0;
        if (args.TryResetSkillCooldown == null) return 0;
        // 只有技能组件确认存在正剩余冷却才算本次刷新成功，避免无冷却也计一次。
        if (!args.TryResetSkillCooldown(args.SkillId)) return 0;
        return 1;
    }
}
